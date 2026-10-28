# Migrating to ObjectListView v3

[Documentation index](README.md) | [Implementation plan](v3-plan.md)

This guide covers the **enumerable collection API, generic-control, and platform migration milestones**, not a completed v3
release. Grouping, styling, renderer/layout changes, and release versioning remain
separate milestones. Runtime targets are .NET Framework 4.6.2 and .NET 10 for Windows.

## Runtime targets, designers, and saved layouts

The six runtime/test projects multi-target `net462;net10.0-windows`.
The .NET 10 runtime assemblies do not depend on System.Design or Designer SDK
assemblies. Existing Framework designer types remain available in the Framework
assembly. Modern designer integration is packaged separately as a preview; it
must pass interactive Visual Studio acceptance checks before full parity is claimed.
The modern designer uses a Framework 4.7.2 client, a .NET 10 server, and a shared
protocol. This does not raise the Framework runtime requirement for applications.
Use the NuGet package for modern designer evaluation; a plain DLL reference does
not include the client/server design assets.

The modern column editor uses the Designer SDK's collection-editor bridge.
Printer brush/pen editors reuse the existing solid/gradient/hatch editing UI in
a modal client-side dialog, editing a copy so Cancel does not mutate the source.
Custom or texture brush editors are not added by this migration.

Applications using ListViewPrinter must reference its matching runtime assembly
from their normal build/deployment location. Do not reference DLLs inside the
NuGet package's `Design\WinForms` directories: Visual Studio deduplicates designer
discovery by package root, so a runtime reference into that directory can prevent
the package's designer assets from being discovered.

The modern printer runtime is not shipped as a server designer plugin. Loading
it once as an application assembly and again as a designer plugin creates distinct
`PenData`/brush type identities, causing apparently identical-type conversion
errors when accepting an editor value. The Framework printer copy used by the
Visual Studio client is a separate, intentional dependency.

Packaged-consumer acceptance now covers standard/concrete typed toolbox creation,
column editing across the eight control families, printer Cancel/Undo/Redo and
supported brush styles, BindingSource/DataSet references and DataMember selection,
image resources, localization, and generated-code rebuilds. Modern DataSource
metadata uses the standard `AttributeProvider(typeof(IListSource))` design binding
mechanism, rather than a Framework-only converter string.

**Known preview limitation:** native header-divider dragging still fails modern
designer acceptance, so its physical-resize Undo/Redo check remains blocked.
Use the Columns editor or Properties to set widths. The smart-tag Columns action
has been exercised; a complete smart-tag View round trip remains unverified.
Visible-runtime checks exercised focus, cell editing, RTL geometry, keyboard
input, and actual OLE drag/drop at 96 and 144 DPI on both targets. These checks do
not claim ARM64 or every Windows/Visual Studio/monitor configuration.

`SaveState()` still returns `byte[]`; `RestoreState(byte[])` still returns `bool`.
Both runtimes now write the same explicit **OLVS schema 1** layout format.
Old BinaryFormatter layouts are deliberately unsupported: discard the stored
bytes and save a new layout. No BinaryFormatter compatibility switch is needed.

All integers are signed little-endian Int32 values. Booleans are single bytes
containing 0 or 1. The record is:

| Offset | Field |
| --- | --- |
| 0 | Four ASCII bytes `OLVS` |
| 4 | Schema version: 1 |
| 8 | Column count |
| 12 | WinForms View value |
| 16 | Primary sort column's AllColumns index, or -1 |
| 20 | WinForms SortOrder value |
| 24 | ShowGroups boolean |
| 25 onward | For each AllColumns entry: width (Int32), remembered display index (Int32), visible (boolean) |

