using System.Text.Json.Serialization;

namespace OrderProcessing.Data;

public class OrderStackMessage
{
    [JsonPropertyName("OrderId")]
    public string OrderId { get; set; } = "";
    
    [JsonPropertyName("pizzas")]
    public Dictionary<string, int> Pizzas { get; set; } = new();
    
    [JsonPropertyName("isBaked")]
    public bool IsBaked { get; set; }
    [JsonPropertyName("startTimestamp")] 
    public long StartTimestamp { get; set; }
    
}