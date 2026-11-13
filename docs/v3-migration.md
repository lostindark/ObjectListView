# Migrating to ObjectListView v3

[Documentation index](README.md) | [Implementation plan](v3-plan.md)

**V3 is still a work in progress.** This guide covers implemented API and platform
changes, not a completed release. Recompile consumers and update affected
overrides; v3 is not a drop-in binary replacement.

## Grouping independently of item sorting

`Grouping` is an immutable configuration, applied on the next sort, `BuildGroups()`,
or rebuild. `ShowGroups` still enables/disables grouping without forgetting it.
The default follows the primary item-sort column and direction:

```csharp
list.Grouping = GroupingConfiguration.FollowSorting();
list.Grouping = GroupingConfiguration.FollowSorting(SortOrder.Ascending);

// Keep departments ascending while sorting their items by name descending.
list.Grouping = GroupingConfiguration.Fixed(departmentColumn, SortOrder.Ascending);
list.ShowGroups = true;
list.Sort(nameColumn, SortOrder.Descending);
```

Add a fixed column to the list before assigning the configuration. It must be
groupable and belong to `AllColumns` or the visible columns; hidden columns are
supported. A removed or subsequently non-groupable fixed column is an error on
the next grouping operation. Tree controls do not support grouping, and virtual
controls require a grouping strategy.

The optional group order is independent of item ordering:

- `null` follows the primary item-sort direction, defaulting to ascending without
  an item sort. `Fixed(column)` uses this behavior.
- `Ascending` / `Descending` fix group direction.
- `None` preserves group discovery order, including null keys.

Grouped items now use all active `SortDescriptions`, without implicitly sorting
by column zero. An empty sort collection removes item sorting while keeping
grouping enabled; `Unsort()` retains its existing behavior of also disabling
groups. `BuildGroups()` no longer invents an item sort. The retained
`BuildGroups(column, order)` overload still requests that item sort and retains
its legacy default-direction behavior for `None`.

Following a non-groupable sort column retains the last valid grouping column
and group order. Without one, the first visible groupable column is used; with
no eligible column, rows are displayed flat and item sorting still works.
Header clicks update item sorting. The "Group by" menu now selects fixed grouping
without changing item sorting; unlocking restores follow-sorting mode.

### Deprecated grouping settings

The old settings still compile and work, but produce obsolete warnings:

| Old setting | Replacement |
| --- | --- |
| `AlwaysGroupByColumn = column` | `GroupingConfiguration.Fixed(column, groupOrder)` |
| `AlwaysGroupByColumn = null` | `GroupingConfiguration.FollowSorting(groupOrder)` |
| `AlwaysGroupBySortOrder = None` | Pass `null` for `groupOrder`, not `None` |
| `SortGroupItemsByPrimaryColumn = true` | Explicitly sort items by column zero |
| `GroupingParameters.SortItemsByPrimaryColumn` | Edit `SortDescriptions` instead |

The column and direction adapters share the new configuration and can still be
assigned in either order. Explicitly assigning `SortGroupItemsByPrimaryColumn`
retains its legacy conditional column-zero behavior: it applies only when
following sorting with one active sort field. Its historical getter/default
value remains `true`, but an unassigned setting no longer opts into that legacy
behavior. Assigning `Grouping` resets this compatibility override.

### Grouping callbacks and state

`GroupingParameters` remains a fresh, mutable operation-local extension point.
Changes made by events do not rewrite the persistent grouping policy.
`BeforeSorting` changes to item sorting are reflected in follow-sorting grouping,
unless the handler explicitly overrides the grouping column or order.

Use `GroupingParameters.ModelComparer` for custom item ordering on both regular
and virtual lists, even without item-sort fields. It takes precedence over
`ItemComparer` and sort descriptions. The existing list-item `ItemComparer`
remains supported on regular lists; built-in virtual grouping rejects it unless
`ModelComparer` is also supplied. A custom `GroupComparer` takes precedence even
when group order is `None`.

Canceling a sort/regroup at either pre-creation event leaves accepted groups,
sorting metadata, and built-in virtual mappings unchanged; completion events
are not raised for canceled operations. Source changes are separate accepted
operations: rebuilding rows clears their old applied groups first. If regrouping
is canceled during that rebuild, the new rows remain accessible without groups;
the source change is not rolled back.

Custom virtual strategies must also keep their lookups tied to accepted
`ListView.OLVGroups`, not publish mappings during `GetGroups`, since prepared
groups can be discarded. The control copies accepted group containers, so a
strategy can reuse its preparation list without mutating the applied view.

