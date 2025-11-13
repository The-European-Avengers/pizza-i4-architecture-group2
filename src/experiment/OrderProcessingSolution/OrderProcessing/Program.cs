using Confluent.Kafka;
using System.Collections.Concurrent;
using System.Text.Json;

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
    // --- Kafka Topic ---
    private const string DOUGH_MACHINE_TOPIC = "dough-machine-topic";

    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<OrderService> _logger;
    
    // A thread-safe queue to hold the pizzas.
    private readonly ConcurrentQueue<Pizza> _pizzaQueue = new();
    
    // Flag to ensure we only start once.
    private int _orderStarted = 0;

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
            _logger.LogInformation("Loading 100 pizzas into the queue...");
            // Create a dummy order of 100 pizzas
            for (int i = 1; i <= 100; i++)
            {
                _pizzaQueue.Enqueue(new Pizza
                {
                    Id = i,
                    Size = "medium",
                    IsBaked = (i % 2 == 0), // Mix of baked/freezer
                    Sauce = "tomato",
                    Meat = ["salami"],
                    Cheese = ["mozzarella"],
                    Vegetable = ["peppers"]
                });
            }

            // Kick off the process by sending the first pizza.
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
            _logger.LogInformation("--> Sending Pizza {Id}. Remaining: {Count}", pizza.Id, _pizzaQueue.Count);
            
            var message = new Message<Null, string>
            {
                Value = JsonSerializer.Serialize(pizza)
            };
            
            _producer.Produce(DOUGH_MACHINE_TOPIC, message, (report) =>
            {
                if(report.Error.IsError)
                    _logger.LogError("Kafka produce error: {Reason}", report.Error.Reason);
            });
            _producer.Flush(TimeSpan.FromSeconds(10));
        }
        else
        {
            _logger.LogInformation("✅✅✅ Order Complete! All 100 pizzas sent. ✅✅✅");
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
    private const string DOUGH_MACHINE_READY_TOPIC = "dough-machine-ready-topic";

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
            AutoOffsetReset = AutoOffsetReset.Earliest // <-- THE FIX: Was .Latest
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
                consumer.Subscribe(DOUGH_MACHINE_READY_TOPIC);
                _logger.LogInformation("Consumer subscribed. Ready to process signals.");

                // Inner loop for consuming messages
                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);

                    if (consumeResult?.Message != null)
                    {
                        _logger.LogInformation("<-- [Dough Machine Ready] signal received. Requesting next pizza.");
                    
                        // Tell the OrderService to send the next pizza.
                        _orderService.SendNextPizza();
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

// --- Data Model ---
public class Pizza
{
    public int Id { get; set; }
    public string Size { get; set; } = "medium";
    public bool IsBaked { get; set; }
    public string Sauce { get; set; } = "tomato";
    public List<string> Meat { get; set; } = [];
    public List<string> Cheese { get; set; } = [];
    public List<string> Vegetable { get; set; } = [];
}