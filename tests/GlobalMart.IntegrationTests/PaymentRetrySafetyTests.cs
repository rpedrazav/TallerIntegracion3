extern alias POSCartServiceAlias;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

[Trait("Plan", "S2-039")]
public sealed class PaymentRetrySafetyTests
{
    [Fact]
    public async Task Timeout_SinEstadoConfirmado_ConsultaUnaVezYNoReautoriza()
    {
        var request = ValidRequest();
        var postCount = 0;
        var getCount = 0;
        var handler = new StubHandler(async (message, cancellationToken) =>
        {
            if (message.Method == HttpMethod.Post)
            {
                postCount++;
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("Inalcanzable");
            }

            getCount++;
            Assert.Equal(HttpMethod.Get, message.Method);
            Assert.Equal(
                $"https://simulator.test/simulator/payments/{request.RequestId}?tenant_id={request.TenantId}",
                message.RequestUri!.ToString());
            Assert.Equal(
                request.RequestId.ToString(),
                Assert.Single(message.Headers.GetValues("Idempotency-Key")));
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(
                    "{\"error\":\"PAYMENT_STATUS_NOT_FOUND\",\"status\":\"UNKNOWN\"}",
                    Encoding.UTF8,
                    "application/json")
            };
        });
        var client = Client(handler, TimeSpan.FromMilliseconds(25));

        var exception = await Assert.ThrowsAsync<Pos.Exceptions.PaymentAuthorizationUncertainException>(
            () => client.ProcessPaymentAsync(request));

        Assert.Equal(request.RequestId, exception.RequestId);
        Assert.Equal(request.TenantId, exception.TenantId);
        Assert.Equal(1, postCount);
        Assert.Equal(1, getCount);
    }

    [Fact]
    public async Task Timeout_ConEstadoAprobado_RecuperaResultadoSinSegundoPost()
    {
        var request = ValidRequest();
        var postCount = 0;
        var getCount = 0;
        var handler = new StubHandler(async (message, cancellationToken) =>
        {
            if (message.Method == HttpMethod.Post)
            {
                postCount++;
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("Inalcanzable");
            }

            getCount++;
            return JsonResponse(new
            {
                request_id = request.RequestId,
                transaction_id = "SIM-TXN-RECOVERED",
                status = "APPROVED",
                authorization_code = "SIM-APPROVED",
                gateway_reference = "SIM-REF-RECOVERED",
                processed_at = "2026-10-17T12:00:00Z"
            });
        });
        var client = Client(handler, TimeSpan.FromMilliseconds(25));

        var result = await client.ProcessPaymentAsync(request);

        Assert.Equal("APPROVED", result.Status);
        Assert.Equal("SIM-TXN-RECOVERED", result.TransactionId);
        Assert.Equal(1, postCount);
        Assert.Equal(1, getCount);
    }

    [Fact]
    public async Task Rechazo_EsResultadoDefinitivoYNoConsultaEstado()
    {
        var request = ValidRequest();
        var postCount = 0;
        var getCount = 0;
        var handler = new StubHandler((message, _) =>
        {
            if (message.Method == HttpMethod.Get)
                getCount++;
            else
                postCount++;

            return Task.FromResult(JsonResponse(new
            {
                request_id = request.RequestId,
                transaction_id = "SIM-TXN-DECLINED",
                status = "DECLINED",
                decline_code = "DO_NOT_HONOR",
                message = "Pago rechazado por escenario de prueba.",
                processed_at = "2026-10-17T12:00:00Z"
            }));
        });
        var client = Client(handler, TimeSpan.FromSeconds(1));

        var result = await client.ProcessPaymentAsync(request);

        Assert.Equal("DECLINED", result.Status);
        Assert.Equal(1, postCount);
        Assert.Equal(0, getCount);
    }

    private static Pos.Services.PaymentGatewayClient Client(
        HttpMessageHandler handler,
        TimeSpan timeout)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://simulator.test/"),
            Timeout = timeout
        };
        return new Pos.Services.PaymentGatewayClient(
            new StubHttpClientFactory(httpClient),
            NullLogger<Pos.Services.PaymentGatewayClient>.Instance);
    }

    private static Pos.DTOs.PaymentGatewayRequest ValidRequest() => new()
    {
        RequestId = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        SaleId = Guid.NewGuid(),
        Amount = 11900m,
        Currency = "CLP",
        ScenarioId = "sim-approved-v1"
    };

    private static HttpResponseMessage JsonResponse(object value) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            JsonSerializer.Serialize(value),
            Encoding.UTF8,
            "application/json")
    };

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal(Pos.Services.PaymentGatewayClient.HttpClientName, name);
            return client;
        }
    }

    private sealed class StubHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => response(request, cancellationToken);
    }
}
