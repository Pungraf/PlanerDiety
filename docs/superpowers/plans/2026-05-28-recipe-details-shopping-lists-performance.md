# Recipe Details Shopping Lists Performance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add recipe details, linked user-created shopping lists, safer plan editing, and first-pass mobile performance/UI improvements.

**Architecture:** Extend the existing domain/API/importer model rather than adding a separate read service. Shopping lists become multiple user-created lists per weekly plan; plan edits delete linked lists only after explicit confirmation from the mobile client. Mobile performance is improved with screen-level caches, in-memory meal filtering, `CollectionView`, and compiled bindings.

**Tech Stack:** .NET 8, ASP.NET Core controllers, EF Core SQLite/PostgreSQL, xUnit/FluentAssertions, .NET MAUI XAML/ViewModels.

---

## File Structure

Modify:
- `src/DietPlanner.Domain/Entities/Meal.cs` - add recipe description.
- `src/DietPlanner.Domain/Entities/MealIngredient.cs` - allow non-shopping ingredients with quantity `0`.
- `src/DietPlanner.Domain/Entities/ShoppingList.cs` - add list name/created timestamp and allow multiple lists per weekly plan.
- `src/DietPlanner.Domain/Entities/ShoppingListItem.cs` - keep checked-state behavior.
- `src/DietPlanner.Infrastructure/Persistence/Configurations/MealConfiguration.cs` - map description.
- `src/DietPlanner.Infrastructure/Persistence/Configurations/MealIngredientConfiguration.cs` - allow zero quantity precision.
- `src/DietPlanner.Infrastructure/Persistence/Configurations/ShoppingListConfiguration.cs` - remove unique weekly-plan index; map name/date.
- `src/DietPlanner.Infrastructure/Persistence/SqliteSchemaBootstrapper.cs` - add new columns for legacy SQLite bootstrap.
- `src/DietPlanner.Infrastructure/Persistence/DietPlannerDbContext.cs` - add detail/list queries and delete helpers.
- `src/DietPlanner.Application/Abstractions/IApplicationDbContext.cs` - expose query/delete helpers.
- `src/DietPlanner.Importer/Import/MealCsvRecord.cs` - include description.
- `src/DietPlanner.Importer/Import/MealImporter.cs` - include description for normalized CSV.
- `src/DietPlanner.Importer/Import/SourceRecipeImporter.cs` - import description and grams-only quantities.
- `src/DietPlanner.Importer/Import/ImportedMealCatalogWriter.cs` - persist description.
- `src/DietPlanner.Application/Meals/Queries/SearchMealsQuery.cs` - keep catalog summary and support detail query nearby or in a new query file.
- `src/DietPlanner.Api/Controllers/MealsController.cs` - add detail endpoint.
- `src/DietPlanner.Application/Shopping/ShoppingListService.cs` - create selected lists from plan selections.
- `src/DietPlanner.Application/Shopping/ShoppingListSyncService.cs` - remove automatic sync from plan edits or replace with delete-linked-list helper.
- `src/DietPlanner.Application/Shopping/Queries/GetShoppingListQuery.cs` - replace current-list query with list-of-lists/detail queries.
- `src/DietPlanner.Application/Shopping/Commands/ToggleShoppingListItemCommand.cs` - toggle by list id/item id.
- `src/DietPlanner.Api/Controllers/ShoppingListsController.cs` - add list, detail, create, delete, toggle routes.
- `src/DietPlanner.Application/Plans/Commands/ReplaceMealCommand.cs` - require `DeleteLinkedShoppingLists` flag.
- `src/DietPlanner.Application/Plans/Commands/CopyDayCommand.cs` - require `DeleteLinkedShoppingLists` flag.
- `src/DietPlanner.Api/Controllers/PlansController.cs` - expose linked-list warnings and delete-confirm flags.
- `src/DietPlanner.Api/Extensions/ServiceCollectionExtensions.cs` - register new handlers.
- `src/DietPlanner.Mobile/Services/PlansApiClient.cs` - add meal detail/catalog, plan mutation flags.
- `src/DietPlanner.Mobile/Services/ShoppingListApiClient.cs` - add list-of-lists/create/detail/delete/toggle APIs.
- `src/DietPlanner.Mobile/ViewModels/HomeViewModel.cs` - tappable meal details, copy-day button state, confirmation-aware edits.
- `src/DietPlanner.Mobile/ViewModels/MealSearchViewModel.cs` - fetch once, in-memory filtering.
- `src/DietPlanner.Mobile/ViewModels/ShoppingListViewModel.cs` - list-of-lists and selected list detail.
- `src/DietPlanner.Mobile/Views/HomePage.xaml` - modernized home layout and compiled bindings.
- `src/DietPlanner.Mobile/Views/MealSearchPage.xaml` - scrollable result list and compiled bindings.
- `src/DietPlanner.Mobile/Views/ShoppingListPage.xaml` - list-of-lists UI and compiled bindings.
- `src/DietPlanner.Mobile/AppShell.xaml` and `.xaml.cs` - add recipe detail/create-list routes.
- `src/DietPlanner.Mobile/MauiProgram.cs` - register new viewmodels/pages/stores/services.

Create:
- `src/DietPlanner.Infrastructure/Persistence/Migrations/20260528120000_AddRecipeDetailsAndShoppingListSets.cs`
- `src/DietPlanner.Application/Meals/Queries/GetMealDetailsQuery.cs`
- `src/DietPlanner.Application/Shopping/Queries/ListShoppingListsQuery.cs`
- `src/DietPlanner.Application/Shopping/Queries/GetShoppingListDetailsQuery.cs`
- `src/DietPlanner.Application/Shopping/Queries/GetShoppingListCreateOptionsQuery.cs`
- `src/DietPlanner.Application/Shopping/Commands/CreateShoppingListCommand.cs`
- `src/DietPlanner.Application/Shopping/Commands/DeleteShoppingListCommand.cs`
- `src/DietPlanner.Application/Shopping/Commands/DeleteShoppingListsForPlanCommand.cs`
- `src/DietPlanner.Mobile/Services/MealDetailsContextStore.cs`
- `src/DietPlanner.Mobile/Services/IUserPromptService.cs`
- `src/DietPlanner.Mobile/Services/ShellUserPromptService.cs`
- `src/DietPlanner.Mobile/ViewModels/MealDetailsViewModel.cs`
- `src/DietPlanner.Mobile/ViewModels/ShoppingListCreateViewModel.cs`
- `src/DietPlanner.Mobile/Views/MealDetailsPage.xaml`
- `src/DietPlanner.Mobile/Views/MealDetailsPage.xaml.cs`
- `src/DietPlanner.Mobile/Views/ShoppingListCreatePage.xaml`
- `src/DietPlanner.Mobile/Views/ShoppingListCreatePage.xaml.cs`