Collapsed state is restored by grouping-column identity and group key, including
null, when the effective grouping column is unchanged. `AboutToCreateGroups`
can override this restored state before groups are committed.

## Unified style resolution

`StyleSheet` is the shared appearance model for cells, headers, and tooltips.
`StyleValues` contains optional font, font style, colors, padding, alignment, and
border properties. `null` inherits; explicit `FontStyle.Regular`, zero border
width, and empty padding override inherited values. Set a property back to
`null` to restore inheritance.

```csharp
list.OwnerDraw = true; // Required for cell state rules and custom cell geometry.
list.RowAppearance.Normal.BackColor = Color.Beige;
nameColumn.CellAppearance.Normal.ForeColor = Color.Navy;

var selected = new StyleRule {
    RequiredState = StyleState.Selected | StyleState.Focused,
    ExcludedState = StyleState.Disabled
};
selected.Values.ForeColor = Color.White;
selected.Values.BackColor = Color.DarkBlue;
list.RowAppearance.States.Add(selected);

list.HeaderAppearance.Normal.FontStyle = FontStyle.Bold;
nameColumn.ToolTipAppearance.Normal.Padding = new Rectangle(4, 2, 4, 2);
```

Rules match all `RequiredState` flags and none of the `ExcludedState` flags.
Matching rules apply in collection order, not by inferred specificity.
Selected, focused, hot, pressed, and disabled are independent flags.
`UseHotControls` (enabled by default), hyperlinks, or hot-item tracking must be
enabled for pointer states. Pressed rows reset on mouse-up or capture loss.

Cell normal values resolve from control `RowAppearance`, column `CellAppearance`,
row `Appearance`, then subitem `Appearance`, per property. They are applied while
rows are built, before `FormatRow` and `FormatCell`. Existing row formatters,
alternating colors, disabled styles, hyperlinks, and hot styles remain supported.
Their existing pipeline is retained; shared normal values precede the format
events, and shared state rules overlay the resulting legacy appearance when
owner-drawn. State scopes use the same control/column/row/subitem order.
Explicit typed row assignments in `FormatRow` override shared normal column
values even when assigning the current value. Use `Item.GetSubItem(index)` for
typed cell assignments when explicitly preserving that cell's current value
against a row override in the same handler. Untyped `SubItems` changes to other
values are also preserved; callbacks without shared normal styling retain their
legacy behavior.
Change normal values before setting objects, or rebuild/refresh the affected
rows afterward. Invalidate the control after changing state rules.

Headers and tooltips resolve the control sheet, then the column sheet; each
scope applies normal values followed by matching rules. Header customization
works with either value of `HeaderUsesThemes`. Legacy `HeaderFormatStyle` now
inherits **per property** across normal/hot/pressed states and control/column
styles, instead of replacing an entire inherited style with an empty state.
An explicitly assigned legacy frame width of zero suppresses the inherited
frame; `ResetFrameWidth()` restores inheritance. `SimpleItemStyle` and
`CellStyle` likewise distinguish an assigned `FontStyle.Regular` from an unset
font style; `ResetFontStyle()` restores inheritance.

`StyleResolving` receives an independent value snapshot and a `StyleContext`
containing the target, model, column, bounds, DPI, RTL, and combined state.
It runs after shared inheritance/state rules. Existing `HeaderDrawing` and
tooltip events still run afterward, so their drawing/event overrides remain
authoritative. Windows high-contrast colors take priority over resolved colors;
fonts, spacing, and geometry remain customizable. Native cell painting uses
normal values and existing native selection behavior; use owner drawing for
state callbacks and custom borders/alignment. Custom renderers can opt into
`ResolveStyle(...)`; `BaseRenderer` consumes it automatically.

Native tooltips consume font, colors, padding, and border visibility. Explicit
event colors temporarily disable visual theming so Windows actually displays
them; popping the tooltip restores its prior settings. Arbitrary tooltip border
colors/widths and content alignment are not supported by the native tooltip
backend. Header image/vertical-text layout retains its existing alignment rules;
the shared alignment applies to ordinary horizontal text. Shared layout and
hit-test redesign remains a separate phase.

Styles and rules serialize as nested designer content, including explicit zero
and Regular values. Application-supplied fonts remain application-owned and must
outlive their use. Fonts derived by OLV are cached per control and disposed with
it; returned resolved fonts must not be disposed by callers.

