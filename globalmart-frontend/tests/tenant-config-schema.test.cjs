const { readFileSync } = require('node:fs');
const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const ts = require('typescript');
const { z } = require('zod');

test('TenantConfig convierte porcentajeIva string a number para el submit', () => {
  const componentPath = path.resolve(__dirname, '../src/components/admin/TenantConfig.tsx');
  const sourceText = readFileSync(componentPath, 'utf8');
  const sourceFile = ts.createSourceFile(
    componentPath,
    sourceText,
    ts.ScriptTarget.Latest,
    true,
    ts.ScriptKind.TSX
  );

  let schemaExpression;
  sourceFile.forEachChild(node => {
    if (!ts.isVariableStatement(node)) return;
    const declaration = node.declarationList.declarations.find(
      item => ts.isIdentifier(item.name) && item.name.text === 'configSchema'
    );
    if (declaration?.initializer) {
      schemaExpression = declaration.initializer.getText(sourceFile);
    }
  });

  assert.ok(schemaExpression, 'TenantConfig debe declarar configSchema');
  const configSchema = Function('z', `return (${schemaExpression});`)(z);
  const formInput = {
    pais: 'CL',
    moneda: 'CLP',
    idioma: 'es',
    porcentajeIva: '18.5',
    zonaHoraria: 'America/Santiago',
  };

  const submitData = configSchema.parse(formInput);

  assert.equal(submitData.porcentajeIva, 18.5);
  assert.equal(typeof submitData.porcentajeIva, 'number');
  assert.notEqual(submitData.porcentajeIva, formInput.porcentajeIva);
  assert.equal(typeof submitData, 'object');
});
