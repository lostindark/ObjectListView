<a name="major-classes-reference"></a>

# Major Classes Reference

[Documentation index](README.md)

The core controls are in the `BrightIdeasSoftware` namespace. Follow a source link for the full API documentation and implementation.

| Class | Purpose | Source |
| --- | --- | --- |
| `ObjectListView` | Displays model objects with column aspects, sorting, grouping, and cell editing. | [ObjectListView.cs](../ObjectListView/ObjectListView.cs) |
| `OLVColumn` | Configures how a column extracts, formats, edits, and renders model values. | [OLVColumn.cs](../ObjectListView/OLVColumn.cs) |
| `VirtualObjectListView` | Provides a virtual-mode list backed by a virtual data source. | [VirtualObjectListView.cs](../ObjectListView/VirtualObjectListView.cs) |
| `FastObjectListView` | Provides a model-backed virtual list for large collections. | [FastObjectListView.cs](../ObjectListView/FastObjectListView.cs) |
| `TreeListView` | Displays expandable parent/child model relationships in a multi-column list. | [TreeListView.cs](../ObjectListView/TreeListView.cs) |
| `DataListView` | Binds an ObjectListView to a data source. | [DataListView.cs](../ObjectListView/DataListView.cs) |
| `FastDataListView` | Combines data binding with a virtual list. | [FastDataListView.cs](../ObjectListView/FastDataListView.cs) |
| `DataTreeListView` | Builds a data-bound tree from parent/child relationships. | [DataTreeListView.cs](../ObjectListView/DataTreeListView.cs) |
| `TypedObjectListView<T>` | Provides strongly typed access to an ObjectListView. | [TypedObjectListView.cs](../ObjectListView/Utilities/TypedObjectListView.cs) |
| `ListViewPrinter` | Prints list contents and provides report formatting and preview. | [ListViewPrinter.cs](../ListViewPrinter/ListViewPrinter.cs) |

See [Getting started](getting-started.md) for configuring controls, the [cookbook](recipes.md) for common tasks, and the [class diagrams](faq.md#don-t-you-have-some-class-diagrams-for-all-this-stuff) for an overview of their relationships.
