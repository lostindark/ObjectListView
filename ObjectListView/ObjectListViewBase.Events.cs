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
        #region Event handlers

        /// <summary>
        /// The application is idle. Trigger a SelectionChanged event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void HandleApplicationIdle(object sender, EventArgs e) {
            // Remove the handler before triggering the event
            Application.Idle -= new EventHandler(HandleApplicationIdle);
            this.hasIdleHandler = false;

            this.OnSelectionChanged(new EventArgs());
        }

        /// <summary>
        /// The application is idle. Handle the column resizing event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void HandleApplicationIdleResizeColumns(object sender, EventArgs e) {
            // Remove the handler before triggering the event
            Application.Idle -= new EventHandler(this.HandleApplicationIdleResizeColumns);
            this.hasResizeColumnsHandler = false;

            this.ResizeFreeSpaceFillingColumns();
        }

        /// <summary>
        /// Handle the BeginScroll listview notification
        /// </summary>
        /// <param name="m"></param>
        /// <returns>True if the event was completely handled</returns>
        protected virtual bool HandleBeginScroll(ref Message m) {
            //System.Diagnostics.Debug.WriteLine("LVN_BEGINSCROLL");

            NativeMethods.NMLVSCROLL nmlvscroll = (NativeMethods.NMLVSCROLL)m.GetLParam(typeof(NativeMethods.NMLVSCROLL));
            if (nmlvscroll.dx != 0) {
                int scrollPositionH = NativeMethods.GetScrollPosition(this, true);
                ScrollEventArgs args = new ScrollEventArgs(ScrollEventType.EndScroll, scrollPositionH - nmlvscroll.dx, scrollPositionH, ScrollOrientation.HorizontalScroll);
                this.OnScroll(args);

                // Force any empty list msg to redraw when the list is scrolled horizontally
                if (this.GetItemCount() == 0)
                {
                    this.Invalidate();
                }
            }
            if (nmlvscroll.dy != 0) {
                int scrollPositionV = NativeMethods.GetScrollPosition(this, false);
                ScrollEventArgs args = new ScrollEventArgs(ScrollEventType.EndScroll, scrollPositionV - nmlvscroll.dy, scrollPositionV, ScrollOrientation.VerticalScroll);
                this.OnScroll(args);
            }

            return false;
        }

        /// <summary>
        /// Handle the EndScroll listview notification
        /// </summary>
        /// <param name="m"></param>
        /// <returns>True if the event was completely handled</returns>
        protected virtual bool HandleEndScroll(ref Message m) {
            //System.Diagnostics.Debug.WriteLine("LVN_BEGINSCROLL");

            // There is a bug in ListView under XP that causes the gridlines to be incorrectly scrolled
            // when the left button is clicked to scroll. This is supposedly documented at
            // KB 813791, but I couldn't find it anywhere. You can follow this thread to see the discussion
            // http://www.ureader.com/msg/1484143.aspx

            if (!ObjectListViewBase.IsVistaOrLater && ObjectListViewBase.IsLeftMouseDown && this.GridLines) {
                this.Invalidate();
                this.Update();
            }

            return false;
        }

        /// <summary>
        /// Handle the LinkClick listview notification
        /// </summary>
        /// <param name="m"></param>
        /// <returns>True if the event was completely handled</returns>
        protected virtual bool HandleLinkClick(ref Message m) {
            //System.Diagnostics.Debug.WriteLine("HandleLinkClick");

            NativeMethods.NMLVLINK nmlvlink = (NativeMethods.NMLVLINK)m.GetLParam(typeof(NativeMethods.NMLVLINK));

            // Find the group that was clicked and trigger an event
            foreach (OLVGroup x in this.OLVGroups) {
                if (x.GroupId == nmlvlink.iSubItem) {
                    this.OnGroupTaskClicked(new GroupTaskClickedEventArgs(x));
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The cell tooltip control wants information about the tool tip that it should show.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void HandleCellToolTipShowing(object sender, ToolTipShowingEventArgs e) {
            this.BuildCellEvent(e, this.PointToClient(Cursor.Position));
            if (e.Item != null) {
                e.TargetBounds = e.HitTest.HitBounds.IsEmpty ?
                    (this.View == View.Details ? e.Item.GetSubItemBounds(e.ColumnIndex) : e.Item.Bounds) : e.HitTest.HitBounds;
                e.Text = this.GetCellToolTip(e.ColumnIndex, e.RowIndex);
                this.ApplyToolTipAppearance(e);
                this.OnCellToolTip(e);
                ApplyToolTipAccessibility(e);
            }
        }

        /// <summary>
        /// Allow the HeaderControl to call back into HandleHeaderToolTipShowing without making that method public
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void HeaderToolTipShowingCallback(object sender, ToolTipShowingEventArgs e) {
            this.HandleHeaderToolTipShowing(sender, e);
        }

        /// <summary>
        /// The header tooltip control wants information about the tool tip that it should show.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void HandleHeaderToolTipShowing(object sender, ToolTipShowingEventArgs e) {
            e.ColumnIndex = this.HeaderControl.ColumnIndexUnderCursor;
            if (e.ColumnIndex < 0)
            {
                return;
            }

            e.RowIndex = -1;
            e.Model = null;
            e.Column = this.GetColumn(e.ColumnIndex);
            e.TargetBounds = this.HeaderControl.GetToolTipTargetBounds(e.Column);
            e.Text = this.GetHeaderToolTip(e.ColumnIndex);
            e.ListView = this;
            this.ApplyToolTipAppearance(e);
            this.OnHeaderToolTip(e);
            ApplyToolTipAccessibility(e);
        }

        /// <summary>
        /// Event handler for the column click event
        /// </summary>
        protected virtual void HandleColumnClick(object sender, ColumnClickEventArgs e) {
            this.HandleColumnClick(e, Control.ModifierKeys);
        }

        /// <summary>
        /// Sort a clicked column using the keyboard modifiers captured for the click.
        /// </summary>
        protected virtual void HandleColumnClick(ColumnClickEventArgs e, Keys modifiers) {
            if (!this.PossibleFinishCellEditing())
            {
                return;
            }

            OLVColumn column = this.GetColumn(e.Column);
            bool addToSort = (modifiers & Keys.Shift) == Keys.Shift;
            SortOrder order = SortOrder.Ascending;
            foreach (OLVSortDescription description in this.SortDescriptions) {
                if (description.Column == column) {
                    order = description.SortOrder == SortOrder.Descending ? SortOrder.Ascending : SortOrder.Descending;
                    break;
                }
            }

            this.BeginUpdate();
            try {
                this.Sort(column, order, addToSort);
            } finally {
                this.EndUpdate();
            }
        }

        #endregion

        #region OnEvent Handling

        /// <summary>
        /// We need the click count in the mouse up event, but that is always 1.
        /// So we have to remember the click count from the preceding mouse down event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e) {
            //System.Diagnostics.Debug.WriteLine(String.Format("OnMouseDown: {0}, {1}", e.Button, e.Clicks));
            this.lastMouseDownClickCount = e.Clicks;
            this.lastMouseDownButton = e.Button;
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && this.IsHandleCreated) {
                this.UpdateHotItem(e.Location);
                this.pressedStyleRowIndex = this.OlvHitTest(e.X, e.Y).RowIndex;
                this.Invalidate();
            }
        }
        private int lastMouseDownClickCount;
        private MouseButtons lastMouseDownButton;

        /// <summary>
        /// When the mouse leaves the control, remove any hot item highlighting
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);

            if (!this.Created)
            {
                return;
            }

            this.UpdateHotItem(new Point(-1,-1));
        }

        // We could change the hot item on the mouse hover event, but it looks wrong.

        //protected override void OnMouseHover(EventArgs e) {
        //    System.Diagnostics.Debug.WriteLine(String.Format("OnMouseHover"));
        //    base.OnMouseHover(e);
        //    this.UpdateHotItem(this.PointToClient(Cursor.Position));
        //}

        /// <summary>
        /// When the mouse moves, we might need to change the hot item.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);

            if (this.Created)
            {
                HandleMouseMove(e.Location);
            }
        }

        internal void HandleMouseMove(Point pt) {
            //System.Diagnostics.Debug.WriteLine(String.Format("HandleMouseMove: {0}", pt));

            CellOverEventArgs args = new CellOverEventArgs();
            this.BuildCellEvent(args, pt);
            this.OnCellOver(args);
            this.MouseMoveHitTest = args.HitTest;

            if (!args.Handled)
            {
                this.UpdateHotItem(args.HitTest);
            }
        }

        /// <summary>
        /// Check to see if we need to start editing a cell
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e) {
            //System.Diagnostics.Debug.WriteLine(String.Format("OnMouseUp: {0}, {1}", e.Button, e.Clicks));

            base.OnMouseUp(e);
            this.pressedStyleRowIndex = -1;
            this.Invalidate();

            if (!this.Created)
            {
                return;
            }

            // Sigh! More complexity. e.Button is not reliable when clicking on group headers.
            // The mouse up event for first click on a group header always reports e.Button as None.
            // Subsequent mouse up events report the button from the previous event.
            // However, mouse down events are correctly reported, so we use the button value from
            // the last mouse down event.
            if (this.lastMouseDownButton == MouseButtons.Right) {
                this.OnRightMouseUp(e);
                return;
            }

            // What event should we listen for to start cell editing?
            // ------------------------------------------------------
            //
            // We can't use OnMouseClick, OnMouseDoubleClick, OnClick, or OnDoubleClick
            // since they are not triggered for clicks on subitems without Full Row Select.
            //
            // We could use OnMouseDown, but selecting rows is done in OnMouseUp. This means
            // that if we start the editing during OnMouseDown, the editor will automatically
            // lose focus when mouse up happens.
            //

            // Tell the world about a cell click. If someone handles it, don't do anything else
            CellClickEventArgs args = new CellClickEventArgs();
            this.BuildCellEvent(args, e.Location);
            args.ClickCount = this.lastMouseDownClickCount;
            this.OnCellClick(args);
            if (args.Handled)
            {
                return;
            }

            // Did the user click a hyperlink?
            if (this.UseHyperlinks &&
                args.HitTest.HitTestLocation == HitTestLocation.Text &&
                args.SubItem != null &&
                !String.IsNullOrEmpty(args.SubItem.Url)) {
                // We have to delay the running of this process otherwise we can generate
                // a series of MouseUp events (don't ask me why)
                this.BeginInvoke((MethodInvoker)delegate { this.ProcessHyperlinkClicked(args); });
            }

            // No one handled it so check to see if we should start editing.
            if (!this.ShouldStartCellEdit(e))
            {
                return;
            }

            // We only start the edit if the user clicked on the image or text.
            if (args.HitTest.HitTestLocation == HitTestLocation.Nothing)
            {
                return;
            }

            // We don't edit the primary column by single clicks -- only subitems.
            if (this.CellEditActivation == CellEditActivateMode.SingleClick && args.ColumnIndex <= 0)
            {
                return;
            }

            // Don't start a cell edit operation when the user clicks on the background of a checkbox column -- it just looks wrong.
            // If the user clicks on the actual checkbox, changing the checkbox state is handled elsewhere.
            if (args.Column != null && args.Column.CheckBoxes)
            {
                return;
            }

            this.EditSubItem(args.Item, args.ColumnIndex);
        }

        /// <summary>
        /// Tell the world that a hyperlink was clicked and if the event isn't handled,
        /// do the default processing.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void ProcessHyperlinkClicked(CellClickEventArgs e) {
            HyperlinkClickedEventArgs args = new HyperlinkClickedEventArgs();
            args.HitTest = e.HitTest;
            args.ListView = this;
            args.Location = new Point(-1, -1);
            args.Item = e.Item;
            args.SubItem = e.SubItem;
            args.Model = e.Model;
            args.ColumnIndex = e.ColumnIndex;
            args.Column = e.Column;
            args.RowIndex = e.RowIndex;
            args.ModifierKeys = Control.ModifierKeys;
            args.Url = e.SubItem.Url;
            this.OnHyperlinkClicked(args);
            if (!args.Handled) {
                this.StandardHyperlinkClickedProcessing(args);
            }
        }

        /// <summary>
        /// Do the default processing for a hyperlink clicked event, which
        /// is to try and open the url.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void StandardHyperlinkClickedProcessing(HyperlinkClickedEventArgs args) {
            Cursor originalCursor = this.Cursor;
            try {
                this.Cursor = Cursors.WaitCursor;
                System.Diagnostics.Process.Start(args.Url);
            } catch (Win32Exception) {
                System.Media.SystemSounds.Beep.Play();
                // ignore it
            } finally {
                this.Cursor = originalCursor;
            }
            this.MarkUrlVisited(args.Url);
            this.RefreshHotItem();
        }

        /// <summary>
        /// The user right clicked on the control
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnRightMouseUp(MouseEventArgs e) {
            CellRightClickEventArgs args = new CellRightClickEventArgs();
            this.BuildCellEvent(args, e.Location);
            this.OnCellRightClick(args);
            if (!args.Handled) {
                if (args.MenuStrip != null) {
                    args.MenuStrip.Show(this, args.Location);
                }
            }
        }

        internal void BuildCellEvent(CellEventArgs args, Point location) {
            BuildCellEvent(args, location, this.OlvHitTest(location.X, location.Y));
        }

        internal void BuildCellEvent(CellEventArgs args, Point location, OlvListViewHitTestInfo hitTest) {
            args.HitTest = hitTest;
            args.ListView = this;
            args.Location = location;
            args.Item = hitTest.Item;
            args.SubItem = hitTest.SubItem;
            args.Model = hitTest.RowObject;
            args.ColumnIndex = hitTest.ColumnIndex;
            args.Column = hitTest.Column;
            if (hitTest.Item != null)
            {
                args.RowIndex = hitTest.Item.Index;
            }

            args.ModifierKeys = Control.ModifierKeys;

            // In non-details view, we want any hit on an item to act as if it was a hit
            // on column 0 -- which, effectively, it was.
            if (args.Item != null && args.ListView.View != View.Details) {
                args.ColumnIndex = 0;
                args.Column = args.ListView.GetColumn(0);
                args.SubItem = args.Item.GetSubItem(0);
            }
        }

        /// <summary>
        /// This method is called every time a row is selected or deselected. This can be
        /// a pain if the user shift-clicks 100 rows. We override this method so we can
        /// trigger one event for any number of select/deselects that come from one user action
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSelectedIndexChanged(EventArgs e) {
            if (this.SelectionEventsSuspended)
            {
                return;
            }

            base.OnSelectedIndexChanged(e);

            this.TriggerDeferredSelectionChangedEvent();
        }

        /// <summary>
        /// Schedule a SelectionChanged event to happen after the next idle event,
        /// unless we've already scheduled that to happen.
        /// </summary>
        protected virtual void TriggerDeferredSelectionChangedEvent() {
            if (this.SelectionEventsSuspended)
            {
                return;
            }

            // If we haven't already scheduled an event, schedule it to be triggered
            // By using idle time, we will wait until all select events for the same
            // user action have finished before triggering the event.
            if (!this.hasIdleHandler) {
                this.hasIdleHandler = true;
                this.RunWhenIdle(HandleApplicationIdle);
            }
        }

        /// <summary>
        /// Rescale known column widths and limits when the control's DPI changes.
        /// </summary>
        [SuppressMessage("Maintainability", "CA1512", Justification = ".NET Framework 4.6.2 lacks ArgumentOutOfRangeException.ThrowIfNegativeOrZero.")]
        protected virtual void RescaleColumnWidthsForDpi(int dpi) {
            if (dpi <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dpi));
            }

            if (this.initializingColumns || this.IsDesignMode || dpi == this.columnDpi)
            {
                return;
            }

            var columns = new HashSet<OLVColumn>(this.AllColumns);
            foreach (OLVColumn column in this.Columns)
            {
                columns.Add(column);
            }

            foreach (OLVColumn column in columns) {
                int width = ScaleColumnDimension(column.Width, dpi, this.columnDpi);
                int minimum = ScaleColumnDimension(column.MinimumWidth, dpi, this.columnDpi);
                int maximum = ScaleColumnDimension(column.MaximumWidth, dpi, this.columnDpi);
                // Clear both limits so fixed-width columns can change size.
                column.MinimumWidth = -1;
                column.MaximumWidth = -1;
                column.MinimumWidth = minimum;
                column.MaximumWidth = maximum;
                if (width < 0)
                {
                    ((ColumnHeader)column).Width = width;
                }
                else
                {
                    column.Width = width;
                }
            }
            this.columnDpi = dpi;
            this.ResizeFreeSpaceFillingColumns();
        }
        private int columnDpi = 96;

        private static int ScaleColumnDimension(int value, int dpi, int oldDpi) {
            return value < 0 ? value : (int)Math.Min(int.MaxValue,
                Math.Round(value * (double)dpi / oldDpi));
        }

        private int GetWindowDpi() {
#if NETFRAMEWORK
            using (Graphics graphics = Graphics.FromHwnd(this.Handle))
            {
                return (int)Math.Round(graphics.DpiX);
            }
#else
            return (int)NativeMethods.GetDpiForWindow(this.Handle);
#endif
        }

