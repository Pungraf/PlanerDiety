# Two-Week Planning Lifecycle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add explicit `current` and `future` weekly plans, Friday-only future-week generation, Sunday rollover, Home week switching, and plan-scoped shopping/edit flows.

**Architecture:** Keep the existing EF Core and weekly-plan generator approach, but stop modeling app behavior as a single implicit readable plan. Introduce a planning-state service that resolves rollover, exposes current/future plan state, and drives both API and mobile behavior. Existing plan edits and shopping-list flows become plan-id targeted instead of assuming a single current week.

**Tech Stack:** .NET 8, ASP.NET Core controllers, EF Core SQLite/PostgreSQL, xUnit/FluentAssertions, .NET MAUI XAML/ViewModels.

---

## File Structure

Modify:
- `src/DietPlanner.Domain/Entities/WeeklyPlan.cs` - add explicit future/current transitions and allow deactivation/removal-safe semantics.
- `src/DietPlanner.Domain/Enums/WeeklyPlanStatus.cs` - replace or extend status values for `Current` and `Future`.
- `src/DietPlanner.Application/Abstractions/IApplicationDbContext.cs` - expose current/future lookup, plan-by-id lookup, and week cleanup helpers.
- `src/DietPlanner.Infrastructure/Persistence/DietPlannerDbContext.cs` - implement current/future queries, plan deletion, and user plan lookups by start date.
- `src/DietPlanner.Application/Plans/Queries/GetCurrentPlanQuery.cs` - replace single-plan bootstrap logic with planning-state load.
- `src/DietPlanner.Application/Plans/Commands/ActivateDraftCommand.cs` - remove or redirect obsolete draft activation path.
- `src/DietPlanner.Application/Plans/Commands/ReplaceMealCommand.cs` - target explicit plan id.
- `src/DietPlanner.Application/Plans/Commands/CopyDayCommand.cs` - target explicit plan id.
- `src/DietPlanner.Application/Shopping/Commands/DeleteShoppingListsForPlanCommand.cs` - reuse for Sunday cleanup.
- `src/DietPlanner.Api/Controllers/PlansController.cs` - expose planning-state and future-generation endpoints.
- `src/DietPlanner.Api/Extensions/ServiceCollectionExtensions.cs` - register planning-state services/handlers.
- `src/DietPlanner.Mobile/Services/PlansApiClient.cs` - consume planning-state and generate-future endpoints.
- `src/DietPlanner.Mobile/Services/ShoppingListApiClient.cs` - send selected plan id context.
- `src/DietPlanner.Mobile/ViewModels/HomeViewModel.cs` - add current/future selector, Friday generate button, selected-plan context.
- `src/DietPlanner.Mobile/ViewModels/ShoppingListViewModel.cs` - load lists by selected plan id instead of implicit current plan.
- `src/DietPlanner.Mobile/ViewModels/ShoppingListCreateViewModel.cs` - create lists for selected plan id.
- `src/DietPlanner.Mobile/Services/MealSearchContextStore.cs` - carry selected plan id into meal replacement flow.
- `src/DietPlanner.Mobile/Services/MealDetailsContextStore.cs` - optionally carry selected plan id for return-safe context.
- `src/DietPlanner.Mobile/Views/HomePage.xaml` - add week selector and generate-next-week action.
- `tests/DietPlanner.Api.Tests/TestData.cs` - support current/future week fixtures.
- `tests/DietPlanner.Mobile.Tests/HomeViewModelTests.cs` - cover generation and switching behavior.
- `tests/DietPlanner.Mobile.Tests/ShoppingListViewModelTests.cs` - cover selected plan context.
- `tests/DietPlanner.Mobile.Tests/ShoppingListCreateViewModelTests.cs` - cover plan-scoped create flow.

