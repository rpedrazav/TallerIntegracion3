const fs = require('fs');
const axios = require('axios');
const path = require('path');

const CATALOG_URL = 'https://catalog-rpedraza.dev.censei.cl';
const DEFAULT_UOM_ID = '00000000-0000-0000-0000-000000000001';

const configPath = path.join(process.env.APPDATA, 'globalmart-frontend', 'config.json');
let token = null;
try {
  const config = JSON.parse(fs.readFileSync(configPath, 'utf8'));
  token = config.jwt_token;
} catch(e) {}

const prod = {
    nombre: "Test Product",
    descripcion: "Test",
    codigoBarras: "123456789",
    precioBase: 1000,
    uomBaseId: DEFAULT_UOM_ID,
    esPesoVariable: false,
    isActive: true
};

async function run() {
    try {
      const resp = await axios.post(`${CATALOG_URL}/products`, prod, {
        headers: { Authorization: `Bearer ${token}` }
      });
      console.log("Success:", resp.data);
    } catch (e) {
      console.error("Error status:", e.response?.status);
      console.error("Error data:", e.response?.data);
      console.error("Error message:", e.message);
    }
}
run();
