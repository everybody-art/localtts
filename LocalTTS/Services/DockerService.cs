using System.Diagnostics;
using System.Net.Http;

namespace LocalTTS.Services;

public class DockerService
{
    private const string ContainerName = "localtts-kokoro";
    private const string Image = "ghcr.io/remsky/kokoro-fastapi-cpu:latest";

    public async Task EnsureRunningAsync()
    {
        Log.Info("Checking container status...");
        var (exitCode, output) = await RunDockerAsync($"inspect -f \"{{{{.State.Running}}}}\" {ContainerName}");
        Log.Info($"inspect exit={exitCode}, output={output.Trim()}");

        if (exitCode == 0 && output.Trim() == "true")
        {
            Log.Info("Container already running");
            return;
        }

        if (exitCode == 0)
        {
            Log.Info("Starting existing container...");
            var (startExit, startOut) = await RunDockerAsync($"start {ContainerName}");
            Log.Info($"start exit={startExit}, output={startOut.Trim()}");
        }
        else
        {
            Log.Info("Creating new container...");
            var (runExit, runOut) = await RunDockerAsync($"run -d --name {ContainerName} -p 8880:8880 {Image}");
            Log.Info($"run exit={runExit}, output={runOut.Trim()}");
        }

        Log.Info("Waiting for API to be ready...");
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        for (int i = 0; i < 60; i++)
        {
            try
            {
                var response = await client.GetAsync("http://localhost:8880/v1/models");
                Log.Info($"Health check {i + 1}: {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    Log.Info("API is ready!");
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Health check {i + 1}: {ex.GetType().Name} - {ex.Message}");
            }
            await Task.Delay(2000);
        }

        throw new Exception("Kokoro API did not become ready in time");
    }

    public async Task StopAsync()
    {
        Log.Info("Stopping container...");
        await RunDockerAsync($"stop {ContainerName}");
    }

    private static async Task<(int ExitCode, string Output)> RunDockerAsync(string arguments)
    {
        Log.Info($"Running: docker {arguments}");
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new Exception("Failed to start docker");
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (!string.IsNullOrWhiteSpace(stderr))
            Log.Info($"stderr: {stderr.Trim()}");

        return (process.ExitCode, stdout);
    }
}
