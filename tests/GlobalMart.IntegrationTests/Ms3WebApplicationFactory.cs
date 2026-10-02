extern alias CatalogPricingServiceAlias;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GlobalMart.IntegrationTests;

public class Ms3WebApplicationFactory : WebApplicationFactory<CatalogPricingServiceAlias::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}
