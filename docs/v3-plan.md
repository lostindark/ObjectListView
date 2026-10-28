# ObjectListView v3 implementation plan

[Documentation index](README.md)

## Goals and compatibility policy

Version 3 is a deliberate API cleanup, not a rewrite. Preserve useful behavior and source compatibility where practical, but remove properties, events, and methods that do not fit the new contracts. Every breaking change needs a documented replacement or an explanation of why the capability is being retired.

The main goals are enumerable-friendly collection APIs, explicit grouping configuration, unified styling, consistent custom rendering and hit testing, and focused cleanup.

The upstream roadmap's proposed .NET Framework 4.0 minimum is historical. The current repository already targets **.NET Framework 4.6.2**; this plan does not lower that target or reintroduce .NET 2.0 support.

The platform migration workstream retains `net462` and adds `net10.0-windows`
to all six runtime/test projects. It also introduces separate modern Designer SDK
client/server/protocol projects, without mixing platform work with the remaining
API redesign. Runtime previews may precede complete designer acceptance.
The Framework designer is retained; .NET 8 is not a target. Before shipping v3,
review the support policy: Microsoft support for .NET Framework 4.6.2 ends on
January 12, 2027. Do not remove Windows compatibility branches without a separate
operating-system support decision.

## Current foundations

These prerequisites are already complete:

- All six runtime/test projects use SDK-style project files and target `net462;net10.0-windows`.
- The test project uses MSTest 4.5.1 and package references.
- The pre-platform-migration baseline is 610 passing cases in both Debug and Release; preserve test identities, not just totals.
- WinForms tests execute serially on STA threads with fresh, invisible, non-activating forms that are disposed after each test.
- All 13 previously excluded Munger tests are included.
- Full-framework MSBuild retains the Framework resource path; .NET 10 uses preserialized resources without a BinaryFormatter opt-in.