Create:
- `src/DietPlanner.Application/Plans/Planning/PlanningStateService.cs`
- `src/DietPlanner.Application/Plans/Planning/PlanningStateDto.cs`
- `src/DietPlanner.Application/Plans/Queries/GetPlanningStateQuery.cs`
- `src/DietPlanner.Application/Plans/Commands/GenerateFutureWeekCommand.cs`
- `src/DietPlanner.Application/Plans/Planning/WeekBoundaryCalculator.cs`
- `tests/DietPlanner.Domain.Tests/WeeklyPlanLifecycleTests.cs`
- `tests/DietPlanner.Api.Tests/Plans/GetPlanningStateTests.cs`
- `tests/DietPlanner.Api.Tests/Plans/GenerateFutureWeekTests.cs`
- `tests/DietPlanner.Api.Tests/Plans/SundayRolloverTests.cs`

---

### Task 1: Model Current/Future Weekly Plan Status

**Files:**
- Modify: `src/DietPlanner.Domain/Enums/WeeklyPlanStatus.cs`
- Modify: `src/DietPlanner.Domain/Entities/WeeklyPlan.cs`
- Test: `tests/DietPlanner.Domain.Tests/WeeklyPlanLifecycleTests.cs`

- [ ] **Step 1: Write the failing domain tests**

Add:

```csharp
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;
using FluentAssertions;

namespace DietPlanner.Domain.Tests;

public sealed class WeeklyPlanLifecycleTests
{
    [Fact]
    public void CreateCurrent_ShouldSetCurrentStatus()
    {
        var plan = WeeklyPlan.CreateCurrent(Guid.NewGuid(), new DateOnly(2026, 5, 31), DinnerMode.BreakfastStyle);

        plan.Status.Should().Be(WeeklyPlanStatus.Current);
    }

    [Fact]
    public void CreateFuture_ShouldSetFutureStatus()
    {
        var plan = WeeklyPlan.CreateFuture(Guid.NewGuid(), new DateOnly(2026, 6, 7), DinnerMode.BreakfastStyle);

        plan.Status.Should().Be(WeeklyPlanStatus.Future);
    }

    [Fact]
    public void PromoteFutureToCurrent_ShouldChangeStatus()
    {
        var plan = WeeklyPlan.CreateFuture(Guid.NewGuid(), new DateOnly(2026, 6, 7), DinnerMode.BreakfastStyle);

        plan.PromoteFutureToCurrent();

        plan.Status.Should().Be(WeeklyPlanStatus.Current);
    }
}
```

- [ ] **Step 2: Run the domain test to verify it fails**

Run:

```powershell
dotnet test tests/DietPlanner.Domain.Tests/DietPlanner.Domain.Tests.csproj --filter WeeklyPlanLifecycleTests
```

Expected: FAIL because `CreateCurrent`, `CreateFuture`, `PromoteFutureToCurrent`, or new status values do not exist yet.

- [ ] **Step 3: Implement the minimal domain lifecycle API**

Update the enum:

```csharp
public enum WeeklyPlanStatus
{
    Current = 1,
    Future = 2
}
```

Update `WeeklyPlan`:

```csharp
public static WeeklyPlan CreateCurrent(Guid userId, DateOnly startDate, DinnerMode dinnerMode)
{
    return new WeeklyPlan(
        Guid.NewGuid(),
        Guard.AgainstEmpty(userId, nameof(userId)),
        startDate,
        Guard.AgainstUndefinedEnum(dinnerMode, nameof(dinnerMode)),
        WeeklyPlanStatus.Current);
}

public static WeeklyPlan CreateFuture(Guid userId, DateOnly startDate, DinnerMode dinnerMode)
{
    return new WeeklyPlan(
        Guid.NewGuid(),
        Guard.AgainstEmpty(userId, nameof(userId)),
        startDate,
        Guard.AgainstUndefinedEnum(dinnerMode, nameof(dinnerMode)),
        WeeklyPlanStatus.Future);
}

public void PromoteFutureToCurrent()
{
    if (Status != WeeklyPlanStatus.Future)
    {
        throw new InvalidOperationException("Only future plans can be promoted.");
    }

    Status = WeeklyPlanStatus.Current;
}
```

- [ ] **Step 4: Run the domain test to verify it passes**

Run:

