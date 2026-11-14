/*
 * Copyright (C) 2006-2014 Phillip Piper
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * If you wish to use this code in a closed source application, please contact phillip.piper@gmail.com.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace BrightIdeasSoftware
{
    public partial class TypedTreeListView<T> where T : class {

        #region Events

        /// <summary>
        /// This event is triggered when user input requests the expansion of a list item.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when a branch is about to expand.")]
        public event EventHandler<TreeBranchExpandingEventArgs> Expanding;

        /// <summary>
        /// This event is triggered when user input requests the collapse of a list item.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when a branch is about to collapsed.")]
        public event EventHandler<TreeBranchCollapsingEventArgs> Collapsing;

        /// <summary>
        /// This event is triggered after the expansion of a list item due to user input.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when a branch has been expanded.")]
        public event EventHandler<TreeBranchExpandedEventArgs> Expanded;

        /// <summary>
        /// This event is triggered after the collapse of a list item due to user input.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when a branch has been collapsed.")]
        public event EventHandler<TreeBranchCollapsedEventArgs> Collapsed;

        #endregion

        #region OnEvents

        /// <summary>
        /// Trigger the expanding event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnExpanding(TreeBranchExpandingEventArgs e) {
            if (this.Expanding != null)
            {
                this.Expanding(this, e);
            }
        }

        /// <summary>
        /// Trigger the collapsing event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnCollapsing(TreeBranchCollapsingEventArgs e) {
            if (this.Collapsing != null)
            {
                this.Collapsing(this, e);
            }
        }

        /// <summary>
        /// Trigger the expanded event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnExpanded(TreeBranchExpandedEventArgs e) {
            if (this.Expanded != null)
            {
                this.Expanded(this, e);
            }
        }

        /// <summary>
        /// Trigger the collapsed event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnCollapsed(TreeBranchCollapsedEventArgs e) {
            if (this.Collapsed != null)
            {
                this.Collapsed(this, e);
            }
        }

        #endregion
    }
}
