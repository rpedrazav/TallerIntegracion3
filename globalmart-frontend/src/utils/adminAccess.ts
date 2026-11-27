export function canAccessAdmin(esAdmin: boolean): boolean {
  return esAdmin === true;
}

/** Pantalla inicial según el rol activo: SUPER_ADMIN gestiona minimarkets, ADMIN su tienda, el resto vende. */
export function rutaInicial(rolActivo?: string): string {
  const rol = rolActivo?.toUpperCase();
  if (rol === 'SUPER_ADMIN') return '/admin/minimarkets';
  return rol === 'ADMIN' ? '/admin' : '/pos';
}
