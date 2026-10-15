import React from 'react';
import { createRoot } from 'react-dom/client';
import { HashRouter } from 'react-router-dom';
import Pos from '../src/pages/Pos';
import TenantConfig from '../src/components/admin/TenantConfig';
import '../src/index.css';

// Renderer real; el puente de sesión se inyecta desde Playwright.
const tenantConfigToken = [
  btoa(JSON.stringify({ alg: 'none', typ: 'JWT' })),
  btoa(JSON.stringify({ tenant_id: 'tenant-a', roles: ['ADMIN'] })),
  '',
].join('.');

const component = new URLSearchParams(window.location.search).get('component');

createRoot(document.getElementById('root')!).render(
  component === 'tenant-config'
    ? <TenantConfig token={tenantConfigToken} />
    : <HashRouter><Pos /></HashRouter>
);
