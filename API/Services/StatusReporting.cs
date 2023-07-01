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
    private readonly IConfiguration _configuration;

    public StatusReporting(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
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

    private static async Task<string> CalculateCpuUsage(CancellationToken cancellationToken)
    {
        string output;

        try
        {
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo("bash", @"bash -c 'printf ""CPU Usage: %d%%"" $((100-$(vmstat 1 2|tail -1|awk ""{print \$15}"")))'")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                process.Start();

                output = await process.StandardOutput.ReadToEndAsync(cancellationToken);

                await process.WaitForExitAsync(cancellationToken);
            }
        }
        catch (Exception)
        {
            output = "Failed to fetch cpu usage";
        }

        return output;
    }

    private static async Task<string> CalculateMemoryUsage(CancellationToken cancellationToken)
    {
        string output;

        try
        {
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo("bash", @"-c free -m | awk 'NR==2{ printf ""Memory Usage: %.2f%%\n"", $3*100/$2 }'")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                process.Start();

                output = await process.StandardOutput.ReadToEndAsync(cancellationToken);

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