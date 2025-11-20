using Confluent.Kafka;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// --- Shared State ---
builder.Services.AddSingleton<CheeseGraterState>();

// --- Register background services ---
builder.Services.AddHostedService<PizzaConsumerService>();
builder.Services.AddHostedService<MeatSignalConsumerService>();
builder.Services.AddHostedService<RestockDoneConsumerService>();
builder.Services.AddHostedService<ProcessingService>();

var app = builder.Build();
app.MapGet("/", () => "Cheese Grater Service is running (v3 - With Restocking).");
app.Run();

// --- Shared State ---
public class CheeseGraterState
{
    public BlockingCollection<PizzaOrderMessage> PizzaQueue { get; } = new BlockingCollection<PizzaOrderMessage>();
    public AutoResetEvent IsMeatMachineReady { get; } = new AutoResetEvent(true);

    // Cheese stock - each type starts with 100 units
    private readonly Dictionary<string, int> _cheeseStock = new Dictionary<string, int>
    {
        { "mozzarella", 11 },
        { "cheddar", 11 },
        { "smoked provolone", 11 },
        { "feta", 11 },
        { "provolone", 11 },
        { "parmesan", 11 },
        { "gorgonzola", 11 },
        { "jalapeño jack", 11 }
    };

    public bool IsRestockInProgress { get; set; } = false;
    private readonly object _stockLock = new object();

    public bool TryUseCheese(string cheeseType)
    {
        lock (_stockLock)
        {
            if (_cheeseStock.TryGetValue(cheeseType, out int stock) && stock > 0)
            {
                _cheeseStock[cheeseType]--;
                return true;
            }
            return false;
        }
    }

    public int GetCheeseStock(string cheeseType)
    {
        lock (_stockLock)
        {
            return _cheeseStock.TryGetValue(cheeseType, out int stock) ? stock : 0;
        }
    }

    public void AddCheeseStock(string cheeseType, int amount)
    {
        lock (_stockLock)
        {
            if (_cheeseStock.ContainsKey(cheeseType))
            {
                _cheeseStock[cheeseType] += amount;
            }
        }
    }

    public List<RestockItem> GetRestockNeeds()
    {
        lock (_stockLock)
        {
            var needs = new List<RestockItem>();

            foreach (var kvp in _cheeseStock)
            {
                // Must restock if <= 10
                if (kvp.Value <= 10)
                {
                    needs.Add(new RestockItem
                    {
                        ItemType = kvp.Key,
                        CurrentStock = kvp.Value,
                        RequestedAmount = 100 - kvp.Value
                    });
                }
                // Opportunistic restock if <= 20 (only if we're already restocking)
                else if (kvp.Value <= 20 && needs.Count > 0)
                {
                    needs.Add(new RestockItem
                    {
                        ItemType = kvp.Key,
                        CurrentStock = kvp.Value,
                        RequestedAmount = 100 - kvp.Value
                    });
                }
            }

            return needs;
        }
    }

    public bool ShouldRequestRestock()
    {
        lock (_stockLock)
        {
            if (IsRestockInProgress) return false;
            return _cheeseStock.Any(kvp => kvp.Value <= 10);
        }
    }

    public Dictionary<string, int> GetAllStockLevels()
    {
        lock (_stockLock)
        {
            return new Dictionary<string, int>(_cheeseStock);
        }
    }
}

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
                        Key = "cheese-machine",
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
                        await Task.Delay(100, stoppingToken); // 100ms per cheese type
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
                    Key = pizza.OrderId.ToString(),
                    Value = JsonSerializer.Serialize(pizza)
                };
                await producer.ProduceAsync(NEXT_TOPIC, nextMessage, stoppingToken);

                // Step 8: Send "done" signal back to SauceMachine
                var doneMessage = new Message<string, string>
                {
                    Key = pizza.OrderId.ToString(),
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

// --- Data Models ---
public class PizzaOrderMessage
{
    [JsonPropertyName("pizzaId")]
    public int PizzaId { get; set; }
    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }
    [JsonPropertyName("orderSize")]
    public int OrderSize { get; set; }
    [JsonPropertyName("startTimestamp")]
    public long? StartTimestamp { get; set; }
    [JsonPropertyName("endTimestamp")]
    public long? EndTimestamp { get; set; }
    [JsonPropertyName("msgDesc")]
    public string? MsgDesc { get; set; }
    [JsonPropertyName("sauce")]
    public string? Sauce { get; set; }
    [JsonPropertyName("baked")]
    public bool Baked { get; set; }
    [JsonPropertyName("cheese")]
    public List<string> Cheese { get; set; } = [];
    [JsonPropertyName("meat")]
    public List<string> Meat { get; set; } = [];
    [JsonPropertyName("veggies")]
    public List<string> Veggies { get; set; } = [];
}

public class PizzaDoneMessage
{
    [JsonPropertyName("pizzaId")]
    public int PizzaId { get; set; }
    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }
    [JsonPropertyName("doneMsg")]
    public bool DoneMsg { get; set; } = true;
}

public class RestockRequestMessage
{
    [JsonPropertyName("machineId")]
    public string MachineId { get; set; } = "";
    [JsonPropertyName("items")]
    public List<RestockItem> Items { get; set; } = [];
    [JsonPropertyName("requestTimestamp")]
    public long RequestTimestamp { get; set; }
}

public class RestockDoneMessage
{
    [JsonPropertyName("machineId")]
    public string MachineId { get; set; } = "";
    [JsonPropertyName("items")]
    public List<RestockDoneItem> Items { get; set; } = [];
    [JsonPropertyName("completedTimestamp")]
    public long CompletedTimestamp { get; set; }
}

public class RestockItem
{
    [JsonPropertyName("itemType")]
    public string ItemType { get; set; } = "";
    [JsonPropertyName("currentStock")]
    public int CurrentStock { get; set; }
    [JsonPropertyName("requestedAmount")]
    public int RequestedAmount { get; set; }
}

public class RestockDoneItem
{
    [JsonPropertyName("itemType")]
    public string ItemType { get; set; } = "";
    [JsonPropertyName("deliveredAmount")]
    public int DeliveredAmount { get; set; }
}