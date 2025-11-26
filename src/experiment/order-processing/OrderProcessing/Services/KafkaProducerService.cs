using System.Text.Json;
using Confluent.Kafka;
using OrderProcessing.Data;

namespace OrderProcessing.Services;

// --- Kafka Producer (as a dedicated service) ---
public class KafkaProducerService : IDisposable
{
    private const string ORDER_PROCESSING_TOPIC = "order-processing";
    private const string DOUGH_MACHINE_TOPIC = "dough-machine";
    private readonly IProducer<string, string> _producer;

    public KafkaProducerService(string bootstrapServers, ILogger<KafkaProducerService> logger)
    {
        var producerConfig = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task ProduceOrderProcessingMessage(OrderProcessingMessage message)
    {
        var kafkaMessage = new Message<string, string>
        {
            Key = message.OrderId.ToString(),
            Value = JsonSerializer.Serialize(message)
        };
        await _producer.ProduceAsync(ORDER_PROCESSING_TOPIC, kafkaMessage);
    }
    
    public async Task ProducePizzaMessage(PizzaOrderMessage pizza)
    {
        var pizzaMessage = new Message<string, string>
        {
            Key = pizza.OrderId.ToString(),
            Value = JsonSerializer.Serialize(pizza)
        };
        await _producer.ProduceAsync(DOUGH_MACHINE_TOPIC, pizzaMessage);
    }
    
    public void Flush() => _producer.Flush();
    public void Dispose()
    {
        _producer.Flush();
        _producer.Dispose();
    }
}