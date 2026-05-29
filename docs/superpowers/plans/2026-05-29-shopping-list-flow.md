# Shopping List Flow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Finish the shopping-list feature so the mobile app supports a saved-lists index, two-step creation, delete from index/details, and weekly-plan lifecycle cleanup.

**Architecture:** Keep the existing backend shopping-list object model and finish the incomplete mobile flow around it. Split the work into three thin vertical slices: backend lifecycle behavior, mobile create flow, and mobile index/details/delete behavior, each protected by tests written first.

**Tech Stack:** .NET 8, ASP.NET Core Web API, EF Core, .NET MAUI, xUnit

---

## File Structure

- `src/DietPlanner.Application/Plans/Commands/SubmitWeeklyPlanCommand.cs`
  - enforce cleanup of previous-plan shopping lists when a new weekly plan becomes active
- `src/DietPlanner.Application/Shopping/Commands/DeleteShoppingListsForPlanCommand.cs`
  - reused plan-linked list cleanup behavior
- `src/DietPlanner.Mobile/Services/ShoppingListApiClient.cs`
  - existing HTTP client contract for list/create/delete/toggle operations
- `src/DietPlanner.Mobile/ViewModels/ShoppingListViewModel.cs`
  - root index/details behavior, refresh, delete, navigation
- `src/DietPlanner.Mobile/ViewModels/ShoppingListCreateViewModel.cs`
  - preset step, builder step, selected ingredient gathering, save flow
- `src/DietPlanner.Mobile/Views/ShoppingListPage.xaml`
  - saved-lists index + details UI
- `src/DietPlanner.Mobile/Views/ShoppingListCreatePage.xaml`
  - create preset/builder flow UI
- `tests/DietPlanner.Api.Tests/Plans/*`
  - weekly-plan cleanup regression coverage
- `tests/DietPlanner.Mobile.Tests/ShoppingListViewModelTests.cs`
  - list index/details/delete flow tests
- `tests/DietPlanner.Mobile.Tests/ShoppingListCreateViewModelTests.cs`
  - preset, builder, and save flow tests

## Task 1: Add weekly-plan shopping-list cleanup regression coverage

**Files:**
- Modify: `tests/DietPlanner.Api.Tests/Plans/SubmitWeeklyPlanTests.cs`
- Check: `src/DietPlanner.Application/Plans/Commands/SubmitWeeklyPlanCommand.cs`
- Check: `src/DietPlanner.Application/Shopping/Commands/DeleteShoppingListsForPlanCommand.cs`

- [ ] **Step 1: Write the failing test**

Add a test that proves submitting a new weekly plan deletes shopping lists for the replaced plan:

```csharp
[Fact]
public async Task SubmitWeeklyPlan_ShouldDeleteShoppingListsFromPreviousActivePlan()
{
    await using var app = await PlansApiFactory.WithSubmittedPlanAsync();
    using var client = await app.CreateAuthenticatedClientAsync();

    var submitResponse = await client.PostAsJsonAsync("/api/plans/submit", new SubmitWeeklyPlanRequest("shared"));
    submitResponse.EnsureSuccessStatusCode();

    var shoppingListsResponse = await client.GetAsync("/api/shopping-lists");
    var lists = await shoppingListsResponse.Content.ReadFromJsonAsync<IReadOnlyList<ShoppingListSummaryResponse>>();

    lists.Should().NotBeNull();
    lists.Should().BeEmpty();
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:  
`dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter SubmitWeeklyPlan_ShouldDeleteShoppingListsFromPreviousActivePlan`

Expected: FAIL because the current submit path does not clear prior-plan shopping lists.

- [ ] **Step 3: Write minimal implementation**

Update the submit command handler to remove shopping lists linked to the plan being replaced before saving the new active plan:

```csharp
if (existingActivePlan is not null)
{
    await _deleteShoppingListsForPlanHandler.DeleteAsync(
        new DeleteShoppingListsForPlanCommand(existingActivePlan.Id),
        cancellationToken);
}
```

Inject `DeleteShoppingListsForPlanHandler` into the submit handler the same way it is already used in replace/copy flows.

- [ ] **Step 4: Run test to verify it passes**

Run:  
`dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter SubmitWeeklyPlan_ShouldDeleteShoppingListsFromPreviousActivePlan`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add tests/DietPlanner.Api.Tests/Plans/SubmitWeeklyPlanTests.cs src/DietPlanner.Application/Plans/Commands/SubmitWeeklyPlanCommand.cs
git commit -m "Clear old shopping lists when submitting new plan"
```

## Task 2: Add delete-from-index and delete-from-details mobile tests

