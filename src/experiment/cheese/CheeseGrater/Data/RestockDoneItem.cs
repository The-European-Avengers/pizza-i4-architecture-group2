using System.Text.Json.Serialization;

namespace CheeseGrater.Data;

public class RestockDoneItem
{
    [JsonPropertyName("itemType")]
    public string ItemType { get; set; } = "";
    
    [JsonPropertyName("deliveredAmount")]
    public int DeliveredAmount { get; set; }
}