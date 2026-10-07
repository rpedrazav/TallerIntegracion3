const fs = require('fs');
let content = fs.readFileSync('src/pages/ProductosPage.tsx', 'utf8');
const replacement = fs.readFileSync('tbody.txt', 'utf8');

const regex = /<tbody>[\s\S]*?<\/tbody>/;
content = content.replace(regex, replacement);
fs.writeFileSync('src/pages/ProductosPage.tsx', content, 'utf8');