**Files:**
- Modify: `tests/DietPlanner.Mobile.Tests/ShoppingListViewModelTests.cs`
- Check: `src/DietPlanner.Mobile/ViewModels/ShoppingListViewModel.cs`

- [ ] **Step 1: Write the failing tests**

Add tests for row delete, details delete, and refresh behavior:

```csharp
[Fact]
public async Task DeleteListFromIndex_ShouldRemoveListAndStayOnIndex()
{
    var api = new FakeShoppingListApiClient(
        lists:
        [
            new ShoppingListSummaryDto(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Week groceries", "2026-05-29", 4),
            new ShoppingListSummaryDto(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Dinner only", "2026-05-29", 2)
        ]);
    var prompts = new RecordingPromptService(confirmResult: true);
    var viewModel = new ShoppingListViewModel(api, new RecordingNavigator(), prompts);

    await viewModel.LoadAsync();
    await viewModel.DeleteListAsync(viewModel.Lists.First());

    Assert.Single(viewModel.Lists);
    Assert.Null(viewModel.SelectedList);
}

[Fact]
public async Task DeleteSelectedList_ShouldNavigateBackToIndexAndRefresh()
{
    var listId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    var api = new FakeShoppingListApiClient(
        lists: [new ShoppingListSummaryDto(listId, "Week groceries", "2026-05-29", 4)],
        details: new ShoppingListDetailsDto(listId, "Week groceries", []));
    var prompts = new RecordingPromptService(confirmResult: true);
    var viewModel = new ShoppingListViewModel(api, new RecordingNavigator(), prompts);

    await viewModel.LoadAsync();
    await viewModel.SelectListAsync(viewModel.Lists.Single());
    await viewModel.DeleteSelectedListAsync();

    Assert.Null(viewModel.SelectedList);
    Assert.Empty(viewModel.Lists);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:  
`dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter "DeleteListFromIndex|DeleteSelectedList"`

Expected: FAIL because the view model has no delete commands or prompt integration yet.

- [ ] **Step 3: Write minimal implementation**

Extend `ShoppingListViewModel` with:

```csharp
public AsyncCommand DeleteListCommand { get; }
public AsyncCommand DeleteSelectedListCommand { get; }

public async Task DeleteListAsync(ShoppingListSummaryViewModel? list, CancellationToken cancellationToken = default)
public async Task DeleteSelectedListAsync(CancellationToken cancellationToken = default)
```

Inject a prompt service:

```csharp
private readonly IUserPromptService _promptService;
```

Implementation rules:

- confirm before delete
- call `_shoppingListApiClient.DeleteAsync(...)`
- reload the list index after success
- if deleting selected list, clear `SelectedList` and `Items`

- [ ] **Step 4: Run tests to verify they pass**

Run:  
`dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter "DeleteListFromIndex|DeleteSelectedList"`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add tests/DietPlanner.Mobile.Tests/ShoppingListViewModelTests.cs src/DietPlanner.Mobile/ViewModels/ShoppingListViewModel.cs
git commit -m "Add mobile shopping list delete flow"
```

## Task 3: Add create-flow tests for presets, selection, and save

**Files:**
- Modify: `tests/DietPlanner.Mobile.Tests/ShoppingListCreateViewModelTests.cs`
- Check: `src/DietPlanner.Mobile/ViewModels/ShoppingListCreateViewModel.cs`
- Check: `src/DietPlanner.Mobile/Services/ShoppingListApiClient.cs`

- [ ] **Step 1: Write the failing tests**

Add tests for preset selection and save:

```csharp
[Fact]
public async Task SelectFullWeekPreset_ShouldPreselectAllIngredients()
{
    var api = new FakeShoppingListApiClient(createOptions: BuildCreateOptions());
    var viewModel = new ShoppingListCreateViewModel(api, new RecordingNavigator());

    await viewModel.LoadAsync();
    viewModel.SelectPresetCommand.Execute(ShoppingListCreatePreset.FullWeek);

    Assert.All(
        viewModel.Days.SelectMany(day => day.Meals).SelectMany(meal => meal.Ingredients),
        ingredient => Assert.True(ingredient.IsSelected));
}

[Fact]
public async Task SaveAsync_ShouldSendSelectedIngredientsAndNavigateBackToIndex()
{
    var api = new FakeShoppingListApiClient(createOptions: BuildCreateOptions());
    var navigator = new RecordingNavigator();
    var viewModel = new ShoppingListCreateViewModel(api, navigator);

    await viewModel.LoadAsync();
    viewModel.SelectPresetCommand.Execute(ShoppingListCreatePreset.FullWeek);
    await viewModel.SaveAsync();

    Assert.Equal("shopping-list", navigator.LastRoute);
    Assert.NotEmpty(api.CreatedIngredientKeys);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:  
`dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter "SelectFullWeekPreset|SaveAsync_ShouldSendSelectedIngredients"`

Expected: FAIL because presets and save flow are not implemented.

- [ ] **Step 3: Write minimal implementation**

Extend `ShoppingListCreateViewModel` with:

```csharp
public AsyncCommand SelectPresetCommand { get; }
public AsyncCommand SaveCommand { get; }
public ShoppingListCreatePreset SelectedPreset { get; private set; }
public bool IsPresetStep { get; private set; }
public bool IsBuilderStep => !IsPresetStep;

