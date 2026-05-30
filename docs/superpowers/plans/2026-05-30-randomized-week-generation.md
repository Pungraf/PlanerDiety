# Randomized Week Generation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace deterministic weekly meal rotation with randomized generation that preserves the current slot structure and meal-type rules while applying anti-repetition constraints.

**Architecture:** Keep the existing `WeeklyPlanGenerator` entry point and request model, but replace index-based rotation with constrained random selection per slot family. Introduce small internal helper methods for candidate filtering and cycle reuse instead of turning the generator into a generic solver.

**Tech Stack:** .NET 8, xUnit, FluentAssertions, existing domain/application planning classes.

---

## File Structure

Modify:
- `src/DietPlanner.Application/Planning/WeeklyPlanGenerator.cs` - replace deterministic rotation with constrained random selection logic
- `tests/DietPlanner.Domain.Tests/WeeklyPlanGeneratorRulesTests.cs` - update/add behavior tests for anti-repetition and valid fallback
- `tests/DietPlanner.Domain.Tests/WeeklyPlanGeneratorTests.cs` - keep structural tests and add generator invariants where needed
- `tests/DietPlanner.Domain.Tests/TestMeals.cs` - add explicit meal pools for same-day and small-pool edge cases if needed

Create:
- no new production files required unless extraction becomes necessary

---

### Task 1: Lock Down New Generator Rules with Failing Tests

**Files:**
- Modify: `tests/DietPlanner.Domain.Tests/WeeklyPlanGeneratorRulesTests.cs`
- Modify: `tests/DietPlanner.Domain.Tests/WeeklyPlanGeneratorTests.cs`
- Optionally modify: `tests/DietPlanner.Domain.Tests/TestMeals.cs`

- [ ] **Step 1: Add failing tests for same-day anti-duplication**

Add tests that enforce:

```csharp
[Fact]
public void Generate_ShouldAvoidBreakfastDuplicatesWithinDay_WhenPoolIsLargeEnough()
{
    var generator = new WeeklyPlanGenerator();
    var meals = TestMeals.ValidPool();

    var plan = generator.Generate(new WeeklyPlanGenerationRequest(
        Guid.NewGuid(),
        new DateOnly(2026, 5, 25),
        DinnerMode.Standard,
        meals));

    plan.Days.Should().OnlyContain(day =>
    {
        var breakfastIds = day.MealSlots
            .Where(slot => slot.SlotType is MealSlotType.Breakfast or MealSlotType.SecondBreakfast)
            .Select(slot => slot.MealId)
            .ToArray();

        return breakfastIds.Distinct().Count() == breakfastIds.Length;
    });
}

[Fact]
public void Generate_WithLunchStyleDinnerMode_ShouldAvoidLunchAndDinnerDuplicateWithinDay_WhenPoolIsLargeEnough()
{
    var generator = new WeeklyPlanGenerator();
    var meals = TestMeals.ValidPool();

    var plan = generator.Generate(new WeeklyPlanGenerationRequest(
        Guid.NewGuid(),
        new DateOnly(2026, 5, 25),
        DinnerMode.LunchStyle,
        meals));

    plan.Days.Should().OnlyContain(day =>
        GetMealId(day, MealSlotType.Lunch) != GetMealId(day, MealSlotType.Dinner));
}
```

- [ ] **Step 2: Add failing tests for anti-repetition across days**

Add tests that enforce:

```csharp
[Fact]
public void Generate_ShouldAvoidConsecutiveLunchRepeats_WhenAlternativeExists()
{
    var generator = new WeeklyPlanGenerator();
    var meals = TestMeals.ValidPool();

    var plan = generator.Generate(new WeeklyPlanGenerationRequest(
        Guid.NewGuid(),
        new DateOnly(2026, 5, 25),
        DinnerMode.Standard,
        meals));

    var lunches = plan.Days.Select(day => GetMealId(day, MealSlotType.Lunch)).ToArray();

    for (var i = 1; i < lunches.Length; i++)
    {
        lunches[i].Should().NotBe(lunches[i - 1]);
    }
}

[Fact]
public void Generate_ShouldUseMoreThanOneEligibleLunchBeforeReusing_WhenPoolHasAlternatives()
{
    var generator = new WeeklyPlanGenerator();
    var meals = TestMeals.ValidPool();
    var eligibleLunchIds = meals.Where(meal => meal.Type == MealType.Lunch).Select(meal => meal.Id).ToHashSet();

    var plan = generator.Generate(new WeeklyPlanGenerationRequest(
        Guid.NewGuid(),
        new DateOnly(2026, 5, 25),
        DinnerMode.Standard,
        meals));

    var firstThreeLunches = plan.Days
        .Take(3)
        .Select(day => GetMealId(day, MealSlotType.Lunch))
        .ToArray();

    firstThreeLunches.Distinct().Count().Should().BeGreaterThan(1);
    firstThreeLunches.Should().OnlyContain(id => eligibleLunchIds.Contains(id));
}
```

