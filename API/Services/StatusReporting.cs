using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

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
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            using (HttpClient client = _httpClientFactory.CreateClient())
            {
                HttpResponseMessage response = await client.PostAsJsonAsync(_configuration["ReportingWebhook"], new Dictionary<string, string>()
                {
                    {
                        "username", "API Report"
                    },
                    {
                        "content", "Test"
                    }
                }, cancellationToken: cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