Test:
- `tests/DietPlanner.Domain.Tests/MealTests.cs`
- `tests/DietPlanner.Domain.Tests/MealIngredientTests.cs`
- `tests/DietPlanner.Domain.Tests/ShoppingListTests.cs`
- `tests/DietPlanner.Api.Tests/Importer/MealImporterTests.cs`
- `tests/DietPlanner.Api.Tests/Meals/GetMealDetailsTests.cs`
- `tests/DietPlanner.Api.Tests/Shopping/ShoppingListSetTests.cs`
- `tests/DietPlanner.Api.Tests/Plans/ReplaceMealTests.cs`
- `tests/DietPlanner.Api.Tests/Plans/CopyDayTests.cs`
- `tests/DietPlanner.Mobile.Tests/HomeViewModelTests.cs`
- `tests/DietPlanner.Mobile.Tests/MealSearchViewModelTests.cs`
- `tests/DietPlanner.Mobile.Tests/ShoppingListViewModelTests.cs`
- `tests/DietPlanner.Mobile.Tests/MealDetailsViewModelTests.cs`
- `tests/DietPlanner.Mobile.Tests/ShoppingListCreateViewModelTests.cs`

---

### Task 1: Recipe Description and Gram-Only Import

**Files:**
- Modify: `src/DietPlanner.Domain/Entities/Meal.cs`
- Modify: `src/DietPlanner.Domain/Entities/MealIngredient.cs`
- Modify: `src/DietPlanner.Importer/Import/MealCsvRecord.cs`
- Modify: `src/DietPlanner.Importer/Import/MealImporter.cs`
- Modify: `src/DietPlanner.Importer/Import/SourceRecipeImporter.cs`
- Modify: `src/DietPlanner.Importer/Import/ImportedMealCatalogWriter.cs`
- Test: `tests/DietPlanner.Domain.Tests/MealTests.cs`
- Test: `tests/DietPlanner.Domain.Tests/MealIngredientTests.cs`
- Test: `tests/DietPlanner.Api.Tests/Importer/MealImporterTests.cs`

- [ ] **Step 1: Write failing domain tests**

Add tests:

```csharp
[Fact]
public void Constructor_ShouldAssignDescription()
{
    var meal = new Meal(Guid.NewGuid(), "Owsianka", MealType.Breakfast, false, 450, 25, "Wymieszaj skladniki.");

    meal.Description.Should().Be("Wymieszaj skladniki.");
}

[Fact]
public void Constructor_ShouldAllowZeroQuantityForNonShoppingIngredient()
{
    var ingredient = new MealIngredient(Guid.NewGuid(), Guid.NewGuid(), 0m, "g", "Przyprawy");

    ingredient.Quantity.Should().Be(0m);
    ingredient.Unit.Should().Be("g");
}
```

- [ ] **Step 2: Write failing importer tests**

Add tests to `MealImporterTests`:

```csharp
[Fact]
public async Task ImportFromSourceCsvPair_ShouldPreserveRecipeDescription()
{
    var recipesCsv = """
Nazwa potrawy,Składnik,Ilość,Kcal,B,Wykonanie,Typ posiłku,Deser Tak/Nie,Nabiał
Owsianka,Platki owsiane,80 g,450,25,"1. Gotuj platki.
2. Podaj.",Śniadanie,NIe,Nie
""";
    var categoriesCsv = """
Składnik,Kategoria
Platki owsiane,Suche
""";
    var importer = new SourceRecipeImporter();

    var result = await importer.ImportAsync(new StringReader(recipesCsv), new StringReader(categoriesCsv), CancellationToken.None);

    result.Meals.Single().Description.Should().Contain("Gotuj platki");
}

[Fact]
public async Task ImportFromSourceCsvPair_ShouldKeepOnlyGramQuantitiesAsShoppingQuantities()
{
    var recipesCsv = """
Nazwa potrawy,Składnik,Ilość,Kcal,B,Wykonanie,Typ posiłku,Deser Tak/Nie,Nabiał
Pasta,Jajka,110 g,430,28,"Opis",Śniadanie,NIe,Tak
,Sol,do smaku,,,,,,
,Mleko,50 ml,,,,,,
""";
    var categoriesCsv = """
Składnik,Kategoria
Jajka,Nabial
Sol,Przyprawy
Mleko,Nabial
""";
    var importer = new SourceRecipeImporter();

    var result = await importer.ImportAsync(new StringReader(recipesCsv), new StringReader(categoriesCsv), CancellationToken.None);

    var meal = result.Meals.Single();
    meal.Ingredients.Should().Contain(x => x.IngredientName == "Jajka" && x.Quantity == 110m && x.Unit == "g");
    meal.Ingredients.Should().Contain(x => x.IngredientName == "Sol" && x.Quantity == 0m && x.Unit == "g");
    meal.Ingredients.Should().Contain(x => x.IngredientName == "Mleko" && x.Quantity == 0m && x.Unit == "g");
}
```

- [ ] **Step 3: Run tests and verify they fail**

Run:

```powershell
dotnet test tests/DietPlanner.Domain.Tests/DietPlanner.Domain.Tests.csproj --filter "MealTests|MealIngredientTests"
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter "MealImporterTests"
```

Expected: compile or assertion failures because `Meal.Description` and zero-quantity imports do not exist yet.

- [ ] **Step 4: Implement domain/import changes**

Core changes:

```csharp
public string Description { get; private set; }

public Meal(Guid id, string name, MealType type, bool isDessert, int kcal, int protein, string description = "")
{
    Id = Guard.AgainstEmpty(id, nameof(id));
    Name = Guard.AgainstBlank(name, nameof(name));
    Type = Guard.AgainstUndefinedEnum(type, nameof(type));
    IsDessert = isDessert;
    Kcal = Guard.AgainstNegative(kcal, nameof(kcal));
    Protein = Guard.AgainstNegative(protein, nameof(protein));
    Description = description?.Trim() ?? string.Empty;
}
```

```csharp
Quantity = quantity < 0 ? throw new ArgumentOutOfRangeException(nameof(quantity)) : quantity;
```

Update import records:

```csharp
public sealed record ImportedMeal(
    string Name,
    MealType Type,
    bool IsDessert,
    int Kcal,
    int Protein,
    string Description,
    IReadOnlyList<ImportedMealIngredient> Ingredients);
```

