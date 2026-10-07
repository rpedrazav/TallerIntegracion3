const fs = require('fs');
let content = fs.readFileSync('src/pages/Pos.tsx', 'utf8');

// Replace the handleTypingSearch and handleSearch logic
// We also need to add `todosProductos` state.

const stateRegex = /const \[searchResults, setSearchResults\] = useState<any\[\]>\(\[\]\);/;
const stateReplacement = `const [searchResults, setSearchResults] = useState<any[]>([]);
  const [todosProductos, setTodosProductos] = useState<any[]>([]);

  useEffect(() => {
    const fetchTodos = async () => {
      try {
        const token = await window.api?.getToken();
        if (!token) return;
        const response = await axios.get(\`\${CATALOG_URL}/products\`, {
          params: { page: 1, pageSize: 500 },
          headers: { Authorization: \`Bearer \${token}\` }
        });
        setTodosProductos(response.data?.data ?? response.data ?? []);
      } catch (e) {
        console.error('Error al cargar catalogo POS', e);
      }
    };
    fetchTodos();
  }, []);`;

content = content.replace(stateRegex, stateReplacement);

const handleTypingSearchRegex = /const handleTypingSearch = useCallback\(async \(query: string\) => \{[\s\S]*?\}, \[\]\);/;
const handleTypingSearchReplacement = `const handleTypingSearch = useCallback((query: string) => {
    if (!query || query.trim() === '') {
      setSearchResults([]);
      return;
    }
    const q = query.trim().toLowerCase();
    const filtrados = todosProductos.filter((p: any) => 
      (p.nombre && p.nombre.toLowerCase().includes(q)) || 
      (p.codigoBarras && p.codigoBarras.includes(q))
    ).slice(0, 15);
    
    setSearchResults(filtrados);
  }, [todosProductos]);`;

content = content.replace(handleTypingSearchRegex, handleTypingSearchReplacement);

const handleSearchRegex = /const handleSearch = useCallback\(async \(query: string\) => \{[\s\S]*?\}, \[\]\);/;
const handleSearchReplacement = `const handleSearch = useCallback(async (query: string) => {
    const q = query.trim();
    if (!q) return;

    const exactMatch = todosProductos.find(p => p.codigoBarras === q);
    if (exactMatch) {
      addProductToCart(exactMatch);
      return;
    }

    const qLower = q.toLowerCase();
    const matches = todosProductos.filter(p => 
      (p.nombre && p.nombre.toLowerCase().includes(qLower)) || 
      (p.codigoBarras && p.codigoBarras.includes(qLower))
    );

    if (matches.length === 1) {
      addProductToCart(matches[0]);
    } else if (matches.length > 1) {
      setSearchResults(matches.slice(0, 15));
    } else {
      try {
        const token = await window.api?.getToken();
        if (!token) return;
        const response = await axios.get(\`\${CATALOG_URL}/products/lookup\`, {
          params: { barcode: query },
          headers: { Authorization: \`Bearer \${token}\` }
        });
        if (response.data) addProductToCart(response.data);
      } catch (error: any) {
        if (error.response?.status === 404) {
          alert(\`Producto con el término '\${query}' no encontrado.\`);
        }
      }
    }
  }, [todosProductos, addProductToCart]);`;

content = content.replace(handleSearchRegex, handleSearchReplacement);
fs.writeFileSync('src/pages/Pos.tsx', content, 'utf8');
