using System.Text.Json;
using Confluent.Kafka;
using DoughShaper.Data;

namespace DoughShaper.Services;

// --- Service 2: Consumes "done" signals from Sauce Machine ---
public class SauceSignalConsumerService : BackgroundService
{
    private const string CONSUME_TOPIC = "sauce-machine-done";
    private readonly ILogger<SauceSignalConsumerService> _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly DoughShaperState _state;

    public SauceSignalConsumerService(DoughShaperState state, ILogger<SauceSignalConsumerService> logger)
    {
        _state = state;
        _logger = logger;
        var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "dough-shaper-sauce-signal-group",
            AutoOffsetReset = AutoOffsetReset.Latest
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sauce Signal Consumer (2/3) running. Waiting for Kafka...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
                consumer.Subscribe(CONSUME_TOPIC);
                _logger.LogInformation("Sauce Signal Consumer (2/3) subscribed to {Topic}. Ready for signals.", CONSUME_TOPIC);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                    if (consumeResult?.Message == null) continue;

                    var doneMessage = JsonSerializer.Deserialize<PizzaDoneMessage>(consumeResult.Message.Value);
                    if (doneMessage != null)
                    {
                        _logger.LogInformation("<-- [Sauce Machine Ready] signal received for Pizza {PizzaId} (Order: {OrderId}). Unlocking processor.", doneMessage.PizzaId, doneMessage.OrderId);
                        _state.IsSauceReady.Set();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Sauce Signal Consumer (2/3) stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sauce Signal Consumer (2/3) error. Retrying in 5s.");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}