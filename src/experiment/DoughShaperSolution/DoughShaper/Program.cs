using Confluent.Kafka;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// --- Background Service (Kafka Consumer) ---
builder.Services.AddHostedService<DoughShaperConsumer>();

var app = builder.Build();
app.MapGet("/", () => "Dough Shaper Service is running.");
app.Run();

// --- Service ---

public class DoughShaperConsumer : BackgroundService
{
    // --- Kafka Topic ---
    private const string CONSUME_TOPIC = "dough-shaper-topic";

    private readonly ILogger<DoughShaperConsumer> _logger;
    private readonly ConsumerConfig _consumerConfig;

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
                consumer.Subscribe(CONSUME_TOPIC);
                _logger.LogInformation("Consumer subscribed. Ready to receive from dough machine.");

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                    if (consumeResult?.Message == null) continue;

                    var pizza = JsonSerializer.Deserialize<Pizza>(consumeResult.Message.Value);
                    _logger.LogInformation("✅ Pizza {Id} received by Dough Shaper.", pizza?.Id);
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