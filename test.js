const axios = require('axios');
const https = require('https');

const agent = new https.Agent({ rejectUnauthorized: false });

axios.post('https://api-rpedraza.dev.censei.cl/auth/login', {
  email: 'cajero@demo.cl',
  password: 'password',
  tenantId: 'aaaaaaaa-0000-0000-0000-000000000000'
}, { httpsAgent: agent, headers: { 'x-tenant-id': 'aaaaaaaa-0000-0000-0000-000000000000' } })
.then(res => console.log('SUCCESS:', res.status, res.data))
.catch(err => console.log('ERROR:', err.response ? err.response.status : err.message, err.response ? err.response.data : ''));
