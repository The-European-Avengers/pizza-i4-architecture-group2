namespace OrderProcessing.Data;

// --- Recipe Repository ---
public class RecipeRepository
{
    private readonly Dictionary<string, PizzaRecipe> _recipes;
    private readonly ILogger<RecipeRepository> _logger;

    public RecipeRepository(ILogger<RecipeRepository> logger)
    {
        _logger = logger;
        _recipes = new Dictionary<string, PizzaRecipe>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "Margherita",
                new PizzaRecipe
                {
                    Name = "Margherita",
                    Sauce = "tomato",
                    Cheese = new List<string> { "mozzarella" },
                    Meat = new List<string>(),
                    Veggies = new List<string> { "basil" }
                }
            },
            {
                "Pepperoni Classic",
                new PizzaRecipe
                {
                    Name = "Pepperoni Classic",
                    Sauce = "tomato",
                    Cheese = new List<string> { "mozzarella" },
                    Meat = new List<string> { "pepperoni" },
                    Veggies = new List<string>()
                }
            },
            {
                "Supreme Deluxe",
                new PizzaRecipe
                {
                    Name = "Supreme Deluxe",
                    Sauce = "tomato",
                    Cheese = new List<string> { "mozzarella", "cheddar" },
                    Meat = new List<string> { "pepperoni", "sausage", "ham" },
                    Veggies = new List<string> { "mushroom", "onion", "green pepper", "black olive" }
                }
            },
            {
                "BBQ Chicken Ranch",
                new PizzaRecipe
                {
                    Name = "BBQ Chicken Ranch",
                    Sauce = "BBQ Sauce",
                    Cheese = new List<string> { "mozzarella", "smoked provolone" },
                    Meat = new List<string> { "grilled chicken" },
                    Veggies = new List<string> { "red onion" }
                }
            },
            {
                "Vegetarian Pesto",
                new PizzaRecipe
                {
                    Name = "Vegetarian Pesto",
                    Sauce = "Pesto",
                    Cheese = new List<string> { "mozzarella", "feta" },
                    Meat = new List<string>(),
                    Veggies = new List<string> { "spinach", "sun-dried tomato", "artichoke heart" }
                }
            },
            {
                "Four Cheese (Quattro Formaggi)",
                new PizzaRecipe
                {
                    Name = "Four Cheese (Quattro Formaggi)",
                    Sauce = "Olive Oil",
                    Cheese = new List<string> { "mozzarella", "provolone", "parmesan", "gorgonzola" },
                    Meat = new List<string>(),
                    Veggies = new List<string>()
                }
            },
            {
                "Hawaiian Delight",
                new PizzaRecipe
                {
                    Name = "Hawaiian Delight",
                    Sauce = "tomato",
                    Cheese = new List<string> { "mozzarella" },
                    Meat = new List<string> { "ham", "bacon" },
                    Veggies = new List<string> { "pineapple" }
                }
            },
            {
                "Spicy Sriracha Beef",
                new PizzaRecipe
                {
                    Name = "Spicy Sriracha Beef",
                    Sauce = "Sriracha-Tomato Blend",
                    Cheese = new List<string> { "mozzarella", "jalapeño jack" },
                    Meat = new List<string> { "ground beef" },
                    Veggies = new List<string> { "jalapeño", "red bell pepper" }
                }
            },
            {
                "Truffle Mushroom",
                new PizzaRecipe
                {
                    Name = "Truffle Mushroom",
                    Sauce = "White Garlic Cream",
                    Cheese = new List<string> { "mozzarella" },
                    Meat = new List<string>(),
                    Veggies = new List<string> { "mushroom", "truffle" }
                }
            },
            {
                "Breakfast Pizza",
                new PizzaRecipe
                {
                    Name = "Breakfast Pizza",
                    Sauce = "Hollandaise Sauce",
                    Cheese = new List<string> { "mozzarella", "cheddar" },
                    Meat = new List<string> { "sausage", "bacon" },
                    Veggies = new List<string> { "scrambled egg", "chives" }
                }
            }
        };

        _logger.LogInformation("Loaded {Count} pizza recipes", _recipes.Count);
    }

    public PizzaRecipe? GetRecipe(string pizzaName)
    {
        return _recipes.TryGetValue(pizzaName, out var recipe) ? recipe : null;
    }
}