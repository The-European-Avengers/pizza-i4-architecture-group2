using Confluent.Kafka;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// --- Shared State for the services ---
builder.Services.AddSingleton<DoughMachineState>();

// --- Register all background services ---
builder.Services.AddHostedService<PizzaConsumerService>();
builder.Services.AddHostedService<ShaperSignalConsumerService>();
builder.Services.AddHostedService<RestockDoneConsumerService>();
builder.Services.AddHostedService<ProcessingService>();

var app = builder.Build();
app.MapGet("/", () => "Dough Machine Service is running (v4 - With Restocking).");
app.Run();

// --- Shared State ---
public class DoughMachineState
{
    // A thread-safe queue to hold pizzas from the OrderProcessor
    public BlockingCollection<PizzaOrderMessage> PizzaQueue { get; } = new BlockingCollection<PizzaOrderMessage>();

    // A signal to indicate that the DoughShaper is ready
    public AutoResetEvent IsShaperReady { get; } = new AutoResetEvent(true);

    // Stock management
    public int DoughStock { get; set; } = 11; // Start with 100 units
    public bool IsRestockInProgress { get; set; } = false;
    private readonly object _stockLock = new object();

    public bool TryUseDough()
    {
        lock (_stockLock)
        {
            if (DoughStock > 0)
            {
                DoughStock--;
                return true;
            }
            return false;
        }
    }

    public int GetCurrentStock()
    {
        lock (_stockLock)
        {
            return DoughStock;
        }
    }

    public void AddStock(int amount)
    {
        lock (_stockLock)
        {
            DoughStock += amount;
        }
    }

    public bool ShouldRequestRestock()
    {
        lock (_stockLock)
        {
            return DoughStock <= 10 && !IsRestockInProgress;
        }
    }
}

// --- Service 1: Consumes new pizzas from OrderProcessing ---
public class PizzaConsumerService : BackgroundService
{
    private const string CONSUME_TOPIC = "dough-machine";
    private readonly ILogger<PizzaConsumerService> _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly DoughMachineState _state;

    public PizzaConsumerService(DoughMachineState state, ILogger<PizzaConsumerService> logger)
    {
        _state = state;
        _logger = logger;
        var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "dough-machine-group",
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
                _logger.LogInformation("Pizza Consumer (1/4) subscribed to {Topic}. Ready for new pizzas.", CONSUME_TOPIC);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                    if (consumeResult?.Message == null) continue;

                    var pizza = JsonSerializer.Deserialize<PizzaOrderMessage>(consumeResult.Message.Value);
                    if (pizza != null)
                    {
                        _logger.LogInformation("--> Pizza {PizzaId} (Order: {OrderId}) received from OrderProcessing. Adding to queue.", pizza.PizzaId, pizza.OrderId);
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
                        Key = "dough-machine",
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
                    Key = pizza.OrderId.ToString(), 
                    Value = JsonSerializer.Serialize(pizza) 
                };
                await producer.ProduceAsync(NEXT_TOPIC, nextMessage, stoppingToken);

                // Step 7: Send "done" signal back to OrderProcessing
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