The legacy style types are retained as input adapters, not a second rendering
API. Their removal, if any, belongs to the final API-cleanup phase after
consumer/designer migration; this phase does not obsolete or remove them.

## Theme configuration and color access

`ObjectListViewColorMode` now contains only `System`, `Light`, and `Dark`.
Replace `Default` with `System`. System is the default automatic mode: it follows
the app's effective WinForms mode on .NET 10 and the sampled Windows preference
on Framework. Its enum values are now System = 0, Light = 1, Dark = 2; migrate any
numeric settings from earlier development builds by name rather than reusing
their old values.

On .NET 10, selecting System through OLV or `FormTheme.SetColorMode` preserves the
host's existing application mode. Hosts that want Windows preference tracking
explicitly call `Application.SetColorMode(SystemColorMode.System)` before
creating controls. Framework hosts can reassign System to resample Windows.

Named static color properties such as `ObjectListViewTheme.WindowColor` have been
removed. Read named colors through the live `IThemeColors` interface and configure
dark overrides separately using `nameof` keys:

```csharp
IThemeColors colors = ObjectListViewTheme.Current;
Color background = colors.WindowColor;
ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.WindowColor), Color.Navy);
```

`Current` is allocation-free and follows later mode and override changes.
Use `new ThemePalette(colors.Dark, colors.HighContrast)` for an immutable
snapshot. `ThemePalette` backs both forms of the same named-color contract; the
color-role enum and indexer are removed. Light and high contrast ignore shared
dark overrides.

`IThemeAware.ApplyTheme`, `FormTheme.Apply`, and form customization/theme-change
callbacks now accept `IThemeColors`, not `ThemePalette`. Update explicitly typed
handlers accordingly; callers can still pass a `ThemePalette` snapshot.

## Runtime targets, designers, and saved layouts

See [Designer support](designer-support.md) for the support matrix, setup,
assembly architecture, and troubleshooting.

Runtime targets are **.NET Framework 4.6.2** and **.NET 10 for Windows**.
`ObjectListView.dll` on either target does not depend on System.Design,
WindowsBase, or the Designer SDK.

Framework designer support now lives in the separate `net462`
`ObjectListView.Design.Framework.dll`, including its column collection editor.
NuGet supplies a non-copy-local design reference through its build targets, so
normal application output and publishing do not include the designer assembly.
Runtime-used overlay converters remain in `ObjectListView.dll`.

For source/project references, keep the runtime project reference and import the
designer target in the consuming project:

```xml
<ItemGroup>
  <ProjectReference Include="..\ObjectListView\ObjectListView.csproj" />
</ItemGroup>
<Import Project="..\ObjectListView.Design.Framework\ObjectListView.Design.Framework.targets" />
```

The import adds a non-copy-local designer project reference for .NET Framework
hosts, including `net462` and `net48`. It does nothing for modern .NET hosts.
The dependency graph stays consumer -> designer -> runtime, never runtime -> designer.
Runtime-only source consumers do not need the import.

For explicit DLL references, add a reference to the matching Framework designer
DLL with `Private` set to `false` when design support is needed. Custom designer
projects deriving from the moved designer/editor types also need this assembly
reference. Code that intentionally uses these design-time editors at runtime must
deploy that assembly explicitly.

Modern designer support is included
for Visual Studio 2026; install the NuGet package rather than referencing a
standalone DLL. Its Framework 4.7.2 designer client does not raise your
application's runtime requirement.

Modern designer discovery is supported through NuGet packages, not
project-reference-only or standalone-DLL development setups.

Select the control to resize header dividers in either designer. Each gesture
is one Undo/Redo operation; Escape cancels it. Fixed-width and free-space-filling
columns retain their resize restrictions. Columns and Properties editors are
also available.
For ListViewPrinter, reference the matching runtime assembly from its normal
deployment location, not DLLs under the package's `Design\WinForms` directories.

`SaveState()` and `RestoreState(byte[])` keep their signatures. Both runtimes
use the same versioned layout format, but **old BinaryFormatter layouts cannot
be restored**. Discard those bytes and save a replacement.

`RestoreState` returns `false` for unsupported, malformed, or column-count-
mismatched layouts without changing the current layout. Null is an argument
error; errors applying a valid layout propagate.

```csharp
if (!list.RestoreState(savedLayout)) {
    // Notify the user and save a replacement layout when appropriate.
}
```

