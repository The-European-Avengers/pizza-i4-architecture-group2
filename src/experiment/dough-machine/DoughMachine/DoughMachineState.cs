using DoughMachine.Data;

namespace DoughMachine;
using System.Collections.Concurrent;

// --- Shared State ---
public class DoughMachineState
{
    // A thread-safe queue to hold pizzas from the OrderProcessor
    public BlockingCollection<PizzaOrderMessage> PizzaQueue { get; } = new BlockingCollection<PizzaOrderMessage>();

    // A signal to indicate that the DoughShaper is ready
    public AutoResetEvent IsShaperReady { get; } = new AutoResetEvent(true);

    // Stock management
    public int DoughStock { get; set; } = 11; // Start with 11 units
    public bool IsRestockInProgress { get; set; } = false;
    private readonly object _stockLock = new object();

    public bool TryUseDough()
    {
        lock (_stockLock)
        {
            if (DoughStock > 0)
            {
                DoughStock--;
                return true;
            }
            return false;
        }
    }

    public int GetCurrentStock()
    {
        lock (_stockLock)
        {
            return DoughStock;
        }
    }

    public void AddStock(int amount)
    {
        lock (_stockLock)
        {
            DoughStock += amount;
        }
    }

    public bool ShouldRequestRestock()
    {
        lock (_stockLock)
        {
            return DoughStock <= 10 && !IsRestockInProgress;
        }
    }
}