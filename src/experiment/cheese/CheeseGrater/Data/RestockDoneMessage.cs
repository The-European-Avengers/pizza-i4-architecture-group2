using System.Text.Json.Serialization;

namespace CheeseGrater.Data;

public class RestockDoneMessage
{
    [JsonPropertyName("machineId")]
    public string MachineId { get; set; } = "";
    
    [JsonPropertyName("items")]
    public List<RestockDoneItem> Items { get; set; } = [];
    
    [JsonPropertyName("completedTimestamp")]
    public long CompletedTimestamp { get; set; }
}