The exact length is `25 + 9 * columnCount`. Columns are matched by AllColumns
position, as before; selection and scroll position are not stored. Hidden/new
columns may have -1 or duplicate remembered display indexes. These are relative
ordering keys, not current native indexes: after column removal a hidden column
may retain a nonnegative key beyond the current column count. Widths retain the
WinForms -1/-2 autosizing sentinels. Unsupported versions, invalid/truncated data,
legacy layouts, and column-count mismatches return false without applying the
layout. Null remains an argument error. Operational errors applying a valid
layout propagate rather than being reported as success.

```csharp
if (!list.RestoreState(savedLayout)) {
    // Inform the user that the saved layout cannot be restored.
    // Keep the current layout and save a replacement when appropriate.
}
```

## DPI-aware hosts and column widths

The host application chooses its DPI awareness. The demos use system awareness
on `net462` and `PerMonitorV2` on .NET 10; ObjectListView does not set process DPI
policy.

Initial column widths and minimum/maximum widths use a 96-DPI design baseline.
At runtime they scale to the native window DPI after component initialization,
including hidden and fixed-width columns. On .NET 10, subsequent DPI changes
rescale current widths rather than restoring defaults, preserving user sizing.
Handle recreation at the same DPI does not apply the scale again.

Remove duplicate manual startup scaling of these initial values. Explicit widths
assigned after initialization and native handle creation, and widths in saved
layouts, remain current device-pixel values; OLVS is not a DPI-independent layout
format. Framework 4.6.2
system-aware windows can still be bitmap-scaled when moved between monitors with
different DPI.

## Generic control hierarchy

`TypedObjectListView<T>` is now a Windows Forms control with owned `List<T>`
storage, rather than a wrapper around another control:

```csharp
ObjectListViewBase
    -> TypedObjectListView<T>
        -> ObjectListView : TypedObjectListView<object>
```

The generic control implements collection assignment, bulk insertion, removal,
and replacement with `List<T>` operations. The shared UI base only enumerates
stored models and invokes storage-operation hooks. Event collections are
snapshots, not the mutable backing-store contract. The shared `ModelObjects`
bridge is read-only source access; it is not a snapshot guarantee.
Accepted `ItemsChanging` payloads are also copied before becoming stored models,
so retaining and later modifying an event payload does not alter the control.

Replace `new TypedObjectListView<Person>(existingControl)` with a new
`TypedObjectListView<Person>()`, configure its columns, and add that control to
the form. Access control properties directly instead of through `.ListView`.
Use `GetTypedColumn(...)` for typed column delegates; `GetColumn(...)` returns
the shared `OLVColumn`. Check-state and tooltip callbacks retain the object-based
control signatures. Use `control.Typed` for typed callback configuration.

To retain the existing control instead, migrate the old wrapper constructor to
`new TypedObjectListViewAdapter<Person>(existingControl)`. The adapter supports
typed source/selection/check snapshots, callbacks, column helpers, and enumerable
commands. It neither creates nor disposes a control. Its `ListView` is read-only:
construct another adapter when adapting a different control.

```csharp
var people = new TypedObjectListViewAdapter<Person>(existingControl);
people.BooleanCheckStateGetter = person => person.IsActive;
people.CellToolTipGetter = (column, person) => person.Name;
people.GetColumn(0).AspectGetter = person => person.Name;
```

Typed callback adapters write to the existing callback slots. Reading a callback
through an adapter reflects later changes made through the untyped API too.
`GetTypedColumn` and adapter `GetColumn` share weakly cached column adapters;
removed columns are not retained by the cache.

For designer use, declare a concrete, non-generic subclass such as
`public class PersonListView : TypedObjectListView<Person> { }`.
For toolbox insertion, place that concrete subclass in a named namespace.
Typed controls are not subclasses of `ObjectListView`. Custom renderers,
overlays, drag/drop implementations, and other integrations that previously
accepted `ObjectListView` now accept `ObjectListViewBase`; update overrides and
interface implementations accordingly. The shared base exposes `ModelObjects`,
`SelectedModelObjects`, `CheckedModelObjects`, `SelectedModelObject`,
`CheckedModelObject`, and `GetModelAt` for untyped access.

