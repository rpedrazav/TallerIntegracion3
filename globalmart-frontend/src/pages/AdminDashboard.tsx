import React from 'react';
import { Link } from 'react-router-dom';

export default function AdminDashboard() {
  const sections = [
    ['/admin/usuarios', 'Usuarios', 'Crear, editar y desactivar cuentas del tenant.'],
    ['/admin/roles', 'Roles', 'Asignar funciones a usuarios activos.'],
    ['/admin/inventario', 'Inventario', 'Consultar existencias por producto y sucursal.'],
    ['/admin/sucursales', 'Sucursales', 'Crear y consultar sucursales del tenant.'],
    ['/admin/productos', 'Productos', 'Mantener el catálogo y precios.'],
    ['/admin/configuracion', 'Configuración del tenant', 'Gestionar datos regionales y fiscales.'],
  ];
  return <section>
    <h2>Administración del tenant</h2>
    <p>Selecciona un área para gestionar tu tienda.</p>
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(230px, 1fr))', gap: '1rem' }}>
      {sections.map(([to, name, description]) => <Link key={to} to={to} style={{ display: 'block', padding: '1.25rem', border: '1px solid var(--color-line)', borderRadius: 12, background: 'var(--color-surface)', color: 'var(--color-ink)', textDecoration: 'none' }}>
        <strong>{name}</strong><p>{description}</p>
      </Link>)}
    </div>
  </section>;
}
