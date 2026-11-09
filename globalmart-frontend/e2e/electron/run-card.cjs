const { spawnSync } = require('node:child_process');
const path = require('node:path');
const cwd = path.resolve(__dirname, '../..');
if (!process.env.GLOBALMART_TEST_POSTGRES) {
  console.error('Define GLOBALMART_TEST_POSTGRES with a disposable PostgreSQL database. See e2e/electron/README.md.');
  process.exit(1);
}
for (const [command, args] of [
  [process.execPath, ['e2e/electron/build.cjs']],
  ['dotnet', ['test', '../tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj',
    '--filter', 'Plan=S2-055', '--logger', 'trx;LogFileName=S2-055.trx', '--results-directory', '../docs/qa/S2-055']],
]) {
  const result = spawnSync(command, args, { cwd, stdio: 'inherit',
    env: { ...process.env, GLOBALMART_ELECTRON_E2E: '1' } });
  if (result.error) console.error(result.error);
  if (result.status !== 0) process.exit(result.status || 1);
}
