using System.Globalization;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Importer.Import;

public sealed class SourceRecipeImporter
{
    private static readonly string[] ExpectedRecipeHeader =
    [
        "Nazwa potrawy",
        "Składnik",
        "Ilość",
        "Kcal",
        "B",
        "Wykonanie",
        "Typ posiłku",
        "Deser Tak/Nie",
        "Nabiał"
    ];

    private static readonly string[] ExpectedCategoryHeader =
    [
        "Składnik",
        "Kategoria"
    ];

    public async Task<MealImportResult> ImportAsync(
        TextReader recipesReader,
        TextReader categoriesReader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(recipesReader);
        ArgumentNullException.ThrowIfNull(categoriesReader);

        var ingredientCategories = await ReadIngredientCategoriesAsync(categoriesReader, cancellationToken);
        var recipeRows = await ReadRecipeRowsAsync(recipesReader, ingredientCategories, cancellationToken);

        var meals = recipeRows
            .GroupBy(row => new MealKey(row.Name, row.Type, row.IsDessert, row.Kcal, row.Protein, row.Description))
            .Select(group => new ImportedMeal(
                group.Key.Name,
                group.Key.Type,
                group.Key.IsDessert,
                group.Key.Kcal,
                group.Key.Protein,
                group.Key.Description,
                group.GroupBy(row => new MealIngredientKey(row.IngredientName, row.Unit, row.Category))
                    .Select(ingredientGroup => new ImportedMealIngredient(
                        ingredientGroup.Key.Name,
                        ingredientGroup.Sum(row => row.Quantity),
                        ingredientGroup.Key.Unit,
                        ingredientGroup.Key.Category))
                    .ToList()))
            .ToList();

        var ingredients = recipeRows
            .GroupBy(row => new IngredientKey(row.IngredientName, row.Unit))
            .Select(group => new ImportedIngredient(group.Key.Name, group.Key.Unit))
            .ToList();

        return new MealImportResult(meals, ingredients);
    }

    private static async Task<Dictionary<string, string>> ReadIngredientCategoriesAsync(
        TextReader reader,
        CancellationToken cancellationToken)
    {
        var header = await CsvRecordReader.ReadRecordAsync(reader, cancellationToken)
            ?? throw new FormatException("Ingredient category CSV is empty.");
        ValidateHeader(header, ExpectedCategoryHeader, "ingredient category");

        var categories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var rowNumber = 2;

        while (true)
        {
            var columns = await CsvRecordReader.ReadRecordAsync(reader, cancellationToken);
            if (columns is null)
            {
                break;
            }

            if (columns.Count == 1 && string.IsNullOrWhiteSpace(columns[0]))
            {
                rowNumber++;
                continue;
            }

            if (columns.Count != ExpectedCategoryHeader.Length)
            {
                throw new FormatException(
                    $"Invalid ingredient category CSV row {rowNumber}. Expected {ExpectedCategoryHeader.Length} columns but found {columns.Count}.");
            }

            var ingredientName = GetRequiredValue(columns, rowNumber, 0, ExpectedCategoryHeader);
            var category = GetRequiredValue(columns, rowNumber, 1, ExpectedCategoryHeader);
            categories[ingredientName] = category;
            rowNumber++;
        }

        return categories;
    }

    private static async Task<List<SourceRecipeRow>> ReadRecipeRowsAsync(
        TextReader reader,
        IReadOnlyDictionary<string, string> ingredientCategories,
        CancellationToken cancellationToken)
    {
        var header = await CsvRecordReader.ReadRecordAsync(reader, cancellationToken)
            ?? throw new FormatException("Recipe CSV is empty.");
        ValidateHeader(header, ExpectedRecipeHeader, "recipe");

        var rows = new List<SourceRecipeRow>();
        RecipeContext? currentMeal = null;
        var rowNumber = 2;

        while (true)
        {
            var columns = await CsvRecordReader.ReadRecordAsync(reader, cancellationToken);
            if (columns is null)
            {
                break;
            }

            if (columns.Count == 1 && string.IsNullOrWhiteSpace(columns[0]))
            {
                rowNumber++;
                continue;
            }

            if (columns.Count != ExpectedRecipeHeader.Length)
            {
                throw new FormatException(
                    $"Invalid recipe CSV row {rowNumber}. Expected {ExpectedRecipeHeader.Length} columns but found {columns.Count}.");
            }

            if (!string.IsNullOrWhiteSpace(columns[0]))
            {
                currentMeal = new RecipeContext(
                    GetRequiredValue(columns, rowNumber, 0, ExpectedRecipeHeader),
                    ParseMealType(GetRequiredValue(columns, rowNumber, 6, ExpectedRecipeHeader), rowNumber),
                    ParseBoolean(GetRequiredValue(columns, rowNumber, 7, ExpectedRecipeHeader), rowNumber, ExpectedRecipeHeader[7]),
                    ParseInt(GetRequiredValue(columns, rowNumber, 3, ExpectedRecipeHeader), rowNumber, ExpectedRecipeHeader[3]),
                    ParseInt(GetRequiredValue(columns, rowNumber, 4, ExpectedRecipeHeader), rowNumber, ExpectedRecipeHeader[4]),
                    GetRequiredValue(columns, rowNumber, 5, ExpectedRecipeHeader));
            }

            if (currentMeal is null)
            {
                throw new FormatException($"Invalid recipe CSV row {rowNumber}. The first row for a meal must contain 'Nazwa potrawy'.");
            }

            var ingredientName = GetRequiredValue(columns, rowNumber, 1, ExpectedRecipeHeader);
            var (quantity, unit) = ParseQuantity(GetRequiredValue(columns, rowNumber, 2, ExpectedRecipeHeader), rowNumber);
            var category = ingredientCategories.TryGetValue(ingredientName, out var classifiedCategory)
                ? classifiedCategory
                : "Inne";

            rows.Add(new SourceRecipeRow(
                currentMeal.Name,
                currentMeal.Type,
                currentMeal.IsDessert,
                currentMeal.Kcal,
                currentMeal.Protein,
                currentMeal.Description,
                ingredientName,
                quantity,
                unit,
                category));

            rowNumber++;
        }

        return rows;
    }

