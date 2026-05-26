namespace DietPlanner.Importer.Import;

public sealed record MealCsvRecord(
    string Name,
    string Type,
    bool IsDessert,
    int Kcal,
    int Protein,
    string IngredientName,
    decimal Quantity,
    string Unit,
    string Category);
