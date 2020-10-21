using System;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HttpClientItemsWorker
{
    public class Worker : BackgroundService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<Worker> _logger;

        public Worker(IHttpClientFactory clientFactory, ILogger<Worker> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var apiClient = _clientFactory.CreateClient("ItemsClient");
            _logger.LogInformation("Items Http Client Worker running");
            
            const int requestCount = 10;
            while (!stoppingToken.IsCancellationRequested)
            {
                var tasks = Observable.Range(0, requestCount)
                    .Select(x =>
                        Observable.FromAsync(() => apiClient.GetStringAsync("/api/items"))
                            .Subscribe(
                                result => _logger.LogInformation($"Received {result}"),
                                ex => _logger.LogError(ex, "Request failed"),
                                () => _logger.LogInformation("COMPLETED"))
                    );

                await tasks;

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}