#if !NETFRAMEWORK
        /// <inheritdoc />
        protected override void RescaleConstantsForDpi(int deviceDpiOld, int deviceDpiNew) {
            base.RescaleConstantsForDpi(deviceDpiOld, deviceDpiNew);
            if (this.IsHandleCreated)
            {
                this.RescaleColumnWidthsForDpi(deviceDpiNew);
            }
        }
#endif

        /// <summary>
        /// Called when the handle of the underlying control is created
        /// </summary>
        /// <param name="e"></param>
        protected override void OnHandleCreated(EventArgs e) {
            //Debug.WriteLine("OnHandleCreated");
            base.OnHandleCreated(e);
            NativeMethods.SetListViewColors(this);
            this.RescaleColumnWidthsForDpi(this.GetWindowDpi());

            this.Invoke((MethodInvoker)this.OnControlCreated);
        }

        /// <inheritdoc />
        protected override void OnEnabledChanged(EventArgs e) {
            base.OnEnabledChanged(e);
            if (this.IsHandleCreated)
            {
                NativeMethods.SetListViewColors(this);
                this.HeaderControl.Invalidate();
            }
        }

        /// <summary>
        /// This method is called after the control has been fully created.
        /// </summary>
        protected virtual void OnControlCreated() {

            //Debug.WriteLine("OnControlCreated");

            // Force the header control to be created when the listview handle is
            HeaderControl hc = this.HeaderControl;
            hc.WordWrap = this.HeaderWordWrap;

            // Make sure any overlays that are set on the hot item style take effect
#pragma warning disable CA2245 // Rebind the hot-item overlays to the recreated handle.
            this.HotItemStyle = this.HotItemStyle;
#pragma warning restore CA2245

            // Arrange for any group images to be installed after the control is created
            NativeMethods.SetGroupImageList(this, this.GroupImageList);

#pragma warning disable CA2245 // Reapply the native theme to the recreated handle.
            this.UseExplorerTheme = this.UseExplorerTheme;
#pragma warning restore CA2245

            this.RememberDisplayIndicies();
            this.SetGroupSpacing();

            if (this.VirtualMode)
            {
                this.ApplyExtendedStyles();
            }
        }

        #endregion

        //-----------------------------------------------------------------------------------

        #region Events

        /// <summary>
        /// Triggered after a ObjectListView has been searched by the user typing into the list
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered after the control has done a search-by-typing action.")]
        public event EventHandler<AfterSearchingEventArgs> AfterSearching;

        /// <summary>
        /// Triggered after a ObjectListView has been sorted
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered after the items in the list have been sorted.")]
        public event EventHandler<AfterSortingEventArgs> AfterSorting;

        /// <summary>
        /// Triggered before a ObjectListView is searched by the user typing into the list
        /// </summary>
        /// <remarks>
        /// Set Cancelled to true to prevent the searching from taking place.
        /// Changing StringToFind or StartSearchFrom will change the subsequent search.
        /// </remarks>
        [Category("ObjectListView"),
         Description("This event is triggered before the control does a search-by-typing action.")]
        public event EventHandler<BeforeSearchingEventArgs> BeforeSearching;

        /// <summary>
        /// Triggered before a ObjectListView is sorted
        /// </summary>
        /// <remarks>
        /// Set Cancelled to true to prevent the sort from taking place.
        /// Changing ColumnToSort or SortOrder will change the subsequent sort.
        /// </remarks>
        [Category("ObjectListView"),
         Description("This event is triggered before the items in the list are sorted.")]
        public event EventHandler<BeforeSortingEventArgs> BeforeSorting;

        /// <summary>
        /// Triggered after a ObjectListView has created groups
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered after the groups are created.")]
        public event EventHandler<CreateGroupsEventArgs> AfterCreatingGroups;

        /// <summary>
        /// Triggered before a ObjectListView begins to create groups
        /// </summary>
        /// <remarks>
        /// Set Groups to prevent the default group creation process
        /// </remarks>
        [Category("ObjectListView"),
         Description("This event is triggered before the groups are created.")]
        public event EventHandler<CreateGroupsEventArgs> BeforeCreatingGroups;

        /// <summary>
        /// Triggered just before a ObjectListView creates groups
        /// </summary>
        /// <remarks>
        /// You can make changes to the groups, which have been created, before those
        /// groups are created within the listview.
        /// </remarks>
        [Category("ObjectListView"),
         Description("This event is triggered when the groups are just about to be created.")]
        public event EventHandler<CreateGroupsEventArgs> AboutToCreateGroups;

        /// <summary>
        /// Triggered when a button in a cell is left clicked.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when the user left clicks a button.")]
        public event EventHandler<CellClickEventArgs> ButtonClick;

        /// <summary>
        /// This event is triggered when the user moves a drag over an ObjectListView that
        /// has a SimpleDropSink installed as the drop handler.
        /// </summary>
        /// <remarks>
        /// Handlers for this event should set the Effect argument and optionally the
        /// InfoMsg property. They can also change any of the DropTarget* settings to change
        /// the target of the drop.
        /// </remarks>
        [Category("ObjectListView"),
         Description("Can the user drop the currently dragged items at the current mouse location?")]
        public event EventHandler<OlvDropEventArgs> CanDrop;

        /// <summary>
        /// Triggered when a cell has finished being edited.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered cell edit operation has completely finished")]
        public event CellEditEventHandler CellEditFinished;

        /// <summary>
        /// Triggered when a cell is about to finish being edited.
        /// </summary>
        /// <remarks>If Cancel is already true, the user is cancelling the edit operation.
        /// Set Cancel to true to prevent the value from the cell being written into the model.
        /// You cannot prevent the editing from finishing within this event -- you need
        /// the CellEditValidating event for that.</remarks>
        [Category("ObjectListView"),
         Description("This event is triggered cell edit operation is finishing.")]
        public event CellEditEventHandler CellEditFinishing;

        /// <summary>
        /// Triggered when a cell is about to be edited.
        /// </summary>
        /// <remarks>Set Cancel to true to prevent the cell being edited.
        /// You can change the Control to be something completely different.</remarks>
        [Category("ObjectListView"),
         Description("This event is triggered when cell edit is about to begin.")]
        public event CellEditEventHandler CellEditStarting;

        /// <summary>
        /// Triggered when a cell editor needs to be validated
        /// </summary>
        /// <remarks>
        /// If this event is cancelled, focus will remain on the cell editor.
        /// </remarks>
        [Category("ObjectListView"),
         Description("This event is triggered when a cell editor is about to lose focus and its new contents need to be validated.")]
        public event CellEditEventHandler CellEditValidating;

        /// <summary>
        /// Triggered when a cell is left clicked.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when the user left clicks a cell.")]
        public event EventHandler<CellClickEventArgs> CellClick;

        /// <summary>
        /// Triggered when the mouse is above a cell.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when the mouse is over a cell.")]
        public event EventHandler<CellOverEventArgs> CellOver;

        /// <summary>
        /// Triggered when a cell is right clicked.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when the user right clicks a cell.")]
        public event EventHandler<CellRightClickEventArgs> CellRightClick;

        /// <summary>
        /// This event is triggered when a cell needs a tool tip.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when a cell needs a tool tip.")]
        public event EventHandler<ToolTipShowingEventArgs> CellToolTipShowing;

        /// <summary>
        /// This event is triggered when a checkbox is checked/unchecked on a subitem
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when a checkbox is checked/unchecked on a subitem.")]
        public event EventHandler<SubItemCheckingEventArgs> SubItemChecking;

        /// <summary>
        /// Triggered when a column header is right clicked.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when the user right clicks a column header.")]
        public event ColumnRightClickEventHandler ColumnRightClick;

        /// <summary>
        /// This event is triggered when the user releases a drag over an ObjectListView that
        /// has a SimpleDropSink installed as the drop handler.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when the user dropped items onto the control.")]
        public event EventHandler<OlvDropEventArgs> Dropped;

        /// <summary>
        /// This event is triggered when the control needs to filter its collection of objects.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when the control needs to filter its collection of objects.")]
        public event EventHandler<FilterEventArgs> Filter;

        /// <summary>
        /// This event is triggered when a cell needs to be formatted.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when a cell needs to be formatted.")]
        public event EventHandler<FormatCellEventArgs> FormatCell;

        /// <summary>
        /// This event is triggered when the frozenness of the control changes.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when frozenness of the control changes.")]
        public event EventHandler<FreezeEventArgs> Freezing;

        /// <summary>
        /// This event is triggered when a row needs to be formatted.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when a row needs to be formatted.")]
        public event EventHandler<FormatRowEventArgs> FormatRow;

        /// <summary>
        /// This event is triggered when a group is about to collapse or expand.
        /// This can be cancelled to prevent the expansion.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when a group is about to collapse or expand.")]
        public event EventHandler<GroupExpandingCollapsingEventArgs> GroupExpandingCollapsing;

        /// <summary>
        /// This event is triggered when a group changes state.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when a group changes state.")]
        public event EventHandler<GroupStateChangedEventArgs> GroupStateChanged;

        /// <summary>
        /// This event is triggered when a header checkbox is changing value
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when a header checkbox changes value.")]
        public event EventHandler<HeaderCheckBoxChangingEventArgs> HeaderCheckBoxChanging;

        /// <summary>
        /// This event is triggered when a header needs a tool tip.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when a header needs a tool tip.")]
        public event EventHandler<ToolTipShowingEventArgs> HeaderToolTipShowing;

        /// <summary>
        /// Triggered when the "hot" item changes
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when the hot item changed.")]
        public event EventHandler<HotItemChangedEventArgs> HotItemChanged;

        /// <summary>
        /// Triggered when a hyperlink cell is clicked.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when a hyperlink cell is clicked.")]
        public event EventHandler<HyperlinkClickedEventArgs> HyperlinkClicked;

        /// <summary>
        /// Triggered when the task text of a group is clicked.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when the task text of a group is clicked.")]
        public event EventHandler<GroupTaskClickedEventArgs> GroupTaskClicked;

        /// <summary>
        /// Is the value in the given cell a hyperlink.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when the control needs to know if a given cell contains a hyperlink.")]
        public event EventHandler<IsHyperlinkEventArgs> IsHyperlink;

        /// <summary>
        /// Some new objects are about to be added to an ObjectListView.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when objects are about to be added to the control")]
        public event EventHandler<ItemsAddingEventArgs> ItemsAdding;

        /// <summary>
        /// The contents of the ObjectListView has changed.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when the contents of the control have changed.")]
        public event EventHandler<ItemsChangedEventArgs> ItemsChanged;

        /// <summary>
        /// The contents of the ObjectListView is about to change via a SetObjects call
        /// </summary>
        /// <remarks>
        /// <para>Set Cancelled to true to prevent the contents of the list changing. This does not work with virtual lists.</para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("This event is triggered when the contents of the control changes.")]
        public event EventHandler<ItemsChangingEventArgs> ItemsChanging;

        /// <summary>
        /// Some objects are about to be removed from an ObjectListView.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when objects are removed from the control.")]
        public event EventHandler<ItemsRemovingEventArgs> ItemsRemoving;

        /// <summary>
        /// This event is triggered when the user moves a drag over an ObjectListView that
        /// has a SimpleDropSink installed as the drop handler, and when the source control
        /// for the drag was an ObjectListView.
        /// </summary>
        /// <remarks>
        /// Handlers for this event should set the Effect argument and optionally the
        /// InfoMsg property. They can also change any of the DropTarget* settings to change
        /// the target of the drop.
        /// </remarks>
        [Category("ObjectListView"),
         Description("Can the dragged collection of model objects be dropped at the current mouse location")]
        public event EventHandler<ModelDropEventArgs> ModelCanDrop;

        /// <summary>
        /// This event is triggered when the user releases a drag over an ObjectListView that
        /// has a SimpleDropSink installed as the drop handler and when the source control
        /// for the drag was an ObjectListView.
        /// </summary>
        [Category("ObjectListView"),
         Description("A collection of model objects from a ObjectListView has been dropped on this control")]
        public event EventHandler<ModelDropEventArgs> ModelDropped;

        /// <summary>
        /// This event is triggered once per user action that changes the selection state
        /// of one or more rows.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered once per user action that changes the selection state of one or more rows.")]
        public event EventHandler SelectionChanged;

        /// <summary>
        /// This event is triggered when the contents of the ObjectListView has scrolled.
        /// </summary>
        [Category("ObjectListView"),
         Description("This event is triggered when the contents of the ObjectListView has scrolled.")]
        public event EventHandler<ScrollEventArgs> Scroll;

        #endregion

        //-----------------------------------------------------------------------------------

        #region OnEvents

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnAboutToCreateGroups(CreateGroupsEventArgs e) {
            if (this.AboutToCreateGroups != null)
            {
                this.AboutToCreateGroups(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnBeforeCreatingGroups(CreateGroupsEventArgs e) {
            if (this.BeforeCreatingGroups != null)
            {
                this.BeforeCreatingGroups(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnAfterCreatingGroups(CreateGroupsEventArgs e) {
            if (this.AfterCreatingGroups != null)
            {
                this.AfterCreatingGroups(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnAfterSearching(AfterSearchingEventArgs e) {
            if (this.AfterSearching != null)
            {
                this.AfterSearching(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnAfterSorting(AfterSortingEventArgs e) {
            this.InvalidateModelRows();
            if (this.AfterSorting != null)
            {
                this.AfterSorting(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnBeforeSearching(BeforeSearchingEventArgs e) {
            if (this.BeforeSearching != null)
            {
                this.BeforeSearching(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnBeforeSorting(BeforeSortingEventArgs e) {
            if (this.BeforeSorting != null)
            {
                this.BeforeSorting(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnButtonClick(CellClickEventArgs args) {
            if (this.ButtonClick != null)
            {
                this.ButtonClick(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCanDrop(OlvDropEventArgs args) {
            if (this.CanDrop != null)
            {
                this.CanDrop(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCellClick(CellClickEventArgs args) {
            if (this.CellClick != null)
            {
                this.CellClick(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCellOver(CellOverEventArgs args) {
            if (this.CellOver != null)
            {
                this.CellOver(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCellRightClick(CellRightClickEventArgs args) {
            if (this.CellRightClick != null)
            {
                this.CellRightClick(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCellToolTip(ToolTipShowingEventArgs args) {
            if (this.CellToolTipShowing != null)
            {
                this.CellToolTipShowing(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnSubItemChecking(SubItemCheckingEventArgs args) {
            if (this.SubItemChecking != null)
            {
                this.SubItemChecking(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnColumnRightClick(ColumnRightClickEventArgs e) {
            if (this.ColumnRightClick != null)
            {
                this.ColumnRightClick(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnDropped(OlvDropEventArgs args) {
            if (this.Dropped != null)
            {
                this.Dropped(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        internal protected virtual void OnFilter(FilterEventArgs e) {
            if (this.Filter != null)
            {
                this.Filter(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnFormatCell(FormatCellEventArgs args) {
            if (this.FormatCell != null)
            {
                this.FormatCell(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnFormatRow(FormatRowEventArgs args) {
            if (this.FormatRow != null)
            {
                this.FormatRow(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnFreezing(FreezeEventArgs args) {
            if (this.Freezing != null)
            {
                this.Freezing(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnGroupExpandingCollapsing(GroupExpandingCollapsingEventArgs args) {
            if (this.GroupExpandingCollapsing != null)
            {
                this.GroupExpandingCollapsing(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnGroupStateChanged(GroupStateChangedEventArgs args) {
            if (this.GroupStateChanged != null)
            {
                this.GroupStateChanged(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnHeaderCheckBoxChanging(HeaderCheckBoxChangingEventArgs args) {
            if (this.HeaderCheckBoxChanging != null)
            {
                this.HeaderCheckBoxChanging(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnHeaderToolTip(ToolTipShowingEventArgs args) {
            if (this.HeaderToolTipShowing != null)
            {
                this.HeaderToolTipShowing(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnHotItemChanged(HotItemChangedEventArgs e) {
            if (this.HotItemChanged != null)
            {
                this.HotItemChanged(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnHyperlinkClicked(HyperlinkClickedEventArgs e) {
            if (this.HyperlinkClicked != null)
            {
                this.HyperlinkClicked(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnGroupTaskClicked(GroupTaskClickedEventArgs e) {
            if (this.GroupTaskClicked != null)
            {
                this.GroupTaskClicked(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnIsHyperlink(IsHyperlinkEventArgs e) {
            if (this.IsHyperlink != null)
            {
                this.IsHyperlink(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnItemsAdding(ItemsAddingEventArgs e) {
            if (this.ItemsAdding != null)
            {
                this.ItemsAdding(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnItemsChanged(ItemsChangedEventArgs e) {
            this.InvalidateModelRows();
            if (this.ItemsChanged != null)
            {
                this.ItemsChanged(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnItemsChanging(ItemsChangingEventArgs e) {
            if (this.ItemsChanging != null)
            {
                this.ItemsChanging(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnItemsRemoving(ItemsRemovingEventArgs e) {
            if (this.ItemsRemoving != null)
            {
                this.ItemsRemoving(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnModelCanDrop(ModelDropEventArgs args) {
            if (this.ModelCanDrop != null)
            {
                this.ModelCanDrop(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnModelDropped(ModelDropEventArgs args) {
            if (this.ModelDropped != null)
            {
                this.ModelDropped(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnSelectionChanged(EventArgs e) {
            if (this.SelectionChanged != null)
            {
                this.SelectionChanged(this, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnScroll(ScrollEventArgs e) {
            if (this.Scroll != null)
            {
                this.Scroll(this, e);
            }
        }


        /// <summary>
        /// Tell the world when a cell is about to be edited.
        /// </summary>
        protected virtual void OnCellEditStarting(CellEditEventArgs e) {
            if (this.CellEditStarting != null)
            {
                this.CellEditStarting(this, e);
            }
        }

        /// <summary>
        /// Tell the world when a cell is about to finish being edited.
        /// </summary>
        protected virtual void OnCellEditorValidating(CellEditEventArgs e) {
            // Hack. ListView is an imperfect control container. It does not manage validation
            // perfectly. If the ListView is part of a TabControl, and the cell editor loses
            // focus by the user clicking on another tab, the TabControl processes the click
            // and switches tabs, even if this Validating event cancels. This results in the
            // strange situation where the cell editor is active, but isn't visible. When the
            // user switches back to the tab with the ListView, composite controls like spin
            // controls, DateTimePicker and ComboBoxes do not work properly. Specifically,
            // keyboard input still works fine, but the controls do not respond to mouse
            // input. SO, if the validation fails, we have to specifically give focus back to
            // the cell editor. (this is the Select() call in the code below). 
            // But (there is always a 'but'), doing that changes the focus so the cell editor
            // triggers another Validating event -- which fails again. From the user's point
            // of view, they click away from the cell editor, and the validating code
            // complains twice. So we only trigger a Validating event if more than 0.1 seconds
            // has elapsed since the last validate event.
            // I know it's a hack. I'm very open to hear a neater solution.

            // Also, this timed response stops us from sending a series of validation events
            // if the user clicks and holds on the OLV scroll bar.
            //System.Diagnostics.Debug.WriteLine(Environment.TickCount - lastValidatingEvent);
            if ((Environment.TickCount - lastValidatingEvent) < 100) {
                e.Cancel = true;
            } else {
                lastValidatingEvent = Environment.TickCount;
                if (this.CellEditValidating != null)
                {
                    this.CellEditValidating(this, e);
                }
            }

            lastValidatingEvent = Environment.TickCount;
        }

        private int lastValidatingEvent;

        /// <summary>
        /// Tell the world when a cell is about to finish being edited.
        /// </summary>
        protected virtual void OnCellEditFinishing(CellEditEventArgs e) {
            if (this.CellEditFinishing != null)
            {
                this.CellEditFinishing(this, e);
            }
        }

        /// <summary>
        /// Tell the world when a cell has finished being edited.
        /// </summary>
        protected virtual void OnCellEditFinished(CellEditEventArgs e) {
            if (this.CellEditFinished != null)
            {
                this.CellEditFinished(this, e);
            }
        }

        #endregion
    }
}
