const fs = require('fs');
let content = fs.readFileSync('globalmart-frontend/src/pages/ProductosPage.tsx', 'utf8');

content = content.replace(/if \(\!window\.confirm\(.*?\)\) return;/g, "if (!window.confirm(¿Estás seguro de que deseas eliminar permanentemente el producto ''?)) return;");
content = content.replace(/await axios\.delete\([^,]+, \{/g, "await axios.delete(\/products/\, {");
content = content.replace(/headers: \{ Authorization: Bearer  \}/g, "headers: { Authorization: Bearer \ }");
content = content.replace(/id=\{ tn-eliminar-producto-\}/g, "id={tn-eliminar-producto-\}");

fs.writeFileSync('globalmart-frontend/src/pages/ProductosPage.tsx', content, 'utf8');
