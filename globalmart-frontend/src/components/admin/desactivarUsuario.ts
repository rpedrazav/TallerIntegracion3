export interface UsuarioDesactivable {
  id: string;
  nombre: string;
  activo: boolean;
}

export interface SolicitudDesactivacionUsuario {
  method: 'delete';
  path: string;
}

type Confirmar = (mensaje: string) => boolean;

export function confirmarDesactivacionUsuario(
  usuario: UsuarioDesactivable,
  confirmar: Confirmar,
): boolean {
  if (!usuario.activo) return false;
  return confirmar(
    `¿Confirmas que deseas desactivar a ${usuario.nombre}? El usuario permanecerá registrado como Inactivo.`,
  );
}

export function crearSolicitudDesactivacionUsuario(
  usuario: UsuarioDesactivable,
): SolicitudDesactivacionUsuario {
  return {
    method: 'delete',
    path: `/users/${usuario.id}`,
  };
}

export function marcarUsuarioInactivo<T extends UsuarioDesactivable>(
  usuarios: readonly T[],
  usuarioId: string,
): T[] {
  return usuarios.map(usuario =>
    usuario.id === usuarioId ? { ...usuario, activo: false } : usuario,
  );
}
