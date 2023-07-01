using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace API.Services;

public class StatusReporting : IHostedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StatusReporting> _logger;

    public StatusReporting(IHttpClientFactory httpClientFactory, ILogger<StatusReporting> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);

            using (HttpClient client = _httpClientFactory.CreateClient())
            {
                WebhookResponse data = new WebhookResponse()
                {
                    Username = "API Report",
                    Content = null,
                    Flags = 4096,
                    Embeds = new[]
                    {
                        new Embed()
                        {
                            Title = "Resource Usage",
                            Description = string.Format("{0}\n{1}", await CalculateCpuUsage(cancellationToken), await CalculateMemoryUsage(cancellationToken))
                        }
                    }
                };

                using (HttpResponseMessage response = await client.PostAsJsonAsync(Environment.GetEnvironmentVariable("WEBHOOK_URL"), data, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<string> CalculateCpuUsage(CancellationToken cancellationToken)
    {
        string output = string.Empty;

        try
        {
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo("bash", "./cpu.sh")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                process.Start();

                while (!process.StandardOutput.EndOfStream)
                {
                    output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                }

                _logger.LogInformation(output);
                
                await process.WaitForExitAsync(cancellationToken);
            }
        }
        catch (Exception)
        {
            output = "Failed to fetch cpu usage";
        }

        return output;
    }

    private async Task<string> CalculateMemoryUsage(CancellationToken cancellationToken)
    {
        string output = string.Empty;

        try
        {
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo("bash", "./mem.sh")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                process.Start();

                while (!process.StandardOutput.EndOfStream)
                {
                    output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                }
                    
                _logger.LogInformation(output);
                
                await process.WaitForExitAsync(cancellationToken);
            }
        }
        catch (Exception)
        {
            output = "Failed to fetch memory usage";
        }

        return output;
    }
}

file class WebhookResponse
{
    public string? Username { get; init; }
    public string? Content { get; init; }
    public Embed[]? Embeds { get; init; }
    public int Flags { get; set; }
}

file class Embed
{
    public string? Title { get; set; }
    public string? Description { get; set; }
}