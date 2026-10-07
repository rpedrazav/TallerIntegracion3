const fs = require('fs');
let content = fs.readFileSync('globalmart-frontend/src/pages/ProductosPage.tsx', 'utf8');

// Use regex with wildcards to catch mangled lines safely
content = content.replace(/const handleEliminar = async \(id: string, nombre: string\) => \{[\s\S]*?fetchDatos\(\);\n    \} catch \(error\)/,
\const handleEliminar = async (id: string, nombre: string) => {
    if (!window.confirm(\\\¿Estás seguro de que deseas eliminar permanentemente el producto '\'?\\\)) return;
    try {
      const token = await window.api?.getToken();
      if (!token) return;
      await axios.delete(\\\\/products/\\\\, {
        headers: { Authorization: \\\Bearer \\\\ }
      });
      fetchDatos();
    } catch (error)\);

content = content.replace(/id=\{ tn-eliminar-producto-\}/g, "id={\tn-eliminar-producto-\\}");

fs.writeFileSync('globalmart-frontend/src/pages/ProductosPage.tsx', content, 'utf8');
