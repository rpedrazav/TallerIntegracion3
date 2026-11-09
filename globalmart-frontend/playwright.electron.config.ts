import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './e2e/electron', timeout: 30000, workers: 1, retries: 0,
  forbidOnly: !!process.env.CI,
  outputDir: 'test-results/electron',
  reporter: [['list'], ['html', { outputFolder: 'playwright-report/electron', open: 'never' }]],
});
