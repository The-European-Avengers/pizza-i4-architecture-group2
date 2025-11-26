using System.Text.Json.Serialization;

namespace DoughMachine.Data;

public class RestockRequestMessage
{
    [JsonPropertyName("machineId")]
    public string MachineId { get; set; } = "";
    
    [JsonPropertyName("items")]
    public List<RestockItem> Items { get; set; } = [];
    
    [JsonPropertyName("requestTimestamp")]
    public long RequestTimestamp { get; set; }
}