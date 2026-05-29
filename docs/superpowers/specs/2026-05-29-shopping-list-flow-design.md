# Shopping List Flow Design

## Goal

Finish the shopping-list feature so it behaves as a real separate object in the mobile app:

- the Shopping tab opens on a saved-lists index
- users can create multiple lists for the current weekly plan
- users can open, check off, and delete lists
- lists persist only for the active weekly plan
- when a new weekly plan is submitted, the previous plan's lists are removed

This spec covers behavior, navigation, data rules, and validation targets. It does not cover broad visual redesign beyond what is needed to make the flow coherent.

## Current Gaps

The current codebase already has partial shopping-list support, but the user-facing flow is incomplete:

- the Shopping tab has a list/details split, but create flow does not finish the job
- there is no usable delete path in mobile UI
- create flow exists as scaffolding but not as a complete preset -> fine-tune -> save path
- weekly-plan lifecycle rules for list cleanup are not expressed as a clear product behavior

## Product Decisions

### Default Shopping Tab

The Shopping tab opens on the saved-lists index, not the latest list and not the create screen.

Rationale:

- this matches the separate-object model
- it makes create/delete behavior visible
- it avoids hidden state about which list is "current"

### Delete Behavior

Delete is available in two places:

- row-level delete from the saved-lists index
- delete action from the list-details screen

Both delete paths require confirmation before the destructive action is sent.

### Create Behavior

Create uses a two-step flow:

1. preset selection
2. optional fine-tune before save

This keeps the default case fast while preserving ingredient-level control.

### Persistence Rule

Shopping lists are persistent only within the active weekly plan.

Rules:

- users may keep multiple lists for the same weekly plan
- those lists remain until deleted by the user or until that weekly plan is replaced
- when a new weekly plan is submitted and becomes the active plan, shopping lists linked to the previous weekly plan are deleted automatically

This cleanup must happen on the backend so mobile state stays consistent across installs/devices.

## UX Flow

### 1. Saved Lists Index

The Shopping tab first screen shows:

- page title
- primary `Create list` action
- list of saved shopping lists for the active weekly plan
- per-row metadata:
  - list name
  - creation timestamp or friendly date
  - item count
- per-row actions:
  - open
  - delete

If there are no lists, the screen shows an empty state with a create CTA.

### 2. Create Step 1: Preset Choice

When the user taps `Create list`, the app opens a preset-selection screen.

Presets:

- `Full week`
- `Selected days`
- `Custom`

Behavior:

- `Full week` preselects every eligible ingredient from the active weekly plan
- `Selected days` moves to a day-selection view before fine-tune
- `Custom` goes straight to fine-tune with nothing forced beyond plan-scoped availability

This screen is optimized for fast selection, not detailed editing.

### 3. Create Step 2: Fine-Tune

The second screen allows meal/ingredient editing before save.

Capabilities:

- group by day
- show meals within each day
- allow meal-level select/deselect
- allow ingredient-level select/deselect
- show enough context to understand what is being added

Save behavior:

- creating a list saves a new shopping list object
- existing lists for the same weekly plan remain untouched
- on success, return to saved-lists index and refresh it

### 4. List Details

Opening a saved list shows:

- list title
- creation metadata
- item list
- check/uncheck actions
- delete action

Check/uncheck updates the persisted list, not a transient local view.

After toggling:

- the current details screen refreshes deterministically
- the index should also reflect any changed item counts if relevant to the displayed summary

### 5. Delete

Delete from either entry point behaves the same:

- ask for confirmation
- on success:
  - if deleting from index, remove the row and stay on index
  - if deleting from details, navigate back to the index and refresh it

If delete fails, show an error and keep the user on the current surface.

## Navigation Model

Screens:

- `ShoppingListIndexPage`
- `ShoppingListCreatePresetPage`
- `ShoppingListCreateBuilderPage`
- `ShoppingListDetailsPage`

Navigation rules:

- Shopping tab root -> index
- index -> preset create
- preset create -> builder when needed
- index -> details
- successful create -> index
- successful delete from details -> index

The navigation contract should use explicit route/query state or a dedicated context object that is robust enough for reload and back navigation.

## Data and Backend Expectations

The backend already supports shopping-list object operations. This flow depends on these contracts being wired fully in mobile:

- list saved shopping lists for active plan
- get create options
- create new shopping list
- get shopping list details
- toggle shopping list item
- delete shopping list

Lifecycle cleanup requirement:

- when a new weekly plan is submitted, delete shopping lists linked to the replaced plan

If this cleanup is not already enforced in the plan-submission path, that must be added as part of implementation.

## Error Handling

### Index

- fetch failure -> inline error state with retry

### Create

- load options failure -> inline error with retry
- save failure -> remain on create flow and show error

### Details

- details fetch failure -> inline error with retry or back navigation if the list no longer exists
- toggle failure -> keep current state stable and show error
- delete failure -> remain on current screen and show error

No shopping-list action should crash the app or silently drop the user into a stale screen.

## Testing Strategy

### Mobile ViewModel Tests

Add or update tests for:

- index loads saved lists
- create CTA navigates correctly
- selecting a list loads details
- deleting from index refreshes index
- deleting from details returns to index and refreshes
- create flow loads presets/options
- create flow saves a new list and returns to index
- toggle still works after the navigation split

### API Tests

Add or update tests for:

- delete shopping list endpoint
- create shopping list endpoint with selected ingredients
- weekly plan submission deletes lists belonging to the replaced plan

### Regression Target

After implementation:

- user can create multiple lists within a week
- user can delete lists from both entry points
- replacing the weekly plan removes previous-week lists
- shopping tab always lands in a coherent index state

## Out of Scope

Not part of this spec:

- copy-day redesign
- broad visual refresh of the whole mobile app
- API/admin UI redesign
- offline shopping-list caching

Those remain separate follow-up tasks.
