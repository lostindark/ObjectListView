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
        #region List commands

        /// <summary>
        /// Add the given model object to this control.
        /// </summary>
        /// <param name="modelObject">The model object to be displayed</param>
        /// <remarks>See AddObjects() for more details</remarks>
        public virtual void AddObject(object modelObject) {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate() { this.AddObject(modelObject); });
            }
            else
            {
                this.AddObjects(new object[] { modelObject });
            }
        }

        /// <summary>
        /// Add the given collection of model objects to this control.
        /// </summary>
        /// <param name="modelObjects">A collection of model objects</param>
        /// <remarks>
        /// <para>The added objects will appear in their correct sort position, if sorting
        /// is active (i.e. if PrimarySortColumn is not null). Otherwise, they will appear at the end of the list.</para>
        /// <para>No check is performed to see if any of the objects are already in the ListView.</para>
        /// <para>Null objects are silently ignored.</para>
        /// </remarks>
        public virtual void AddObjects(IEnumerable modelObjects) {
            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate() { this.AddObjects(modelObjects); });
                return;
            }
            this.InsertObjects(ObjectListViewBase.EnumerableCount(this.ModelObjects), modelObjects);
            this.Sort();
        }

        /// <summary>
        /// Resize the columns to the maximum of the header width and the data.
        /// </summary>		
        public virtual void AutoResizeColumns() {
            foreach (OLVColumn c in this.Columns) {
                this.AutoResizeColumn(c.Index, ColumnHeaderAutoResizeStyle.HeaderSize);
            }
        }

        /// <summary>
        /// Set up any automatically initialized column widths (columns that 
        /// have a width of 0 or -1 will be resized to the width of their 
        /// contents or header respectively).
        /// </summary>
        /// <remarks>
        /// Obviously, this will only work once. Once it runs, the columns widths will
        /// be changed to something else (other than 0 or -1), so it wont do anything the 
        /// second time through. Use <see cref="AutoResizeColumns()"/> to force all columns
        /// to change their size.
        /// </remarks>
        public virtual void AutoSizeColumns() {
            // If we are supposed to resize to content, but if there is no content, 
            // resize to the header size instead.
            ColumnHeaderAutoResizeStyle resizeToContentStyle = this.GetItemCount() == 0 ?
                ColumnHeaderAutoResizeStyle.HeaderSize :
                ColumnHeaderAutoResizeStyle.ColumnContent;
            foreach (ColumnHeader column in this.Columns) {
                switch (column.Width) {
                    case 0:
                        this.AutoResizeColumn(column.Index, resizeToContentStyle);
                        break;
                    case -1:
                        this.AutoResizeColumn(column.Index, ColumnHeaderAutoResizeStyle.HeaderSize);
                        break;
                }
            }
        }

        /// <summary>
        /// Build/rebuild all the list view items in the list, preserving as much state as is possible
        /// </summary>
        public virtual void BuildList() {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(this.BuildList));
            }
            else
            {
                this.BuildList(true);
            }
        }

        /// <summary>
        /// Build/rebuild all the list view items in the list
        /// </summary>
        /// <param name="shouldPreserveState">If this is true, the control will try to preserve the selection,
        /// focused item, and the scroll position (see Remarks)
        /// </param>
        /// <remarks>
        /// <para>
        /// Use this method in situations were the contents of the list is basically the same
        /// as previously.
        /// </para>
        /// </remarks>
        public virtual void BuildList(bool shouldPreserveState) {
            this.InvalidateModelRows();
            if (this.Frozen)
            {
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();

            this.ApplyExtendedStyles();
            this.ClearHotItem();
            int previousTopIndex = this.TopItemIndex;
            Point currentScrollPosition = this.LowLevelScrollPosition;

            IList previousSelection = new ArrayList();
            Object previousFocus = null;
            if (shouldPreserveState && this.StoredModelObjects != null) {
                previousSelection = this.SelectedModelObjects;
                OLVListItem focusedItem = this.FocusedItem as OLVListItem;
                if (focusedItem != null)
                {
                    previousFocus = focusedItem.RowObject;
                }
            }

            IEnumerable objectsToDisplay = this.FilteredObjects;

            this.BeginUpdate();
            try {
                this.ClearGrouping();
                this.Items.Clear();
                this.ListViewItemSorter = null;

                if (objectsToDisplay != null) {
                    // Build a list of all our items and then display them. (Building
                    // a list and then doing one AddRange is about 10-15% faster than individual adds)
                    List<ListViewItem> itemList = new List<ListViewItem>(); // use ListViewItem to avoid co-variant conversion
                    foreach (object rowObject in objectsToDisplay) {
                        OLVListItem lvi = new OLVListItem(rowObject);
                        this.FillInValues(lvi, rowObject);
                        itemList.Add(lvi);
                    }
                    this.Items.AddRange(itemList.ToArray());
                    this.Sort();

                    if (shouldPreserveState) {
                        this.SelectedModelObjects = previousSelection;
                        this.FocusedItem = this.ModelToItem(previousFocus);
                    }
                }
            } finally {
                this.EndUpdate();
            }

            this.RefreshHotItem();

            // We can only restore the scroll position after the EndUpdate() because
            // of caching that the ListView does internally during a BeginUpdate/EndUpdate pair.
            if (shouldPreserveState) {
                // Restore the scroll position. TopItemIndex is best, but doesn't work
                // when the control is grouped.
                if (this.ShowGroups)
                {
                    this.LowLevelScroll(currentScrollPosition.X, currentScrollPosition.Y);
                }
                else
                {
                    this.TopItemIndex = previousTopIndex;
                }
            }

            // System.Diagnostics.Debug.WriteLine(String.Format("PERF - Building list for {2} objects took {0}ms / {1} ticks", sw.ElapsedMilliseconds, sw.ElapsedTicks, this.GetItemCount()));
        }

        /// <summary>
        /// Clear any cached info this list may have been using
        /// </summary>
        public virtual void ClearCachedInfo()
        {
            // ObjectListView doesn't currently cache information but subclass do (or might)
        }

        /// <summary>
        /// Apply all required extended styles to our control.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Whenever .NET code sets an extended style, it erases all other extended styles
        /// that it doesn't use. So, we have to explicit reapply the styles that we have
        /// added.
        /// </para>
        /// <para>
        /// Normally, we would override CreateParms property and update
        /// the ExStyle member, but ListView seems to ignore all ExStyles that
        /// it doesn't already know about. Worse, when we set the LVS_EX_HEADERINALLVIEWS 
        /// value, bad things happen (the control crashes!).
        /// </para>
        /// </remarks>
        protected virtual void ApplyExtendedStyles() {
            const int LVS_EX_SUBITEMIMAGES = 0x00000002;
            //const int LVS_EX_TRANSPARENTBKGND = 0x00400000;
            const int LVS_EX_HEADERINALLVIEWS = 0x02000000;

            const int STYLE_MASK = LVS_EX_SUBITEMIMAGES | LVS_EX_HEADERINALLVIEWS;
            int style = 0;

            if (this.ShowImagesOnSubItems && !this.VirtualMode)
            {
                style ^= LVS_EX_SUBITEMIMAGES;
            }

            if (this.ShowHeaderInAllViews)
            {
                style ^= LVS_EX_HEADERINALLVIEWS;
            }

            NativeMethods.SetExtendedStyle(this, style, STYLE_MASK);
        }

        /// <summary>
        /// Give the listview a reasonable size of its tiles, based on the number of lines of
        /// information that each tile is going to display.
        /// </summary>
        public virtual void CalculateReasonableTileSize() {
            if (this.Columns.Count <= 0)
            {
                return;
            }

            List<OLVColumn> columns = this.AllColumns.FindAll(delegate(OLVColumn x) {
                return (x.Index == 0) || x.IsTileViewColumn;
            });

            int imageHeight = (this.LargeImageList == null ? 16 : this.LargeImageList.ImageSize.Height);
            int dataHeight = (this.Font.Height + 1) * columns.Count;
            int tileWidth = (this.TileSize.Width == 0 ? 200 : this.TileSize.Width);
            int tileHeight = Math.Max(this.TileSize.Height, Math.Max(imageHeight, dataHeight));
            this.TileSize = new Size(tileWidth, tileHeight);
        }

        /// <summary>
        /// Rebuild this list for the given view
        /// </summary>
        /// <param name="view"></param>
        public virtual void ChangeToFilteredColumns(View view) {
            // Store the state
            this.SuspendSelectionEvents();
            IList previousSelection = this.SelectedModelObjects;
            int previousTopIndex = this.TopItemIndex;

            this.Freeze();
            this.Clear();
            List<OLVColumn> columns = this.GetFilteredColumns(view);
            if (view == View.Details || this.ShowHeaderInAllViews) {
                // Make sure all columns have a reasonable LastDisplayIndex
                for (int index = 0; index < columns.Count; index++)
                {
                    if (columns[index].LastDisplayIndex == -1)
                    {
                        columns[index].LastDisplayIndex = index;
                    }
                }
                // ListView will ignore DisplayIndex FOR ALL COLUMNS if there are any errors, 
                // e.g. duplicates (two columns with the same DisplayIndex) or gaps. 
                // LastDisplayIndex isn't guaranteed to be unique, so we just sort the columns by
                // the last position they were displayed and use that to generate a sequence 
                // we can use for the DisplayIndex values.
                IEnumerable<OLVColumn> columnsInDisplayOrder = columns.OrderBy(column => column.LastDisplayIndex);
                int i = 0;
                foreach (OLVColumn col in columnsInDisplayOrder)
                {
                    col.DisplayIndex = i++;
                }
            }

// ReSharper disable once CoVariantArrayConversion
            this.Columns.AddRange(columns.ToArray());
            if (view == View.Details || this.ShowHeaderInAllViews)
            {
                this.ShowSortIndicator();
            }

            this.UpdateFiltering();
            this.Unfreeze();

            // Restore the state
            this.SelectedModelObjects = previousSelection;
            this.TopItemIndex = previousTopIndex;
            this.ResumeSelectionEvents();
        }

        /// <summary>
        /// Remove all items from this list
        /// </summary>
        /// <remark>This method can safely be called from background threads.</remark>
        public virtual void ClearObjects() {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(this.ClearObjects));
            }
            else
            {
                this.SetObjects(null);
            }
        }

        /// <summary>
        /// Reset the memory of which URLs have been visited
        /// </summary>
        public virtual void ClearUrlVisited() {
            this.visitedUrlMap = new Dictionary<string, bool>();
        }

        /// <summary>
        /// Copy a text and html representation of the selected rows onto the clipboard.
        /// </summary>
        /// <remarks>Be careful when using this with virtual lists. If the user has selected
        /// 10,000,000 rows, this method will faithfully try to copy all of them to the clipboard.
        /// From the user's point of view, your program will appear to have hung.</remarks>
        public virtual void CopySelectionToClipboard() {
            IList selection = this.SelectedModelObjects;
            if (selection.Count == 0)
            {
                return;
            }

            // Use the DragSource object to create the data object, if so configured.
            // This relies on the assumption that DragSource will handle the selected objects only.
            // It is legal for StartDrag to return null.
            object data = null;
            if (this.CopySelectionOnControlCUsesDragSource && this.DragSource != null)
            {
                data = this.DragSource.StartDrag(this, MouseButtons.Left, this.ModelToItem(selection[0]));
            }

            Clipboard.SetDataObject(data ?? new OLVDataObject(this, selection));
        }

        /// <summary>
        /// Copy a text and html representation of the given objects onto the clipboard.
        /// </summary>
        /// <remarks>The input is materialized before exporting. An empty sequence leaves
        /// the clipboard unchanged. Call this method on an STA thread.</remarks>
        /// <exception cref="ArgumentNullException">The input sequence is null.</exception>
        public virtual void CopyObjectsToClipboard(IEnumerable objectsToCopy) {
            ArgumentNullException.ThrowIfNull(objectsToCopy, nameof(objectsToCopy));

            ArrayList snapshot = ObjectListViewBase.EnumerableToArray(objectsToCopy, true);
            if (snapshot.Count == 0)
            {
                return;
            }

            // We don't know where these objects came from, so we can't use the DragSource to create
            // the data object, like we do with CopySelectionToClipboard() above.
            OLVDataObject dataObject = new OLVDataObject(this, snapshot);
            dataObject.CreateTextFormats();
            Clipboard.SetDataObject(dataObject);
        }

        /// <summary>
        /// Return a html representation of the given objects
        /// </summary>
        public virtual string ObjectsToHtml(IList objectsToConvert) {
            if (objectsToConvert.Count == 0)
            {
                return String.Empty;
            }

            OLVExporter exporter = new OLVExporter(this, objectsToConvert);
            return exporter.ExportTo(OLVExporter.ExportFormat.HTML);
        }

        /// <summary>
        /// Deselect all rows in the listview
        /// </summary>
        public virtual void DeselectAll() {
            NativeMethods.DeselectAllItems(this);
        }

        /// <summary>
        /// Return the ListViewItem that appears immediately after the given item.
        /// If the given item is null, the first item in the list will be returned.
        /// Return null if the given item is the last item.
        /// </summary>
        /// <param name="itemToFind">The item that is before the item that is returned, or null</param>
        /// <returns>A ListViewItem</returns>
        public virtual OLVListItem GetNextItem(OLVListItem itemToFind) {
            if (this.ShowGroups && this.Groups.Count > 0) {
                bool isFound = (itemToFind == null);
                foreach (ListViewGroup group in this.Groups) {
                    foreach (OLVListItem olvi in group.Items) {
                        if (isFound)
                        {
                            return olvi;
                        }

                        isFound = (itemToFind == olvi);
                    }
                }
                return null;
            }
            if (this.GetItemCount() == 0)
            {
                return null;
            }

            if (itemToFind == null)
            {
                return this.GetItem(0);
            }

            if (itemToFind.Index == this.GetItemCount() - 1)
            {
                return null;
            }

            return this.GetItem(itemToFind.Index + 1);
        }

        /// <summary>
        /// Return the last item in the order they are shown to the user.
        /// If the control is not grouped, the display order is the same as the
        /// sorted list order. But if the list is grouped, the display order is different.
        /// </summary>
        /// <returns></returns>
        public virtual OLVListItem GetLastItemInDisplayOrder() {
            if (!this.ShowGroups || this.Groups.Count == 0)
            {
                return this.GetItem(this.GetItemCount() - 1);
            }

            if (this.Groups.Count > 0) {
                ListViewGroup lastGroup = this.Groups[this.Groups.Count - 1];
                if (lastGroup.Items.Count > 0)
                {
                    return (OLVListItem)lastGroup.Items[lastGroup.Items.Count - 1];
                }
            }

            return null;
        }

        /// <summary>
        /// Return the n'th item (0-based) in the order they are shown to the user.
        /// If the control is not grouped, the display order is the same as the
        /// sorted list order. But if the list is grouped, the display order is different.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public virtual OLVListItem GetNthItemInDisplayOrder(int n) {
            if (!this.ShowGroups || this.Groups.Count == 0)
            {
                return this.GetItem(n);
            }

            foreach (ListViewGroup group in this.Groups) {
                if (n < group.Items.Count)
                {
                    return (OLVListItem)group.Items[n];
                }

                n -= group.Items.Count;
            }

            return null;
        }

        /// <summary>
        /// Return the display index of the given listviewitem index.
        /// If the control is not grouped, the display order is the same as the
        /// sorted list order. But if the list is grouped, the display order is different.
        /// </summary>
        /// <param name="itemIndex"></param>
        /// <returns></returns>
        public virtual int GetDisplayOrderOfItemIndex(int itemIndex) {
            if (!this.ShowGroups || this.Groups.Count == 0)
            {
                return itemIndex;
            }

            // TODO: This could be optimized
            int i = 0;
            foreach (ListViewGroup lvg in this.Groups) {
                foreach (ListViewItem lvi in lvg.Items) {
                    if (lvi.Index == itemIndex)
                    {
                        return i;
                    }

                    i++;
                }
            }

            return -1;
        }

        /// <summary>
        /// Return the ListViewItem that appears immediately before the given item.
        /// If the given item is null, the last item in the list will be returned.
        /// Return null if the given item is the first item.
        /// </summary>
        /// <param name="itemToFind">The item that is before the item that is returned</param>
        /// <returns>A ListViewItem</returns>
        public virtual OLVListItem GetPreviousItem(OLVListItem itemToFind) {
            if (this.ShowGroups && this.Groups.Count > 0) {
                OLVListItem previousItem = null;
                foreach (ListViewGroup group in this.Groups) {
                    foreach (OLVListItem lvi in group.Items) {
                        if (lvi == itemToFind)
                        {
                            return previousItem;
                        }

                        previousItem = lvi;
                    }
                }
                return itemToFind == null ? previousItem : null;
            }
            if (this.GetItemCount() == 0)
            {
                return null;
            }

            if (itemToFind == null)
            {
                return this.GetItem(this.GetItemCount() - 1);
            }

            if (itemToFind.Index == 0)
            {
                return null;
            }

            return this.GetItem(itemToFind.Index - 1);
        }

        /// <summary>
        /// Insert the given collection of objects before the given position
        /// </summary>
        /// <param name="index">Where to insert the objects</param>
        /// <param name="modelObjects">The objects to be inserted</param>
        /// <remarks>
        /// <para>
        /// This operation only makes sense of non-sorted, non-grouped
        /// lists, since any subsequent sort/group operation will rearrange
        /// the list.
        /// </para>
        /// <para>This method only works on ObjectListViews and FastObjectListViews.</para>
        /// <para>The input is copied before events or collection changes. Event handlers
        /// can modify or replace the materialized collection. Null makes no change.</para>
        ///</remarks>
        public virtual void InsertObjects(int index, IEnumerable modelObjects) {
            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate() {
                    this.InsertObjects(index, modelObjects);
                });
                return;
            }
            if (modelObjects == null)
            {
                return;
            }

            ICollection snapshot = this.MaterializeModelObjects(modelObjects);
            this.BeginUpdate();
            try {
                // Give the world a chance to cancel or change the added objects
                ItemsAddingEventArgs args = new ItemsAddingEventArgs(snapshot);
                this.OnItemsAdding(args);
                if (args.Canceled)
                {
                    return;
                }

                snapshot = this.MaterializeModelObjects(args.ObjectsToAdd);

                this.TakeOwnershipOfObjects();

                // If we are filtering the list, there is no way to efficiently
                // insert the objects, so just put them into our collection and rebuild.
                // Sigh -- yet another ListView anomoly. In every view except Details, an item
                // inserted into the Items collection always appear at the end regardless of
                // their actual insertion index.
                if (this.IsFiltering || this.View != View.Details) {
                    this.InsertStoredModelObjects(index, snapshot, true);
                    this.BuildList(true);
                } else {
                    this.ListViewItemSorter = null;
                    index = Math.Max(0, Math.Min(index, this.GetItemCount()));
                    this.InsertStoredModelObjects(index, snapshot, false);
                    int i = index;
                    foreach (object modelObject in snapshot) {
                        if (modelObject != null) {
                            OLVListItem lvi = new OLVListItem(modelObject);
                            this.FillInValues(lvi, modelObject);
                            this.Items.Insert(i, lvi);
                            i++;
                        }
                    }

                    for (i = index; i < this.GetItemCount(); i++) {
                        OLVListItem lvi = this.GetItem(i);
                        this.SetSubItemImages(lvi.Index, lvi);
                    }

                    this.PostProcessRows();
                }

                // Tell the world that the list has changed
                this.SubscribeNotifications(snapshot);
                this.OnItemsChanged(new ItemsChangedEventArgs());
            } finally {
                this.EndUpdate();
            }
        }

        /// <summary>
        /// Return true if the row representing the given model is selected
        /// </summary>
        /// <param name="model">The model object to look for</param>
        /// <returns>Is the row selected</returns>
        public bool IsSelected(object model) {
            OLVListItem item = this.ModelToItem(model);
            return item != null && item.Selected;
        }

        /// <summary>
        /// Has the given URL been visited?
        /// </summary>
        /// <param name="url">The string to be consider</param>
        /// <returns>Has it been visited</returns>
        public virtual bool IsUrlVisited(string url) {
            return this.visitedUrlMap.ContainsKey(url);
        }

        /// <summary>
        /// Scroll the ListView by the given deltas.
        /// </summary>
        /// <param name="dx">Horizontal delta</param>
        /// <param name="dy">Vertical delta</param>
        public void LowLevelScroll(int dx, int dy) {
            NativeMethods.Scroll(this, dx, dy);
        }

        /// <summary>
        /// Return a point that represents the current horizontal and vertical scroll positions 
        /// </summary>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Point LowLevelScrollPosition 
        {
            get {
                return new Point(NativeMethods.GetScrollPosition(this, true), NativeMethods.GetScrollPosition(this, false));
            }
        }

        /// <summary>
        /// Remember that the given URL has been visited
        /// </summary>
        /// <param name="url">The url to be remembered</param>
        /// <remarks>This does not cause the control be redrawn</remarks>
        public virtual void MarkUrlVisited(string url) {
            this.visitedUrlMap[url] = true;
        }

        /// <summary>
        /// Move the given collection of objects to the given index.
        /// </summary>
        /// <remarks>This operation only makes sense on non-grouped ObjectListViews.</remarks>
        /// <param name="index"></param>
        /// <param name="modelObjects"></param>
        public virtual void MoveObjects(int index, ICollection modelObjects) {
            ICollection snapshot = this.MaterializeModelObjects(modelObjects);

            // We are going to remove all the given objects from our list
            // and then insert them at the given location
            this.TakeOwnershipOfObjects();

            List<int> indicesToRemove = new List<int>();
            int displacedObjectCount = 0;
            foreach (object modelObject in snapshot) {
                if (modelObject != null) {
                    int i = this.IndexOf(modelObject);
                    if (i >= 0) {
                        indicesToRemove.Add(i);
                        this.RemoveStoredModelObject(modelObject);
                        // Compare against the original target; displayed items are removed after this loop.
                        if (i < index)
                        {
                            displacedObjectCount++;
                        }
                    }
                }
            }
            index -= displacedObjectCount;

            // Remove the objects in reverse order so earlier
            // deletes don't change the index of later ones
            indicesToRemove.Sort();
            indicesToRemove.Reverse();
            try {
                this.BeginUpdate();
                foreach (int i in indicesToRemove) {
                    this.Items.RemoveAt(i);
                }
                this.InsertObjects(index, snapshot);
            } finally {
                this.EndUpdate();
            }
        }

        /// <summary>
        /// Calculate what item is under the given point?
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public new ListViewHitTestInfo HitTest(int x, int y) {
            // Everything costs something. Playing with the layout of the header can cause problems
            // with the hit testing. If the header shrinks, the underlying control can throw a tantrum.
            try {
                return base.HitTest(x, y);
            } catch (ArgumentOutOfRangeException) {
                return new ListViewHitTestInfo(null, null, ListViewHitTestLocations.None);
            }
        }

        /// <summary>
        /// Perform a hit test using the Windows control's SUBITEMHITTEST message.
        /// This provides information about group hits that the standard ListView.HitTest() does not.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected OlvListViewHitTestInfo LowLevelHitTest(int x, int y) {

            // If it's not even in the control, don't bother with anything else
            if (!this.ClientRectangle.Contains(x, y))
            {
                return new OlvListViewHitTestInfo(null, null, 0, null, 0);
            }

            // If there are no columns, also don't bother with anything else
            if (this.Columns.Count == 0)
            {
                return new OlvListViewHitTestInfo(null, null, 0, null, 0);
            }

            // Is the point over the header?
            OlvListViewHitTestInfo.HeaderHitTestInfo headerHitTestInfo = this.HeaderControl.HitTest(x, y);
            if (headerHitTestInfo != null)
            {
                return new OlvListViewHitTestInfo(this, headerHitTestInfo.ColumnIndex, headerHitTestInfo.IsOverCheckBox, headerHitTestInfo.OverDividerIndex);
            }

            // Call the native hit test method, which is a little confusing.
            NativeMethods.LVHITTESTINFO lParam = new NativeMethods.LVHITTESTINFO();
            lParam.pt_x = x;
            lParam.pt_y = y;
            int index = NativeMethods.HitTest(this, ref lParam);

            // Setup the various values we need to make our hit test structure
            bool isGroupHit = (lParam.flags & (int)HitTestLocationEx.LVHT_EX_GROUP) != 0;
            OLVListItem hitItem = isGroupHit || index == -1 ? null : this.GetItem(index);
            OLVListSubItem subItem = (this.View == View.Details && hitItem != null) ? hitItem.GetSubItem(lParam.iSubItem) : null;

            // Figure out which group is involved in the hit test. This is a little complicated:
            // If the list is virtual:
            //   - the returned value is list view item index
            //   - iGroup is the *index* of the hit group.
            // If the list is not virtual:
            //   - iGroup is always -1.
            //   - if the point is over a group, the returned value is the *id* of the hit group.
            //   - if the point is not over a group, the returned value is list view item index.
            OLVGroup group = null;
            if (this.ShowGroups && this.OLVGroups != null) {
                if (this.VirtualMode) {
                    group = lParam.iGroup >= 0 && lParam.iGroup < this.OLVGroups.Count ? this.OLVGroups[lParam.iGroup] : null;
                } else {
                    if (isGroupHit) {
                        foreach (OLVGroup olvGroup in this.OLVGroups) {
                            if (olvGroup.GroupId == index) {
                                group = olvGroup;
                                break;
                            }
                        }
                    }
                }
            }
            OlvListViewHitTestInfo olvListViewHitTest = new OlvListViewHitTestInfo(hitItem, subItem, lParam.flags, group, lParam.iSubItem);
            // System.Diagnostics.Debug.WriteLine(String.Format("HitTest({0}, {1})=>{2}", x, y, olvListViewHitTest));
            return olvListViewHitTest;
        }

        /// <summary>
        /// What is under the given point? This takes the various parts of a cell into account, including
        /// any custom parts that a custom renderer might use
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>An information block about what is under the point</returns>
        public virtual OlvListViewHitTestInfo OlvHitTest(int x, int y) {
            OlvListViewHitTestInfo hti = this.LowLevelHitTest(x, y);

            // There is a bug/"feature" of the ListView concerning hit testing.
            // If FullRowSelect is false and the point is over cell 0 but not on
            // the text or icon, HitTest will not register a hit. We could turn
            // FullRowSelect on, do the HitTest, and then turn it off again, but
            // toggling FullRowSelect in that way messes up the tooltip in the
            // underlying control. So we have to find another way.
            //
            // It's too hard to try to write the hit test from scratch. Grouping (for
            // example) makes it just too complicated. So, we have to use HitTest
            // but try to get around its limits.
            //
            // First step is to determine if the point was within column 0.
            // If it was, then we only have to determine if there is an actual row
            // under the point. If there is, then we know that the point is over cell 0.
            // So we try a Battleship-style approach: is there a subcell to the right
            // of cell 0? This will return a false negative if column 0 is the rightmost column,
            // so we also check for a subcell to the left. But if only column 0 is visible,
            // then that will fail too, so we check for something at the very left of the
            // control.
            //
            // This will still fail under pathological conditions. If column 0 fills
            // the whole listview and no part of the text column 0 is visible
            // (because it is horizontally scrolled offscreen), then the hit test will fail.

            // Are we in the buggy context? Details view, not full row select, and
            // failing to find anything
            if (hti.Item == null && !this.FullRowSelect && this.View == View.Details) {
                // Is the point within the column 0? If it is, maybe it should have been a hit.
                // Let's test slightly to the right and then to left of column 0. Hopefully one
                // of those will hit a subitem
                Point sides = NativeMethods.GetScrolledColumnSides(this, 0);
                if (x >= sides.X && x <= sides.Y) {
                    // We look for:
                    // - any subitem to the right of cell 0?
                    // - any subitem to the left of cell 0?
                    // - cell 0 at the left edge of the screen
                    hti = this.LowLevelHitTest(sides.Y + 4, y);
                    if (hti.Item == null)
                    {
                        hti = this.LowLevelHitTest(sides.X - 4, y);
                    }

                    if (hti.Item == null)
                    {
                        hti = this.LowLevelHitTest(4, y);
                    }

                    if (hti.Item != null) {
                        // We hit something! So, the original point must have been in cell 0
                        hti.ColumnIndex = 0;
                        hti.SubItem = hti.Item.GetSubItem(0);
                        hti.Location = ListViewHitTestLocations.None;
                        hti.HitTestLocation = HitTestLocation.InCell;
                    }
                }
            }

            if (this.OwnerDraw)
            {
                this.CalculateOwnerDrawnHitTest(hti, x, y);
            }
            else
            {
                this.CalculateStandardHitTest(hti, x, y);
            }

            return hti;
        }

        /// <summary>
        /// Perform a hit test when the control is not owner drawn
        /// </summary>
        /// <param name="hti"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected virtual void CalculateStandardHitTest(OlvListViewHitTestInfo hti, int x, int y) {

            // Standard hit test works fine for the primary column
            if (this.View != View.Details || hti.ColumnIndex == 0 ||
                hti.SubItem == null || hti.Column == null)
            {
                return;
            }

            Rectangle cellBounds = hti.SubItem.Bounds;
            bool hasImage = (this.GetActualImageIndex(hti.SubItem.ImageSelector) != -1);

            // Unless we say otherwise, it was an general incell hit
            hti.HitTestLocation = HitTestLocation.InCell;

            // Check if the point is over where an image should be.
            // If there is a checkbox or image there, tag it and exit.
            Rectangle r = cellBounds;
            r.Width = this.SmallImageSize.Width;
            if (r.Contains(x, y)) {
                if (hti.Column.CheckBoxes) {
                    hti.HitTestLocation = HitTestLocation.CheckBox;
                    return;
                }
                if (hasImage) {
                    hti.HitTestLocation = HitTestLocation.Image;
                    return;
                }
            }

            // Figure out where the text actually is and if the point is in it
            // The standard HitTest assumes that any point inside a subitem is
            // a hit on Text -- which is clearly not true.
            Rectangle textBounds = cellBounds;
            textBounds.X += 4;
            if (hasImage)
            {
                textBounds.X += this.SmallImageSize.Width;
            }

            Size proposedSize = new Size(textBounds.Width, textBounds.Height);
            Size textSize = TextRenderer.MeasureText(hti.SubItem.Text, this.Font, proposedSize, TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            textBounds.Width = textSize.Width;

            switch (hti.Column.TextAlign) {
                case HorizontalAlignment.Center:
                    textBounds.X += (cellBounds.Right - cellBounds.Left - textSize.Width) / 2;
                    break;
                case HorizontalAlignment.Right:
                    textBounds.X = cellBounds.Right - textSize.Width;
                    break;
            }
            if (textBounds.Contains(x, y)) {
                hti.HitTestLocation = HitTestLocation.Text;
            }
        }

        /// <summary>
        /// Perform a hit test when the control is owner drawn. This hands off responsibility
        /// to the renderer.
        /// </summary>
        /// <param name="hti"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected virtual void CalculateOwnerDrawnHitTest(OlvListViewHitTestInfo hti, int x, int y) {
            // If the click wasn't on an item, give up
            if (hti.Item == null)
            {
                return;
            }

            // If the list is showing column, but they clicked outside the columns, also give up
            if (this.View == View.Details && hti.Column == null)
            {
                return;
            }

            // Which renderer was responsible for drawing that point
            IRenderer renderer = this.View == View.Details
                ? this.GetCellRenderer(hti.RowObject, hti.Column)
                : this.ItemRenderer;

            // We can't decide who was responsible. Give up
            if (renderer == null)
            {
                return;
            }

            // Ask the responsible renderer what is at that point
            renderer.HitTest(hti, x, y);
        }

        /// <summary>
        /// Pause (or unpause) all animations in the list
        /// </summary>
        /// <param name="isPause">true to pause, false to unpause</param>
        public virtual void PauseAnimations(bool isPause) {
            for (int i = 0; i < this.Columns.Count; i++) {
                OLVColumn col = this.GetColumn(i);
                ImageRenderer renderer = col.Renderer as ImageRenderer;
                if (renderer != null) {
                    renderer.ListView = this;
                    renderer.Paused = isPause;
                }
            }
        }

        /// <summary>
        /// Rebuild the columns based upon its current view and column visibility settings
        /// </summary>
        public virtual void RebuildColumns() {
            this.ChangeToFilteredColumns(this.View);
        }

        /// <summary>
        /// Remove the given model object from the ListView
        /// </summary>
        /// <param name="modelObject">The model to be removed</param>
        /// <remarks>See RemoveObjects() for more details
        /// <para>This method is thread-safe.</para>
        /// </remarks>
        public virtual void RemoveObject(object modelObject) {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate() { this.RemoveObject(modelObject); });
            }
            else
            {
                this.RemoveObjects(new object[] { modelObject });
            }
        }

        /// <summary>
        /// Remove all of the given objects from the control.
        /// </summary>
        /// <param name="modelObjects">Collection of objects to be removed</param>
        /// <remarks>
        /// <para>Nulls and model objects that are not in the ListView are silently ignored.</para>
        /// <para>This method is thread-safe.</para>
        /// </remarks>
        public virtual void RemoveObjects(IEnumerable modelObjects) {
            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate() { this.RemoveObjects(modelObjects); });
                return;
            }
            if (modelObjects == null)
            {
                return;
            }

            ICollection snapshot = this.MaterializeModelObjects(modelObjects);
            this.BeginUpdate();
            try {
                // Give the world a chance to cancel or change the added objects
                ItemsRemovingEventArgs args = new ItemsRemovingEventArgs(snapshot);
                this.OnItemsRemoving(args);
                if (args.Canceled)
                {
                    return;
                }

                snapshot = this.MaterializeModelObjects(args.ObjectsToRemove);

                this.TakeOwnershipOfObjects();
                foreach (object modelObject in snapshot) {
                    if (modelObject != null) {
                        this.RemoveStoredModelObject(modelObject);
                        int i = this.IndexOf(modelObject);
                        if (i >= 0)
                        {
                            this.Items.RemoveAt(i);
                        }
                    }
                }
                this.PostProcessRows();

                // Tell the world that the list has changed
                this.UnsubscribeNotifications(snapshot);
                this.OnItemsChanged(new ItemsChangedEventArgs());
            } finally {
                this.EndUpdate();
            }
        }

        /// <summary>
        /// Select all rows in the listview
        /// </summary>
        public virtual void SelectAll() {
            NativeMethods.SelectAllItems(this);
        }

        /// <summary>
        /// Set the given image to be fixed in the bottom right of the list view.
        /// This image will not scroll when the list view scrolls.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method uses ListView's native ability to display a background image.
        /// It has a few limitations: 
        /// </para>
        /// <list type="bullet">
        /// <item><description>It doesn't work well with owner drawn mode. In owner drawn mode, each cell draws itself,
        /// including its background, which covers the background image.</description></item>
        /// <item><description>It doesn't look very good when grid lines are enabled, since the grid lines are drawn over the image.</description></item>
        /// <item><description>It does not work at all on XP.</description></item>
        /// <item><description>Obviously, it doesn't look good when alternate row background colors are enabled.</description></item>
        /// </list>
        /// <para>
        /// If you can live with these limitations, native watermarks are quite neat. They are true backgrounds, not
        /// translucent overlays like the OverlayImage uses. They also have the decided advantage over overlays in that
        /// they work correctly even in MDI applications.
        /// </para>
        /// <para>Setting this clears any background image.</para>
        /// </remarks>
        /// <param name="image">The image to be drawn. If null, any existing image will be removed.</param>
        public void SetNativeBackgroundWatermark(Image image) {
            NativeMethods.SetBackgroundImage(this, image, true, false, 0, 0);
        }

        /// <summary>
        /// Set the given image to be background of the ListView so that it appears at the given
        /// percentage offsets within the list.
        /// </summary>
        /// <remarks>
        /// <para>This has the same limitations as described in <see cref="SetNativeBackgroundWatermark"/>. Make sure those limitations
        /// are understood before using the method.</para>
        /// <para>This is very similar to setting the <see cref="System.Windows.Forms.Control.BackgroundImage"/> property of the standard .NET ListView, except that the standard
        /// BackgroundImage does not handle images with transparent areas properly -- it renders transparent areas as black. This 
        /// method does not have that problem.</para>
        /// <para>Setting this clears any background watermark.</para>
        /// </remarks>
        /// <param name="image">The image to be drawn. If null, any existing image will be removed.</param>
        /// <param name="xOffset">The horizontal percentage where the image will be placed. 0 is absolute left, 100 is absolute right.</param>
        /// <param name="yOffset">The vertical percentage where the image will be placed.</param>
        public void SetNativeBackgroundImage(Image image, int xOffset, int yOffset) {
            NativeMethods.SetBackgroundImage(this, image, false, false, xOffset, yOffset);
        }

        /// <summary>
        /// Set the given image to be the tiled background of the ListView.
        /// </summary>
        /// <remarks>
        /// <para>This has the same limitations as described in <see cref="SetNativeBackgroundWatermark"/> and <see cref="SetNativeBackgroundImage"/>.
        /// Make sure those limitations
        /// are understood before using the method.</para>
        /// </remarks>
        /// <param name="image">The image to be drawn. If null, any existing image will be removed.</param>
        public void SetNativeBackgroundTiledImage(Image image) {
            NativeMethods.SetBackgroundImage(this, image, false, true, 0, 0);
        }

        /// <summary>
        /// Set the collection of objects that will be shown in this list view.
        /// </summary>
        /// <remark>This method can safely be called from background threads.</remark>
        /// <remarks>The list is updated immediately</remarks>
        /// <param name="collection">The objects to be displayed</param>
        public virtual void SetObjects(IEnumerable collection) {
            this.SetObjects(collection, false);
        }

        /// <summary>
        /// Set the collection of objects that will be shown in this list view.
        /// </summary>
        /// <remark>This method can safely be called from background threads.</remark>
        /// <remarks>The list is updated immediately</remarks>
        /// <param name="collection">The objects to be displayed</param>
        /// <param name="preserveState">Should the state of the list be preserved as far as is possible.</param>
        public virtual void SetObjects(IEnumerable collection, bool preserveState) {
            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate { this.SetObjects(collection, preserveState); });
                return;
            }

            ICollection snapshot = collection == null ? null : this.MaterializeModelObjects(collection);

            // Give the world a chance to cancel or change the assigned collection
            ItemsChangingEventArgs args = new ItemsChangingEventArgs(this.ModelObjects, snapshot);
            this.OnItemsChanging(args);
            if (args.Canceled)
            {
                return;
            }

            this.SetStoredModelObjects(args.NewObjects == null ? null : this.MaterializeModelObjects(args.NewObjects));
            this.BuildList(preserveState);

            // Tell the world that the list has changed
            this.UpdateNotificationSubscriptions(this.ModelObjects);
            this.OnItemsChanged(new ItemsChangedEventArgs());
        }

        /// <summary>
        /// Update the given model object into the ListView. The model will be added if it doesn't already exist.
        /// </summary>
        /// <param name="modelObject">The model to be updated</param>
        /// <remarks>
        /// <para>
        /// See <see cref="RemoveObjects(IEnumerable)"/> for more details
        /// </para>
        /// <para>This method is thread-safe.</para>
        /// <para>This method will cause the list to be resorted.</para>
        /// <para>This method only works on ObjectListViews and FastObjectListViews.</para>
        /// </remarks>
        public virtual void UpdateObject(object modelObject) {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate() { this.UpdateObject(modelObject); });
            }
            else
            {
                this.UpdateObjects(new object[] { modelObject });
            }
        }

        /// <summary>
        /// Update the pre-existing models that are equal to the given objects. If any of the model doesn't
        /// already exist in the control, they will be added.
        /// </summary>
        /// <param name="modelObjects">Collection of objects to be updated/added</param>
        /// <remarks>
        /// <para>This method will cause the list to be resorted.</para>
        /// <para>Nulls are silently ignored.</para>
        /// <para>This method is thread-safe.</para>
        /// <para>This method only works on ObjectListViews and FastObjectListViews.</para>
        /// </remarks>
        public virtual void UpdateObjects(ICollection modelObjects) {
            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate() { this.UpdateObjects(modelObjects); });
                return;
            }
            if (modelObjects == null || modelObjects.Count == 0)
            {
                return;
            }

            ICollection snapshot = this.MaterializeModelObjects(modelObjects);
            this.BeginUpdate();
            try {
                this.UnsubscribeNotifications(snapshot);

                ArrayList objectsToAdd = new ArrayList();

                this.TakeOwnershipOfObjects();
                foreach (object modelObject in snapshot) {
                    if (modelObject != null) {
                        if (!this.ReplaceStoredModelObject(modelObject, modelObject))
                        {
                            objectsToAdd.Add(modelObject);
                        }
                        else {
                            OLVListItem olvi = this.ModelToItem(modelObject);
                            if (olvi != null) {
                                olvi.RowObject = modelObject;
                                this.RefreshItem(olvi);
                            }
                        }
                    }
                }
                this.PostProcessRows();

                this.AddObjects(objectsToAdd);

                // Tell the world that the list has changed
                this.SubscribeNotifications(snapshot);
                this.OnItemsChanged(new ItemsChangedEventArgs());
            }
            finally {
                this.EndUpdate();
            }
        }

        /// <summary>
        /// Change any subscriptions to INotifyPropertyChanged events on our current
        /// model objects so that we no longer listen for events on the old models
        /// and do listen for events on the given collection.
        /// </summary>
        /// <remarks>This does nothing if UseNotifyPropertyChanged is false.</remarks>
        /// <param name="collection"></param>
        protected virtual void UpdateNotificationSubscriptions(IEnumerable collection) {
            if (!this.UseNotifyPropertyChanged)
            {
                return;
            }

            // We could calculate a symmetric difference between the old models and the new models
            // except that we don't have the previous models at this point.

            this.UnsubscribeNotifications(null);
            this.SubscribeNotifications(collection ?? this.ModelObjects);
        }

        /// <summary>
        /// Gets or sets whether or not ObjectListView should subscribe to INotifyPropertyChanged
        /// events on the model objects that it is given.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This should be set before calling SetObjects(). If you set this to false,
        /// ObjectListView will unsubscribe to all current model objects.
        /// </para>
        /// <para>If you set this to true on a virtual list, the ObjectListView will 
        /// walk all the objects in the list trying to subscribe to change notifications.
        /// If you have 10,000,000 items in your virtual list, this may take some time.</para>
        /// </remarks>
        [Category("ObjectListView"),
        Description("Should ObjectListView listen for property changed events on the model objects?"),
        DefaultValue(false)]
        public bool UseNotifyPropertyChanged {
            get { return this.useNotifyPropertyChanged; }
            set {
                if (this.useNotifyPropertyChanged == value)
                {
                    return;
                }

                this.useNotifyPropertyChanged = value;
                if (value)
                {
                    this.SubscribeNotifications(this.ModelObjects);
                }
                else
                {
                    this.UnsubscribeNotifications(null);
                }
            }
        }
        private bool useNotifyPropertyChanged;

        /// <summary>
        /// Subscribe to INotifyPropertyChanges on the given collection of objects.
        /// </summary>
        /// <param name="models"></param>
        protected void SubscribeNotifications(IEnumerable models) {
            if (!this.UseNotifyPropertyChanged || models == null)
            {
                return;
            }

            foreach (object x in models) {
                INotifyPropertyChanged notifier = x as INotifyPropertyChanged;
                if (notifier != null && !subscribedModels.ContainsKey(notifier)) {
                    notifier.PropertyChanged += HandleModelOnPropertyChanged;
                    subscribedModels[notifier] = notifier;
                }
            }
        }

        /// <summary>
        /// Unsubscribe from INotifyPropertyChanges on the given collection of objects.
        /// If the given collection is null, unsubscribe from all current subscriptions
        /// </summary>
        /// <param name="models"></param>
        protected void UnsubscribeNotifications(IEnumerable models) {
            if (models == null) {
                foreach (INotifyPropertyChanged notifier in this.subscribedModels.Keys) {
                    notifier.PropertyChanged -= HandleModelOnPropertyChanged;
                }
                subscribedModels = new Hashtable();
            } else {
                foreach (object x in models) {
                    INotifyPropertyChanged notifier = x as INotifyPropertyChanged;
                    if (notifier != null) {
                        notifier.PropertyChanged -= HandleModelOnPropertyChanged;
                        subscribedModels.Remove(notifier);
                    }
                }
            }
        }

        private void HandleModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs) {
            // System.Diagnostics.Debug.WriteLine(String.Format("PropertyChanged: '{0}' on '{1}", propertyChangedEventArgs.PropertyName, sender));
            this.RefreshObject(sender);
        }

        private Hashtable subscribedModels = new Hashtable();

        #endregion

        #region OLV accessing

        /// <summary>
        /// Return the column at the given index
        /// </summary>
        /// <param name="index">Index of the column to be returned</param>
        /// <returns>An OLVColumn, or null if the index is out of bounds</returns>
        public virtual OLVColumn GetColumn(int index) {
            return (index >=0 && index < this.Columns.Count) ? (OLVColumn)this.Columns[index] : null;
        }

        /// <summary>
        /// Return the column at the given title.
        /// </summary>
        /// <param name="name">Name of the column to be returned</param>
        /// <returns>An OLVColumn</returns>
        public virtual OLVColumn GetColumn(string name) {
            foreach (ColumnHeader column in this.Columns) {
                if (column.Text == name)
                {
                    return (OLVColumn)column;
                }
            }
            return null;
        }

        /// <summary>
        /// Return a collection of columns that are visible to the given view.
        /// Only Tile and Details have columns; all other views have 0 columns.
        /// </summary>
        /// <param name="view">Which view are the columns being calculate for?</param>
        /// <returns>A list of columns</returns>
        public virtual List<OLVColumn> GetFilteredColumns(View view) {
            // For both detail and tile view, the first column must be included. Normally, we would
            // use the ColumnHeader.Index property, but if the header is not currently part of a ListView
            // that property returns -1. So, we track the index of
            // the column header, and always include the first header.

            int index = 0;
            return this.AllColumns.FindAll(delegate(OLVColumn x) {
                return (index++ == 0) || x.IsVisible;
            });
        }

        /// <summary>
        /// Return the number of items in the list
        /// </summary>
        /// <returns>the number of items in the list</returns>
        /// <remarks>If a filter is installed, this will return the number of items that match the filter.</remarks>
        public virtual int GetItemCount() {
            return this.Items.Count;
        }

        /// <summary>
        /// Return the item at the given index
        /// </summary>
        /// <param name="index">Index of the item to be returned</param>
        /// <returns>An OLVListItem</returns>
        public virtual OLVListItem GetItem(int index) {
            if (index < 0 || index >= this.GetItemCount())
            {
                return null;
            }

            return (OLVListItem)this.Items[index];
        }

        /// <summary>
        /// Return the model object at the given index
        /// </summary>
        /// <param name="index">Index of the model object to be returned</param>
        /// <returns>A model object</returns>
        public virtual object GetModelAt(int index) {
            OLVListItem item = this.GetItem(index);
            return item == null ? null : item.RowObject;
        }

        /// <summary>
        /// Find the item and column that are under the given co-ords
        /// </summary>
        /// <param name="x">X co-ord</param>
        /// <param name="y">Y co-ord</param>
        /// <param name="hitColumn">The column under the given point</param>
        /// <returns>The item under the given point. Can be null.</returns>
        public virtual OLVListItem GetItemAt(int x, int y, out OLVColumn hitColumn) {
            hitColumn = null;
            ListViewHitTestInfo info = this.HitTest(x, y);
            if (info.Item == null)
            {
                return null;
            }

            if (info.SubItem != null) {
                int subItemIndex = info.Item.SubItems.IndexOf(info.SubItem);
                hitColumn = this.GetColumn(subItemIndex);
            }

            return (OLVListItem)info.Item;
        }

        /// <summary>
        /// Return the sub item at the given index/column
        /// </summary>
        /// <param name="index">Index of the item to be returned</param>
        /// <param name="columnIndex">Index of the subitem to be returned</param>
        /// <returns>An OLVListSubItem</returns>
        public virtual OLVListSubItem GetSubItem(int index, int columnIndex) {
            OLVListItem olvi = this.GetItem(index);
            return olvi == null ? null : olvi.GetSubItem(columnIndex);
        }

        #endregion

        #region Object manipulation

        /// <summary>
        /// Scroll the listview so that the given group is at the top.
        /// </summary>
        /// <param name="lvg">The group to be revealed</param>
        /// <remarks><para>
        /// If the group is already visible, the list will still be scrolled to move
        /// the group to the top, if that is possible.
        /// </para>
        /// <para>This only works when the list is showing groups (obviously).</para>
        /// <para>This does not work on virtual lists, since virtual lists don't use ListViewGroups
        /// for grouping. Use <see cref="TypedVirtualObjectListView{T}.EnsureNthGroupVisible"/> instead.</para>
        /// </remarks>
        public virtual void EnsureGroupVisible(ListViewGroup lvg) {
            if (!this.ShowGroups || lvg == null)
            {
                return;
            }

            int groupIndex = this.Groups.IndexOf(lvg);
            if (groupIndex <= 0) {
                // There is no easy way to scroll back to the beginning of the list
                int delta = 0 - NativeMethods.GetScrollPosition(this, false);
                NativeMethods.Scroll(this, 0, delta);
            } else {
                // Find the display rectangle of the last item in the previous group
                ListViewGroup previousGroup = this.Groups[groupIndex - 1];
                ListViewItem lastItemInGroup = previousGroup.Items[previousGroup.Items.Count - 1];
                Rectangle r = this.GetItemRect(lastItemInGroup.Index);

                // Scroll so that the last item of the previous group is just out of sight,
                // which will make the desired group header visible.
                int delta = r.Y + r.Height / 2;
                NativeMethods.Scroll(this, 0, delta);
            }
        }

        /// <summary>
        /// Ensure that the given model object is visible
        /// </summary>
        /// <param name="modelObject">The model object to be revealed</param>
        public virtual void EnsureModelVisible(Object modelObject) {
            int index = this.IndexOf(modelObject);
            if (index >= 0)
            {
                this.EnsureVisible(index);
            }
        }

        /// <summary>
        /// Return the model object of the row that is selected or null if there is no selection or more than one selection
        /// </summary>
        /// <returns>Model object or null</returns>
        [Obsolete("Use SelectedObject property instead of this method")]
        public virtual object GetSelectedObject() {
            return this.SelectedModelObject;
        }

        /// <summary>
        /// Return the model objects of the rows that are selected or an empty collection if there is no selection
        /// </summary>
        /// <returns>ArrayList</returns>
        [Obsolete("Use SelectedObjects property instead of this method")]
        public virtual ArrayList GetSelectedObjects() {
            return ObjectListViewBase.EnumerableToArray(this.SelectedModelObjects, false);
        }

        /// <summary>
        /// Return the model object of the row that is checked or null if no row is checked
        /// or more than one row is checked
        /// </summary>
        /// <returns>Model object or null</returns>
        /// <remarks>Use CheckedObject property instead of this method</remarks>
        [Obsolete("Use CheckedObject property instead of this method")]
        public virtual object GetCheckedObject() {
            return this.CheckedModelObject;
        }

        /// <summary>
        /// Get the collection of model objects that are checked.
        /// </summary>
        /// <remarks>Use CheckedObjects property instead of this method</remarks>
        [Obsolete("Use CheckedObjects property instead of this method")]
        public virtual ArrayList GetCheckedObjects() {
            return ObjectListViewBase.EnumerableToArray(this.CheckedModelObjects, false);
        }

        /// <summary>
        /// Find the given model object within the listview and return its index
        /// </summary>
        /// <param name="modelObject">The model object to be found</param>
        /// <returns>The index of the object. -1 means the object was not present</returns>
        public virtual int IndexOf(Object modelObject) {
            for (int i = 0; i < this.GetItemCount(); i++) {
                if (this.GetModelAt(i).Equals(modelObject))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Rebuild the given ListViewItem with the data from its associated model.
        /// </summary>
        /// <remarks>This method does not resort or regroup the view. It simply updates
        /// the displayed data of the given item</remarks>
        public virtual void RefreshItem(OLVListItem olvi) {
            olvi.UseItemStyleForSubItems = true;
            olvi.SubItems.Clear();
            this.FillInValues(olvi, olvi.RowObject);
            this.PostProcessOneRow(olvi.Index, this.GetDisplayOrderOfItemIndex(olvi.Index), olvi);
        }

        /// <summary>
        /// Rebuild the data on the row that is showing the given object.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method does not resort or regroup the view.
        /// </para>
        /// <para>
        /// The given object is *not* used as the source of data for the rebuild.
        /// It is only used to locate the matching model in the <see cref="ModelObjects"/> collection,
        /// then that matching model is used as the data source. This distinction is
        /// only important in model classes that have overridden the Equals() method.
        /// </para>
        /// <para>
        /// If you want the given model object to replace the pre-existing model,
        /// use <see cref="UpdateObject"/>. 
        /// </para>
        /// </remarks>
        public virtual void RefreshObject(object modelObject) {
            this.RefreshObjects(new object[] { modelObject });
        }

        /// <summary>
        /// Update the rows that are showing the given objects
        /// </summary>
        /// <remarks>
        /// <para>This method does not resort or regroup the view.</para>
        /// <para>This method can safely be called from background threads.</para>
        /// <para>The input is materialized on the control thread before any rows are refreshed.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">The input sequence is null.</exception>
        public virtual void RefreshObjects(IEnumerable modelObjects) {
            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate { this.RefreshObjects(modelObjects); });
                return;
            }
            ArgumentNullException.ThrowIfNull(modelObjects, nameof(modelObjects));

            ICollection snapshot = this.MaterializeModelObjects(modelObjects);
            foreach (object modelObject in snapshot) {
                OLVListItem olvi = this.ModelToItem(modelObject);
                if (olvi != null) {
                    this.ReplaceModel(olvi, modelObject);
                    this.RefreshItem(olvi);
                }
            }
        }

        private void ReplaceModel(OLVListItem olvi, object newModel) {
            if (ReferenceEquals(olvi.RowObject, newModel))
            {
                return;
            }

            this.TakeOwnershipOfObjects();
            this.ReplaceStoredModelObject(olvi.RowObject, newModel);

            olvi.RowObject = newModel;
        }

        /// <summary>
        /// Update the rows that are selected
        /// </summary>
        /// <remarks>This method does not resort or regroup the view.</remarks>
        public virtual void RefreshSelectedObjects() {
            foreach (ListViewItem lvi in this.SelectedItems)
            {
                this.RefreshItem((OLVListItem)lvi);
            }
        }

        /// <summary>
        /// Select the row that is displaying the given model object, in addition to any current selection.
        /// </summary>
        /// <param name="modelObject">The object to be selected</param>
        /// <remarks>Use the <see cref="SelectedModelObject"/> property to deselect all other rows</remarks>
        public virtual void SelectObject(object modelObject) {
            this.SelectObject(modelObject, false);
        }

        /// <summary>
        /// Select the row that is displaying the given model object, in addition to any current selection.
        /// </summary>
        /// <param name="modelObject">The object to be selected</param>
        /// <param name="setFocus">Should the object be focused as well?</param>
        /// <remarks>Use the <see cref="SelectedModelObject"/> property to deselect all other rows</remarks>
        public virtual void SelectObject(object modelObject, bool setFocus) {
            OLVListItem olvi = this.ModelToItem(modelObject);
            if (olvi != null && olvi.Enabled) {
                olvi.Selected = true;
                if (setFocus)
                {
                    olvi.Focused = true;
                }
            }
        }

        /// <summary>
        /// Select the given model objects and deselect all other objects.
        /// </summary>
        /// <param name="modelObjects">The model objects to select, or null to clear selection.</param>
        /// <remarks>Delegates to SelectObjects so existing overrides are honored.
        /// The input is consumed before clearing selection. Call this method on the control thread.</remarks>
        public virtual void SetSelectedObjects(IEnumerable modelObjects) {
            this.SelectObjects(modelObjects);
        }

        /// <summary>
        /// Select the rows that is displaying any of the given model object. All other rows are deselected.
        /// </summary>
        /// <param name="modelObjects">A collection of model objects</param>
        public virtual void SelectObjects(IEnumerable modelObjects) {
            ICollection snapshot = this.MaterializeModelObjects(modelObjects);
            this.SelectedIndices.Clear();

            foreach (object modelObject in snapshot) {
                OLVListItem olvi = this.ModelToItem(modelObject);
                if (olvi != null && olvi.Enabled)
                {
                    olvi.Selected = true;
                }
            }
        }

        #endregion

        #region Freezing/Suspending

        /// <summary>
        /// Get or set whether or not the listview is frozen. When the listview is
        /// frozen, it will not update itself.
        /// </summary>
        /// <remarks><para>The Frozen property is similar to the methods Freeze()/Unfreeze()
        /// except that setting Frozen property to false immediately unfreezes the control
        /// regardless of the number of Freeze() calls outstanding.</para></remarks>
        /// <example>objectListView1.Frozen = false; // unfreeze the control now!
        /// </example>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual bool Frozen {
            get { return freezeCount > 0; }
            set {
                if (value)
                {
                    Freeze();
                }
                else if (freezeCount > 0) {
                    freezeCount = 1;
                    Unfreeze();
                }
            }
        }
        private int freezeCount;

        /// <summary>
        /// Freeze the listview so that it no longer updates itself.
        /// </summary>
        /// <remarks>Freeze()/Unfreeze() calls nest correctly</remarks>
        public virtual void Freeze() {
            if (freezeCount == 0)
            {
                DoFreeze();
            }

            freezeCount++;
            this.OnFreezing(new FreezeEventArgs(freezeCount));
        }

        /// <summary>
        /// Unfreeze the listview. If this call is the outermost Unfreeze(),
        /// the contents of the listview will be rebuilt.
        /// </summary>
        /// <remarks>Freeze()/Unfreeze() calls nest correctly</remarks>
        public virtual void Unfreeze() {
            if (freezeCount <= 0)
            {
                return;
            }

            freezeCount--;
            if (freezeCount == 0)
            {
                DoUnfreeze();
            }

            this.OnFreezing(new FreezeEventArgs(freezeCount));
        }

        /// <summary>
        /// Do the actual work required when the listview is frozen
        /// </summary>
        protected virtual void DoFreeze() {
            this.BeginUpdate();
        }

        /// <summary>
        /// Do the actual work required when the listview is unfrozen
        /// </summary>
        protected virtual void DoUnfreeze()
        {
            this.EndUpdate();
            this.ResizeFreeSpaceFillingColumns();
            this.BuildList();
        }

        /// <summary>
        /// Returns true if selection events are currently suspended.
        /// While selection events are suspended, neither SelectedIndexChanged
        /// or SelectionChanged events will be raised.
        /// </summary>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected bool SelectionEventsSuspended {
            get { return this.suspendSelectionEventCount > 0; }
        }

        /// <summary>
        /// Suspend selection events until a matching ResumeSelectionEvents()
        /// is called.
        /// </summary>
        /// <remarks>Calls to this method nest correctly. Every call to SuspendSelectionEvents()
        /// must have a matching ResumeSelectionEvents().</remarks>
        protected void SuspendSelectionEvents() {
            this.suspendSelectionEventCount++;
        }

        /// <summary>
        /// Resume raising selection events.
        /// </summary>
        protected void ResumeSelectionEvents() {
            Debug.Assert(this.SelectionEventsSuspended, "Mismatched called to ResumeSelectionEvents()");
            this.suspendSelectionEventCount--;
        }

        /// <summary>
        /// Returns a disposable that will disable selection events
        /// during a using() block.
        /// </summary>
        /// <returns></returns>
        protected IDisposable SuspendSelectionEventsDuring() {
            return new SuspendSelectionDisposable(this);
        }

        /// <summary>
        /// Implementation only class that suspends and resumes selection
        /// events on instance creation and disposal.
        /// </summary>
        private sealed class SuspendSelectionDisposable : IDisposable {
            public SuspendSelectionDisposable(ObjectListViewBase objectListView) {
                this.objectListView = objectListView;
                this.objectListView.SuspendSelectionEvents();
            }

            public void Dispose() {
                this.objectListView.ResumeSelectionEvents();
            }

            private readonly ObjectListViewBase objectListView;
        }

        #endregion

        #region Column sorting

        /// <summary>
        /// Sort the items by the last sort column and order
        /// </summary>
        public new void Sort() {
            this.Sort(this.sortDescriptions.GetColumn(0), this.sortDescriptions.GetOrder(0));
        }

        /// <summary>
        /// Sort the items in the list view by the values in the given column and the last sort order
        /// </summary>
        /// <param name="columnToSortName">The name of the column whose values will be used for the sorting</param>
        public virtual void Sort(string columnToSortName) {
            this.Sort(this.GetColumn(columnToSortName));
        }

        /// <summary>
        /// Sort the items in the list view by the values in the given column and the last sort order
        /// </summary>
        /// <param name="columnToSortIndex">The index of the column whose values will be used for the sorting</param>
        public virtual void Sort(int columnToSortIndex) {
            if (columnToSortIndex >= 0 && columnToSortIndex < this.Columns.Count)
            {
                this.Sort(this.GetColumn(columnToSortIndex));
            }
        }

        /// <summary>
        /// Sort the items in the list view by the values in the given column and the last sort order
        /// </summary>
        /// <param name="columnToSort">The column whose values will be used for the sorting</param>
        public virtual void Sort(OLVColumn columnToSort) {
            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate { this.Sort(columnToSort); });
            } else {
                SortOrder order = this.sortDescriptions.GetOrder(0) == SortOrder.None ? this.Sorting : this.sortDescriptions.GetOrder(0);
                this.Sort(columnToSort, order == SortOrder.None ? SortOrder.Ascending : order);
            }
        }

        /// <summary>
        /// Sort the items in the list view by the values in the given column and by the given order.
        /// </summary>
        /// <param name="columnToSort">The column whose values will be used for the sorting.
        /// If null, the first column will be used.</param>
        /// <param name="order">The ordering to use. None preserves item order and disables
        /// automatic item sorting, including within groups.</param>
        /// <remarks>Grouping follows Grouping configuration independently of item sorting.</remarks>
        public virtual void Sort(OLVColumn columnToSort, SortOrder order) {
            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate { this.Sort(columnToSort, order); });
            } else {
                this.DoSort(this.requestedSortDescriptions ?? this.sortDescriptions.WithPrimary(columnToSort, order));
                this.PostProcessRows();
            }
        }

        /// <summary>
        /// Sort by a column, optionally adding it to the existing sort.
        /// An existing column keeps its priority and receives the given direction.
        /// </summary>
        public virtual void Sort(OLVColumn columnToSort, SortOrder order, bool addToSort) {
            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate { this.Sort(columnToSort, order, addToSort); });
                return;
            }
            ArgumentNullException.ThrowIfNull(columnToSort, nameof(columnToSort));
            List<OLVSortDescription> descriptions = addToSort
                ? new List<OLVSortDescription>(this.SortDescriptions) : new List<OLVSortDescription>();
            int index = descriptions.FindIndex(x => x.Column == columnToSort);
            OLVSortDescription description = new OLVSortDescription(columnToSort, order);
            if (index < 0)
            {
                descriptions.Add(description);
            }
            else
            {
                descriptions[index] = description;
            }

            this.Sort(descriptions);
        }

        /// <summary>
        /// Replace the sort with the given fields in priority order. An empty collection removes sorting.
        /// </summary>
        public virtual void Sort(IEnumerable<OLVSortDescription> sortDescriptions) {
            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate { this.Sort(sortDescriptions); });
                return;
            }
            SortDescriptionCollection descriptions = new SortDescriptionCollection(sortDescriptions);
            if (descriptions.Count == 0) {
                if (this.ShowGroups && this.CanShowGroups) {
                    this.DoSort(descriptions);
                    this.PostProcessRows();
                } else {
                    this.Unsort();
                }
                return;
            }
            SortDescriptionCollection previous = this.requestedSortDescriptions;
            this.requestedSortDescriptions = descriptions;
            try {
                this.Sort(descriptions.GetColumn(0), descriptions.GetOrder(0));
            } finally {
                this.requestedSortDescriptions = previous;
            }
        }

        internal ModelObjectComparer CreateModelObjectComparer(OLVColumn column, SortOrder order) {
            return new ModelObjectComparer(this.sortOperation?.Descriptions ??
                this.sortDescriptions.WithPrimary(column, order).GetActiveDescriptions());
        }

        private BeforeSortingEventArgs BuildBeforeSortingEventArgs(SortDescriptionCollection descriptions, bool buildGroups) {
            OLVColumn groupBy = null;
            SortOrder groupByOrder = SortOrder.None;
            if (buildGroups)
            {
                this.ResolveGrouping(descriptions, out groupBy, out groupByOrder);
            }
            BeforeSortingEventArgs args = new BeforeSortingEventArgs(groupBy, groupByOrder, descriptions);
            foreach (OLVSortDescription description in descriptions.GetActiveDescriptions())
            {
                args.Canceled |= !description.Column.Sortable;
            }
            return args;
        }

        private void DoSort(SortDescriptionCollection descriptions, bool forceGroups = false) {
            if (this.Columns.Count == 0)
            {
                return;
            }
            if (forceGroups && !this.CanShowGroups)
            {
                throw new NotSupportedException("This control does not support grouping without a grouping strategy.");
            }
            bool buildGroups = forceGroups || (this.ShowGroups && this.CanShowGroups);

            // Give the world a chance to fiddle with or completely avoid the sorting process
            BeforeSortingEventArgs args = this.BuildBeforeSortingEventArgs(descriptions, buildGroups);
            this.OnBeforeSorting(args);
            if (args.Canceled)
            {
                return;
            }
            if (buildGroups) {
                this.ResolveGrouping(new SortDescriptionCollection(args.SortDescriptions),
                    out OLVColumn groupColumn, out SortOrder groupOrder);
                if (!args.GroupingColumnChanged)
                {
                    args.ColumnToGroupBy = groupColumn;
                }
                if (!args.GroupingOrderChanged)
                {
                    args.GroupByOrder = groupOrder;
                }
            }

            SortOperation previous = this.sortOperation;
            SortOperation operation = new SortOperation(args.GetActiveSortDescriptions());
            this.sortOperation = operation;
            try {
                // Virtual lists don't preserve selection, so we have to do it specifically
                IList selection = this.VirtualMode ? this.SelectedModelObjects : null;
                this.SuspendSelectionEvents();
                try {
                    this.ClearHotItem();

                    // An event handler may have already performed the sort.
                    if (!args.Handled) {
                        if (buildGroups && args.ColumnToGroupBy != null) {
                            this.BuildGroups(args.ColumnToGroupBy, args.GroupByOrder, args.SortDescriptions);
                            if (operation.Canceled)
                            {
                                return;
                            }
                        } else {
                            if (buildGroups) {
                                this.ClearGrouping();
                            }
                            if (operation.Descriptions.Count == 0 && !this.VirtualMode)
                            {
                                this.ListViewItemSorter = null;
                            }
                            if (operation.Descriptions.Count > 0) {
                                if (this.CustomSorter != null)
                                {
                                    this.CustomSorter(args.ColumnToSort, args.SortOrder);
                                }
                                else
                                {
                                    this.ListViewItemSorter = new ColumnComparer(operation.Descriptions);
                                }
                            }
                        }
                    }

                    this.sortDescriptions.ReplaceWith(operation.Descriptions.Count > 0
                        ? operation.Descriptions : args.SortDescriptions);
                    if (this.TintSortColumn)
                    {
                        this.SelectedColumn = this.sortDescriptions.GetColumn(0);
                    }

                    if (this.ShowSortIndicators)
                    {
                        this.ShowSortIndicator(args.ColumnToSort, args.SortOrder);
                    }

                    if (selection != null && selection.Count > 0)
                    {
                        this.SelectedModelObjects = selection;
                    }
                } finally {
                    this.ResumeSelectionEvents();
                }

                this.RefreshHotItem();
                operation.Commit();
                this.OnAfterSorting(new AfterSortingEventArgs(args));
            } finally {
                this.sortOperation = previous;
            }
        }

        /// <summary>
        /// Put a sort indicator next to the text of the sort column
        /// </summary>
        public virtual void ShowSortIndicator() {
            if (this.ShowSortIndicators) {
                IList<OLVSortDescription> descriptions = this.SortDescriptions;
                this.ShowSortIndicator(descriptions.Count > 0 ? descriptions[0].Column : null,
                    descriptions.Count > 0 ? descriptions[0].SortOrder : SortOrder.None);
            }
        }

        /// <summary>
        /// Put a sort indicator next to the text of the given column
        /// </summary>
        /// <param name="columnToSort">The column to be marked</param>
        /// <param name="sortOrder">The sort order in effect on that column</param>
        protected virtual void ShowSortIndicator(OLVColumn columnToSort, SortOrder sortOrder) {
            int imageIndex = -1;

            if (!NativeMethods.HasBuiltinSortIndicators()) {
                // If we can't use builtin image, we have to make and then locate the index of the
                // sort indicator we want to use. SortOrder.None doesn't show an image.
                if (this.SmallImageList == null || !this.SmallImageList.Images.ContainsKey(SORT_INDICATOR_UP_KEY))
                {
                    MakeSortIndicatorImages();
                }

                if (this.SmallImageList != null)
                {
                    string key = sortOrder == SortOrder.Ascending ? SORT_INDICATOR_UP_KEY : SORT_INDICATOR_DOWN_KEY;
                    imageIndex = this.SmallImageList.Images.IndexOfKey(key);
                }
            }

            // Set the image for each column
            for (int i = 0; i < this.Columns.Count; i++) {
                if (columnToSort != null && i == columnToSort.Index)
                {
                    NativeMethods.SetColumnImage(this, i, sortOrder, imageIndex);
                }
                else
                {
                    NativeMethods.SetColumnImage(this, i, SortOrder.None, -1);
                }
            }
            this.HeaderControl.Invalidate();
        }

        /// <summary>
        /// The name of the image used when a column is sorted ascending
        /// </summary>
        /// <remarks>This image is only used on pre-XP systems. System images are used for XP and later</remarks>
        public const string SORT_INDICATOR_UP_KEY = "sort-indicator-up";

        /// <summary>
        /// The name of the image used when a column is sorted descending
        /// </summary>
        /// <remarks>This image is only used on pre-XP systems. System images are used for XP and later</remarks>
        public const string SORT_INDICATOR_DOWN_KEY = "sort-indicator-down";

        /// <summary>
        /// If the sort indicator images don't already exist, this method will make and install them
        /// </summary>
        protected virtual void MakeSortIndicatorImages() {
            // Don't mess with the image list in design mode
            if (this.DesignMode)
            {
                return;
            }

            ImageList il = this.SmallImageList;
            if (il == null) {
                il = new ImageList();
                il.ImageSize = new Size(16, 16);
                il.ColorDepth = ColorDepth.Depth32Bit;
            }

            // This arrangement of points works well with (16,16) images, and OK with others
            int midX = il.ImageSize.Width / 2;
            int midY = (il.ImageSize.Height / 2) - 1;
            int deltaX = midX - 2;
            int deltaY = deltaX / 2;

            if (il.Images.IndexOfKey(SORT_INDICATOR_UP_KEY) == -1) {
                Point pt1 = new Point(midX - deltaX, midY + deltaY);
                Point pt2 = new Point(midX, midY - deltaY - 1);
                Point pt3 = new Point(midX + deltaX, midY + deltaY);
                il.Images.Add(SORT_INDICATOR_UP_KEY, MakeTriangleBitmap(il.ImageSize, new Point[] { pt1, pt2, pt3 }));
            }

            if (il.Images.IndexOfKey(SORT_INDICATOR_DOWN_KEY) == -1) {
                Point pt1 = new Point(midX - deltaX, midY - deltaY);
                Point pt2 = new Point(midX, midY + deltaY);
                Point pt3 = new Point(midX + deltaX, midY - deltaY);
                il.Images.Add(SORT_INDICATOR_DOWN_KEY, MakeTriangleBitmap(il.ImageSize, new Point[] { pt1, pt2, pt3 }));
            }

            this.SmallImageList = il;
        }

        private static Bitmap MakeTriangleBitmap(Size sz, Point[] pts) {
            Bitmap bm = new Bitmap(sz.Width, sz.Height);
            Graphics g = Graphics.FromImage(bm);
            g.FillPolygon(new SolidBrush(Color.Gray), pts);
            return bm;
        }

        /// <summary>
        /// Remove any sorting and revert to the given order of the model objects
        /// </summary>
        public virtual void Unsort() {
            this.ShowGroups = false;
            this.sortDescriptions.Clear();
            if (this.TintSortColumn)
            {
                this.SelectedColumn = null;
            }

            if (this.ShowSortIndicators)
            {
                this.ShowSortIndicator(null, SortOrder.None);
            }

            this.BuildList();
        }

        #endregion

        #region Filtering

        /// <summary>
        /// Create a filter that will enact all the filtering currently installed
        /// on the visible columns.
        /// </summary>
        public virtual IModelFilter CreateColumnFilter() {
            List<IModelFilter> filters = new List<IModelFilter>();
            foreach (OLVColumn column in this.Columns) {
                IModelFilter filter = column.ValueBasedFilter;
                if (filter != null)
                {
                    filters.Add(filter);
                }
            }
            return (filters.Count == 0) ? null : new CompositeAllFilter(filters);
        }

        /// <summary>
        /// Do the actual work of filtering
        /// </summary>
        /// <param name="originalObjects"></param>
        /// <param name="aModelFilter"></param>
        /// <param name="aListFilter"></param>
        /// <returns></returns>
        protected virtual IEnumerable FilterObjects(IEnumerable originalObjects, IModelFilter aModelFilter, IListFilter aListFilter) {
            // Being cautious
            originalObjects = originalObjects ?? new ArrayList();

            // Tell the world to filter the objects. If they do so, don't do anything else
// ReSharper disable PossibleMultipleEnumeration
            FilterEventArgs args = new FilterEventArgs(originalObjects);
            this.OnFilter(args);
            if (args.FilteredObjects != null)
            {
                return args.FilteredObjects;
            }

            // Apply a filter to the list as a whole
            if (aListFilter != null)
            {
                originalObjects = aListFilter.Filter(originalObjects);
            }

            // Apply the object filter if there is one
            if (aModelFilter != null) {
                ArrayList filteredObjects = new ArrayList();
                foreach (object model in originalObjects) {
                    if (aModelFilter.Filter(model))
                    {
                        filteredObjects.Add(model);
                    }
                }
                originalObjects = filteredObjects;
            }

            return originalObjects;
// ReSharper restore PossibleMultipleEnumeration
        }

        /// <summary>
        /// Remove all column filtering.
        /// </summary>
        public virtual void ResetColumnFiltering() {
            foreach (OLVColumn column in this.Columns) {
                column.ValuesChosenForFiltering.Clear();
            }
            this.UpdateColumnFiltering();
        }

        /// <summary>
        /// Update the filtering of this ObjectListView based on the value filtering
        /// defined in each column
        /// </summary>
        public virtual void UpdateColumnFiltering() {
            //List<IModelFilter> filters = new List<IModelFilter>();
            //IModelFilter columnFilter = this.CreateColumnFilter();
            //if (columnFilter != null)
            //    filters.Add(columnFilter);
            //if (this.AdditionalFilter != null)
            //    filters.Add(this.AdditionalFilter);
            //this.ModelFilter = filters.Count == 0 ? null : new CompositeAllFilter(filters);

            if (this.AdditionalFilter == null)
            {
                this.ModelFilter = this.CreateColumnFilter();
            }
            else {
                IModelFilter columnFilter = this.CreateColumnFilter();
                if (columnFilter == null)
                {
                    this.ModelFilter = this.AdditionalFilter;
                }
                else {
                    List<IModelFilter> filters = new List<IModelFilter>();
                    filters.Add(columnFilter);
                    filters.Add(this.AdditionalFilter);
                    this.ModelFilter = new CompositeAllFilter(filters);
                }
            }
        }

        /// <summary>
        /// When some setting related to filtering changes, this method is called.
        /// </summary>
        protected virtual void UpdateFiltering() {
            this.BuildList(true);
        }

        /// <summary>
        /// Update all renderers with the currently installed model filter
        /// </summary>
        protected virtual void NotifyNewModelFilter() {
            IFilterAwareRenderer filterAware = this.DefaultRenderer as IFilterAwareRenderer;
            if (filterAware != null)
            {
                filterAware.Filter = this.ModelFilter;
            }

            foreach (OLVColumn column in this.AllColumns) {
                filterAware = column.Renderer as IFilterAwareRenderer;
                if (filterAware != null)
                {
                    filterAware.Filter = this.ModelFilter;
                }
            }
        }

        #endregion
    }
}
