using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Replicator
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunReplicate();
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task RunReplicate()
        {
            string SourceServers = _configuration.GetValue<string>("Kafka:Source");
            string TargetServers = _configuration.GetValue<string>("Kafka:Target");
            string Topics = _configuration.GetValue<string>("Kafka:TopicsForReplication");
            string TargetPrefix = _configuration.GetValue<string>("Kafka:TargetPrefix");
            string groupId = _configuration.GetValue<string>("Kafka:GroupId");
            //bool enableAutoCommit = _configuration.GetValue<bool>("Kafka:EnableAutoCommit");

            var configSource = new ConsumerConfig
            {
                GroupId = groupId,
                BootstrapServers = SourceServers,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            //Para Acaciano - Verificar a necessidade de manter esse config, seria utilizado para o producer target
            var configTarget = new ConsumerConfig
            {
                GroupId = groupId,
                BootstrapServers = TargetServers,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            _logger.LogInformation("========================================================================================");

            try
            {
                using var consumer = new ConsumerBuilder<Ignore, string>(configSource).Build();
                {

                    foreach (string topic in Topics.Split(','))
                    {
                        _logger.LogInformation($"Lendo topic de origem: {topic}");

                        consumer.Subscribe(topic);

                        var cr = consumer.Consume();

                        _logger.LogInformation($"Mensagem de origem: {cr.Message.Value}");

                        //Para Acaciano, produzir as mensagens para o target e commitar a mensagem do source somente se conseguir gravar no target
                       
                         consumer.Commit(cr);
                       
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
            }

        }  
    }
}
