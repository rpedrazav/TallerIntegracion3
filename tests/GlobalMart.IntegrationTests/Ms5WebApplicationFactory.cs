extern alias POSCartServiceAlias;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

public class Ms5WebApplicationFactory : WebApplicationFactory<POSCartServiceAlias::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("OutboxPublisher:Enabled", "false");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<Pos.Services.ISucursalClient>();
            services.AddSingleton<Pos.Services.ISucursalClient, AlwaysActiveSucursalClient>();
        });
    }

    private sealed class AlwaysActiveSucursalClient : Pos.Services.ISucursalClient
    {
        public Task<bool> IsActiveAsync(Guid sucursalId, string bearerToken, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
