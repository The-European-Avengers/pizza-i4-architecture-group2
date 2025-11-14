using Confluent.Kafka;
using System.Text.Json;
using System.Text.Json.Serialization; // Added for JSON property names

var builder = WebApplication.CreateBuilder(args);

// --- Background Service (Kafka Consumer/Processor) ---
builder.Services.AddHostedService<DoughMachineProcessor>();

var app = builder.Build();
app.MapGet("/", () => "Dough Machine Service is running.");
app.Run();

// --- Service ---

public class DoughMachineProcessor : BackgroundService
{
    // --- Kafka Topics (Updated) ---
    private const string CONSUME_TOPIC = "dough-machine";
    private const string SHAPER_TOPIC = "dough-shaper";
    private const string READY_TOPIC = "dough-machine-done";

    private readonly ILogger<DoughMachineProcessor> _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly ProducerConfig _producerConfig;

    public DoughMachineProcessor(ILogger<DoughMachineProcessor> logger)
    {
        _logger = logger;
        var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";

        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "dough-machine-group", // All instances share the work
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        
        _producerConfig = new ProducerConfig 
        { 
            BootstrapServers = kafkaBootstrapServers 
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dough Machine processor running. Waiting for Kafka...");

        // Retry loop to wait for Kafka to be ready
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<Ignore, string>(_consumerConfig).Build();
                using var producer = new ProducerBuilder<Null, string>(_producerConfig).Build();
        
                consumer.Subscribe(CONSUME_TOPIC);
                _logger.LogInformation("Consumer subscribed to {Topic}. Ready to process pizzas.", CONSUME_TOPIC);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                    if (consumeResult?.Message == null) continue;

                    var pizza = JsonSerializer.Deserialize<PizzaOrderMessage>(consumeResult.Message.Value);
                    if (pizza == null) continue;
                    
                    _logger.LogInformation("Processing Pizza {Id} (Order: {OrderId})...", pizza.PizzaId, pizza.OrderId);

                    // --- Simulate work (e.g., latency) ---
                    await Task.Delay(1000, stoppingToken); // 1 second processing time
                    
                    // --- Update Pizza Message ---
                    pizza.MsgDesc = "Dough prepared";
                    _logger.LogInformation("...Finished Pizza {Id} (Order: {OrderId}).", pizza.PizzaId, pizza.OrderId);

                    // 1. Send updated PizzaOrderMessage to Dough Shaper
                    var shaperMessage = new Message<Null, string> { Value = JsonSerializer.Serialize(pizza) };
                    await producer.ProduceAsync(SHAPER_TOPIC, shaperMessage, stoppingToken);
                
                    // 2. Send "PizzaDoneMessage" back to Order Processor
                    var readyMessage = new Message<Null, string> 
                    { 
                        Value = JsonSerializer.Serialize(new PizzaDoneMessage 
                        { 
                            PizzaId = pizza.PizzaId, // Added this
                            OrderId = pizza.OrderId    // Renamed from Id
                        }) 
                    };
                    await producer.ProduceAsync(READY_TOPIC, readyMessage, stoppingToken);
                
                    producer.Flush(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Dough Machine stopping (OperationCanceled).");
                break; // Exit loop on cancellation
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka error. Retrying in 5 seconds... (Kafka might be starting or topic not ready)");
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
    public bool Baked { get; set; }
    [JsonPropertyName("cheese")]
    public List<string> Cheese { get; set; } = [];
    [JsonPropertyName("meat")]
    public List<string> Meat { get; set; } = [];
    [JsonPropertyName("veggies")]
    public List<string> Veggies { get; set; } = [];
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