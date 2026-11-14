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
| Dark mode | Implemented: shared palette, independent list refresh, opt-in extensible form theming, stock .NET 10 controls, and Framework fallbacks |
| Modern designer | Implemented for NuGet: client/server/protocol integration, header resize/Undo, smart tags, and column/printer editors |
| Grouping redesign | Implemented: explicit policy, deprecated setting adapters, independent item ordering, cancellation, and collapsed-state restoration |
| Unified style precedence | Implemented: per-property sheets, combined-state rules, callbacks, and legacy adapters |
| Shared rendering/hit-test layout | Implemented: renderer-owned cell/header geometry, shared targets, tree indentation, and editor placement |
| Final API cleanup and release | Cleanup and preview packaging implemented; publication and interactive release acceptance pending |

## Platform migration checkpoint

The runtime migration, NuGet designer integration, and grouping redesign are
implemented, together with shared style resolution and renderer-owned layout.
Preview cleanup and packaging are implemented. Preview publication, performance
baselines, and interactive release acceptance remain.

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
- One `nameof`-keyed map defines native system colors and dark colors. Light and high
  contrast ignore shared overrides; dark custom drawing uses defined dark colors
  on both runtimes. `IThemeColors` exposes named read-only properties. One
  `ThemePalette` implementation backs the allocation-free live reader and explicit
  immutable snapshots, without a color enum or a separate resolver type.
- System is the sole automatic mode: it follows WinForms on .NET 10 and Windows'
  preference on Framework. Selecting System does not replace a .NET 10 host's
  explicit application mode.
- `RefreshTheme()` reapplies OLV styling independently of host form theming.
  Applications can bridge their own theme manager without changing WinForms'
  application color mode or unrelated controls.
- Optional `FormTheme` supports `IThemeAware` and post-styling callbacks. It
  preserves explicit system colors and application-owned renderers, and detaches
  handlers/restores captured appearance when controls leave its subtree.
- Opt-in first-frame preparation delegates to standalone `FormFirstFrame`, so
  dark startup does not require demo-specific display overrides or form theming.
- The .NET 10 demo retains stock control rendering, with targeted background/header
  color corrections. Framework-only dark fallbacks use buffered painting.
- Demo banners and illustrative colors are configured explicitly, not inferred
  from existing RGB values. The theme menu reloads examples while preserving
  window position and the selected tab.
- Demo defaults consume central named colors without mode-dependent RGB branches.
  Theme-aware tabs update banners, header accents, and links through `IThemeColors`;
  business-card defaults remain live while preserving explicit renderer styles.
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

Generic virtual, fast, tree, and data-bound controls are also implemented.
Replacement data-source interfaces remain separate work.

## Phase 3: explicit grouping configuration

**Implemented.** `GroupingConfiguration` selects following sorting or fixed
grouping, independently of item ordering. Fresh `GroupingParameters` retain
operation-local event customization. Header clicks, menus, programmatic calls,
and virtual grouping use the same resolution pipeline. Regular and fast virtual
groups share one construction algorithm; policy and orchestration live in
`ObjectListViewBase.Grouping.cs` rather than expanding the main control file.

`AlwaysGroupByColumn`, `AlwaysGroupBySortOrder`, and the column-zero item-ordering
switch remain supported as deprecated adapters. See the migration guide for
the default-ordering change and explicit compatibility opt-in.

Focused regressions cover independent directions, multi-sort/header clicks,
unsorted grouped items, cancellation, model/group comparers, non-groupable and
hidden columns, null keys, collapsed state, menus, legacy settings, and regular,
fast, and custom virtual strategies. Custom strategies must not publish lookup
mappings for groups that have not been accepted. Rebuilds invalidate applied
groups before replacing rows, so canceled regrouping cannot retain stale row
references or virtual mappings.

## Phase 4: unified style resolution

**Implemented.** Shared row/cell, header, and tooltip concepts use `StyleSheet`,
`StyleValues`, ordered `StyleRule` state overrides, and `StyleContext`, without an
oversized renderer interface:

| Concept | Responsibility |
| --- | --- |
| Values | Font, colors, spacing, alignment, and borders |
| State | Selected, focused, hot, pressed, disabled, and combinations |
| Resolution | Per-property inheritance, explicit overrides, and callback precedence |
| Context | Bounds, DPI, RTL, model, column, and state |