- [ ] **Step 3: Add failing tests for valid fallback with small pools**

Add tests that verify generation still succeeds when anti-repetition cannot be perfectly satisfied:

```csharp
[Fact]
public void Generate_WithSingleEligibleLunchMeal_ShouldStillSucceed()
{
    var generator = new WeeklyPlanGenerator();
    var meals = TestMeals.PoolWithSingleLunch();

    var plan = generator.Generate(new WeeklyPlanGenerationRequest(
        Guid.NewGuid(),
        new DateOnly(2026, 5, 25),
        DinnerMode.Standard,
        meals));

    plan.Days.Should().HaveCount(7);
}
```

The point is to ensure “best valid fallback,” not over-strict rejection.

- [ ] **Step 4: Run the domain tests to verify failure**

Run:

```powershell
dotnet test tests/DietPlanner.Domain.Tests/DietPlanner.Domain.Tests.csproj --filter "WeeklyPlanGeneratorRulesTests|WeeklyPlanGeneratorTests"
```

Expected: FAIL because the current generator still uses deterministic index-based rotation and fixed 2-day lunch repetition.

- [ ] **Step 5: Commit the failing-test checkpoint if using a TDD branch discipline**

```powershell
git add tests/DietPlanner.Domain.Tests/WeeklyPlanGeneratorRulesTests.cs tests/DietPlanner.Domain.Tests/WeeklyPlanGeneratorTests.cs tests/DietPlanner.Domain.Tests/TestMeals.cs
git commit -m "test: lock randomized week generation rules"
```

---

### Task 2: Replace Deterministic Rotation with Constrained Random Selection

**Files:**
- Modify: `src/DietPlanner.Application/Planning/WeeklyPlanGenerator.cs`

- [ ] **Step 1: Introduce week-local randomized selection helpers**

Refactor the generator to stop assigning meals directly by index math.

Add internal helpers with responsibilities like:

- choose a meal from an eligible pool
- filter candidates by:
  - not already used today
  - unused in current cycle
  - not same as previous day in same slot family
- relax constraints in a defined order when no perfect candidate exists

Suggested helper shape:

```csharp
private static Meal DrawMeal(
    IReadOnlyList<Meal> pool,
    HashSet<Guid> usedInCurrentCycle,
    HashSet<Guid> usedToday,
    Guid? previousDayMealId,
    Random random)
```

This helper should:

1. prefer unused-in-cycle, not-used-today, not-previous-day
2. then unused-in-cycle, not-used-today
3. then not-used-today, not-previous-day
4. then not-used-today
5. then any eligible fallback

- [ ] **Step 2: Replace fixed slot-index arithmetic in `Generate`**

Remove this kind of logic:

```csharp
var breakfastStartIndex = dayOffset % breakfastMeals.Length;
var lunch = lunchMeals[(dayOffset / 2) % lunchMeals.Length];
```

Replace it with daily constrained draws:

- draw breakfast
- draw second breakfast
- draw lunch
- draw dinner according to `DinnerMode`

Track:

- meals used in the current day
- meals used in the current cycle for each pool family
- previous day meal per slot family

Recommended pool-family state:

- `breakfastCycleUsed`
- `lunchCycleUsed`
- `dinnerCycleUsed`

And previous-day markers:

- `previousBreakfastMealId`
- `previousSecondBreakfastMealId` is not necessary if breakfast family is handled together; the relevant rule is family repetition, not slot identity
- `previousLunchMealId`
- `previousDinnerMealId`

For breakfast-family handling, the “previous day” rule should apply by family, not by exact breakfast vs second breakfast slot.

- [ ] **Step 3: Preserve current minimum-pool guards**

Do not loosen the existing pool-size checks:

- standard mode still requires 2 breakfasts and 1 lunch and 1 dinner
- breakfast-style dinner still requires 3 breakfasts
- lunch-style dinner still requires 2 lunches

These guards are still structurally correct because they guarantee non-duplicate same-day assignment is possible in normal mode assumptions.

- [ ] **Step 4: Use explicit randomness without breaking testability**

It is acceptable to instantiate a local `Random` for production behavior, but keep the implementation structured enough that tests validate constraints instead of exact sequences.

If needed, add an internal constructor overload:

```csharp
internal WeeklyPlanGenerator(Random random)
```

Only do this if it materially simplifies testing. Do not add public API surface unless necessary.

