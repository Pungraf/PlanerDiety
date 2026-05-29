using DietPlanner.Domain.Enums;
using DietPlanner.Importer.Import;
using DietPlanner.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Api.Tests.Importer;

public class MealImporterTests
{
    [Fact]
    public async Task Program_ShouldPersistMealsFromPolishSourceCsvPairToSqliteDatabase()
    {
        var recipesCsv = """
Nazwa potrawy,Składnik,Ilość,Kcal,B,Wykonanie,Typ posiłku,Deser Tak/Nie,Nabiał
Bajgiel z domowym twarożkiem,Bajgiel,75 g,410,33,"Opis",Śniadanie,NIe,Tak
,Ser twarogowy półtłusty,125 g,,,,,,
""";
        var categoriesCsv = """
Składnik,Kategoria
Bajgiel,Pieczywo
Ser twarogowy półtłusty,Nabiał
""";
        var recipesPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-recipes.csv");
        var categoriesPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-categories.csv");
        var databaseName = Guid.NewGuid().ToString("N");
        var connectionString = $"Data Source={databaseName};Mode=Memory;Cache=Shared";

        await File.WriteAllTextAsync(recipesPath, recipesCsv);
        await File.WriteAllTextAsync(categoriesPath, categoriesCsv);

        try
        {
            await using var keepAliveConnection = new SqliteConnection(connectionString);
            await keepAliveConnection.OpenAsync();

            await DietPlanner.Importer.Program.Main([recipesPath, categoriesPath, connectionString]);

            var options = new DbContextOptionsBuilder<DietPlannerDbContext>()
                .UseSqlite(connectionString)
                .Options;

            await using var dbContext = new DietPlannerDbContext(options);
            await SqliteSchemaBootstrapper.InitializeAsync(dbContext);

            var meal = await dbContext.Meals
                .Include(x => x.Ingredients)
                .SingleAsync();

            meal.Name.Should().Be("Bajgiel z domowym twarożkiem");
            meal.Type.Should().Be(MealType.Breakfast);
            meal.Ingredients.Should().HaveCount(2);

            dbContext.MealIngredients.Should().Contain(x => x.ShoppingCategory == "Pieczywo");
            dbContext.MealIngredients.Should().Contain(x => x.ShoppingCategory == "Nabiał");
        }
        finally
        {
            File.Delete(recipesPath);
            File.Delete(categoriesPath);
        }
    }

