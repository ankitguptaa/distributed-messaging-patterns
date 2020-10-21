using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ShowTopicSubscriber
{
    public enum ShowPlatforms
    {
        Hulu,
        Netflix,
        DisneyPlus
    }

    public class TopicSubscriber : BackgroundService
    {
        private readonly ServiceBusClient _sBusClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TopicSubscriber> _logger;
        private const string topicName = "shows";

        public TopicSubscriber(ServiceBusClient sBusClient, IConfiguration configuration, ILogger<TopicSubscriber> logger)
        {
            _sBusClient = sBusClient;
            _configuration = configuration;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var platform = _configuration.GetValue<string>("TOPIC"); // ShowPlatforms.Hulu.ToString().ToLower();
            _logger.LogInformation("{WorkerName} listening for topics about {ShowPlatform}", nameof(TopicSubscriber),platform);
            await ProcessMessages(platform, stoppingToken);
        }

        private async Task ProcessMessages(string platform, CancellationToken stoppingToken)
        {
            await using ServiceBusReceiver receiver = _sBusClient.CreateReceiver(topicName, platform);
            
            await foreach (var message in receiver.ReceiveMessagesAsync(stoppingToken))
            {
                _logger.LogInformation("New show message received for {Platform}", platform);
                _logger.LogInformation($"Show Message ID=> {message.MessageId}");
                _logger.LogInformation($"Show Message Body=> {message.Body.ToString()}");
                await receiver.CompleteMessageAsync(message, stoppingToken);
            }
        }
    }
}