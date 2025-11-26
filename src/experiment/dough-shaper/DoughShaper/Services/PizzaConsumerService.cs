using System.Text.Json;
using Confluent.Kafka;
using DoughShaper.Data;

namespace DoughShaper.Services;

// --- Service 1: Consumes pizzas from DoughMachine ---
public class PizzaConsumerService : BackgroundService
{
    private const string CONSUME_TOPIC = "dough-shaper";
    private readonly ILogger<PizzaConsumerService> _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly DoughShaperState _state;

    public PizzaConsumerService(DoughShaperState state, ILogger<PizzaConsumerService> logger)
    {
        _state = state;
        _logger = logger;
        var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "dough-shaper-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Pizza Consumer (1/3) running. Waiting for Kafka...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
                consumer.Subscribe(CONSUME_TOPIC);
                _logger.LogInformation("Pizza Consumer (1/3) subscribed to {Topic}. Ready for pizzas.", CONSUME_TOPIC);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                    if (consumeResult?.Message == null) continue;

                    var pizza = JsonSerializer.Deserialize<PizzaOrderMessage>(consumeResult.Message.Value);
                    if (pizza != null)
                    {
                        _logger.LogInformation("--> Pizza {PizzaId} (Order: {OrderId}) received from DoughMachine. Adding to queue.", pizza.PizzaId, pizza.OrderId);
                        _state.PizzaQueue.Add(pizza, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Pizza Consumer (1/3) stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pizza Consumer (1/3) error. Retrying in 5s.");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}