    [Fact]
    public async Task Program_ShouldPersistMealsIngredientsAndLinksToSqliteDatabase()
    {
        var csv = """
Name,Type,IsDessert,Kcal,Protein,IngredientName,Quantity,Unit,Category
Owsianka,Breakfast,false,450,25,Platki owsiane,80,g,Suche
Owsianka,Breakfast,false,450,25,Mleko,250,ml,Nabial
""";
        var csvPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        var databaseName = Guid.NewGuid().ToString("N");
        var connectionString = $"Data Source={databaseName};Mode=Memory;Cache=Shared";

        await File.WriteAllTextAsync(csvPath, csv);

        try
        {
            await using var keepAliveConnection = new SqliteConnection(connectionString);
            await keepAliveConnection.OpenAsync();

            await DietPlanner.Importer.Program.Main([csvPath, connectionString]);

            var options = new DbContextOptionsBuilder<DietPlannerDbContext>()
                .UseSqlite(connectionString)
                .Options;

            await using (var dbContext = new DietPlannerDbContext(options))
            {
                await SqliteSchemaBootstrapper.InitializeAsync(dbContext);

                var meal = await dbContext.Meals
                    .Include(x => x.Ingredients)
                    .SingleAsync();

                meal.Name.Should().Be("Owsianka");
                meal.Ingredients.Should().HaveCount(2);

                dbContext.Ingredients.Should().HaveCount(2);
                dbContext.MealIngredients.Should().HaveCount(2);
            }
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    [Fact]
    public async Task ImportFromSourceCsvPair_ShouldCreateMealsAndIngredientsFromPolishRecipeAndCategoryFiles()
    {
        var recipesCsv = """
Nazwa potrawy,Składnik,Ilość,Kcal,B,Wykonanie,Typ posiłku,Deser Tak/Nie,Nabiał
Bajgiel z domowym twarożkiem,Bajgiel,75 g,410,33,"Opis",Śniadanie,NIe,Tak
,Ser twarogowy półtłusty,125 g,,,,,,
Ciasto cytrynowe jednoporcjowe,Cytryna,50 g,421,19,"Opis 2",Śniadanie,Tak,Tak
""";
        var categoriesCsv = """
Składnik,Kategoria
Bajgiel,Pieczywo
Ser twarogowy półtłusty,Nabiał
""";
        var importer = new SourceRecipeImporter();

        var result = await importer.ImportAsync(
            new StringReader(recipesCsv),
            new StringReader(categoriesCsv),
            CancellationToken.None);

        result.Meals.Should().HaveCount(2);
        result.Ingredients.Should().HaveCount(3);

        var bagel = result.Meals.Single(x => x.Name == "Bajgiel z domowym twarożkiem");
        bagel.Type.Should().Be(MealType.Breakfast);
        bagel.IsDessert.Should().BeFalse();
        bagel.Kcal.Should().Be(410);
        bagel.Protein.Should().Be(33);
        bagel.Ingredients.Should().HaveCount(2);
        bagel.Ingredients.Should().Contain(x => x.IngredientName == "Bajgiel" && x.Quantity == 75m && x.Unit == "g" && x.Category == "Pieczywo");
        bagel.Ingredients.Should().Contain(x => x.IngredientName == "Ser twarogowy półtłusty" && x.Quantity == 125m && x.Unit == "g" && x.Category == "Nabiał");

        var dessert = result.Meals.Single(x => x.Name == "Ciasto cytrynowe jednoporcjowe");
        dessert.IsDessert.Should().BeTrue();
        dessert.Ingredients.Should().ContainSingle();
        dessert.Ingredients.Single().Category.Should().Be("Inne");
    }

    [Fact]
    public async Task ImportFromSourceCsvPair_ShouldPreserveQualitativeQuantitiesAsUnits()
    {
        var recipesCsv = """
Nazwa potrawy,Składnik,Ilość,Kcal,B,Wykonanie,Typ posiłku,Deser Tak/Nie,Nabiał
Pasta jajeczna,Jajka,110 g,430,28,"Opis",Śniadanie,NIe,Tak
,SĂłl,do smaku,,,,,,
,SĹ‚odzidĹ‚o,opcjonalnie,,,,,,
""";
        var categoriesCsv = """
Składnik,Kategoria
Jajka,NabiaĹ‚
SĂłl,Przyprawy
SĹ‚odzidĹ‚o,SĹ‚odycze
""";
        var importer = new SourceRecipeImporter();

        var result = await importer.ImportAsync(
            new StringReader(recipesCsv),
            new StringReader(categoriesCsv),
            CancellationToken.None);

        var meal = result.Meals.Single();
        meal.Ingredients.Should().Contain(x => x.IngredientName == "SĂłl" && x.Quantity == 1m && x.Unit == "do smaku" && x.Category == "Przyprawy");
        meal.Ingredients.Should().Contain(x => x.IngredientName == "SĹ‚odzidĹ‚o" && x.Quantity == 1m && x.Unit == "opcjonalnie" && x.Category == "SĹ‚odycze");
    }

    [Fact]
    public async Task ImportFromSourceCsvPair_ShouldRoundDecimalNutritionValues()
    {
        var recipesCsv = """
Nazwa potrawy,Składnik,Ilość,Kcal,B,Wykonanie,Typ posiłku,Deser Tak/Nie,Nabiał
Owsianka premium,Skyr naturalny,150 g,"410,4","30,5","Opis",Śniadanie,NIe,Tak
""";
        var categoriesCsv = """
Składnik,Kategoria
Skyr naturalny,Nabiał
""";
        var importer = new SourceRecipeImporter();

        var result = await importer.ImportAsync(
            new StringReader(recipesCsv),
            new StringReader(categoriesCsv),
            CancellationToken.None);

        var meal = result.Meals.Single();
        meal.Kcal.Should().Be(410);
        meal.Protein.Should().Be(31);
    }

    [Fact]
    public async Task ImportFromSourceCsvPair_ShouldMergeDuplicateIngredientsWithinMeal()
    {
        var recipesCsv = """
Nazwa potrawy,Składnik,Ilość,Kcal,B,Wykonanie,Typ posiłku,Deser Tak/Nie,Nabiał
Kurczak Kung Pao,Sos sojowy,10 g,520,38,"Opis",Obiad,NIe,Nie
,Sos sojowy,15 g,,,,,,
""";
        var categoriesCsv = """
Składnik,Kategoria
Sos sojowy,Przetwory
""";
        var importer = new SourceRecipeImporter();

        var result = await importer.ImportAsync(
            new StringReader(recipesCsv),
            new StringReader(categoriesCsv),
            CancellationToken.None);

        var meal = result.Meals.Single();
        meal.Ingredients.Should().ContainSingle();
        meal.Ingredients.Single().IngredientName.Should().Be("Sos sojowy");
        meal.Ingredients.Single().Quantity.Should().Be(25m);
        meal.Ingredients.Single().Unit.Should().Be("g");
    }

    [Fact]
    public async Task Import_ShouldCreateMealsAndIngredientsFromCsv()
    {
        var csv = """
Name,Type,IsDessert,Kcal,Protein,IngredientName,Quantity,Unit,Category
Owsianka,Breakfast,false,450,25,Platki owsiane,80,g,Suche
""";
        var importer = new MealImporter();

        var result = await importer.ImportAsync(new StringReader(csv), CancellationToken.None);

        result.Meals.Should().ContainSingle();
        result.Ingredients.Should().ContainSingle();

        var meal = result.Meals.Single();
        meal.Name.Should().Be("Owsianka");
        meal.Type.Should().Be(MealType.Breakfast);
        meal.IsDessert.Should().BeFalse();
        meal.Kcal.Should().Be(450);
        meal.Protein.Should().Be(25);
        meal.Ingredients.Should().ContainSingle();
        meal.Ingredients.Single().IngredientName.Should().Be("Platki owsiane");
        meal.Ingredients.Single().Quantity.Should().Be(80);
        meal.Ingredients.Single().Unit.Should().Be("g");
        meal.Ingredients.Single().Category.Should().Be("Suche");

        var ingredient = result.Ingredients.Single();
        ingredient.Name.Should().Be("Platki owsiane");
        ingredient.Unit.Should().Be("g");
    }

    [Fact]
    public async Task Import_ShouldHandleQuotedValuesAndEscapedQuotes()
    {
        var csv = "Name,Type,IsDessert,Kcal,Protein,IngredientName,Quantity,Unit,Category\r\n" +
                  "\"Owsianka, deluxe\",Breakfast,false,450,25,\"Platki \"\"gorskie\"\"\",80,g,Suche\r\n";
        var importer = new MealImporter();

        var result = await importer.ImportAsync(new StringReader(csv), CancellationToken.None);

        result.Meals.Should().ContainSingle();
        result.Meals.Single().Name.Should().Be("Owsianka, deluxe");
        result.Meals.Single().Ingredients.Should().ContainSingle();
        result.Meals.Single().Ingredients.Single().IngredientName.Should().Be("Platki \"gorskie\"");
    }

    [Fact]
    public async Task Import_ShouldRejectUnexpectedHeaderSchema()
    {
        var csv = """
MealName,Type,IsDessert,Kcal,Protein,IngredientName,Quantity,Unit,Category
Owsianka,Breakfast,false,450,25,Platki owsiane,80,g,Suche
""";
        var importer = new MealImporter();

        var act = () => importer.ImportAsync(new StringReader(csv), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<FormatException>(act);
        exception.Message.Should().Contain("header");
        exception.Message.Should().Contain("MealName");
    }

    [Fact]
    public async Task Import_ShouldReportRowColumnAndValue_WhenParsingFails()
    {
        var csv = """
Name,Type,IsDessert,Kcal,Protein,IngredientName,Quantity,Unit,Category
Owsianka,Breakfast,false,abc,25,Platki owsiane,80,g,Suche
""";
        var importer = new MealImporter();

        var act = () => importer.ImportAsync(new StringReader(csv), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<FormatException>(act);
        exception.Message.Should().Contain("row 2");
        exception.Message.Should().Contain("Kcal");
        exception.Message.Should().Contain("abc");
    }
}