```powershell
dotnet test tests/DietPlanner.Domain.Tests/DietPlanner.Domain.Tests.csproj --filter WeeklyPlanLifecycleTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/DietPlanner.Domain/Enums/WeeklyPlanStatus.cs src/DietPlanner.Domain/Entities/WeeklyPlan.cs tests/DietPlanner.Domain.Tests/WeeklyPlanLifecycleTests.cs
git commit -m "feat: model current and future weekly plans"
```

### Task 2: Add Planning-State and Sunday Rollover Service

**Files:**
- Create: `src/DietPlanner.Application/Plans/Planning/WeekBoundaryCalculator.cs`
- Create: `src/DietPlanner.Application/Plans/Planning/PlanningStateDto.cs`
- Create: `src/DietPlanner.Application/Plans/Planning/PlanningStateService.cs`
- Modify: `src/DietPlanner.Application/Abstractions/IApplicationDbContext.cs`
- Modify: `src/DietPlanner.Infrastructure/Persistence/DietPlannerDbContext.cs`
- Modify: `src/DietPlanner.Application/Shopping/Commands/DeleteShoppingListsForPlanCommand.cs`
- Test: `tests/DietPlanner.Api.Tests/Plans/SundayRolloverTests.cs`

- [ ] **Step 1: Write the failing rollover tests**

Add:

```csharp
[Fact]
public async Task GetPlanningState_ShouldPromoteFuturePlanOnSunday()
{
    var clockDate = new DateOnly(2026, 6, 7); // Sunday
    var user = await TestData.CreateUserAsync(App);
    var oldCurrent = await TestData.CreateWeeklyPlanAsync(App, user.Id, new DateOnly(2026, 5, 31), WeeklyPlanStatus.Current);
    var future = await TestData.CreateWeeklyPlanAsync(App, user.Id, new DateOnly(2026, 6, 7), WeeklyPlanStatus.Future);

    var state = await App.GetRequiredService<PlanningStateService>()
        .GetAsync(user.Id, clockDate, CancellationToken.None);

    state.CurrentPlanId.Should().Be(future.Id);
    state.FuturePlanId.Should().BeNull();
}

[Fact]
public async Task GetPlanningState_ShouldAutoGenerateCurrentPlanOnSundayWhenFutureMissing()
{
    var clockDate = new DateOnly(2026, 6, 7); // Sunday
    var user = await TestData.CreateUserAsync(App);
    await TestData.CreateWeeklyPlanAsync(App, user.Id, new DateOnly(2026, 5, 31), WeeklyPlanStatus.Current);

    var state = await App.GetRequiredService<PlanningStateService>()
        .GetAsync(user.Id, clockDate, CancellationToken.None);

    state.CurrentStartDate.Should().Be(new DateOnly(2026, 6, 7));
    state.FuturePlanId.Should().BeNull();
}
```

- [ ] **Step 2: Run the rollover tests to verify they fail**

Run:

```powershell
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter SundayRolloverTests
```

Expected: FAIL because `PlanningStateService` and supporting db methods do not exist yet.

- [ ] **Step 3: Implement the date-boundary and planning-state service**

Create the boundary helper:

```csharp
public static class WeekBoundaryCalculator
{
    public static DateOnly GetWeekStart(DateOnly date)
    {
        return date.AddDays(-(int)date.DayOfWeek);
    }

    public static DateOnly GetNextWeekStart(DateOnly date)
    {
        return GetWeekStart(date).AddDays(7);
    }

    public static bool CanGenerateFutureWeek(DateOnly today, bool hasFuturePlan)
    {
        return !hasFuturePlan && today.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;
    }
}
```

Create the service core:

```csharp
public async Task<PlanningStateDto?> GetAsync(Guid userId, DateOnly today, CancellationToken cancellationToken)
{
    var current = await _dbContext.FindCurrentWeeklyPlanAsync(userId, cancellationToken);
    var future = await _dbContext.FindFutureWeeklyPlanAsync(userId, cancellationToken);

    if (today.DayOfWeek == DayOfWeek.Sunday)
    {
        (current, future) = await RollForwardIfNeededAsync(userId, today, current, future, cancellationToken);
    }

    if (current is null)
    {
        current = await BootstrapCurrentAsync(userId, today, cancellationToken);
        future = await _dbContext.FindFutureWeeklyPlanAsync(userId, cancellationToken);
    }

    return current is null ? null : await BuildStateAsync(today, current, future, cancellationToken);
}
```

