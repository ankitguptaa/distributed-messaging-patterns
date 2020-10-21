using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LoadProcessor
{
    public class SbQueueProcessorWorker : BackgroundService
    {
        private readonly ServiceBusClient _sBusClient;
        private readonly ILogger<SbQueueProcessorWorker> _logger;
        private const string queueName = "loadqueue";

        public SbQueueProcessorWorker(ServiceBusClient sBusClient, ILogger<SbQueueProcessorWorker> logger)
        {
            _sBusClient = sBusClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running {WorkerName}", nameof(SbQueueProcessorWorker));
            
            await ProcessMessages(stoppingToken);
            
            _logger.LogInformation("{WorkerName} processing stopped", nameof(SbQueueProcessorWorker));
        }

        private async Task ProcessMessages(CancellationToken stoppingToken)
        {
            ServiceBusReceiver receiver = _sBusClient.CreateReceiver(queueName);
            await foreach (var message in receiver.ReceiveMessagesAsync(stoppingToken))
            {
                _logger.LogInformation("Message received");
                _logger.LogInformation($"Message ID=> {message.MessageId}");
                _logger.LogInformation($"Message Body=> {message.Body.ToString()}");
                await receiver.CompleteMessageAsync(message, stoppingToken);
            }

        }
    }
}