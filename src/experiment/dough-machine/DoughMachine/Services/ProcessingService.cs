using System.Text.Json;
using Confluent.Kafka;
using DoughMachine.Data;

namespace DoughMachine;

// --- Service 4: The Main Processor Logic ---
public class ProcessingService : BackgroundService
{
    private const string NEXT_TOPIC = "dough-shaper";
    private const string DONE_TOPIC = "dough-machine-done";
    private const string RESTOCK_TOPIC = "dough-machine-restock";

    private readonly ILogger<ProcessingService> _logger;
    private readonly ProducerConfig _producerConfig;
    private readonly DoughMachineState _state;

    public ProcessingService(DoughMachineState state, ILogger<ProcessingService> logger)
    {
        _state = state;
        _logger = logger;
        var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
        _producerConfig = new ProducerConfig { BootstrapServers = kafkaBootstrapServers };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Main Processor (4/4) running. Waiting for work...");

        using var producer = new ProducerBuilder<string, string>(_producerConfig).Build();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Step 1: Wait for a pizza to be in our local queue
                _logger.LogInformation("Main Processor (4/4) waiting for a pizza from OrderProcessing...");
                var pizza = await Task.Run(() => _state.PizzaQueue.Take(stoppingToken), stoppingToken);
                
                // Step 2: Check stock before processing and request restock if needed
                if (_state.ShouldRequestRestock())
                {
                    _logger.LogWarning("⚠️ Dough stock low ({Stock} units). Requesting restock...", _state.GetCurrentStock());
                    _state.IsRestockInProgress = true;

                    var restockRequest = new RestockRequestMessage
                    {
                        MachineId = "dough-machine",
                        Items = new List<RestockItem>
                        {
                            new RestockItem
                            {
                                ItemType = "dough",
                                CurrentStock = _state.GetCurrentStock(),
                                RequestedAmount = 90
                            }
                        },
                        RequestTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    var restockMessage = new Message<string, string>
                    {
                        Key = pizza.OrderId,
                        Value = JsonSerializer.Serialize(restockRequest)
                    };
                    await producer.ProduceAsync(RESTOCK_TOPIC, restockMessage, stoppingToken);
                    producer.Flush(stoppingToken);
                    _logger.LogInformation("📦 Restock request sent. Requested 90 units.");
                }

                // Step 3: Wait for stock to be available
                while (!_state.TryUseDough())
                {
                    _logger.LogWarning("⏳ No dough in stock. Waiting for restock...");
                    await Task.Delay(1000, stoppingToken); // Wait 1 second before checking again
                }

                _logger.LogInformation("✅ Dough available. Current stock: {Stock}", _state.GetCurrentStock());

                // Step 4: Wait for the Dough Shaper to be ready
                _logger.LogInformation("Main Processor (4/4) waiting for Dough Shaper to be ready (Pizza {PizzaId})...", pizza.PizzaId);
                await Task.Run(() => _state.IsShaperReady.WaitOne(), stoppingToken);

                if (pizza.PizzaId == 1)
                {
                    _logger.LogInformation("First pizza in order - consumed initial 'ready' signal.");
                }
                pizza.StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Step 5: Process the pizza
                _logger.LogInformation("Processing Pizza {PizzaId} (Order: {OrderId})...", pizza.PizzaId, pizza.OrderId);
                await Task.Delay(1000, stoppingToken); // 1 second processing time
                pizza.MsgDesc = "Dough prepared";
                _logger.LogInformation("...Finished Pizza {PizzaId}. Sending to {Topic}", pizza.PizzaId, NEXT_TOPIC);

                // Step 6: Send pizza to DoughShaper
                var nextMessage = new Message<string, string> 
                { 
                    Key = pizza.OrderId, 
                    Value = JsonSerializer.Serialize(pizza) 
                };
                await producer.ProduceAsync(NEXT_TOPIC, nextMessage, stoppingToken);

                // Step 7: Send "done" signal back to OrderProcessing
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
            _logger.LogInformation("Main Processor (4/4) stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Main Processor (4/4) critical error.");
        }
    }
}