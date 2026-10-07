const fs = require('fs');
let content = fs.readFileSync('globalmart-frontend/src/pages/ProductosPage.tsx', 'utf8');

content = content.replace(
  "if (!window.confirm(Ests seguro de que deseas eliminar permanentemente el producto ''?)) return;",
  "if (!window.confirm(¿Estás seguro de que deseas eliminar permanentemente el producto ''?)) return;"
);
content = content.replace(
  "await axios.delete(${CATALOG_URL}/products/, {",
  "await axios.delete(${CATALOG_URL}/products/, {"
);
content = content.replace(
  "headers: { Authorization: Bearer  }",
  "headers: { Authorization: Bearer  }"
);
content = content.replace(
  "id={ tn-eliminar-producto-}",
  "id={tn-eliminar-producto-}"
);

fs.writeFileSync('globalmart-frontend/src/pages/ProductosPage.tsx', content, 'utf8');
