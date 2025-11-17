using Confluent.Kafka;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// --- Shared State ---
builder.Services.AddSingleton<CheeseGraterState>();

// --- Register background services ---
builder.Services.AddHostedService<PizzaConsumerService>();
builder.Services.AddHostedService<MeatSignalConsumerService>();
builder.Services.AddHostedService<ProcessingService>();

var app = builder.Build();
app.MapGet("/", () => "Cheese Grater Service is running (v2 - Backpressure).");
app.Run();

// --- Shared State ---
public class CheeseGraterState
{
    // C# equivalent of Java's BlockingQueue
    public BlockingCollection<PizzaOrderMessage> PizzaQueue { get; } = new BlockingCollection<PizzaOrderMessage>();
    
    // C# equivalent of Java's AtomicBoolean + wait/notify
    public AutoResetEvent IsMeatMachineReady { get; } = new AutoResetEvent(true); // Start ready for first pizza
}

// --- Service 1: Consumes pizzas from SauceMachine ---
public class PizzaConsumerService : BackgroundService
{
    private const string CONSUME_TOPIC = "cheese-machine";
    private readonly ILogger<PizzaConsumerService> _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly CheeseGraterState _state;

    public PizzaConsumerService(CheeseGraterState state, IConfiguration config, ILogger<PizzaConsumerService> logger)
    {
        _state = state;
        _logger = logger;
        var kafkaBootstrapServers = config["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092";
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "cheese-grater-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Pizza Consumer (1/3) running. Waiting for Kafka...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
                consumer.Subscribe(CONSUME_TOPIC);
                _logger.LogInformation("Pizza Consumer (1/3) subscribed to {Topic}. Ready for pizzas.", CONSUME_TOPIC);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                    if (consumeResult?.Message == null) continue;

                    var pizza = JsonSerializer.Deserialize<PizzaOrderMessage>(consumeResult.Message.Value);
                    if (pizza != null)
                    {
                        _logger.LogInformation("--> Pizza {PizzaId} (Order: {OrderId}) received from SauceMachine. Adding to queue.", pizza.PizzaId, pizza.OrderId);
                        _state.PizzaQueue.Add(pizza, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Pizza Consumer (1/3) stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pizza Consumer (1/3) error. Retrying in 5s.");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}

// --- Service 2: Consumes "done" signals from Meat Machine ---
public class MeatSignalConsumerService : BackgroundService
{
    private const string CONSUME_TOPIC = "meat-machine-done";
    private readonly ILogger<MeatSignalConsumerService> _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly CheeseGraterState _state;

    public MeatSignalConsumerService(CheeseGraterState state, IConfiguration config, ILogger<MeatSignalConsumerService> logger)
    {
        _state = state;
        _logger = logger;
        var kafkaBootstrapServers = config["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092";
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "cheese-grater-signal-group",
            AutoOffsetReset = AutoOffsetReset.Latest
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Meat Signal Consumer (2/3) running. Waiting for Kafka...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
                consumer.Subscribe(CONSUME_TOPIC);
                _logger.LogInformation("Meat Signal Consumer (2/3) subscribed to {Topic}. Ready for signals.", CONSUME_TOPIC);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                    if (consumeResult?.Message == null) continue;

                    var doneMessage = JsonSerializer.Deserialize<PizzaDoneMessage>(consumeResult.Message.Value);
                    if (doneMessage != null)
                    {
                        _logger.LogInformation("<-- [Meat Machine Ready] signal received for Pizza {PizzaId} (Order: {OrderId}). Unlocking processor.", doneMessage.PizzaId, doneMessage.OrderId);
                        _state.IsMeatMachineReady.Set();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Meat Signal Consumer (2/3) stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Meat Signal Consumer (2/3) error. Retrying in 5s.");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}

// --- Service 3: Main Processing Logic ---
public class ProcessingService : BackgroundService
{
    private const string NEXT_TOPIC = "meat-machine";
    private const string DONE_TOPIC = "cheese-machine-done";

    private readonly ILogger<ProcessingService> _logger;
    private readonly ProducerConfig _producerConfig;
    private readonly CheeseGraterState _state;

    public ProcessingService(CheeseGraterState state, IConfiguration config, ILogger<ProcessingService> logger)
    {
        _state = state;
        _logger = logger;
        var kafkaBootstrapServers = config["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092";
        _producerConfig = new ProducerConfig { BootstrapServers = kafkaBootstrapServers };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Main Processor (3/3) running. Waiting for work...");

        using var producer = new ProducerBuilder<string, string>(_producerConfig).Build();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Step 1: Wait for a pizza
                _logger.LogInformation("Main Processor (3/3) waiting for a pizza from SauceMachine...");
                var pizza = await Task.Run(() => _state.PizzaQueue.Take(stoppingToken), stoppingToken);
                
                // Step 2: Wait for Meat Machine signal 
                _logger.LogInformation("Main Processor (3/3) waiting for Meat Machine to be ready (Pizza {PizzaId})...", pizza.PizzaId);
                await Task.Run(() => _state.IsMeatMachineReady.WaitOne(), stoppingToken);

                if (pizza.PizzaId == 1)
                {
                    _logger.LogInformation("First pizza in order - consumed initial 'ready' signal.");
                }
                
                

                
                // Step 3: Process the pizza
                _logger.LogInformation("Grating cheese for Pizza {PizzaId} (Order: {OrderId})...", pizza.PizzaId, pizza.OrderId);
                await Task.Delay(750, stoppingToken); // 750ms processing time from Java service
                pizza.MsgDesc = "Cheese grated";
                _logger.LogInformation("...Finished Pizza {PizzaId}. Sending to {Topic}", pizza.PizzaId, NEXT_TOPIC);

                // Step 4: Send pizza to Meat Machine
                var nextMessage = new Message<string, string>
                {
                    Key = pizza.OrderId.ToString(),
                    Value = JsonSerializer.Serialize(pizza)
                };
                await producer.ProduceAsync(NEXT_TOPIC, nextMessage, stoppingToken);

                // Step 5: Send "done" signal back to SauceMachine
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
            _logger.LogInformation("Main Processor (3/3) stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Main Processor (3/3) critical error.");
        }
    }
}

// --- Data Models (from your example) ---
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