Existing subclasses of `ObjectListView` retain their legacy virtual model
properties and `GetModelObject` extension point. Fast, virtual, tree, and
data-bound controls remain non-generic and keep their specialized data sources.
They do not materialize those data sources into the generic backing list.

### Subclass extension points

`GetModelAt` and the shared selection/check-state bridges are sealed on the
generic control. Override `GetModelObject`, `SelectedObject`, `CheckedObject`,
`SelectedObjects`, or `CheckedObjects` instead. Those overrides are honored by
shared UI operations, `Rows`, and adapters. The compatibility class routes typed
selection/check operations through legacy `IList` property overrides, including
when it is referenced as `TypedObjectListView<object>`.

For source customization, override `GetModelObjectsCore` and
`SetModelObjectsCore`. Move source-producing or source-assigning logic out of
legacy `Objects` overrides into those hooks. **The UI no longer calls the public
`Objects` snapshot getter to obtain its source.** This is an intentional migration
for custom source subclasses; otherwise every internal query would allocate a
full snapshot. Existing built-in control sources have been migrated.

The typed `Objects` accessor and typed enumerable command overloads are
non-virtual entry points. The commands dispatch through the existing untyped
virtual command pipeline, so legacy command overrides still run. Storage
extensions use `MaterializeModelObjects`, `SetStoredModelObjects`,
`InsertStoredModelObjects`, `RemoveStoredModelObject`, and
`ReplaceStoredModelObject`. Adopt/insert hooks receive private, validated
snapshots, not caller/event-owned collections. Do not retain or mutate a
transferred snapshot outside the storage implementation. Custom storage must
implement both its read and mutation hooks consistently.

Storage lookup retains `object.Equals` semantics to match the existing UI
selection, checking, and model maps. Implementing only `IEquatable<T>` does not
change the control's identity policy.

## Changed contracts

These signature changes require recompiling consumers and updating overrides.
Existing arrays and lists still work as inputs; iterator methods and LINQ queries
no longer need a caller-side `ToList()` or `ToArray()`.

| API | Previous contract | New contract |
| --- | --- | --- |
| `SelectedObjects`, `CheckedObjects` | `IList` | Unchanged: `IList` properties with materialized snapshot getters |
| `AddObjects`, `InsertObjects`, `RemoveObjects` | `ICollection` input | `IEnumerable` input |
| `RefreshObjects`, `SelectObjects`, `CopyObjectsToClipboard` | `IList` input | `IEnumerable` input |
| `SetSelectedObjects` | No method | `IEnumerable` input; replaces the selection |
| `SetCheckedObjects` | No method | `IEnumerable` input; replaces the checked-object set |
| `TypedObjectListView<T>.SelectedObjects`, `.CheckedObjects` | `IList<T>` | Unchanged: `IList<T>` properties with materialized snapshot getters |
| `TypedObjectListView<T>.Objects` | `IList<T>` | `IEnumerable<T>`, with a materialized snapshot getter |
| Typed add, insert, refresh, remove, select, check, and clipboard methods | No typed wrappers | Corresponding `IEnumerable<T>` methods |
| `IVirtualListDataSource` add, insert, and remove methods | `ICollection` input | `IEnumerable` input |
| `OLVDataObject` constructor | `IList` input, retained by reference | `IEnumerable` input, copied into a snapshot |

The non-generic `Objects` and `SetObjects` APIs still accept `IEnumerable`.
Regular and typed controls preserve the legacy null-source distinction:
`Objects` is null until a source is assigned or after assigning null, while an
explicitly empty source returns an empty snapshot. Typed adapters preserve the
same distinction. `GetObjectsSnapshot()` also returns null for an unset/null
source and an independent list otherwise.
`Objects` getters on regular, fast, virtual, and tree controls now return
independent snapshots. Mutating the returned collection no longer mutates an
internal backing list. Tree snapshots contain roots, not flattened rows.
Regular controls now copy assigned sequences into their owned `List<T>` before
events or rebuilding rows, so single-use iterators also work when model
notification subscriptions are enabled. The source collection is no longer
borrowed. Later changes to it do not update the control; use the control's
mutation methods or assign the sequence again. Untyped mutations on a typed
control reject incompatible model types before modifying rows.

