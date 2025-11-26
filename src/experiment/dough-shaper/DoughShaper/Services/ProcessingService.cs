using System.Text.Json;
using Confluent.Kafka;
using DoughShaper.Data;

namespace DoughShaper.Services;

// --- Service 3: Main Processing Logic ---
public class ProcessingService : BackgroundService
{
    private const string NEXT_TOPIC = "sauce-machine";
    private const string DONE_TOPIC = "dough-shaper-done";

    private readonly ILogger<ProcessingService> _logger;
    private readonly ProducerConfig _producerConfig;
    private readonly DoughShaperState _state;

    public ProcessingService(DoughShaperState state, ILogger<ProcessingService> logger)
    {
        _state = state;
        _logger = logger;
        var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
        _producerConfig = new ProducerConfig { BootstrapServers = kafkaBootstrapServers };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Main Processor (3/3) running. Waiting for work...");

        using var producer = new ProducerBuilder<string, string>(_producerConfig).Build();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Step 1: Wait for a pizza
                _logger.LogInformation("Main Processor (3/3) waiting for a pizza from DoughMachine...");
                var pizza = await Task.Run(() => _state.PizzaQueue.Take(stoppingToken), stoppingToken);
                
                _logger.LogInformation("Main Processor (3/3) waiting for Sauce Machine to be ready (Pizza {PizzaId})...", pizza.PizzaId);
                await Task.Run(() => _state.IsSauceReady.WaitOne(), stoppingToken);

                if (pizza.PizzaId == 1)
                {
                    // This log is still helpful
                    _logger.LogInformation("First pizza in order - consumed initial 'ready' signal.");
                }

                // Step 3: Process the pizza
                _logger.LogInformation("Processing Pizza {PizzaId} (Order: {OrderId})...", pizza.PizzaId, pizza.OrderId);
                await Task.Delay(500, stoppingToken); // 0.5 second processing time
                pizza.MsgDesc = "Dough shaped";
                _logger.LogInformation("...Finished Pizza {PizzaId}. Sending to {Topic}", pizza.PizzaId, NEXT_TOPIC);

                // Step 4: Send pizza to Sauce Machine
                var nextMessage = new Message<string, string>
                {
                    Key = pizza.OrderId,
                    Value = JsonSerializer.Serialize(pizza)
                };
                await producer.ProduceAsync(NEXT_TOPIC, nextMessage, stoppingToken);

                // Step 5: Send "done" signal back to DoughMachine
                var doneMessage = new Message<string, string>
                {
                    Key = pizza.OrderId,
                    Value = JsonSerializer.Serialize(new PizzaDoneMessage
                    {
                        PizzaId = pizza.PizzaId,
                        OrderId = pizza.OrderId
                    })
                };
                await producer.ProduceAsync(DONE_TOPIC, doneMessage, stoppingToken);

                producer.Flush(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Main Processor (3/3) stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Main Processor (3/3) critical error.");
        }
    }
}
