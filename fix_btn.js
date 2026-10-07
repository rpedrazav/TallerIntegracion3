const fs = require('fs');
let content = fs.readFileSync('globalmart-frontend/src/pages/ProductosPage.tsx', 'utf8');

// Fix broken templates in button
content = content.replace(/title=\{Editar \}/g, 'title={Editar \}');
content = content.replace(/title=\{Eliminar \}/g, 'title={Eliminar \}');
content = content.replace(/id=\{ tn-eliminar-producto-\}/g, 'id={tn-eliminar-producto-\}');
content = content.replace(/id=\{ tn-editar-producto-\}/g, 'id={tn-editar-producto-\}');

fs.writeFileSync('globalmart-frontend/src/pages/ProductosPage.tsx', content, 'utf8');
