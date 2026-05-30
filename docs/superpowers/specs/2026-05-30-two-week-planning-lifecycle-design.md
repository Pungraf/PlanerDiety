# Two-Week Planning Lifecycle Design

## Goal

Add an explicit two-week planning lifecycle so the app can hold:

- one `current` weekly plan
- one `future` weekly plan

The user must be able to generate the next week in advance, switch between the two weeks from Home, and keep shopping lists isolated per week. The system must also roll weeks forward automatically on Sunday.

This spec defines the product behavior, lifecycle rules, backend responsibilities, and mobile behavior for that two-week model.

## Current Gap

The current app only reliably supports one readable weekly plan at a time.

What exists now:

- first-time bootstrap for a missing plan
- editable current-week plan
- shopping lists linked to a plan
- backend draft activation endpoint

What does not exist as a finished product flow:

- a visible `Generate next week` action
- a stable `current` + `future` model
- automatic Sunday rollover
- Home week switching
- separate shopping-list behavior for current vs future plan in normal app use

## Product Decisions

### Week Shape

A week always runs:

- `Sunday -> Saturday`

This is the canonical rule for plan ranges, rollover, button visibility, and future-week generation.

### Maximum Plans

The system may hold at most:

- one `current` plan
- one `future` plan

No third readable week is allowed.

That means:

- user cannot generate another future week while a future plan already exists
- Sunday rollover always leaves the system with at most one current plan

### Generate Button Rule

The `Generate next week` button appears on:

- `Friday`

It remains available until one of these happens:

1. user generates the future week manually
2. Sunday rollover arrives and the system auto-generates the new week because no future week exists

Once a future week exists, the button disappears.

### Sunday Rollover Rule

At the beginning of Sunday, the old current week is obsolete because its last day was Saturday.

Rules:

- if a future week exists, it becomes the new current week
- if no future week exists, the system generates a new current week automatically
- the old current week is deleted
- shopping lists linked to the deleted week are also deleted

After rollover:

- there is one current week
- there is no future week

### Next Friday Rule

After Sunday rollover has completed, the next opportunity to generate another future week starts on the following Friday.

That means the user sees `Generate next week` again only when:

- today is Friday or later within the active current week
- and no future week already exists

## Home Behavior

### Week Selector Ownership

Week switching is owned by:

- `Home`

There is no separate week selector inside Shopping Lists or other pages.

### Visible Week Modes

Home can show:

- `Current week`
- `Next week`

The selector appears only when a future week exists.

If no future week exists, Home behaves exactly like a single-week app except that, from Friday onward, it may show `Generate next week`.

### Editable Future Week

The future week is fully editable, the same as the current week.

Allowed actions while viewing future week:

- replace meal
- copy day
- open meal details
- create shopping lists
- open and manage shopping lists linked to that future plan

Future week is not read-only.

## Shopping List Behavior

Shopping lists remain attached to a specific weekly plan.

Rules:

- current week has its own shopping lists
- future week has its own shopping lists
- switching week on Home changes which plan context Shopping Lists should use when entered
- when a week is deleted during Sunday rollover, its shopping lists are deleted with it

There must be no cross-week shopping-list leakage.

## UX Flow

### 1. Normal Early Week Flow

From Sunday through Thursday:

- app shows current week
- no `Generate next week` button
- no future week unless one was already created earlier and Sunday has not happened yet, which should not be possible under this lifecycle

### 2. Friday Generation Window

From Friday onward, if there is no future week:

- Home shows `Generate next week`
- user may tap it once
- system creates the next Sunday-starting week as `future`
- Home now shows a week switcher with `Current week` and `Next week`
- `Generate next week` disappears

### 3. Working on Future Week

After future week exists:

- user can switch to `Next week` on Home
- user can edit meals there
- user can create and manage shopping lists for that future week
- user can switch back to `Current week`

### 4. Sunday Rollover

At Sunday start:

- if future exists:
  - old current is deleted
  - future becomes current
- if future does not exist:
  - old current is deleted
  - a new current week is generated automatically

After this:

- Home opens on the new current week
- no future week exists
- old week shopping lists are gone

## Backend Model

The backend should stop treating this as a vague "latest readable plan" problem and instead expose explicit lifecycle meaning.

The model should represent:

- current plan
- optional future plan

This does not require user-visible "draft" terminology.

The backend may keep existing internal statuses if useful, but the API contract should speak in current/future terms.

### Required Backend Capabilities

The backend needs explicit operations for:

- get planning state:
  - current plan
  - optional future plan
  - whether generation is currently allowed
- generate future week
- roll weeks forward when Sunday starts
- select a plan by role or plan id for edit/query operations

The rollover check should run server-side so behavior is consistent across devices.

## API Contract Direction

The current `GET /api/plans/current` contract is too narrow for this lifecycle.

The API should move toward a planning-state response that includes:

- current plan summary/data
- future plan summary/data if present
- selected edit targets by plan id
- `canGenerateFutureWeek`
- `availableFromDate` semantics are not needed in the mobile contract if server already decides the boolean

Likely command additions:

- generate future week
- fetch current/future planning state
- edit operations that target an explicit plan id rather than assuming only one current readable plan

Existing replace/copy endpoints should be updated to act on the selected plan explicitly.

## Mobile Contract Direction

Mobile should stop assuming that "current plan" is the only editable plan.

The Home screen needs:

- planning-state load
- current/future switcher
- `Generate next week` action when allowed
- selected week context stored in Home view model

When user navigates from Home into related flows, that selected plan context should be passed through:

- meal search / replace
- copy day
- shopping list index / create / details

Meal details do not need a separate week selector, but the originating context must still use the correct plan for edit-return flows.

## Automatic Rollover Execution

Sunday rollover must not depend on the user opening Home at a precise time.

Practical rule:

- whenever the backend is asked for planning state or plan mutations, it first checks whether rollover is required
- if rollover is required, it performs it before answering

This gives deterministic behavior without requiring a scheduled job for the first version.

If a scheduled job is added later, it should preserve the same lifecycle rules, not redefine them.

## Error Handling

### Generation

If future-week generation fails:

- keep user on Home
- show an error
- do not create partial shopping-list or plan state

### Rollover

If rollover fails:

- backend should fail the request rather than serving inconsistent current/future state
- mobile should show a blocking error surface or retry path, not stale mixed-week data

### Conflict Prevention

If a future plan already exists:

- generate action is not shown in UI
- backend also rejects duplicate generation attempts defensively

## Testing Strategy

### Backend Tests

Add or update tests for:

- first-time bootstrap creates a Sunday-starting current week
- Friday planning state exposes `canGenerateFutureWeek = true` when no future exists
- generating next week creates exactly one future plan
- cannot generate second future week while one exists
- Sunday rollover promotes existing future to current and deletes old current
- Sunday rollover auto-generates current when future is missing
- old-week shopping lists are deleted during rollover
- replace/copy operations target the selected plan correctly

### Mobile Tests

Add or update tests for:

- Home shows `Generate next week` only when allowed
- Home hides it after generation succeeds
- Home shows current/future selector only when future exists
- switching Home week changes displayed plan data
- replace/copy actions operate on selected week
- Shopping Lists load for the selected Home week context

## Out of Scope

Not part of this spec:

- generating more than one future week ahead
- separate week selector inside Shopping Lists
- scheduled background rollover infrastructure beyond request-time server checks
- historical archive of old plans after Sunday rollover

Old weeks are deleted, not archived.
