using DoughShaper;
using DoughShaper.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Shared State ---
builder.Services.AddSingleton<DoughShaperState>();

// --- Register background services ---
builder.Services.AddHostedService<PizzaConsumerService>();
builder.Services.AddHostedService<SauceSignalConsumerService>();
builder.Services.AddHostedService<ProcessingService>();

var app = builder.Build();
app.MapGet("/", () => "Dough Shaper Service is running (v2 - Backpressure).");
app.Run();
