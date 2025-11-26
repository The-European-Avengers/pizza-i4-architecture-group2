using DoughMachine;

var builder = WebApplication.CreateBuilder(args);

// --- Shared State for the services ---
builder.Services.AddSingleton<DoughMachineState>();

// --- Register all background services ---
builder.Services.AddHostedService<PizzaConsumerService>();
builder.Services.AddHostedService<ShaperSignalConsumerService>();
builder.Services.AddHostedService<RestockDoneConsumerService>();
builder.Services.AddHostedService<ProcessingService>();

var app = builder.Build();
app.MapGet("/", () => "Dough Machine Service is running (v4 - With Restocking).");
app.Run();