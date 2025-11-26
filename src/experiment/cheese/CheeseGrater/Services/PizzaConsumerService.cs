using System.Text.Json;
using CheeseGrater.Data;
using Confluent.Kafka;

namespace CheeseGrater.Services;

// --- Service 1: Consumes pizzas from SauceMachine ---
public class PizzaConsumerService : BackgroundService
{
    private const string CONSUME_TOPIC = "cheese-machine";
    private readonly ILogger<PizzaConsumerService> _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly CheeseGraterState _state;

    public PizzaConsumerService(CheeseGraterState state, IConfiguration config, ILogger<PizzaConsumerService> logger)
    {
        _state = state;
        _logger = logger;
        var kafkaBootstrapServers = config["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092";
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "cheese-grater-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Pizza Consumer (1/4) running. Waiting for Kafka...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
                consumer.Subscribe(CONSUME_TOPIC);
                _logger.LogInformation("Pizza Consumer (1/4) subscribed to {Topic}. Ready for pizzas.", CONSUME_TOPIC);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                    if (consumeResult?.Message == null) continue;

                    var pizza = JsonSerializer.Deserialize<PizzaOrderMessage>(consumeResult.Message.Value);
                    if (pizza != null)
                    {
                        _logger.LogInformation("--> Pizza {PizzaId} (Order: {OrderId}) received from SauceMachine. Adding to queue.", pizza.PizzaId, pizza.OrderId);
                        _state.PizzaQueue.Add(pizza, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Pizza Consumer (1/4) stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pizza Consumer (1/4) error. Retrying in 5s.");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}