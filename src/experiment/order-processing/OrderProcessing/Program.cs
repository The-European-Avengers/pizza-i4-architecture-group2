using Confluent.Kafka;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";

// --- Shared State ---
// We create a single shared state object, just like the other services.
builder.Services.AddSingleton<OrderState>();

// --- Register Background Services ---
builder.Services.AddSingleton<KafkaProducerService>(sp => 
    new KafkaProducerService(kafkaBootstrapServers, sp.GetRequiredService<ILogger<KafkaProducerService>>()));

// Service 1: Listens for "done" signals from DoughMachine
builder.Services.AddHostedService<DoughMachineSignalConsumer>();
// Service 2: The main processing loop that sends pizzas
builder.Services.AddHostedService<OrderProcessingService>();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// --- API Endpoints ---
app.MapPost("/start-order/{count:int}", 
    (int count, OrderState state, KafkaProducerService producer, ILogger<Program> logger) =>
    {
        if (count <= 0)
        {
            return Results.BadRequest("Order count must be greater than 0.");
        }
    
        logger.LogInformation("🚀 Order received for {Count} pizzas! Adding to queue...", count);
    
        int orderId = new Random().Next(100, 1000);

        // 1. Load all pizzas into the shared processing queue
        for (int i = 1; i <= count; i++)
        {
            state.PizzaQueue.Add(new PizzaOrderMessage
            {
                PizzaId = i,
                OrderId = orderId,
                OrderSize = count,
                StartTimestamp = null, 
                MsgDesc = "Order received",
                Sauce = "tomato",
                Baked = (i % 2 == 0),
                Cheese = ["mozzarella", "feta"],
                Meat = ["ham"],
                Veggies = ["mushroom", "spinach"]
            });
        }
    
        logger.LogInformation("✅ Added {Count} pizzas for Order {OrderId} to the queue.", count, orderId);
        return Results.Ok($"Order {orderId} for {count} pizzas added to queue.");
    });

app.Run();

// --- Shared State (like DoughShaperState) ---
public class OrderState
{
    public BlockingCollection<PizzaOrderMessage> PizzaQueue { get; } = new();
    public AutoResetEvent IsDoughMachineReady { get; } = new(true); // Start ready
}

// --- Kafka Producer (as a dedicated service) ---
public class KafkaProducerService : IDisposable
{
    private const string ORDER_PROCESSING_TOPIC = "order-processing";
    private const string DOUGH_MACHINE_TOPIC = "dough-machine";
    private readonly IProducer<string, string> _producer;

    public KafkaProducerService(string bootstrapServers, ILogger<KafkaProducerService> logger)
    {
        var producerConfig = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task ProduceOrderProcessingMessage(OrderProcessingMessage message)
    {
        var kafkaMessage = new Message<string, string>
        {
            Key = message.OrderId.ToString(),
            Value = JsonSerializer.Serialize(message)
        };
        await _producer.ProduceAsync(ORDER_PROCESSING_TOPIC, kafkaMessage);
    }
    
    public async Task ProducePizzaMessage(PizzaOrderMessage pizza)
    {
        var pizzaMessage = new Message<string, string>
        {
            Key = pizza.OrderId.ToString(),
            Value = JsonSerializer.Serialize(pizza)
        };
        await _producer.ProduceAsync(DOUGH_MACHINE_TOPIC, pizzaMessage);
    }
    
    public void Flush() => _producer.Flush();
    public void Dispose()
    {
        _producer.Flush();
        _producer.Dispose();
    }
}


// --- Service 1: Listens for "done" signals ---
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
            GroupId = "order-processor-group-main", // Static Group ID
            AutoOffsetReset = AutoOffsetReset.Latest
        };
    }

    // --- THIS IS THE CORRECT, SIMPLE LOGIC FOR THIS CLASS ---
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
                        _state.IsDoughMachineReady.Set(); // Just signal that the machine is free
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

// --- Service 2: Main Processing Loop (sends pizzas) ---
public class OrderProcessingService : BackgroundService
{
    private readonly OrderState _state;
    private readonly KafkaProducerService _producer;
    private readonly ILogger<OrderProcessingService> _logger;

