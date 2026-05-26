using DietPlanner.Domain.Enums;

namespace DietPlanner.Importer.Import;

public sealed record MealCsvRecord(
    string Name,
    MealType Type,
    bool IsDessert,
    int Kcal,
    int Protein,
    string IngredientName,
    decimal Quantity,
    string Unit,
    string Category);
