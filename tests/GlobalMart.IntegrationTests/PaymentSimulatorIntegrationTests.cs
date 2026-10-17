extern alias POSCartServiceAlias;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

[Trait("Plan", "S2-057")]
[Trait("Plan", "S2-058")]
[Trait("Plan", "S2-038")]
public sealed class PaymentSimulatorIntegrationTests : IAsyncLifetime
{
    private Process? _simulatorProcess;
    private int _port;
    private string _baseUrl = string.Empty;
    private readonly HttpClient _rawHttpClient = new();

    public async Task InitializeAsync()
    {
        // 1. Obtener un puerto libre disponible en loopback
        using (var listener = new TcpListener(IPAddress.Loopback, 0))
        {
            listener.Start();
            _port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
        }

        _baseUrl = $"http://127.0.0.1:{_port}/";

        // 2. Localizar Docker/simulator/server.js relativo a la raíz del repositorio
        var repoRoot = FindRepoRoot();
        var serverScript = Path.Combine(repoRoot, "Docker", "simulator", "server.js");

        if (!File.Exists(serverScript))
        {
            throw new FileNotFoundException($"No se encontró el script del simulador en {serverScript}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "node",
            Arguments = $"\"{serverScript}\"",
            Environment =
            {
                ["PORT"] = _port.ToString(),
                ["HOST"] = "127.0.0.1"
            },
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _simulatorProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("No fue posible iniciar el proceso de Node para el simulador.");

        // 3. Esperar a que el simulador responda en /health
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var healthy = false;
        while (!cts.IsCancellationRequested)
        {
            try
            {
                var response = await _rawHttpClient.GetAsync($"{_baseUrl}health", cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    healthy = true;
                    break;
                }
            }
            catch
            {
                await Task.Delay(100, cts.Token);
            }
        }

        if (!healthy)
        {
            throw new TimeoutException($"El simulador en {_baseUrl} no estuvo listo dentro del tiempo esperado.");
        }
    }

    public Task DisposeAsync()
    {
        _rawHttpClient.Dispose();
        if (_simulatorProcess != null && !_simulatorProcess.HasExited)
        {
            try
            {
                _simulatorProcess.Kill(entireProcessTree: true);
                _simulatorProcess.Dispose();
            }
            catch
            {
                // Limpieza silenciosa
            }
        }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Simulator_HealthEndpoint_Returns200Healthy()
    {
        var response = await _rawHttpClient.GetAsync($"{_baseUrl}health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"Healthy\"", json);
        Assert.Contains("\"service\":\"payment-simulator\"", json);
    }

    [Fact]
    public async Task Simulator_ApprovedScenario_WorksWithPaymentGatewayClient()
    {
        var client = CreatePaymentClient(_baseUrl);
        var request = ValidRequest("sim-approved-v1");

        var result = await client.ProcessPaymentAsync(request);

        Assert.NotNull(result);
        Assert.Equal("APPROVED", result.Status);
        Assert.Equal(request.RequestId, result.RequestId);
        Assert.StartsWith("SIM-TXN-", result.TransactionId);
        Assert.Equal("SIM-APPROVED", result.AuthorizationCode);
        Assert.StartsWith("SIM-REF-", result.GatewayReference);
        Assert.DoesNotContain(
            request.RequestId.ToString("N"),
            result.GatewayReference!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Simulator_DeclinedScenario_ReturnsDeclinedWithoutError()
    {
        var client = CreatePaymentClient(_baseUrl);
        var request = ValidRequest("sim-declined-v1");

        var result = await client.ProcessPaymentAsync(request);

        Assert.NotNull(result);
        Assert.Equal("DECLINED", result.Status);
        Assert.Equal("DO_NOT_HONOR", result.DeclineCode);
        Assert.NotEmpty(result.Message!);
    }

    [Fact]
    public async Task Simulator_RejectsCardData_EnforcesRN02()
    {
        var payloadWithPan = new
        {
            request_id = Guid.NewGuid(),
            tenant_id = Guid.NewGuid(),
            sale_id = Guid.NewGuid(),
            amount = 10000,
            currency = "CLP",
            scenario_id = "sim-approved-v1",
            pan = "4111111111111111", // Prohibido por RN-02 / PCI DSS
            cvv = "123"
        };

        var response = await PostRawAsync(payloadWithPan, payloadWithPan.request_id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("CARD_DATA_FORBIDDEN", body);
        Assert.Contains("RN-02", body);
    }

    [Fact]
    public async Task Simulator_IdempotencyConflict_Returns409()
    {
        var requestId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var payload1 = new
        {
            request_id = requestId,
            tenant_id = tenantId,
            sale_id = Guid.NewGuid(),
            amount = 5000,
            currency = "CLP",
            scenario_id = "sim-approved-v1"
        };

        var payload2WithDifferentAmount = new
        {
            request_id = requestId,
            tenant_id = tenantId,
            sale_id = payload1.sale_id,
            amount = 9999, // Distinto monto -> conflicto de idempotencia
            currency = "CLP",
            scenario_id = "sim-approved-v1"
        };

        var res1 = await PostRawAsync(payload1, requestId);
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);

        var res2 = await PostRawAsync(payload2WithDifferentAmount, requestId);
        Assert.Equal(HttpStatusCode.Conflict, res2.StatusCode);
        var body2 = await res2.Content.ReadAsStringAsync();
        Assert.Contains("IDEMPOTENCY_CONFLICT", body2);
    }

    [Fact]
    public async Task Simulator_SameLogicalRequest_ReturnsExactlySameResult()
    {
        var client = CreatePaymentClient(_baseUrl);
        var request = ValidRequest("sim-approved-v1");

        var first = await client.ProcessPaymentAsync(request);
        var second = await client.ProcessPaymentAsync(request);

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Simulator_MismatchedIdempotencyKey_Returns409()
    {
        var payload = new
        {
            request_id = Guid.NewGuid(),
            tenant_id = Guid.NewGuid(),
            sale_id = Guid.NewGuid(),
            amount = 5000,
            currency = "CLP",
            scenario_id = "sim-approved-v1"
        };

        var response = await PostRawAsync(payload, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("IDEMPOTENCY_CONFLICT", body);
    }

    [Fact]
    [Trait("Plan", "S2-039")]
    public async Task Simulator_StatusQuery_ReturnsStoredResultForSameKey()
    {
        var client = CreatePaymentClient(_baseUrl);
        var request = ValidRequest("sim-approved-v1");
        var authorized = await client.ProcessPaymentAsync(request);

        var recovered = await client.GetPaymentStatusAsync(request.TenantId, request.RequestId);

        Assert.NotNull(recovered);
        Assert.Equal(authorized, recovered);
    }

    private async Task<HttpResponseMessage> PostRawAsync(object payload, Guid idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}simulator/payments")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey.ToString());
        return await _rawHttpClient.SendAsync(request);
    }

    private static Pos.Services.PaymentGatewayClient CreatePaymentClient(string baseUrl)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(5)
        };

        return new Pos.Services.PaymentGatewayClient(
            new StubHttpClientFactory(httpClient),
            NullLogger<Pos.Services.PaymentGatewayClient>.Instance);
    }

    private static Pos.DTOs.PaymentGatewayRequest ValidRequest(string scenarioId) => new()
    {
        RequestId = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        SaleId = Guid.NewGuid(),
        Amount = 15990m,
        Currency = "CLP",
        ScenarioId = scenarioId
    };

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "GlobalMartOS.sln")))
            {
                return dir;
            }
            dir = Directory.GetParent(dir)?.FullName;
        }
        return Directory.GetCurrentDirectory();
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal(Pos.Services.PaymentGatewayClient.HttpClientName, name);
            return client;
        }
    }
}
