<a name="major-classes-reference"></a>

# Major Classes Reference

[Documentation index](README.md)

The core controls are in the `BrightIdeasSoftware` namespace. Follow a source link for the full API documentation and implementation.

| Class | Purpose | Source |
| --- | --- | --- |
| `ObjectListView` | Non-generic control using `TypedObjectListView<object>`, with legacy model properties. | [ObjectListView.Compatibility.cs](../ObjectListView/ObjectListView.Compatibility.cs) |
| `OLVColumn` | Configures how a column extracts, formats, edits, and renders model values. | [OLVColumn.cs](../ObjectListView/OLVColumn.cs) |
| `VirtualObjectListView` | Provides a virtual-mode list backed by a virtual data source. | [VirtualObjectListView.cs](../ObjectListView/VirtualObjectListView.cs) |
| `FastObjectListView` | Provides a model-backed virtual list for large collections. | [FastObjectListView.cs](../ObjectListView/FastObjectListView.cs) |
| `TreeListView` | Displays expandable parent/child model relationships in a multi-column list. | [TreeListView.cs](../ObjectListView/TreeListView.cs) |
| `DataListView` | Binds an ObjectListView to a data source. | [DataListView.cs](../ObjectListView/DataListView.cs) |
| `FastDataListView` | Combines data binding with a virtual list. | [FastDataListView.cs](../ObjectListView/FastDataListView.cs) |
| `DataTreeListView` | Builds a data-bound tree from parent/child relationships. | [DataTreeListView.cs](../ObjectListView/DataTreeListView.cs) |
| `ObjectListViewBase` | Shared non-generic UI implementation and integration API. | [ObjectListView.cs](../ObjectListView/ObjectListView.cs) |
| `TypedObjectListView<T>` | A control backed by `List<T>`, with typed model access. | [TypedObjectListView.cs](../ObjectListView/Utilities/TypedObjectListView.cs) |
| `TypedObjectListViewAdapter<T>` | Typed access and callbacks for an existing regular, virtual, or tree control. | [TypedObjectListViewAdapter.cs](../ObjectListView/Utilities/TypedObjectListViewAdapter.cs) |
| `ListViewPrinter` | Prints list contents and provides report formatting and preview. | [ListViewPrinter.cs](../ListViewPrinter/ListViewPrinter.cs) |

See [Getting started](getting-started.md) for configuring controls, the [cookbook](recipes.md) for common tasks, and the [class diagrams](faq.md#don-t-you-have-some-class-diagrams-for-all-this-stuff) for an overview of their relationships.
