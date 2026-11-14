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
        #region Low level Windows Message handling

        private bool paintingScrollBars;
        private GlassPanelForm scrollBarGlass;
        private bool protectingStateImages;

        /// <summary>
        /// Override the basic message pump for this control
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            // Native checkbox/theme refreshes can destroy even a managed state image list.
            if (!this.protectingStateImages && this.IsHandleCreated && this.StateImageList != null &&
                (m.Msg == 0x031A || m.Msg == 0x1036)) { // WM_THEMECHANGED, LVM_SETEXTENDEDLISTVIEWSTYLE
                this.protectingStateImages = true;
                try {
                    NativeMethods.SetStateImageList(this, null);
                    base.WndProc(ref m);
                } finally {
                    this.protectingStateImages = false;
                    if (this.IsHandleCreated)
                        NativeMethods.SetStateImageList(this, this.StateImageList);
                }
                return;
            }
            // System.Diagnostics.Debug.WriteLine(m.Msg);
            switch (m.Msg) {
                case 2: // WM_DESTROY
                    this.scrollBarGlass?.HideGlass();
                    if (this.StateImageList != null && this.IsHandleCreated)
                    {
                        // Keep managed checkbox images alive when the native themed handle is recreated.
                        NativeMethods.SetStateImageList(this, null);
                    }
                    if (!this.HandleDestroy(ref m))
                    {
                        base.WndProc(ref m);
                    }

                    break;
                //case 0x14: // WM_ERASEBKGND
                //    Can't do anything here since, when the control is double buffered, anything
                //    done here is immediately over-drawn
                //    break;
                case 0x0F: // WM_PAINT
                    if (!this.HandlePaint(ref m))
                    {
                        base.WndProc(ref m);
                    }

                    break;
                case 0x46: // WM_WINDOWPOSCHANGING
                    if (this.PossibleFinishCellEditing() && !this.HandleWindowPosChanging(ref m))
                    {
                        base.WndProc(ref m);
                    }

                    break;
                case 0x4E: // WM_NOTIFY
                    if (!this.HandleNotify(ref m))
                    {
                        base.WndProc(ref m);
                    }

                    break;
                case 0x0100: // WM_KEY_DOWN
                    if (!this.HandleKeyDown(ref m))
                    {
                        base.WndProc(ref m);
                    }

                    break;
                case 0x0102: // WM_CHAR
                    if (!this.HandleChar(ref m))
                    {
                        base.WndProc(ref m);
                    }

                    break;
                case 0x0200: // WM_MOUSEMOVE
                    if (!this.HandleMouseMove(ref m))
                    {
                        base.WndProc(ref m);
                    }

                    break;
                case 0x0201: // WM_LBUTTONDOWN
                    // System.Diagnostics.Debug.WriteLine("WM_LBUTTONDOWN");
                    if (this.PossibleFinishCellEditing() && !this.HandleLButtonDown(ref m))
                    {
                        base.WndProc(ref m);
                    }

                    break;
                case 0x202:  // WM_LBUTTONUP
                    // System.Diagnostics.Debug.WriteLine("WM_LBUTTONUP");
                    if (this.PossibleFinishCellEditing() && !this.HandleLButtonUp(ref m))
                    {
                        base.WndProc(ref m);
                    }

                    break;
                case 0x0203: // WM_LBUTTONDBLCLK
                    if (this.PossibleFinishCellEditing() && !this.HandleLButtonDoubleClick(ref m))
                    {
                        base.WndProc(ref m);
                    }

                    break;
                case 0x0204: // WM_RBUTTONDOWN
                    // System.Diagnostics.Debug.WriteLine("WM_RBUTTONDOWN");
                    if (this.PossibleFinishCellEditing() && !this.HandleRButtonDown(ref m))
                    {
                        base.WndProc(ref m);
                    }

                    break;
                case 0x0205: // WM_RBUTTONUP
                    // System.Diagnostics.Debug.WriteLine("WM_RBUTTONUP");
                    base.WndProc(ref m);
                    break;
                case 0x0206: // WM_RBUTTONDBLCLK
                    if (this.PossibleFinishCellEditing() && !this.HandleRButtonDoubleClick(ref m))
                    {
                        base.WndProc(ref m);
                    }

                    break;
                case 0x204E: // WM_REFLECT_NOTIFY
                    if (!this.HandleReflectNotify(ref m))
                    {
                        base.WndProc(ref m);
                    }

                    break;
                case 0x114: // WM_HSCROLL:
                case 0x115: // WM_VSCROLL:
                    //System.Diagnostics.Debug.WriteLine("WM_VSCROLL");
                    if (this.PossibleFinishCellEditing())
                    {
                        base.WndProc(ref m);
                    }

                    break;
                case 0x20A: // WM_MOUSEWHEEL:
                case 0x20E: // WM_MOUSEHWHEEL:
                    if (this.AllowCellEditorsToProcessMouseWheel && this.IsCellEditing)
                    {
                        break;
                    }

                    if (this.PossibleFinishCellEditing())
                    {
                        base.WndProc(ref m);
                    }

                    break;
                case 0x7B: // WM_CONTEXTMENU
                    if (!this.HandleContextMenu(ref m))
                    {
                        base.WndProc(ref m);
                    }

                    break;
                case 0x1000 + 18: // LVM_HITTEST:
                    //System.Diagnostics.Debug.WriteLine("LVM_HITTEST");
                    if (this.skipNextHitTest) {
                        //System.Diagnostics.Debug.WriteLine("SKIPPING LVM_HITTEST");
                        this.skipNextHitTest = false;
                    } else {
                        base.WndProc(ref m);
                    }
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }

            if (!this.paintingScrollBars && this.IsHandleCreated
                && NativeListViewTheme.IsPaintMessage(m.Msg)
                && (m.Msg != 0x317 || (m.LParam.ToInt64() & 2) != 0))
            {
                bool paintEmptyClient = ObjectListViewTheme.IsDarkMode && !this.Enabled && this.GetItemCount() == 0;
                bool paintScrollBars = NativeListViewTheme.ShouldPaint;
                this.paintingScrollBars = true;
                try
                {
                    if (m.Msg == 0x317)
                    {
                        _ = NativeListViewTheme.Paint(this, m.WParam, paintScrollBars, paintEmptyClient);
                    }
                    else
                    {
                        if (paintEmptyClient && (m.Msg == 0x0F || m.Msg == 0x0A))
                        {
                            _ = NativeListViewTheme.Paint(this, IntPtr.Zero, false, true);
                        }
                        this.UpdateScrollBarGlass(paintScrollBars);
                    }
                }
                finally
                {
                    this.paintingScrollBars = false;
                }
            }
        }

        private void UpdateScrollBarGlass(bool enabled) {
            Form owner = this.FindForm();
            if (!enabled || this.DesignMode || this.Disposing || this.IsDisposed || !this.Visible
                || owner == null || !owner.Visible || owner.WindowState == FormWindowState.Minimized)
            {
                this.scrollBarGlass?.HideGlass();
                return;
            }
            if (this.scrollBarGlass == null)
            {
                this.scrollBarGlass = new GlassPanelForm(true);
                this.scrollBarGlass.Bind(this, NativeListViewTheme.CreateOverlay());
            }
            if (owner.Opacity <= 0)
            {
                this.scrollBarGlass.HideGlass();
                return;
            }
            this.scrollBarGlass.ShowGlass();
            this.scrollBarGlass.Invalidate();
            this.scrollBarGlass.Update();
        }

        internal void DrawEmptyListBackground(Graphics graphics) {
            using (Brush background = new SolidBrush(this.BackColor))
            {
                graphics.FillRectangle(background, this.ContentRectangle);
            }
            this.DrawAllDecorations(graphics, new List<OLVListItem>());
        }

        /// <summary>
        /// Handle the search for item m if possible.
        /// </summary>
        /// <param name="m">The m to be processed</param>
        /// <returns>bool to indicate if the msg has been handled</returns>
        protected virtual bool HandleChar(ref Message m) {

            // Trigger a normal KeyPress event, which listeners can handle if they want.
            // Handling the event stops ObjectListView's fancy search-by-typing.
            if (this.ProcessKeyEventArgs(ref m))
            {
                return true;
            }

            const int MILLISECONDS_BETWEEN_KEYPRESSES = 1000;

            // What character did the user type and was it part of a longer string?
            char character = (char)m.WParam.ToInt32(); //TODO: Will this work on 64 bit or MBCS?
            if (character == (char)Keys.Back) {
                // Backspace forces the next key to be considered the start of a new search
                this.timeLastCharEvent = 0;
                return true;
            }

            if (System.Environment.TickCount < (this.timeLastCharEvent + MILLISECONDS_BETWEEN_KEYPRESSES))
            {
                this.lastSearchString += character;
            }
            else
            {
                this.lastSearchString = character.ToString(CultureInfo.InvariantCulture);
            }

            // If this control is showing checkboxes, we want to ignore single space presses,
            // since they are used to toggle the selected checkboxes.
            if (this.CheckBoxes && this.lastSearchString == " ") {
                this.timeLastCharEvent = 0;
                return true;
            }

            // Where should the search start?
            int start = 0;
            ListViewItem focused = this.FocusedItem;
            if (focused != null) {
                start = this.GetDisplayOrderOfItemIndex(focused.Index);

                // If the user presses a single key, we search from after the focused item,
                // being careful not to march past the end of the list
                if (this.lastSearchString.Length == 1) {
                    start += 1;
                    if (start == this.GetItemCount())
                    {
                        start = 0;
                    }
                }
            }

            // Give the world a chance to fiddle with or completely avoid the searching process
            BeforeSearchingEventArgs args = new BeforeSearchingEventArgs(this.lastSearchString, start);
            this.OnBeforeSearching(args);
            if (args.Canceled)
            {
                return true;
            }

            // The parameters of the search may have been changed
            string searchString = args.StringToFind;
            start = args.StartSearchFrom;

            // Do the actual search
            int found = this.FindMatchingRow(searchString, start, SearchDirectionHint.Down);
            if (found >= 0) {
                // Select and focus on the found item
                this.BeginUpdate();
                try {
                    this.SelectedIndices.Clear();
                    OLVListItem lvi = this.GetNthItemInDisplayOrder(found);
                    if (lvi != null) {
                        if (lvi.Enabled)
                        {
                            lvi.Selected = true;
                        }

                        lvi.Focused = true;
                        this.EnsureVisible(lvi.Index);
                    }
                } finally {
                    this.EndUpdate();
                }
            }

            // Tell the world that a search has occurred
            AfterSearchingEventArgs args2 = new AfterSearchingEventArgs(searchString, found);
            this.OnAfterSearching(args2);
            if (!args2.Handled) {
                if (found < 0)
                {
                    System.Media.SystemSounds.Beep.Play();
                }
            }

            // When did this event occur?
            this.timeLastCharEvent = System.Environment.TickCount;
            return true;
        }
        private int timeLastCharEvent;
        private string lastSearchString;

        /// <summary>
        /// The user wants to see the context menu.
        /// </summary>
        /// <param name="m">The windows m</param>
        /// <returns>A bool indicating if this m has been handled</returns>
        /// <remarks>
        /// We want to ignore context menu requests that are triggered by right clicks on the header
        /// </remarks>
        protected virtual bool HandleContextMenu(ref Message m) {
            // Don't try to handle context menu commands at design time.
            if (this.DesignMode)
            {
                return false;
            }

            // If the context menu command was generated by the keyboard, LParam will be -1.
            // We don't want to process these.
            if (m.LParam == this.minusOne)
            {
                return false;
            }

            // If the context menu came from somewhere other than the header control,
            // we also don't want to ignore it
            if (m.WParam != this.HeaderControl.Handle)
            {
                return false;
            }

            // OK. Looks like a right click in the header
            if (!this.PossibleFinishCellEditing())
            {
                return true;
            }

            int columnIndex = this.HeaderControl.ColumnIndexUnderCursor;
            return this.HandleHeaderRightClick(columnIndex);
        }
        readonly IntPtr minusOne = new IntPtr(-1);

        private void DrawDarkDisabledGridLines(Graphics graphics) {
            if (!ObjectListViewTheme.IsDarkMode || this.Enabled || !this.GridLines
                || this.View != View.Details || this.ShowGroups || this.GetItemCount() == 0)
            {
                return;
            }

            OLVListItem first = this.GetItem(this.TopItemIndex);
            if (first == null || first.Bounds.Height <= 0)
            {
                return;
            }

            Rectangle row = first.Bounds;
            var state = graphics.Save();
            try {
                if (this.RightToLeftLayout && this.RightToLeft == RightToLeft.Yes)
                {
                    // GDI+ does not inherit the native DC's mirrored layout.
                    graphics.TranslateTransform(this.ClientSize.Width - 1, 0);
                    graphics.ScaleTransform(-1, 1);
                }
                using (Pen pen = new Pen(ObjectListViewTheme.Current.ControlDarkColor)) {
                    for (int y = row.Bottom - 1; y < this.ClientSize.Height; y += row.Height)
                    {
                        graphics.DrawLine(pen, 0, y, this.ClientSize.Width - 1, y);
                    }
                    foreach (OLVColumn column in this.Columns) {
                        Point sides = NativeMethods.GetScrolledColumnSides(this, column.Index);
                        graphics.DrawLine(pen, sides.Y, row.Top, sides.Y, this.ClientSize.Height - 1);
                    }
                }
            } finally {
                graphics.Restore(state);
            }
        }

        private void DrawDarkGroupHeader(NativeMethods.NMLVCUSTOMDRAW draw) {
            foreach (OLVGroup group in this.OLVGroups) {
                if (group.GroupId != draw.nmcd.dwItemSpec.ToInt64())
                {
                    continue;
                }

                if (!NativeMethods.GetGroupLabelRectangle(this, group.GroupId, out Rectangle bounds)) {
                    Trace.TraceWarning("ObjectListView could not get the caption bounds for group {0}.", group.GroupId);
                    return;
                }

                if (bounds.Width > 0 && bounds.Height > 0) {
                    TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding
                        | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter
                        | TextFormatFlags.EndEllipsis | TextFormatFlags.PreserveGraphicsClipping;
                    if (this.RightToLeft == RightToLeft.Yes)
                    {
                        flags |= TextFormatFlags.RightToLeft | TextFormatFlags.Right;
                    }

                    // The native theme ignores group foreground colors. Repaint only the label,
                    // preserving native padding, collapse controls, focus cues, and hit testing.
                    using (Graphics graphics = Graphics.FromHdc(draw.nmcd.hdc))
                    using (Font font = Font.FromHdc(draw.nmcd.hdc))
                    using (Pen separator = new Pen(ObjectListViewTheme.Current.GroupHeaderSeparatorColor)) {
                        bool explicitForeground = TypeDescriptor.GetProperties(this)[nameof(this.ForeColor)].ShouldSerializeValue(this);
                        Color foreground = ObjectListViewTheme.IsDarkMode && !this.Enabled
                            ? ObjectListViewTheme.Current.GrayTextColor
                            : explicitForeground ? this.ForeColor : ObjectListViewTheme.Current.GroupHeaderTextColor;
                        Rectangle caption = bounds;
                        if (string.IsNullOrEmpty(group.Task)) {
                            int affordance = group.Collapsible ? draw.rcText.bottom - draw.rcText.top : 0;
                            caption.Width = Math.Max(caption.Width, draw.rcText.right - affordance - caption.Left);
                        }
                        using (Brush background = new SolidBrush(this.BackColor))
                        {
                            graphics.FillRectangle(background, caption);
                        }
                        if (string.IsNullOrEmpty(group.Task))
                        {
                            int gap = (int)Math.Ceiling(4 * graphics.DpiX / 96f);
                            int start = bounds.Right + gap;
                            int end = caption.Right - 1;
                            if (start < end)
                            {
                                int middle = bounds.Top + (bounds.Height + 1) / 2;
                                graphics.DrawLine(separator, start, middle, end, middle);
                            }
                        }
                        TextRenderer.DrawText(graphics, group.Header, font, bounds, foreground, flags);
                    }
                }
                return;
            }
        }

        /// <summary>
        /// Handle the Custom draw series of notifications
        /// </summary>
        /// <param name="m">The message</param>
        /// <returns>True if the message has been handled</returns>
        protected virtual bool HandleCustomDraw(ref Message m) {
            const int CDDS_PREPAINT = 1;
            const int CDDS_POSTPAINT = 2;
            const int CDDS_PREERASE = 3;
            const int CDDS_POSTERASE = 4;
            //const int CDRF_NEWFONT = 2;
            //const int CDRF_SKIPDEFAULT = 4;
            const int CDDS_ITEM = 0x00010000;
            const int CDDS_SUBITEM = 0x00020000;
            const int CDDS_ITEMPREPAINT = (CDDS_ITEM | CDDS_PREPAINT);
            const int CDDS_ITEMPOSTPAINT = (CDDS_ITEM | CDDS_POSTPAINT);
            const int CDDS_ITEMPREERASE = (CDDS_ITEM | CDDS_PREERASE);
            const int CDDS_ITEMPOSTERASE = (CDDS_ITEM | CDDS_POSTERASE);
            const int CDDS_SUBITEMPREPAINT = (CDDS_SUBITEM | CDDS_ITEMPREPAINT);
            const int CDDS_SUBITEMPOSTPAINT = (CDDS_SUBITEM | CDDS_ITEMPOSTPAINT);
            const int CDRF_NOTIFYPOSTPAINT = 0x10;
            //const int CDRF_NOTIFYITEMDRAW = 0x20;
            //const int CDRF_NOTIFYSUBITEMDRAW = 0x20; // same value as above!
            const int CDRF_NOTIFYPOSTERASE = 0x40;

            NativeMethods.NMLVCUSTOMDRAW nmcustomdraw = (NativeMethods.NMLVCUSTOMDRAW)m.GetLParam(typeof(NativeMethods.NMLVCUSTOMDRAW));
            //System.Diagnostics.Debug.WriteLine(String.Format("cd: {0:x}, {1}, {2}", nmcustomdraw.nmcd.dwDrawStage, nmcustomdraw.dwItemType, nmcustomdraw.nmcd.dwItemSpec));

            if (ObjectListViewTheme.IsDarkMode && !this.Enabled && nmcustomdraw.dwItemType != 1
                && nmcustomdraw.nmcd.dwDrawStage == CDDS_PREPAINT)
            {
                // Native disabled painting ignores LVM_SETBKCOLOR, leaving light gaps around owner-drawn rows.
                using (Graphics graphics = Graphics.FromHdc(nmcustomdraw.nmcd.hdc))
                using (Brush background = new SolidBrush(this.BackColor))
                {
                    graphics.FillRectangle(background, this.ClientRectangle);
                }
            }

            // Group captions also paint through WM_PRINT, outside the row-paint guards below.
            if (nmcustomdraw.dwItemType == 1) {
                if (ObjectListViewTheme.IsDarkMode || (!SystemInformation.HighContrast
                    && ObjectListViewTheme.HasGroupHeaderColorOverrides)) {
                    int stage = nmcustomdraw.nmcd.dwDrawStage & 0xFFFF;
                    if (stage == CDDS_PREPAINT) {
                        m.Result = (IntPtr)CDRF_NOTIFYPOSTPAINT;
                    } else if (stage == CDDS_POSTPAINT) {
                        this.DrawDarkGroupHeader(nmcustomdraw);
                    }
                }
                return true;
            }

            // There is a bug in owner drawn virtual lists which causes lots of custom draw messages
            // to be sent to the control *outside* of a WmPaint event. AFAIK, these custom draw events
            // are spurious and only serve to make the control flicker annoyingly.
            // So, we ignore messages that are outside of a paint event.
            if (!this.isInWmPaintEvent)
            {
                return true;
            }

            // One more complication! Sometimes with owner drawn virtual lists, the act of drawing
            // the overlays triggers a second attempt to paint the control -- which makes an annoying
            // flicker. So, we only do the custom drawing once per WmPaint event.
            if (!this.shouldDoCustomDrawing)
            {
                return true;
            }

            switch (nmcustomdraw.nmcd.dwDrawStage) {
                case CDDS_PREPAINT:
                    //System.Diagnostics.Debug.WriteLine("CDDS_PREPAINT");
                    // Remember which items were drawn during this paint cycle
                    if (this.prePaintLevel == 0)
                    {
                        this.drawnItems = new List<OLVListItem>();
                    }

                    // If there are any items, we have to wait until at least one has been painted
                    // before we draw the overlays. If there aren't any items, there will never be any
                    // item paint events, so we can draw the overlays whenever
                    this.isAfterItemPaint = (this.GetItemCount() == 0);
                    this.prePaintLevel++;
                    base.WndProc(ref m);

                    // Make sure that we get postpaint notifications
                    m.Result = (IntPtr)((int)m.Result | CDRF_NOTIFYPOSTPAINT | CDRF_NOTIFYPOSTERASE);
                    return true;

                case CDDS_POSTPAINT:
                    //System.Diagnostics.Debug.WriteLine("CDDS_POSTPAINT");
                    this.prePaintLevel--;

                    // When in group view, we have two problems. On XP, the control sends
                    // a whole heap of PREPAINT/POSTPAINT messages before drawing any items.
                    // We have to wait until after the first item paint before we draw overlays.
                    // On Vista, we have a different problem. On Vista, the control nests calls
                    // to PREPAINT and POSTPAINT. We only want to draw overlays on the outermost
                    // POSTPAINT.
                    if (this.prePaintLevel == 0 && (this.isMarqueSelecting || this.isAfterItemPaint)) {
                        this.shouldDoCustomDrawing = false;

                        // Draw our overlays after everything has been drawn
                        using (Graphics g = Graphics.FromHdc(nmcustomdraw.nmcd.hdc)) {
                            this.DrawDarkDisabledGridLines(g);
                            this.DrawAllDecorations(g, this.drawnItems);
                        }
                    }
                    break;

                case CDDS_ITEMPREPAINT:
                    //System.Diagnostics.Debug.WriteLine("CDDS_ITEMPREPAINT");

                    // When in group view on XP, the control send a whole heap of PREPAINT/POSTPAINT
                    // messages before drawing any items.
                    // We have to wait until after the first item paint before we draw overlays
                    this.isAfterItemPaint = true;

                    // This scheme of catching custom draw msgs works fine, except
                    // for Tile view. Something in .NET's handling of Tile view causes lots
                    // of invalidates and erases. So, we just ignore completely
                    // .NET's handling of Tile view and let the underlying control
                    // do its stuff. Strangely, if the Tile view is
                    // completely owner drawn, those erasures don't happen.
                    if (this.View == View.Tile) {
                        if (this.OwnerDraw && this.ItemRenderer != null)
                        {
                            base.WndProc(ref m);
                        }
                    } else {
                        base.WndProc(ref m);
                    }

                    if (ObjectListViewTheme.IsDarkMode && !this.Enabled && !this.OwnerDraw)
                    {
                        nmcustomdraw = (NativeMethods.NMLVCUSTOMDRAW)m.GetLParam(typeof(NativeMethods.NMLVCUSTOMDRAW));
                        nmcustomdraw.clrText = ColorTranslator.ToWin32(ObjectListViewTheme.Current.GrayTextColor);
                        OLVListItem nativeItem = this.GetItem(checked((int)nmcustomdraw.nmcd.dwItemSpec));
                        if (nativeItem != null)
                        {
                            nmcustomdraw.clrTextBk = ColorTranslator.ToWin32(nativeItem.BackColor);
                            nmcustomdraw.clrFace = nmcustomdraw.clrTextBk;
                            Marshal.StructureToPtr(nmcustomdraw, m.LParam, false);
                        }
                    }

                    m.Result = (IntPtr)((int)m.Result | CDRF_NOTIFYPOSTPAINT | CDRF_NOTIFYPOSTERASE);
                    return true;

                case CDDS_ITEMPOSTPAINT:
                    //System.Diagnostics.Debug.WriteLine("CDDS_ITEMPOSTPAINT");
                    // Remember which items have been drawn so we can draw any decorations for them
                    // once all other painting is finished
                    if (this.Columns.Count > 0) {
                        OLVListItem olvi = this.GetItem(checked((int)nmcustomdraw.nmcd.dwItemSpec));
                        if (olvi != null)
                        {
                            this.drawnItems.Add(olvi);
                        }
                    }
                    break;

                case CDDS_SUBITEMPREPAINT:
                    //System.Diagnostics.Debug.WriteLine(String.Format("CDDS_SUBITEMPREPAINT ({0},{1})", (int)nmcustomdraw.nmcd.dwItemSpec, nmcustomdraw.iSubItem));

                    if (ObjectListViewTheme.IsDarkMode && !this.Enabled && !this.OwnerDraw)
                    {
                        base.WndProc(ref m);
                        nmcustomdraw = (NativeMethods.NMLVCUSTOMDRAW)m.GetLParam(typeof(NativeMethods.NMLVCUSTOMDRAW));
                        nmcustomdraw.clrText = ColorTranslator.ToWin32(ObjectListViewTheme.Current.GrayTextColor);
                        Marshal.StructureToPtr(nmcustomdraw, m.LParam, false);
                        return true;
                    }

                    // There is a bug in the .NET framework which appears when column 0 of an owner drawn listview
                    // is dragged to another column position.
                    // The bounds calculation always returns the left edge of column 0 as being 0.
                    // The effects of this bug become apparent
                    // when the listview is scrolled horizontally: the control can think that column 0
                    // is no longer visible (the horizontal scroll position is subtracted from the bounds, giving a
                    // rectangle that is offscreen). In those circumstances, column 0 is not redraw because
                    // the control thinks it is not visible and so does not trigger a DrawSubItem event.

                    // To fix this problem, we have to detected the situation -- owner drawing column 0 in any column except 0 --
                    // trigger our own DrawSubItem, and then prevent the default processing from occurring.

                    // Are we owner drawing column 0 when it's in any column except 0?
                    if (!this.OwnerDraw)
                    {
                        return false;
                    }

                    int columnIndex = nmcustomdraw.iSubItem;
                    if (columnIndex != 0)
                    {
                        return false;
                    }

                    int displayIndex = this.Columns[0].DisplayIndex;
                    if (displayIndex == 0)
                    {
                        return false;
                    }

                    int rowIndex = checked((int)nmcustomdraw.nmcd.dwItemSpec);
                    OLVListItem item = this.GetItem(rowIndex);
                    if (item == null)
                    {
                        return false;
                    }

                    // OK. We have the error condition, so lets do what the .NET framework should do.
                    // Trigger an event to draw column 0 when it is not at display index 0
                    using (Graphics g = Graphics.FromHdc(nmcustomdraw.nmcd.hdc)) {

                        // Correctly calculate the bounds of cell 0
                        Rectangle r = item.GetSubItemBounds(0);

                        // We can hardcode "0" here since we know we are only doing this for column 0
                        DrawListViewSubItemEventArgs args = new DrawListViewSubItemEventArgs(g, r, item, item.SubItems[0], rowIndex, 0,
                            this.Columns[0], (ListViewItemStates)nmcustomdraw.nmcd.uItemState);
                        this.OnDrawSubItem(args);

                        // If the event handler wants to do the default processing (i.e. DrawDefault = true), we are stuck.
                        // There is no way we can force the default drawing because of the bug in .NET we are trying to get around.
                        System.Diagnostics.Trace.Assert(!args.DrawDefault, "Default drawing is impossible in this situation");
                    }
                    m.Result = (IntPtr)4;

                    return true;

                case CDDS_SUBITEMPOSTPAINT:
                    //System.Diagnostics.Debug.WriteLine("CDDS_SUBITEMPOSTPAINT");
                    break;

                // I have included these stages, but it doesn't seem that they are sent for ListViews.
                // http://www.tech-archive.net/Archive/VC/microsoft.public.vc.mfc/2006-08/msg00220.html

                case CDDS_PREERASE:
                    //System.Diagnostics.Debug.WriteLine("CDDS_PREERASE");
                    break;

                case CDDS_POSTERASE:
                    //System.Diagnostics.Debug.WriteLine("CDDS_POSTERASE");
                    break;

                case CDDS_ITEMPREERASE:
                    //System.Diagnostics.Debug.WriteLine("CDDS_ITEMPREERASE");
                    break;

                case CDDS_ITEMPOSTERASE:
                    //System.Diagnostics.Debug.WriteLine("CDDS_ITEMPOSTERASE");
                    break;
            }

            return false;
        }
        bool isAfterItemPaint;
        List<OLVListItem> drawnItems;

        /// <summary>
        /// Handle the underlying control being destroyed
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        protected virtual bool HandleDestroy(ref Message m) {
            //System.Diagnostics.Debug.WriteLine(String.Format("WM_DESTROY: Disposing={0}, IsDisposed={1}, VirtualMode={2}", Disposing, IsDisposed, VirtualMode));

            // Recreate the header control when the listview control is destroyed
            this.headerControl = null;

            // When the underlying control is destroyed, we need to recreate and reconfigure its tooltip
            if (this.cellToolTip != null && !this.Disposing && !this.IsDisposed) {
                this.cellToolTip.PreserveSettingsForHandleRecreation();
                this.BeginInvoke((MethodInvoker)delegate {
                    this.UpdateCellToolTipHandle();
                    this.cellToolTip.PopSettings();
                });
            }

            return false;
        }

        /// <summary>
        /// Handle the search for item m if possible.
        /// </summary>
        /// <param name="m">The m to be processed</param>
        /// <returns>bool to indicate if the msg has been handled</returns>
        protected virtual bool HandleFindItem(ref Message m) {
            // NOTE: As far as I can see, this message is never actually sent to the control, making this
            // method redundant!

            const int LVFI_STRING = 0x0002;

            NativeMethods.LVFINDINFO findInfo = (NativeMethods.LVFINDINFO)m.GetLParam(typeof(NativeMethods.LVFINDINFO));

            // We can only handle string searches
            if ((findInfo.flags & LVFI_STRING) != LVFI_STRING)
            {
                return false;
            }

            int start = m.WParam.ToInt32();
            m.Result = (IntPtr)this.FindMatchingRow(findInfo.psz, start, SearchDirectionHint.Down);
            return true;
        }

        /// <summary>
        /// Find the first row after the given start in which the text value in the
        /// comparison column begins with the given text. The comparison column is column 0,
        /// unless IsSearchOnSortColumn is true, in which case the current sort column is used.
        /// </summary>
        /// <param name="text">The text to be prefix matched</param>
        /// <param name="start">The index of the first row to consider</param>
        /// <param name="direction">Which direction should be searched?</param>
        /// <returns>The index of the first row that matched, or -1</returns>
        /// <remarks>The text comparison is a case-insensitive, prefix match. The search will
        /// search the every row until a match is found, wrapping at the end if needed.</remarks>
        public virtual int FindMatchingRow(string text, int start, SearchDirectionHint direction) {
            // We also can't do anything if we don't have data
            if (this.Columns.Count == 0)
            {
                return -1;
            }

            int rowCount = this.GetItemCount();
            if (rowCount == 0)
            {
                return -1;
            }

            // Which column are we going to use for our comparing?
            OLVColumn column = this.GetColumn(0);
            if (this.IsSearchOnSortColumn && this.View == View.Details && this.PrimarySortColumn != null)
            {
                column = this.PrimarySortColumn;
            }

            // Do two searches if necessary to find a match. The second search is the wrap-around part of searching
            int i;
            if (direction == SearchDirectionHint.Down) {
                i = this.FindMatchInRange(text, start, rowCount - 1, column);
                if (i == -1 && start > 0)
                {
                    i = this.FindMatchInRange(text, 0, start - 1, column);
                }
            } else {
                i = this.FindMatchInRange(text, start, 0, column);
                if (i == -1 && start != rowCount)
                {
                    i = this.FindMatchInRange(text, rowCount - 1, start + 1, column);
                }
            }

            return i;
        }

        /// <summary>
        /// Find the first row in the given range of rows that prefix matches the string value of the given column.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <param name="column"></param>
        /// <returns>The index of the matched row, or -1</returns>
        protected virtual int FindMatchInRange(string text, int first, int last, OLVColumn column) {
            if (first <= last) {
                for (int i = first; i <= last; i++) {
                    string data = column.GetStringValue(this.GetNthItemInDisplayOrder(i).RowObject);
                    if (data.StartsWith(text, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return i;
                    }
                }
            } else {
                for (int i = first; i >= last; i--) {
                    string data = column.GetStringValue(this.GetNthItemInDisplayOrder(i).RowObject);
                    if (data.StartsWith(text, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Handle the Group Info series of notifications
        /// </summary>
        /// <param name="m">The message</param>
        /// <returns>True if the message has been handled</returns>
        protected virtual bool HandleGroupInfo(ref Message m)
        {
            NativeMethods.NMLVGROUP nmlvgroup = (NativeMethods.NMLVGROUP)m.GetLParam(typeof(NativeMethods.NMLVGROUP));

            //System.Diagnostics.Debug.WriteLine(String.Format("group: {0}, old state: {1}, new state: {2}",
            //    nmlvgroup.iGroupId, StateToString(nmlvgroup.uOldState), StateToString(nmlvgroup.uNewState)));

            // Ignore state changes that aren't related to selection, focus or collapsedness
            const uint INTERESTING_STATES = (uint) (GroupState.LVGS_COLLAPSED | GroupState.LVGS_FOCUSED | GroupState.LVGS_SELECTED);
            if ((nmlvgroup.uOldState & INTERESTING_STATES) == (nmlvgroup.uNewState & INTERESTING_STATES))
            {
                return false;
            }

            foreach (OLVGroup group in this.OLVGroups) {
                if (group.GroupId == nmlvgroup.iGroupId) {
                    GroupStateChangedEventArgs args = new GroupStateChangedEventArgs(group, (GroupState)nmlvgroup.uOldState, (GroupState)nmlvgroup.uNewState);
                    this.OnGroupStateChanged(args);
                    break;
                }
            }

            return false;
        }

        //private static string StateToString(uint state)
        //{
        //    if (state == 0)
        //        return Enum.GetName(typeof(GroupState), 0);
        //
        //    List<string> names = new List<string>();
        //    foreach (int value in Enum.GetValues(typeof(GroupState)))
        //    {
        //        if (value != 0 && (state & value) == value)
        //        {
        //            names.Add(Enum.GetName(typeof(GroupState), value));
        //        }
        //    }
        //    return names.Count == 0 ? state.ToString("x") : String.Join("|", names.ToArray());
        //}

        /// <summary>
        /// Handle a key down message
        /// </summary>
        /// <param name="m"></param>
        /// <returns>True if the msg has been handled</returns>
        protected virtual bool HandleKeyDown(ref Message m) {

            // If this is a checkbox list, toggle the selected rows when the user presses Space
            if (this.CheckBoxes && m.WParam.ToInt32() == (int)Keys.Space && this.SelectedIndices.Count > 0) {
                this.ToggleSelectedRowCheckBoxes();
                return true;
            }

            // Remember the scroll position so we can decide if the listview has scrolled in the
            // handling of the event.
            int scrollPositionH = NativeMethods.GetScrollPosition(this, true);
            int scrollPositionV = NativeMethods.GetScrollPosition(this, false);

            base.WndProc(ref m);

            // It's possible that the processing in base.WndProc has actually destroyed this control
            if (this.IsDisposed)
            {
                return true;
            }

            // If the keydown processing changed the scroll position, trigger a Scroll event
            int newScrollPositionH = NativeMethods.GetScrollPosition(this, true);
            int newScrollPositionV = NativeMethods.GetScrollPosition(this, false);

            if (scrollPositionH != newScrollPositionH) {
                ScrollEventArgs args = new ScrollEventArgs(ScrollEventType.EndScroll,
                    scrollPositionH, newScrollPositionH, ScrollOrientation.HorizontalScroll);
                this.OnScroll(args);
            }
            if (scrollPositionV != newScrollPositionV) {
                ScrollEventArgs args = new ScrollEventArgs(ScrollEventType.EndScroll,
                    scrollPositionV, newScrollPositionV, ScrollOrientation.VerticalScroll);
                this.OnScroll(args);
            }

            if (scrollPositionH != newScrollPositionH || scrollPositionV != newScrollPositionV)
            {
                this.RefreshHotItem();
            }

            return true;
        }

        /// <summary>
        /// Toggle the checkedness of the selected rows
        /// </summary>
        /// <remarks>
        /// <para>
        /// Actually, this doesn't actually toggle all rows. It toggles the first row, and
        /// all other rows get the check state of that first row. This is actually a much
        /// more useful behaviour.
        /// </para>
        /// <para>
        /// If no rows are selected, this method does nothing.
        /// </para>
        /// </remarks>
        public void ToggleSelectedRowCheckBoxes() {
            if (this.SelectedIndices.Count == 0)
            {
                return;
            }

            Object primaryModel = this.GetItem(this.SelectedIndices[0]).RowObject;
            this.ToggleCheckObject(primaryModel);
            CheckState? state = this.GetCheckState(primaryModel);
            if (state.HasValue) {
                foreach (Object x in this.SelectedModelObjects)
                {
                    this.SetObjectCheckedness(x, state.Value);
                }
            }
        }

        /// <summary>
        /// Catch the Left Button down event.
        /// </summary>
        /// <param name="m">The m to be processed</param>
        /// <returns>bool to indicate if the msg has been handled</returns>
        protected virtual bool HandleLButtonDown(ref Message m) {
            // If there are no columns, the base ListView class can throw OutOfRange exceptions.
            if (this.Columns.Count == 0)
            {
                return true;
            }

            // We have to intercept this low level message rather than the more natural
            // overridding of OnMouseDown, since ListCtrl's internal mouse down behavior
            // is to select (or deselect) rows when the mouse is released. We don't
            // want the selection to change when the user checks or unchecks a checkbox, so if the
            // mouse down event was to check/uncheck, we have to hide this mouse
            // down event from the control.

            int x = m.LParam.ToInt32() & 0xFFFF;
            int y = (m.LParam.ToInt32() >> 16) & 0xFFFF;

            return this.ProcessLButtonDown(this.OlvHitTest(x, y));
        }

        /// <summary>
        /// Handle a left mouse down at the given hit test location
        /// </summary>
        /// <remarks>Subclasses can override this to do something unique</remarks>
        /// <param name="hti"></param>
        /// <returns>True if the message has been handled</returns>
        protected virtual bool ProcessLButtonDown(OlvListViewHitTestInfo hti) {

            if (hti.Item == null)
            {
                return false;
            }

            // If the click occurs on a button, ignore it so the row isn't selected
            if (hti.HitTestLocation == HitTestLocation.Button) {
                this.Invalidate();

                return true;
            }

            // If they didn't click checkbox, we can just return
            if (hti.HitTestLocation != HitTestLocation.CheckBox)
            {
                return false;
            }

            // Disabled rows cannot change checkboxes
            if (!hti.Item.Enabled)
            {
                return true;
            }

            // Did they click a sub item checkbox?
            if (hti.Column != null && hti.Column.Index > 0) {
                if (hti.Column.IsEditable && hti.Item.Enabled)
                {
                    this.ToggleSubItemCheckBox(hti.RowObject, hti.Column);
                }

                return true;
            }

            // They must have clicked the primary checkbox
            this.ToggleCheckObject(hti.RowObject);

            // If they change the checkbox of a selected row, all the rows in the selection
            // should be given the same state
            if (hti.Item.Selected) {
                CheckState? state = this.GetCheckState(hti.RowObject);
                if (state.HasValue) {
                    foreach (Object x in this.SelectedModelObjects)
                    {
                        this.SetObjectCheckedness(x, state.Value);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Catch the Left Button up event.
        /// </summary>
        /// <param name="m">The m to be processed</param>
        /// <returns>bool to indicate if the msg has been handled</returns>
        protected virtual bool HandleLButtonUp(ref Message m) {
            // If there are no columns, the base ListView class can throw OutOfRange exceptions.
            if (this.Columns.Count == 0)
            {
                return true;
            }

            if (this.MouseMoveHitTest == null)
            {
                return false;
            }

            int x = m.LParam.ToInt32() & 0xFFFF;
            int y = (m.LParam.ToInt32() >> 16) & 0xFFFF;

            // Did they click an enabled, non-empty button?
            if (this.MouseMoveHitTest.HitTestLocation == HitTestLocation.Button) {
                // If a button was hit, Item and Column must be non-null
                if (this.MouseMoveHitTest.Item.Enabled || this.MouseMoveHitTest.Column.EnableButtonWhenItemIsDisabled) {
                    string buttonText = this.MouseMoveHitTest.Column.GetStringValue(this.MouseMoveHitTest.RowObject);
                    if (!String.IsNullOrEmpty(buttonText)) {
                        this.Invalidate();
                        CellClickEventArgs args = new CellClickEventArgs();
                        this.BuildCellEvent(args, new Point(x, y), this.MouseMoveHitTest);
                        this.OnButtonClick(args);
                        return true;
                    }
                }
            }

            // Are they trying to expand/collapse a group?
            if (this.MouseMoveHitTest.HitTestLocation == HitTestLocation.GroupExpander) {
                if (this.TriggerGroupExpandCollapse(this.MouseMoveHitTest.Group))
                {
                    return true;
                }
            }

            if (ObjectListViewBase.IsVistaOrLater && this.HasCollapsibleGroups)
            {
                base.DefWndProc(ref m);
            }

            return false;
        }

        /// <summary>
        /// Trigger a GroupExpandCollapse event and return true if the action was cancelled
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        protected virtual bool TriggerGroupExpandCollapse(OLVGroup group)
        {
            GroupExpandingCollapsingEventArgs args = new GroupExpandingCollapsingEventArgs(group);
            this.OnGroupExpandingCollapsing(args);
            return args.Canceled;
        }

        /// <summary>
        /// Catch the Right Button down event.
        /// </summary>
        /// <param name="m">The m to be processed</param>
        /// <returns>bool to indicate if the msg has been handled</returns>
        protected virtual bool HandleRButtonDown(ref Message m) {
            // If there are no columns, the base ListView class can throw OutOfRange exceptions.
            if (this.Columns.Count == 0)
            {
                return true;
            }

            int x = m.LParam.ToInt32() & 0xFFFF;
            int y = (m.LParam.ToInt32() >> 16) & 0xFFFF;

            return this.ProcessRButtonDown(this.OlvHitTest(x, y));
        }

        /// <summary>
        /// Handle a left mouse down at the given hit test location
        /// </summary>
        /// <remarks>Subclasses can override this to do something unique</remarks>
        /// <param name="hti"></param>
        /// <returns>True if the message has been handled</returns>
        protected virtual bool ProcessRButtonDown(OlvListViewHitTestInfo hti) {            
            if (hti.Item == null)
            {
                return false;
            }

            // Ignore clicks on checkboxes
            return (hti.HitTestLocation == HitTestLocation.CheckBox);
        }

        /// <summary>
        /// Catch the Left Button double click event.
        /// </summary>
        /// <param name="m">The m to be processed</param>
        /// <returns>bool to indicate if the msg has been handled</returns>
        protected virtual bool HandleLButtonDoubleClick(ref Message m) {
            // If there are no columns, the base ListView class can throw OutOfRange exceptions.
            if (this.Columns.Count == 0)
            {
                return true;
            }

            int x = m.LParam.ToInt32() & 0xFFFF;
            int y = (m.LParam.ToInt32() >> 16) & 0xFFFF;

            return this.ProcessLButtonDoubleClick(this.OlvHitTest(x, y));
        }

        /// <summary>
        /// Handle a mouse double click at the given hit test location
        /// </summary>
        /// <remarks>Subclasses can override this to do something unique</remarks>
        /// <param name="hti"></param>
        /// <returns>True if the message has been handled</returns>
        protected virtual bool ProcessLButtonDoubleClick(OlvListViewHitTestInfo hti) {
            // If the user double clicked on a checkbox, ignore it
            return (hti.HitTestLocation == HitTestLocation.CheckBox);
        }

        /// <summary>
        /// Catch the right Button double click event.
        /// </summary>
        /// <param name="m">The m to be processed</param>
        /// <returns>bool to indicate if the msg has been handled</returns>
        protected virtual bool HandleRButtonDoubleClick(ref Message m) {

            // If there are no columns, the base ListView class can throw OutOfRange exceptions.
            if (this.Columns.Count == 0)
            {
                return true;
            }

            int x = m.LParam.ToInt32() & 0xFFFF;
            int y = (m.LParam.ToInt32() >> 16) & 0xFFFF;

            return this.ProcessRButtonDoubleClick(this.OlvHitTest(x, y));
        }

        /// <summary>
        /// Handle a right mouse double click at the given hit test location
        /// </summary>
        /// <remarks>Subclasses can override this to do something unique</remarks>
        /// <param name="hti"></param>
        /// <returns>True if the message has been handled</returns>
        protected virtual bool ProcessRButtonDoubleClick(OlvListViewHitTestInfo hti) {

            // If the user double clicked on a checkbox, ignore it
            return (hti.HitTestLocation == HitTestLocation.CheckBox);
        }

        /// <summary>
        /// Catch the MouseMove event.
        /// </summary>
        /// <param name="m">The m to be processed</param>
        /// <returns>bool to indicate if the msg has been handled</returns>
        protected virtual bool HandleMouseMove(ref Message m)
        {
            //int x = m.LParam.ToInt32() & 0xFFFF;
            //int y = (m.LParam.ToInt32() >> 16) & 0xFFFF;

            //this.lastMouseMoveX = x;
            //this.lastMouseMoveY = y;

            return false;
        }
        //private int lastMouseMoveX = -1;
        //private int lastMouseMoveY = -1;

        /// <summary>
        /// Handle notifications that have been reflected back from the parent window
        /// </summary>
        /// <param name="m">The m to be processed</param>
        /// <returns>bool to indicate if the msg has been handled</returns>
        protected virtual bool HandleReflectNotify(ref Message m) {
            const int NM_CLICK = -2;
            const int NM_DBLCLK = -3;
            const int NM_RDBLCLK = -6;
            const int NM_CUSTOMDRAW = -12;
            const int NM_RELEASEDCAPTURE = -16;
            const int LVN_FIRST = -100;
            const int LVN_ITEMCHANGED = LVN_FIRST - 1;
            const int LVN_ITEMCHANGING = LVN_FIRST - 0;
            const int LVN_HOTTRACK = LVN_FIRST - 21;
            const int LVN_MARQUEEBEGIN = LVN_FIRST - 56;
            const int LVN_GETINFOTIP = LVN_FIRST - 58;
            const int LVN_GETDISPINFO = LVN_FIRST - 77;
            const int LVN_BEGINSCROLL = LVN_FIRST - 80;
            const int LVN_ENDSCROLL = LVN_FIRST - 81;
            const int LVN_LINKCLICK = LVN_FIRST - 84;
            const int LVN_GROUPINFO = LVN_FIRST - 88; // undocumented
            const int LVIF_STATE = 8;
            //const int LVIS_FOCUSED = 1;
            const int LVIS_SELECTED = 2;

            bool isMsgHandled = false;

            // TODO: Don't do any logic in this method. Create separate methods for each message

            NativeMethods.NMHDR nmhdr = (NativeMethods.NMHDR)m.GetLParam(typeof(NativeMethods.NMHDR));
            //System.Diagnostics.Debug.WriteLine(String.Format("rn: {0}", nmhdr->code));

            switch (nmhdr.code) {
                case NM_CLICK:
                    // The standard ListView does some strange stuff here when the list has checkboxes.
                    // If you shift click on non-primary columns when FullRowSelect is true, the 
                    // checkedness of the selected rows changes. 
                    // We can't just not do the base class stuff because it sets up state that is used to
                    // determine mouse up events later on.
                    // So, we sabotage the base class's process of the click event. The base class does a HITTEST
                    // in order to determine which row was clicked -- if that fails, the base class does nothing.
                    // So when we get a CLICK, we know that the base class is going to send a HITTEST very soon,
                    // so we ignore the next HITTEST message, which will cause the click processing to fail.
                    //System.Diagnostics.Debug.WriteLine("NM_CLICK");
                    this.skipNextHitTest = true;
                    break;

                case LVN_BEGINSCROLL:
                    //System.Diagnostics.Debug.WriteLine("LVN_BEGINSCROLL");
                    isMsgHandled = this.HandleBeginScroll(ref m);
                    break;

                case LVN_ENDSCROLL:
                    isMsgHandled = this.HandleEndScroll(ref m);
                    break;

                case LVN_LINKCLICK:
                    isMsgHandled = this.HandleLinkClick(ref m);
                    break;

                case LVN_MARQUEEBEGIN:
                    //System.Diagnostics.Debug.WriteLine("LVN_MARQUEEBEGIN");
                    this.isMarqueSelecting = true;
                    break;

                case LVN_GETINFOTIP:
                    //System.Diagnostics.Debug.WriteLine("LVN_GETINFOTIP");
                    // When virtual lists are in SmallIcon view, they generates tooltip message with invalid item indices.
                    NativeMethods.NMLVGETINFOTIP nmGetInfoTip = (NativeMethods.NMLVGETINFOTIP)m.GetLParam(typeof(NativeMethods.NMLVGETINFOTIP));
                    isMsgHandled = nmGetInfoTip.iItem >= this.GetItemCount() || this.Columns.Count == 0;
                    break;

                case NM_RELEASEDCAPTURE:
                    //System.Diagnostics.Debug.WriteLine("NM_RELEASEDCAPTURE");
                    this.isMarqueSelecting = false;
                    this.Invalidate();
                    break;

                case NM_CUSTOMDRAW:
                    //System.Diagnostics.Debug.WriteLine("NM_CUSTOMDRAW");
                    isMsgHandled = this.HandleCustomDraw(ref m);
                    break;

                case NM_DBLCLK:
                    // The default behavior of a .NET ListView with checkboxes is to toggle the checkbox on
                    // double-click. That's just silly, if you ask me :)
                    if (this.CheckBoxes) {
                        // How do we make ListView not do that silliness? We could just ignore the message
                        // but the last part of the base code sets up state information, and without that
                        // state, the ListView doesn't trigger MouseDoubleClick events. So we fake a
                        // right button double click event, which sets up the same state, but without
                        // toggling the checkbox.
                        nmhdr.code = NM_RDBLCLK;
                        Marshal.StructureToPtr(nmhdr, m.LParam, false);
                    }
                    break;

                case LVN_ITEMCHANGED:
                    //System.Diagnostics.Debug.WriteLine("LVN_ITEMCHANGED");
                    NativeMethods.NMLISTVIEW nmlistviewPtr2 = (NativeMethods.NMLISTVIEW)m.GetLParam(typeof(NativeMethods.NMLISTVIEW));
                    if ((nmlistviewPtr2.uChanged & LVIF_STATE) != 0) {
                        CheckState currentValue = CalculateCheckState(nmlistviewPtr2.uOldState);
                        CheckState newCheckValue = CalculateCheckState(nmlistviewPtr2.uNewState);
                        if (currentValue != newCheckValue)
                        {
                            // Remove the state indices so that we don't trigger the OnItemChecked method
                            // when we call our base method after exiting this method
                            nmlistviewPtr2.uOldState = (nmlistviewPtr2.uOldState & 0x0FFF);
                            nmlistviewPtr2.uNewState = (nmlistviewPtr2.uNewState & 0x0FFF);
                            Marshal.StructureToPtr(nmlistviewPtr2, m.LParam, false);
                        }
                        else
                        {
                            bool isSelected = (nmlistviewPtr2.uNewState & LVIS_SELECTED) == LVIS_SELECTED;

                            if (isSelected)
                            {
                               // System.Diagnostics.Debug.WriteLine(String.Format("Selected: {0}", nmlistviewPtr2.iItem));
                                bool isShiftDown = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

                                // -1 indicates that all rows are to be selected -- in fact, they already have been.
                                // We now have to deselect all the disabled objects.
                                if (nmlistviewPtr2.iItem == -1 || isShiftDown) {
                                    Stopwatch sw = Stopwatch.StartNew();
                                    foreach (object disabledModel in this.DisabledObjects)
                                    {
                                        int modelIndex = this.IndexOf(disabledModel);
                                        if (modelIndex >= 0)
                                        {
                                            NativeMethods.DeselectOneItem(this, modelIndex);
                                        }
                                    }
                                    // System.Diagnostics.Debug.WriteLine(String.Format("PERF - Deselecting took {0}ms / {1} ticks", sw.ElapsedMilliseconds, sw.ElapsedTicks));
                                }
                                else
                                {
                                    // If the object just selected is disabled, explicitly de-select it
                                    OLVListItem olvi = this.GetItem(nmlistviewPtr2.iItem);
                                    if (olvi != null && !olvi.Enabled)
                                    {
                                        NativeMethods.DeselectOneItem(this, nmlistviewPtr2.iItem);
                                    }
                                }
                            }
                        }
                    }
                    break;

                case LVN_ITEMCHANGING:
                    //System.Diagnostics.Debug.WriteLine("LVN_ITEMCHANGING");
                    NativeMethods.NMLISTVIEW nmlistviewPtr = (NativeMethods.NMLISTVIEW)m.GetLParam(typeof(NativeMethods.NMLISTVIEW));
                    if ((nmlistviewPtr.uChanged & LVIF_STATE) != 0) {
                        CheckState currentValue = CalculateCheckState(nmlistviewPtr.uOldState);
                        CheckState newCheckValue = CalculateCheckState(nmlistviewPtr.uNewState);

                        if (currentValue != newCheckValue) {
                            // Prevent the base method from seeing the state change,
                            // since we handled it elsewhere
                            nmlistviewPtr.uChanged &= ~LVIF_STATE;
                            Marshal.StructureToPtr(nmlistviewPtr, m.LParam, false);
                        }
                    }
                    break;

                case LVN_HOTTRACK:
                    break;

                case LVN_GETDISPINFO:
                    break;

                case LVN_GROUPINFO:
                    //System.Diagnostics.Debug.WriteLine("reflect notify: GROUP INFO");
                    isMsgHandled = this.HandleGroupInfo(ref m);
                    break;

                //default:
                    //System.Diagnostics.Debug.WriteLine(String.Format("reflect notify: {0}", nmhdr.code));
                    //break;
            }

            return isMsgHandled;
        }
        private bool skipNextHitTest;

        private static CheckState CalculateCheckState(int state) {
            switch ((state & 0xf000) >> 12) {
                case 1:
                    return CheckState.Unchecked;
                case 2:
                    return CheckState.Checked;
                case 3:
                    return CheckState.Indeterminate;
                default:
                    return CheckState.Checked;
            }
        }

        /// <summary>
        /// In the notification messages, we handle attempts to change the width of our columns
        /// </summary>
        /// <param name="m">The m to be processed</param>
        /// <returns>bool to indicate if the msg has been handled</returns>
        protected bool HandleNotify(ref Message m) {
            bool isMsgHandled = false;

            const int NM_CUSTOMDRAW = -12;

            const int HDN_FIRST = (0 - 300);
            const int HDN_ITEMCHANGINGA = (HDN_FIRST - 0);
            const int HDN_ITEMCHANGINGW = (HDN_FIRST - 20);
            const int HDN_ITEMCLICKA = (HDN_FIRST - 2);
            const int HDN_ITEMCLICKW = (HDN_FIRST - 22);
            const int HDN_DIVIDERDBLCLICKA = (HDN_FIRST - 5);
            const int HDN_DIVIDERDBLCLICKW = (HDN_FIRST - 25);
            const int HDN_BEGINTRACKA = (HDN_FIRST - 6);
            const int HDN_BEGINTRACKW = (HDN_FIRST - 26);
            const int HDN_ENDTRACKA = (HDN_FIRST - 7);
            const int HDN_ENDTRACKW = (HDN_FIRST - 27);
            const int HDN_TRACKA = (HDN_FIRST - 8);
            const int HDN_TRACKW = (HDN_FIRST - 28);

            // Handle the notification, remembering to handle both ANSI and Unicode versions
            NativeMethods.NMHEADER nmheader = (NativeMethods.NMHEADER)m.GetLParam(typeof(NativeMethods.NMHEADER));
            //System.Diagnostics.Debug.WriteLine(String.Format("not: {0}", nmhdr->code));

            //if (nmhdr.code < HDN_FIRST)
            //    System.Diagnostics.Debug.WriteLine(nmhdr.code);

            // In KB Article #183258, MS states that when a header control has the HDS_FULLDRAG style, it will receive
            // ITEMCHANGING events rather than TRACK events. Under XP SP2 (at least) this is not always true, which may be
            // why MS has withdrawn that particular KB article. It is true that the header is always given the HDS_FULLDRAG
            // style. But even while window style set, the control doesn't always received ITEMCHANGING events.
            // The controlling setting seems to be the Explorer option "Show Window Contents While Dragging"!
            // In the category of "truly bizarre side effects", if the this option is turned on, we will receive
            // ITEMCHANGING events instead of TRACK events. But if it is turned off, we receive lots of TRACK events and
            // only one ITEMCHANGING event at the very end of the process.
            // If we receive HDN_TRACK messages, it's harder to control the resizing process. If we return a result of 1, we
            // cancel the whole drag operation, not just that particular track event, which is clearly not what we want.
            // If we are willing to compile with unsafe code enabled, we can modify the size of the column in place, using the
            // commented out code below. But without unsafe code, the best we can do is allow the user to drag the column to
            // any width, and then spring it back to within bounds once they release the mouse button. UI-wise it's very ugly.
            switch (nmheader.nhdr.code) {

                case NM_CUSTOMDRAW:
                    if (!this.OwnerDrawnHeader)
                    {
                        isMsgHandled = this.HeaderControl.HandleHeaderCustomDraw(ref m);
                    }

                    break;

                case HDN_ITEMCLICKA:
                case HDN_ITEMCLICKW:
                    if (!this.PossibleFinishCellEditing())
                    {
                        m.Result = (IntPtr)1; // prevent the change from happening
                        isMsgHandled = true;
                    }
                    break;

                case HDN_DIVIDERDBLCLICKA:
                case HDN_DIVIDERDBLCLICKW:
                case HDN_BEGINTRACKA:
                case HDN_BEGINTRACKW:
                    if (!this.PossibleFinishCellEditing()) {
                        m.Result = (IntPtr)1; // prevent the change from happening
                        isMsgHandled = true;
                        break;
                    }
                    if (nmheader.iItem >= 0 && nmheader.iItem < this.Columns.Count) {
                        OLVColumn column = this.GetColumn(nmheader.iItem);
                        // Space filling columns can't be dragged or double-click resized
                        if (column.FillsFreeSpace) {
                            m.Result = (IntPtr)1; // prevent the change from happening
                            isMsgHandled = true;
                        }
                        else if (this.OwnerDraw && this.View == View.Details &&
                            (nmheader.nhdr.code == HDN_DIVIDERDBLCLICKA || nmheader.nhdr.code == HDN_DIVIDERDBLCLICKW)) {
                            this.ResizeOwnerDrawnColumnToContents(column);
                            m.Result = (IntPtr)1;
                            isMsgHandled = true;
                        }
                    }
                    break;
                case HDN_ENDTRACKA:
                case HDN_ENDTRACKW:
                    //if (this.ShowGroups)
                    //    this.ResizeLastGroup();
                    break;
                case HDN_TRACKA:
                case HDN_TRACKW:
                    if (nmheader.iItem >= 0 && nmheader.iItem < this.Columns.Count) {
                        NativeMethods.HDITEM hditem = Marshal.PtrToStructure<NativeMethods.HDITEM>(nmheader.pHDITEM);
                        OLVColumn column = this.GetColumn(nmheader.iItem);
                        if (hditem.cxy < column.MinimumWidth)
                        {
                            hditem.cxy = column.MinimumWidth;
                        }
                        else if (column.MaximumWidth != -1 && hditem.cxy > column.MaximumWidth)
                        {
                            hditem.cxy = column.MaximumWidth;
                        }

                        Marshal.StructureToPtr(hditem, nmheader.pHDITEM, false);
                    }
                    break;

                case HDN_ITEMCHANGINGA:
                case HDN_ITEMCHANGINGW:
                    nmheader = (NativeMethods.NMHEADER)m.GetLParam(typeof(NativeMethods.NMHEADER));
                    if (nmheader.iItem >= 0 && nmheader.iItem < this.Columns.Count) {
                        NativeMethods.HDITEM hditem = Marshal.PtrToStructure<NativeMethods.HDITEM>(nmheader.pHDITEM);
                        OLVColumn column = this.GetColumn(nmheader.iItem);
                        // Check the mask to see if the width field is valid, and if it is, make sure it's within range
                        if ((hditem.mask & 1) == 1) {
                            if (hditem.cxy < column.MinimumWidth ||
                                (column.MaximumWidth != -1 && hditem.cxy > column.MaximumWidth)) {
                                m.Result = (IntPtr)1; // prevent the change from happening
                                isMsgHandled = true;
                            }
                        }
                    }
                    break;

                case ToolTipControl.TTN_SHOW:
                    //System.Diagnostics.Debug.WriteLine("olv TTN_SHOW");
                    if (this.CellToolTip.Handle == nmheader.nhdr.hwndFrom)
                    {
                        isMsgHandled = this.CellToolTip.HandleShow(ref m);
                    }

                    break;

                case ToolTipControl.TTN_POP:
                    //System.Diagnostics.Debug.WriteLine("olv TTN_POP");
                    if (this.CellToolTip.Handle == nmheader.nhdr.hwndFrom)
                    {
                        isMsgHandled = this.CellToolTip.HandlePop(ref m);
                    }

                    break;

                case ToolTipControl.TTN_GETDISPINFO:
                    //System.Diagnostics.Debug.WriteLine("olv TTN_GETDISPINFO");
                    if (this.CellToolTip.Handle == nmheader.nhdr.hwndFrom)
                    {
                        isMsgHandled = this.CellToolTip.HandleGetDispInfo(ref m);
                    }

                    break;

//                default:
//                    System.Diagnostics.Debug.WriteLine(String.Format("notify: {0}", nmheader.nhdr.code));
//                    break;
            }

            return isMsgHandled;
        }

        /// <summary>
        /// Create a ToolTipControl to manage the tooltip control used by the listview control
        /// </summary>
        protected virtual void CreateCellToolTip() {
            this.cellToolTip = new ToolTipControl();
            this.cellToolTip.AssignHandle(NativeMethods.GetTooltipControl(this));
            this.cellToolTip.Showing += new EventHandler<ToolTipShowingEventArgs>(HandleCellToolTipShowing);
            this.cellToolTip.SetMaxWidth();
            NativeMethods.MakeTopMost(this.cellToolTip);
        }

        /// <summary>
        /// Update the handle used by our cell tooltip to be the tooltip used by
        /// the underlying Windows listview control.
        /// </summary>
        protected virtual void UpdateCellToolTipHandle() {
            if (this.cellToolTip != null && this.cellToolTip.Handle == IntPtr.Zero)
            {
                this.cellToolTip.AssignHandle(NativeMethods.GetTooltipControl(this));
            }
        }

        /// <summary>
        /// Handle the WM_PAINT event
        /// </summary>
        /// <param name="m"></param>
        /// <returns>Return true if the msg has been handled and nothing further should be done</returns>
        protected virtual bool HandlePaint(ref Message m) {
            //System.Diagnostics.Debug.WriteLine("> WMPAINT");

            // We only want to custom draw the control within WmPaint message and only
            // once per paint event. We use these bools to insure this.
            this.isInWmPaintEvent = true;
            this.shouldDoCustomDrawing = true;
            this.prePaintLevel = 0;

            this.ShowOverlays();

            this.HandlePrePaint();
            base.WndProc(ref m);
            this.HandlePostPaint();
            this.isInWmPaintEvent = false;
            //System.Diagnostics.Debug.WriteLine("< WMPAINT");
            return true;
        }
        private int prePaintLevel;

        /// <summary>
        /// Perform any steps needed before painting the control
        /// </summary>
        protected virtual void HandlePrePaint() {
            // When we get a WM_PAINT msg, remember the rectangle that is being updated.
            // We can't get this information later, since the BeginPaint call wipes it out.
            // this.lastUpdateRectangle = NativeMethods.GetUpdateRect(this); // we no longer need this, but keep the code so we can see it later

            //// When the list is empty, we want to handle the drawing of the control by ourselves.
            //// Unfortunately, there is no easy way to tell our superclass that we want to do this.
            //// So we resort to guile and deception. We validate the list area of the control, which
            //// effectively tells our superclass that this area does not need to be painted.
            //// Our superclass will then not paint the control, leaving us free to do so ourselves.
            //// Without doing this trickery, the superclass will draw the list as empty,
            //// and then moments later, we will draw the empty list msg, giving a nasty flicker
            //if (this.GetItemCount() == 0 && this.HasEmptyListMsg)
            //    NativeMethods.ValidateRect(this, this.ClientRectangle);
        }

        /// <summary>
        /// Perform any steps needed after painting the control
        /// </summary>
        protected virtual void HandlePostPaint() {
            // This message is no longer necessary, but we keep it for compatibility
        }

        /// <summary>
        /// Handle the window position changing.
        /// </summary>
        /// <param name="m">The m to be processed</param>
        /// <returns>bool to indicate if the msg has been handled</returns>
        protected virtual bool HandleWindowPosChanging(ref Message m) {
            const int SWP_NOSIZE = 1;

            NativeMethods.WINDOWPOS pos = (NativeMethods.WINDOWPOS)m.GetLParam(typeof(NativeMethods.WINDOWPOS));
            if ((pos.flags & SWP_NOSIZE) == 0) {
                if (pos.cx < this.Bounds.Width) // only when shrinking
                {
                    // pos.cx is the window width, not the client area width, so we have to subtract the border widths
                    this.ResizeFreeSpaceFillingColumns(pos.cx - (this.Bounds.Width - this.ClientSize.Width));
                }
            }

            return false;
        }

        #endregion
    }
}