Use the [documented build and test workflow](../README.md#tests). The test baseline includes inherited fixtures for regular, fast, and tree list controls. Preserve those concrete cases, not merely an overall test count.

Some v3 building blocks also already exist: `Objects` and `SetObjects()` use `IEnumerable`, `OLVExporter` accepts an enumerable, and `IRenderer` supports cell rendering, hit testing, and editor positioning. Extend these mechanisms rather than introducing parallel implementations.

## Platform migration checkpoint

This checkpoint is the repository handoff for subsequent .NET support iterations.
The runtime migration is implemented; modern designer support is still a preview,
not a completed acceptance milestone. Do not mix the open designer work with the
unrelated grouping/styling phases below.

| Workstream | Progress |
| --- | --- |
| Runtime targets | All six original projects target `net462;net10.0-windows`; there is no .NET 8 target. Both demos run on both targets. |
| Saved layouts | Shared, versioned OLVS schema 1 replaces BinaryFormatter. Old layouts must be recreated; malformed/incompatible input is rejected before applying it. |
| Framework compatibility | Existing Framework designer types and resource build path are retained. The actual Framework designer's column editor, serialization, rebuild, and reopening were exercised. |
| Modern designer implementation | SDK 1.6.0 client/server/protocol projects, custom column editor, filtering, smart-tag properties, and copied-value modal printer editors are packaged separately from runtime assets. |
| Packaging and CI | Both configurations/targets are built and tested; package contents and fresh consumers are checked, including actual two-way layout interchange. The publishing workflow still consumes a successful CI artifact rather than rebuilding. |
| Desktop acceptance | Most packaged-consumer checks below passed. Native header-divider dragging remains a failed check, blocking physical-resize Undo/Redo and a complete-support claim. |
| Review follow-up | The reproduced layout-restoration finding is fixed with focused regressions. Security review reported no vulnerabilities in the reviewed changes. The final unfiltered four-way test matrix and package checks passed. |

### Verified behavior and important fixes

- Before review, the full post-migration suites passed 621 Framework cases and
  620 modern cases in each configuration, with no skipped cases. Four review
  regressions bring the passing totals to 625 and 624. The difference is the
  Framework-only DesignSurface test; shared typed-property metadata assertions
  remain covered on both targets.
- The final local review run passed all 625 Framework and 624 modern cases,
  unfiltered, in both configurations, including the clipboard integration test.
  Earlier attempts encountered desktop clipboard access denial (Win32 error 5)
  and transient ImageList allocation failures. Isolated cases and the subsequent
  complete suites passed once desktop access recovered. Keep these environment
  failures visible when diagnosing future runs; do not remove or permanently skip
  the affected tests.
- Review found that hiding a column, removing an earlier column, and saving could
  produce a layout that its own control rejected. Remembered display positions
  are relative ordering keys and can exceed the remaining column count. Restore
  now preserves those keys, and menu ordering uses `CompareTo` so extreme
  nonnegative keys and the -1 sentinel cannot overflow a subtraction comparer.
  [State regressions](../Tests/TestState.cs) cover regular/fast/tree hide-remove
  round trips, unhiding, and extreme-key ordering on both targets.
- Custom column editing and serialization were exercised on ObjectListView,
  FastObjectListView, VirtualObjectListView, TreeListView, DataListView,
  FastDataListView, DataTreeListView, and a concrete typed subclass.
- Actual toolbox insertion passed for the standard control and a concrete typed
  control in a named namespace. The fixture's original global-namespace subclass
  failed Visual Studio toolbox type resolution; it was a fixture/tooling issue,
  not a runtime generic-control failure.
- Column add/hide, selection of visible versus AllColumns entries, Undo/Redo,
  Cancel rollback, generated-code rebuild, and save/close/reopen were exercised.
- Printer editors passed existing-value Cancel isolation, pen Undo/Redo, and the
  supported solid/hatch/gradient/None brush cases. Do not put the modern
  ListViewPrinter runtime DLL in the server designer-plugin directory: duplicate
  load contexts caused `PenData`-to-`PenData` assignment failures. The Framework
  printer copy needed by the client is intentional.
- Modern DataSource metadata uses `AttributeProvider(typeof(IListSource))`.
  The legacy converter string had allowed component names to be serialized as
  string values. BindingSource references and DataSet/People DataMember references
  now round-trip as actual component references on all three bound-control types.
- ImageList PNG import, header-image keys, French/neutral resources, and
  generated-code execution in both cultures passed. These checks do not enable
  BinaryFormatter compatibility switches.
- A visible host exercised focus, cell-editor geometry and commit, rendering and
  hit testing, Windows keyboard input, and actual OLE drag/drop with model identity
  preserved. LTR/RTL cases passed on both runtimes at 96-DPI virtualization and
  actual 144-DPI per-monitor mode. A modern x86 package-consumer smoke check also
  passed; ARM64 and other Windows/Visual Studio/monitor combinations are not claimed.
- The smart-tag Columns action opened the custom editor in action-list context.
  A complete smart-tag View round trip is not yet verified.

### Open acceptance gates, in priority order

1. Make native header-divider dragging work in the modern designer, then verify
   serialization and one-operation Undo/Redo after an actual drag. Width editing
   through Columns/Properties is the current workaround.
2. Verify a complete smart-tag View change and return, including native header
   interaction after returning to Details view.
3. Establish modern designer asset discovery for a consumer using project
   references only. Package-consumer acceptance does not prove that workflow.
4. Repeat the affected packaged-consumer checks after each designer fix; retain
   the Framework designer and runtime-only deployment checks.

The header investigation compared the built-in designer's native child handling
and end-track notifications with the custom designer. `HookChildHandles` is not
accessible through the pinned stable SDK reference API. Unsupported/untested
experiments were reverted; do not silently restore private-reflection workarounds
or label a passing build as proof of native interaction.

### Continuing an iteration

Use the [build/test commands](../README.md#tests), running target frameworks
sequentially because clipboard state is shared. Build the whole solution before
packing so every required client/server asset exists, then run
[`Test-Package.ps1`](../.github/scripts/Test-Package.ps1) against that exact package:

```powershell
dotnet pack ObjectListView\ObjectListView.csproj --configuration Release --no-build --no-restore -p:PackageVersion=3.0.0-iteration --output artifacts\packages
.\.github\scripts\Test-Package.ps1 -PackageDirectory artifacts\packages -PackageVersion 3.0.0-iteration
```

Rerun the unfiltered four-way test matrix on an available Windows desktop,
including the clipboard integration test. Confirm CI results for the committed
revision rather than treating a filtered local run as full coverage.

For desktop acceptance, use a standalone WinForms package consumer in Visual
Studio 2026, a namespaced concrete typed subclass, and an ordinary runtime printer
reference when testing printer editors. Never reference runtime DLLs directly
inside a package's reserved `Design\WinForms` folders: Visual Studio may deduplicate
the package using that wrong directory and skip its designer assets.

The previous desktop fixtures, screenshots, and TRX logs were local session
artifacts, not committed test infrastructure. A fresh checkout must recreate the
desktop consumer and re-exercise the relevant checks; do not assume those local
artifacts are available to another contributor or cloud session. Update this
checkpoint and the [migration guide](v3-migration.md) with new results and remaining
limitations after each iteration. No release publication is implied by a commit.

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
| `SelectedObjects`, `CheckedObjects` | Keep `IList` properties with materialized snapshot getters. |
| `SetSelectedObjects()`, `SetCheckedObjects()` | Accept `IEnumerable` to replace selection and checked state; retain `SelectObjects()` for compatibility. |
| `AddObjects()`, `InsertObjects()` | Accept `IEnumerable`. |
| `RefreshObjects()`, `RemoveObjects()` | Accept `IEnumerable`. |
| `CopyObjectsToClipboard()` | Accept `IEnumerable`. |
| `TypedObjectListView<T>` | Expose corresponding `IEnumerable<T>` methods; retain `IList<T>` selection and check properties. |

Use non-generic `IEnumerable` at the heterogeneous UI boundary, matching the existing `SetObjects()` contract. Typed controls and adapters provide convenient LINQ access.

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

The enumerable milestone is implemented; see the [migration guide](v3-migration.md)
for the new signatures, enumeration semantics, and retained control-specific behavior.
The later phases below are not part of this milestone.

### Immediate generic-control design

`ObjectListViewBase` owns shared UI behavior, `TypedObjectListView<T>` owns typed
model storage/operations, and `ObjectListView` retains the non-generic facade.
The control-design work retains the existing Framework designer; modern designer
acceptance remains a distinct platform milestone. The control-design work focuses on:

- Canonical row, selection, and check-state dispatch through typed and legacy overrides.
- Snapshot `Objects` access, with separate read-only source hooks and an inexpensive
  display-order `Rows` view.
- Non-virtual typed command overloads that preserve the existing virtual command pipeline.
- `TypedObjectListViewAdapter<T>` for existing controls and typed callbacks, with weak
  column-adapter caching and a single underlying callback slot.
- Private accepted mutation batches, event-payload isolation, and consistent model equality.

Custom source subclasses migrate from `Objects` source overrides to
`GetModelObjectsCore`/`SetModelObjectsCore`; public getters no longer drive UI
source enumeration. Generic virtual/tree controls and new data-source interfaces
remain separate work. See the migration guide for the supported hooks and ownership rules.

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

- Split the large `ObjectListViewBase.cs` into focused partial files without unnecessarily redesigning the inheritance hierarchy.
- Remove redundant compatibility APIs. For example, `CheckedObjectsEnumerable` becomes unnecessary once `SetCheckedObjects()` accepts an enumerable.
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
