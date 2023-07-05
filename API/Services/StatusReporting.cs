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
            await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);

            string? webhook = Environment.GetEnvironmentVariable("WEBHOOK_URL");
            
            if(string.IsNullOrEmpty(webhook))
                continue;
            
            string usageDescription = string.Format("__CPU__: {0}\n__MEM__: {1}",
                                                    await RunProcess("./cpu.sh", cancellationToken),
                                                    await RunProcess("./mem.sh", cancellationToken));

            try
            {
                await PostWebhook("Resource Usage", usageDescription, webhook, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("Webhook failed to post with reason: {Reason}", ex.Message);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task PostWebhook(string title, string description, string webhook, CancellationToken cancellationToken)
    {
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
                        Title = title,
                        Description = description
                    }
                }
            };

            using (HttpResponseMessage response = await client.PostAsJsonAsync(webhook, data, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
            }
        }
    }

    private static async Task<string> RunProcess(string processName, CancellationToken cancellationToken)
    {
        string output;

        try
        {
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo("bash", processName)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                process.Start();

                output = await process.StandardOutput.ReadToEndAsync(cancellationToken);

                if (string.IsNullOrEmpty(output))
                {
                    throw new Exception("Failed to fetch usage");
                }

                await process.WaitForExitAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            output = ex.Message;
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