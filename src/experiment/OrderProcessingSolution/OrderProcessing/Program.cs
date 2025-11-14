using Confluent.Kafka;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization; // Added for JSON property names

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
var kafkaConfig = new ProducerConfig { BootstrapServers = kafkaBootstrapServers };

// --- Singleton Service for Order Logic ---
builder.Services.AddSingleton<OrderService>();

// --- Background Service (Kafka Consumer) ---
builder.Services.AddHostedService<KafkaConsumerService>();

var app = builder.Build();

app.MapPost("/start-order", (OrderService orderService) =>
{
    app.Logger.LogInformation("🚀 Order received! Starting production line...");
    orderService.StartOrder();
    return Results.Ok("Order started. Sending first pizza.");
});

app.Run();

// --- Services ---

/// <summary>
/// Manages the order state and sends pizzas to the dough machine.
/// </summary>
public class OrderService
{
    // --- Kafka Topics ---
    private const string DOUGH_MACHINE_TOPIC = "dough-machine";
    private const string ORDER_PROCESSING_TOPIC = "order-processing"; // New topic

    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<OrderService> _logger;
    
    // A thread-safe queue to hold the pizzas.
    private readonly ConcurrentQueue<PizzaOrderMessage> _pizzaQueue = new(); // Changed to new model
    
    // Flag to ensure we only start once.
    private int _orderStarted = 0;
    private const int ORDER_SIZE = 100; // Define order size

    public OrderService(ILogger<OrderService> logger)
    {
        _logger = logger;
        var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
        _producer = new ProducerBuilder<Null, string>(new ProducerConfig 
        { 
            BootstrapServers = kafkaBootstrapServers 
        }).Build();
    }

    public void StartOrder()
    {
        // Use Interlocked.CompareExchange to ensure this only runs once.
        if (Interlocked.CompareExchange(ref _orderStarted, 1, 0) == 0)
        {
            _logger.LogInformation($"Loading {ORDER_SIZE} pizzas into the queue...");

            // --- Create Order-level details ---
            int orderId = new Random().Next(100, 999);
            long startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // --- 1. Send message to order-processing topic ---
            var orderProcessingMsg = new OrderProcessingMessage
            {
                OrderId = orderId,
                StartTimestamp = startTimestamp
            };
            var orderMessage = new Message<Null, string> { Value = JsonSerializer.Serialize(orderProcessingMsg) };
            _producer.Produce(ORDER_PROCESSING_TOPIC, orderMessage, (report) =>
            {
                if(report.Error.IsError)
                    _logger.LogError("Kafka produce error (order-processing): {Reason}", report.Error.Reason);
            });

            // --- 2. Create all pizza messages for the order ---
            for (int i = 1; i <= ORDER_SIZE; i++)
            {
                _pizzaQueue.Enqueue(new PizzaOrderMessage
                {
                    PizzaId = i,
                    OrderId = orderId,
                    OrderSize = ORDER_SIZE,
                    StartTimestamp = startTimestamp,
                    EndTimestamp = null,
                    MsgDesc = "Order submitted",
                    Sauce = "tomato",
                    Baked = (i % 2 == 0), // Mix of baked/freezer
                    Meat = ["salami"],
                    Cheese = ["mozzarella"],
                    Veggies = ["peppers"]
                });
            }

            // --- 3. Kick off the process by sending the first pizza. ---
            SendNextPizza();
        }
        else
        {
            _logger.LogWarning("Order already started.");
        }
    }

    public void SendNextPizza()
    {
        if (_pizzaQueue.TryDequeue(out var pizza))
        {
            _logger.LogInformation("--> Sending Pizza {Id} (Order: {OrderId}). Remaining: {Count}", pizza.PizzaId, pizza.OrderId, _pizzaQueue.Count);
            
            var message = new Message<Null, string>
            {
                Value = JsonSerializer.Serialize(pizza)
            };
            
            _producer.Produce(DOUGH_MACHINE_TOPIC, message, (report) =>
            {
                if(report.Error.IsError)
                    _logger.LogError("Kafka produce error (dough-machine): {Reason}", report.Error.Reason);
            });
            _producer.Flush(TimeSpan.FromSeconds(10));
        }
        else
        {
            _logger.LogInformation("✅✅✅ Order Complete! All {ORDER_SIZE} pizzas sent. ✅✅✅", ORDER_SIZE);
            // Set _orderStarted back to 0 if you want to allow new orders
            Interlocked.Exchange(ref _orderStarted, 0); 
        }
    }
}

/// <summary>
/// Background service that listens for the "ready" signal from the dough machine.
/// </summary>
public class KafkaConsumerService : BackgroundService
{
    // --- Kafka Topic ---
    private const string DOUGH_MACHINE_DONE_TOPIC = "dough-machine-done"; // Renamed topic

    private readonly OrderService _orderService;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly ConsumerConfig _consumerConfig;

    public KafkaConsumerService(OrderService orderService, ILogger<KafkaConsumerService> logger)
    {
        _orderService = orderService;
        _logger = logger;
        
        var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = $"order-processor-group-{Guid.NewGuid()}", // Unique group ID
            AutoOffsetReset = AutoOffsetReset.Earliest // Corrected from .Latest
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka Consumer Service running. Waiting for Kafka...");

        // Retry loop to wait for Kafka to be ready
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<Ignore, string>(_consumerConfig).Build();
                consumer.Subscribe(DOUGH_MACHINE_DONE_TOPIC);
                _logger.LogInformation("Consumer subscribed to {Topic}. Ready to process signals.", DOUGH_MACHINE_DONE_TOPIC);

                // Inner loop for consuming messages
                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);

                    if (consumeResult?.Message?.Value != null)
                    {
                        // Deserialize the new "PizzaDoneMessage"
                        var doneMessage = JsonSerializer.Deserialize<PizzaDoneMessage>(consumeResult.Message.Value);
                        if (doneMessage != null && doneMessage.DoneMsg)
                        {
                            _logger.LogInformation("<-- [Dough Machine Ready] signal received for Pizza {PizzaId} (Order: {OrderId}). Requesting next pizza.", doneMessage.PizzaId, doneMessage.OrderId);
                            
                            // Tell the OrderService to send the next pizza.
                            _orderService.SendNextPizza();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consumer service stopping (OperationCanceled).");
                break; // Exit loop on cancellation
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka consumer error. Retrying in 5 seconds... (Kafka might be starting or topic not ready)");
                await Task.Delay(5000, stoppingToken); // Wait 5s before retrying connection
            }
        }
    }
}

// --- Data Models (from PizzaProductionExperiment.md) ---

// The main message for a single pizza
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
    public bool Baked { get; set; } // Renamed from IsBaked
    [JsonPropertyName("cheese")]
    public List<string> Cheese { get; set; } = []; // Renamed from Chees
    [JsonPropertyName("meat")]
    public List<string> Meat { get; set; } = [];
    [JsonPropertyName("veggies")]
    public List<string> Veggies { get; set; } = []; // Renamed from Vegetable
}

// The "done" signal from a machine
public class PizzaDoneMessage
{
    [JsonPropertyName("pizzaId")]
    public int PizzaId { get; set; }
    [JsonPropertyName("orderId")]
    public int OrderId { get; set; } // Renamed from "id"
    [JsonPropertyName("doneMsg")]
    public bool DoneMsg { get; set; } = true;
}

// The message for the order-processing topic
public class OrderProcessingMessage
{
    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }
    [JsonPropertyName("startTimestamp")]
    public long StartTimestamp { get; set; }
}