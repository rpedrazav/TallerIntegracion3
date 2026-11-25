// Fixture compartido de las suites @integrado (S2-112, S2-113): Electron real contra MS-1..MS-5 en localhost.
import { test as base, expect, _electron, ElectronApplication, Page, Response } from '@playwright/test';
import { execFileSync } from 'node:child_process';
import * as fs from 'node:fs';
import * as path from 'node:path';

export { expect };
export const tenantId = 'aaaaaaaa-0000-0000-0000-000000000001';
export const MS1 = 'http://localhost:5124';
export const MS3 = 'http://localhost:5203';
export const MS5 = 'http://localhost:5000';

export async function api(url: string, token?: string, init: RequestInit = {}) {
  const res = await fetch(url, { ...init, headers: { 'Content-Type': 'application/json', 'x-tenant-id': tenantId, ...(token ? { Authorization: `Bearer ${token}` } : {}) } });
  return { status: res.status, body: await res.json().catch(() => null) };
}
export const tokenOf = async (email: string) =>
  (await api(`${MS1}/auth/login`, undefined, { method: 'POST', body: JSON.stringify({ email, password: 'demo1234', tenantId }) })).body.token as string;

/** Consulta escalar en la BD de MS-5 (postgres-pos de docker compose). */
export const posDb = (sql: string) => execFileSync('docker', ['exec', 'postgres-pos', 'sh', '-c',
  `psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -tAc '${sql.replace(/'/g, `'"'"'`)}'`]).toString().trim();

export const test = base.extend<{ app: Page; electron: ElectronApplication; apiErrors: string[] }>({
  apiErrors: async ({}, use) => use([]),
  electron: async ({}, use, info) => {
    const profile = info.outputPath('profile');
    fs.mkdirSync(profile, { recursive: true });
    // El terminal simulado del main "lee" la tarjeta; el resultado del cobro lo decide la pasarela simulada vía MS-5.
    const env: NodeJS.ProcessEnv = { ...process.env, E2E_USER_DATA: profile, E2E_INTEGRATED: '1', MOCK_TERMINAL_SCENARIO: 'approved' };
    delete env.ELECTRON_RUN_AS_NODE;
    const electron = await _electron.launch({ args: [path.resolve('e2e/electron/launch.cjs')], env });
    await use(electron);
    await electron.close();
  },
  app: async ({ electron, apiErrors }, use, info) => {
    await expect.poll(() => electron.windows().some(w => w.url().startsWith('file:'))).toBe(true);
    const page = electron.windows().find(w => w.url().startsWith('file:'))!;
    await page.waitForLoadState('domcontentloaded');
    await electron.evaluate(({ BrowserWindow }) => BrowserWindow.getAllWindows().forEach(w => {
      if (w.webContents.isDevToolsOpened()) {
        w.webContents.closeDevTools();
      }
      w.setSize(1440, 900);
    })).catch(() => undefined);
    await page.goto(page.url().replace('index.html', 'integrated.html'));
    // Toda respuesta >= 400 es un error de API, salvo las que la prueba declara esperadas.
    page.on('response', (r: Response) => {
      const expected404 = r.status() === 404 && r.url().endsWith('/api/turnos/activo');
      const declared = (info.annotations.find(a => a.type === 'api-esperada')?.description ?? '').split(',').filter(Boolean)
        .some(code => r.status() === Number(code));
      if (r.url().startsWith('http://localhost') && r.status() >= 400 && !expected404 && !declared) apiErrors.push(`${r.request().method()} ${r.url()} → ${r.status()}`);
    });
    page.on('dialog', d => d.accept());
    await electron.context().tracing.start({ screenshots: true, snapshots: true });
    try {
      await use(page);
      expect(apiErrors, 'El camino no debe tener errores de API inesperados').toEqual([]);
    } finally {
      if (info.status !== info.expectedStatus) {
        await page.screenshot({ path: info.outputPath('failure.png') }).catch(() => undefined);
        await electron.context().tracing.stop({ path: info.outputPath('trace.zip') });
      } else await electron.context().tracing.stop();
    }
  },
});

export async function login(page: Page, email: string) {
  await page.locator('input[type=text]').fill(tenantId);
  await page.locator('input[type=email]').fill(email);
  await page.locator('input[type=password]').fill('demo1234');
  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
}
export const snap = (page: Page, name: string) => page.screenshot({ path: test.info().outputPath(`${name}.png`) });
