# ObjectListView v3 implementation plan

[Documentation index](README.md) | [Migration guide](v3-migration.md)

**V3 is a work in progress, not a completed release.** This plan tracks remaining
work and acceptance gates; the migration guide describes implemented contracts.

## Goals and compatibility policy

V3 is an API cleanup, not a rewrite. Preserve useful behavior and source
compatibility where practical. Every breaking change must have a documented
replacement or retirement rationale.

Priorities are enumerable-friendly APIs, explicit grouping, shared style
resolution, consistent rendering/hit testing, and focused cleanup.

Runtime targets are **.NET Framework 4.6.2** and **.NET 10 for Windows**.
Framework designer support is retained, and modern designer support is implemented
for NuGet consumers. .NET 8 and older Framework targets are out of scope.
Before release, review Framework support: Microsoft's support for 4.6.2 ends
January 12, 2027. Changes to Windows compatibility require a separate support decision.

## Current status

| Workstream | Status |
| --- | --- |
| Platform and test infrastructure | Implemented: SDK-style projects, both runtime targets, MSTest, serial STA UI fixtures, and preserialized modern resources |
| Enumerable APIs | Implemented: sequence inputs, snapshot getters, private accepted batches, and updated dependent commands/interfaces |
| Generic control foundation | Implemented: shared UI base, typed storage, compatibility facade, typed adapters, source hooks, and display-order `Rows` |
| Saved layouts | Implemented: shared versioned format; legacy BinaryFormatter layouts must be recreated |
| DPI and demo layout | Implemented: host DPI configuration, column scaling, and corrected layout/drawing geometry |
| Dark mode | Implemented: shared palette, stock .NET 10 controls, Framework fallbacks, and demo theme selection |
| Modern designer | Implemented for NuGet: client/server/protocol integration, header resize/Undo, smart tags, and column/printer editors |
| Grouping redesign | Planned |
| Unified style precedence | Planned; dark mode does not complete this phase |
| Shared rendering/hit-test layout | Planned |
| Final API cleanup and release | Pending |

## Platform migration checkpoint

The runtime migration and NuGet designer integration are implemented. The grouping,
styling, and layout redesigns remain separate workstreams.

### Implemented foundations

- Both demos and runtime/test projects target `net462;net10.0-windows`.
  Modern designer assets are separate from runtime dependencies.
- Framework resources are retained; its designer and column editor live in the
  separate `ObjectListView.Design.Framework` assembly. Core runtime assemblies
  have no System.Design, WindowsBase, or Designer SDK dependency. Modern resources do not
  require a BinaryFormatter compatibility switch.
- Saved layouts use the same format on both runtimes. State regressions cover
  hidden/removed columns and remembered display-order keys.
- Modern column and copied-value printer editors, binding metadata, localization,
  image resources, and generated-code rebuilds have packaged-consumer coverage.
- Header resizing uses public designer adorner/behavior APIs, with bounded
  preview, cancellation, and one transaction per gesture. Do not reintroduce
  private-reflection hooks.
- Modern header resizing participates in the selected control's `GetGlyphs`
  pipeline instead of an independently registered adorner. Smart-tag action lists
  retain their identity across refreshes. Handle destruction cancels an active
  resize, and canceled source-control checkout does not start a gesture.
- The Framework designer shares the same resize behavior and transactions;
  native width changes now persist and participate in one-operation Undo/Redo.
- Shared gesture sources live in `ObjectListView.Design.Shared`, outside runtime
  and host-specific projects. Framework Escape cancellation uses a scoped,
  in-process UI-thread message hook, independent of the host's WinForms/WPF pump.
- The modern designer's Framework 4.7.2 client shares printer editor sources under its own
  `BrightIdeasSoftware.Design.Client.Printing` namespace. It neither references
  nor ships runtime ObjectListView/ListViewPrinter assemblies, preventing
  Framework types from leaking into the modern server's load context.
- Package checks reject runtime DLLs in reserved designer directories, client
  runtime assembly references, and runtime-namespaced client data types.
- CI builds/tests both configurations and targets and checks package contents,
  fresh consumers, and two-way layout interchange.
- Framework NuGet consumers receive a non-copy-local design reference from
  `build`/`buildTransitive`, including legacy project imports. Source consumers import the Framework designer target.
  Separate source-reference checks cover `net462` and compatible `net48` hosts.

