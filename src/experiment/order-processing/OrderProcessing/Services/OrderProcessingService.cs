using System.Collections.Concurrent;
using OrderProcessing.Data;

namespace OrderProcessing.Services;

// --- Service 3: Main Processing Loop (sends pizzas) ---
public class OrderProcessingService : BackgroundService
{
    private readonly OrderState _state;
    private readonly KafkaProducerService _producer;
    private readonly ILogger<OrderProcessingService> _logger;

    private readonly ConcurrentDictionary<string, long> _orderStartTimes = new();

    public OrderProcessingService(OrderState state, KafkaProducerService producer, ILogger<OrderProcessingService> logger)
    {
        _state = state;
        _producer = producer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Main Order Processing Service running. Waiting for pizzas in queue...");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // 1. Wait for a pizza to be in our queue
                var pizza = await Task.Run(() => _state.PizzaQueue.Take(stoppingToken), stoppingToken);

                // 2. Wait for the Dough Machine to be ready
                _logger.LogInformation("Waiting for Dough Machine to be ready before sending Pizza {PizzaId} (Order: {OrderId})...",
                    pizza.PizzaId, pizza.OrderId);
                await Task.Run(() => _state.IsDoughMachineReady.WaitOne(), stoppingToken);

                // 3. Set production start timestamp
                long productionStartTime = _orderStartTimes.GetOrAdd(pizza.OrderId, (orderId) => {
                    long newTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    _logger.LogInformation("🎉 Production starting for Order {OrderId} at {Timestamp}!", 
                        orderId, newTimestamp);
                    return newTimestamp;
                });
                
                pizza.StartTimestamp = productionStartTime;

                // 4. Send order-processing message for first pizza
                if (pizza.PizzaId == 1)
                {
                    _logger.LogInformation("First pizza in order - consumed initial 'ready' signal.");

                    var orderProcessingMessage = new OrderProcessingMessage
                    {
                        OrderId = pizza.OrderId,
                        OrderSize = pizza.OrderSize,
                        StartTimestamp = productionStartTime
                    };
                    _ = _producer.ProduceOrderProcessingMessage(orderProcessingMessage);
                }

                // 5. Send the pizza
                _logger.LogInformation("--> Sending Pizza {PizzaId} (Order: {OrderId}). Remaining in queue: {Count}",
                    pizza.PizzaId, pizza.OrderId, _state.PizzaQueue.Count);
                
                await _producer.ProducePizzaMessage(pizza);
                _producer.Flush();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Main processing service stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Main processing service critical error.");
        }
    }
}