Update source quantity parser:

```csharp
private static (decimal Quantity, string Unit) ParseQuantity(string value)
{
    var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 2
        && string.Equals(parts[1], "g", StringComparison.OrdinalIgnoreCase)
        && decimal.TryParse(parts[0].Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var grams))
    {
        return (grams, "g");
    }

    return (0m, "g");
}
```

- [ ] **Step 5: Run tests and verify they pass**

Run:

```powershell
dotnet test tests/DietPlanner.Domain.Tests/DietPlanner.Domain.Tests.csproj --filter "MealTests|MealIngredientTests"
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter "MealImporterTests"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/DietPlanner.Domain/Entities/Meal.cs src/DietPlanner.Domain/Entities/MealIngredient.cs src/DietPlanner.Importer/Import tests/DietPlanner.Domain.Tests tests/DietPlanner.Api.Tests/Importer/MealImporterTests.cs
git commit -m "feat: import recipe descriptions and gram quantities"
```

### Task 2: Persistence Migration for Recipe Details and Multiple Shopping Lists

**Files:**
- Modify: `src/DietPlanner.Infrastructure/Persistence/Configurations/MealConfiguration.cs`
- Modify: `src/DietPlanner.Infrastructure/Persistence/Configurations/MealIngredientConfiguration.cs`
- Modify: `src/DietPlanner.Infrastructure/Persistence/Configurations/ShoppingListConfiguration.cs`
- Modify: `src/DietPlanner.Infrastructure/Persistence/SqliteSchemaBootstrapper.cs`
- Create: `src/DietPlanner.Infrastructure/Persistence/Migrations/20260528120000_AddRecipeDetailsAndShoppingListSets.cs`
- Test: `tests/DietPlanner.Api.Tests/Persistence/DatabaseBootstrapTests.cs`

- [ ] **Step 1: Write failing persistence expectations**

Add or extend a persistence test to create a meal with description and two shopping lists for the same plan:

```csharp
var first = new ShoppingList(Guid.NewGuid(), plan.Id, "Lista 1", new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero));
var second = new ShoppingList(Guid.NewGuid(), plan.Id, "Lista 2", new DateTimeOffset(2026, 5, 28, 11, 0, 0, TimeSpan.Zero));

await dbContext.ShoppingLists.AddRangeAsync(first, second);
await dbContext.SaveChangesAsync();

(await dbContext.ShoppingLists.CountAsync(x => x.WeeklyPlanId == plan.Id)).Should().Be(2);
```

- [ ] **Step 2: Run test and verify it fails**

Run:

```powershell
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter "DatabaseBootstrapTests"
```

Expected: FAIL because shopping lists have a unique weekly-plan index and no name/created columns.

- [ ] **Step 3: Implement persistence mapping and migration**

Update `ShoppingList` constructor first:

```csharp
public string Name { get; private set; }
public DateTimeOffset CreatedAt { get; private set; }

public ShoppingList(Guid id, Guid weeklyPlanId, string name = "Shopping list", DateTimeOffset? createdAt = null)
{
    Id = Guard.AgainstEmpty(id, nameof(id));
    WeeklyPlanId = Guard.AgainstEmpty(weeklyPlanId, nameof(weeklyPlanId));
    Name = Guard.AgainstBlank(name, nameof(name));
    CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
}
```

Update mapping:

```csharp
builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
builder.Property(x => x.CreatedAt).IsRequired();
builder.HasIndex(x => x.WeeklyPlanId);
builder.HasOne<WeeklyPlan>()
    .WithMany()
    .HasForeignKey(x => x.WeeklyPlanId)
    .OnDelete(DeleteBehavior.Cascade);
```

Create migration:

```csharp
[Migration("20260528120000_AddRecipeDetailsAndShoppingListSets")]
public partial class AddRecipeDetailsAndShoppingListSets : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("Description", "Meals", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("Name", "ShoppingLists", maxLength: 120, nullable: false, defaultValue: "Shopping list");
        migrationBuilder.AddColumn<DateTimeOffset>("CreatedAt", "ShoppingLists", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP");
        migrationBuilder.DropIndex("IX_ShoppingLists_WeeklyPlanId", "ShoppingLists");
        migrationBuilder.CreateIndex("IX_ShoppingLists_WeeklyPlanId", "ShoppingLists", "WeeklyPlanId");
    }
}
```

- [ ] **Step 4: Run persistence tests**

Run:

```powershell
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter "DatabaseBootstrapTests|PostgresConfigurationTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/DietPlanner.Infrastructure src/DietPlanner.Domain/Entities/ShoppingList.cs tests/DietPlanner.Api.Tests/Persistence
git commit -m "feat: persist recipe details and multiple shopping lists"
```

### Task 3: Meal Detail API

**Files:**
- Create: `src/DietPlanner.Application/Meals/Queries/GetMealDetailsQuery.cs`
- Modify: `src/DietPlanner.Application/Abstractions/IApplicationDbContext.cs`
- Modify: `src/DietPlanner.Infrastructure/Persistence/DietPlannerDbContext.cs`
- Modify: `src/DietPlanner.Api/Controllers/MealsController.cs`
- Modify: `tests/DietPlanner.Api.Tests/TestData.cs`
- Create: `tests/DietPlanner.Api.Tests/Meals/GetMealDetailsTests.cs`

- [ ] **Step 1: Write failing API test**

```csharp
[Fact]
public async Task GetMealDetails_ShouldReturnDescriptionAndCategorizedIngredients()
{
    await using var app = await PlansApiFactory.WithDraftPlanAsync();
    using var client = await app.CreateAuthenticatedClientAsync();

    var response = await client.GetAsync($"/api/meals/{TestData.BreakfastMealId}");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<MealDetailsResponse>();
    payload!.Name.Should().Be("Berry Oat Bowl");
    payload.Description.Should().Contain("Prepare");
    payload.Ingredients.Should().Contain(x => x.Name == "Oats" && x.Quantity == 80m && x.Unit == "g" && x.Category == "Pantry");
}

private sealed record MealDetailsResponse(Guid Id, string Name, string Type, int Kcal, int Protein, string Description, IReadOnlyList<MealIngredientResponse> Ingredients);
private sealed record MealIngredientResponse(string Name, decimal Quantity, string Unit, string Category);
```

- [ ] **Step 2: Run test and verify it fails**

Run:

```powershell
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter "GetMealDetails"
```

Expected: FAIL with 404 or missing route.

