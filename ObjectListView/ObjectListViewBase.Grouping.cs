using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace BrightIdeasSoftware {
    public abstract partial class ObjectListViewBase {
        /// <summary>
        /// Gets or sets the grouping policy. Changes take effect on the next sort or rebuild.
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public GroupingConfiguration Grouping {
            get { return this.grouping; }
            set {
                ArgumentNullException.ThrowIfNull(value, nameof(value));
                this.ValidateGroupingColumn(value.Column);
                this.grouping = value;
                this.legacySortGroupItemsByPrimaryColumn = null;
            }
        }
        private GroupingConfiguration grouping = GroupingConfiguration.FollowSorting();

        /// <summary>
        /// Gets or sets a fixed grouping column.
        /// </summary>
        [Obsolete("Use Grouping = GroupingConfiguration.Fixed(column), or FollowSorting() to unlock grouping."),
         Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual OLVColumn AlwaysGroupByColumn {
            get { return this.grouping.Column; }
            set {
                this.grouping = value == null
                    ? GroupingConfiguration.FollowSorting(this.grouping.GroupOrder)
                    : GroupingConfiguration.Fixed(value, this.grouping.GroupOrder);
            }
        }

        /// <summary>
        /// Gets or sets the group direction. None follows the item-sort direction.
        /// </summary>
        [Obsolete("Use GroupingConfiguration's groupOrder argument. Use null, not None, to follow sorting."),
         Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual SortOrder AlwaysGroupBySortOrder {
            get { return this.grouping.GroupOrder ?? SortOrder.None; }
            set {
                SortOrder? order = value == SortOrder.None ? (SortOrder?)null : value;
                this.grouping = this.grouping.Column == null
                    ? GroupingConfiguration.FollowSorting(order)
                    : GroupingConfiguration.Fixed(this.grouping.Column, order);
            }
        }

        /// <summary>
        /// Gets or sets the legacy column-zero ordering override for grouped items.
        /// </summary>
        [Obsolete("Grouped items now use SortDescriptions. Set this property explicitly only to retain legacy column-zero ordering."),
         Browsable(false),
         Category("ObjectListView"),
         Description("Use legacy column-zero ordering within groups when following one sort field."),
         DefaultValue(true)]
        public virtual bool SortGroupItemsByPrimaryColumn {
            get { return this.legacySortGroupItemsByPrimaryColumn ?? true; }
            set { this.legacySortGroupItemsByPrimaryColumn = value; }
        }
        private bool? legacySortGroupItemsByPrimaryColumn;
        private OLVColumn lastGroupingColumn;
        private SortOrder lastGroupingOrder;
        private NullableDictionary<object, bool> collapsedGroups = new NullableDictionary<object, bool>();
        private SortOperation groupingOperationToDispatch;

        /// <summary>
        /// Clear applied groups before rebuilding their rows, retaining policy and collapsed state.
        /// </summary>
        protected void ClearGrouping() {
            bool hadGroups = this.OLVGroups != null && this.OLVGroups.Count > 0;
            this.RememberCollapsedGroups();
            this.OLVGroups = new List<OLVGroup>();
            if (hadGroups)
            {
                this.CreateGroups(this.OLVGroups);
            }
            this.InvalidateModelRows();
        }

        private void RememberCollapsedGroups() {
            if (this.OLVGroups == null || this.OLVGroups.Count == 0)
            {
                return;
            }
            this.collapsedGroups = new NullableDictionary<object, bool>();
            foreach (OLVGroup group in this.OLVGroups)
            {
                this.collapsedGroups[group.Key] = group.Collapsed;
            }
        }

        /// <summary>
        /// Rebuild groups using the current grouping policy and item sorting.
        /// </summary>
        public virtual void BuildGroups() {
            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)this.BuildGroups);
                return;
            }
            this.DoSort(new SortDescriptionCollection(this.sortDescriptions), true);
            this.PostProcessRows();
        }

        /// <summary>
        /// Request an item sort and rebuild groups using the current grouping policy.
        /// </summary>
        /// <remarks>None retains the legacy Sorting/Ascending default for this overload.</remarks>
        public virtual void BuildGroups(OLVColumn column, SortOrder order) {
            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate { this.BuildGroups(column, order); });
                return;
            }
            if (order == SortOrder.None)
            {
                order = this.Sorting == SortOrder.None ? SortOrder.Ascending : this.Sorting;
            }
            this.DoSort(this.requestedSortDescriptions ?? this.sortDescriptions.WithPrimary(column, order), true);
            this.PostProcessRows();
        }

        /// <summary>
        /// Build groups using an ordered collection of sort fields.
        /// </summary>
        /// <remarks>This overload raises grouping events, but not sorting events.</remarks>
        public virtual void BuildGroups(OLVColumn groupByColumn, SortOrder groupByOrder,
            IEnumerable<OLVSortDescription> sortDescriptions) {
            SortDescriptionCollection descriptions = new SortDescriptionCollection(sortDescriptions);
            SortOperation previous = this.sortOperation;
            SortOperation previousDispatch = this.groupingOperationToDispatch;
            SortOperation operation = new SortOperation(descriptions.GetActiveDescriptions());
            this.sortOperation = operation;
            this.groupingOperationToDispatch = operation;
            try {
                this.BuildGroups(groupByColumn, groupByOrder, descriptions.GetColumn(0), descriptions.GetOrder(0),
                    descriptions.GetColumn(1), descriptions.GetOrder(1));
            } finally {
                this.sortOperation = previous;
                this.groupingOperationToDispatch = previousDispatch;
                if (operation.Canceled && previous != null)
                {
                    previous.Cancel();
                }
            }
        }

        /// <summary>
        /// Build groups using explicit grouping and primary/secondary item sorting.
        /// </summary>
        /// <remarks>This overload raises grouping events, but not sorting events.</remarks>
        public virtual void BuildGroups(OLVColumn groupByColumn, SortOrder groupByOrder,
            OLVColumn column, SortOrder order, OLVColumn secondaryColumn, SortOrder secondaryOrder) {
            if (groupByColumn == null)
            {
                return;
            }
            SortOperation previous = this.sortOperation;
            // Consume the enumerable overload's handoff before any callbacks can reenter.
            SortOperation operation = this.groupingOperationToDispatch ??
                new SortOperation(new SortDescriptionCollection(column, order, secondaryColumn, secondaryOrder));
            this.groupingOperationToDispatch = null;
            this.sortOperation = operation;
            try {
                this.ValidateGroupingColumn(groupByColumn);
                if (!this.CanShowGroups)
                {
                    throw new NotSupportedException("This control does not support grouping without a grouping strategy.");
                }

                // Flush WinForms' pending item collection before enumerating it.
                _ = this.Items.Count;
                GroupingParameters parameters = this.CollectGroupingParameters(groupByColumn, groupByOrder,
                    column, order, secondaryColumn, secondaryOrder);
                this.BuildGroupsCore(parameters, operation);
            } finally {
                this.sortOperation = previous;
            }
        }

        private void BuildGroupsCore(GroupingParameters parameters, SortOperation operation) {
            CreateGroupsEventArgs args = new CreateGroupsEventArgs(parameters) {
                Canceled = parameters.GroupByColumn != null && !parameters.GroupByColumn.Groupable
            };
            this.OnBeforeCreatingGroups(args);
            if (args.Canceled) {
                operation.Cancel();
                return;
            }
            if (parameters.GroupByColumn == null)
            {
                throw new InvalidOperationException("Grouping parameters must specify a grouping column.");
            }
            this.ValidateGroupingColumn(parameters.GroupByColumn);
            this.RememberCollapsedGroups();
            args.Groups ??= this.MakeGroups(parameters);
            foreach (OLVGroup group in args.Groups) {
                if (this.lastGroupingColumn == parameters.GroupByColumn &&
                    this.collapsedGroups.TryGetValue(group.Key, out bool collapsed))
                {
                    group.Collapsed = collapsed;
                }
            }
            this.OnAboutToCreateGroups(args);
            if (args.Canceled) {
                operation.Cancel();
                return;
            }

            // Own the accepted container: strategies may reuse their preparation buffers.
            args.Groups = new List<OLVGroup>(args.Groups);
            if (!this.VirtualMode)
            {
                this.ListViewItemSorter = null;
            }
            this.OLVGroups = args.Groups;
            this.CreateGroups(args.Groups);
            this.InvalidateModelRows();
            if (this.lastGroupingColumn != parameters.GroupByColumn)
            {
                this.collapsedGroups = new NullableDictionary<object, bool>();
            }
            this.lastGroupingColumn = parameters.GroupByColumn;
            this.lastGroupingOrder = parameters.GroupByOrder;
            operation.Commit();
            this.OnAfterCreatingGroups(args);
        }

        /// <summary>
        /// Collect the settings that govern this grouping operation.
        /// </summary>
        protected virtual GroupingParameters CollectGroupingParameters(OLVColumn groupByColumn, SortOrder groupByOrder,
            OLVColumn sortByColumn, SortOrder sortByOrder, OLVColumn secondaryColumn, SortOrder secondaryOrder) {
            SortDescriptionCollection descriptions = this.sortOperation != null
                ? new SortDescriptionCollection(this.sortOperation.Configuration).WithPrimary(sortByColumn, sortByOrder)
                : new SortDescriptionCollection(sortByColumn, sortByOrder, secondaryColumn, secondaryOrder);
            return new GroupingParameters(this, groupByColumn, groupByOrder, descriptions,
                this.ShowItemCountOnGroups ? groupByColumn.GroupWithItemCountFormatOrDefault : null,
                this.ShowItemCountOnGroups ? groupByColumn.GroupWithItemCountSingularFormatOrDefault : null,
                this.legacySortGroupItemsByPrimaryColumn == true && this.grouping.Column == null &&
                descriptions.GetActiveDescriptions().Count == 1);
        }

        /// <summary>
        /// Prepare groups without changing the displayed groups.
        /// </summary>
        protected virtual IList<OLVGroup> MakeGroups(GroupingParameters parms) {
            IList<OLVSortDescription> descriptions = parms.GetItemSortDescriptions();
            IComparer<OLVListItem> comparer = parms.ModelComparer != null
                ? Comparer<OLVListItem>.Create((x, y) => parms.ModelComparer.Compare(x.RowObject, y.RowObject))
                : parms.ItemComparer ?? (descriptions.Count > 0 ? new ColumnComparer(descriptions) : null);
            return parms.MakeGroups(parms.ListView.Items.Cast<OLVListItem>(), item => item.RowObject, comparer,
                (group, members) => group.Items = members);
        }

        private void ValidateGroupingColumn(OLVColumn column) {
            if (column == null)
            {
                return;
            }
            if (!column.Groupable)
            {
                throw new ArgumentException("The grouping column must be groupable.", nameof(column));
            }
            if (!this.AllColumns.Contains(column) && column.ListView != this)
            {
                throw new ArgumentException("The grouping column must belong to this list.", nameof(column));
            }
        }

        private void ResolveGrouping(SortDescriptionCollection descriptions, out OLVColumn column, out SortOrder order) {
            column = this.grouping.Column;
            SortOrder sortOrder = descriptions.GetOrder(0);
            order = this.grouping.GroupOrder ??
                (sortOrder == SortOrder.None ? SortOrder.Ascending : sortOrder);
            if (column != null) {
                this.ValidateGroupingColumn(column);
                return;
            }
            column = descriptions.GetColumn(0);
            if (this.IsGroupingColumn(column))
            {
                return;
            }
            if (this.IsGroupingColumn(this.lastGroupingColumn)) {
                column = this.lastGroupingColumn;
                order = this.grouping.GroupOrder ?? this.lastGroupingOrder;
                return;
            }
            column = this.Columns.Cast<OLVColumn>().FirstOrDefault(candidate => candidate.Groupable);
        }

        private bool IsGroupingColumn(OLVColumn column) {
            return column != null && column.Groupable &&
                (this.AllColumns.Contains(column) || column.ListView == this);
        }
    }
}
