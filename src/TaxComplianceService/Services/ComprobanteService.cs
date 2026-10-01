using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaxComplianceService.Data;
using TaxComplianceService.Exceptions;
using TaxComplianceService.Models;

namespace TaxComplianceService.Services;

/// <summary>
/// Implementación de <see cref="IComprobanteService"/>.
/// Obtiene el correlativo mediante la función PostgreSQL <c>obtener_correlativo_comprobante(uuid)</c>,
/// que crea la secuencia del tenant si no existe y ejecuta <c>nextval</c> de forma atómica,
/// garantizando unicidad incluso con requests concurrentes.
/// </summary>
public class ComprobanteService : IComprobanteService
{
    private readonly TaxDbContext _context;
    private readonly ITenantConfigClient _tenantConfigClient;
    private readonly ITaxCalculatorService _taxCalculatorService;
    private readonly ILogger<ComprobanteService> _logger;

    public ComprobanteService(
        TaxDbContext context,
        ITenantConfigClient tenantConfigClient,
        ITaxCalculatorService taxCalculatorService,
        ILogger<ComprobanteService> logger)
    {
        _context            = context            ?? throw new ArgumentNullException(nameof(context));
        _tenantConfigClient = tenantConfigClient ?? throw new ArgumentNullException(nameof(tenantConfigClient));
        _taxCalculatorService= taxCalculatorService?? throw new ArgumentNullException(nameof(taxCalculatorService));
        _logger             = logger             ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Comprobante> EmitirAsync(
        CrearComprobanteRequest request,
        Guid tenantId,
        Guid cajeroId,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (request.VentaId == Guid.Empty)
            throw new ArgumentException("El venta_id es obligatorio.", nameof(request));

        if (request.Items is null || request.Items.Count == 0)
            throw new ArgumentException("La lista de items no puede estar vacía.", nameof(request));

        // 1. Resolver el porcentaje de IVA del tenant en MS-1 (RN: los importes no se confían del cliente)
        var tenantConfig = await _tenantConfigClient.GetTenantConfigAsync(tenantId, cancellationToken);
        if (tenantConfig is null)
        {
            _logger.LogWarning(
                "[ComprobanteService] No se encontró configuración fiscal para el tenant {TenantId}",
                tenantId);
            throw new TenantConfigNotFoundException(tenantId);
        }

        // 2. Calcular subtotal, IVA y total en el servidor
        var breakdown = _taxCalculatorService.Calculate(request.Items, tenantConfig.PorcentajeIva);

        // 3. Obtener el correlativo atómico del tenant desde la secuencia de PostgreSQL
        var correlativo = await ObtenerCorrelativoAsync(tenantId, cancellationToken);

        // 4. Persistir el comprobante
        var comprobante = new Comprobante
        {
            TenantId         = tenantId,
            NumeroCorrelativo= correlativo,
            VentaId          = request.VentaId,
            CajeroId         = cajeroId,
            Items            = JsonSerializer.Serialize(breakdown.Items),
            Subtotal         = breakdown.SubtotalRedondeado,
            Iva              = breakdown.IvaRedondeado,
            Total            = breakdown.TotalRedondeado,
            FechaEmision     = DateTime.UtcNow
        };

        _context.Comprobantes.Add(comprobante);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[ComprobanteService] Comprobante {NumeroCorrelativo} emitido para tenant {TenantId}, venta {VentaId}. Total: {Total}",
            comprobante.NumeroCorrelativo, tenantId, comprobante.VentaId, comprobante.Total);

        return comprobante;
    }

    /// <inheritdoc/>
    public async Task<Comprobante?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // El HasQueryFilter global de TaxDbContext ya filtra por el tenant del request,
        // inyectado por TenantMiddleware. No hace falta filtrar manualmente (RN-01).
        return await _context.Comprobantes
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <summary>
    /// Invoca <c>obtener_correlativo_comprobante(uuid)</c> para obtener el siguiente correlativo del tenant.
    /// </summary>
    private async Task<long> ObtenerCorrelativoAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var resultado = await _context.Database
            .SqlQuery<long>($"SELECT obtener_correlativo_comprobante({tenantId})")
            .ToListAsync(cancellationToken);

        if (resultado.Count == 0)
            throw new InvalidOperationException("No se pudo obtener el correlativo del comprobante.");

        return resultado[0];
    }
}
