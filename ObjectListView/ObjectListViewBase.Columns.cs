/*
 * Copyright (C) 2006-2018 Phillip Piper
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using MethodInvoker = System.Windows.Forms.MethodInvoker;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Threading;

namespace BrightIdeasSoftware
{
    public abstract partial class ObjectListViewBase {
        #region Column header clicking, column hiding and resizing

        /// <summary>
        /// The user has right clicked on the column headers. Do whatever is required
        /// </summary>
        /// <returns>Return true if this event has been handle</returns>
        protected virtual bool HandleHeaderRightClick(int columnIndex) {
            ToolStripDropDown menu = this.MakeHeaderRightClickMenu(columnIndex);
            ColumnRightClickEventArgs eventArgs = new ColumnRightClickEventArgs(columnIndex, menu, Cursor.Position);
            this.OnColumnRightClick(eventArgs);

            // Did the event handler stop any further processing?
            if (eventArgs.Cancel)
            {
                return false;
            }

            return this.ShowHeaderRightClickMenu(columnIndex, eventArgs.MenuStrip, eventArgs.Location);
        }

        /// <summary>
        /// Show a menu that is appropriate when the given column header is clicked.
        /// </summary>
        /// <param name="columnIndex">The index of the header that was clicked. This
        /// can be -1, indicating that the header was clicked outside of a column</param>
        /// <param name="menu">The menu to display.</param>
        /// <param name="pt">Where should the menu be shown</param>
        /// <returns>True if a menu was displayed</returns>
        protected virtual bool ShowHeaderRightClickMenu(int columnIndex, ToolStripDropDown menu, Point pt) {
            if (menu.Items.Count > 0) {
                menu.Show(pt);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Create the menu that should be displayed when the user right clicks
        /// on the given column header.
        /// </summary>
        /// <param name="columnIndex">Index of the column that was right clicked.
        /// This can be negative, which indicates a click outside of any header.</param>
        /// <returns>The toolstrip that should be displayed</returns>
        protected virtual ToolStripDropDown MakeHeaderRightClickMenu(int columnIndex) {
            ToolStripDropDown m = new ContextMenuStrip();

            if (columnIndex >= 0 && this.UseFiltering && this.ShowFilterMenuOnRightClick)
            {
                m = this.MakeFilteringMenu(m, columnIndex);
            }

            if (columnIndex >= 0 && this.ShowCommandMenuOnRightClick)
            {
                m = this.MakeColumnCommandMenu(m, columnIndex);
            }

            if (this.SelectColumnsOnRightClickBehaviour != ColumnSelectBehaviour.None) {
                m = this.MakeColumnSelectMenu(m);
            }

            return m;
        }

        /// <summary>
        /// The user has right clicked on the column headers. Do whatever is required
        /// </summary>
        /// <returns>Return true if this event has been handle</returns>
        [Obsolete("Use HandleHeaderRightClick(int) instead")]
        protected virtual bool HandleHeaderRightClick() {
            return false;
        }

        /// <summary>
        /// Show a popup menu at the given point which will allow the user to choose which columns
        /// are visible on this listview
        /// </summary>
        /// <param name="pt">Where should the menu be placed</param>
        [Obsolete("Use ShowHeaderRightClickMenu instead")]
        protected virtual void ShowColumnSelectMenu(Point pt) {
            ToolStripDropDown m = this.MakeColumnSelectMenu(new ContextMenuStrip());
            m.Show(pt);
        }

        /// <summary>
        /// Show a popup menu at the given point which will allow the user to choose which columns
        /// are visible on this listview
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="pt">Where should the menu be placed</param>
        [Obsolete("Use ShowHeaderRightClickMenu instead")]
        protected virtual void ShowColumnCommandMenu(int columnIndex, Point pt) {
            ToolStripDropDown m = this.MakeColumnCommandMenu(new ContextMenuStrip(), columnIndex);
            if (this.SelectColumnsOnRightClick) {
                if (m.Items.Count > 0)
                {
                    m.Items.Add(new ToolStripSeparator());
                }

                this.MakeColumnSelectMenu(m);
            }
            m.Show(pt);
        }

        /// <summary>
        /// Gets or set the text to be used for the sorting ascending command
        /// </summary>
        [Category("Labels - ObjectListView"), DefaultValue("Sort ascending by '{0}'"), Localizable(true)]
        public string MenuLabelSortAscending {
            get { return this.menuLabelSortAscending; }
            set { this.menuLabelSortAscending = value; }
        }
        private string menuLabelSortAscending = "Sort ascending by '{0}'";

        /// <summary>
        /// 
        /// </summary>
        [Category("Labels - ObjectListView"), DefaultValue("Sort descending by '{0}'"), Localizable(true)]
        public string MenuLabelSortDescending {
            get { return this.menuLabelSortDescending; }
            set { this.menuLabelSortDescending = value; }
        }
        private string menuLabelSortDescending = "Sort descending by '{0}'";

        /// <summary>
        /// 
        /// </summary>
        [Category("Labels - ObjectListView"), DefaultValue("Group by '{0}'"), Localizable(true)]
        public string MenuLabelGroupBy {
            get { return this.menuLabelGroupBy; }
            set { this.menuLabelGroupBy = value; }
        }
        private string menuLabelGroupBy = "Group by '{0}'";

        /// <summary>
        /// 
        /// </summary>
        [Category("Labels - ObjectListView"), DefaultValue("Lock grouping on '{0}'"), Localizable(true)]
        public string MenuLabelLockGroupingOn {
            get { return this.menuLabelLockGroupingOn; }
            set { this.menuLabelLockGroupingOn = value; }
        }
        private string menuLabelLockGroupingOn = "Lock grouping on '{0}'";

        /// <summary>
        /// 
        /// </summary>
        [Category("Labels - ObjectListView"), DefaultValue("Unlock grouping from '{0}'"), Localizable(true)]
        public string MenuLabelUnlockGroupingOn {
            get { return this.menuLabelUnlockGroupingOn; }
            set { this.menuLabelUnlockGroupingOn = value; }
        }
        private string menuLabelUnlockGroupingOn = "Unlock grouping from '{0}'";

        /// <summary>
        /// 
        /// </summary>
        [Category("Labels - ObjectListView"), DefaultValue("Turn off groups"), Localizable(true)]
        public string MenuLabelTurnOffGroups {
            get { return this.menuLabelTurnOffGroups; }
            set { this.menuLabelTurnOffGroups = value; }
        }
        private string menuLabelTurnOffGroups = "Turn off groups";

        /// <summary>
        /// 
        /// </summary>
        [Category("Labels - ObjectListView"), DefaultValue("Unsort"), Localizable(true)]
        public string MenuLabelUnsort {
            get { return this.menuLabelUnsort; }
            set { this.menuLabelUnsort = value; }
        }
        private string menuLabelUnsort = "Unsort";

        /// <summary>
        /// 
        /// </summary>
        [Category("Labels - ObjectListView"), DefaultValue("Columns"), Localizable(true)]
        public string MenuLabelColumns {
            get { return this.menuLabelColumns; }
            set { this.menuLabelColumns = value; }
        }
        private string menuLabelColumns = "Columns";

        /// <summary>
        /// 
        /// </summary>
        [Category("Labels - ObjectListView"), DefaultValue("Select Columns..."), Localizable(true)]
        public string MenuLabelSelectColumns {
            get { return this.menuLabelSelectColumns; }
            set { this.menuLabelSelectColumns = value; }
        }
        private string menuLabelSelectColumns = "Select Columns...";

        /// <summary>
        /// Gets or sets the image that will be place next to the Sort Ascending command
        /// </summary>
        public static Bitmap SortAscendingImage { get; set; } = BrightIdeasSoftware.Properties.Resources.SortAscending;

        /// <summary>
        /// Gets or sets the image that will be placed next to the Sort Descending command
        /// </summary>
        public static Bitmap SortDescendingImage { get; set; } = BrightIdeasSoftware.Properties.Resources.SortDescending;

        /// <summary>
        /// Append the column selection menu items to the given menu strip.
        /// </summary>
        /// <param name="strip">The menu to which the items will be added.</param>
        /// <param name="columnIndex"></param>
        /// <returns>Return the menu to which the items were added</returns>
        public virtual ToolStripDropDown MakeColumnCommandMenu(ToolStripDropDown strip, int columnIndex) {
            OLVColumn column = this.GetColumn(columnIndex);
            if (column == null)
            {
                return strip;
            }

            if (strip.Items.Count > 0)
            {
                strip.Items.Add(new ToolStripSeparator());
            }

            string label = String.Format(CultureInfo.CurrentCulture, this.MenuLabelSortAscending, column.Text);
            if (column.Sortable && !String.IsNullOrEmpty(label)) {
                strip.Items.Add(label, ObjectListViewBase.SortAscendingImage, (EventHandler)delegate(object sender, EventArgs args) {
                    this.Sort(column, SortOrder.Ascending);
                });
            }
            label = String.Format(CultureInfo.CurrentCulture, this.MenuLabelSortDescending, column.Text);
            if (column.Sortable && !String.IsNullOrEmpty(label)) {
                strip.Items.Add(label, ObjectListViewBase.SortDescendingImage, (EventHandler)delegate(object sender, EventArgs args) {
                    this.Sort(column, SortOrder.Descending);
                });
            }
            if (this.CanShowGroups) {
                label = String.Format(CultureInfo.CurrentCulture, this.MenuLabelGroupBy, column.Text);
                if (column.Groupable && !String.IsNullOrEmpty(label)) {
                    strip.Items.Add(label, null, (EventHandler)delegate(object sender, EventArgs args) {
                        this.Grouping = GroupingConfiguration.Fixed(column, SortOrder.Ascending);
                        this.ShowGroups = true;
                        this.BuildGroups();
                    });
                }
            }
            if (this.ShowGroups) {
                if (this.grouping.Column == column) {
                    label = String.Format(CultureInfo.CurrentCulture, this.MenuLabelUnlockGroupingOn, column.Text);
                    if (!String.IsNullOrEmpty(label)) {
                        strip.Items.Add(label, null, (EventHandler)delegate(object sender, EventArgs args) {
                            this.Grouping = GroupingConfiguration.FollowSorting();
                            this.BuildGroups();
                        });
                    }
                } else {
                    label = String.Format(CultureInfo.CurrentCulture, this.MenuLabelLockGroupingOn, column.Text);
                    if (column.Groupable && !String.IsNullOrEmpty(label)) {
                        strip.Items.Add(label, null, (EventHandler)delegate(object sender, EventArgs args) {
                            this.Grouping = GroupingConfiguration.Fixed(column, SortOrder.Ascending);
                            this.ShowGroups = true;
                            this.BuildGroups();
                        });
                    }
                }
                label = String.Format(CultureInfo.CurrentCulture, this.MenuLabelTurnOffGroups, column.Text);
                if (!String.IsNullOrEmpty(label)) {
                    strip.Items.Add(label, null, (EventHandler)delegate(object sender, EventArgs args) {
                        this.ShowGroups = false;
                        this.BuildList();
                    });
                }
            } else {
                label = String.Format(CultureInfo.CurrentCulture, this.MenuLabelUnsort, column.Text);
                if (column.Sortable && !String.IsNullOrEmpty(label) && this.PrimarySortOrder != SortOrder.None) {
                    strip.Items.Add(label, null, (EventHandler)delegate(object sender, EventArgs args) {
                        this.Unsort();
                    });
                }
            }

            return strip;
        }

        /// <summary>
        /// Append the column selection menu items to the given menu strip.
        /// </summary>
        /// <param name="strip">The menu to which the items will be added.</param>
        /// <returns>Return the menu to which the items were added</returns>
        public virtual ToolStripDropDown MakeColumnSelectMenu(ToolStripDropDown strip) {

            System.Diagnostics.Debug.Assert(this.SelectColumnsOnRightClickBehaviour != ColumnSelectBehaviour.None);

            // Append a separator if the menu isn't empty and the last item isn't already a separator
            if (strip.Items.Count > 0 && (!(strip.Items[strip.Items.Count-1] is ToolStripSeparator)))
            {
                strip.Items.Add(new ToolStripSeparator());
            }

            if (this.SelectColumnsOnRightClickBehaviour == ColumnSelectBehaviour.ModelDialog) {
                strip.Items.Add(this.MenuLabelSelectColumns, null, delegate(object sender, EventArgs args) {
                    (new ColumnSelectionForm()).OpenOn(this);
                });
            }

            if (this.SelectColumnsOnRightClickBehaviour == ColumnSelectBehaviour.Submenu) {
                ToolStripMenuItem menu = new ToolStripMenuItem(this.MenuLabelColumns);
                menu.DropDownItemClicked += new ToolStripItemClickedEventHandler(this.ColumnSelectMenuItemClicked);
                strip.Items.Add(menu);
                this.AddItemsToColumnSelectMenu(menu.DropDownItems);
            }

            if (this.SelectColumnsOnRightClickBehaviour == ColumnSelectBehaviour.InlineMenu) {
                strip.ItemClicked += new ToolStripItemClickedEventHandler(this.ColumnSelectMenuItemClicked);
                strip.Closing += new ToolStripDropDownClosingEventHandler(this.ColumnSelectMenuClosing);
                this.AddItemsToColumnSelectMenu(strip.Items);
            }

            FormTheme.ApplyMenu(this.FindForm(), strip);
            return strip;
        }

        /// <summary>
        /// Create the menu items that will allow columns to be choosen and add them to the 
        /// given collection
        /// </summary>
        /// <param name="items"></param>
        protected void AddItemsToColumnSelectMenu(ToolStripItemCollection items) {

            this.RememberDisplayIndicies();

            // Predict the filtered-list position assigned when a never-shown column is enabled.
            Dictionary<OLVColumn, int> displayIndices = new Dictionary<OLVColumn, int>();
            int visibleIndex = 0;
            foreach (OLVColumn column in this.AllColumns) {
                displayIndices[column] = column.LastDisplayIndex == -1 ? visibleIndex : column.LastDisplayIndex;
                if (visibleIndex == 0 || column.IsVisible)
                {
                    visibleIndex++;
                }
            }

            // Sort columns by display order
            IEnumerable<OLVColumn> columns = this.AllColumns.OrderBy(column => displayIndices[column]);

            // Build menu from sorted columns
            foreach (OLVColumn col in columns) {
                ToolStripMenuItem mi = new ToolStripMenuItem(col.Text);
                mi.Checked = col.IsVisible;
                mi.Tag = col;

                // The 'Index' property returns -1 when the column is not visible, so if the
                // column isn't visible we have to enable the item. Also the first column can't be turned off
                mi.Enabled = !col.IsVisible || col.CanBeHidden;
                items.Add(mi);
            }
        }

        private void ColumnSelectMenuItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            this.contextMenuStaysOpen = false;
            ToolStripMenuItem menuItemClicked = e.ClickedItem as ToolStripMenuItem;
            if (menuItemClicked == null)
            {
                return;
            }

            OLVColumn col = menuItemClicked.Tag as OLVColumn;
            if (col == null)
            {
                return;
            }

            menuItemClicked.Checked = !menuItemClicked.Checked;
            col.IsVisible = menuItemClicked.Checked;
            this.contextMenuStaysOpen = this.SelectColumnsMenuStaysOpen;
            this.BeginInvoke(new MethodInvoker(this.RebuildColumns));
        }
        private bool contextMenuStaysOpen;

        private void ColumnSelectMenuClosing(object sender, ToolStripDropDownClosingEventArgs e) {
            e.Cancel = this.contextMenuStaysOpen && e.CloseReason == ToolStripDropDownCloseReason.ItemClicked;
            this.contextMenuStaysOpen = false;
        }

        /// <summary>
        /// Create a Filtering menu
        /// </summary>
        /// <param name="strip"></param>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        public virtual ToolStripDropDown MakeFilteringMenu(ToolStripDropDown strip, int columnIndex) {
            OLVColumn column = this.GetColumn(columnIndex);
            if (column == null)
            {
                return strip;
            }

            FilterMenuBuilder strategy = this.FilterMenuBuildStrategy;
            if (strategy == null)
            {
                return strip;
            }

            ToolStripDropDown menu = strategy.MakeFilterMenu(strip, this, column);
            FormTheme.ApplyMenu(this.FindForm(), menu);
            return menu;
        }

        /// <summary>
        /// Override the OnColumnReordered method to do what we want
        /// </summary>
        /// <param name="e"></param>
        protected override void OnColumnReordered(ColumnReorderedEventArgs e) {
            base.OnColumnReordered(e);

            // The internal logic of the .NET code behind a ENDDRAG event means that,
            // at this point, the DisplayIndex's of the columns are not yet as they are
            // going to be. So we have to invoke a method to run later that will remember
            // what the real DisplayIndex's are.
            this.BeginInvoke(new MethodInvoker(this.RememberDisplayIndicies));
        }

        private void RememberDisplayIndicies() {
            // Remember the display indexes so we can put them back at a later date
            foreach (OLVColumn column in this.AllColumns) {
                if (column.Index >= 0)
                {
                    column.LastDisplayIndex = column.DisplayIndex;
                }
            }
        }

        /// <summary>
        /// When the column widths are changing, resize the space filling columns
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void HandleColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e) {
            if (this.UpdateSpaceFillingColumnsWhenDraggingColumnDivider && !this.GetColumn(e.ColumnIndex).FillsFreeSpace) {
                // If the width of a column is increasing, resize any space filling columns allowing the extra
                // space that the new column width is going to consume
                int oldWidth = this.GetColumn(e.ColumnIndex).Width;
                if (e.NewWidth > oldWidth)
                {
                    this.ResizeFreeSpaceFillingColumns(this.ClientSize.Width - (e.NewWidth - oldWidth));
                }
                else
                {
                    this.ResizeFreeSpaceFillingColumns();
                }
            }
        }

        /// <summary>
        /// When the column widths change, resize the space filling columns
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void HandleColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e) {
            if (!this.GetColumn(e.ColumnIndex).FillsFreeSpace)
            {
                this.ResizeFreeSpaceFillingColumns();
            }
        }

        /// <summary>
        /// When the size of the control changes, we have to resize our space filling columns.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void HandleLayout(object sender, LayoutEventArgs e) {
            // We have to delay executing the recalculation of the columns, since virtual lists
            // get terribly confused if we resize the column widths during this event.
            if (!this.hasResizeColumnsHandler) {
                this.hasResizeColumnsHandler = true;
                this.RunWhenIdle(this.HandleApplicationIdleResizeColumns);
            }
        }

        private void RunWhenIdle(EventHandler eventHandler) {
            Application.Idle += eventHandler;
            if (!this.CanUseApplicationIdle) {
                SynchronizationContext.Current.Post(delegate(object x) { Application.RaiseIdle(EventArgs.Empty); }, null);
            }
        }

        /// <summary>
        /// Resize our space filling columns so they fill any unoccupied width in the control
        /// </summary>
        protected virtual void ResizeFreeSpaceFillingColumns() {
            this.ResizeFreeSpaceFillingColumns(this.ClientSize.Width);
        }

        /// <summary>
        /// Resize our space filling columns so they fill any unoccupied width in the control
        /// </summary>
        protected virtual void ResizeFreeSpaceFillingColumns(int freeSpace) {
            // It's too confusing to dynamically resize columns at design time.
            if (this.DesignMode)
            {
                return;
            }

            if (this.Frozen)
            {
                return;
            }

            this.BeginUpdate();

            // Calculate the free space available
            int totalProportion = 0;
            List<OLVColumn> spaceFillingColumns = new List<OLVColumn>();
            for (int i = 0; i < this.Columns.Count; i++) {
                OLVColumn col = this.GetColumn(i);
                if (col.FillsFreeSpace) {
                    spaceFillingColumns.Add(col);
                    totalProportion += col.FreeSpaceProportion;
                } else
                {
                    freeSpace -= col.Width;
                }
            }
            freeSpace = Math.Max(0, freeSpace);

            // Any space filling column that would hit it's Minimum or Maximum
            // width must be treated as a fixed column.
            foreach (OLVColumn col in spaceFillingColumns.ToArray()) {
                int newWidth = (freeSpace * col.FreeSpaceProportion) / totalProportion;

                if (col.MinimumWidth != -1 && newWidth < col.MinimumWidth)
                {
                    newWidth = col.MinimumWidth;
                }
                else if (col.MaximumWidth != -1 && newWidth > col.MaximumWidth)
                {
                    newWidth = col.MaximumWidth;
                }
                else
                {
                    newWidth = 0;
                }

                if (newWidth > 0) {
                    col.Width = newWidth;
                    freeSpace -= newWidth;
                    totalProportion -= col.FreeSpaceProportion;
                    spaceFillingColumns.Remove(col);
                }
            }

            // Distribute the free space between the columns
            foreach (OLVColumn col in spaceFillingColumns) {
                col.Width = (freeSpace*col.FreeSpaceProportion)/totalProportion;
            }

            this.EndUpdate();
        }

        #endregion
    }
}
