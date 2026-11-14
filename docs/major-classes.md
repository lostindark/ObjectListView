<a name="major-classes-reference"></a>

# Major Classes Reference

[Documentation index](README.md)

The core controls are in the `BrightIdeasSoftware` namespace. Follow a source link for the full API documentation and implementation.

Core controls, typed adapters, and control-owned partials live at the project
root. Shared data sources, row collections, and event payloads are under
`Implementation`; drawing and styles are under `Rendering`; palette and theme
services are under `Theming`. Auxiliary controls and forms, including
`ColumnSelectionForm`, are under `SubControls`. Folder placement does not change
the public namespaces or nested type names.

| Class | Purpose | Source |
| --- | --- | --- |
| `ObjectListView` | Object specialization inheriting the generic control API directly. | [NonGenericListViews.cs](../ObjectListView/NonGenericListViews.cs) |
| `OLVColumn` | Configures how a column extracts, formats, edits, and renders model values. | [OLVColumn.cs](../ObjectListView/OLVColumn.cs) |
| `TypedColumn<T>` | Type-safe access to an existing column's callbacks and aspect getters. | [TypedColumn.cs](../ObjectListView/TypedColumn.cs) |
| `VirtualObjectListView` | Object specialization of the generic virtual control. | [NonGenericListViews.cs](../ObjectListView/NonGenericListViews.cs) |
| `FastObjectListView` | Object specialization of the fast generic control. | [NonGenericListViews.cs](../ObjectListView/NonGenericListViews.cs) |
| `TreeListView` | Object specialization of the generic tree control. | [NonGenericListViews.cs](../ObjectListView/NonGenericListViews.cs) |
| `DataListView` | Object specialization of the data-bound generic control. | [NonGenericListViews.cs](../ObjectListView/NonGenericListViews.cs) |
| `FastDataListView` | Object specialization of the data-bound fast generic control. | [NonGenericListViews.cs](../ObjectListView/NonGenericListViews.cs) |
| `DataTreeListView` | Object specialization of the data-bound generic tree control. | [NonGenericListViews.cs](../ObjectListView/NonGenericListViews.cs) |
| `ObjectListViewBase` | Shared non-generic UI implementation and integration API. | [ObjectListViewBase.cs](../ObjectListView/ObjectListViewBase.cs) |
| `TypedObjectListView<T>` | A control backed by `List<T>`, with typed model access. | [TypedObjectListView.cs](../ObjectListView/TypedObjectListView.cs) |
| `TypedObjectListViewAdapter<T>` | Typed access and callbacks for an existing regular, virtual, or tree control. | [TypedObjectListViewAdapter.cs](../ObjectListView/TypedObjectListViewAdapter.cs) |
| `TypedVirtualObjectListView<T>`, `TypedFastObjectListView<T>`, `TypedTreeListView<T>` | Generic implementations of virtual, fast, and tree controls. | [TypedVirtualObjectListView.cs](../ObjectListView/TypedVirtualObjectListView.cs), [TypedFastObjectListView.cs](../ObjectListView/TypedFastObjectListView.cs), [TypedTreeListView.cs](../ObjectListView/TypedTreeListView.cs) |
| `TypedTreeListView<T>.TreeRenderer` | Draws the tree column while retaining the control's nested renderer API. | [TypedTreeListView.Renderer.cs](../ObjectListView/TypedTreeListView.Renderer.cs) |
| `TypedDataListView<T>`, `TypedFastDataListView<T>`, `TypedDataTreeListView<T>` | Generic implementations of the data-bound controls. | [TypedDataListView.cs](../ObjectListView/TypedDataListView.cs), [TypedFastDataListView.cs](../ObjectListView/TypedFastDataListView.cs), [TypedDataTreeListView.cs](../ObjectListView/TypedDataTreeListView.cs) |
| `ListViewPrinter` | Prints list contents and provides report formatting and preview. | [ListViewPrinter.cs](../ListViewPrinter/ListViewPrinter.cs) |

See [Getting started](getting-started.md) for configuring controls, the [cookbook](recipes.md) for common tasks, and the [class diagrams](faq.md#don-t-you-have-some-class-diagrams-for-all-this-stuff) for an overview of their relationships.
