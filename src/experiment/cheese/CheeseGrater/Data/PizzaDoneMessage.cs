using System.Text.Json.Serialization;

namespace CheeseGrater.Data;

public class PizzaDoneMessage
{
    [JsonPropertyName("pizzaId")]
    public int PizzaId { get; set; }
    
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; }
    
    [JsonPropertyName("doneMsg")]
    public bool DoneMsg { get; set; } = true;
}