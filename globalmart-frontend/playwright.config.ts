import { defineConfig } from '@playwright/test';
export default defineConfig({
  testIgnore: '**/electron/**',
  testDir: './e2e', timeout: 20000, retries: 0, workers: 1,
  reporter: [['list'], ['html', { open: 'never' }], ['json', { outputFile: 'test-results/results.json' }]],
  use: { baseURL: 'http://127.0.0.1:4175', channel: 'chrome', viewport: { width: 1440, height: 900 }, screenshot: 'only-on-failure', trace: 'retain-on-failure' },
  webServer: { command: 'node e2e/serve.cjs', url: 'http://127.0.0.1:4175', reuseExistingServer: false }
});
