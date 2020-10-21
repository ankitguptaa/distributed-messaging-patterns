using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ShowTopicPublisher
{
    public enum ShowPlatforms
    {
        Hulu,
        Netflix,
        DisneyPlus
    }
    public class TopicPublisher : BackgroundService
    {
        private readonly ILogger<TopicPublisher> _logger;
        private readonly ServiceBusClient _sBusClient;
        private const string topicName = "shows";

        public TopicPublisher(ServiceBusClient sBusClient, ILogger<TopicPublisher> logger)
        {
            _sBusClient = sBusClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{WorkerName} publish messages to topic {TopicName}", nameof(TopicPublisher), topicName);

            await GenerateShowTopicMessages(stoppingToken);

            _logger.LogInformation("{WorkerName} processing stopped", nameof(TopicPublisher));
        }

        private async Task GenerateShowTopicMessages(CancellationToken stoppingToken)
        {
            var random = new Random();
            var messageCount = 10;
            
            string GetRandomShowPlatform()
            {
                var platformValues = Enum.GetValues(typeof(ShowPlatforms));
                var chosenPlatform = platformValues.GetValue(random.Next(platformValues.Length));
                return chosenPlatform.ToString().ToLower();
            }
            
            ServiceBusSender sender = _sBusClient.CreateSender(topicName);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                var platform = GetRandomShowPlatform();
                
                for (int i = 0; i < messageCount; i++)
                {
                    var payload = JsonSerializer.Serialize(new
                    {
                        content = $"Now show for {platform} #{i} sent from {nameof(TopicPublisher)}",
                    });
                    
                    ServiceBusMessage message = new ServiceBusMessage(Encoding.UTF8.GetBytes(payload))
                    {
                        MessageId = Guid.NewGuid().ToString("N")
                    };
                    message.ApplicationProperties.Add("platform", platform);
                    message.ApplicationProperties.Add("client", nameof(TopicPublisher));
                    await sender.SendMessageAsync(message, stoppingToken);
                }

                _logger.LogInformation("{MessageCount} shows sent to {TopicName} topic for {Platform}", messageCount, topicName, platform);
                messageCount += random.Next(5, 20);
                await Task.Delay(TimeSpan.FromSeconds(4), stoppingToken);
            }
            
        }
    }
}