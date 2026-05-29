# Meal Details Crash Fix Design

Date: 2026-05-29
Scope: Mobile meal-details navigation crash only
Status: Approved in conversation, pending user review of written spec

## Goal

Fix the current mobile behavior where tapping a meal from the home screen crashes the app instead of opening recipe details.

Success for this slice means:

- tapping a meal opens a full-page recipe details screen when the flow succeeds
- failures show an alert instead of crashing the app
- the app remains on a stable screen if navigation or loading fails

This spec does not attempt to solve offline recipe access, the copy-day UX redesign, shopping-list create/delete UI, or the broader visual redesign. Those remain separate follow-up work.

## Current State

The current codebase already contains:

- `HomeViewModel.OpenMealDetailsCommand`
- `MealDetailsPage`
- `MealDetailsViewModel`
- `meal-details` Shell route
- `IMealDetailsContextStore`

So the problem is not missing capability. The problem is the robustness of the transition from the home screen into the recipe-details flow.

## Chosen Approach

Keep the existing route plus in-memory context-store pattern and harden it.

This was chosen because it is the narrowest safe fix:

- it matches the current structure
- it avoids widening the scope into route-query refactoring
- it can stop the crash without dragging in new persistence or offline state

The main tradeoff is that the flow still depends on transient in-memory state. That is acceptable for this fix because the immediate requirement is reliability, not architecture cleanup.

## UX Behavior

### Success path

When the user taps a meal card on the home screen:

1. the app verifies the meal has a valid `MealId`
2. the app stores that id in `IMealDetailsContextStore`
3. the app navigates to a full-page recipe screen
4. the recipe screen loads:
   - meal name
   - meal type
   - kcal/protein summary
   - ingredients
   - preparation text

### Failure path

If any step fails:

- the app shows a user-facing alert
- the app does not terminate
- the user stays on a stable screen

User-facing failure cases:

- meal slot has no `MealId`
- context store is empty on the details page
- API request fails
- API returns invalid or incomplete payload
- Shell navigation throws

The failure handling target is explicit stability, not silent fallback content.

## Design Details

### Home screen responsibilities

`HomeViewModel.OpenMealDetailsAsync` becomes defensive.

Required behavior:

- reject null slot input
- reject slots without `MealId`
- only write to context store when a valid id exists
- wrap navigation in `try/catch`
- show alert if navigation cannot proceed

This keeps the home screen as the place that validates whether a details transition is even possible.

### Details screen responsibilities

`MealDetailsPage` and `MealDetailsViewModel` become defensive around load timing and data access.

Required behavior:

- if no meal id is available from the context store, show alert and leave the user on a stable screen
- if details loading fails, show alert and avoid leaving the screen in a half-initialized crash state
- loading state is shown while the request is in flight
- successful loads populate the full recipe card

### Navigation behavior

The route remains `meal-details`.

For this fix, navigation stays context-driven rather than route-query-driven. That avoids touching Shell route contracts and dependent tests beyond the crash path.

### Alert behavior

Alerts are the chosen failure surface for this slice.

Design intent:

- keep user feedback immediate and unambiguous
- avoid bouncing the user through extra states
- avoid hidden failure where tapping appears to do nothing

The alert copy should clearly say that recipe details could not be opened or loaded.

## Data Requirements

The details page must render a full recipe card using existing API data:

- `Name`
- `Type`
- `Kcal`
- `Protein`
- `Description`
- `Ingredients[]`

No new API fields are required for this fix if the current endpoint already supplies these values correctly.

## Error Handling Rules

The following rules are explicit:

- no unhandled exception may propagate from meal tap flow
- no null `MealId` may be passed into the details request path
- missing context is treated as a recoverable user-facing error
- transport or deserialization failure is treated as a recoverable user-facing error

If recovery requires leaving the details page, the app should navigate back or keep the user on the current stable page, but never crash the process.

## Testing Strategy

This fix should be validated primarily in mobile tests around viewmodel behavior and then device-tested manually.

Automated coverage should verify:

- tapping a valid meal stores `MealId` and attempts navigation
- tapping a slot without `MealId` does not navigate and triggers alert behavior
- details load populates the full recipe card on success
- missing context triggers failure handling
- API failure triggers failure handling

Manual validation should verify:

- tapping a meal from the installed app no longer closes the app
- a valid meal opens the expected page
- a forced failure path shows an alert instead of terminating

## Out of Scope

Not part of this spec:

- copy-day redesign to button-plus-options
- shopping-list create/delete UI completion
- broader visual redesign of the mobile app
- route-query refactor for meal details
- offline or cached details fallback

Those should be handled as separate specs or follow-up tasks after this crash path is stable.

## Implementation Notes

The implementation should prefer minimal edits in:

- `HomeViewModel`
- `MealDetailsViewModel`
- `MealDetailsPage`
- any shared prompt/alert abstraction already used in mobile flows

Avoid unrelated refactoring during this fix. The goal is a reliable transition and stable failure behavior, not architectural cleanup.
