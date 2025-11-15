using Confluent.Kafka;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";

// --- Singleton Service for Order Logic ---
builder.Services.AddSingleton<OrderService>(sp => 
    new OrderService(kafkaBootstrapServers, sp.GetRequiredService<ILogger<OrderService>>()));

// --- Background Service (Kafka Consumer for dough-machine-done signals) ---
builder.Services.AddHostedService<KafkaConsumerService>();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// --- API Endpoints ---
app.MapPost("/start-order/{count:int}", async (int count, OrderService orderService) =>
{
    if (count <= 0)
    {
        return Results.BadRequest("Order count must be greater than 0.");
    }
    
    app.Logger.LogInformation("🚀 Order received for {Count} pizzas! Starting production line...", count);
    
    // Start the order (this runs in the background, not awaited)
    _ = orderService.StartOrder(count);
    
    return Results.Ok($"Order started for {count} pizzas.");
});

app.MapPost("/start-order/10", async (OrderService orderService) =>
{
    app.Logger.LogInformation("🚀 Order received for 10 pizzas! Starting production line...");
    _ = orderService.StartOrder(10);
    return Results.Ok("Order started for 10 pizzas.");
});

app.MapPost("/start-order/50", async (OrderService orderService) =>
{
    app.Logger.LogInformation("🚀 Order received for 50 pizzas! Starting production line...");
    _ = orderService.StartOrder(50);
    return Results.Ok("Order started for 50 pizzas.");
});

app.MapPost("/start-order/100", async (OrderService orderService) =>
{
    app.Logger.LogInformation("🚀 Order received for 100 pizzas! Starting production line...");
    _ = orderService.StartOrder(100);
    return Results.Ok("Order started for 100 pizzas.");
});

app.Run();

// --- Services ---

public class OrderService
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<OrderService> _logger;
    private readonly ConcurrentQueue<PizzaOrderMessage> _pizzaQueue = new();
    private readonly AutoResetEvent _doughMachineReady = new(true); // Start "ready" for first pizza
    private static readonly Random Rng = new Random();
    
    private volatile int _currentOrderId = -1;
    private volatile int _pizzasRemainingInOrder = 0;

    // --- Kafka Topics ---
    private const string ORDER_PROCESSING_TOPIC = "order-processing";
    private const string DOUGH_MACHINE_TOPIC = "dough-machine";

    public OrderService(string bootstrapServers, ILogger<OrderService> logger)
    {
        _logger = logger;
        var producerConfig = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }
    
    public int GetCurrentOrderId() => _currentOrderId;

    public async Task StartOrder(int pizzaCount)
    {
        int orderId = Rng.Next(100, 1000);
        Interlocked.Exchange(ref _currentOrderId, orderId);
        Interlocked.Exchange(ref _pizzasRemainingInOrder, pizzaCount);
        
        long startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        try
        {
            // 1. Send the single "OrderProcessing" message
            var orderProcessingMessage = new OrderProcessingMessage
            {
                OrderId = orderId,
                StartTimestamp = startTimestamp
            };
            
            await _producer.ProduceAsync(ORDER_PROCESSING_TOPIC,
                new Message<string, string>
                {
                    Key = orderId.ToString(), 
                    Value = JsonSerializer.Serialize(orderProcessingMessage)
                });

            _logger.LogInformation("Loading {PizzaCount} pizzas into the queue for Order ID {OrderId}...", pizzaCount, orderId);

            // 2. Load all pizzas into the local queue
            for (int i = 1; i <= pizzaCount; i++)
            {
                _pizzaQueue.Enqueue(new PizzaOrderMessage
                {
                    PizzaId = i,
                    OrderId = orderId,
                    OrderSize = pizzaCount,
                    StartTimestamp = startTimestamp,
                    EndTimestamp = null,
                    MsgDesc = "Order received",
                    Sauce = "tomato",
                    Baked = (i % 2 == 0), 
                    Cheese = ["mozzarella"],
                    Meat = ["salami"],
                    Veggies = ["peppers"]
                });
            }

            // 3. Reset the "ready" signal to true for the first pizza
            _doughMachineReady.Set();

            // 4. Send the *first* pizza immediately (no waiting)
            SendNextPizza();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting order {OrderId}", orderId);
            Interlocked.Exchange(ref _currentOrderId, -1);
        }
    }

    public async void SendNextPizza()
    {
        if (_pizzaQueue.TryDequeue(out var pizza))
        {
            try
            {
                // For pizzas after the first one, wait for the "ready" signal
                if (pizza.PizzaId > 1)
                {
                    _logger.LogInformation("Waiting for Dough Machine to be ready before sending Pizza {PizzaId}...", pizza.PizzaId);
                    _doughMachineReady.WaitOne(); // Block until we get the signal
                }

                _logger.LogInformation("--> Sending Pizza {PizzaId} (Order: {OrderId}). Remaining in queue: {Count}", 
                    pizza.PizzaId, pizza.OrderId, _pizzaQueue.Count);

                var pizzaMessage = new Message<string, string>
                {
                    Key = pizza.OrderId.ToString(),
                    Value = JsonSerializer.Serialize(pizza)
                };
                
                await _producer.ProduceAsync(DOUGH_MACHINE_TOPIC, pizzaMessage);
                _producer.Flush();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send pizza {PizzaId}", pizza.PizzaId);
            }
        }
        else
        {
            // Queue is empty - check if order is complete
            if (Interlocked.Decrement(ref _pizzasRemainingInOrder) < 0)
            {
                // Already finished, just ignore
                return;
            }

            _logger.LogInformation("✅✅✅ Order {OrderId} Complete! All pizzas sent. ✅✅✅", GetCurrentOrderId());
            Interlocked.Exchange(ref _currentOrderId, -1);
        }
    }

    public void OnDoughMachineReady()
    {
        _doughMachineReady.Set(); // Signal that we can send the next pizza
    }

    public void Dispose()
    {
        _producer.Flush();
        _producer.Dispose();
    }
}

