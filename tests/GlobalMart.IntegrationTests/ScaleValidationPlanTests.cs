using System.Text.RegularExpressions;
using Xunit;

namespace GlobalMart.IntegrationTests;

[Trait("Plan", "S2-081")]
public sealed class ScaleValidationPlanTests
{
    private static string FindPlan()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "docs", "qa", "S2-081", "plan-validacion.md");
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException("No se encontró el plan documental S2-081 desde la raíz de ejecución.");
    }

    [Fact]
    public void MatrixSeparatesSimulatedAndPhysicalEvidence()
    {
        var plan = File.ReadAllText(FindPlan());
        var rows = plan.Split('\n').Where(line => line.StartsWith("| SIMULADA") || line.StartsWith("| FÍSICA")).ToArray();

        Assert.Contains(rows, row => row.StartsWith("| SIMULADA"));
        Assert.Contains(rows, row => row.StartsWith("| FÍSICA"));
        Assert.All(rows, row => Assert.Equal(7, row.Split('|').Length));
        Assert.Contains("criterios planificados, no resultados aprobados", plan);
        Assert.Contains("no acredita por sí mismo el recorrido completo del POS", plan);
    }

    [Fact]
    public void PhysicalApprovalRequiresDeviceModelProtocolAndRealEvidence()
    {
        var plan = File.ReadAllText(FindPlan());
        Assert.Contains("Integración física: CONDICIONADA / [NO VERIFICADO]", plan);
        Assert.Contains("Dispositivo prestado disponible", plan);
        Assert.Contains("marca, modelo", plan);
        Assert.Contains("Protocolo confirmado", plan);
        Assert.Contains("Implementación real", plan);
        Assert.Contains("No deducirlos desde mocks", plan);
        Assert.Contains("sin equipo/modelo/protocolo no ejecutar ni aprobar el bloque físico", plan);
        Assert.Contains("No declarar “integración física completada” a partir de mocks", plan);
        Assert.Contains("Un caso omitido, bloqueado o no ejecutado nunca cuenta como aprobado", plan);
    }

    [Fact]
    public void EveryLocalMarkdownLinkPointsToExistingEvidenceOrProtocol()
    {
        var path = FindPlan();
        var links = Regex.Matches(File.ReadAllText(path), @"\[[^\]]+\]\(([^)]+)\)")
            .Select(match => match.Groups[1].Value)
            .Where(target => !Uri.TryCreate(target, UriKind.Absolute, out _) && !target.StartsWith('#'))
            .ToArray();

        Assert.NotEmpty(links);
        foreach (var link in links)
        {
            var target = Uri.UnescapeDataString(link.Split('#')[0]);
            var resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, target));
            Assert.True(File.Exists(resolved), $"Enlace local inexistente: {link}");
        }
    }
}
