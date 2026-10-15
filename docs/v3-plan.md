# ObjectListView v3 implementation plan

[Documentation index](README.md)

## Goals and compatibility policy

Version 3 is a deliberate API cleanup, not a rewrite. Preserve useful behavior and source compatibility where practical, but remove properties, events, and methods that do not fit the new contracts. Every breaking change needs a documented replacement or an explanation of why the capability is being retired.

The main goals are enumerable-friendly collection APIs, explicit grouping configuration, unified styling, consistent custom rendering and hit testing, and focused cleanup.

The upstream roadmap's proposed .NET Framework 4.0 minimum is historical. The current repository already targets **.NET Framework 4.6.2**; this plan does not lower that target or reintroduce .NET 2.0 support.

Keep the current framework target during the refactor. Before shipping v3, review the support policy: Microsoft support for .NET Framework 4.6.2 ends on January 12, 2027. Raising the minimum or adding a modern .NET Windows target, such as .NET 10, is a separate decision and workstream. Do not remove Windows compatibility branches without a separate operating-system support decision.

## Current foundations

These prerequisites are already complete:

- All six projects use SDK-style project files and target `net462`.
- The test project uses MSTest 4.5.1 and package references.
- The baseline is 463 discovered fixture/method pairs, passing in Debug and Release.
- WinForms tests execute serially on STA threads with fresh, invisible, non-activating forms that are disposed after each test.
- All 13 previously excluded Munger tests are included.
- Full-framework MSBuild compiles the existing WinForms resources without adding production runtime dependencies.