public class KafkaConsumerService : BackgroundService
{
    private readonly OrderService _orderService;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly ConsumerConfig _consumerConfig;

    private const string READY_TOPIC = "dough-machine-done";

    public KafkaConsumerService(OrderService orderService, ILogger<KafkaConsumerService> logger)
    {
        _orderService = orderService;
        _logger = logger;
        
        var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "order-processor-group-main",
            AutoOffsetReset = AutoOffsetReset.Latest 
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka Consumer Service running. Waiting for dough-machine-done signals...");
        
        using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
        consumer.Subscribe(READY_TOPIC);
        _logger.LogInformation("Consumer subscribed to {Topic}. Ready to process signals.", READY_TOPIC);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                if (consumeResult?.Message == null) continue;

                try
                {
                    var doneMessage = JsonSerializer.Deserialize<PizzaDoneMessage>(consumeResult.Message.Value);
                    if (doneMessage == null)
                    {
                        _logger.LogWarning("Failed to deserialize 'done' message. Skipping.");
                        continue;
                    }
                    
                    // Filter for the current order
                    int currentOrderId = _orderService.GetCurrentOrderId();
                    if (currentOrderId != -1 && doneMessage.OrderId == currentOrderId)
                    {
                        _logger.LogInformation("<-- [Dough Machine Ready] signal received for Pizza {PizzaId} (Order: {OrderId}).", 
                            doneMessage.PizzaId, doneMessage.OrderId);
                        
                        _orderService.OnDoughMachineReady();
                        _orderService.SendNextPizza();
                    }
                    else if (currentOrderId != -1)
                    {
                        _logger.LogWarning("Ignoring stale 'done' signal for Pizza {PizzaId} (Order: {OrderId}). Current order is {CurrentOrderId}.", 
                            doneMessage.PizzaId, doneMessage.OrderId, currentOrderId);
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
        finally
        {
            consumer.Close();
        }
    }
}

// --- Data Models ---

public class OrderProcessingMessage
{
    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }
    
    [JsonPropertyName("startTimestamp")]
    public long StartTimestamp { get; set; }
}

public class PizzaDoneMessage
{
    [JsonPropertyName("pizzaId")]
    public int PizzaId { get; set; }

    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }
    
    [JsonPropertyName("doneMsg")]
    public bool DoneMsg { get; set; }
}

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
    public string MsgDesc { get; set; } = "";

    [JsonPropertyName("sauce")]
    public string Sauce { get; set; } = "";

    [JsonPropertyName("baked")]
    public bool Baked { get; set; }

    [JsonPropertyName("cheese")]
    public List<string> Cheese { get; set; } = [];

    [JsonPropertyName("meat")]
    public List<string> Meat { get; set; } = [];

    [JsonPropertyName("veggies")]
    public List<string> Veggies { get; set; } = [];
}