    private static void ValidateHeader(IReadOnlyList<string> actual, IReadOnlyList<string> expected, string fileLabel)
    {
        if (actual.Count != expected.Count)
        {
            throw new FormatException(
                $"Invalid {fileLabel} CSV header. Expected {expected.Count} columns but found {actual.Count}: {string.Join(", ", actual)}");
        }

        for (var i = 0; i < expected.Count; i++)
        {
            if (!string.Equals(actual[i], expected[i], StringComparison.Ordinal))
            {
                throw new FormatException(
                    $"Invalid {fileLabel} CSV header. Expected column {i + 1} to be '{expected[i]}' but found '{actual[i]}'.");
            }
        }
    }

    private static string GetRequiredValue(IReadOnlyList<string> columns, int rowNumber, int columnIndex, IReadOnlyList<string> header)
    {
        var value = columns[columnIndex];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException($"Invalid CSV row {rowNumber}, column '{header[columnIndex]}': value is required.");
        }

        return value.Trim();
    }

    private static MealType ParseMealType(string value, int rowNumber)
        => value.Trim() switch
        {
            "Śniadanie" => MealType.Breakfast,
            "Obiad" => MealType.Lunch,
            _ => throw new FormatException($"Invalid CSV row {rowNumber}, column 'Typ posiłku', value '{value}'.")
        };

    private static bool ParseBoolean(string value, int rowNumber, string columnName)
        => value.Trim().ToLowerInvariant() switch
        {
            "tak" => true,
            "nie" => false,
            _ => throw new FormatException($"Invalid CSV row {rowNumber}, column '{columnName}', value '{value}'.")
        };

    private static int ParseInt(string value, int rowNumber, string columnName)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        var normalizedValue = value.Replace(',', '.');
        if (decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
        {
            return (int)Math.Round(decimalValue, MidpointRounding.AwayFromZero);
        }

        throw new FormatException($"Invalid CSV row {rowNumber}, column '{columnName}', value '{value}'.");
    }

    private static (decimal Quantity, string Unit) ParseQuantity(string value, int rowNumber)
    {
        var normalizedValue = value.Trim();
        var separatorIndex = normalizedValue.IndexOf(' ');
        if (separatorIndex > 0 && separatorIndex < normalizedValue.Length - 1)
        {
            var quantityText = normalizedValue[..separatorIndex];
            var unit = normalizedValue[(separatorIndex + 1)..].Trim();

            if (decimal.TryParse(quantityText, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity)
                && string.Equals(unit, "g", StringComparison.OrdinalIgnoreCase))
            {
                return (quantity, "g");
            }

            if (decimal.TryParse(quantityText, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            {
                return (0m, "g");
            }
        }

        if (!char.IsDigit(normalizedValue[0]))
        {
            return (0m, "g");
        }

        throw new FormatException($"Invalid CSV row {rowNumber}, column 'Ilość', value '{value}'.");
    }

    private sealed record RecipeContext(string Name, MealType Type, bool IsDessert, int Kcal, int Protein, string Description);

    private sealed record SourceRecipeRow(
        string Name,
        MealType Type,
        bool IsDessert,
        int Kcal,
        int Protein,
        string Description,
        string IngredientName,
        decimal Quantity,
        string Unit,
        string Category);

    private sealed record MealKey(string Name, MealType Type, bool IsDessert, int Kcal, int Protein, string Description);
    private sealed record MealIngredientKey(string Name, string Unit, string Category);
    private sealed record IngredientKey(string Name, string Unit);
}