- [ ] **Step 3: Implement detail query and endpoint**

Query DTO:

```csharp
public sealed record GetMealDetailsQuery(Guid MealId);

public sealed record MealDetailsDto(
    Guid Id,
    string Name,
    string Type,
    int Kcal,
    int Protein,
    string Description,
    IReadOnlyList<MealDetailsIngredientDto> Ingredients);
```

Db query:

```csharp
public Task<Meal?> FindMealDetailsByIdAsync(Guid mealId, CancellationToken cancellationToken)
    => Meals.Include(meal => meal.Ingredients)
        .SingleOrDefaultAsync(meal => meal.Id == mealId, cancellationToken);
```

Controller:

```csharp
[HttpGet("{mealId:guid}")]
public async Task<ActionResult<MealDetailsResponse>> GetDetails(Guid mealId, CancellationToken cancellationToken)
{
    if (mealId == Guid.Empty) return BadRequest();
    var meal = await _getMealDetailsHandler.HandleAsync(new GetMealDetailsQuery(mealId), cancellationToken);
    return meal is null ? NotFound() : Ok(MealDetailsResponse.From(meal));
}
```

- [ ] **Step 4: Run meal API tests**

Run:

```powershell
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter "SearchMealsTests|GetMealDetails"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/DietPlanner.Application/Meals src/DietPlanner.Application/Abstractions src/DietPlanner.Infrastructure/Persistence/DietPlannerDbContext.cs src/DietPlanner.Api/Controllers/MealsController.cs tests/DietPlanner.Api.Tests/Meals tests/DietPlanner.Api.Tests/TestData.cs
git commit -m "feat: expose meal details"
```

### Task 4: Shopping List API as List-of-Lists

**Files:**
- Create: `src/DietPlanner.Application/Shopping/Queries/ListShoppingListsQuery.cs`
- Create: `src/DietPlanner.Application/Shopping/Queries/GetShoppingListDetailsQuery.cs`
- Create: `src/DietPlanner.Application/Shopping/Queries/GetShoppingListCreateOptionsQuery.cs`
- Create: `src/DietPlanner.Application/Shopping/Commands/CreateShoppingListCommand.cs`
- Create: `src/DietPlanner.Application/Shopping/Commands/DeleteShoppingListCommand.cs`
- Modify: `src/DietPlanner.Application/Shopping/Commands/ToggleShoppingListItemCommand.cs`
- Modify: `src/DietPlanner.Application/Shopping/ShoppingListService.cs`
- Modify: `src/DietPlanner.Api/Controllers/ShoppingListsController.cs`
- Modify: `src/DietPlanner.Api/Extensions/ServiceCollectionExtensions.cs`
- Test: `tests/DietPlanner.Api.Tests/Shopping/ShoppingListSetTests.cs`
- Test: `tests/DietPlanner.Domain.Tests/ShoppingListTests.cs`

- [ ] **Step 1: Write failing shopping-list API tests**

```csharp
[Fact]
public async Task CreateShoppingList_ShouldSummarizeSelectedIngredientsForCurrentWeek()
{
    await using var app = await PlansApiFactory.WithActivePlanAsync(userId =>
        PlansApiFactory.CreateActivePlan(userId, new DateOnly(2026, 5, 25), plan =>
        {
            PlansApiFactory.AssignMeal(plan, new DateOnly(2026, 5, 25), MealSlotType.Breakfast, TestData.BreakfastMealId);
            PlansApiFactory.AssignMeal(plan, new DateOnly(2026, 5, 25), MealSlotType.Dinner, TestData.LunchMealId);
        }));
    using var client = await app.CreateAuthenticatedClientAsync();

    var response = await client.PostAsJsonAsync("/api/shopping-lists", new
    {
        name = "Weekly shop",
        ingredientKeys = new[]
        {
            new { date = "2026-05-25", slotType = "breakfast", ingredientId = TestData.OatsIngredientId },
            new { date = "2026-05-25", slotType = "dinner", ingredientId = TestData.ChickenIngredientId }
        }
    });

    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var payload = await response.Content.ReadFromJsonAsync<ShoppingListDetailsResponse>();
    payload!.Name.Should().Be("Weekly shop");
    payload.Items.Should().Contain(x => x.Name == "Oats" && x.Quantity == 80m);
    payload.Items.Should().Contain(x => x.Name == "Chicken" && x.Quantity == 140m);
}

[Fact]
public async Task ListShoppingLists_ShouldReturnAllListsForCurrentWeek()
{
    await using var app = await PlansApiFactory.WithShoppingListAsync();
    using var client = await app.CreateAuthenticatedClientAsync();

    var response = await client.GetAsync("/api/shopping-lists");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<ShoppingListSummaryResponse>>();
    payload.Should().NotBeNull();
    payload!.Should().NotBeEmpty();
}
```

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter "ShoppingListSetTests"
```

Expected: FAIL because routes/handlers do not exist.

- [ ] **Step 3: Implement DTOs and handlers**

Create selected ingredient key:

```csharp
public sealed record SelectedShoppingIngredient(DateOnly Date, MealSlotType SlotType, Guid IngredientId);
```

Create command:

```csharp
public sealed record CreateShoppingListCommand(Guid UserId, string Name, IReadOnlyList<SelectedShoppingIngredient> Ingredients);
```

Summarize selected shopping ingredients:

```csharp
var selectedKeys = command.Ingredients.ToHashSet();
var items = plan.Days
    .SelectMany(day => day.MealSlots.Select(slot => new { day.Date, Slot = slot }))
    .Where(x => x.Slot.MealId.HasValue)
    .SelectMany(x => mealsById[x.Slot.MealId!.Value].Ingredients
        .Where(ingredient => ingredient.Quantity > 0m)
        .Where(ingredient => selectedKeys.Contains(new SelectedShoppingIngredient(x.Date, x.Slot.SlotType, ingredient.IngredientId))))
    .GroupBy(ingredient => new { ingredient.IngredientId, ingredient.Unit })
    .Select(group => new ShoppingListItem(Guid.NewGuid(), group.Key.IngredientId, group.Sum(x => x.Quantity), group.Key.Unit))
    .ToArray();
