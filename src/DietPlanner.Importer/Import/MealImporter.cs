using System.Globalization;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Importer.Import;

public sealed class MealImporter
{
    public async Task<MealImportResult> ImportAsync(TextReader reader, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return MealImportResult.Empty;
        }

        var rows = new List<MealCsvRecord>();
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            rows.Add(ParseRow(line));
        }

        var meals = rows
            .GroupBy(row => new MealKey(row.Name, ParseMealType(row.Type), row.IsDessert, row.Kcal, row.Protein))
            .Select(group => new ImportedMeal(
                group.Key.Name,
                group.Key.Type,
                group.Key.IsDessert,
                group.Key.Kcal,
                group.Key.Protein,
                group.Select(row => new ImportedMealIngredient(
                    row.IngredientName,
                    row.Quantity,
                    row.Unit,
                    row.Category))
                .ToList()))
            .ToList();

        var ingredients = rows
            .GroupBy(row => new IngredientKey(row.IngredientName, row.Unit))
            .Select(group => new ImportedIngredient(group.Key.Name, group.Key.Unit))
            .ToList();

        return new MealImportResult(meals, ingredients);
    }

    private static MealCsvRecord ParseRow(string line)
    {
        var columns = SplitCsvLine(line);
        if (columns.Count != 9)
        {
            throw new FormatException("Each CSV row must contain exactly 9 columns.");
        }

        return new MealCsvRecord(
            columns[0],
            columns[1],
            bool.Parse(columns[2]),
            int.Parse(columns[3], CultureInfo.InvariantCulture),
            int.Parse(columns[4], CultureInfo.InvariantCulture),
            columns[5],
            decimal.Parse(columns[6], CultureInfo.InvariantCulture),
            columns[7],
            columns[8]);
    }

    private static MealType ParseMealType(string value)
    {
        if (Enum.TryParse<MealType>(value, ignoreCase: true, out var mealType))
        {
            return mealType;
        }

        throw new FormatException($"Unsupported meal type '{value}'.");
    }

    private static List<string> SplitCsvLine(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var character in line)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        values.Add(current.ToString().Trim());
        return values;
    }

    private sealed record MealKey(string Name, MealType Type, bool IsDessert, int Kcal, int Protein);

    private sealed record IngredientKey(string Name, string Unit);
}

public sealed record MealImportResult(
    IReadOnlyList<ImportedMeal> Meals,
    IReadOnlyList<ImportedIngredient> Ingredients)
{
    public static MealImportResult Empty { get; } = new([], []);
}

public sealed record ImportedMeal(
    string Name,
    MealType Type,
    bool IsDessert,
    int Kcal,
    int Protein,
    IReadOnlyList<ImportedMealIngredient> Ingredients);

public sealed record ImportedMealIngredient(
    string IngredientName,
    decimal Quantity,
    string Unit,
    string Category);

public sealed record ImportedIngredient(
    string Name,
    string Unit);
