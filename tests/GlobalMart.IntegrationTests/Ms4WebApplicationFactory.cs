extern alias WarehouseInventoryServiceAlias;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WarehouseDbContext = WarehouseInventoryServiceAlias::WarehouseInventoryService.Data.WarehouseDbContext;
using KafkaConsumerService = WarehouseInventoryServiceAlias::WarehouseInventoryService.Messaging.KafkaConsumerService;

namespace GlobalMart.IntegrationTests;

public class Ms4WebApplicationFactory : WebApplicationFactory<WarehouseInventoryServiceAlias::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:GroupId"] = $"integration-test-{Guid.NewGuid():N}"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.AddHostedService<MigrationFirstKafkaConsumer>();
        });
    }

    private sealed class MigrationFirstKafkaConsumer : IHostedService, IDisposable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly IConfiguration _configuration;
        private KafkaConsumerService? _consumer;

        public MigrationFirstKafkaConsumer(
            IServiceScopeFactory scopeFactory,
            ILogger<KafkaConsumerService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
            await db.Database.EnsureDeletedAsync(cancellationToken);
            await db.Database.MigrateAsync(cancellationToken);

            _consumer = new KafkaConsumerService(_logger, _configuration, _scopeFactory);
            await _consumer.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_consumer is not null)
            {
                try
                {
                    await _consumer.StopAsync(cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    // The Kafka handle may already be closed by the host during teardown.
                }
            }
        }

        public void Dispose()
        {
            _consumer?.Dispose();
        }
    }
}
