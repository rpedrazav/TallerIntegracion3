const webpack = require('webpack');
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const output = path.resolve(__dirname, '../.webpack/e2e');
webpack({ mode: 'development', entry: path.join(__dirname, 'renderer.tsx'),
  output: { path: output, filename: 'app.js' },
  resolve: { extensions: ['.tsx', '.ts', '.js'] },
  module: { rules: [
    { test: /\.tsx?$/, exclude: /node_modules/, use: { loader: 'ts-loader', options: { transpileOnly: true } } },
    { test: /\.css$/, use: ['style-loader', 'css-loader'] }
  ] }
}, (error, stats) => {
  if (error || stats.hasErrors()) { console.error(error || stats.toString()); process.exit(1); }
  http.createServer((req, res) => {
    if (req.url === '/app.js') { res.setHeader('Content-Type', 'application/javascript'); fs.createReadStream(path.join(output, 'app.js')).pipe(res); }
    else { res.setHeader('Content-Type', 'text/html'); res.end('<!doctype html><html><meta charset="utf-8"><title>Regresión S2-025</title><div id="root"></div><script src="/app.js"></script></html>'); }
  }).listen(4175, '127.0.0.1');
});
