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
        { "mozzarella", 32 },
        { "cheddar", 32 },
        { "smoked provolone", 32 },
        { "feta", 32 },
        { "provolone", 32 },
        { "parmesan", 32 },
        { "gorgonzola", 32 },
        { "jalapeño jack", 32 }
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
                        RequestedAmount = 50 - kvp.Value
                    });
                }
                // Opportunistic restock if <= 20 (only if we're already restocking)
                else if (kvp.Value <= 20 && needs.Count > 0)
                {
                    needs.Add(new RestockItem
                    {
                        ItemType = kvp.Key,
                        CurrentStock = kvp.Value,
                        RequestedAmount = 50 - kvp.Value
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

    public bool ShouldRequestRestock(List<string> requiredCheeses)
    {
        if (IsRestockInProgress) return false;
    
        // Check if any required cheese is critically low (≤10)
        foreach (var cheese in requiredCheeses)
        {
            if (GetCheeseStock(cheese) <= 10)
                return true;
        }
    
        // Or if any cheese at all is at or below 20%
        foreach (var cheese in _cheeseStock.Keys)
        {
            if (_cheeseStock[cheese] <= 20)
                return true;
        }
    
        return false;
    }

    public bool HasRequiredCheese(List<string> requiredCheeses)
    {
        foreach (var cheese in requiredCheeses)
        {
            if (GetCheeseStock(cheese) <= 0)
                return false;
        }
        return true;
    }

    public List<string> GetAllCheeses()
    {
        lock (_stockLock)
        {
            return new List<string>(_cheeseStock.Keys);
        }
    }
    
}