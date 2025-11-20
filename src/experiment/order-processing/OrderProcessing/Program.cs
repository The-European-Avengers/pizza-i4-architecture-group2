using Confluent.Kafka;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";

// --- Shared State ---
builder.Services.AddSingleton<OrderState>();
builder.Services.AddSingleton<RecipeRepository>();

// --- Register Background Services ---
builder.Services.AddSingleton<KafkaProducerService>(sp => 
    new KafkaProducerService(kafkaBootstrapServers, sp.GetRequiredService<ILogger<KafkaProducerService>>()));

// Service 1: Listens for incoming orders from order-stack
builder.Services.AddHostedService<OrderStackConsumer>();
// Service 2: Listens for "done" signals from DoughMachine
builder.Services.AddHostedService<DoughMachineSignalConsumer>();
// Service 3: The main processing loop that sends pizzas
builder.Services.AddHostedService<OrderProcessingService>();

var app = builder.Build();

app.MapGet("/", () => "Order Processing Service is running.");

app.Run();

// --- Recipe Repository ---
public class RecipeRepository
{
    private readonly Dictionary<string, PizzaRecipe> _recipes;
    private readonly ILogger<RecipeRepository> _logger;

    public RecipeRepository(ILogger<RecipeRepository> logger)
    {
        _logger = logger;
        _recipes = new Dictionary<string, PizzaRecipe>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "Margherita",
                new PizzaRecipe
                {
                    Name = "Margherita",
                    Sauce = "tomato",
                    Cheese = new List<string> { "mozzarella" },
                    Meat = new List<string>(),
                    Veggies = new List<string> { "basil" }
                }
            },
            {
                "Pepperoni Classic",
                new PizzaRecipe
                {
                    Name = "Pepperoni Classic",
                    Sauce = "tomato",
                    Cheese = new List<string> { "mozzarella" },
                    Meat = new List<string> { "pepperoni" },
                    Veggies = new List<string>()
                }
            },
            {
                "Supreme Deluxe",
                new PizzaRecipe
                {
                    Name = "Supreme Deluxe",
                    Sauce = "tomato",
                    Cheese = new List<string> { "mozzarella", "cheddar" },
                    Meat = new List<string> { "pepperoni", "sausage", "ham" },
                    Veggies = new List<string> { "mushroom", "onion", "green pepper", "black olive" }
                }
            },
            {
                "BBQ Chicken Ranch",
                new PizzaRecipe
                {
                    Name = "BBQ Chicken Ranch",
                    Sauce = "BBQ Sauce",
                    Cheese = new List<string> { "mozzarella", "smoked provolone" },
                    Meat = new List<string> { "grilled chicken" },
                    Veggies = new List<string> { "red onion" }
                }
            },
            {
                "Vegetarian Pesto",
                new PizzaRecipe
                {
                    Name = "Vegetarian Pesto",
                    Sauce = "Pesto",
                    Cheese = new List<string> { "mozzarella", "feta" },
                    Meat = new List<string>(),
                    Veggies = new List<string> { "spinach", "sun-dried tomato", "artichoke heart" }
                }
            },
            {
                "Four Cheese (Quattro Formaggi)",
                new PizzaRecipe
                {
                    Name = "Four Cheese (Quattro Formaggi)",
                    Sauce = "Olive Oil",
                    Cheese = new List<string> { "mozzarella", "provolone", "parmesan", "gorgonzola" },
                    Meat = new List<string>(),
                    Veggies = new List<string>()
                }
            },
            {
                "Hawaiian Delight",
                new PizzaRecipe
                {
                    Name = "Hawaiian Delight",
                    Sauce = "tomato",
                    Cheese = new List<string> { "mozzarella" },
                    Meat = new List<string> { "ham", "bacon" },
                    Veggies = new List<string> { "pineapple" }
                }
            },
            {
                "Spicy Sriracha Beef",
                new PizzaRecipe
                {
                    Name = "Spicy Sriracha Beef",
                    Sauce = "Sriracha-Tomato Blend",
                    Cheese = new List<string> { "mozzarella", "jalapeño jack" },
                    Meat = new List<string> { "ground beef" },
                    Veggies = new List<string> { "jalapeño", "red bell pepper" }
                }
            },
            {
                "Truffle Mushroom",
                new PizzaRecipe
                {
                    Name = "Truffle Mushroom",
                    Sauce = "White Garlic Cream",
                    Cheese = new List<string> { "mozzarella" },
                    Meat = new List<string>(),
                    Veggies = new List<string> { "mushroom", "truffle" }
                }
            },
            {
                "Breakfast Pizza",
                new PizzaRecipe
                {
                    Name = "Breakfast Pizza",
                    Sauce = "Hollandaise Sauce",
                    Cheese = new List<string> { "mozzarella", "cheddar" },
                    Meat = new List<string> { "sausage", "bacon" },
                    Veggies = new List<string> { "scrambled egg", "chives" }
                }
            }
        };

        _logger.LogInformation("Loaded {Count} pizza recipes", _recipes.Count);
    }

    public PizzaRecipe? GetRecipe(string pizzaName)
    {
        return _recipes.TryGetValue(pizzaName, out var recipe) ? recipe : null;
    }
}

// --- Shared State ---
public class OrderState
{
    public BlockingCollection<PizzaOrderMessage> PizzaQueue { get; } = new();
    public AutoResetEvent IsDoughMachineReady { get; } = new(true);
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

// --- Service 1: Listens for incoming orders from order-stack ---
public class OrderStackConsumer : BackgroundService
{
    private readonly OrderState _state;
    private readonly RecipeRepository _recipeRepo;
    private readonly ILogger<OrderStackConsumer> _logger;
    private readonly ConsumerConfig _consumerConfig;
    private const string ORDER_STACK_TOPIC = "order-stack";

