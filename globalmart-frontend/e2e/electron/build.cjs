const webpack = require('webpack');
const path = require('node:path');
const fs = require('node:fs');
const { pathToFileURL } = require('node:url');
const root = path.resolve(__dirname, '../..');
const output = path.join(root, '.webpack/electron-e2e');
const rules = [
  { test: /\.tsx?$/, exclude: /node_modules/, use: { loader: 'ts-loader', options: { transpileOnly: true } } },
  { test: /\.css$/, use: ['style-loader', 'css-loader'] },
];
const config = (name, target, entry, plugins = []) => ({
  mode: 'development', context: root, target, entry: path.join(root, entry),
  output: { path: output, filename: `${name}.js` },
  resolve: { extensions: ['.tsx', '.ts', '.js'] }, module: { rules }, plugins,
});
webpack([
  config('main', 'electron-main', 'src/index.ts', [new webpack.DefinePlugin({
    MAIN_WINDOW_WEBPACK_ENTRY: JSON.stringify(pathToFileURL(path.join(output, 'index.html')).href),
    MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY: JSON.stringify(path.join(output, 'preload.js')),
  })]),
  config('preload', 'electron-preload', 'src/preload.ts'),
  config('renderer', 'web', 'src/renderer.tsx'),
], (error, stats) => {
  if (error || stats.hasErrors()) { console.error(error || stats.toString()); process.exitCode = 1; return; }
  fs.writeFileSync(path.join(output, 'index.html'), `<!doctype html><html><meta charset="utf-8"><title>GlobalMart E2E</title><div id="root"></div><script>window.env={API_MODE:'local',IDENTITY_API_URL:'http://127.0.0.1:4199',CATALOG_API_URL:'http://127.0.0.1:4199',POS_API_URL:'http://127.0.0.1:4199'}</script><script src="renderer.js"></script></html>`);
});
