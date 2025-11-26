using System.Collections.Concurrent;
using DoughShaper.Data;

namespace DoughShaper;

// --- Shared State ---
public class DoughShaperState
{
    public BlockingCollection<PizzaOrderMessage> PizzaQueue { get; } = new BlockingCollection<PizzaOrderMessage>();
    public AutoResetEvent IsSauceReady { get; } = new AutoResetEvent(true); // Start ready for first pizza
}