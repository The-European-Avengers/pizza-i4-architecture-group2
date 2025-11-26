using System.Collections.Concurrent;
using CheeseGrater.Data;

namespace CheeseGrater;

// --- Shared State ---
public class CheeseGraterState
{
    public BlockingCollection<PizzaOrderMessage> PizzaQueue { get; } = new BlockingCollection<PizzaOrderMessage>();
    public AutoResetEvent IsMeatMachineReady { get; } = new AutoResetEvent(true);

    // Cheese stock - each type starts with 11 units
    private readonly Dictionary<string, int> _cheeseStock = new Dictionary<string, int>
    {
        { "mozzarella", 11 },
        { "cheddar", 11 },
        { "smoked provolone", 11 },
        { "feta", 11 },
        { "provolone", 11 },
        { "parmesan", 11 },
        { "gorgonzola", 11 },
        { "jalapeño jack", 11 }
    };

    public bool IsRestockInProgress { get; set; } = false;
    private readonly object _stockLock = new object();

    public bool TryUseCheese(string cheeseType)
    {
        lock (_stockLock)
        {
            if (_cheeseStock.TryGetValue(cheeseType, out int stock) && stock > 0)
            {
                _cheeseStock[cheeseType]--;
                return true;
            }
            return false;
        }
    }

    public int GetCheeseStock(string cheeseType)
    {
        lock (_stockLock)
        {
            return _cheeseStock.TryGetValue(cheeseType, out int stock) ? stock : 0;
        }
    }

    public void AddCheeseStock(string cheeseType, int amount)
    {
        lock (_stockLock)
        {
            if (_cheeseStock.ContainsKey(cheeseType))
            {
                _cheeseStock[cheeseType] += amount;
            }
        }
    }

    public List<RestockItem> GetRestockNeeds()
    {
        lock (_stockLock)
        {
            var needs = new List<RestockItem>();

            foreach (var kvp in _cheeseStock)
            {
                // Must restock if <= 10
                if (kvp.Value <= 10)
                {
                    needs.Add(new RestockItem
                    {
                        ItemType = kvp.Key,
                        CurrentStock = kvp.Value,
                        RequestedAmount = 100 - kvp.Value
                    });
                }
                // Opportunistic restock if <= 20 (only if we're already restocking)
                else if (kvp.Value <= 20 && needs.Count > 0)
                {
                    needs.Add(new RestockItem
                    {
                        ItemType = kvp.Key,
                        CurrentStock = kvp.Value,
                        RequestedAmount = 100 - kvp.Value
                    });
                }
            }

            return needs;
        }
    }

    public bool ShouldRequestRestock()
    {
        lock (_stockLock)
        {
            if (IsRestockInProgress) return false;
            return _cheeseStock.Any(kvp => kvp.Value <= 10);
        }
    }

    public Dictionary<string, int> GetAllStockLevels()
    {
        lock (_stockLock)
        {
            return new Dictionary<string, int>(_cheeseStock);
        }
    }
}