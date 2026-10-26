'use strict';
// Executes production Pos.tsx total expressions and server-renders real CartItem.
// No mounted Electron session, browser DOM, or renderer network request is claimed.
const assert = require('node:assert/strict');
const fs = require('node:fs'); const path = require('node:path'); const vm = require('node:vm');
const [root, json] = process.argv.slice(2); const venta = JSON.parse(json);
const frontend = path.join(root, 'globalmart-frontend');
const ts = require(path.join(frontend, 'node_modules/typescript'));
const React = require(path.join(frontend, 'node_modules/react'));
const { renderToStaticMarkup } = require(path.join(frontend, 'node_modules/react-dom/server'));
const options = { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.React, esModuleInterop: true };
const filename = path.join(frontend, 'src/pages/Pos.tsx');
const source = fs.readFileSync(filename, 'utf8'); const ast = ts.createSourceFile(filename, source, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX);
const wanted = new Set(['subtotal', 'tasaIva', 'iva', 'total']); const found = new Map();
function visit(node) {
  if (ts.isVariableDeclaration(node) && ts.isIdentifier(node.name) && wanted.has(node.name.text)) {
    assert.ok(!found.has(node.name.text), `Ambiguous total declaration ${node.name.text}`);
    found.set(node.name.text, node.getText(ast));
  }
  ts.forEachChild(node, visit);
}
visit(ast); assert.equal(found.size, 4);
const context = { venta, draftSubtotal: 99999999, Math };
vm.runInNewContext([...wanted].map(name => `const ${found.get(name)};`).join('\n') + '\nglobalThis.amounts = { subtotal, iva, total };', context);
assert.equal(context.amounts.subtotal, venta.subtotal); assert.equal(context.amounts.iva, venta.impuestos); assert.equal(context.amounts.total, venta.total);
const cartPath = path.join(frontend, 'src/components/pos/CartItem.tsx');
const moduleBox = { exports: {} };
vm.runInNewContext(ts.transpileModule(fs.readFileSync(cartPath, 'utf8'), { compilerOptions: options, fileName: cartPath }).outputText,
  { module: moduleBox, exports: moduleBox.exports, require: name => { assert.equal(name, 'react'); return React; } });
for (const item of venta.items) {
  const html = renderToStaticMarkup(React.createElement(moduleBox.exports.default, {
    id: 's2084', name: 'Pesable', price: item.precio_unitario, quantity: item.cantidad,
    subtotal: item.subtotal, isWeightVariable: true, onQuantityChange() {}, onRemove() {},
  }));
  assert.ok(html.includes(`${item.cantidad.toFixed(3)} kg`), html);
  assert.ok(html.includes(`$ ${(item.subtotal).toLocaleString('es-CL')}`), html);
}
console.log(`PASS production renderer expressions + CartItem: ${venta.subtotal}/${venta.impuestos}/${venta.total}`);
