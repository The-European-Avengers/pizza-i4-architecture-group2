using System.Text.Json.Serialization;

namespace OrderProcessing.Data;

public class OrderProcessingMessage
{
    [JsonPropertyName("orderId")] 
    public string OrderId { get; set; }
    
    [JsonPropertyName("orderSize")] 
    public int OrderSize { get; set; }
    
    [JsonPropertyName("startTimestamp")] 
    public long StartTimestamp { get; set; }
}