- [ ] **Step 4: Implement persistence helpers and cleanup hooks**

Add to `IApplicationDbContext` and `DietPlannerDbContext`:

```csharp
Task<WeeklyPlan?> FindCurrentWeeklyPlanAsync(Guid userId, CancellationToken cancellationToken);
Task<WeeklyPlan?> FindFutureWeeklyPlanAsync(Guid userId, CancellationToken cancellationToken);
Task<WeeklyPlan?> FindWeeklyPlanByIdAsync(Guid userId, Guid planId, CancellationToken cancellationToken);
Task DeleteWeeklyPlanAsync(WeeklyPlan plan, CancellationToken cancellationToken);
```

Use status-based queries:

```csharp
public Task<WeeklyPlan?> FindCurrentWeeklyPlanAsync(Guid userId, CancellationToken cancellationToken)
{
    return WeeklyPlans
        .Include(plan => plan.Days)
            .ThenInclude(day => day.MealSlots)
        .SingleOrDefaultAsync(plan => plan.UserId == userId && plan.Status == WeeklyPlanStatus.Current, cancellationToken);
}
```

- [ ] **Step 5: Run the rollover tests to verify they pass**

Run:

```powershell
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter SundayRolloverTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/DietPlanner.Application/Plans/Planning src/DietPlanner.Application/Abstractions/IApplicationDbContext.cs src/DietPlanner.Infrastructure/Persistence/DietPlannerDbContext.cs src/DietPlanner.Application/Shopping/Commands/DeleteShoppingListsForPlanCommand.cs tests/DietPlanner.Api.Tests/Plans/SundayRolloverTests.cs
git commit -m "feat: add planning state and sunday rollover"
```

### Task 3: Add Planning-State and Generate-Future API Endpoints

**Files:**
- Create: `src/DietPlanner.Application/Plans/Queries/GetPlanningStateQuery.cs`
- Create: `src/DietPlanner.Application/Plans/Commands/GenerateFutureWeekCommand.cs`
- Modify: `src/DietPlanner.Api/Controllers/PlansController.cs`
- Modify: `src/DietPlanner.Api/Extensions/ServiceCollectionExtensions.cs`
- Modify: `src/DietPlanner.Application/Plans/Queries/GetCurrentPlanQuery.cs`
- Modify: `src/DietPlanner.Application/Plans/Commands/ActivateDraftCommand.cs`
- Test: `tests/DietPlanner.Api.Tests/Plans/GetPlanningStateTests.cs`
- Test: `tests/DietPlanner.Api.Tests/Plans/GenerateFutureWeekTests.cs`

- [ ] **Step 1: Write the failing API tests**

Add:

```csharp
[Fact]
public async Task GetPlanningState_ShouldReturnCurrentAndFuturePlans()
{
    var client = App.CreateClient();
    await App.AuthenticateAsync(client);

    var response = await client.GetAsync("/api/plans/state");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<PlanningStateResponse>();
    payload.Should().NotBeNull();
    payload!.CurrentPlan.Should().NotBeNull();
}

[Fact]
public async Task GenerateFutureWeek_ShouldCreateFuturePlanOnce()
{
    var client = App.CreateClient();
    await App.AuthenticateAsync(client);

    var first = await client.PostAsync("/api/plans/future/generate", null);
    var second = await client.PostAsync("/api/plans/future/generate", null);

    first.StatusCode.Should().Be(HttpStatusCode.OK);
    second.StatusCode.Should().Be(HttpStatusCode.Conflict);
}
```

- [ ] **Step 2: Run the API tests to verify they fail**

Run:

```powershell
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter "GetPlanningStateTests|GenerateFutureWeekTests"
```

Expected: FAIL because endpoints and handlers do not exist yet.

- [ ] **Step 3: Implement planning-state query and future-generation handler**

Create the query:

