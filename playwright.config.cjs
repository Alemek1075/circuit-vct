const { defineConfig, devices } = require('@playwright/test');
const fs = require('node:fs');
const path = require('node:path');

const tempDir = path.resolve('tests/.tmp');
fs.mkdirSync(tempDir, { recursive: true });
const database = path.join(tempDir, `circuit-${process.pid}.db`);
const baseURL = 'http://127.0.0.1:5307';

module.exports = defineConfig({
  testDir: './tests/e2e',
  timeout: 30000,
  expect: { timeout: 5000 },
  use: {
    baseURL,
    ...devices['Desktop Chrome'],
    ...(process.platform === 'win32' ? { channel: 'msedge' } : {})
  },
  webServer: {
    command: `dotnet run --project src/Circuit/Circuit.csproj --no-build -c Release --no-launch-profile --urls ${baseURL}`,
    url: `${baseURL}/health`,
    timeout: 120000,
    reuseExistingServer: false,
    env: {
      ASPNETCORE_ENVIRONMENT: 'Development',
      ConnectionStrings__Circuit: `Data Source=${database}`
    }
  }
});
