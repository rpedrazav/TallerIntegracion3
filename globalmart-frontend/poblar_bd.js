const fs = require('fs');
const axios = require('axios');
const path = require('path');

process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0'; // Ignorar certificados autofirmados

const CATALOG_URL = 'https://catalog-rpedraza.dev.censei.cl';
const DEFAULT_UOM_ID = '00000000-0000-0000-0000-000000000001';

const configPath = path.join(process.env.APPDATA, 'globalmart-frontend', 'config.json');
let token = null;
try {
  const config = JSON.parse(fs.readFileSync(configPath, 'utf8'));
  token = config.jwt_token;
} catch(e) {}

const grocerieNames = [
  "Leche Entera", "Leche Descremada", "Pan de Molde Blanco", "Pan de Molde Integral",
  "Coca Cola Regular 2L", "Coca Cola Zero 2L", "Sprite 2L", "Fanta 2L",
  "Arroz Grado 1 1kg", "Arroz Grado 2 1kg", "Fideos Espagueti 400g", "Fideos Espirales 400g",
  "Salsa de Tomate Natural", "Salsa de Tomate Italiana", "Aceite Maravilla 1L", "Aceite de Oliva 500ml",
  "Huevos Blancos x12", "Huevos Color x12", "Mantequilla con Sal 250g", "Margarina 500g",
  "Queso Gouda Laminado 250g", "Queso Mantecoso 250g", "Jamon Pierna Acuenta 250g", "Jamon Pavo 250g",
  "Yogurt Frutilla 125g", "Yogurt Vainilla 125g", "Cereal Chocapic 500g", "Cereal Trix 500g",
  "Galletas Triton", "Galletas Vino", "Café Instantáneo Nescafé 100g", "Té Supremo 100 bols",
  "Azúcar Blanca 1kg", "Sal Fina 1kg", "Harina sin polvos 1kg", "Harina con polvos 1kg",
  "Atún Lomitos en Agua", "Atún Lomitos en Aceite", "Jugo Naranja en Polvo", "Jugo Manzana Caja 1L",
  "Agua Mineral con Gas 1.5L", "Agua Mineral sin Gas 1.5L", "Cerveza Cristal Lata 355cc", "Vino Tinto Casillero",
  "Papas Fritas Lays 100g", "Doritos Queso 100g", "Chocolate Trencito 150g", "Helado Savory Vainilla 1L",
  "Detergente Omo 1kg", "Lavalozas Quix 500ml", "Papel Higiénico Confort 40m", "Toalla Nova Clasica",
  "Pasta Dental Colgate", "Jabón Líquido Ballerina", "Shampoo Head & Shoulders", "Acondicionador Pantene",
  "Desodorante Rexona", "Pañales Huggies M", "Comida Perro Pedigree 3kg", "Comida Gato Whiskas 1kg",
  "Carbón Vegetal 2kg", "Servilletas Favorita 100 un", "Lavandina Clorox 1L", "Desengrasante Cif"
];

const generatedProducts = [];
for (let i = 0; i < 300; i++) {
  const baseName = grocerieNames[i % grocerieNames.length];
  const modifiers = ["Premium", "Económico", "Familiar", "Light", "Pro", "Extra", "Clásico"];
  const modifier = modifiers[Math.floor(Math.random() * modifiers.length)];
  const finalName = i >= grocerieNames.length ? `${baseName} ${modifier} v${i}` : baseName;
  const barcode = Math.floor(1000000000000 + Math.random() * 9000000000000).toString();
  const precio = Math.floor(Math.random() * 450) * 10 + 500;

  generatedProducts.push({
    nombre: finalName,
    descripcion: "Producto inyectado automáticamente",
    codigoBarras: barcode,
    precioBase: precio,
    uomBaseId: DEFAULT_UOM_ID,
    esPesoVariable: false,
    isActive: true
  });
}

async function run() {
  console.log(`Reintentando inyección de ${generatedProducts.length} productos...`);
  let success = 0;
  let error = 0;

  for (const prod of generatedProducts) {
    try {
      await axios.post(`${CATALOG_URL}/products`, prod, {
        headers: { Authorization: `Bearer ${token}` }
      });
      success++;
      process.stdout.write(`\rProgreso: ${success}/${generatedProducts.length} insertados.`);
    } catch (e) {
      error++;
      if(error === 1) console.error("\nPrimer error:", e.response?.data || e.message);
    }
  }
  console.log(`\n¡Finalizado! Insertados: ${success}, Errores: ${error}`);
}

run();
