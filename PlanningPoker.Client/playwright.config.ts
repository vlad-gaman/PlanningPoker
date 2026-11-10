import { defineConfig, devices } from '@playwright/test';

// E2E_PORT lets a second checkout run beside another server on the default port.
const port = Number(process.env['E2E_PORT'] ?? 5100);

// Runs against the real API serving the built SPA from wwwroot (run `npm run build:api` first).
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 1 : 0,
  reporter: process.env['CI'] ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: `http://localhost:${port}`,
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: process.env['E2E_SERVER_COMMAND'] ?? 'dotnet run --project ../PlanningPoker.Api --no-launch-profile',
    env: { ASPNETCORE_URLS: `http://localhost:${port}`, ASPNETCORE_ENVIRONMENT: 'Production' },
    url: `http://localhost:${port}`,
    reuseExistingServer: !process.env['CI'],
    timeout: 120_000,
  },
});
