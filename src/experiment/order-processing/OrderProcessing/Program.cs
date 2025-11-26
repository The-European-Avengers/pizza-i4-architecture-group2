using OrderProcessing;
using OrderProcessing.Data;
using OrderProcessing.Services;

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
