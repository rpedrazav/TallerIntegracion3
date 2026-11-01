// Test-only bootstrap: production main/preload/renderer, isolated persistent store.
const { app, session } = require('electron');
const path = require('node:path');
if (!process.env.E2E_USER_DATA) throw new Error('E2E_USER_DATA is required');
app.setPath('userData', process.env.E2E_USER_DATA);
app.whenReady().then(() => {
  // Only the synthetic destination can reach Playwright's interception layer.
  // The fixture aborts every request without an explicit matching mock.
  session.defaultSession.webRequest.onBeforeRequest({ urls: ['http://*/*', 'https://*/*'] },
    (details, callback) => callback({ cancel: new URL(details.url).origin !== 'http://127.0.0.1:4199' }));
});
require(path.resolve(__dirname, '../../.webpack/electron-e2e/main.js'));
