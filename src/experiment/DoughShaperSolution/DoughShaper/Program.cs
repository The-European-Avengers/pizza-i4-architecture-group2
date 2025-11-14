using Confluent.Kafka;
using System.Text.Json;
using System.Text.Json.Serialization; // Added for JSON property names

var builder = WebApplication.CreateBuilder(args);

// --- Background Service (Kafka Consumer) ---
builder.Services.AddHostedService<DoughShaperConsumer>();

var app = builder.Build();
app.MapGet("/", () => "Dough Shaper Service is running.");
app.Run();

// --- Service ---

public class DoughShaperConsumer : BackgroundService
{
    // --- Kafka Topics ---
    private const string CONSUME_TOPIC = "dough-shaper";
    //private const string NEXT_TOPIC = "sauce-machine"; // Next step from markdown
    private const string NEXT_TOPIC = "cheese-machine";
    private const string DONE_TOPIC = "dough-shaper-done"; // This machine's done topic

    private readonly ILogger<DoughShaperConsumer> _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly ProducerConfig _producerConfig; // Added producer

    public DoughShaperConsumer(ILogger<DoughShaperConsumer> logger)
    {
        _logger = logger;
        var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";

        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "dough-shaper-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        
        // Added producer config
        _producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaBootstrapServers
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dough Shaper consumer running. Waiting for Kafka...");

        // Retry loop to wait for Kafka to be ready
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<Ignore, string>(_consumerConfig).Build();
                using var producer = new ProducerBuilder<Null, string>(_producerConfig).Build(); // Added producer
                
                consumer.Subscribe(CONSUME_TOPIC);
                _logger.LogInformation("Consumer subscribed to {Topic}. Ready to receive from dough machine.", CONSUME_TOPIC);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                    if (consumeResult?.Message == null) continue;

                    var pizza = JsonSerializer.Deserialize<PizzaOrderMessage>(consumeResult.Message.Value);
                    if (pizza == null) continue;

                    _logger.LogInformation("✅ Pizza {Id} received by Dough Shaper. Processing...", pizza.PizzaId);

                    // --- Simulate work ---
                    await Task.Delay(500, stoppingToken); // 0.5 second processing time
                    pizza.MsgDesc = "Dough shaped";
                    
                    // 1. Send updated PizzaOrderMessage to Sauce Machine
                    var nextMessage = new Message<Null, string> { Value = JsonSerializer.Serialize(pizza) };
                    await producer.ProduceAsync(NEXT_TOPIC, nextMessage, stoppingToken);
                    
                    // 2. Send "PizzaDoneMessage" to our done topic
                    var doneMessage = new Message<Null, string>
                    {
                        Value = JsonSerializer.Serialize(new PizzaDoneMessage 
                        { 
                            PizzaId = pizza.PizzaId, // Added this
                            OrderId = pizza.OrderId    // Renamed from Id
                        })
                    };
                    await producer.ProduceAsync(DONE_TOPIC, doneMessage, stoppingToken);

                    producer.Flush(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Dough Shaper stopping (OperationCanceled).");
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