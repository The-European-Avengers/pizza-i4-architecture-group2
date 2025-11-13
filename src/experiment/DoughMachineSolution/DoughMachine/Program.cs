using Confluent.Kafka;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// --- Background Service (Kafka Consumer/Processor) ---
builder.Services.AddHostedService<DoughMachineProcessor>();

var app = builder.Build();
app.MapGet("/", () => "Dough Machine Service is running.");
app.Run();

// --- Service ---

public class DoughMachineProcessor : BackgroundService
{
    // --- Kafka Topics ---
    private const string CONSUME_TOPIC = "dough-machine-topic";
    private const string SHAPER_TOPIC = "dough-shaper-topic";
    private const string READY_TOPIC = "dough-machine-ready-topic";

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
        
                consumer.Subscribe(CONSUME_TOPIC); // <-- FIX: Corrected typo from CONSUME_TOPCiC
                _logger.LogInformation("Consumer subscribed. Ready to process pizzas.");

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                    if (consumeResult?.Message == null) continue;

                    // --- FIX: Added the missing deserialization line ---
                    var pizza = JsonSerializer.Deserialize<Pizza>(consumeResult.Message.Value);
                    _logger.LogInformation("Processing Pizza {Id}...", pizza?.Id);

                    // --- Simulate work (e.g., latency) ---
                    await Task.Delay(1000, stoppingToken); // 1 second processing time
                    _logger.LogInformation("...Finished Pizza {Id}.", pizza?.Id); // This line will work now

                    // 1. Send to Dough Shaper
                    var shaperMessage = new Message<Null, string> { Value = consumeResult.Message.Value };
                    await producer.ProduceAsync(SHAPER_TOPIC, shaperMessage, stoppingToken);
                
                    // 2. Send "ready" signal back to Order Processor
                    // --- FIX: Added the missing "ready" message ---
                    var readyMessage = new Message<Null, string> { Value = "ready" };
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