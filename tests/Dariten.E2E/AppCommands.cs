using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;

namespace Dariten.E2E;

/// <summary>
/// Starts the Blazor dev server for the assembly, unless <c>E2E_BASE_URL</c> already points at one.
/// E2E 0.7.0 does not launch <c>app.command</c>, so the test project owns the process.
/// </summary>
[SetUpFixture]
public sealed class AppHost
{
    private static Process? _process;

    public static string BaseUrl { get; private set; } = "http://127.0.0.1:5173";

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        var configured = Environment.GetEnvironmentVariable("E2E_BASE_URL");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            BaseUrl = configured.TrimEnd('/');
            return;
        }

        var port = FreePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        var repo = FindRepoRoot();
        var logDir = Path.Combine(repo, "tests", "Dariten.E2E", ".e2e");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "blazor-devserver.log");

        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repo,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add("run");
        start.ArgumentList.Add("--project");
        start.ArgumentList.Add("src/Dariten/Dariten.csproj");
        start.ArgumentList.Add("--no-launch-profile");
        start.ArgumentList.Add("--");
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add(BaseUrl);
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        start.Environment["ASPNETCORE_URLS"] = BaseUrl;
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        start.Environment["DOTNET_NOLOGO"] = "1";

        _process = Process.Start(start) ?? throw new InvalidOperationException("Could not start dotnet run.");
        var log = new StreamWriter(logPath, append: true) { AutoFlush = true };
        _process.OutputDataReceived += (_, e) => { if (e.Data is not null) log.WriteLine(e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data is not null) log.WriteLine(e.Data); };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow.AddMinutes(3);
        while (DateTime.UtcNow < deadline)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException($"The Blazor dev server exited with code {_process.ExitCode}. See {logPath}.");
            }

            try
            {
                using var response = await http.GetAsync(BaseUrl);
                if ((int)response.StatusCode is >= 200 and <= 499)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"The Blazor dev server did not answer at {BaseUrl}. See {logPath}.");
    }

    [OneTimeTearDown]
    public void Stop()
    {
        if (_process is null || _process.HasExited)
        {
            return;
        }

        _process.Kill(entireProcessTree: true);
        _process.WaitForExit(10_000);
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dariten.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find the repository root (Dariten.slnx) above the test assembly.");
    }
}