### DPI, dark mode, and review follow-up

- Demos use system awareness on Framework and `PerMonitorV2` on .NET 10.
  Tabs inherit parent autoscaling; split-panel content uses docked layout.
- Initial column widths and limits scale from 96-DPI values. Modern DPI changes
  preserve current user sizing; handle recreation must not scale widths twice.
- Business-card and task-button geometry is DPI-aware and measured against content.
- `ObjectListViewTheme` owns palette decisions. Explicit editor colors, including
  system-color assignments, are preserved through WinForms property metadata.
- The .NET 10 demo retains stock control rendering, with targeted background/header
  color corrections. Framework-only dark fallbacks use buffered painting.
- Demo banners and illustrative colors are configured explicitly, not inferred
  from existing RGB values. The theme menu reloads examples while preserving
  window position and the selected tab.
- Drawing resources are scoped. Control-owned image-list copies are disposed on
  replacement, control disposal, and failed construction/attachment.
- Serialized image lists are copied before appending checkbox states. Setup waits
  until `EndInit` so late-loaded designer icons and keys are preserved.
- Runtime theme tests and actual-demo tests are separate. Demo tests reference the
  built assembly and cover control wiring, theme reloads, captions, images, and
  print preview.

The designer follow-up passes the full Debug/Release test matrix on both runtimes.
Package checks also exercise a concrete typed designer fixture and its View round
trip on both runtimes, independently of the desktop acceptance below.
Historical test totals and temporary package names are not release criteria.

### NuGet designer acceptance

Fresh package consumers in an owned Visual Studio 2026 instance cover:

- Actual OS mouse input for header resizing, saved Width values, one-operation
  Undo/Redo, and Escape cancellation without adding an Undo operation.
- The actual smart-tag LargeIcon/Tile/Details round trip, followed by another
  successful header resize.
- Column-editor Add/Remove/OK and consistent saved column collections.
- Modal brush and pen editors, all brush preview styles, saved runtime values,
  and Cancel preserving the original values. Client-only data types are not
  serialized into consumer code.
- Rebuilding and running designer-edited code without Designer SDK runtime
  dependencies, plus the two-runtime package/layout checks.
- The retained Framework designer, including header resizing, saved widths,
  and one-operation Undo/Redo.
- Discovery of the separated Framework designer and column editor from NuGet and
  project references, with no designer DLLs copied into normal application output.

Modern designer support is scoped to NuGet consumers. Project-reference-only
designer discovery is not part of this milestone; runtime project references
remain supported. Do not infer designer discovery from runtime rendering.

ARM64 and every Windows/Visual Studio/monitor configuration are not claimed.
Local desktop fixtures and screenshots are not reproducible acceptance evidence
unless recreated from a fresh checkout.

## Phase 1: establish API and performance baselines

Test infrastructure is ready; the API inventory and performance budgets remain.

- Classify public APIs and extension points as keep, replace, or remove.
- Document replacements before introducing breaking changes.
- Measure regular, fast, virtual, bound, and tree controls with small/large sources.
  Include sorting, filtering, selection, checks, and batch mutations.
- Preserve concrete test identities and coverage, not just passing totals.

**Exit criteria:** agreed API inventory, reproducible measurements, regression
budgets, and retained baseline cases passing without new skips.

## Phase 2: enumerable APIs and generic controls

**Implemented.** See the [migration guide](v3-migration.md) for signatures,
ownership, null behavior, and subclass hooks.

Preserve these invariants:

- Consume sequences once before mutation; enumeration failures do not partially
  apply a batch.
- Keep independent snapshots and read-only source/display-order access distinct.
- Preserve cancellation, notification subscriptions, ordering, and control-specific
  behavior.
- Typed commands dispatch through the existing virtual pipeline; accepted event
  batches are private, validated snapshots.

Generic virtual/tree controls and replacement data-source interfaces remain
separate work, not implied by the current typed control.

## Phase 3: explicit grouping configuration

Replace `AlwaysGroupByColumn` and `AlwaysGroupBySortOrder` with one configuration
built on `GroupingParameters`.

