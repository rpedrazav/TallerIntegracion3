const fs = require('fs');
let content = fs.readFileSync('globalmart-frontend/src/pages/ProductosPage.tsx', 'utf8');

const regex = /const handleEliminar = async \(id: string, nombre: string\) => \{[\s\S]*?fetchDatos\(\);\r?\n    \} catch \(error\)/m;
const replacement = "const handleEliminar = async (id: string, nombre: string) => {\n" +
"    if (!window.confirm(¿Estás seguro de que deseas eliminar permanentemente el producto ''?)) return;\n" +
"    try {\n" +
"      const token = await window.api?.getToken();\n" +
"      if (!token) return;\n" +
"      await axios.delete(${CATALOG_URL}/products/, {\n" +
"        headers: { Authorization: Bearer  }\n" +
"      });\n" +
"      fetchDatos();\n" +
"    } catch (error)";

content = content.replace(regex, replacement);
content = content.replace(/id=\{ tn-eliminar-producto-\}/g, "id={tn-eliminar-producto-}");

fs.writeFileSync('globalmart-frontend/src/pages/ProductosPage.tsx', content, 'utf8');
