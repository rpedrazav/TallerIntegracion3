extern alias TenantIdentityServiceAlias;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GlobalMart.IntegrationTests;

public class Ms1WebApplicationFactory : WebApplicationFactory<TenantIdentityServiceAlias::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}