```

Routes:

```csharp
[HttpGet]
[HttpGet("{listId:guid}")]
[HttpGet("create-options")]
[HttpPost]
[HttpDelete("{listId:guid}")]
[HttpPost("{listId:guid}/items/{itemId:guid}/toggle")]
```

- [ ] **Step 4: Run shopping-list tests**

Run:

```powershell
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter "ShoppingListSetTests|ToggleShoppingListItemTests"
```

Expected: PASS after updating old toggle tests to use `/api/shopping-lists/{listId}/items/{itemId}/toggle`.

- [ ] **Step 5: Commit**

```powershell
git add src/DietPlanner.Application/Shopping src/DietPlanner.Api/Controllers/ShoppingListsController.cs src/DietPlanner.Api/Extensions/ServiceCollectionExtensions.cs tests/DietPlanner.Api.Tests/Shopping tests/DietPlanner.Domain.Tests/ShoppingListTests.cs
git commit -m "feat: create linked shopping list sets"
```

### Task 5: Plan Edit Confirmation Contract

**Files:**
- Create: `src/DietPlanner.Application/Shopping/Commands/DeleteShoppingListsForPlanCommand.cs`
- Modify: `src/DietPlanner.Application/Plans/Commands/ReplaceMealCommand.cs`
- Modify: `src/DietPlanner.Application/Plans/Commands/CopyDayCommand.cs`
- Modify: `src/DietPlanner.Api/Controllers/PlansController.cs`
- Modify: `src/DietPlanner.Application/Shopping/ShoppingListSyncService.cs`
- Test: `tests/DietPlanner.Api.Tests/Plans/ReplaceMealTests.cs`
- Test: `tests/DietPlanner.Api.Tests/Plans/CopyDayTests.cs`

- [ ] **Step 1: Write failing API tests**

```csharp
[Fact]
public async Task ReplaceMeal_ShouldReturnConflict_WhenLinkedShoppingListsExistAndDeleteIsNotConfirmed()
{
    await using var app = await PlansApiFactory.WithShoppingListAsync();
    using var client = await app.CreateAuthenticatedClientAsync();

    var response = await client.PutAsJsonAsync("/api/plans/current/days/2026-05-25/slots/breakfast", new
    {
        mealId = TestData.DinnerMealId,
        deleteLinkedShoppingLists = false
    });

    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
}

[Fact]
public async Task ReplaceMeal_ShouldDeleteLinkedShoppingLists_WhenDeleteIsConfirmed()
{
    await using var app = await PlansApiFactory.WithShoppingListAsync();
    using var client = await app.CreateAuthenticatedClientAsync();

    var response = await client.PutAsJsonAsync("/api/plans/current/days/2026-05-25/slots/breakfast", new
    {
        mealId = TestData.DinnerMealId,
        deleteLinkedShoppingLists = true
    });

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    (await app.ReadShoppingListsAsync()).Should().BeEmpty();
}
```

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter "ReplaceMealTests|CopyDayTests"
```

Expected: FAIL because edit commands do not check list conflicts.

- [ ] **Step 3: Implement conflict contract**

Request DTO:

```csharp
public sealed record ReplaceMealRequest(Guid MealId, bool DeleteLinkedShoppingLists);
public sealed record CopyDayRequest(string SourceDate, string TargetDate, bool DeleteLinkedShoppingLists);
```

Result:

```csharp
public enum PlanEditFailureReason
{
    NotFound,
    LinkedShoppingListsExist
}

public sealed record PlanEditResult(bool Succeeded, PlanEditFailureReason? FailureReason);
```

Handler logic:

```csharp
var linkedLists = await _dbContext.ListShoppingListsByWeeklyPlanIdAsync(plan.Id, cancellationToken);
if (linkedLists.Count > 0 && !command.DeleteLinkedShoppingLists)
{
    return ReplaceMealResult.LinkedShoppingListsExist();
}

if (linkedLists.Count > 0)
{
    _dbContext.RemoveShoppingLists(linkedLists);
}
```

Controller maps linked-list failure to `409 Conflict`:

```csharp
return result.FailureReason == PlanEditFailureReason.LinkedShoppingListsExist
    ? Conflict(new { code = "linkedShoppingListsExist" })
    : NotFound();
```

Remove automatic `_shoppingListSyncService.SyncAsync(plan, cancellationToken)` from replace/copy commands.

- [ ] **Step 4: Run plan tests**

Run:

```powershell
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter "ReplaceMealTests|CopyDayTests|ShoppingListSetTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/DietPlanner.Application/Plans src/DietPlanner.Application/Shopping src/DietPlanner.Api/Controllers/PlansController.cs tests/DietPlanner.Api.Tests/Plans
git commit -m "feat: require confirmation before deleting linked shopping lists"
```

### Task 6: Mobile API Clients, Context Stores, and Prompt Service

**Files:**
- Modify: `src/DietPlanner.Mobile/Services/PlansApiClient.cs`
- Modify: `src/DietPlanner.Mobile/Services/ShoppingListApiClient.cs`
- Create: `src/DietPlanner.Mobile/Services/MealDetailsContextStore.cs`
- Create: `src/DietPlanner.Mobile/Services/IUserPromptService.cs`
- Create: `src/DietPlanner.Mobile/Services/ShellUserPromptService.cs`
- Modify: `src/DietPlanner.Mobile/MauiProgram.cs`
- Test: `tests/DietPlanner.Mobile.Tests/HomeViewModelTests.cs`
- Test: `tests/DietPlanner.Mobile.Tests/MealSearchViewModelTests.cs`
- Test: `tests/DietPlanner.Mobile.Tests/ShoppingListViewModelTests.cs`

- [ ] **Step 1: Write failing mobile service/viewmodel test doubles**

Update fake clients to include:

```csharp
Task<MealDetailsDto> GetMealDetailsAsync(Guid mealId, CancellationToken cancellationToken = default);
Task<IReadOnlyList<MealSummaryDto>> GetMealCatalogAsync(CancellationToken cancellationToken = default);
Task ReplaceMealAsync(DateOnly date, string slotType, Guid mealId, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default);
Task CopyDayAsync(DateOnly sourceDate, DateOnly targetDate, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default);
```

- [ ] **Step 2: Implement prompt service**

```csharp
public interface IUserPromptService
{
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}

public sealed class ShellUserPromptService : IUserPromptService
{
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        => Shell.Current.DisplayAlert(title, message, accept, cancel);
}
```

- [ ] **Step 3: Implement context store**

```csharp
public sealed record MealDetailsContext(Guid MealId);

public interface IMealDetailsContextStore
{
    MealDetailsContext? Current { get; set; }
}

public sealed class InMemoryMealDetailsContextStore : IMealDetailsContextStore
{
    public MealDetailsContext? Current { get; set; }
}
```

- [ ] **Step 4: Implement client methods**

Handle conflict:

