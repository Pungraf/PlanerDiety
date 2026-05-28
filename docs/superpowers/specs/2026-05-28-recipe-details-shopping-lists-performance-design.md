# Recipe Details, Shopping Lists, and Performance Design

## Goal

Upgrade the diet planner so users can inspect recipe preparation details, create linked shopping lists from selected weekly meals, edit plans safely when linked lists exist, and use faster screens with a more modern mobile UI.

## Current State

The app is a .NET MAUI Android client backed by an ASP.NET Core API, EF Core persistence, and a CSV importer. Weekly plan meals currently expose only summary data. The source recipe CSV contains `Wykonanie`, but the domain model does not store recipe preparation text. Shopping list behavior is currently one generated list tied to the active weekly plan, and plan changes synchronize that list automatically. Meal search sends API requests as search text changes, which creates avoidable latency and overlapping network work.

## Data and Import

Add `Meal.Description` and map it from the source CSV `Wykonanie` column. The source importer reads the description once per recipe from the row that contains `Nazwa potrawy`, then persists it with the meal. The meal detail API returns recipe name, kcal, protein, categorized ingredients, and description.

Ingredient quantity must come only from the source CSV quantity column, referenced as `Ilosc` in this spec. The app supports grams as the shopping unit. Values like `75 g` are imported as quantity `75` and unit `g`. Non-gram or qualitative values such as `do smaku`, `opcjonalnie`, `2 szt`, or `ml` are preserved for recipe details as non-shopping ingredients with quantity `0` and unit `g`, so they remain visible in recipe instructions but do not corrupt shopping totals.

Ingredient category comes from the ingredient classification CSV, referenced as `przepisy do apki - Klasyfikacja skladniki.csv` in this spec. Missing classifications fall back to `Inne`.

## Meal Details and Plan Editing

Meal cards in the weekly plan are tappable. Tapping a card opens a recipe detail page. The existing replace action stays separate on the card, so users have distinct actions for viewing details and replacing a meal.

The recipe detail page shows:

- Recipe name
- Kcal
- Protein
- Ingredients grouped or labeled by category
- Ingredient quantity in grams where applicable
- Preparation description from `Wykonanie`

Before replacing a meal or copying a day, the app checks whether the current week has linked shopping lists. If linked lists exist, the app shows a confirmation that continuing will delete those lists. Confirming deletes linked shopping lists and applies the plan change. Cancelling leaves both the plan and lists unchanged.

## Shopping Lists

Replace the fixed current shopping list with a list of shopping lists linked to the current week.

The Shopping tab starts with the list of existing shopping lists for the current week. A user can open a list, check or uncheck items, and delete a list.

`Create List` opens a selection screen generated from the current week. Ingredients are shown by day and meal, with checkboxes for whole meals and individual ingredients. Selecting or unselecting a meal selects or unselects its ingredients. Pressing `Create` creates a summarized shopping list from selected shopping ingredients, grouped by ingredient/category and summed in grams.

Created lists remain linked to the week. Later meal replacement or day copy for that week uses the destructive confirmation flow described above.

## Performance

The first performance phase avoids a full offline database and instead reduces network round trips and UI binding work:

- Fetch the weekly plan once when entering Home.
- After successful replace or copy actions, update local Home state where practical instead of forcing a full reload.
- Fetch the meal catalog once when entering Replace Meal.
- Filter meal search results in memory as the user types.
- Debounce or cancel overlapping search/filter operations.
- Use `CollectionView` for scrollable recipe, search, and shopping-list surfaces.
- Avoid nesting virtualized lists inside `ScrollView` where it breaks scrolling or virtualization.
- Add MAUI compiled bindings with `x:DataType` in XAML.
- Keep local database/offline-first sync out of this phase, but shape mobile services around cache/repository boundaries so it can be added later.

This follows current platform guidance: Microsoft documents compiled bindings as faster because they resolve bindings at compile time instead of runtime reflection, and recommends `CollectionView` for performant scrolling/selectable lists. Android architecture guidance recommends local source-of-truth and offline-first repositories for deeper future optimization, but that is intentionally deferred.

## UI Direction

The UI should become a modern app workflow, not a landing page. It should be compact, clear, and efficient for repeated planning.

Home shows a compact week/day selector, daily totals, and tappable meal rows/cards. Recipe details and replace actions are visually distinct.

Recipe detail focuses on readability: summary nutrition at the top, categorized ingredients, then preparation text.

Replace Meal uses a scrollable result list with responsive search/filtering and clear tap targets.

Shopping shows existing lists first, then a create-list flow with grouped meal/ingredient selection and clear selected states.

Dialogs are used for destructive plan edits that delete linked shopping lists.

## Testing Strategy

Add importer tests for description persistence, gram-only quantity parsing, non-shopping qualitative ingredients, and category fallback.

Add domain/application/API tests for meal details, linked shopping-list creation, list deletion, list item toggling, and destructive list deletion on confirmed plan edits.

Add MAUI viewmodel tests for opening recipe details, in-memory meal filtering, copy-day confirmation state, shopping list list-of-lists, create-list selection behavior, and delete-list flows.

Run the full .NET test suite after implementation.

## Out of Scope

This design does not add full offline mobile persistence, multi-device conflict resolution, barcode scanning, store sections, or nutrition beyond kcal/protein.
