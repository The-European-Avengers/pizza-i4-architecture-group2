using System.Text.Json.Serialization;

namespace CheeseGrater.Data;

public class RestockItem
{
    [JsonPropertyName("itemType")]
    public string ItemType { get; set; } = "";
    
    [JsonPropertyName("currentStock")]
    public int CurrentStock { get; set; }
    
    [JsonPropertyName("requestedAmount")]
    public int RequestedAmount { get; set; }
}