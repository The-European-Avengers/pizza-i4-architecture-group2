using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;

var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});
var logger = loggerFactory.CreateLogger<Program>();

// --- All topics defined in your markdown ---
var topicsToCreate = new[]
{
    "dough-machine", "dough-machine-done",
    "dough-shaper", "dough-shaper-done",
    "sauce-machine", "sauce-machine-done",
    "cheese-machine", "cheese-machine-done",
    "meat-machine", "meat-machine-done",
    "vegetables-machine", "vegetables-machine-done",
    "freezer-machine", "freezer-machine-done",
    "oven-machine", "oven-machine-done",
    "packaging-machine", "packaging-machine-done",
    "order-stack", "order-processing", "order-done", "pizza-done",
    "order-dispatch"
    // Restock topics
    "dough-machine-restock", "dough-machine-restock-done",
    "sauce-machine-restock", "sauce-machine-restock-done",
    "cheese-machine-restock", "cheese-machine-restock-done",
    "meat-machine-restock", "meat-machine-restock-done",
    "vegetables-machine-restock", "vegetables-machine-restock-done"
};

var adminConfig = new AdminClientConfig { BootstrapServers = kafkaBootstrapServers };

logger.LogInformation("Kafka Initializer starting...");
logger.LogInformation("Target Kafka: {Servers}", kafkaBootstrapServers);

// --- Retry Logic ---
int maxRetries = 10;
int delay = 5000; // 5 seconds

for (int i = 1; i <= maxRetries; i++)
{
    try
    {
        using (var adminClient = new AdminClientBuilder(adminConfig).Build())
        {
            logger.LogInformation("Attempting to connect to Kafka (Attempt {Attempt}/{Max})...", i, maxRetries);
            // Simple operation to test connection
            adminClient.GetMetadata(TimeSpan.FromSeconds(5)); 
            logger.LogInformation("✅ Successfully connected to Kafka.");

            // Create topics
            var topicSpecifications = topicsToCreate
                .Select(name => new TopicSpecification { Name = name, NumPartitions = 1, ReplicationFactor = 1 })
                .ToList();

            try
            {
                await adminClient.CreateTopicsAsync(topicSpecifications);
                foreach (var topic in topicsToCreate)
                {
                    logger.LogInformation("Topic '{Topic}' ensured.", topic);
                }
            }
            catch (CreateTopicsException e)
            {
                // This is OK. It just means some (or all) of the topics already exist.
                foreach (var result in e.Results)
                {
                    if (result.Error.Code == ErrorCode.TopicAlreadyExists)
                    {
                        logger.LogWarning("Topic '{Topic}' already exists. Skipping.", result.Topic);
                    }
                    else
                    {
                        logger.LogError("Failed to create topic '{Topic}': {Reason}", result.Topic, result.Error.Reason);
                    }
                }
            }

            logger.LogInformation("✅ All topics are ready.");
            logger.LogInformation("Kafka Initializer finished successfully.");
            return 0; // Success
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to connect to Kafka: {Message}. Retrying in {Delay}ms...", ex.Message, delay);
        await Task.Delay(delay);
    }
}

logger.LogCritical("Failed to connect to Kafka after {MaxRetries} attempts. Exiting.", maxRetries);
return 1; // Failure