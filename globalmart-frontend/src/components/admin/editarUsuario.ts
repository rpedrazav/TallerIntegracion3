export interface UsuarioEditable {
  id: string;
  email: string;
}

export interface SolicitudEdicionUsuario {
  method: 'put';
  path: string;
  body: {
    email: string;
  };
}

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function obtenerEmailInicial(usuario: UsuarioEditable): string {
  return usuario.email;
}

export function validarEmailEdicion(email: string): string | undefined {
  const normalizado = email.trim();
  if (!normalizado) return 'El correo es obligatorio.';
  if (!EMAIL_REGEX.test(normalizado)) return 'Ingresa un correo válido.';
  if (normalizado.length > 200) return 'El correo no puede superar los 200 caracteres.';
  return undefined;
}

export function crearSolicitudEdicionUsuario(
  usuario: UsuarioEditable,
  email: string,
): SolicitudEdicionUsuario {
  return {
    method: 'put',
    path: `/users/${usuario.id}`,
    body: { email: email.trim().toLowerCase() },
  };
}