public async Task SaveAsync(CancellationToken cancellationToken = default)
```

Define a preset enum:

```csharp
public enum ShoppingListCreatePreset
{
    FullWeek,
    SelectedDays,
    Custom
}
```

Implementation rules:

- `FullWeek` selects every ingredient and advances to builder
- `Custom` advances to builder without forcing selection
- `SelectedDays` marks day-selection mode before builder
- `SaveAsync` builds `CreateShoppingListIngredientRequest` values from selected ingredients and calls:

```csharp
await _shoppingListApiClient.CreateAsync("Shopping list", selectedIngredients, cancellationToken);
await _navigator.GoToAsync("//shopping-list");
```

- [ ] **Step 4: Run tests to verify they pass**

Run:  
`dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter "SelectFullWeekPreset|SaveAsync_ShouldSendSelectedIngredients"`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add tests/DietPlanner.Mobile.Tests/ShoppingListCreateViewModelTests.cs src/DietPlanner.Mobile/ViewModels/ShoppingListCreateViewModel.cs
git commit -m "Add shopping list create preset and save flow"
```

## Task 4: Wire the create page UI to the two-step flow

**Files:**
- Modify: `src/DietPlanner.Mobile/Views/ShoppingListCreatePage.xaml`
- Modify: `src/DietPlanner.Mobile/Views/ShoppingListCreatePage.xaml.cs`
- Modify: `src/DietPlanner.Mobile/ViewModels/ShoppingListCreateViewModel.cs`

- [ ] **Step 1: Write the failing test**

Add a view model test proving the screen state changes from preset step to builder step:

```csharp
[Fact]
public async Task SelectCustomPreset_ShouldShowBuilderStep()
{
    var api = new FakeShoppingListApiClient(createOptions: BuildCreateOptions());
    var viewModel = new ShoppingListCreateViewModel(api, new RecordingNavigator());

    await viewModel.LoadAsync();
    viewModel.SelectPresetCommand.Execute(ShoppingListCreatePreset.Custom);

    Assert.False(viewModel.IsPresetStep);
    Assert.True(viewModel.IsBuilderStep);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:  
`dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter SelectCustomPreset_ShouldShowBuilderStep`

Expected: FAIL until the view model exposes screen-step state.

- [ ] **Step 3: Write minimal implementation**

Update `ShoppingListCreatePage.xaml` to render two states:

```xml
<VerticalStackLayout IsVisible="{Binding IsPresetStep}">
  <Button Command="{Binding SelectPresetCommand}" CommandParameter="{x:Static viewModels:ShoppingListCreatePreset.FullWeek}" Text="Full week" />
  <Button Command="{Binding SelectPresetCommand}" CommandParameter="{x:Static viewModels:ShoppingListCreatePreset.SelectedDays}" Text="Selected days" />
  <Button Command="{Binding SelectPresetCommand}" CommandParameter="{x:Static viewModels:ShoppingListCreatePreset.Custom}" Text="Custom" />
</VerticalStackLayout>

<CollectionView IsVisible="{Binding IsBuilderStep}" ItemsSource="{Binding Days}">
  <!-- existing grouped meal/ingredient selection UI -->
</CollectionView>

<Button IsVisible="{Binding IsBuilderStep}" Command="{Binding SaveCommand}" Text="Save list" />
```

In `ShoppingListCreatePage.xaml.cs`, keep `LoadAsync()` on first appearance only.

- [ ] **Step 4: Run test to verify it passes**

Run:  
`dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter SelectCustomPreset_ShouldShowBuilderStep`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/DietPlanner.Mobile/Views/ShoppingListCreatePage.xaml src/DietPlanner.Mobile/Views/ShoppingListCreatePage.xaml.cs src/DietPlanner.Mobile/ViewModels/ShoppingListCreateViewModel.cs tests/DietPlanner.Mobile.Tests/ShoppingListCreateViewModelTests.cs
git commit -m "Wire shopping list two-step create UI"
```

