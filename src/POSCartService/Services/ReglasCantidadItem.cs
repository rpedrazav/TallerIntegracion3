namespace POSCartService.Services;

/// <summary>Contrato S2-061 compartido por crear, agregar y editar ítems.</summary>
public static class ReglasCantidadItem
{
    public static string? Validar(bool esPesoVariable, decimal? cantidad, decimal? pesoKg,
        bool permitirCantidadUnitariaOmitida, out decimal cantidadFacturable)
    {
        cantidadFacturable = 0m;
        if (esPesoVariable)
        {
            if (!pesoKg.HasValue)
                return "peso_kg es obligatorio para productos de peso variable.";
            if (pesoKg.Value < 0.010m)
                return "peso_kg debe ser mayor o igual a 0.010 kg (10 gramos).";
            if (pesoKg.Value % 0.001m != 0)
                return "peso_kg debe expresarse en incrementos de 0.001 kg (1 gramo), sin redondeo.";
            if (cantidad.HasValue && cantidad.Value != pesoKg.Value)
                return "cantidad debe coincidir con peso_kg para productos de peso variable.";
            cantidadFacturable = pesoKg.Value;
        }
        else
        {
            if (pesoKg.HasValue)
                return "peso_kg no corresponde a un producto que no es de peso variable.";
            cantidadFacturable = cantidad ?? (permitirCantidadUnitariaOmitida ? 1m : 0m);
            if (cantidadFacturable <= 0)
                return "cantidad debe ser mayor a 0.";
        }
        return null;
    }
}