```csharp
if (response.StatusCode == HttpStatusCode.Conflict)
{
    throw new LinkedShoppingListsExistException();
}
```

Add:

```csharp
public sealed class LinkedShoppingListsExistException : Exception
{
    public LinkedShoppingListsExistException()
        : base("This week has linked shopping lists.")
    {
    }
}
```

- [ ] **Step 5: Register services**

```csharp
builder.Services.AddSingleton<IUserPromptService, ShellUserPromptService>();
builder.Services.AddSingleton<IMealDetailsContextStore, InMemoryMealDetailsContextStore>();
```

- [ ] **Step 6: Run mobile tests**

Run:

```powershell
dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj
```

Expected: compile after all fake clients are updated.

- [ ] **Step 7: Commit**

```powershell
git add src/DietPlanner.Mobile/Services src/DietPlanner.Mobile/MauiProgram.cs tests/DietPlanner.Mobile.Tests
git commit -m "feat: add mobile clients for recipe and shopping workflows"
```

### Task 7: Home and Replace Meal ViewModels

**Files:**
- Modify: `src/DietPlanner.Mobile/ViewModels/HomeViewModel.cs`
- Modify: `src/DietPlanner.Mobile/ViewModels/MealSearchViewModel.cs`
- Test: `tests/DietPlanner.Mobile.Tests/HomeViewModelTests.cs`
- Test: `tests/DietPlanner.Mobile.Tests/MealSearchViewModelTests.cs`

- [ ] **Step 1: Write failing Home tests**

```csharp
[Fact]
public async Task OpenMealDetailsCommand_ShouldStoreMealIdAndNavigateToDetails()
{
    var contextStore = new InMemoryMealDetailsContextStore();
    var navigator = new RecordingNavigator();
    var viewModel = CreateLoadedHomeViewModel(contextStore: contextStore, navigator: navigator);

    await viewModel.OpenMealDetailsCommand.ExecuteAsync(viewModel.SelectedDay!.Meals.First());

    contextStore.Current!.MealId.Should().Be(viewModel.SelectedDay.Meals.First().MealId);
    navigator.LastRoute.Should().Be("meal-details");
}

[Fact]
public async Task ReplaceMealAfterConflict_ShouldPromptAndRetryWithDeleteFlag()
{
    var prompt = new AcceptingPromptService();
    var plansClient = new ConflictThenSuccessPlansApiClient();
    var viewModel = CreateMealSearchViewModel(plansClient, prompt);

    await viewModel.ReplaceMealCommand.ExecuteAsync(viewModel.Meals.Single());

    plansClient.ReplaceCalls.Should().Equal(false, true);
}
```

- [ ] **Step 2: Write failing MealSearch performance test**

```csharp
[Fact]
public async Task SearchText_ShouldFilterLoadedCatalogInMemory()
{
    var plansClient = new FakePlansApiClient(catalog:
    [
        new MealSummaryDto(Guid.NewGuid(), "Chicken Rice", "lunch", 700, 45),
        new MealSummaryDto(Guid.NewGuid(), "Oats Bowl", "breakfast", 500, 30)
    ]);
    var viewModel = CreateMealSearchViewModel(plansClient);

    await viewModel.LoadAsync();
    viewModel.SearchText = "oats";

    viewModel.Meals.Should().ContainSingle(x => x.Name == "Oats Bowl");
    plansClient.CatalogCalls.Should().Be(1);
}
```

- [ ] **Step 3: Run tests and verify they fail**

Run:

```powershell
dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter "HomeViewModelTests|MealSearchViewModelTests"
```

Expected: FAIL because commands/cache behavior do not exist.

- [ ] **Step 4: Implement viewmodel behavior**

Add `MealId` to plan DTO and home meal viewmodel:

```csharp
public sealed record PlanMealSlotDto(string SlotType, Guid? MealId, string Name, int Kcal, int Protein);

public Guid? MealId { get; }
public bool HasMeal => MealId.HasValue;
```

Add details command:

```csharp
OpenMealDetailsCommand = new AsyncCommand(slot => OpenMealDetailsAsync(slot as HomeMealSlotViewModel));
```

Replace search loading:

```csharp
private IReadOnlyList<MealSearchMealViewModel> _catalog = [];

private async Task LoadMealsAsync(CancellationToken cancellationToken)
{
    if (_catalog.Count == 0)
    {
        var meals = await _plansApiClient.GetMealCatalogAsync(cancellationToken);
        _catalog = meals.Select(MapMeal).OrderBy(x => x.Name).ToArray();
    }

    ApplyFilter();
}
```

Conflict retry:

```csharp
catch (LinkedShoppingListsExistException)
{
    var confirmed = await _promptService.ConfirmAsync(
        "Delete shopping lists?",
        "This week has linked shopping lists. Changing the plan will delete them.",
        "Continue",
        "Cancel");

    if (confirmed)
    {
        await _plansApiClient.ReplaceMealAsync(context.Date, context.SlotType, meal.Id, deleteLinkedShoppingLists: true);
        await _navigator.GoToAsync("//home");
    }
}
```

- [ ] **Step 5: Run tests**

Run:

```powershell
dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter "HomeViewModelTests|MealSearchViewModelTests"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/DietPlanner.Mobile/ViewModels/HomeViewModel.cs src/DietPlanner.Mobile/ViewModels/MealSearchViewModel.cs src/DietPlanner.Mobile/Services/PlansApiClient.cs tests/DietPlanner.Mobile.Tests/HomeViewModelTests.cs tests/DietPlanner.Mobile.Tests/MealSearchViewModelTests.cs
git commit -m "feat: add recipe navigation and cached meal search"
```

### Task 8: Meal Details Mobile Page

**Files:**
- Create: `src/DietPlanner.Mobile/ViewModels/MealDetailsViewModel.cs`
- Create: `src/DietPlanner.Mobile/Views/MealDetailsPage.xaml`
- Create: `src/DietPlanner.Mobile/Views/MealDetailsPage.xaml.cs`
- Modify: `src/DietPlanner.Mobile/AppShell.xaml.cs`
- Modify: `src/DietPlanner.Mobile/MauiProgram.cs`
- Test: `tests/DietPlanner.Mobile.Tests/MealDetailsViewModelTests.cs`

- [ ] **Step 1: Write failing viewmodel tests**

