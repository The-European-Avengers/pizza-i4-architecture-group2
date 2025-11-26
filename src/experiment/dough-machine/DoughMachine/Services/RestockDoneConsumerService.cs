using System.Text.Json;
using Confluent.Kafka;
using DoughMachine.Data;

namespace DoughMachine;

// --- Service 3: Consumes restock done messages ---
public class RestockDoneConsumerService : BackgroundService
{
    private const string CONSUME_TOPIC = "dough-machine-restock-done";
    private readonly ILogger<RestockDoneConsumerService> _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly DoughMachineState _state;

    public RestockDoneConsumerService(DoughMachineState state, ILogger<RestockDoneConsumerService> logger)
    {
        _state = state;
        _logger = logger;
        var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "dough-machine-restock-group",
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
                    if (restockDone != null && restockDone.MachineId == "dough-machine")
                    {
                        foreach (var item in restockDone.Items)
                        {
                            if (item.ItemType == "dough")
                            {
                                _state.AddStock(item.DeliveredAmount);
                                _logger.LogInformation("✅ Restock completed! Received {Amount} units of dough. New stock: {Stock}", 
                                    item.DeliveredAmount, _state.GetCurrentStock());
                            }
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