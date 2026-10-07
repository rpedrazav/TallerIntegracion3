import sys

path = 'tests/GlobalMart.IntegrationTests/UserIntegrationTests.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

new_tests = '''
    [Fact]
    public async Task PostUserRoles_RolesValidos_ActualizaAtomicamenteYRetorna200OK()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var rolCajeroId = Guid.Parse("11111111-0000-0000-0000-000000000001");
        var rolReponedorId = Guid.Parse("11111111-0000-0000-0000-000000000002");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            db.Database.Migrate();

            db.Tenants.Add(new Tenant { Id = tenantId, Nombre = $"Tenant Roles {tenantId:N}", Pais = "CL", Moneda = "CLP" });
            
            var usuario = new Usuario
            {
                Id = userId,
                TenantId = tenantId,
                Nombre = "User Roles",
                Email = $"user_roles_{Guid.NewGuid():N}@test.cl",
                PasswordHash = "hash",
                Activo = true
            };
            db.Usuarios.Add(usuario);

            // Tiene Cajero inicialmente
            db.UsuarioRoles.Add(new UsuarioRol { UsuarioId = userId, RolId = rolCajeroId });
            await db.SaveChangesAsync();
        }

        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var token = CreateAdminToken(configuration, tenantId, adminId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act: Asignar Reponedor (debe quitar Cajero y agregar Reponedor)
        var payload = new { Roles = new[] { "REPONEDOR" } };
        var response = await _client.PostAsJsonAsync($"/api/v1/users/{userId}/roles", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            var rolesEnDb = await db.UsuarioRoles.Where(ur => ur.UsuarioId == userId).ToListAsync();
            
            Assert.Single(rolesEnDb);
            Assert.Equal(rolReponedorId, rolesEnDb.First().RolId);
        }
    }

    [Fact]
    public async Task PostUserRoles_RolFalso_Retorna400BadRequest()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            db.Database.Migrate();

            db.Tenants.Add(new Tenant { Id = tenantId, Nombre = $"Tenant BadRole {tenantId:N}", Pais = "CL", Moneda = "CLP" });
            db.Usuarios.Add(new Usuario
            {
                Id = userId, TenantId = tenantId, Nombre = "User BadRole", Email = $"bad_{Guid.NewGuid():N}@test.cl", PasswordHash = "hash", Activo = true
            });
            await db.SaveChangesAsync();
        }

        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var token = CreateAdminToken(configuration, tenantId, adminId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new { Roles = new[] { "BATMAN" } };
        var response = await _client.PostAsJsonAsync($"/api/v1/users/{userId}/roles", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostUserRoles_UsuarioOtroTenant_Retorna404NotFound()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var adminA = Guid.NewGuid();
        var userIdEnB = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            db.Database.Migrate();

            db.Tenants.Add(new Tenant { Id = tenantA, Nombre = $"Tenant A {tenantA:N}", Pais = "CL", Moneda = "CLP" });
            db.Tenants.Add(new Tenant { Id = tenantB, Nombre = $"Tenant B {tenantB:N}", Pais = "CL", Moneda = "CLP" });
            
            db.Usuarios.Add(new Usuario
            {
                Id = userIdEnB, TenantId = tenantB, Nombre = "User Tenant B", Email = $"user_b_{Guid.NewGuid():N}@test.cl", PasswordHash = "hash", Activo = true
            });
            await db.SaveChangesAsync();
        }

        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var tokenA = CreateAdminToken(configuration, tenantA, adminA);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        var payload = new { Roles = new[] { "CAJERO" } };
        var response = await _client.PostAsJsonAsync($"/api/v1/users/{userIdEnB}/roles", payload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string CreateAdminToken'''

content = content.replace('    private static string CreateAdminToken', new_tests)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Success')
