namespace BrightIdeasSoftware {
    /// <summary>The non-generic object specialization of the virtual control.</summary>
    public class VirtualObjectListView : TypedVirtualObjectListView<object> { }

    /// <summary>The non-generic object specialization of the fast control.</summary>
    public class FastObjectListView : TypedFastObjectListView<object> {
        /// <inheritdoc/>
        protected override FastObjectListDataSource<object> CreateDataSource() { return new FastObjectListDataSource(this); }
    }

    /// <summary>The non-generic object specialization of the tree control.</summary>
    public class TreeListView : TypedTreeListView<object> { }

    /// <summary>The non-generic object specialization of the data-bound control.</summary>
    public class DataListView : TypedDataListView<object> { }

    /// <summary>The non-generic object specialization of the data-bound fast control.</summary>
    public class FastDataListView : TypedFastDataListView<object> {
        /// <inheritdoc/>
        protected override FastObjectListDataSource<object> CreateDataSource() { return new FastObjectListDataSource(this); }
    }

    /// <summary>The non-generic object specialization of the data-bound tree control.</summary>
    public class DataTreeListView : TypedDataTreeListView<object> { }

    /// <summary>The object specialization of the fast list data source.</summary>
    public class FastObjectListDataSource : FastObjectListDataSource<object> {
        /// <summary>Create a source for an existing list.</summary>
        public FastObjectListDataSource(ObjectListViewBase listView) : base(listView) { }
    }

    /// <summary>The object specialization of the tree binding adapter.</summary>
    public class TreeDataSourceAdapter : TreeDataSourceAdapter<object> {
        /// <summary>Create an adapter for an existing data-bound tree.</summary>
        public TreeDataSourceAdapter(TypedDataTreeListView<object> listView) : base(listView) { }
    }
}
