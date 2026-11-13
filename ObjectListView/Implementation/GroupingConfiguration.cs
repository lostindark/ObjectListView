using System;
using System.Windows.Forms;

namespace BrightIdeasSoftware {
    /// <summary>
    /// Defines how grouping follows, or stays independent of, item sorting.
    /// </summary>
    public sealed class GroupingConfiguration {
        private GroupingConfiguration(OLVColumn column, SortOrder? groupOrder) {
            if (groupOrder.HasValue && groupOrder != SortOrder.None &&
                groupOrder != SortOrder.Ascending && groupOrder != SortOrder.Descending)
            {
                throw new ArgumentOutOfRangeException(nameof(groupOrder));
            }

            this.Column = column;
            this.GroupOrder = groupOrder;
        }

        /// <summary>
        /// Gets the fixed grouping column, or null to follow item sorting.
        /// </summary>
        public OLVColumn Column { get; }

        /// <summary>
        /// Gets the group direction. Null follows item sorting; None preserves discovery order.
        /// </summary>
        public SortOrder? GroupOrder { get; }

        /// <summary>
        /// Follow the primary item-sort column, optionally fixing the group direction.
        /// </summary>
        public static GroupingConfiguration FollowSorting(SortOrder? groupOrder = null) {
            return new GroupingConfiguration(null, groupOrder);
        }

        /// <summary>
        /// Group by a fixed column. Null groupOrder follows the item-sort direction.
        /// </summary>
        public static GroupingConfiguration Fixed(OLVColumn column, SortOrder? groupOrder = null) {
            ArgumentNullException.ThrowIfNull(column, nameof(column));
            return new GroupingConfiguration(column, groupOrder);
        }
    }
}