```csharp
public sealed record GetPlanningStateQuery(Guid UserId, DateOnly Today);

public sealed class GetPlanningStateHandler
{
    private readonly PlanningStateService _planningStateService;

    public GetPlanningStateHandler(PlanningStateService planningStateService)
    {
        _planningStateService = planningStateService;
    }

    public Task<PlanningStateDto?> HandleAsync(GetPlanningStateQuery query, CancellationToken cancellationToken)
    {
        return _planningStateService.GetAsync(query.UserId, query.Today, cancellationToken);
    }
}
```

Create the command:

```csharp
public sealed record GenerateFutureWeekCommand(Guid UserId, DateOnly Today);

public sealed class GenerateFutureWeekHandler
{
    private readonly PlanningStateService _planningStateService;

    public GenerateFutureWeekHandler(PlanningStateService planningStateService)
    {
        _planningStateService = planningStateService;
    }

    public Task<PlanningStateDto?> HandleAsync(GenerateFutureWeekCommand command, CancellationToken cancellationToken)
    {
        return _planningStateService.GenerateFutureAsync(command.UserId, command.Today, cancellationToken);
    }
}
```

- [ ] **Step 4: Replace obsolete single-current API surface**

Update controller routes:

```csharp
[HttpGet("state")]
public async Task<ActionResult<PlanningStateResponse>> GetState(CancellationToken cancellationToken)
{
    var userId = GetCurrentUserId();
    if (userId is null)
    {
        return Unauthorized();
    }

    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var state = await _getPlanningStateHandler.HandleAsync(new GetPlanningStateQuery(userId.Value, today), cancellationToken);
    return state is null ? NotFound() : Ok(PlanningStateResponse.From(state));
}

[HttpPost("future/generate")]
public async Task<ActionResult<PlanningStateResponse>> GenerateFutureWeek(CancellationToken cancellationToken)
{
    var userId = GetCurrentUserId();
    if (userId is null)
    {
        return Unauthorized();
    }

    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var state = await _generateFutureWeekHandler.HandleAsync(new GenerateFutureWeekCommand(userId.Value, today), cancellationToken);
    return state is null ? Conflict() : Ok(PlanningStateResponse.From(state));
}
```

- [ ] **Step 5: Run the API tests to verify they pass**

Run:

```powershell
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter "GetPlanningStateTests|GenerateFutureWeekTests"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/DietPlanner.Application/Plans/Queries/GetPlanningStateQuery.cs src/DietPlanner.Application/Plans/Commands/GenerateFutureWeekCommand.cs src/DietPlanner.Api/Controllers/PlansController.cs src/DietPlanner.Api/Extensions/ServiceCollectionExtensions.cs src/DietPlanner.Application/Plans/Queries/GetCurrentPlanQuery.cs src/DietPlanner.Application/Plans/Commands/ActivateDraftCommand.cs tests/DietPlanner.Api.Tests/Plans/GetPlanningStateTests.cs tests/DietPlanner.Api.Tests/Plans/GenerateFutureWeekTests.cs
git commit -m "feat: expose planning state and future generation api"
```

### Task 4: Target Replace/Copy and Shopping Flows by Plan Id

**Files:**
- Modify: `src/DietPlanner.Application/Plans/Commands/ReplaceMealCommand.cs`
- Modify: `src/DietPlanner.Application/Plans/Commands/CopyDayCommand.cs`
- Modify: `src/DietPlanner.Api/Controllers/PlansController.cs`
- Modify: `src/DietPlanner.Mobile/Services/PlansApiClient.cs`
- Modify: `src/DietPlanner.Mobile/Services/ShoppingListApiClient.cs`
- Modify: `src/DietPlanner.Mobile/Services/MealSearchContextStore.cs`
- Modify: `src/DietPlanner.Mobile/ViewModels/ShoppingListViewModel.cs`
- Modify: `src/DietPlanner.Mobile/ViewModels/ShoppingListCreateViewModel.cs`
- Test: `tests/DietPlanner.Api.Tests/Plans/ReplaceMealTests.cs`
- Test: `tests/DietPlanner.Api.Tests/Plans/CopyDayTests.cs`
- Test: `tests/DietPlanner.Mobile.Tests/ShoppingListViewModelTests.cs`
- Test: `tests/DietPlanner.Mobile.Tests/ShoppingListCreateViewModelTests.cs`