## Queries and snapshot getters

```csharp
using System.Linq;
using BrightIdeasSoftware;

var peopleView = new TypedObjectListView<Person>();
peopleView.Columns.Add(new OLVColumn("Name", "Name"));
peopleView.Objects = people.Where(person => person.IsActive == true);
peopleView.SetSelectedObjects(people.Where(person => person.Name.StartsWith("A")));
peopleView.SetCheckedObjects(peopleView.SelectedObjects.Where(person => person.CanTellJokes));
peopleView.AddObjects(newPeople.Where(person => person.IsActive == true));
peopleView.RemoveObjects(peopleView.Objects.Where(person => person.CulinaryRating < 100));
```

The heterogeneous control accepts non-generic `System.Collections.IEnumerable`
in bulk methods. Its selection and check properties retain `IList`, including
`.Count` and indexed access. Use `Cast<T>()` to query those snapshots with LINQ:

```csharp
var selected = listView.SelectedObjects;
int checkedCount = listView.CheckedObjects.Count;
listView.CopyObjectsToClipboard(selected.Cast<Person>().Where(person => person.CanTellJokes));
```

Selection and checked-object getters are snapshots, not live queries. Changing
the control after reading a getter does not change that snapshot; changing the
returned collection does not update the control. Snapshot contents still refer
to the original model objects.

Use `GetObjectsSnapshot()` when the copy should be explicit. For display-order
access without a full copy, use `Rows`, an `IReadOnlyList<T>` available on both
typed controls and adapters:

```csharp
IReadOnlyList<Person> rows = peopleView.Rows;
int visibleCount = rows.Count;
Person firstVisible = visibleCount == 0 ? null : rows[0];
```

The cached row view follows source replacement, filtering, sorting, and tree
expansion. It has no collection mutation API. Count and indexed access do not
enumerate a virtual source; an explicit source snapshot still can. Rebuilding,
sorting, and row changes invalidate active row enumerators. Use these APIs on
the UI thread and use a snapshot if a stable collection is needed across changes.

Read-only `ModelObjects` access is reserved for the shared integration boundary.
Its source ordering is distinct from `Rows` display ordering, and callers must
not assume it remains attached to the same collection after source replacement.

Existing collection assignments to `SelectedObjects` and `CheckedObjects` still
work. For sequence inputs, use `SetSelectedObjects(IEnumerable)` and
`SetCheckedObjects(IEnumerable)` rather than changing the property types.
Both methods replace the current set; they do not add to it. The typed control
provides equivalent `IEnumerable<T>` methods and keeps `IList<T>` selection and
check snapshots.

`SelectObjects(IEnumerable)` remains available for compatibility.
`SetSelectedObjects` delegates to it so existing selection overrides still run;
the selection property setter uses `SetSelectedObjects`.

`CheckedObjectsEnumerable` remains as a compatibility alias in this milestone.
Its setter materializes the input and assigns the virtual `CheckedObjects`
property, preserving existing property overrides. Typed selection and check
property setters adapt generic-only lists when necessary and preserve null
assignments. Shared UI operations on non-generic controls continue to honor
their legacy virtual property overrides.
Prefer `SetCheckedObjects` for new sequence-based calls.

## Enumeration, events, and threading

When processing an input, changed mutation APIs consume the caller-provided
sequence once before making changes. Sequences used for selection are materialized
before clearing the selection, and sequences used for checks are consumed before changing check
states. Queries over the control's own contents can therefore be used for
removal, selection, and checking.