Use the [documented build and test workflow](../README.md#tests). The test baseline includes inherited fixtures for regular, fast, and tree list controls. Preserve those concrete cases, not merely an overall test count.

Some v3 building blocks also already exist: `Objects` and `SetObjects()` use `IEnumerable`, `OLVExporter` accepts an enumerable, and `IRenderer` supports cell rendering, hit testing, and editor positioning. Extend these mechanisms rather than introducing parallel implementations.

## Phase 1: establish the API contract and regression baseline

The test infrastructure is ready, but API and performance baselines still need to be established.

- Inventory public properties, methods, events, and extension points. Classify each as keep, replace, or remove.
- Record replacement APIs and migration examples before making breaking changes.
- Establish behavioral and performance baselines for regular, fast, virtual, data-bound, and tree lists.
- Measure representative small and large collections, including sorting, filtering, selection, checking, and batch updates. Agree on regression budgets before optimizing.

**Exit criteria:** an agreed API-change inventory, reproducible measurements, and the existing test suite passing without lost or newly skipped cases.

## Phase 2: make collection APIs enumerable-friendly

Change these APIs together:

| API | Proposed v3 contract |
| --- | --- |
| `SelectedObjects`, `CheckedObjects` | Accept `IEnumerable`; return a materialized snapshot exposed as `IEnumerable`. |
| `AddObjects()`, `InsertObjects()` | Accept `IEnumerable`. |
| `RefreshObjects()`, `RemoveObjects()` | Accept `IEnumerable`. |
| `CopyObjectsToClipboard()` | Accept `IEnumerable`. |
| `TypedObjectListView<T>` | Expose corresponding `IEnumerable<T>` APIs. |

Use non-generic `IEnumerable` on the heterogeneous base control, matching the existing `SetObjects()` contract. Typed wrappers provide convenient LINQ access.

Enumeration semantics are part of the contract:

- Consume each input sequence once where repeated access is necessary, materializing it at a defined point on the appropriate thread.
- Materialize before clearing selection, changing checked state, or modifying a collection that the input sequence may reference.
- Preserve snapshot getters rather than introducing live, deferred enumeration over UI state.
- Preserve ordering, duplicate handling, cancellation events, notification subscriptions, and documented null behavior.
- Keep collection-shaped event payloads where handlers need to inspect or modify them.
- Propagate enumeration failures explicitly; do not turn a failed sequence into an apparently successful empty update.

Update every override and dependent interface in the same milestone, including `VirtualObjectListView`, `FastObjectListView`, data-bound controls, `TreeListView`, and `IVirtualListDataSource`. Reuse the existing collection helpers where their ownership and enumeration semantics fit.

**Acceptance cases:** LINQ queries, iterator methods, single-use enumerables, self-referential selection changes, empty/null inputs, enumeration failures, notification cleanup, and the control-specific checked-object behavior of tree and virtual lists.

**Deliverable:** the enumerable API change, its migration examples, and focused additions to the existing collection, selection, checkbox, notification, tree, and export tests.

## Phase 3: replace the grouping-property tangle

Remove `AlwaysGroupByColumn` and `AlwaysGroupBySortOrder`, but preserve the ability to group by one column while sorting items by another.

Build one explicit grouping configuration on top of the existing `GroupingParameters` pipeline:

- Separate the group column and order from the item sort column and order.
- Make following the current sort column versus using explicit grouping a documented choice.
- Route header clicks, command menus, programmatic grouping, and virtual grouping through the same configuration.
- Review related switches, including `SortGroupItemsByPrimaryColumn`; remove them only when the replacement covers their useful behavior.
- Preserve grouping cancellation and custom comparer extension points where they fit the new contract.

**Acceptance cases:** sorting within groups, cancelled grouping, non-groupable columns, collapsed-group state, independent group/item ordering, and virtual-list grouping.

**Deliverable:** a single grouping model, updated callers and demos, focused regression tests, and an old-to-new API mapping.

## Phase 4: unify styling before expanding custom rendering

Row/cell styles, header styles, and tooltip formatting currently use separate systems. Unify their shared concepts without forcing every surface into one oversized renderer interface.

| Shared concept | Responsibility |
| --- | --- |
| Style values | Font, colors, spacing, alignment, and applicable borders. |
| Visual state | Selected, focused, hot, pressed, disabled, and their combinations. |
| Style resolution | Defined per-property inheritance and override precedence. |
| Layout context | Bounds, DPI, RTL direction, model, column, and state. |

An unspecified value must be distinguishable from an explicit override, such as inheriting a font style versus explicitly requesting regular text. Define how formatting callbacks participate in precedence so existing customizations are not silently overwritten.

Integrate incrementally: cells/rows, then headers, then tooltips. Preserve the default appearance. Use temporary adapters where they make migration straightforward, with a clear retirement plan rather than permanent duplicate style systems.

**Acceptance cases:** state combinations, per-column overrides, formatting callbacks, high contrast, resource ownership, and designer serialization round trips.

**Deliverable:** shared style resolution used by all three surfaces, with documented inheritance and precedence.

## Phase 5: share layout across rendering and hit testing

Extend the existing renderer architecture:

- Calculate layout once and reuse its geometry for painting, hit testing, tooltip targeting, and cell-editor placement.
- Introduce surface-specific renderer contracts for headers and tooltips where needed.
- Preserve native header resizing, sorting, checkbox interaction, keyboard behavior, and accessibility.
- Verify scrolling offsets, reordered columns, DPI scaling, RTL layouts, and tree indentation.
- Preserve native drawing where no custom rendering is requested.

Do not let drawing and hit testing independently calculate where an interactive element is located.

**Exit criteria:** working custom cell, header, and tooltip examples with matching layout and hit detection, covered by focused geometry tests and Windows integration checks. Visual or activation-dependent checks must use an appropriate visible test host rather than assuming the invisible fixture can validate those behaviors.

## Phase 6: finish cleanup and prepare migration

After the behavioral contracts stabilize:

- Split the large `ObjectListView.cs` into focused partial files without unnecessarily redesigning the inheritance hierarchy.
- Remove redundant compatibility APIs. For example, `CheckedObjectsEnumerable` becomes unnecessary once `CheckedObjects` accepts an enumerable.
- Remove obsolete compatibility paths only where their replacement and support implications are understood.
- Update demos, designer-generated configuration through the designer, and Markdown examples.
- Publish a migration guide covering removed members, changed return types, overrides, grouping, custom renderers, and style precedence.
- Set v3 assembly and package versions deliberately, accounting for source and binary compatibility and existing signing.
- Release a v3 alpha before freezing the final API.

**Exit criteria:** all retained baseline tests pass in Debug and Release, new contracts have focused coverage, demos build and run, serialization and representative consumer migrations have been exercised, and the migration guide accounts for every public breaking change.

## Delivery order

Implement independently reviewable milestones in this order:

1. API inventory and behavioral/performance baseline.
2. Enumerable collection APIs and dependent callers.
3. Explicit grouping configuration.
4. Shared style resolution.
5. Shared layout, custom rendering, and hit testing.
6. Cleanup, migration documentation, and alpha release.

The enumerable APIs are the first feature milestone. They deliver a concrete benefit and expose cross-control dependencies before the higher-risk styling and rendering work. Do not mix a test-framework migration, platform retargeting, or unrelated feature additions into those API changes.
