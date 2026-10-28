using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BrightIdeasSoftware;
using Microsoft.DotNet.DesignTools.Designers;
using Microsoft.DotNet.DesignTools.Designers.Actions;
using Microsoft.DotNet.DesignTools.TypeRouting;

namespace BrightIdeasSoftware.Design
{
    public sealed class ObjectListViewDesigner : ControlDesigner
    {
        private ObjectListViewBase ListView { get { return (ObjectListViewBase)this.Control; } }

        public override void Initialize(IComponent component) {
            base.Initialize(component);
            this.ListView.ColumnWidthChanging += this.HandleColumnWidthChanging;
            this.ListView.ColumnWidthChanged += this.HandleColumnWidthChanged;
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                this.ListView.ColumnWidthChanging -= this.HandleColumnWidthChanging;
                this.ListView.ColumnWidthChanged -= this.HandleColumnWidthChanged;
            }
            base.Dispose(disposing);
        }

        protected override bool GetHitTest(Point point) {
            Point client = this.ListView.PointToClient(point);
            for (int i = 0; i < this.ListView.Columns.Count; i++) {
                Rectangle bounds = this.ListView.HeaderControl.GetItemRect(i);
                if (client.Y >= bounds.Top && client.Y < bounds.Bottom && Math.Abs(client.X - bounds.Right) <= 3)
                    return true;
            }
            return false;
        }

        public override System.Collections.Generic.IReadOnlyCollection<IComponent> AssociatedComponents {
            get { return base.AssociatedComponents.Concat(this.ListView.AllColumns).Distinct().ToArray(); }
        }

        public override DesignerActionListCollection ActionLists {
            get { return new DesignerActionListCollection { new Options(this.ListView) }; }
        }

        public sealed class Options : DesignerActionList
        {
            internal ObjectListViewBase ListView { get; }
            public Options(ObjectListViewBase list) : base(list) { this.ListView = list; }

            public View View {
                get { return this.ListView.View; }
                set { TypeDescriptor.GetProperties(this.ListView)["View"].SetValue(this.ListView, value); }
            }

            public ImageList SmallImageList {
                get { return this.ListView.SmallImageList; }
                set { TypeDescriptor.GetProperties(this.ListView)["SmallImageList"].SetValue(this.ListView, value); }
            }

            public ImageList LargeImageList {
                get { return this.ListView.LargeImageList; }
                set { TypeDescriptor.GetProperties(this.ListView)["LargeImageList"].SetValue(this.ListView, value); }
            }

            [Editor("OLVColumnCollectionEditor", "System.Drawing.Design.UITypeEditor")]
            public ListView.ColumnHeaderCollection Columns {
                get { return this.ListView.Columns; }
            }

            public override DesignerActionItemCollection GetSortedActionItems() {
                return new DesignerActionItemCollection {
                    new DesignerActionPropertyItem(nameof(Columns), "Edit columns"),
                    new DesignerActionPropertyItem(nameof(View), "View"),
                    new DesignerActionPropertyItem(nameof(SmallImageList), "Small image list"),
                    new DesignerActionPropertyItem(nameof(LargeImageList), "Large image list")
                };
            }
        }

        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties);
            string[] hidden = {
                "BackgroundImage", "BackgroundImageTiled", "HotTracking", "HoverSelection",
                "LabelEdit", "VirtualListSize", "VirtualMode"
            };
            foreach (string name in properties.Keys.Cast<string>().ToArray()) {
                bool treeProperty = this.ListView is TreeListView && new[] {
                    "GroupImageList", "GroupWithItemCountFormat", "GroupWithItemCountSingularFormat",
                    "HasCollapsibleGroups", "SpaceBetweenGroups", "ShowGroups",
                    "SortGroupItemsByPrimaryColumn", "ShowItemCountOnGroups"
                }.Contains(name);
                if (hidden.Contains(name) || name.StartsWith("ToolTip", StringComparison.Ordinal) || treeProperty)
                    properties[name] = TypeDescriptor.CreateProperty(typeof(ObjectListViewBase),
                        (PropertyDescriptor)properties[name], BrowsableAttribute.No);
            }
        }

        protected override void PreFilterEvents(IDictionary events) {
            base.PreFilterEvents(events);
            string[] hidden = {
                "AfterLabelEdit", "BeforeLabelEdit", "DrawColumnHeader", "DrawItem", "DrawSubItem",
                "RetrieveVirtualItem", "SearchForVirtualItem", "VirtualItemsSelectionRangeChanged"
            };
            string[] treeEvents = {
                "AboutToCreateGroups", "AfterCreatingGroups", "BeforeCreatingGroups",
                "GroupTaskClicked", "GroupExpandingCollapsing", "GroupStateChanged"
            };
            foreach (string name in events.Keys.Cast<string>().ToArray()) {
                if (hidden.Contains(name) || (this.ListView is TreeListView && treeEvents.Contains(name)))
                    events[name] = TypeDescriptor.CreateEvent(typeof(ObjectListViewBase),
                        (EventDescriptor)events[name], BrowsableAttribute.No);
            }
        }

        private void HandleColumnWidthChanged(object sender, ColumnWidthChangedEventArgs args) {
            var service = (IComponentChangeService)this.GetService(typeof(IComponentChangeService));
            OLVColumn column = this.ListView.GetColumn(args.ColumnIndex);
            service?.OnComponentChanged(column, TypeDescriptor.GetProperties(column)["Width"], null, column.Width);
        }

        private void HandleColumnWidthChanging(object sender, ColumnWidthChangingEventArgs args) {
            var service = (IComponentChangeService)this.GetService(typeof(IComponentChangeService));
            OLVColumn column = this.ListView.GetColumn(args.ColumnIndex);
            service?.OnComponentChanging(column, TypeDescriptor.GetProperties(column)["Width"]);
        }
    }

    [ExportTypeRoutingDefinitionProvider]
    internal sealed class TypeRoutingProvider : TypeRoutingDefinitionProvider
    {
        public override System.Collections.Generic.IEnumerable<TypeRoutingDefinition> GetDefinitions() {
            yield return new TypeRoutingDefinition(TypeRoutingKinds.Designer,
                "ObjectListViewDesigner", typeof(ObjectListViewDesigner));
        }
    }
}