Enumeration errors propagate to the caller. A sequence that yields one object
and then throws does not partially add, remove, refresh, select, or check that
object. This is not a general transaction guarantee: exceptions from application
event handlers, model callbacks, renderers, or custom data sources still follow
their existing behavior.

`ItemsAddingEventArgs.ObjectsToAdd` and `ItemsRemovingEventArgs.ObjectsToRemove`
remain `ICollection`. They receive materialized input that handlers can inspect,
modify, replace, or cancel. Accepted event payloads are copied before mutation
so an event handler can also replace a payload with a collection owned by the
control. Notification subscriptions use the accepted snapshot.
The typed storage layer adopts or inserts the private accepted batch directly,
without making a third copy. Event payload isolation is retained even when an
event handler saves the arguments for later use. Overrides of notification
methods should call their base implementation to preserve row-enumerator
invalidation and normal event delivery.

Add, insert, remove, and refresh operations marshal to an existing control
handle's thread before enumeration. This does not make the source collection
itself thread-safe. Selection and check properties, `SetSelectedObjects`, `SelectObjects`,
`SetCheckedObjects`, and data transfer APIs still require the
appropriate UI thread; clipboard operations require STA. Direct
virtual-data-source calls do not marshal automatically.

## Nulls and control-specific behavior

- Assigning `null` to `SelectedObjects` clears selection; assigning `null` to
  `CheckedObjects` unchecks all. Passing null or an empty sequence to
  `SetSelectedObjects` or `SetCheckedObjects` has the same effect.
- Null add/remove inputs make no collection changes. Null insert inputs are a
  no-op on regular lists and empty input for virtual data sources.
- `RefreshObjects` and `CopyObjectsToClipboard` require non-null sequences and
  report `ArgumentNullException` when they process a null input. Existing
  no-data-source and empty-tree early exits remain unchanged.
- Empty clipboard input leaves the clipboard unchanged.
- `OLVDataObject` requires a non-null control and input sequence.
- Setting checks with checkboxes disabled remains a no-op. Virtual lists retain
  their optimized check-state tracking and custom-getter path; trees still
  include checked descendants in collapsed branches and recalculate hierarchical
  parent checks.
- Data-bound `DataListView` add/insert/remove methods remain no-ops, without
  enumerating the input. Modify the bound data source instead.
- Tree add/remove operations still affect roots. Tree insertion is still
  unsupported and throws `NotImplementedException`.

Ordering, sorting, filtering, and each control's existing treatment of duplicate
models and null model elements are unchanged. Accepting enumerable inputs does
not enable previously unsupported operations.

## Overrides and virtual data sources

Update override and interface implementation parameter types together:

```csharp
using System.Collections;

public override void AddObjects(IEnumerable modelObjects)
{
    base.AddObjects(modelObjects);
}
```

The `CheckedObjects` override signature stays `IList`. Custom controls that need
to customize sequence-based checking should override `SetCheckedObjects(IEnumerable)`
and route their property setter through it. The built-in virtual and tree controls
do this so property assignments, method calls, and the compatibility alias share
the same check-state behavior. New sequence methods do not invoke existing
property overrides automatically; existing typed property assignments and the
compatibility alias continue to honor them.

For a custom `IVirtualListDataSource`, update `AddObjects`, `InsertObjects`, and
`RemoveObjects` to accept `IEnumerable`. Materialize before changing the source
when the input might refer to it or when multiple passes are needed:

```csharp
public override void RemoveObjects(IEnumerable modelObjects)
{
    var snapshot = ObjectListView.EnumerableToArray(modelObjects, true);
    foreach (object model in snapshot)
        models.Remove(model);
}
```

Use `alwaysCreate: true` to obtain an owned copy, rather than an adapter over a
caller's mutable list. No new behavior is required of the abstract data source's
intentionally empty implementations.
