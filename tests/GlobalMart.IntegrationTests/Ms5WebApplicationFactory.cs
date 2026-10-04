extern alias POSCartServiceAlias;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GlobalMart.IntegrationTests;

public class Ms5WebApplicationFactory : WebApplicationFactory<POSCartServiceAlias::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}
