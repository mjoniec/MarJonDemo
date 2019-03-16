﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mqtt.Client;

namespace Gold.ExternalApiClient.Service
{
    public class GoldExternalApiClientService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IOptions<GoldExternalApiClientConfig> _config;
        private readonly MqttDualTopicClient _mqttDualTopicClient;

        public GoldExternalApiClientService(ILogger<GoldExternalApiClientConfig> logger, IOptions<GoldExternalApiClientConfig> config)
        {
            _logger = logger;
            _config = config;

            //field initializer can not reference non static - replace with interface and DI
            _mqttDualTopicClient = new MqttDualTopicClient(new MqttDualTopicData(
                "localhost", 1883, "RequestMqttTopic", "ResponseMqttTopic"));

            _mqttDualTopicClient.RaiseMessageReceivedEvent += RequestReceivedHandler;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            _mqttDualTopicClient.Start();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        public void RequestReceivedHandler(object sender, MessageEventArgs e)
        {
            _logger.LogInformation(e.Message);

            var goldData = GetGoldData();

            _mqttDualTopicClient.Send(goldData);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting external gold data api client service: " + _config.Value.Name);

            return Task.CompletedTask;
        }

        private string GetGoldData()
        {
            var goldPricesClient = new GoldPricesClient();
            var goldData = string.Empty;

            goldPricesClient.Get().ContinueWith(t =>
            {
                goldData = t.Result;
            })
            .Wait();

            _logger.LogInformation(goldData);

            return goldData;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping external gold data api client service.");

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing....");
        }
    }

    public class GoldExternalApiClientConfig
    {
        public string Name { get; set; }
    }
}
