using System.ComponentModel.DataAnnotations.Schema;

namespace SmartRx.Domain;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}

public class Drug
{
    public int Id { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public List<string> Ingredients { get; set; } = new();

    [NotMapped]
    public string IngredientsCsv
    {
        get => Ingredients != null ? string.Join(", ", Ingredients) : "";
        set => Ingredients = !string.IsNullOrWhiteSpace(value)
            ? value.Split(',').Select(i => i.Trim()).ToList()
            : new List<string>();
    }

    public string DosageInstruction { get; set; } = string.Empty;
    public DateTime ManufacturedDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal Price { get; set; }
}
