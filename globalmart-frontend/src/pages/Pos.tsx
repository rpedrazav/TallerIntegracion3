export default function Pos() {
  return (
    <div style={{ padding: '1.5rem', backgroundColor: '#ffffff', borderRadius: '8px', border: '1px solid #e5e7eb', boxShadow: '0 1px 3px rgba(0,0,0,0.05)' }}>
      <h2 style={{ margin: 0, color: '#111827', fontSize: '1.5rem' }}>Punto de Venta (POS)</h2>
      <p style={{ color: '#4b5563', marginTop: '0.5rem' }}>
        Sesión activa con rol <strong>CAJERO</strong>. El token está guardado en <code>electron-store</code>.
      </p>
    </div>
  );
}