Null means unspecified; explicit Regular, zero borders, and empty padding are
preserved. Normal cell values precede legacy format events; owner-drawn state
rules and the final `StyleResolving` callback consume their results. Headers
inherit per property from legacy styles before shared overrides; native tooltips
apply shared/event overrides temporarily and restore their settings when popped.
High contrast keeps system colors authoritative, and derived fonts are
control-owned. Existing style APIs remain supported adapters in the preview cleanup policy;
they are not obsoleted or removed.

Focused regressions cover combined/excluded states, scope and callback
precedence, regular/virtual/tile rows, pressed capture loss, actual cell/header
drawing, native tooltip restoration, high-contrast color decisions, font reuse
and ownership, and designer content/explicit-default serialization.
See the [migration guide](v3-migration.md#unified-style-resolution) for precedence
and backend limits: native cells use normal styling; state rules/custom cell
geometry require owner drawing, and native tooltips do not support arbitrary
border colors/widths or content alignment. Shared header image/vertical-text
geometry is covered by Phase 5.

## Phase 5: shared rendering and hit-test layout

**Implemented.** Existing renderer measurement/alignment hooks produce immutable
`CellLayout` snapshots. Standard checkbox/image/text cells share their component
rectangles between painting, hit testing, tooltip targets, and text editors.
Painting, hit testing, and editing use the same live native cell bounds, including
column-zero reordering and horizontal/vertical scrolling. Padding and indentation
are applied once; checkbox and button targets no longer calculate different
padded areas from painting. Tree content, expansion glyphs, and editors use the
same level indentation and RTL arrangement.

Headers share image/text, checkbox, sort/filter reservation, and measurement
geometry. Shared horizontal/vertical alignment also applies to header images and
vertical text; rotated drawing preserves the caller's graphics transform.
`OlvListViewHitTestInfo.HitBounds` and tooltip `TargetBounds` expose the hovered
renderer target, and tooltip style contexts consume that target. Native tooltip
positioning and native cell/header rendering remain unchanged when customization
is unnecessary. Bespoke renderers retain their existing drawing/hit-test hooks;
their custom parts can supply `HitBounds` without changing `IRenderer`.

Visible Windows-hosted regressions cover regular/virtual cells, reordered and
scrolled columns, RTL, padded trees, wrapped/GDI/GDI+ text, image/font changes,
DPI drawing contexts, narrow cells, actual editor activation, checkbox/Space
interaction, sorting, resizing, and accessibility roles. Physical cross-monitor
DPI transitions remain an interactive release-acceptance check.

## Phase 6: cleanup and preview release

**Cleanup and local preview preparation implemented; release not published.**

- `ObjectListViewBase` now separates model operations, native messages, columns,
  checks, editing, images, events, interaction, decorations, and saved state into
  focused partial files. Existing grouping/style/layout partials are retained;
  moved implementations and extension-point signatures are unchanged.
- Typed column/data-source/row helpers have separate files. Base and tree event
  declarations, nested enums, and the nested tree renderer are grouped with their
  owning controls. Column appearance properties live with `OLVColumn`.
  Theme services are grouped under `Theming`; the column-selection form and its
  designer/resource files live together under `SubControls`, retaining the same
  resource name and public types.
- `CheckedObjectsEnumerable` is removed. The migration guide establishes
  sequence-command and snapshot-property replacements, including the difference
  between method overrides and the old alias's property dispatch.
- The preview API inventory keeps working grouping/style adapters, custom
  renderer hooks, and data-source interfaces; it does not remove unrelated
  obsolete members or introduce replacement interfaces.
- Current demos and designer-generated code use retained APIs; migrated tests,
  examples, and packaged consumers exercise the replacement check APIs.
- Runtime assembly identity is fixed at `3.0.0.0`, signing is retained, and local
  packing defaults to `3.0.0-preview`. CI builds stable and preview artifacts
  with matching binary versions. Source/binary compatibility is documented.

Local automated acceptance passes solution builds and the retained test matrix
in Debug and Release on both targets, without skips. Fresh preview consumers
build in both configurations, run on `net462`, `net48`, and .NET 10, and exchange
saved layouts in both directions. Framework source-reference consumers also
build and run on `net462` and `net48`. Actual-demo fixtures cover both demos;
these checks do not replace the interactive gates below.

Remaining release gates:

- Establish the Phase 1 performance measurements and regression budgets.
- Recreate interactive designer and physical cross-monitor DPI acceptance from
  a fresh checkout; do not infer these checks from automated UI tests.
- Confirm CI on the revision selected for publication, then explicitly publish
  a preview through the existing Publish NuGet workflow.
- Gather preview feedback before freezing the final API or publishing stable V3.

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