    private readonly ConcurrentDictionary<int, long> _orderStartTimes = new();

    public OrderProcessingService(OrderState state, KafkaProducerService producer, ILogger<OrderProcessingService> logger)
    {
        _state = state;
        _producer = producer;
        _logger = logger;
    }

    // --- THIS IS WHERE THE TIMESTAMP LOGIC BELONGS ---
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Main Order Processing Service running. Waiting for pizzas in queue...");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // 1. Wait for a pizza to be in our queue
                var pizza = await Task.Run(() => _state.PizzaQueue.Take(stoppingToken), stoppingToken);

                // 2. Wait for the Dough Machine to be ready
                _logger.LogInformation("Waiting for Dough Machine to be ready before sending Pizza {PizzaId} (Order: {OrderId})...",
                    pizza.PizzaId, pizza.OrderId);
                await Task.Run(() => _state.IsDoughMachineReady.WaitOne(), stoppingToken);

                
                // --- NEW TIMESTAMP LOGIC ---
                // GetOrAdd is atomic. It will only execute the factory function
                // the very first time an orderId is seen.
                long productionStartTime = _orderStartTimes.GetOrAdd(pizza.OrderId, (orderId) => {
                    long newTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    _logger.LogInformation("🎉 Production starting for Order {OrderId} at {Timestamp}!", 
                        orderId, newTimestamp);
                    return newTimestamp;
                });
                
                // Set the timestamp for the current pizza
                pizza.StartTimestamp = productionStartTime;
                // --- END NEW LOGIC ---

                // --- COMBINED LOGIC BLOCK ---
                if (pizza.PizzaId == 1)
                {
                    _logger.LogInformation("First pizza in order - consumed initial 'ready' signal.");

                    // Send the "order-processing" topic message *now*
                    var orderProcessingMessage = new OrderProcessingMessage
                    {
                        OrderId = pizza.OrderId,
                        OrderSize = pizza.OrderSize,
                        StartTimestamp = productionStartTime // Use the real production start time
                    };
                    // Send the message (fire-and-forget, don't block the pizza)
                    _ = _producer.ProduceOrderProcessingMessage(orderProcessingMessage);
                }

                // 3. Send the pizza
                _logger.LogInformation("--> Sending Pizza {PizzaId} (Order: {OrderId}). Remaining in queue: {Count}",
                    pizza.PizzaId, pizza.OrderId, _state.PizzaQueue.Count);
                
                await _producer.ProducePizzaMessage(pizza);
                _producer.Flush(); // Ensure it's sent
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Main processing service stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Main processing service critical error.");
        }
    }
}


// --- Data Models ---
public class OrderProcessingMessage
{
    [JsonPropertyName("orderId")] public int OrderId { get; set; }
    [JsonPropertyName("orderSize")] public int OrderSize { get; set; }
    [JsonPropertyName("startTimestamp")] public long StartTimestamp { get; set; }
}

public class PizzaDoneMessage
{
    [JsonPropertyName("pizzaId")] public int PizzaId { get; set; }
    [JsonPropertyName("orderId")] public int OrderId { get; set; }
    [JsonPropertyName("doneMsg")] public bool DoneMsg { get; set; }
}

public class PizzaOrderMessage
{
    [JsonPropertyName("pizzaId")] public int PizzaId { get; set; }
    [JsonPropertyName("orderId")] public int OrderId { get; set; }
    [JsonPropertyName("orderSize")] public int OrderSize { get; set; }
    [JsonPropertyName("startTimestamp")] public long? StartTimestamp { get; set; }
    [JsonPropertyName("endTimestamp")] public long? EndTimestamp { get; set; }
    [JsonPropertyName("msgDesc")] public string MsgDesc { get; set; } = "";
    [JsonPropertyName("sauce")] public string Sauce { get; set; } = "";
    [JsonPropertyName("baked")] public bool Baked { get; set; }
    [JsonPropertyName("cheese")] public List<string> Cheese { get; set; } = [];
    [JsonPropertyName("meat")] public List<string> Meat { get; set; } = [];
    [JsonPropertyName("veggies")] public List<string> Veggies { get; set; } = [];
}