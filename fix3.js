const fs = require('fs');
let content = fs.readFileSync('globalmart-frontend/src/pages/ProductosPage.tsx', 'utf8');

content = content.replace("await axios.delete(/products/, {", "await axios.delete(\/products/\, {");
content = content.replace("id={ tn-eliminar-producto-}", "id={tn-eliminar-producto-\}");
content = content.replace("Ests seguro de que deseas eliminar permanentemente el producto ''?", "¿Estás seguro de que deseas eliminar permanentemente el producto ''?");

fs.writeFileSync('globalmart-frontend/src/pages/ProductosPage.tsx', content, 'utf8');
