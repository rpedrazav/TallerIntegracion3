using System.Diagnostics;
using Xunit;

namespace GlobalMart.IntegrationTests;

/// <summary>
/// Executes the real frontend scale adapters, cart boundary and extracted POS callback.
/// MS5 is represented by spies: these tests exercise no HTTP, database or hardware.
/// </summary>
public sealed class ScaleParserRoundingIntegrationTests
{
    [Theory]
    [InlineData("sim-kg")]
    [InlineData("sim-g")]
    [InlineData("sim-min-kg")]
    [InlineData("sim-min-g")]
    [InlineData("sim-floating-noise")]
    [InlineData("sim-nan")]
    [InlineData("sim-infinity")]
    [InlineData("sim-negative-infinity")]
    [InlineData("sim-negative")]
    [InlineData("sim-zero")]
    [InlineData("sim-below-min")]
    [InlineData("sim-subgram-kg")]
    [InlineData("sim-subgram-g")]
    [InlineData("sim-text")]
    [InlineData("sim-comma")]
    [InlineData("sim-unit")]
    public Task SimulatorValidatesNumericFormatAndCanonicalizesKilograms(string scenario) => RunCase(scenario);

    [Theory]
    [InlineData("state-disconnected")]
    [InlineData("state-timeout")]
    [InlineData("state-unstable")]
    [InlineData("state-waiting")]
    [InlineData("state-disconnect-during-read")]
    public Task StabilityAndConnectionFailuresDoNotBecomeWeights(string scenario) => RunCase(scenario);

    [Theory]
    [InlineData("mock-round-down")]
    [InlineData("mock-round-up")]
    [InlineData("mock-tare")]
    [InlineData("mock-tare-zero")]
    [InlineData("mock-nan")]
    [InlineData("mock-infinity")]
    [InlineData("mock-negative")]
    public Task MockCalculatesNetWeightAndRejectsNonFiniteInput(string scenario) => RunCase(scenario);

    [Theory]
    [InlineData("pos-valid-add")]
    [InlineData("pos-valid-update")]
    [InlineData("pos-nan")]
    [InlineData("pos-infinity")]
    [InlineData("pos-negative-infinity")]
    [InlineData("pos-negative")]
    [InlineData("pos-zero")]
    [InlineData("pos-below-min")]
    [InlineData("pos-subgram")]
    [InlineData("pos-text")]
    [InlineData("pos-comma")]
    [InlineData("pos-unit")]
    [InlineData("pos-unstable")]
    [InlineData("pos-timeout")]
    [InlineData("pos-disconnected")]
    public Task ProductivePosCallbackRejectsInvalidWeightBeforeCallingMs5(string scenario) => RunCase(scenario);

    private static async Task RunCase(string scenario)
    {
        var root = FindRepositoryRoot();
        var helper = Path.Combine(root, "tests", "GlobalMart.IntegrationTests", "s2082-scale-tests.cjs");
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
