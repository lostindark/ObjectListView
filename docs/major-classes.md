<a name="major-classes-reference"></a>

# Major Classes Reference

[Documentation index](README.md)

The core controls are in the `BrightIdeasSoftware` namespace. Follow a source link for the full API documentation and implementation.

| Class | Purpose | Source |
| --- | --- | --- |
| `ObjectListView` | Object specialization inheriting the generic control API directly. | [ObjectListView.cs](../ObjectListView/ObjectListView.cs) |
| `OLVColumn` | Configures how a column extracts, formats, edits, and renders model values. | [OLVColumn.cs](../ObjectListView/OLVColumn.cs) |
| `VirtualObjectListView` | Object specialization of the generic virtual control. | [NonGenericListViews.cs](../ObjectListView/NonGenericListViews.cs) |
| `FastObjectListView` | Object specialization of the fast generic control. | [NonGenericListViews.cs](../ObjectListView/NonGenericListViews.cs) |
| `TreeListView` | Object specialization of the generic tree control. | [NonGenericListViews.cs](../ObjectListView/NonGenericListViews.cs) |
| `DataListView` | Object specialization of the data-bound generic control. | [NonGenericListViews.cs](../ObjectListView/NonGenericListViews.cs) |
| `FastDataListView` | Object specialization of the data-bound fast generic control. | [NonGenericListViews.cs](../ObjectListView/NonGenericListViews.cs) |
| `DataTreeListView` | Object specialization of the data-bound generic tree control. | [NonGenericListViews.cs](../ObjectListView/NonGenericListViews.cs) |
| `ObjectListViewBase` | Shared non-generic UI implementation and integration API. | [ObjectListViewBase.cs](../ObjectListView/ObjectListViewBase.cs) |
| `TypedObjectListView<T>` | A control backed by `List<T>`, with typed model access. | [TypedObjectListView.cs](../ObjectListView/Utilities/TypedObjectListView.cs) |
| `TypedObjectListViewAdapter<T>` | Typed access and callbacks for an existing regular, virtual, or tree control. | [TypedObjectListViewAdapter.cs](../ObjectListView/Utilities/TypedObjectListViewAdapter.cs) |
| `TypedVirtualObjectListView<T>`, `TypedFastObjectListView<T>`, `TypedTreeListView<T>` | Generic implementations of virtual, fast, and tree controls. | [VirtualObjectListView.cs](../ObjectListView/VirtualObjectListView.cs), [FastObjectListView.cs](../ObjectListView/FastObjectListView.cs), [TreeListView.cs](../ObjectListView/TreeListView.cs) |
| `TypedDataListView<T>`, `TypedFastDataListView<T>`, `TypedDataTreeListView<T>` | Generic implementations of the data-bound controls. | [DataListView.cs](../ObjectListView/DataListView.cs), [FastDataListView.cs](../ObjectListView/FastDataListView.cs), [DataTreeListView.cs](../ObjectListView/DataTreeListView.cs) |
| `ListViewPrinter` | Prints list contents and provides report formatting and preview. | [ListViewPrinter.cs](../ListViewPrinter/ListViewPrinter.cs) |

See [Getting started](getting-started.md) for configuring controls, the [cookbook](recipes.md) for common tasks, and the [class diagrams](faq.md#don-t-you-have-some-class-diagrams-for-all-this-stuff) for an overview of their relationships.
