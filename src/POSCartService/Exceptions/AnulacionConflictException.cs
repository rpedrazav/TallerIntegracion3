namespace POSCartService.Exceptions;

public sealed class AnulacionConflictException(string codigo, string message)
    : InvalidOperationException(message)
{
    public string Codigo { get; } = codigo;
}
