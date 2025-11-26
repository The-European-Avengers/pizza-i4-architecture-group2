using System.Text.Json;
using CheeseGrater.Data;
using Confluent.Kafka;

namespace CheeseGrater.Services;

// --- Service 2: Consumes "done" signals from Meat Machine ---
public class MeatSignalConsumerService : BackgroundService
{
    private const string CONSUME_TOPIC = "meat-machine-done";
    private readonly ILogger<MeatSignalConsumerService> _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly CheeseGraterState _state;

    public MeatSignalConsumerService(CheeseGraterState state, IConfiguration config, ILogger<MeatSignalConsumerService> logger)
    {
        _state = state;
        _logger = logger;
        var kafkaBootstrapServers = config["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092";
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "cheese-grater-signal-group",
            AutoOffsetReset = AutoOffsetReset.Latest
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Meat Signal Consumer (2/4) running. Waiting for Kafka...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
                consumer.Subscribe(CONSUME_TOPIC);
                _logger.LogInformation("Meat Signal Consumer (2/4) subscribed to {Topic}. Ready for signals.", CONSUME_TOPIC);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                    if (consumeResult?.Message == null) continue;

                    var doneMessage = JsonSerializer.Deserialize<PizzaDoneMessage>(consumeResult.Message.Value);
                    if (doneMessage != null)
                    {
                        _logger.LogInformation("<-- [Meat Machine Ready] signal received for Pizza {PizzaId} (Order: {OrderId}). Unlocking processor.", doneMessage.PizzaId, doneMessage.OrderId);
                        _state.IsMeatMachineReady.Set();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Meat Signal Consumer (2/4) stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Meat Signal Consumer (2/4) error. Retrying in 5s.");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}