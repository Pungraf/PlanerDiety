# Randomized Week Generation Design

## Goal

Replace the current deterministic weekly meal rotation with randomized week generation that still preserves the existing slot model and meal-type rules.

The new generator must:

- keep the current 4-slot day structure
- keep existing meal pool rules by slot and `DinnerMode`
- produce more natural weekly variety
- reduce visible repetition
- remain valid when meal pools are small

This spec defines the generation behavior only. It does not change the two-week lifecycle, shopping list model, or meal details model.

## Current Gap

The current generator in [WeeklyPlanGenerator.cs](C:/Users/johny/PlanerDiety/.worktrees/diet-planner-mvp/src/DietPlanner.Application/Planning/WeeklyPlanGenerator.cs) is fully deterministic.

Today it:

- rotates breakfast meals by index
- repeats lunches in fixed 2-day blocks
- rotates dinners by fixed index rules based on `DinnerMode`

That has two drawbacks:

1. generated weeks feel predictable
2. repeated generation with the same meal catalog produces the same visible pattern

The user wants randomized generation, but without losing the nutritional/slot structure already built into the app.

## Product Decisions

### Slot Structure Stays The Same

Each day still contains exactly:

- `Breakfast`
- `SecondBreakfast`
- `Lunch`
- `Dinner`

This is not changing to “two breakfasts and two dinners” in the literal slot sense.

The intended meaning is:

- breakfast-family slots remain breakfast-based
- lunch remains lunch-based
- dinner remains driven by `DinnerMode`

### Pool Rules Stay The Same

The generator still selects from these pools:

- `Breakfast` and `SecondBreakfast`:
  - `MealType.Breakfast`
  - excluding desserts
- `Lunch`:
  - `MealType.Lunch`
- `Dinner`:
  - `MealType.Dinner` in `Standard` mode
  - breakfast pool in `BreakfastStyle` mode
  - lunch pool in `LunchStyle` mode

The current minimum-pool validation rules remain valid:

- at least 2 breakfast meals for standard mode
- at least 3 breakfast meals for breakfast-style dinner mode
- at least 1 lunch meal for standard mode
- at least 2 lunch meals for lunch-style dinner mode
- at least 1 dinner meal for standard mode

If those minimums are not met, generation should still fail as it does today.

## Randomization Model

### Principle

Meal assignment should become randomized, but not unconstrained.

The generator should behave like:

- random selection within the eligible pool
- anti-repetition preference rules
- deterministic fallback behavior when a pool is too small to satisfy all preferences perfectly

This is not “pure random.” It is constrained random generation.

### Pool Exhaustion Before Reuse

Within each slot family, the generator should try to use every eligible meal once before reusing it.

Examples:

- breakfast-family generation should try to cycle through the available breakfast pool before reusing breakfast meals
- lunch generation should try to cycle through all lunch meals before repeating one
- dinner generation should try to cycle through all dinner-eligible meals for the active mode before reuse

This rule exists to create variety across the week.

### Consecutive-Day Anti-Repetition

When reuse becomes necessary, the generator should avoid repeating the same meal on consecutive days in the same slot family if possible.

Examples:

- if `Lunch A` is used on Monday, Tuesday lunch should prefer a different lunch meal
- if `Dinner B` is used on Wednesday, Thursday dinner should prefer a different dinner-eligible meal

This is a preference rule, not a hard impossibility rule, because small pools may require reuse.

## Same-Day Anti-Repetition

The same meal should not appear twice within the same day unless the eligible pool is too small for the current mode and slot structure to avoid it.

Examples that should normally be prevented:

- breakfast and second breakfast using the same meal
- lunch and dinner using the same meal in lunch-style dinner mode
- breakfast and dinner using the same meal in breakfast-style dinner mode

If a non-duplicate assignment is possible, the generator must prefer it.

## Rule Priority

Candidate selection should effectively follow this preference order:

1. eligible for the slot
2. not already used today
3. unused in the current cycle of that pool
4. not equal to the previous day’s meal in the same slot family

If no meal satisfies every preference, the generator may relax rules in a controlled order.

Recommended relaxation order:

1. allow reuse from the already-used cycle
2. still avoid same-day duplicates if possible
3. still avoid consecutive-day repeats if possible
4. only allow same-day duplication when mathematically unavoidable

This preserves variety without making generation brittle.

## Dinner Mode Behavior

### Standard

- breakfast-family slots use breakfast pool
- lunch uses lunch pool
- dinner uses dinner pool

Randomization and anti-repetition rules apply independently per family.

### BreakfastStyle

- dinner uses breakfast pool
- the day now draws three meals from the breakfast pool:
  - breakfast
  - second breakfast
  - dinner

The generator should avoid duplicates across those three breakfast-derived slots within the same day whenever possible.

This is why the existing minimum of three breakfast meals remains correct.

### LunchStyle

- dinner uses lunch pool
- the day now draws two meals from the lunch pool:
  - lunch
  - dinner

The generator should avoid assigning the same lunch meal to both slots within the same day whenever possible.

This is why the existing minimum of two lunch meals remains correct.

## Determinism vs Randomness

The user asked for random generation. That means repeated generation should not look identical all the time.

However, the implementation should still be testable and debuggable.

The design direction is:

- allow randomness in production behavior
- structure the generator so tests can verify constraints reliably
- optionally support seeded randomness internally if that simplifies testing

The user does not need a visible “seed” concept in the UI.

## UX Expectations

The user-facing effect should be:

- week plans feel less mechanical
- similar meals are spread more naturally
- repeated breakfasts/lunches/dinners do not cluster in obvious deterministic patterns
- existing meal-type expectations are preserved

No UI changes are required for this feature by itself.

## Error Handling

Generation should still fail only for true pool insufficiency.

It should not fail just because the preferred anti-repetition arrangement could not be satisfied perfectly.

That means:

- too few eligible meals: throw as today
- anti-repetition impossible due to small pool: generate the best valid fallback plan

The generator must always prefer “valid with some repetition” over “throw for avoidable constraint strictness.”

## Testing Strategy

### Unit Tests

Add or update tests to verify:

- generated plan still has 7 days and 4 slots per day
- breakfast-family slots only use allowed breakfast meals
- lunch slots only use allowed lunch meals
- dinner slots still obey `DinnerMode`
- same-day duplicates do not occur when pools are large enough
- lunch-style days avoid lunch/dinner duplicates when possible
- breakfast-style days avoid breakfast/breakfast/dinner duplicates when possible
- generation eventually reuses meals only after pool exhaustion
- consecutive-day repeats are avoided when an alternative exists
- generation still succeeds for minimum valid pools
- generation still throws for insufficient eligible pools

### Statistical/Behavioral Tests

Tests should not rely on one specific output week unless the implementation uses an explicit injected seed.

Instead, tests should assert constraints and invariants:

- allowed pool membership
- no impossible duplicates
- valid fallback behavior

If seeded randomness is introduced in tests, the seed should be internal to test setup only.

## Out of Scope

Not part of this spec:

- user-configurable randomness settings
- user-controlled meal exclusions per week
- historical tracking of recently used meals across multiple weeks
- calorie-balancing changes
- changing slot count or slot semantics

This feature changes weekly selection strategy, not the overall nutrition model.
