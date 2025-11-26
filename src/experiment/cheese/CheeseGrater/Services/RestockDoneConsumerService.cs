using System.Text.Json;
using CheeseGrater.Data;
using Confluent.Kafka;

namespace CheeseGrater.Services;

// --- Service 3: Consumes restock done messages ---
public class RestockDoneConsumerService : BackgroundService
{
    private const string CONSUME_TOPIC = "cheese-machine-restock-done";
    private readonly ILogger<RestockDoneConsumerService> _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly CheeseGraterState _state;

    public RestockDoneConsumerService(CheeseGraterState state, IConfiguration config, ILogger<RestockDoneConsumerService> logger)
    {
        _state = state;
        _logger = logger;
        var kafkaBootstrapServers = config["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092";
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "cheese-grater-restock-group",
            AutoOffsetReset = AutoOffsetReset.Latest
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Restock Done Consumer (3/4) running. Waiting for Kafka...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
                consumer.Subscribe(CONSUME_TOPIC);
                _logger.LogInformation("Restock Done Consumer (3/4) subscribed to {Topic}. Ready for restock confirmations.", CONSUME_TOPIC);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                    if (consumeResult?.Message == null) continue;

                    var restockDone = JsonSerializer.Deserialize<RestockDoneMessage>(consumeResult.Message.Value);
                    if (restockDone != null && restockDone.MachineId == "cheese-machine")
                    {
                        _logger.LogInformation("✅ Restock completed! Processing {Count} cheese types:", restockDone.Items.Count);
                        foreach (var item in restockDone.Items)
                        {
                            _state.AddCheeseStock(item.ItemType, item.DeliveredAmount);
                            _logger.LogInformation("   • {CheeseType}: +{Amount} units (New stock: {Stock})", 
                                item.ItemType, item.DeliveredAmount, _state.GetCheeseStock(item.ItemType));
                        }
                        _state.IsRestockInProgress = false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Restock Done Consumer (3/4) stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Restock Done Consumer (3/4) error. Retrying in 5s.");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}