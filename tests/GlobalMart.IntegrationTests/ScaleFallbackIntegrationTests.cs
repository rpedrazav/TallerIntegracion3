using System.Diagnostics;
using Xunit;

namespace GlobalMart.IntegrationTests;

/// <summary>
/// Executes the real frontend scale adapters, cart boundary and extracted POS callback.
/// MS5 is represented by spies: these tests exercise no HTTP, database or hardware.
/// </summary>
public sealed class ScaleFallbackIntegrationTests
{
    [Theory]
    [InlineData("sale-existing")]
    [InlineData("sale-new")]
    [InlineData("draft-existing")]
    [InlineData("draft-new")]
    public Task DisconnectPreservesCartAndExplicitRetryAddsExactlyOnce(string scenario) => RunCase(scenario);

    // Capability guard only: expected to fail until productive authorized manual
    // fallback exists. This is deliberately not an E2E acceptance test.
    [Fact]
    public Task AcceptanceIncomplete_ManualFallbackCapabilityGuard_NotEndToEnd() => RunCase("manual-capability");
    private static async Task RunCase(string scenario)
    {
        var root = FindRepositoryRoot();
        var helper = Path.Combine(root, "tests", "GlobalMart.IntegrationTests", "s2083-scale-tests.cjs");
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("S2082_NODE_BINARY") ?? "node",
                WorkingDirectory = root,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                ArgumentList = { helper, root, scenario }
            }
        };
        Assert.True(process.Start(), "Could not start Node for the frontend integration scenario.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(deadline.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            Assert.Fail($"Scale scenario {scenario} exceeded 30 seconds.\n{await stdoutTask}\n{await stderrTask}");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        Assert.True(process.ExitCode == 0, $"Scale scenario {scenario} failed (exit {process.ExitCode}).\n{stdout}\n{stderr}");
        Assert.Contains($"PASS {scenario}", stdout);
    }

    private static string FindRepositoryRoot()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            for (var current = new DirectoryInfo(start); current is not null; current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, "globalmart-frontend", "src", "hardware", "scale-cart.ts")))
                    return current.FullName;
            }
        }
        throw new DirectoryNotFoundException("Cannot locate the repository containing globalmart-frontend/src/hardware/scale-cart.ts.");
    }
}

