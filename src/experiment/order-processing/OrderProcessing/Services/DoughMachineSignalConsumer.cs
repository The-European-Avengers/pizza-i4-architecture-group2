using System.Text.Json;
using Confluent.Kafka;
using OrderProcessing.Data;

namespace OrderProcessing.Services;

// --- Service 2: Listens for "done" signals ---
public class DoughMachineSignalConsumer : BackgroundService
{
    private readonly OrderState _state;
    private readonly ILogger<DoughMachineSignalConsumer> _logger;
    private readonly ConsumerConfig _consumerConfig;
    private const string READY_TOPIC = "dough-machine-done";

    public DoughMachineSignalConsumer(OrderState state, IConfiguration config, ILogger<DoughMachineSignalConsumer> logger)
    {
        _state = state;
        _logger = logger;
        var kafkaBootstrapServers = config["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092";
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "order-processor-group-main",
            AutoOffsetReset = AutoOffsetReset.Latest
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dough Machine Signal Consumer running...");
        using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
        consumer.Subscribe(READY_TOPIC);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                
                try
                {
                    var doneMessage = JsonSerializer.Deserialize<PizzaDoneMessage>(consumeResult.Message.Value);
                    if (doneMessage != null)
                    {
                        _logger.LogInformation("<-- [Dough Machine Ready] signal received for Pizza {PizzaId} (Order: {OrderId}).",
                            doneMessage.PizzaId, doneMessage.OrderId);
                        _state.IsDoughMachineReady.Set();
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize JSON: {Message}", consumeResult.Message.Value);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumer service stopping.");
        }
    }
}