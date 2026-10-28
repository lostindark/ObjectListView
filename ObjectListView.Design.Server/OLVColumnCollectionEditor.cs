using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.DotNet.DesignTools.Editors;

namespace BrightIdeasSoftware.Design
{
    public sealed class OLVColumnCollectionEditor : CollectionEditor
    {
        private ObjectListViewBase ListView {
            get {
                return this.Context.Instance is ObjectListViewDesigner.Options options ?
                    options.ListView : (ObjectListViewBase)this.Context.Instance;
            }
        }
        public OLVColumnCollectionEditor(IServiceProvider provider, Type collectionType)
            : base(provider, collectionType) { }

        protected override Type CreateCollectionItemType() {
            return typeof(OLVColumn);
        }

        protected override object[] GetItems(object editValue) {
            return this.ListView.AllColumns.Cast<object>().ToArray();
        }

        protected override object SetItems(object editValue, object[] value) {
            var list = this.ListView;
            list.AllColumns.Clear();
            list.AllColumns.AddRange(value.Cast<OLVColumn>());
            list.RebuildColumns();
            return list.Columns;
        }

        protected override string GetDisplayText(object value) {
            var column = (OLVColumn)value;
            return string.IsNullOrEmpty(column.AspectName) ? column.Text :
                string.Format("{0} ({1})", column.Text, column.AspectName);
        }

        [ExportCollectionEditorFactory("ObjectListView.Columns")]
        private sealed class Factory : CollectionEditorFactory<OLVColumnCollectionEditor>
        {
            protected override OLVColumnCollectionEditor CreateCollectionEditor(IServiceProvider provider, Type collectionType) {
                return new OLVColumnCollectionEditor(provider, collectionType);
            }
        }
    }
}
