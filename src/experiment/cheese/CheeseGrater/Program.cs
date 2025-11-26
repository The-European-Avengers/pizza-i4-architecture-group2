using CheeseGrater;
using CheeseGrater.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Shared State ---
builder.Services.AddSingleton<CheeseGraterState>();

// --- Register background services ---
builder.Services.AddHostedService<PizzaConsumerService>();
builder.Services.AddHostedService<MeatSignalConsumerService>();
builder.Services.AddHostedService<RestockDoneConsumerService>();
builder.Services.AddHostedService<ProcessingService>();

var app = builder.Build();
app.MapGet("/", () => "Cheese Grater Service is running (v3 - With Restocking).");
app.Run();