Layouts match columns by `AllColumns` position and do not store selection or
scroll position.

## DPI-aware hosts and column widths

Your application configures process DPI awareness; ObjectListView does not.
The demos use system awareness on Framework and `PerMonitorV2` on .NET 10.

Initial column widths and minimum/maximum widths are **96-DPI design values**.
ObjectListView scales them after initialization, including hidden and fixed-width
columns. Remove duplicate manual startup scaling. On .NET 10, monitor DPI changes
also rescale current user widths; recreating a handle at the same DPI does not
scale them again.

Widths assigned after initialization and handle creation, and widths in saved
layouts, remain device-pixel values. Saved layouts are not DPI-independent.
Framework system-aware windows may still be bitmap-scaled between monitors.

## Custom selection colours

`UseCustomSelectionColors` has been removed. It had no effect since v2.9.
Remove assignments to it, including any saved in designer code. With `OwnerDraw`
enabled, configure `SelectedBackColor` and `SelectedForeColor` directly.

## Static utility methods

Stateless helpers are now static. For example, use
`ColumnComparer.CompareValues(x, y)`, `ModelObjectComparer.CompareValues(x, y)`,
and `HeaderControl.HasCheckBox(column)` rather than calling them on instances.
In subclasses, call protected utility methods directly instead of through `this`.

## Configuration and parameter names

Mutable static settings, including filtering labels, images, the editor registry,
and tree indentation, are now properties. Their names, defaults, and writable
behavior are unchanged; update field-specific reflection or `ref`/`out` usage.

Override parameter names now match their base/interface declarations. Update named
arguments using the old names, for example `shouldPreserveSelection` to
`shouldPreserveState` on virtual `BuildList`, and `text` to `value` on data-source
`SearchText`.

## Generic control hierarchy

`TypedObjectListView<T>` is now a control with owned `List<T>` storage, not a
wrapper. Both it and `ObjectListView` derive from `ObjectListViewBase`;
`ObjectListView` is the `<object>` specialization.

| Previous usage | Migration |
| --- | --- |
| `new TypedObjectListView<Person>(existingControl)` | Create `new TypedObjectListView<Person>()`, configure columns, and add it to the form |
| Access through `.ListView` on the old wrapper | Access the typed control's properties directly |
| Keep an existing control and add typed access | Use `new TypedObjectListViewAdapter<Person>(existingControl)` |
| Integrations accepting `ObjectListView` | Accept `ObjectListViewBase` to support typed controls |

The adapter does not create or dispose the adapted control. Its `ListView` is
read-only; create another adapter to adapt a different control.

```csharp
var people = new TypedObjectListViewAdapter<Person>(existingControl);
people.BooleanCheckStateGetter = person => person.IsActive;
people.CellToolTipGetter = (column, person) => person.Name;
people.GetColumn(0).AspectGetter = person => person.Name;
```

On a typed control, use `GetTypedColumn(...)` for typed column delegates and
`control.Typed` for typed callback configuration. `GetColumn(...)` and the
control's check-state/tooltip callbacks retain their shared object-based contracts.

For designer/toolbox use, declare a concrete, non-generic subclass in a named
namespace:

```csharp
public class PersonListView : TypedObjectListView<Person> { }
```

Virtual, tree, and data-bound controls retain their specialized source behavior;
fast list data sources now use owned generic backing lists.

They also have generic control variants: `TypedVirtualObjectListView<T>`,
`TypedFastObjectListView<T>`, `TypedTreeListView<T>`, `TypedDataListView<T>`,
`TypedFastDataListView<T>`, and `TypedDataTreeListView<T>`. Each derives from its
generic base and exposes typed `Objects`, `Rows`, selection, checks, column
helpers, and enumerable commands. The generic classes own the implementation;
each non-generic control is a thin `<object>` specialization. Use the same
concrete-subclass pattern for the designer.

Non-generic controls inherit the generic API directly: `Objects` is
`IEnumerable<object>`, and `SelectedObjects` / `CheckedObjects` are
`IList<object>`. The old non-generic collection property shims are removed.
For sequence inputs, prefer `SetObjects`, `SetSelectedObjects`, and
`SetCheckedObjects`; these still accept non-generic enumerables such as
`ArrayList`. The shared untyped integration API keeps its distinct names:
`ModelObjects`, `SelectedModelObjects`, and `CheckedModelObjects`.

Tree controls likewise inherit object-typed `Roots`, `ExpandedObjects`, and
child callbacks. Convert an untyped sequence with `Cast<object>()` when
assigning these properties or returning children.

