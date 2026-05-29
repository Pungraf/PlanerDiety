using System.Globalization;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Importer.Import;

public sealed class MealImporter
{
    private static readonly string[] ExpectedHeader =
    [
        "Name",
        "Type",
        "IsDessert",
        "Kcal",
        "Protein",
        "IngredientName",
        "Quantity",
        "Unit",
        "Category"
    ];

    public async Task<MealImportResult> ImportAsync(TextReader reader, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var header = await CsvRecordReader.ReadRecordAsync(reader, cancellationToken);
        if (header is null)
        {
            return MealImportResult.Empty;
        }

        ValidateHeader(header);

        var rows = new List<MealCsvRecord>();
        var rowNumber = 2;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

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

            rows.Add(ParseRow(columns, rowNumber));
            rowNumber++;
        }

        var meals = rows
            .GroupBy(row => new MealKey(row.Name, row.Type, row.IsDessert, row.Kcal, row.Protein))
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

    private static void ValidateHeader(IReadOnlyList<string> header)
    {
        if (header.Count != ExpectedHeader.Length)
        {
            throw new FormatException(
                $"Invalid CSV header. Expected {ExpectedHeader.Length} columns but found {header.Count}: {string.Join(", ", header)}");
        }

        for (var i = 0; i < ExpectedHeader.Length; i++)
        {
            if (!string.Equals(header[i], ExpectedHeader[i], StringComparison.Ordinal))
            {
                throw new FormatException(
                    $"Invalid CSV header. Expected column {i + 1} to be '{ExpectedHeader[i]}' but found '{header[i]}'.");
            }
        }
    }

    private static MealCsvRecord ParseRow(IReadOnlyList<string> columns, int rowNumber)
    {
        if (columns.Count != ExpectedHeader.Length)
        {
            throw new FormatException(
                $"Invalid CSV row {rowNumber}. Expected {ExpectedHeader.Length} columns but found {columns.Count}.");
        }

        return new MealCsvRecord(
            GetRequiredValue(columns, rowNumber, 0),
            ParseValue(columns, rowNumber, 1, ParseMealType),
            ParseValue(columns, rowNumber, 2, static value => bool.Parse(value)),
            ParseValue(columns, rowNumber, 3, static value => int.Parse(value, CultureInfo.InvariantCulture)),
            ParseValue(columns, rowNumber, 4, static value => int.Parse(value, CultureInfo.InvariantCulture)),
            GetRequiredValue(columns, rowNumber, 5),
            ParseValue(columns, rowNumber, 6, static value => decimal.Parse(value, CultureInfo.InvariantCulture)),
            GetRequiredValue(columns, rowNumber, 7),
            GetRequiredValue(columns, rowNumber, 8));
    }

    private static MealType ParseMealType(string value)
    {
        if (Enum.TryParse<MealType>(value, ignoreCase: true, out var mealType))
        {
            return mealType;
        }

        throw new FormatException($"Unsupported meal type '{value}'.");
    }

    private static string GetRequiredValue(IReadOnlyList<string> columns, int rowNumber, int columnIndex)
    {
        var value = columns[columnIndex];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException(
                $"Invalid CSV row {rowNumber}, column '{ExpectedHeader[columnIndex]}': value is required.");
        }

        return value;
    }

    private static T ParseValue<T>(IReadOnlyList<string> columns, int rowNumber, int columnIndex, Func<string, T> parser)
    {
        var value = GetRequiredValue(columns, rowNumber, columnIndex);

        try
        {
            return parser(value);
        }
        catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentException)
        {
            throw new FormatException(
                $"Invalid CSV row {rowNumber}, column '{ExpectedHeader[columnIndex]}', value '{value}'.",
                exception);
        }
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