```csharp
[Fact]
public async Task LoadAsync_ShouldLoadMealDetailsFromContext()
{
    var mealId = Guid.NewGuid();
    var context = new InMemoryMealDetailsContextStore { Current = new MealDetailsContext(mealId) };
    var api = new FakePlansApiClient(details: new MealDetailsDto(mealId, "Owsianka", "breakfast", 450, 25, "Gotuj.", []));
    var viewModel = new MealDetailsViewModel(api, context);

    await viewModel.LoadAsync();

    viewModel.Name.Should().Be("Owsianka");
    viewModel.Description.Should().Be("Gotuj.");
}
```

- [ ] **Step 2: Implement viewmodel**

```csharp
public sealed class MealDetailsViewModel : INotifyPropertyChanged
{
    public ObservableCollection<MealDetailsIngredientViewModel> Ingredients { get; } = [];

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var mealId = _contextStore.Current?.MealId;
        if (mealId is null)
        {
            ErrorMessage = "No meal selected.";
            return;
        }

        var details = await _plansApiClient.GetMealDetailsAsync(mealId.Value, cancellationToken);
        Name = details.Name;
        Kcal = details.Kcal;
        Protein = details.Protein;
        Description = details.Description;
        ApplyIngredients(details.Ingredients);
    }
}
```

- [ ] **Step 3: Add page and route**

```csharp
Routing.RegisterRoute("meal-details", typeof(MealDetailsPage));
builder.Services.AddTransient<MealDetailsViewModel>();
builder.Services.AddTransient<Views.MealDetailsPage>();
```

- [ ] **Step 4: Add compiled-binding XAML**

```xml
<ContentPage
    x:Class="DietPlanner.Mobile.Views.MealDetailsPage"
    xmlns:viewModels="clr-namespace:DietPlanner.Mobile.ViewModels"
    x:DataType="viewModels:MealDetailsViewModel"
    Title="Recipe">
    <CollectionView ItemsSource="{Binding Ingredients}">
        <CollectionView.Header>
            <VerticalStackLayout Padding="20" Spacing="12">
                <Label FontSize="26" FontAttributes="Bold" Text="{Binding Name}" />
                <Label Text="{Binding MacroSummary}" />
                <Label FontAttributes="Bold" Text="Ingredients" />
            </VerticalStackLayout>
        </CollectionView.Header>
        <CollectionView.Footer>
            <VerticalStackLayout Padding="20" Spacing="8">
                <Label FontAttributes="Bold" Text="Preparation" />
                <Label Text="{Binding Description}" LineBreakMode="WordWrap" />
            </VerticalStackLayout>
        </CollectionView.Footer>
    </CollectionView>
</ContentPage>
```

- [ ] **Step 5: Run tests**

Run:

```powershell
dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter "MealDetailsViewModelTests"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/DietPlanner.Mobile/ViewModels/MealDetailsViewModel.cs src/DietPlanner.Mobile/Views/MealDetailsPage.xaml src/DietPlanner.Mobile/Views/MealDetailsPage.xaml.cs src/DietPlanner.Mobile/AppShell.xaml.cs src/DietPlanner.Mobile/MauiProgram.cs tests/DietPlanner.Mobile.Tests/MealDetailsViewModelTests.cs
git commit -m "feat: add meal details screen"
```

### Task 9: Shopping Lists Mobile Flow

**Files:**
- Modify: `src/DietPlanner.Mobile/ViewModels/ShoppingListViewModel.cs`
- Create: `src/DietPlanner.Mobile/ViewModels/ShoppingListCreateViewModel.cs`
- Modify: `src/DietPlanner.Mobile/Views/ShoppingListPage.xaml`
- Create: `src/DietPlanner.Mobile/Views/ShoppingListCreatePage.xaml`
- Create: `src/DietPlanner.Mobile/Views/ShoppingListCreatePage.xaml.cs`
- Modify: `src/DietPlanner.Mobile/AppShell.xaml.cs`
- Modify: `src/DietPlanner.Mobile/MauiProgram.cs`
- Test: `tests/DietPlanner.Mobile.Tests/ShoppingListViewModelTests.cs`
- Test: `tests/DietPlanner.Mobile.Tests/ShoppingListCreateViewModelTests.cs`

- [ ] **Step 1: Write failing list-of-lists tests**

```csharp
[Fact]
public async Task LoadAsync_ShouldShowShoppingListsBeforeItems()
{
    var api = new FakeShoppingListApiClient(lists:
    [
        new ShoppingListSummaryDto(Guid.NewGuid(), "Weekly shop", "2026-05-28T10:00:00Z", 3)
    ]);
    var viewModel = new ShoppingListViewModel(api, new RecordingNavigator());

    await viewModel.LoadAsync();

    viewModel.Lists.Should().ContainSingle(x => x.Name == "Weekly shop");
    viewModel.ShowListPicker.Should().BeTrue();
}
```

- [ ] **Step 2: Write failing create-list tests**

```csharp
[Fact]
public async Task ToggleMeal_ShouldToggleAllIngredientsInMeal()
{
    var meal = new ShoppingListCreateMealOptionDto("2026-05-25", "breakfast", "Breakfast",
    [
        new ShoppingListCreateIngredientOptionDto(TestData.OatsIngredientId, "Oats", 80m, "g", "Pantry", true)
    ]);
    var api = new FakeShoppingListApiClient(createOptions: [new ShoppingListCreateDayOptionDto("2026-05-25", [meal])]);
    var viewModel = new ShoppingListCreateViewModel(api, new RecordingNavigator());

    await viewModel.LoadAsync();
    viewModel.ToggleMealCommand.Execute(viewModel.Days.Single().Meals.Single());

    viewModel.Days.Single().Meals.Single().Ingredients.Single().IsSelected.Should().BeFalse();
}
```

- [ ] **Step 3: Implement viewmodels**

Main shopping state:

```csharp
public ObservableCollection<ShoppingListSummaryViewModel> Lists { get; } = [];
public ObservableCollection<ShoppingListSummaryItemViewModel> Items { get; } = [];
public bool ShowListPicker => SelectedList is null;
public bool ShowListDetails => SelectedList is not null;
```

Create selection:

```csharp
private IReadOnlyList<CreateShoppingListIngredientRequest> GetSelectedIngredients()
    => Days.SelectMany(day => day.Meals)
        .SelectMany(meal => meal.Ingredients
            .Where(ingredient => ingredient.IsSelected && ingredient.Quantity > 0m)
            .Select(ingredient => new CreateShoppingListIngredientRequest(meal.Date, meal.SlotType, ingredient.IngredientId)))
        .ToArray();
```

- [ ] **Step 4: Add routes and XAML**

Register:

```csharp
Routing.RegisterRoute("shopping-list-create", typeof(ShoppingListCreatePage));
```

