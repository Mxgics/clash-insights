import { defineConfig } from '@playwright/test';
export default defineConfig({
  testDir: './e2e',
  workers: 1,
  use: { browserName: 'chromium', viewport: { width: 1440, height: 1000 } },
  reporter: 'list',
});