## Task 5: Finish index/details UI wiring for delete and refresh

**Files:**
- Modify: `src/DietPlanner.Mobile/Views/ShoppingListPage.xaml`
- Modify: `src/DietPlanner.Mobile/ViewModels/ShoppingListViewModel.cs`
- Modify: `tests/DietPlanner.Mobile.Tests/ShoppingListViewModelTests.cs`

- [ ] **Step 1: Write the failing test**

Add a view model regression for toggle preserving selected details state:

```csharp
[Fact]
public async Task ToggleItemAsync_ShouldRefreshSelectedListDetails()
{
    var listId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    var api = new FakeShoppingListApiClient(
        lists: [new ShoppingListSummaryDto(listId, "Week groceries", "2026-05-29", 1)],
        details: new ShoppingListDetailsDto(listId, "Week groceries",
        [
            new ShoppingListSummaryItemDto(Guid.NewGuid(), "Oats", 80m, "g", false)
        ]),
        toggledDetails: new ShoppingListDetailsDto(listId, "Week groceries",
        [
            new ShoppingListSummaryItemDto(Guid.NewGuid(), "Oats", 80m, "g", true)
        ]));
    var viewModel = new ShoppingListViewModel(api, new RecordingNavigator(), new RecordingPromptService(true));

    await viewModel.LoadAsync();
    await viewModel.SelectListAsync(viewModel.Lists.Single());
    await viewModel.ToggleItemAsync(viewModel.Items.Single());

    Assert.True(viewModel.Items.Single().IsChecked);
    Assert.Equal("Week groceries", viewModel.SelectedList!.Name);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:  
`dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter ToggleItemAsync_ShouldRefreshSelectedListDetails`

Expected: FAIL if refresh logic resets state incorrectly while wiring delete/index behavior.

- [ ] **Step 3: Write minimal implementation**

Update `ShoppingListPage.xaml`:

```xml
<Button
    Command="{Binding DeleteListCommand}"
    CommandParameter="{Binding .}"
    Text="Delete" />

<Button
    IsVisible="{Binding ShowListDetails}"
    Command="{Binding DeleteSelectedListCommand}"
    Text="Delete list" />

<Button
    IsVisible="{Binding ShowListDetails}"
    Command="{Binding BackToListsCommand}"
    Text="Back" />
```

Add to the view model:

```csharp
public AsyncCommand BackToListsCommand { get; }

private void BackToLists()
{
    SelectedList = null;
    Items.Clear();
    OnPropertyChanged(nameof(ShowEmptyState));
}
```

Ensure `ToggleItemAsync()` reapplies item details without losing the current selected list header.

- [ ] **Step 4: Run test to verify it passes**

Run:  
`dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter ToggleItemAsync_ShouldRefreshSelectedListDetails`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/DietPlanner.Mobile/Views/ShoppingListPage.xaml src/DietPlanner.Mobile/ViewModels/ShoppingListViewModel.cs tests/DietPlanner.Mobile.Tests/ShoppingListViewModelTests.cs
git commit -m "Finish shopping list index and details interactions"
```

## Task 6: Run broad verification and ship

**Files:**
- Modify if needed: any failing test targets discovered during verification

- [ ] **Step 1: Run focused mobile tests**

Run:  
`dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter "ShoppingListViewModelTests|ShoppingListCreateViewModelTests"`

Expected: PASS

- [ ] **Step 2: Run focused API tests**

Run:  
`dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter "ShoppingListSetTests|SubmitWeeklyPlanTests"`

Expected: PASS

- [ ] **Step 3: Run full solution tests**

Run:  
`dotnet test DietPlanner.sln`

Expected: PASS across mobile, domain, and api test projects.

- [ ] **Step 4: Build mobile and API**

Run:  
`dotnet build src/DietPlanner.Api/DietPlanner.Api.csproj`  
`dotnet build src/DietPlanner.Mobile/DietPlanner.Mobile.csproj -f net8.0`

Expected: both succeed

- [ ] **Step 5: Commit**

```bash
git add .
git commit -m "Complete shopping list flow"
```

- [ ] **Step 6: Push**

```bash
git push origin main
```

- [ ] **Step 7: Verify deployment and app behavior**

Run:

```bash
railway.cmd status
railway.cmd logs --service api --lines 120
```

Manual verification target:

- Shopping tab opens on saved lists index
- create flow completes and returns a saved list
- delete works from index and details
- old lists disappear after submitting a new weekly plan
