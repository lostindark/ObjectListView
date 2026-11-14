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
        #region Hot row and cell handling

        /// <summary>
        /// Force the hot item to be recalculated
        /// </summary>
        public virtual void ClearHotItem() {
            this.UpdateHotItem(new Point(-1, -1));
        }

        /// <summary>
        /// Force the hot item to be recalculated
        /// </summary>
        public virtual void RefreshHotItem() {
            this.UpdateHotItem(this.PointToClient(Cursor.Position));
        }

        /// <summary>
        /// The mouse has moved to the given pt. See if the hot item needs to be updated
        /// </summary>
        /// <param name="pt">Where is the mouse?</param>
        /// <remarks>This is the main entry point for hot item handling</remarks>
        protected virtual void UpdateHotItem(Point pt) {
            this.UpdateHotItem(this.OlvHitTest(pt.X, pt.Y));
        }

        /// <summary>
        /// The mouse has moved to the given pt. See if the hot item needs to be updated
        /// </summary>
        /// <param name="hti"></param>
        /// <remarks>This is the main entry point for hot item handling</remarks>
        protected virtual void UpdateHotItem(OlvListViewHitTestInfo hti) {

            // We only need to do the work of this method when the list has hot parts 
            // (i.e. some element whose visual appearance changes when under the mouse)?
            // Hot item decorations and hyperlinks are obvious, but if we have checkboxes
            // or buttons, those are also "hot". It's difficult to quickly detect if there are any
            // columns that have checkboxes or buttons, so we just abdicate responsibility and 
            // provide a property (UseHotControls) which lets the programmer say whether to do
            // the hot processing or not.
            if (!this.UseHotItem && !this.UseHyperlinks && !this.UseHotControls)
            {
                return;
            }

            int newHotRow = hti.RowIndex;
            int newHotColumn = hti.ColumnIndex;
            HitTestLocation newHotCellHitLocation = hti.HitTestLocation;
            HitTestLocationEx newHotCellHitLocationEx = hti.HitTestLocationEx;
            OLVGroup newHotGroup = hti.Group;

            // In non-details view, we treat any hit on a row as if it were a hit
            // on column 0 -- which (effectively) it is!
            if (newHotRow >= 0 && this.View != View.Details)
            {
                newHotColumn = 0;
            }

            if (this.HotRowIndex == newHotRow &&
                this.HotColumnIndex == newHotColumn &&
                this.HotCellHitLocation == newHotCellHitLocation &&
                this.HotCellHitLocationEx == newHotCellHitLocationEx &&
                this.HotGroup == newHotGroup) {
                return;
            }

            // Trigger the hotitem changed event
            HotItemChangedEventArgs args = new HotItemChangedEventArgs();
            args.HotCellHitLocation = newHotCellHitLocation;
            args.HotCellHitLocationEx = newHotCellHitLocationEx;
            args.HotColumnIndex = newHotColumn;
            args.HotRowIndex = newHotRow;
            args.HotGroup = newHotGroup;
            args.OldHotCellHitLocation = this.HotCellHitLocation;
            args.OldHotCellHitLocationEx = this.HotCellHitLocationEx;
            args.OldHotColumnIndex = this.HotColumnIndex;
            args.OldHotRowIndex = this.HotRowIndex;
            args.OldHotGroup = this.HotGroup;
            this.OnHotItemChanged(args);

            // Update the state of the control
            this.HotRowIndex = newHotRow;
            this.HotColumnIndex = newHotColumn;
            this.HotCellHitLocation = newHotCellHitLocation;
            this.HotCellHitLocationEx = newHotCellHitLocationEx;
            this.HotGroup = newHotGroup;

            // If the event handler handled it complete, don't do anything else
            if (args.Handled)
            {
                return;
            }

            //            System.Diagnostics.Debug.WriteLine(String.Format("Changed hot item: {0}", args));

            this.BeginUpdate();
            try {
                this.Invalidate();
                if (args.OldHotRowIndex != -1)
                {
                    this.UnapplyHotItem(args.OldHotRowIndex);
                }

                if (this.HotRowIndex != -1) {
                    // Virtual lists apply hot item style when fetching their rows
                    if (this.VirtualMode) {
                        this.ClearCachedInfo();
                        this.RedrawItems(this.HotRowIndex, this.HotRowIndex, true);
                    } else {
                        this.UpdateHotRow(this.HotRowIndex, this.HotColumnIndex, this.HotCellHitLocation, hti.Item);
                    }
                }

                if (this.UseHotItem && this.HotItemStyleOrDefault.Overlay != null) {
                    this.RefreshOverlays();
                }
            }
            finally {
                this.EndUpdate();
            }
        }

        /// <summary>
        /// Update the given row using the current hot item information
        /// </summary>
        /// <param name="olvi"></param>
        protected virtual void UpdateHotRow(OLVListItem olvi) {
            this.UpdateHotRow(this.HotRowIndex, this.HotColumnIndex, this.HotCellHitLocation, olvi);
        }

        /// <summary>
        /// Update the given row using the given hot item information
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="columnIndex"></param>
        /// <param name="hitLocation"></param>
        /// <param name="olvi"></param>
        protected virtual void UpdateHotRow(int rowIndex, int columnIndex, HitTestLocation hitLocation, OLVListItem olvi) {
            if (rowIndex < 0 || columnIndex < 0)
            {
                return;
            }

            // System.Diagnostics.Debug.WriteLine(String.Format("UpdateHotRow: {0}, {1}, {2}", rowIndex, columnIndex, hitLocation));

            if (this.UseHyperlinks) {
                OLVColumn column = this.GetColumn(columnIndex);
                OLVListSubItem subItem = olvi.GetSubItem(columnIndex);
                if (column != null && column.Hyperlink && hitLocation == HitTestLocation.Text && !String.IsNullOrEmpty(subItem.Url)) {
                    this.ApplyCellStyle(olvi, columnIndex, this.HyperlinkStyle.Over);
                    this.Cursor = this.HyperlinkStyle.OverCursor ?? Cursors.Default;
                } else {
                    this.Cursor = Cursors.Default;
                }
            }

            if (this.UseHotItem) {
                if (!olvi.Selected && olvi.Enabled) {
                    this.ApplyRowStyle(olvi, this.HotItemStyleOrDefault);
                }
            }
        }

        /// <summary>
        /// Apply a style to the given row
        /// </summary>
        /// <param name="olvi"></param>
        /// <param name="style"></param>
        public virtual void ApplyRowStyle(OLVListItem olvi, IItemStyle style) {
            if (style == null)
            {
                return;
            }

            Font font = style.Font ?? olvi.Font;

            if (HasItemFontStyle(style))
            {
                font = this.ResolveStyleFont(font, style.FontStyle);
            }

            if (!Equals(font, olvi.Font)) {
                if (olvi.UseItemStyleForSubItems)
                {
                    olvi.Font = font;
                }
                else {
                    foreach (ListViewItem.ListViewSubItem x in olvi.SubItems)
                    {
                        x.Font = font;
                    }
                }
            }

            if (!style.ForeColor.IsEmpty) {
                if (olvi.UseItemStyleForSubItems)
                {
                    olvi.ForeColor = style.ForeColor;
                }
                else {
                    foreach (ListViewItem.ListViewSubItem x in olvi.SubItems)
                    {
                        x.ForeColor = style.ForeColor;
                    }
                }
            }

            if (!style.BackColor.IsEmpty) {
                if (olvi.UseItemStyleForSubItems)
                {
                    olvi.BackColor = style.BackColor;
                }
                else {
                    foreach (ListViewItem.ListViewSubItem x in olvi.SubItems)
                    {
                        x.BackColor = style.BackColor;
                    }
                }
            }
        }

        /// <summary>
        /// Apply a style to a cell
        /// </summary>
        /// <param name="olvi"></param>
        /// <param name="columnIndex"></param>
        /// <param name="style"></param>
        protected virtual void ApplyCellStyle(OLVListItem olvi, int columnIndex, IItemStyle style) {
            if (style == null)
            {
                return;
            }

            // Don't apply formatting to subitems when not in Details view
            if (this.View != View.Details && columnIndex > 0)
            {
                return;
            }

            olvi.UseItemStyleForSubItems = false;

            ListViewItem.ListViewSubItem subItem = olvi.SubItems[columnIndex];
            if (style.Font != null)
            {
                subItem.Font = style.Font;
            }

            if (HasItemFontStyle(style))
            {
                subItem.Font = this.ResolveStyleFont(subItem.Font ?? olvi.Font, style.FontStyle);
            }

            if (!style.ForeColor.IsEmpty)
            {
                subItem.ForeColor = style.ForeColor;
            }

            if (!style.BackColor.IsEmpty)
            {
                subItem.BackColor = style.BackColor;
            }
        }

        /// <summary>
        /// Remove hot item styling from the given row
        /// </summary>
        /// <param name="index"></param>
        protected virtual void UnapplyHotItem(int index) {
            this.Cursor = Cursors.Default;
            // Virtual lists will apply the appropriate formatting when the row is fetched
            if (this.VirtualMode) {
                if (index < this.VirtualListSize)
                {
                    this.RedrawItems(index, index, true);
                }
            } else {
                OLVListItem olvi = this.GetItem(index);
                if (olvi != null) {
                    //this.PostProcessOneRow(index, index, olvi);
                    this.RefreshItem(olvi);
                }
            }
        }


        #endregion

        #region Drag and drop

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnItemDrag(ItemDragEventArgs e) {
            base.OnItemDrag(e);

            if (this.DragSource == null)
            {
                return;
            }

            Object data = this.DragSource.StartDrag(this, e.Button, (OLVListItem)e.Item);
            if (data != null) {
                DragDropEffects effect = this.DoDragDrop(data, this.DragSource.GetAllowedEffects(data));
                this.DragSource.EndDrag(data, effect);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="drgevent"></param>
        protected override void OnDragEnter(DragEventArgs drgevent) {
            base.OnDragEnter(drgevent);

            if (this.DropSink != null)
            {
                this.DropSink.Enter(drgevent);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="drgevent"></param>
        protected override void OnDragOver(DragEventArgs drgevent) {
            base.OnDragOver(drgevent);

            if (this.DropSink != null)
            {
                this.DropSink.Over(drgevent);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="drgevent"></param>
        protected override void OnDragDrop(DragEventArgs drgevent) {
            base.OnDragDrop(drgevent);

            this.lastMouseDownClickCount = 0;  // prevent drop events from becoming cell edits

            if (this.DropSink != null)
            {
                this.DropSink.Drop(drgevent);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDragLeave(EventArgs e) {
            base.OnDragLeave(e);

            if (this.DropSink != null)
            {
                this.DropSink.Leave();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gfbevent"></param>
        protected override void OnGiveFeedback(GiveFeedbackEventArgs gfbevent) {
            base.OnGiveFeedback(gfbevent);

            if (this.DropSink != null)
            {
                this.DropSink.GiveFeedback(gfbevent);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qcdevent"></param>
        protected override void OnQueryContinueDrag(QueryContinueDragEventArgs qcdevent) {
            base.OnQueryContinueDrag(qcdevent);

            if (this.DropSink != null)
            {
                this.DropSink.QueryContinue(qcdevent);
            }
        }

        #endregion
    }
}
