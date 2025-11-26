using System.Text.Json;
using CheeseGrater.Data;
using Confluent.Kafka;

namespace CheeseGrater.Services;

// --- Service 4: Main Processing Logic ---
public class ProcessingService : BackgroundService
{
    private const string NEXT_TOPIC = "meat-machine";
    private const string DONE_TOPIC = "cheese-machine-done";
    private const string RESTOCK_TOPIC = "cheese-machine-restock";

    private readonly ILogger<ProcessingService> _logger;
    private readonly ProducerConfig _producerConfig;
    private readonly CheeseGraterState _state;

    public ProcessingService(CheeseGraterState state, IConfiguration config, ILogger<ProcessingService> logger)
    {
        _state = state;
        _logger = logger;
        var kafkaBootstrapServers = config["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092";
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
                // Step 1: Wait for a pizza
                _logger.LogInformation("Main Processor (4/4) waiting for a pizza from SauceMachine...");
                var pizza = await Task.Run(() => _state.PizzaQueue.Take(stoppingToken), stoppingToken);
                
                // Step 2: Check if restock is needed before processing
                if (_state.ShouldRequestRestock())
                {
                    var restockNeeds = _state.GetRestockNeeds();
                    _logger.LogWarning("⚠️ Cheese stock low. Requesting restock for {Count} types:", restockNeeds.Count);
                    
                    foreach (var need in restockNeeds)
                    {
                        _logger.LogWarning("   • {CheeseType}: {Current} units (requesting {Requested})", 
                            need.ItemType, need.CurrentStock, need.RequestedAmount);
                    }

                    _state.IsRestockInProgress = true;

                    var restockRequest = new RestockRequestMessage
                    {
                        MachineId = "cheese-machine",
                        Items = restockNeeds,
                        RequestTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    var restockMessage = new Message<string, string>
                    {
                        Key = pizza.OrderId,
                        Value = JsonSerializer.Serialize(restockRequest)
                    };
                    await producer.ProduceAsync(RESTOCK_TOPIC, restockMessage, stoppingToken);
                    producer.Flush(stoppingToken);
                    _logger.LogInformation("📦 Restock request sent.");
                }

                // Step 3: Check if we have all required cheese types for this pizza
                bool canProcessPizza = true;
                var missingCheeses = new List<string>();
                
                foreach (var cheeseType in pizza.Cheese)
                {
                    if (_state.GetCheeseStock(cheeseType) <= 0)
                    {
                        canProcessPizza = false;
                        missingCheeses.Add(cheeseType);
                    }
                }

                // Step 4: Wait for stock if needed
                while (!canProcessPizza)
                {
                    _logger.LogWarning("⏳ Missing cheese for Pizza {PizzaId}: {MissingTypes}. Waiting for restock...", 
                        pizza.PizzaId, string.Join(", ", missingCheeses));
                    await Task.Delay(1000, stoppingToken);

                    // Re-check stock
                    canProcessPizza = true;
                    missingCheeses.Clear();
                    foreach (var cheeseType in pizza.Cheese)
                    {
                        if (_state.GetCheeseStock(cheeseType) <= 0)
                        {
                            canProcessPizza = false;
                            missingCheeses.Add(cheeseType);
                        }
                    }
                }

                // Step 5: Wait for Meat Machine signal 
                _logger.LogInformation("Main Processor (4/4) waiting for Meat Machine to be ready (Pizza {PizzaId})...", pizza.PizzaId);
                await Task.Run(() => _state.IsMeatMachineReady.WaitOne(), stoppingToken);

                if (pizza.PizzaId == 1)
                {
                    _logger.LogInformation("First pizza in order - consumed initial 'ready' signal.");
                }

                // Step 6: Process the pizza - apply each cheese type (100ms per cheese)
                _logger.LogInformation("Grating cheese for Pizza {PizzaId} (Order: {OrderId}). Cheese types: {CheeseTypes}", 
                    pizza.PizzaId, pizza.OrderId, string.Join(", ", pizza.Cheese));

                foreach (var cheeseType in pizza.Cheese)
                {
                    // Use the cheese (deduct from stock)
                    if (_state.TryUseCheese(cheeseType))
                    {
                        _logger.LogInformation("   • Applying {CheeseType}... (Stock remaining: {Stock})", 
                            cheeseType, _state.GetCheeseStock(cheeseType));
                        await Task.Delay(250, stoppingToken); // 250ms per cheese type
                    }
                    else
                    {
                        _logger.LogError("ERROR: Failed to use {CheeseType} - this shouldn't happen!", cheeseType);
                    }
                }

                pizza.MsgDesc = "Cheese grated";
                _logger.LogInformation("...Finished Pizza {PizzaId}. Sending to {Topic}", pizza.PizzaId, NEXT_TOPIC);

                // Step 7: Send pizza to Meat Machine
                var nextMessage = new Message<string, string>
                {
                    Key = pizza.OrderId,
                    Value = JsonSerializer.Serialize(pizza)
                };
                await producer.ProduceAsync(NEXT_TOPIC, nextMessage, stoppingToken);

                // Step 8: Send "done" signal back to SauceMachine
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