The generic inheritance chain is:

```text
TypedObjectListView<T>
  TypedVirtualObjectListView<T>
    TypedFastObjectListView<T>
      TypedFastDataListView<T>
    TypedTreeListView<T>
      TypedDataTreeListView<T>
  TypedDataListView<T>
```

Non-generic specialized controls are no longer subclasses of `ObjectListView`,
`VirtualObjectListView`, or `TreeListView`. Use `ObjectListViewBase` for shared
UI integrations, or the appropriate generic base (including `<object>` for
non-generic controls). Custom virtual sources now accept `ObjectListViewBase`.
`IVirtualListView` exposes virtual item creation for printing; `ITreeListView`
provides model-independent tree access for generators, designers, and drag/drop.
Fast sources use `FastObjectListDataSource<T>` with owned `List<T>` storage,
and data-bound trees use `TreeDataSourceAdapter<T>`. Custom tree factories and
models now accept `TypedTreeListView<T>` (use `<object>` for legacy controls).

```csharp
var fast = new TypedFastObjectListView<Person>();
fast.Columns.Add(new OLVColumn("Name", "Name"));
fast.GetTypedColumn(0).AspectGetter = person => person.Name;
fast.Objects = people;
Person selected = fast.SelectedObject;
```

Typed tree controls additionally expose typed `Roots`, `ExpandedObjects`,
`CanExpandGetter`, `ChildrenGetter`, `ParentGetter`, `GetChildren`, and `GetParent`.
Data-bound controls keep their existing
`DataSource` / `DataMember` contracts: `T` is the row type exposed by the binding
source (for example, `DataRowView` when binding a `DataTable`). Virtual controls
still require a virtual data source returning models of type `T`. Shared
object-based callbacks and direct data-source interfaces are unchanged.

### Subclass extension points

- Update renderer, overlay, and drag/drop signatures to `ObjectListViewBase`.
  Shared untyped access includes `ModelObjects`, `GetModelAt`, and the
  `SelectedModelObject(s)` / `CheckedModelObject(s)` bridges.
- Generic `GetModelAt` and selection/check bridges are sealed. Existing
  `ObjectListView` subclasses should keep overriding `GetModelObject`,
  `SelectedObject(s)`, and `CheckedObject(s)` using their inherited generic
  signatures (`object` and `IList<object>` on non-generic controls).
- Replace public `Objects` overrides with `GetModelObjectsCore` and
  `SetModelObjectsCore`, and `Roots` overrides with `GetRootsCore` and
  `SetRootsCore`. Public collection snapshots are not virtual source hooks.
- Typed enumerable commands are non-virtual entry points that dispatch through
  the existing untyped virtual pipeline. Update those overrides to `IEnumerable`.
- Custom storage must consistently implement its read and mutation hooks:
  `MaterializeModelObjects`, `SetStoredModelObjects`, `InsertStoredModelObjects`,
  `RemoveStoredModelObject`, and `ReplaceStoredModelObject`. Transferred batches
  are private snapshots; do not retain or mutate them outside storage.

Model lookup still uses `object.Equals`. Implementing only `IEquatable<T>` does
not change identity semantics.

## Changed contracts

Arrays and lists remain valid inputs. LINQ queries and iterators no longer need
caller-side `ToList()` or `ToArray()`.

| API | V3 contract |
| --- | --- |
| `AddObjects`, `InsertObjects`, `RemoveObjects` | `IEnumerable` instead of `ICollection` |
| `RefreshObjects`, `SelectObjects`, `CopyObjectsToClipboard` | `IEnumerable` instead of `IList` |
| `SetSelectedObjects`, `SetCheckedObjects` | New enumerable methods that replace the current set |
| `SelectedObjects`, `CheckedObjects` | `IList<T>` snapshots, including `IList<object>` on non-generic controls |
| Typed `Objects` | `IEnumerable<T>` with a snapshot getter, instead of `IList<T>` |
| Typed selection/check properties and commands | `IList<T>` snapshots and corresponding `IEnumerable<T>` methods |
| `IVirtualListDataSource` add/insert/remove methods | `IEnumerable` inputs |
| `OLVDataObject` constructor | `IEnumerable` input, copied rather than retained |

Regular controls copy assigned sequences. Later source-collection changes do not
update the control; use mutation methods or assign the sequence again. Untyped
mutations on typed controls reject incompatible model types before changing rows.

