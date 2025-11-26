using System.Collections.Concurrent;
using OrderProcessing.Data;

namespace OrderProcessing;

// --- Shared State ---
public class OrderState
{
    public BlockingCollection<PizzaOrderMessage> PizzaQueue { get; } = new();
    public AutoResetEvent IsDoughMachineReady { get; } = new(true);
}