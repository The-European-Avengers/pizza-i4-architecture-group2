namespace OrderProcessing.Data;

public class PizzaRecipe
{
    public string Name { get; set; } = "";
    public string Sauce { get; set; } = "";
    public List<string> Cheese { get; set; } = new();
    public List<string> Meat { get; set; } = new();
    public List<string> Veggies { get; set; } = new();
}