- [ ] **Step 1: Write the failing plan-targeting tests**

Add:

```csharp
[Fact]
public async Task ReplaceMeal_ShouldOnlyUpdateSpecifiedPlan()
{
    var client = App.CreateClient();
    await App.AuthenticateAsync(client);
    var state = await TestData.CreateCurrentAndFuturePlansAsync(App);

    var response = await client.PutAsJsonAsync(
        $"/api/plans/{state.FuturePlanId}/days/2026-06-08/slots/breakfast",
        new { mealId = TestData.AlternateMealId, deleteLinkedShoppingLists = false });

    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

And for shopping context:

```csharp
[Fact]
public async Task LoadAsync_ShouldRequestListsForSelectedPlan()
{
    var apiClient = new FakeShoppingListApiClient();
    var viewModel = new ShoppingListViewModel(apiClient, navigator, promptService);

    await viewModel.LoadAsync(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    apiClient.LastRequestedPlanId.Should().Be(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
}
```

- [ ] **Step 2: Run the targeted tests to verify they fail**

Run:

```powershell
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter "ReplaceMealTests|CopyDayTests"
dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter "ShoppingListViewModelTests|ShoppingListCreateViewModelTests"
```

Expected: FAIL because plan-id targeted APIs and mobile methods do not exist yet.

- [ ] **Step 3: Update commands and controller routes to use plan id**

Change the command records:

```csharp
public sealed record ReplaceMealCommand(Guid UserId, Guid PlanId, DateOnly Date, MealSlotType SlotType, Guid MealId, bool DeleteLinkedShoppingLists);

public sealed record CopyDayCommand(Guid UserId, Guid PlanId, DateOnly SourceDate, DateOnly TargetDate, bool DeleteLinkedShoppingLists);
```

Update routes:

```csharp
[HttpPut("{planId:guid}/days/{date}/slots/{slotType}")]
[HttpPost("{planId:guid}/copy-day")]
```

Handlers should resolve the plan with:

```csharp
var plan = await _dbContext.FindWeeklyPlanByIdAsync(command.UserId, command.PlanId, cancellationToken);
```

- [ ] **Step 4: Thread selected plan id through mobile clients and shopping flows**

Update mobile contracts:

```csharp
Task ReplaceMealAsync(Guid planId, DateOnly date, string slotType, Guid mealId, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default);
Task CopyDayAsync(Guid planId, DateOnly sourceDate, DateOnly targetDate, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default);
```

Update endpoints:

```csharp
$"api/plans/{planId}/days/{date:yyyy-MM-dd}/slots/{slotType}"
$"api/plans/{planId}/copy-day"
```

Add plan-id parameters to shopping APIs:

```csharp
Task<IReadOnlyList<ShoppingListSummaryDto>> GetShoppingListsAsync(Guid planId, CancellationToken cancellationToken = default);
Task<ShoppingListDetailsDto> GetShoppingListAsync(Guid planId, Guid listId, CancellationToken cancellationToken = default);
Task CreateShoppingListAsync(Guid planId, CreateShoppingListRequest request, CancellationToken cancellationToken = default);
```

- [ ] **Step 5: Run the targeted tests to verify they pass**

Run:

```powershell
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter "ReplaceMealTests|CopyDayTests"
dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter "ShoppingListViewModelTests|ShoppingListCreateViewModelTests"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/DietPlanner.Application/Plans/Commands/ReplaceMealCommand.cs src/DietPlanner.Application/Plans/Commands/CopyDayCommand.cs src/DietPlanner.Api/Controllers/PlansController.cs src/DietPlanner.Mobile/Services/PlansApiClient.cs src/DietPlanner.Mobile/Services/ShoppingListApiClient.cs src/DietPlanner.Mobile/Services/MealSearchContextStore.cs src/DietPlanner.Mobile/ViewModels/ShoppingListViewModel.cs src/DietPlanner.Mobile/ViewModels/ShoppingListCreateViewModel.cs tests/DietPlanner.Api.Tests/Plans/ReplaceMealTests.cs tests/DietPlanner.Api.Tests/Plans/CopyDayTests.cs tests/DietPlanner.Mobile.Tests/ShoppingListViewModelTests.cs tests/DietPlanner.Mobile.Tests/ShoppingListCreateViewModelTests.cs
git commit -m "feat: scope plan edits and shopping lists by plan id"
```

### Task 5: Add Home Current/Future Switcher and Generate Button

**Files:**
- Modify: `src/DietPlanner.Mobile/Services/PlansApiClient.cs`
- Modify: `src/DietPlanner.Mobile/ViewModels/HomeViewModel.cs`
- Modify: `src/DietPlanner.Mobile/Views/HomePage.xaml`
- Test: `tests/DietPlanner.Mobile.Tests/HomeViewModelTests.cs`

- [ ] **Step 1: Write the failing Home view-model tests**

Add:

```csharp
[Fact]
public async Task LoadAsync_ShouldShowGenerateButtonWhenAllowed()
{
    var apiClient = new FakePlansApiClient
    {
        PlanningState = new PlanningStateDto(
            CurrentPlan: FakeData.CurrentPlan(),
            FuturePlan: null,
            CanGenerateFutureWeek: true)
    };

    var viewModel = CreateViewModel(apiClient);

    await viewModel.LoadAsync();

    viewModel.CanGenerateFutureWeek.Should().BeTrue();
}

[Fact]
public async Task SwitchWeekCommand_ShouldChangeSelectedPlanToFuture()
{
    var apiClient = new FakePlansApiClient
    {
        PlanningState = FakeData.PlanningStateWithFuture()
    };

    var viewModel = CreateViewModel(apiClient);
    await viewModel.LoadAsync();

    viewModel.SelectFutureWeekCommand.Execute(null);

    viewModel.SelectedPlanId.Should().Be(apiClient.PlanningState.FuturePlan!.Id);
}
```

- [ ] **Step 2: Run the Home tests to verify they fail**

Run:

```powershell
dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter HomeViewModelTests
```

Expected: FAIL because planning-state DTOs, commands, and selected-week logic do not exist yet.

- [ ] **Step 3: Extend the mobile plans client to load planning state and generate future week**

Add:

```csharp
Task<PlanningStateDto> GetPlanningStateAsync(CancellationToken cancellationToken = default);
Task<PlanningStateDto> GenerateFutureWeekAsync(CancellationToken cancellationToken = default);
```

Implementation:

```csharp
using var request = CreateRequest(HttpMethod.Get, "api/plans/state");
using var response = await _httpClient.SendAsync(request, cancellationToken);
response.EnsureSuccessStatusCode();
return await response.Content.ReadFromJsonAsync<PlanningStateDto>(cancellationToken: cancellationToken)
    ?? throw new InvalidOperationException("Plans API returned an empty planning state response.");
```

- [ ] **Step 4: Rework HomeViewModel around selected current/future plan**

Add properties and commands:

```csharp
public bool CanGenerateFutureWeek { get; private set; }
public bool HasFutureWeek => FutureWeek is not null;
public Guid SelectedPlanId { get; private set; }
public AsyncCommand GenerateFutureWeekCommand { get; }
public AsyncCommand SelectCurrentWeekCommand { get; }
public AsyncCommand SelectFutureWeekCommand { get; }
```

Map selected plan:

```csharp
private async Task LoadAsync(Guid? preferredPlanId, DateOnly? preferredDate, CancellationToken cancellationToken)
{
    var state = await _plansApiClient.GetPlanningStateAsync(cancellationToken);
    CurrentWeek = MapPlan(state.CurrentPlan);
    FutureWeek = state.FuturePlan is null ? null : MapPlan(state.FuturePlan);
    CanGenerateFutureWeek = state.CanGenerateFutureWeek;
    SelectedPlan = ResolveSelectedPlan(preferredPlanId) ?? CurrentWeek;
    SelectedPlanId = SelectedPlan.Id;
    SyncVisibleDays(preferredDate);
}
```

- [ ] **Step 5: Run the Home tests to verify they pass**

Run:

```powershell
dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter HomeViewModelTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/DietPlanner.Mobile/Services/PlansApiClient.cs src/DietPlanner.Mobile/ViewModels/HomeViewModel.cs src/DietPlanner.Mobile/Views/HomePage.xaml tests/DietPlanner.Mobile.Tests/HomeViewModelTests.cs
git commit -m "feat: add home week switching and future generation"
```

### Task 6: Full Integration Verification

**Files:**
- Modify: `tests/DietPlanner.Api.Tests/TestData.cs`
- Test: `tests/DietPlanner.Api.Tests/Plans/GetPlanningStateTests.cs`
- Test: `tests/DietPlanner.Api.Tests/Plans/GenerateFutureWeekTests.cs`
- Test: `tests/DietPlanner.Api.Tests/Plans/SundayRolloverTests.cs`
- Test: `tests/DietPlanner.Mobile.Tests/HomeViewModelTests.cs`
- Test: `tests/DietPlanner.Mobile.Tests/ShoppingListViewModelTests.cs`
- Test: `tests/DietPlanner.Mobile.Tests/ShoppingListCreateViewModelTests.cs`

- [ ] **Step 1: Add end-to-end regression tests for Friday/Sunday lifecycle**

Add:

```csharp
[Fact]
public async Task FridayToSundayLifecycle_ShouldAllowGenerateThenPromoteFuture()
{
    var user = await TestData.CreateUserAsync(App);
    var friday = new DateOnly(2026, 6, 5);
    var sunday = new DateOnly(2026, 6, 7);

    var generated = await App.GetRequiredService<GenerateFutureWeekHandler>()
        .HandleAsync(new GenerateFutureWeekCommand(user.Id, friday), CancellationToken.None);

    generated!.FuturePlan.Should().NotBeNull();

    var rolled = await App.GetRequiredService<GetPlanningStateHandler>()
        .HandleAsync(new GetPlanningStateQuery(user.Id, sunday), CancellationToken.None);

    rolled!.FuturePlan.Should().BeNull();
    rolled.CurrentPlan.StartDate.Should().Be(sunday);
}
```

- [ ] **Step 2: Run the targeted API and mobile suites**

Run:

```powershell
dotnet test tests/DietPlanner.Api.Tests/DietPlanner.Api.Tests.csproj --filter "GetPlanningStateTests|GenerateFutureWeekTests|SundayRolloverTests|ReplaceMealTests|CopyDayTests"
dotnet test tests/DietPlanner.Mobile.Tests/DietPlanner.Mobile.Tests.csproj --filter "HomeViewModelTests|ShoppingListViewModelTests|ShoppingListCreateViewModelTests"
```

Expected: PASS.

- [ ] **Step 3: Run the full solution tests**

Run:

```powershell
dotnet test C:\Users\johny\PlanerDiety\.worktrees\diet-planner-mvp\DietPlanner.sln -m:1
```

Expected: PASS across domain, API, and mobile test projects.

- [ ] **Step 4: Build the API and mobile projects**

Run:

```powershell
dotnet build src/DietPlanner.Api/DietPlanner.Api.csproj
dotnet build src/DietPlanner.Mobile/DietPlanner.Mobile.csproj -f net8.0
dotnet build src/DietPlanner.Mobile/DietPlanner.Mobile.csproj -f net8.0-android -r android-arm64 -p:EnableMobileAndroid=true -p:EmbedAssembliesIntoApk=true -restore
```

Expected: all builds succeed.

- [ ] **Step 5: Commit**

```powershell
git add tests/DietPlanner.Api.Tests/TestData.cs tests/DietPlanner.Api.Tests/Plans tests/DietPlanner.Mobile.Tests/HomeViewModelTests.cs tests/DietPlanner.Mobile.Tests/ShoppingListViewModelTests.cs tests/DietPlanner.Mobile.Tests/ShoppingListCreateViewModelTests.cs
git commit -m "test: verify two-week planning lifecycle"
```