Separate group column/order from item sort column/order, with an explicit choice
between following sorting and fixed grouping. Use the same model for header
clicks, menus, programmatic calls, and virtual grouping. Retire related switches
only after their useful behavior has a replacement.

**Acceptance:** independent group/item ordering, cancellation, custom comparers,
non-groupable columns, collapsed-group state, and virtual grouping.

## Phase 4: unified style resolution

Unify shared row/cell, header, and tooltip concepts without creating an oversized
renderer interface:

| Concept | Responsibility |
| --- | --- |
| Values | Font, colors, spacing, alignment, and borders |
| State | Selected, focused, hot, pressed, disabled, and combinations |
| Resolution | Per-property inheritance, explicit overrides, and callback precedence |
| Context | Bounds, DPI, RTL, model, column, and state |

Distinguish unspecified values from explicit overrides. Integrate rows/cells,
then headers, then tooltips; temporary adapters need a retirement plan.

**Acceptance:** state combinations, column overrides, callbacks, high contrast,
resource ownership, and designer serialization.

## Phase 5: shared rendering and hit-test layout

Extend existing renderers rather than introducing parallel geometry calculations.
Reuse layout for painting, hit testing, tooltip targets, and editor placement.
Keep native rendering when customization is unnecessary.

**Acceptance:** matching geometry under scrolling, column reordering, DPI changes,
RTL, and tree indentation; preserve resizing, sorting, checkboxes, keyboard
behavior, and accessibility. Use visible Windows hosts for interaction checks.

## Phase 6: cleanup and preview release

Once contracts stabilize:

- Split `ObjectListViewBase.cs` into focused partial files where it improves cohesion.
- Retire redundant compatibility APIs such as `CheckedObjectsEnumerable` only after
  their replacements and migration impact are established.
- Update demos, designer-generated code, and migration examples.
- Set assembly/package versions deliberately, preserving signing and documenting
  source/binary compatibility.
- Release a v3 preview before freezing the final API.

**Exit criteria:** retained tests pass in Debug and Release, new contracts have
focused coverage, demos run, representative consumers and serialization work,
and every public breaking change is documented.

## Continuing an iteration

Use the [build/test workflow](../README.md#tests). Run frameworks sequentially
because clipboard state is shared. Build the whole solution before packing:

```powershell
dotnet pack ObjectListView\ObjectListView.csproj --configuration Release --no-build --no-restore -p:PackageVersion=3.0.0-iteration --output artifacts\packages
.\.github\scripts\Test-Package.ps1 -PackageDirectory artifacts\packages -PackageVersion 3.0.0-iteration
```

For designer acceptance, use a fresh package consumer in Visual Studio 2026 and
a namespaced concrete typed subclass. Reference runtime printer DLLs from their
normal deployment location, never the package's reserved `Design\WinForms` folders.

`Test-Package.ps1` creates `artifacts\PackageSmoke\consumer\PackageConsumer.sln`
(under the requested output directory when overridden). Its project records the
exact local package version, source, and isolated package cache, so it can also
be reopened in Visual Studio without command-line restore properties.
Both Debug and Release are built for the consumer. Cache paths are kept short
because the Framework designer client cannot load assemblies beyond MAX_PATH;
use a short output directory when the checkout path is unusually long.
Open `DesignerSmokeForm.cs` in the designer, select the list, and verify:

- The Columns editor creates, removes, hides, and reorders `OLVColumn` components;
  saving and reopening retains both `AllColumns` and the visible columns.
- The smart-tag View selector changes to LargeIcon or Tile and back to Details;
  header interaction still works after returning.
- Drag a header divider, save, and inspect its serialized Width. One Undo restores
  the old width; one Redo reapplies it. Cancel a second gesture and confirm that
  neither the width nor the Undo history changes.
- Rebuild and run the consumer after saving; repeat the appropriate checks on
  Framework without introducing modern Designer SDK runtime dependencies.
- For printer editors, reference the matching runtime ListViewPrinter assembly,
  edit a format's brush or border pen, and verify both OK and Cancel. Saved code
  must use runtime data types, never `Design.Client.Printing` types.

Use only the owned Visual Studio instance for desktop input checks. Do not send
pointer-bearing native messages across processes using local buffers.
Confirm CI for the committed revision and update this plan and the migration
guide with remaining limitations. A commit is not a release publication.
