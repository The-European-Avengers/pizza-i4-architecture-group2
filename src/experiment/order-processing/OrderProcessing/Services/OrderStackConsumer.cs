using System.Text.Json;
using Confluent.Kafka;
using OrderProcessing.Data;

namespace OrderProcessing.Services;

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

                        
                        string orderId = orderMessage.OrderId;
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