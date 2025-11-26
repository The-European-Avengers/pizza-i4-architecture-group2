using System.Text.Json.Serialization;

namespace CheeseGrater.Data;

public class PizzaOrderMessage
{
    [JsonPropertyName("pizzaId")]
    public int PizzaId { get; set; }
    
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; }
    
    [JsonPropertyName("orderSize")]
    public int OrderSize { get; set; }
    
    [JsonPropertyName("startTimestamp")]
    public long? StartTimestamp { get; set; }
    
    [JsonPropertyName("endTimestamp")]
    public long? EndTimestamp { get; set; }
    
    [JsonPropertyName("msgDesc")]
    public string? MsgDesc { get; set; }
    
    [JsonPropertyName("sauce")]
    public string? Sauce { get; set; }
    
    [JsonPropertyName("baked")]
    public bool Baked { get; set; }
    
    [JsonPropertyName("cheese")]
    public List<string> Cheese { get; set; } = [];
    
    [JsonPropertyName("meat")]
    public List<string> Meat { get; set; } = [];
    
    [JsonPropertyName("veggies")]
    public List<string> Veggies { get; set; } = [];
}