### Drawing-resource lifetime

`TextAdornment` (including text overlays/decorations), `BorderDecoration`,
`SimpleDropSink`, `TextSprite`, `Animation`, and `AnimationAdapter` implement
`IDisposable`. Dispose caller-created instances when no longer needed.
They release their generated drawing resources or timers, not caller-assigned
fonts, formats, pens, brushes, or animation components. Gradient borders use
a temporary brush without replacing or disposing `FillBrush`.

ObjectListView disposes its generated standard text overlays and drop sinks
created by `IsSimpleDropSink`. Explicitly assigned overlays and drop sinks remain
caller-owned; detached drop sinks stop scrolling and can be reused.

## Queries and snapshot getters

```csharp
var view = new TypedObjectListView<Person>();
view.Columns.Add(new OLVColumn("Name", "Name"));
view.Objects = people.Where(person => person.IsActive);
view.SetSelectedObjects(view.Objects.Where(person => person.Name.StartsWith("A")));
view.RemoveObjects(view.Objects.Where(person => person.CulinaryRating < 10));
```

Import `System.Linq` and `BrightIdeasSoftware` for this example. On non-generic
controls, use `Cast<Person>()` to query object-typed snapshots.

`Objects`, `SelectedObjects`, and `CheckedObjects` getters return independent
collections, not live views. Mutating them does not update the control, though
their elements still refer to the original model objects. Tree `Objects`
snapshots contain roots, not flattened rows. An unset/null source returns null;
an explicitly empty source returns an empty snapshot.

Use `GetObjectsSnapshot()` for an explicit copy or `Rows` for read-only
display-order access without a full snapshot. `Rows` follows sorting, filtering,
and tree expansion; changes invalidate active row enumerators. `ModelObjects`
is shared source access, not a snapshot or display-order guarantee.

Selection/check property assignments still work. Prefer `SetSelectedObjects`
and `SetCheckedObjects` for sequence inputs; `CheckedObjectsEnumerable` remains
a compatibility alias.

## Enumeration, events, and threading

Changed mutation APIs consume input once, before modifying models or clearing
selection/check state. Queries over the control's own snapshots are safe inputs.
Enumeration failures propagate without partially applying the enumerated batch.
This is **not a general transaction guarantee** for event handlers, callbacks,
renderers, or custom data sources.

Adding/removing event payloads remain `ICollection`. Handlers can modify, replace,
or cancel them; accepted payloads are copied before mutation. Notification-method
overrides should call their base implementation.

Add, insert, remove, and refresh marshal to an existing handle's thread before
enumeration. Other selection/check and data-transfer operations require the UI
thread; clipboard operations require STA. Sources themselves are not made
thread-safe, and direct virtual-data-source calls do not marshal.

## Nulls and control-specific behavior

| Input or control | Behavior |
| --- | --- |
| Null/empty selection or checked set | Clears the set |
| Null add/remove/insert input | No additions or removals |
| Null refresh or clipboard input | Requires non-null input; existing no-source/empty-tree early exits remain |
| Empty clipboard input | Leaves the clipboard unchanged |
| `OLVDataObject` | Requires a non-null control and sequence |
| Checkboxes disabled | Setting checks is a no-op |
| Data-bound add/insert/remove | No-op; modify the bound source instead |
| Tree add/remove | Changes roots |
| Tree insertion | Still throws `NotImplementedException` |

Virtual check-state optimizations and hierarchical tree checks are retained.
Duplicate/null model handling, ordering, sorting, and filtering are otherwise
unchanged; enumerable inputs do not enable unsupported operations.

## Overrides and virtual data sources

Update override and interface parameter types together:

```csharp
public override void AddObjects(System.Collections.IEnumerable models) {
    base.AddObjects(models);
}
```

`CheckedObjects` overrides use `IList<T>` (`IList<object>` on non-generic
controls). To customize sequence-based checking, override
`SetCheckedObjects(IEnumerable)` and route the property setter through it.
The method does not automatically invoke a property override; typed property
assignments and the `CheckedObjectsEnumerable` alias retain that behavior.

For custom `IVirtualListDataSource` implementations, materialize inputs before
mutation if they refer to the source or require multiple passes:

```csharp
var snapshot = ObjectListView.EnumerableToArray(models, alwaysCreate: true);
foreach (object model in snapshot)
    storedModels.Remove(model);
```

`alwaysCreate: true` guarantees an owned copy rather than borrowing a caller's list.
