using System.Text.Json;
using Confluent.Kafka;
using DoughMachine.Data;

namespace DoughMachine;

// --- Service 2: Consumes "done" signals from DoughShaper ---
public class ShaperSignalConsumerService : BackgroundService
{
    private const string CONSUME_TOPIC = "dough-shaper-done";
    private readonly ILogger<ShaperSignalConsumerService> _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly DoughMachineState _state;

    public ShaperSignalConsumerService(DoughMachineState state, ILogger<ShaperSignalConsumerService> logger)
    {
        _state = state;
        _logger = logger;
        var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "dough-machine-shaper-signal-group",
            AutoOffsetReset = AutoOffsetReset.Latest
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Shaper Signal Consumer (2/4) running. Waiting for Kafka...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
                consumer.Subscribe(CONSUME_TOPIC);
                _logger.LogInformation("Shaper Signal Consumer (2/4) subscribed to {Topic}. Ready for signals.", CONSUME_TOPIC);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                    if (consumeResult?.Message == null) continue;

                    var doneMessage = JsonSerializer.Deserialize<PizzaDoneMessage>(consumeResult.Message.Value);
                    if (doneMessage != null)
                    {
                        _logger.LogInformation("<-- [Dough Shaper Ready] signal received for Pizza {PizzaId} (Order: {OrderId}). Unlocking processor.", doneMessage.PizzaId, doneMessage.OrderId);
                        _state.IsShaperReady.Set();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Shaper Signal Consumer (2/4) stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Shaper Signal Consumer (2/4) error. Retrying in 5s.");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}