    public OrderStackConsumer(
        OrderState state, 
        RecipeRepository recipeRepo,
        IConfiguration config, 
        ILogger<OrderStackConsumer> logger)
    {
        _state = state;
        _recipeRepo = recipeRepo;
        _logger = logger;
        var kafkaBootstrapServers = config["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092";
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "order-processor-stack-group",
            AutoOffsetReset = AutoOffsetReset.Latest
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order Stack Consumer running. Waiting for orders from order-stack topic...");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
                consumer.Subscribe(ORDER_STACK_TOPIC);
                _logger.LogInformation("Order Stack Consumer subscribed to {Topic}", ORDER_STACK_TOPIC);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                    
                    if (consumeResult?.Message == null) continue;

                    try
                    {
                        var orderMessage = JsonSerializer.Deserialize<OrderStackMessage>(consumeResult.Message.Value);
                        if (orderMessage == null)
                        {
                            _logger.LogWarning("Failed to deserialize order message");
                            continue;
                        }

                        _logger.LogInformation("📦 Received order {OrderId} with {Count} pizza types", 
                            orderMessage.OrderId, orderMessage.Pizzas.Count);

                        // Convert string OrderId to int (for now)
                        int orderId = orderMessage.OrderId.GetHashCode();
                        int totalPizzas = orderMessage.Pizzas.Sum(p => p.Value);
                        int pizzaCounter = 1;

                        // Create pizza objects based on recipes
                        foreach (var pizzaEntry in orderMessage.Pizzas)
                        {
                            var recipe = _recipeRepo.GetRecipe(pizzaEntry.Key);
                            if (recipe == null)
                            {
                                _logger.LogWarning("Unknown pizza type: {PizzaType}. Skipping.", pizzaEntry.Key);
                                continue;
                            }

                            // Create the specified quantity of this pizza type
                            for (int i = 0; i < pizzaEntry.Value; i++)
                            {
                                var pizza = new PizzaOrderMessage
                                {
                                    PizzaId = pizzaCounter++,
                                    OrderId = orderId,
                                    OrderSize = totalPizzas,
                                    StartTimestamp = null,
                                    MsgDesc = "Order received",
                                    Sauce = recipe.Sauce,
                                    Baked = orderMessage.IsBaked,
                                    Cheese = new List<string>(recipe.Cheese),
                                    Meat = new List<string>(recipe.Meat),
                                    Veggies = new List<string>(recipe.Veggies)
                                };

                                _state.PizzaQueue.Add(pizza, stoppingToken);
                                _logger.LogInformation("  ➕ Added {PizzaType} (Pizza {PizzaId}/{Total})", 
                                    recipe.Name, pizza.PizzaId, totalPizzas);
                            }
                        }

                        _logger.LogInformation("✅ Order {OrderId} queued: {Total} pizzas total", 
                            orderMessage.OrderId, totalPizzas);
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "Failed to deserialize order: {Message}", consumeResult.Message.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing order");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Order Stack Consumer stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Order Stack Consumer error. Retrying in 5s.");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}

// --- Service 2: Listens for "done" signals ---
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
            GroupId = "order-processor-group-main",
            AutoOffsetReset = AutoOffsetReset.Latest
        };
    }

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
                        _state.IsDoughMachineReady.Set();
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

// --- Service 3: Main Processing Loop (sends pizzas) ---
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

                // 3. Set production start timestamp
                long productionStartTime = _orderStartTimes.GetOrAdd(pizza.OrderId, (orderId) => {
                    long newTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    _logger.LogInformation("🎉 Production starting for Order {OrderId} at {Timestamp}!", 
                        orderId, newTimestamp);
                    return newTimestamp;
                });
                
                pizza.StartTimestamp = productionStartTime;

                // 4. Send order-processing message for first pizza
                if (pizza.PizzaId == 1)
                {
                    _logger.LogInformation("First pizza in order - consumed initial 'ready' signal.");

                    var orderProcessingMessage = new OrderProcessingMessage
                    {
                        OrderId = pizza.OrderId,
                        OrderSize = pizza.OrderSize,
                        StartTimestamp = productionStartTime
                    };
                    _ = _producer.ProduceOrderProcessingMessage(orderProcessingMessage);
                }

                // 5. Send the pizza
                _logger.LogInformation("--> Sending Pizza {PizzaId} (Order: {OrderId}). Remaining in queue: {Count}",
                    pizza.PizzaId, pizza.OrderId, _state.PizzaQueue.Count);
                
                await _producer.ProducePizzaMessage(pizza);
                _producer.Flush();
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
public class PizzaRecipe
{
    public string Name { get; set; } = "";
    public string Sauce { get; set; } = "";
    public List<string> Cheese { get; set; } = new();
    public List<string> Meat { get; set; } = new();
    public List<string> Veggies { get; set; } = new();
}

public class OrderStackMessage
{
    [JsonPropertyName("orderID")]
    public string OrderId { get; set; } = "";
    
    [JsonPropertyName("pizzas")]
    public Dictionary<string, int> Pizzas { get; set; } = new();
    
    [JsonPropertyName("isBaked")]
    public bool IsBaked { get; set; }
    [JsonPropertyName("startTimestamp")] 
    public long StartTimestamp { get; set; }
    
}

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