- [ ] **Step 5: Run domain tests**

Run:

```powershell
dotnet test tests/DietPlanner.Domain.Tests/DietPlanner.Domain.Tests.csproj --filter "WeeklyPlanGeneratorRulesTests|WeeklyPlanGeneratorTests"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/DietPlanner.Application/Planning/WeeklyPlanGenerator.cs
git commit -m "feat: randomize weekly meal selection"
```

---

### Task 3: Tighten Edge-Case Coverage for Small Pools and Dinner Modes

**Files:**
- Modify: `tests/DietPlanner.Domain.Tests/WeeklyPlanGeneratorRulesTests.cs`
- Modify: `tests/DietPlanner.Domain.Tests/TestMeals.cs`

- [ ] **Step 1: Add regression tests for breakfast-style and lunch-style fallback behavior**

Add tests to prove the new generator still respects dinner mode rules under randomness:

```csharp
[Fact]
public void Generate_WithBreakfastStyleDinnerMode_ShouldUseOnlyBreakfastMealsForDinner()
{
    var generator = new WeeklyPlanGenerator();
    var meals = TestMeals.ValidPool();
    var allowedDinnerIds = meals
        .Where(meal => meal.Type == MealType.Breakfast && !meal.IsDessert)
        .Select(meal => meal.Id)
        .ToHashSet();

    var plan = generator.Generate(new WeeklyPlanGenerationRequest(
        Guid.NewGuid(),
        new DateOnly(2026, 5, 25),
        DinnerMode.BreakfastStyle,
        meals));

    plan.Days.Select(day => GetMealId(day, MealSlotType.Dinner))
        .Should().OnlyContain(id => allowedDinnerIds.Contains(id));
}
```

Add same-day uniqueness checks for breakfast-style days where possible:

```csharp
[Fact]
public void Generate_WithBreakfastStyleDinnerMode_ShouldAvoidBreakfastFamilyDuplicatesWithinDay_WhenPoolIsLargeEnough()
{
    var generator = new WeeklyPlanGenerator();
    var meals = TestMeals.ValidPool();

    var plan = generator.Generate(new WeeklyPlanGenerationRequest(
        Guid.NewGuid(),
        new DateOnly(2026, 5, 25),
        DinnerMode.BreakfastStyle,
        meals));

    plan.Days.Should().OnlyContain(day =>
    {
        var ids = day.MealSlots
            .Where(slot => slot.SlotType is MealSlotType.Breakfast or MealSlotType.SecondBreakfast or MealSlotType.Dinner)
            .Select(slot => slot.MealId)
            .ToArray();

        return ids.Distinct().Count() == ids.Length;
    });
}
```

- [ ] **Step 2: Add regression tests for “allowed imperfection” with tiny valid pools**

Where the pool is only barely valid, assert generation succeeds even if reuse happens later in the week.

Examples:

- lunch-style with exactly 2 lunches
- breakfast-style with exactly 3 breakfasts

The tests should not require zero repetition across the whole week. They should require:

- valid slot assignment
- valid pool membership
- no unnecessary same-day duplicates

- [ ] **Step 3: Run focused domain tests**

Run:

```powershell
dotnet test tests/DietPlanner.Domain.Tests/DietPlanner.Domain.Tests.csproj --filter WeeklyPlanGeneratorRulesTests
```

Expected: PASS.

- [ ] **Step 4: Commit**

```powershell
git add tests/DietPlanner.Domain.Tests/WeeklyPlanGeneratorRulesTests.cs tests/DietPlanner.Domain.Tests/TestMeals.cs
git commit -m "test: cover randomized generator edge cases"
```

---

### Task 4: Full Verification

**Files:**
- No new file targets; verification only

- [ ] **Step 1: Run the full domain suite**

Run:

```powershell
dotnet test tests/DietPlanner.Domain.Tests/DietPlanner.Domain.Tests.csproj
```

Expected: PASS.

- [ ] **Step 2: Run the full solution tests**

Run:

```powershell
dotnet test C:\Users\johny\PlanerDiety\.worktrees\diet-planner-mvp\DietPlanner.sln -m:1
```

Expected: PASS across domain, API, and mobile test projects.

- [ ] **Step 3: Build the solution**

Run:

```powershell
dotnet build C:\Users\johny\PlanerDiety\.worktrees\diet-planner-mvp\DietPlanner.sln
```

Expected: success.

- [ ] **Step 4: Commit verification-only changes if any test fixtures or cleanup changes were needed**

```powershell
git add .
git commit -m "test: verify randomized week generation"
```

Only do this if verification required real tracked-file edits. Do not create a no-op commit.
