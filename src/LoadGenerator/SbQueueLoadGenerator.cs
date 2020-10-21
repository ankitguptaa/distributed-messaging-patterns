using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LoadGenerator
{
    public class SbQueueLoadGenerator : BackgroundService
    {
        private readonly ServiceBusClient _sBusClient;
        private readonly ILogger<SbQueueLoadGenerator> _logger;
        private const string QUEUE_NAME = "loadqueue";

        public SbQueueLoadGenerator(ServiceBusClient sBusClient, ILogger<SbQueueLoadGenerator> logger)
        {
            _sBusClient = sBusClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running {WorkerName}", nameof(SbQueueLoadGenerator));

            await GenerateLoad(stoppingToken);

            _logger.LogInformation("{WorkerName} processing stopped", nameof(SbQueueLoadGenerator));
        }

        private async Task GenerateLoad(CancellationToken stoppingToken)
        {
            var messageCount = 5;
            var random  = new Random();
            
            var sender = _sBusClient.CreateSender(QUEUE_NAME);

            while (!stoppingToken.IsCancellationRequested)
            {
                for (var i = 0; i < messageCount; i++)
                {
                    var payload = JsonSerializer.Serialize(new
                    {
                        content = $"Message {i} sent from {nameof(SbQueueLoadGenerator)}",
                        timestamp = DateTime.UtcNow
                    });
                    
                    var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(payload))
                    {
                        MessageId = Guid.NewGuid().ToString("N")
                    };
                    await sender.SendMessageAsync(message, stoppingToken);
                }

                _logger.LogInformation("{MessageCount} messages sent to {QueueName}", messageCount, QUEUE_NAME);
                messageCount += random.Next(5, 20);
                await Task.Delay(TimeSpan.FromSeconds(4), stoppingToken);
            }
        }
    }
}