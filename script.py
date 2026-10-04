import re

with open('C:/Users/kkk/Desktop/TallerIntegracion3/src/TenantIdentityService/Controllers/UsuarioController.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# For GET
content = re.sub(r'(\s+)\[HttpGet\]', r'\1/// <summary>\1/// Obtiene todos los usuarios del tenant.\1/// </summary>\1/// <returns>Lista de usuarios.</returns>\1/// <response code="200">Retorna la lista de usuarios.</response>\1/// <response code="401">No autorizado.</response>\1/// <response code="403">No tienes permisos.</response>\1[HttpGet]', content)

# For POST
content = re.sub(r'(\s+)\[HttpPost\]', r'\1/// <summary>\1/// Crea un nuevo usuario en el tenant.\1/// </summary>\1/// <param name="dto">Datos del nuevo usuario.</param>\1/// <returns>El usuario creado.</returns>\1/// <response code="201">Usuario creado exitosamente.</response>\1/// <response code="400">Datos inválidos.</response>\1/// <response code="401">No autorizado.</response>\1/// <response code="403">No tienes permisos.</response>\1[HttpPost]', content)

# For PUT
content = re.sub(r'(\s+)\[HttpPut\("\{id:guid\}"\)\]', r'\1/// <summary>\1/// Actualiza un usuario existente.\1/// </summary>\1/// <param name="id">ID del usuario.</param>\1/// <param name="dto">Nuevos datos.</param>\1/// <returns>El usuario actualizado.</returns>\1/// <response code="200">Usuario actualizado.</response>\1/// <response code="400">Datos inválidos.</response>\1/// <response code="401">No autorizado.</response>\1/// <response code="403">No tienes permisos.</response>\1/// <response code="404">Usuario no encontrado.</response>\1[HttpPut("{id:guid}")]', content)

# For DELETE
content = re.sub(r'(\s+)\[HttpDelete\("\{id:guid\}"\)\]', r'\1/// <summary>\1/// Elimina (lógicamente) un usuario.\1/// </summary>\1/// <param name="id">ID del usuario.</param>\1/// <returns>No content.</returns>\1/// <response code="204">Usuario eliminado.</response>\1/// <response code="401">No autorizado.</response>\1/// <response code="403">No tienes permisos.</response>\1/// <response code="404">Usuario no encontrado.</response>\1[HttpDelete("{id:guid}")]', content)

# For POST Roles
content = re.sub(r'(\s+)\[HttpPost\("\{id:guid\}/roles"\)\]', r'\1/// <summary>\1/// Asigna roles a un usuario.\1/// </summary>\1/// <param name="id">ID del usuario.</param>\1/// <param name="dto">Lista de IDs de roles.</param>\1/// <returns>El usuario actualizado.</returns>\1/// <response code="200">Roles asignados.</response>\1/// <response code="400">Datos inválidos.</response>\1/// <response code="401">No autorizado.</response>\1/// <response code="403">No tienes permisos.</response>\1/// <response code="404">Usuario no encontrado.</response>\1[HttpPost("{id:guid}/roles")]', content)

with open('C:/Users/kkk/Desktop/TallerIntegracion3/src/TenantIdentityService/Controllers/UsuarioController.cs', 'w', encoding='utf-8') as f:
    f.write(content)
