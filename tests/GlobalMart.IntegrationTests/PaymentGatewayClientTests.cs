extern alias POSCartServiceAlias;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Pos = POSCartServiceAlias::POSCartService;

namespace GlobalMart.IntegrationTests;

[Trait("Plan", "S2-031")]
[Trait("Plan", "S2-037")]
public sealed class PaymentGatewayClientTests
{
    [Fact]
    public void ContratoPasarela_NoContieneCamposSensiblesDeTarjeta_SoloReferenciasOpacas()
    {
        // S2-031: Verificar por contrato/reflexión que los DTOs de pasarela
        // no transportan ni exponen campos de tarjeta (PAN, CVV, CardNumber, etc.)
        var tiposContrato = new[] { typeof(Pos.DTOs.PaymentGatewayRequest), typeof(Pos.DTOs.PaymentGatewayResponse) };
        var palabrasProhibidas = new[] { "card", "pan", "cvv", "cvc", "pin", "track", "expiration", "vencimiento" };

        foreach (var tipo in tiposContrato)
        {
            var propiedades = tipo.GetProperties().Select(p => p.Name.ToLowerInvariant());
            foreach (var prop in propiedades)
            {
                foreach (var prohibida in palabrasProhibidas)
                {
                    Assert.DoesNotContain(prohibida, prop);
                }
            }
        }

        // Verificar que PaymentGatewayResponse expone únicamente referencias opacas
        var propsResponse = typeof(Pos.DTOs.PaymentGatewayResponse).GetProperties().Select(p => p.Name).ToArray();
        Assert.Contains(nameof(Pos.DTOs.PaymentGatewayResponse.TransactionId), propsResponse);
        Assert.Contains(nameof(Pos.DTOs.PaymentGatewayResponse.AuthorizationCode), propsResponse);
        Assert.Contains(nameof(Pos.DTOs.PaymentGatewayResponse.GatewayReference), propsResponse);
        Assert.Contains(nameof(Pos.DTOs.PaymentGatewayResponse.Status), propsResponse);
        Assert.Contains(nameof(Pos.DTOs.PaymentGatewayResponse.DeclineCode), propsResponse);
    }

    [Fact]
    public async Task ProcessPaymentAsync_PropagaIdempotencyKeyYEnviaSoloCamposSeguros()
    {
        var request = ValidRequest("sim-approved-v1");
        HttpRequestMessage? captured = null;
        string? body = null;
        var handler = new StubHandler(async (message, _) =>
        {
            captured = message;
            body = await message.Content!.ReadAsStringAsync();
            return JsonResponse(new
            {
                request_id = request.RequestId,
                transaction_id = "SIM-APP-1",
                status = "APPROVED",
                authorization_code = "SIM-APPROVED",
                gateway_reference = "SIM-RRN-1",
                processed_at = "2026-10-16T12:00:00Z"
            });
        });
        var client = Client(handler, new Uri("https://simulator.test/base/"), TimeSpan.FromSeconds(2));

        var result = await client.ProcessPaymentAsync(request);

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("https://simulator.test/base/simulator/payments", captured.RequestUri!.ToString());
        Assert.Equal(
            request.RequestId.ToString(),
            Assert.Single(captured.Headers.GetValues("Idempotency-Key")));
        Assert.Equal("APPROVED", result.Status);
        Assert.Equal(request.RequestId, result.RequestId);

        using var json = JsonDocument.Parse(body!);
        var names = json.RootElement.EnumerateObject().Select(property => property.Name).ToArray();
        Assert.Equal(
            new[] { "request_id", "tenant_id", "sale_id", "amount", "currency", "scenario_id" },
            names);
        Assert.DoesNotContain(names, name =>
            name.Contains("card", StringComparison.OrdinalIgnoreCase)
            || name.Contains("pan", StringComparison.OrdinalIgnoreCase)
            || name.Contains("cvv", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ProcessPaymentAsync_RechazoEsResultadoDeNegocioYConservaRequestId()
    {
        var request = ValidRequest("sim-declined-v1");
        var handler = new StubHandler((_, _) => Task.FromResult(JsonResponse(new
        {
            request_id = request.RequestId,
            transaction_id = "SIM-DEC-1",
            status = "DECLINED",
            decline_code = "DO_NOT_HONOR",
            message = "Pago rechazado por escenario de prueba.",
            processed_at = "2026-10-16T12:00:00Z"
        })));
        var client = Client(handler, new Uri("https://simulator.test/"), TimeSpan.FromSeconds(2));

        var result = await client.ProcessPaymentAsync(request);

        Assert.Equal(request.RequestId, result.RequestId);
        Assert.Equal("DECLINED", result.Status);
        Assert.Equal("DO_NOT_HONOR", result.DeclineCode);
        Assert.Null(result.AuthorizationCode);
        Assert.Null(result.GatewayReference);
    }

    [Fact]
    public async Task ProcessPaymentAsync_SiVenceTimeout_LanzaEstadoIncierto()
    {
        var handler = new StubHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Inalcanzable");
        });
        var client = Client(handler, new Uri("https://simulator.test/"), TimeSpan.FromMilliseconds(25));

        var exception = await Assert.ThrowsAsync<Pos.Exceptions.PaymentAuthorizationUncertainException>(
            () => client.ProcessPaymentAsync(ValidRequest("sim-timeout-v1")));

        Assert.Contains("no respondió", exception.Message);
    }

    [Fact]
    public async Task ProcessPaymentAsync_RequestIdVacio_NoInvocaLaPasarela()
    {
        var invoked = false;
        var handler = new StubHandler((_, _) =>
        {
            invoked = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        var client = Client(handler, new Uri("https://simulator.test/"), TimeSpan.FromSeconds(2));
        var invalid = ValidRequest("sim-approved-v1") with { RequestId = Guid.Empty };

        await Assert.ThrowsAsync<ArgumentException>(() => client.ProcessPaymentAsync(invalid));

        Assert.False(invoked);
    }

    private static Pos.Services.PaymentGatewayClient Client(
        HttpMessageHandler handler,
        Uri baseAddress,
        TimeSpan timeout)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = baseAddress,
            Timeout = timeout
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
        Amount = 11900m,
        Currency = "CLP",
        ScenarioId = scenarioId
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
