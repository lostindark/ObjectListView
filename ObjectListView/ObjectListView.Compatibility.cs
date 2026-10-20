using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace BrightIdeasSoftware {
    /// <summary>
    /// Non-generic control backed by the object specialization of the generic list view.
    /// </summary>
    public class ObjectListView : TypedObjectListView<object> {
        /// <summary>
        /// Get or set all model objects, without applying filters.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new virtual IEnumerable Objects {
            get { return CreateModelList(base.ModelObjects); }
            set { base.ModelObjects = value; }
        }

        /// <summary>
        /// Get or set the selected model objects.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new virtual IList SelectedObjects {
            get { return (IList)base.GetSelectedObjectsCore(); }
            set { base.SetSelectedObjectsCore(value == null ? null : value as IList<object> ?? CreateModelList(value)); }
        }

        /// <summary>
        /// Get or set the checked model objects.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new virtual IList CheckedObjects {
            get { return (IList)base.GetCheckedObjectsCore(); }
            set { base.SetCheckedObjectsCore(value == null ? null : value as IList<object> ?? CreateModelList(value)); }
        }

        // Keep legacy state overrides in the typed control's canonical dispatch path.
        /// <inheritdoc/>
        protected override IList<object> GetSelectedObjectsCore() {
            IList selected = this.SelectedObjects;
            return selected as IList<object> ?? CreateModelList(selected);
        }

        /// <inheritdoc/>
        protected override void SetSelectedObjectsCore(IList<object> value) {
            this.SelectedObjects = value == null ? null : value as IList ?? CreateModelList(value);
        }

        /// <inheritdoc/>
        protected override IList<object> GetCheckedObjectsCore() {
            IList checkedObjects = this.CheckedObjects;
            return checkedObjects as IList<object> ?? CreateModelList(checkedObjects);
        }

        /// <inheritdoc/>
        protected override void SetCheckedObjectsCore(IList<object> value) {
            this.CheckedObjects = value == null ? null : value as IList ?? CreateModelList(value);
        }

        // Keep the serialized type name so previously saved layouts can still be restored.
        [Serializable]
        internal class ObjectListViewState {
            public int VersionNumber = 1;
            public int NumberOfColumns = 1;
            public View CurrentView;
            public int SortColumn = -1;
            public bool IsShowingGroups;
            public SortOrder LastSortOrder = SortOrder.None;
            public ArrayList ColumnIsVisible = new ArrayList();
            public ArrayList ColumnDisplayIndicies = new ArrayList();
            public ArrayList ColumnWidths = new ArrayList();
        }
    }
}
