using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.Services;

public class ResourceUsageReporting : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ResourceUsageReporting> _logger;
    private readonly string _webhook;
    private WebhookMessage? _message;
    private Process? _usageProcess;

    public ResourceUsageReporting(IHttpClientFactory httpClientFactory, ILogger<ResourceUsageReporting> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        _webhook = Environment.GetEnvironmentVariable("WEBHOOK_URL") ?? string.Empty;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Return early if no webhook is present.
        if (string.IsNullOrEmpty(_webhook))
        {
            _logger.LogError("Webhook missing from environment variables, therefore resource reporting will be disabled");
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);

            (string cpuUsage, string memUsage) = await GetResourceUsage(cancellationToken);

            string usageDescription = string.Format("__CPU__: {0}\n__MEM__: {1}", cpuUsage, memUsage);

            try
            {
                await PostWebhook("Resource Usage", usageDescription, _webhook, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("Webhook failed to post with reason: {Reason}", ex.Message);
            }
        }
    }

    private async Task PostWebhook(string title, string description, string webhook, CancellationToken cancellationToken)
    {
        using (HttpClient client = _httpClientFactory.CreateClient())
        {
            _message ??= new WebhookMessage()
            {
                Content = null,
                Flags = 4096,
                Embeds = new[]
                {
                    new Embed()
                    {
                        Title = "Resource Usage"
                    }
                }
            };

            _message.Embeds[0].Title = title;
            _message.Embeds[0].Description = description;

            using (HttpResponseMessage response = await client.PostAsJsonAsync(webhook, _message, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
            }
        }
    }

    private async Task<(string, string)> GetResourceUsage(CancellationToken cancellationToken)
    {
        _usageProcess ??= new Process()
        {
            StartInfo = new ProcessStartInfo("bash", "resources.sh")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true
            }
        };

        _usageProcess.Start();

        string cpuUsage = await _usageProcess.StandardOutput.ReadLineAsync(cancellationToken) ?? "Failed to fetch cpu usage.";
        string memoryUsage = await _usageProcess.StandardOutput.ReadLineAsync(cancellationToken) ?? "Failed to fetch memory usage.";

        await _usageProcess.WaitForExitAsync(cancellationToken);

        return (cpuUsage, memoryUsage);
    }
}

public class WebhookMessage
{
    public string? Content { get; init; }
    public Embed[] Embeds { get; init; } = null!;

    public int Flags { get; set; }
}

public class Embed
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}