Use `CollectionView` for lists and create options with `x:DataType` on pages and data templates.

- [ ] **Step 5: Run mobile shopping tests**

Run:

```powershell
dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter "ShoppingListViewModelTests|ShoppingListCreateViewModelTests"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/DietPlanner.Mobile/ViewModels/ShoppingListViewModel.cs src/DietPlanner.Mobile/ViewModels/ShoppingListCreateViewModel.cs src/DietPlanner.Mobile/Views/ShoppingListPage.xaml src/DietPlanner.Mobile/Views/ShoppingListCreatePage.xaml src/DietPlanner.Mobile/Views/ShoppingListCreatePage.xaml.cs src/DietPlanner.Mobile/AppShell.xaml.cs src/DietPlanner.Mobile/MauiProgram.cs tests/DietPlanner.Mobile.Tests/ShoppingListViewModelTests.cs tests/DietPlanner.Mobile.Tests/ShoppingListCreateViewModelTests.cs
git commit -m "feat: add shopping list sets mobile flow"
```

### Task 10: Home, Search, and Shopping UI Modernization

**Files:**
- Modify: `src/DietPlanner.Mobile/Views/HomePage.xaml`
- Modify: `src/DietPlanner.Mobile/Views/MealSearchPage.xaml`
- Modify: `src/DietPlanner.Mobile/Views/ShoppingListPage.xaml`
- Modify: `src/DietPlanner.Mobile/DietPlanner.Mobile.csproj`

- [ ] **Step 1: Enable compiled binding support**

Add for Android target:

```xml
<MauiEnableXamlCBindingWithSourceCompilation>true</MauiEnableXamlCBindingWithSourceCompilation>
```

- [ ] **Step 2: Add `x:DataType` to pages and templates**

Example:

```xml
xmlns:viewModels="clr-namespace:DietPlanner.Mobile.ViewModels"
x:DataType="viewModels:HomeViewModel"
```

Template example:

```xml
<DataTemplate x:DataType="viewModels:HomeMealSlotViewModel">
```

- [ ] **Step 3: Replace nested scroll/list combinations**

Home can keep an outer `ScrollView` only if inner meal/day sections are small and not virtualized. Meal search and shopping-list results must use `CollectionView` as the scrolling root:

```xml
<CollectionView ItemsSource="{Binding Meals}" SelectionMode="None">
    <CollectionView.Header>
        <VerticalStackLayout Padding="20" Spacing="12">
            <Label FontSize="26" FontAttributes="Bold" Text="{Binding ScreenTitle}" />
            <SearchBar Placeholder="Search meals" Text="{Binding SearchText}" />
        </VerticalStackLayout>
    </CollectionView.Header>
</CollectionView>
```

- [ ] **Step 4: Make recipe detail vs replace visually separate**

In `HomePage.xaml`, make the card tap open details and keep replace as a separate button:

```xml
<Border Padding="14">
    <Border.GestureRecognizers>
        <TapGestureRecognizer
            Command="{Binding Source={RelativeSource AncestorType={x:Type ContentPage}}, Path=BindingContext.OpenMealDetailsCommand}"
            CommandParameter="{Binding .}" />
    </Border.GestureRecognizers>
    <Grid ColumnDefinitions="*,Auto">
        <VerticalStackLayout>
            <Label FontAttributes="Bold" Text="{Binding SlotLabel}" />
            <Label Text="{Binding Name}" />
            <Label Text="{Binding MacroSummary}" />
        </VerticalStackLayout>
        <Button Grid.Column="1" Text="Replace" Command="{Binding Source={RelativeSource AncestorType={x:Type ContentPage}}, Path=BindingContext.OpenMealSearchCommand}" CommandParameter="{Binding .}" />
    </Grid>
</Border>
```

- [ ] **Step 5: Build mobile project**

Run:

```powershell
dotnet build src/DietPlanner.Mobile/DietPlanner.Mobile.csproj -f net8.0
```

Expected: PASS with no XAML compile errors for the net8.0 test target.

- [ ] **Step 6: Commit**

```powershell
git add src/DietPlanner.Mobile/Views src/DietPlanner.Mobile/DietPlanner.Mobile.csproj
git commit -m "style: modernize mobile planning screens"
```

### Task 11: End-to-End Verification

**Files:**
- Modify if needed: `README.md`
- Verify: whole solution

- [ ] **Step 1: Run full tests**

Run:

```powershell
dotnet test DietPlanner.sln
```

Expected: PASS.

- [ ] **Step 2: Build API and mobile test target**

Run:

```powershell
dotnet build src/DietPlanner.Api/DietPlanner.Api.csproj
dotnet build src/DietPlanner.Mobile/DietPlanner.Mobile.csproj -f net8.0
```

Expected: PASS.

- [ ] **Step 3: Smoke-check importer against the real CSV files**

Use a throwaway SQLite file:

```powershell
$db = Join-Path $env:TEMP "dietplanner-import-smoke.db"
if (Test-Path $db) { Remove-Item $db }
dotnet run --project src/DietPlanner.Importer -- "..\..\przepisy do apki - Przepisy.csv" "..\..\przepisy do apki - Klasyfikacja składniki.csv" "Data Source=$db"
```

Expected: command prints imported meal, ingredient, and meal-ingredient counts without throwing.

- [ ] **Step 4: Update README if endpoint/import usage changed**

If importer arguments or shopping-list API behavior are mentioned in README, update examples. Do not document internal implementation details.

- [ ] **Step 5: Final commit**

```powershell
git add README.md
git commit -m "docs: update recipe import and shopping list notes"
```

Skip this commit if README did not change.

---

## Plan Self-Review

Spec coverage:
- Recipe descriptions: Task 1, Task 2, Task 3, Task 8.
- Gram-only import and non-shopping ingredients: Task 1.
- Ingredient categories: Task 1 and Task 4 preserve category output.
- Meal detail navigation: Task 7 and Task 8.
- Replace/copy destructive confirmation: Task 5 and Task 7.
- Shopping list list-of-lists/create/delete/toggle: Task 4 and Task 9.
- Performance: Task 7 and Task 10.
- UI modernization: Task 8, Task 9, Task 10.

Placeholder scan:
- The plan contains concrete steps, commands, and code snippets rather than deferred work markers.

Type consistency:
- Mobile API methods use `deleteLinkedShoppingLists` consistently.
- Shopping-list routes consistently use `/api/shopping-lists` and `/api/shopping-lists/{listId}`.
- Plan edit conflict code is `linkedShoppingListsExist`.
