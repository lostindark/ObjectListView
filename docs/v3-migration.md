# Migrating to ObjectListView v3

[Documentation index](README.md) | [Implementation plan](v3-plan.md)

This guide covers the **enumerable collection API milestone**, not a completed v3
release. Grouping, styling, renderer/layout changes, and release versioning remain
separate milestones. The target remains .NET Framework 4.6.2.

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

`Objects` and `SetObjects` on the base control already accept `IEnumerable` and
their contracts are unchanged. The typed `Objects` setter now copies its input
before calling `SetObjects`, so single-use iterators also work when model
notification subscriptions are enabled. It no longer borrows the caller's list.

## Queries and snapshot getters

```csharp
using System.Linq;
using BrightIdeasSoftware;

var peopleView = new TypedObjectListView<Person>(listView);
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

Existing collection assignments to `SelectedObjects` and `CheckedObjects` still
work. For sequence inputs, use `SetSelectedObjects(IEnumerable)` and
`SetCheckedObjects(IEnumerable)` rather than changing the property types.
Both methods replace the current set; they do not add to it. The typed wrapper
provides equivalent `IEnumerable<T>` methods and keeps `IList<T>` selection and
check snapshots.

`SelectObjects(IEnumerable)` remains available for compatibility.
`SetSelectedObjects` delegates to it so existing selection overrides still run;
the selection property setter uses `SetSelectedObjects`.

`CheckedObjectsEnumerable` remains as a compatibility alias in this milestone.
Its setter materializes the input and assigns the virtual `CheckedObjects`
property, preserving existing property overrides. The typed selection and check
property setters also assign the wrapped control's virtual properties, adapting
generic-only lists when necessary and preserving null assignments.
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

Add, insert, remove, and refresh operations marshal to an existing control
handle's thread before enumeration. This does not make the source collection
itself thread-safe. Selection and check properties, `SetSelectedObjects`, `SelectObjects`,
`SetCheckedObjects`, typed `Objects`, and data transfer APIs still require the
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
