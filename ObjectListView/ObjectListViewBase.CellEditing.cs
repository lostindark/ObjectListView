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
        #region Cell editing

        /// <summary>
        /// Should we start editing the cell in response to the given mouse button event?
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual bool ShouldStartCellEdit(MouseEventArgs e) {
            if (this.IsCellEditing)
            {
                return false;
            }

            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right)
            {
                return false;
            }

            if ((Control.ModifierKeys & (Keys.Shift | Keys.Control | Keys.Alt)) != 0)
            {
                return false;
            }

            if (this.lastMouseDownClickCount == 1 && (
                this.CellEditActivation == CellEditActivateMode.SingleClick ||
                this.CellEditActivation == CellEditActivateMode.SingleClickAlways))
            {
                return true;
            }

            return (this.lastMouseDownClickCount == 2 && this.CellEditActivation == CellEditActivateMode.DoubleClick);
        }

        /// <summary>
        /// Handle a key press on this control. We specifically look for F2 which edits the primary column,
        /// or a Tab character during an edit operation, which tries to start editing on the next (or previous) cell.
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessDialogKey(Keys keyData) {

            if (this.IsCellEditing)
            {
                return this.CellEditKeyEngine.HandleKey(this, keyData);
            }

            // Treat F2 as a request to edit the primary column
            if (keyData == Keys.F2) {
                this.EditSubItem((OLVListItem)this.FocusedItem, 0);
                return base.ProcessDialogKey(keyData);
            }

            // Treat Ctrl-C as Copy To Clipboard. 
            if (this.CopySelectionOnControlC && keyData == (Keys.C | Keys.Control)) {
                this.CopySelectionToClipboard();
                return true;
            }

            // Treat Ctrl-A as Select All.
            if (this.SelectAllOnControlA && keyData == (Keys.A | Keys.Control)) {
                this.SelectAll();
                return true;
            }

            return base.ProcessDialogKey(keyData);
        }
        
        /// <summary>
        /// Start an editing operation on the first editable column of the given model.
        /// </summary>
        /// <param name="rowModel"></param>
        /// <remarks>
        /// <para>
        /// If the model doesn't exist, or there are no editable columns, this method
        /// will do nothing.</para>
        /// <para>
        /// This will start an edit operation regardless of CellActivationMode.
        /// </para>
        /// </remarks>
        public virtual void EditModel(object rowModel) {
            OLVListItem olvItem = this.ModelToItem(rowModel);
            if (olvItem == null)
            {
                return;
            }

            for (int i = 0; i < olvItem.SubItems.Count; i++) {
                var olvColumn = this.GetColumn(i);
                if (olvColumn != null && olvColumn.IsEditable) {
                    this.StartCellEdit(olvItem, i);
                    return;
                }
            }
        }

        /// <summary>
        /// Begin an edit operation on the given cell.
        /// </summary>
        /// <remarks>This performs various sanity checks and passes off the real work to StartCellEdit().</remarks>
        /// <param name="item">The row to be edited</param>
        /// <param name="subItemIndex">The index of the cell to be edited</param>
        public virtual void EditSubItem(OLVListItem item, int subItemIndex) {
            if (item == null)
            {
                return;
            }

            if (this.CellEditActivation == CellEditActivateMode.None)
            {
                return;
            }

            OLVColumn olvColumn = this.GetColumn(subItemIndex);
            if (olvColumn == null || !olvColumn.IsEditable)
            {
                return;
            }

            if (!item.Enabled)
            {
                return;
            }

            this.StartCellEdit(item, subItemIndex);
        }

        /// <summary>
        /// Really start an edit operation on a given cell. The parameters are assumed to be sane.
        /// </summary>
        /// <param name="item">The row to be edited</param>
        /// <param name="subItemIndex">The index of the cell to be edited</param>
        public virtual void StartCellEdit(OLVListItem item, int subItemIndex) {
            OLVColumn column = this.GetColumn(subItemIndex);
            if (column == null)
            {
                return;
            }

            Control c = this.GetCellEditor(item, subItemIndex);
            ObjectListViewTheme.ApplyCellEditorColors(c);
            Rectangle cellBounds = this.CalculateCellBounds(item, subItemIndex);
            c.Bounds = this.CalculateCellEditorBounds(item, subItemIndex, c.PreferredSize);

            // Try to align the control as the column is aligned. Not all controls support this property
            Munger.PutProperty(c, "TextAlign", column.TextAlign);

            // Give the control the value from the model
            this.SetControlValue(c, column.GetValue(item.RowObject), column.GetStringValue(item.RowObject));

            // Give the outside world the chance to munge with the process
            this.CellEditEventArgs = new CellEditEventArgs(column, c, cellBounds, item, subItemIndex);
            this.OnCellEditStarting(this.CellEditEventArgs);
            if (this.CellEditEventArgs.Cancel)
            {
                return;
            }

            // The event handler may have completely changed the control, so we need to remember it
            this.cellEditor = this.CellEditEventArgs.Control;
            
            this.Invalidate();
            this.Controls.Add(this.cellEditor);
            this.ConfigureControl();
            this.PauseAnimations(true);
        }
        private Control cellEditor;
        internal CellEditEventArgs CellEditEventArgs;

        /// <summary>
        /// Calculate the bounds of the edit control for the given item/column
        /// </summary>
        /// <param name="item"></param>
        /// <param name="subItemIndex"></param>
        /// <param name="preferredSize"> </param>
        /// <returns></returns>
        public Rectangle CalculateCellEditorBounds(OLVListItem item, int subItemIndex, Size preferredSize) {
            Rectangle r = CalculateCellBounds(item, subItemIndex);

            // Calculate the width of the cell's current contents
            return this.OwnerDraw 
                ? CalculateCellEditorBoundsOwnerDrawn(item, subItemIndex, r, preferredSize) 
                : CalculateCellEditorBoundsStandard(item, subItemIndex, r, preferredSize);
        }

        /// <summary>
        /// Calculate the bounds of the edit control for the given item/column, when the listview
        /// is being owner drawn.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="subItemIndex"></param>
        /// <param name="r"></param>
        /// <param name="preferredSize"> </param>
        /// <returns>A rectangle that is the bounds of the cell editor</returns>
        protected Rectangle CalculateCellEditorBoundsOwnerDrawn(OLVListItem item, int subItemIndex, Rectangle r, Size preferredSize) {
            IRenderer renderer = this.View == View.Details
                ? this.GetCellRenderer(item.RowObject, this.GetColumn(subItemIndex))
                : this.ItemRenderer;

            if (renderer == null)
            {
                return r;
            }

            using (Graphics g = this.CreateGraphics()) {
                return renderer.GetEditRectangle(g, r, item, subItemIndex, preferredSize);
            }
        }

        /// <summary>
        /// Calculate the bounds of the edit control for the given item/column, when the listview
        /// is not being owner drawn.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="subItemIndex"></param>
        /// <param name="cellBounds"></param>
        /// <param name="preferredSize"> </param>
        /// <returns>A rectangle that is the bounds of the cell editor</returns>
        protected Rectangle CalculateCellEditorBoundsStandard(OLVListItem item, int subItemIndex, Rectangle cellBounds, Size preferredSize) {
            if (this.View == View.Tile)
            {
                return cellBounds;
            }

            // Center the editor vertically
            if (cellBounds.Height != preferredSize.Height)
            {
                cellBounds.Y += (cellBounds.Height - preferredSize.Height) / 2;
            }

            // Only Details view needs more processing
            if (this.View != View.Details)
            {
                return cellBounds;
            }

            // Allow for image (if there is one). 
            int offset = 0;
            object imageSelector = null;
            if (subItemIndex == 0)
            {
                imageSelector = item.ImageSelector;
            }
            else {
                // We only check for subitem images if we are owner drawn or showing subitem images
                if (this.OwnerDraw || this.ShowImagesOnSubItems)
                {
                    imageSelector = item.GetSubItem(subItemIndex).ImageSelector;
                }
            }
            if (this.GetActualImageIndex(imageSelector) != -1) {
                offset += this.SmallImageSize.Width + 2;
            }

            // Allow for checkbox
            if (this.CheckBoxes && this.StateImageList != null && subItemIndex == 0) {
                offset += this.StateImageList.ImageSize.Width + 2;
            }

            // Allow for indent (first column only)
            if (subItemIndex == 0 && item.IndentCount > 0) {
                offset += (this.SmallImageSize.Width * item.IndentCount);
            }

            // Do the adjustment
            if (offset > 0) {
                cellBounds.X += offset;
                cellBounds.Width -= offset;
            }

            return cellBounds;
        }

        /// <summary>
        /// Try to give the given value to the provided control. Fall back to assigning a string
        /// if the value assignment fails.
        /// </summary>
        /// <param name="control">A control</param>
        /// <param name="value">The value to be given to the control</param>
        /// <param name="stringValue">The string to be given if the value doesn't work</param>
        protected virtual void SetControlValue(Control control, Object value, String stringValue) {
            // Does the control implement our custom interface?
            IOlvEditor olvEditor = control as IOlvEditor;
            if (olvEditor != null) {
                olvEditor.Value = value;
                return;
            }

            // Handle combobox explicitly
            ComboBox cb = control as ComboBox;
            if (cb != null) {
                if (cb.Created)
                {
                    cb.SelectedValue = value;
                }
                else
                {
                    this.BeginInvoke(new MethodInvoker(delegate {
                        cb.SelectedValue = value;
                    }));
                }

                return;
            }

            if (Munger.PutProperty(control, "Value", value))
            {
                return;
            }

            // There wasn't a Value property, or we couldn't set it, so set the text instead
            try
            {
                String valueAsString = value as String;
                control.Text = valueAsString ?? stringValue;
            }
            catch (ArgumentOutOfRangeException) {
                // The value couldn't be set via the Text property.
            }
        }

        /// <summary>
        /// Setup the given control to be a cell editor
        /// </summary>
        protected virtual void ConfigureControl() {
            this.cellEditor.Validating += new CancelEventHandler(CellEditor_Validating);
            this.cellEditor.Select();
        }

        /// <summary>
        /// Return the value that the given control is showing
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        protected virtual Object GetControlValue(Control control) {
            if (control == null)
            {
                return null;
            }

            IOlvEditor olvEditor = control as IOlvEditor;
            if (olvEditor != null)
            {
                return olvEditor.Value;
            }

            TextBox box = control as TextBox;
            if (box != null)
            {
                return box.Text;
            }

            ComboBox comboBox = control as ComboBox;
            if (comboBox != null)
            {
                return comboBox.SelectedValue;
            }

            CheckBox checkBox = control as CheckBox;
            if (checkBox != null)
            {
                return checkBox.Checked;
            }

            try {
                return control.GetType().InvokeMember("Value", BindingFlags.GetProperty, null, control, null, CultureInfo.CurrentCulture);
            } catch (MissingMethodException) { // Microsoft throws this
                return control.Text;
            } catch (MissingFieldException) { // Mono throws this
                return control.Text;
            }
        }

        /// <summary>
        /// Called when the cell editor could be about to lose focus. Time to commit the change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void CellEditor_Validating(object sender, CancelEventArgs e) {
            this.CellEditEventArgs.Cancel = false;
            this.CellEditEventArgs.NewValue = this.GetControlValue(this.cellEditor);
            this.OnCellEditorValidating(this.CellEditEventArgs);

            if (this.CellEditEventArgs.Cancel) {
                this.CellEditEventArgs.Control.Select();
                e.Cancel = true;
            } else
            {
                FinishCellEdit();
            }
        }

        /// <summary>
        /// Return the bounds of the given cell
        /// </summary>
        /// <param name="item">The row to be edited</param>
        /// <param name="subItemIndex">The index of the cell to be edited</param>
        /// <returns>A Rectangle</returns>
        public virtual Rectangle CalculateCellBounds(OLVListItem item, int subItemIndex) {

            // It seems on Win7, GetSubItemBounds() does not have the same problems with
            // column 0 that it did previously.

            // TODO - Check on XP

            if (this.View != View.Details)
            {
                return this.GetItemRect(item.Index, ItemBoundsPortion.Label);
            }

            Rectangle r = item.GetSubItemBounds(subItemIndex);
            if (!this.OwnerDraw) {
                r.Width -= 1;
                r.Height -= 1;
            }
            return r;

            // We use ItemBoundsPortion.Label rather than ItemBoundsPortion.Item
            // since Label extends to the right edge of the cell, whereas Item gives just the
            // current text width.
            //return this.CalculateCellBounds(item, subItemIndex, ItemBoundsPortion.Label);
        }

        /// <summary>
        /// Return the bounds of the given cell only until the edge of the current text
        /// </summary>
        /// <param name="item">The row to be edited</param>
        /// <param name="subItemIndex">The index of the cell to be edited</param>
        /// <returns>A Rectangle</returns>
        public virtual Rectangle CalculateCellTextBounds(OLVListItem item, int subItemIndex) {
            return this.CalculateCellBounds(item, subItemIndex, ItemBoundsPortion.ItemOnly);
        }

        private Rectangle CalculateCellBounds(OLVListItem item, int subItemIndex, ItemBoundsPortion portion) {
            // SubItem.Bounds works for every subitem, except the first.
            if (subItemIndex > 0)
            {
                return item.GetSubItemBounds(subItemIndex);
            }

            // For non detail views, we just use the requested portion
            Rectangle r = this.GetItemRect(item.Index, portion);
            if (r.Y < -10000000 || r.Y > 10000000) {
                r.Y = item.Bounds.Y;
            }
            if (this.View != View.Details)
            {
                return r;
            }

            // Finding the bounds of cell 0 should not be a difficult task, but it is. Problems:
            // 1) item.SubItem[0].Bounds is always the full bounds of the entire row, not just cell 0.
            // 2) if column 0 has been dragged to some other position, the bounds always has a left edge of 0.

            // We avoid both these problems by using the position of sides the column header to calculate
            // the sides of the cell
            Point sides = NativeMethods.GetScrolledColumnSides(this, 0);
            r.X = sides.X + 4;
            r.Width = sides.Y - sides.X - 5;

            return r;
        }

        /// <summary>
        /// Calculate the visible bounds of the given column. The column's bottom edge is 
        /// either the bottom of the last row or the bottom of the control.
        /// </summary>
        /// <param name="bounds">The bounds of the control itself</param>
        /// <param name="column">The column</param>
        /// <returns>A Rectangle</returns>
        /// <remarks>This returns an empty rectangle if the control isn't in Details mode, 
        /// OR has doesn't have any rows, OR if the given column is hidden.</remarks>
        public virtual Rectangle CalculateColumnVisibleBounds(Rectangle bounds, OLVColumn column)
        {
            // Sanity checks
            if (column == null ||
                this.View != System.Windows.Forms.View.Details ||
                this.GetItemCount() == 0 ||
                !column.IsVisible)
            {
                return Rectangle.Empty;
            }

            Point sides = NativeMethods.GetScrolledColumnSides(this, column.Index);
            if (sides.X == -1)
            {
                return Rectangle.Empty;
            }

            Rectangle columnBounds = new Rectangle(sides.X, bounds.Top, sides.Y - sides.X, bounds.Bottom);

            // Find the bottom of the last item. The column only extends to there.
            OLVListItem lastItem = this.GetLastItemInDisplayOrder();
            if (lastItem != null)
            {
                Rectangle lastItemBounds = lastItem.Bounds;
                if (!lastItemBounds.IsEmpty && lastItemBounds.Bottom < columnBounds.Bottom)
                {
                    columnBounds.Height = lastItemBounds.Bottom - columnBounds.Top;
                }
            }

            return columnBounds;
        }

        /// <summary>
        /// Return a control that can be used to edit the value of the given cell.
        /// </summary>
        /// <param name="item">The row to be edited</param>
        /// <param name="subItemIndex">The index of the cell to be edited</param>
        /// <returns>A Control to edit the given cell</returns>
        protected virtual Control GetCellEditor(OLVListItem item, int subItemIndex) {
            OLVColumn column = this.GetColumn(subItemIndex);
            Object value = column.GetValue(item.RowObject);

            // Does the column have its own special way of creating cell editors?
            if (column.EditorCreator != null) {
                Control customEditor = column.EditorCreator(item.RowObject, column, value);
                if (customEditor != null)
                {
                    return customEditor;
                }
            }

            // Ask the registry for an instance of the appropriate editor.
            // Use a default editor if the registry can't create one for us.
            Control editor = ObjectListViewBase.EditorRegistry.GetEditor(item.RowObject, column, value ?? this.GetFirstNonNullValue(column));
            return editor ?? this.MakeDefaultCellEditor(column);
        }

        /// <summary>
        /// Get the first non-null value of the given column.
        /// At most 1000 rows will be considered.
        /// </summary>
        /// <param name="column"></param>
        /// <returns>The first non-null value, or null if no non-null values were found</returns>
        internal object GetFirstNonNullValue(OLVColumn column) {
            for (int i = 0; i < Math.Min(this.GetItemCount(), 1000); i++) {
                object value = column.GetValue(this.GetModelAt(i));
                if (value != null)
                {
                    return value;
                }
            }
            return null;
        }

        /// <summary>
        /// Return a TextBox that can be used as a default cell editor.
        /// </summary>
        /// <param name="column">What column does the cell belong to?</param>
        /// <returns></returns>
        protected virtual Control MakeDefaultCellEditor(OLVColumn column) {
            TextBox tb = new TextBox();
            if (column.AutoCompleteEditor)
            {
                this.ConfigureAutoComplete(tb, column);
            }

            return tb;
        }

        /// <summary>
        /// Configure the given text box to autocomplete unique values
        /// from the given column. At most 1000 rows will be considered.
        /// </summary>
        /// <param name="tb">The textbox to configure</param>
        /// <param name="column">The column used to calculate values</param>
        public void ConfigureAutoComplete(TextBox tb, OLVColumn column) {
            this.ConfigureAutoComplete(tb, column, 1000);
        }


        /// <summary>
        /// Configure the given text box to autocomplete unique values
        /// from the given column. At most 1000 rows will be considered.
        /// </summary>
        /// <param name="tb">The textbox to configure</param>
        /// <param name="column">The column used to calculate values</param>
        /// <param name="maxRows">Consider only this many rows</param>
        public void ConfigureAutoComplete(TextBox tb, OLVColumn column, int maxRows) {
            // Don't consider more rows than we actually have
            maxRows = Math.Min(this.GetItemCount(), maxRows);

            // Reset any existing autocomplete
            tb.AutoCompleteCustomSource.Clear();

            // CONSIDER: Should we use ClusteringStrategy here?

            // Build a list of unique values, to be used as autocomplete on the editor
            Dictionary<string, bool> alreadySeen = new Dictionary<string, bool>();
            List<string> values = new List<string>();
            for (int i = 0; i < maxRows; i++) {
                string valueAsString = column.GetStringValue(this.GetModelAt(i));
                if (!String.IsNullOrEmpty(valueAsString) && !alreadySeen.ContainsKey(valueAsString)) {
                    values.Add(valueAsString);
                    alreadySeen[valueAsString] = true;
                }
            }

            tb.AutoCompleteCustomSource.AddRange(values.ToArray());
            tb.AutoCompleteSource = AutoCompleteSource.CustomSource;
            tb.AutoCompleteMode = column.AutoCompleteEditorMode;
        }

        /// <summary>
        /// Stop editing a cell and throw away any changes.
        /// </summary>
        public virtual void CancelCellEdit() {
            if (!this.IsCellEditing)
            {
                return;
            }

            // Let the world know that the user has cancelled the edit operation
            this.CellEditEventArgs.Cancel = true;
            this.CellEditEventArgs.NewValue = this.GetControlValue(this.cellEditor);
            this.OnCellEditFinishing(this.CellEditEventArgs);

            // Now cleanup the editing process
            this.CleanupCellEdit(false, this.CellEditEventArgs.AutoDispose);
        }

        /// <summary>
        /// If a cell edit is in progress, finish the edit.
        /// </summary>
        /// <returns>Returns false if the finishing process was cancelled
        /// (i.e. the cell editor is still on screen)</returns>
        /// <remarks>This method does not guarantee that the editing will finish. The validation
        /// process can cause the finishing to be aborted. Developers should check the return value
        /// or use IsCellEditing property after calling this method to see if the user is still
        /// editing a cell.</remarks>
        public virtual bool PossibleFinishCellEditing() {
            return this.PossibleFinishCellEditing(false);
        }

        /// <summary>
        /// If a cell edit is in progress, finish the edit.
        /// </summary>
        /// <returns>Returns false if the finishing process was cancelled
        /// (i.e. the cell editor is still on screen)</returns>
        /// <remarks>This method does not guarantee that the editing will finish. The validation
        /// process can cause the finishing to be aborted. Developers should check the return value
        /// or use IsCellEditing property after calling this method to see if the user is still
        /// editing a cell.</remarks>
        /// <param name="expectingCellEdit">True if it is likely that another cell is going to be 
        /// edited immediately after this cell finishes editing</param>
        public virtual bool PossibleFinishCellEditing(bool expectingCellEdit) {
            if (!this.IsCellEditing)
            {
                return true;
            }

            this.CellEditEventArgs.Cancel = false;
            this.CellEditEventArgs.NewValue = this.GetControlValue(this.cellEditor);
            this.OnCellEditorValidating(this.CellEditEventArgs);

            if (this.CellEditEventArgs.Cancel)
            {
                return false;
            }

            this.FinishCellEdit(expectingCellEdit);

            return true;
        }

        /// <summary>
        /// Finish the cell edit operation, writing changed data back to the model object
        /// </summary>
        /// <remarks>This method does not trigger a Validating event, so it always finishes
        /// the cell edit.</remarks>
        public virtual void FinishCellEdit() {
            this.FinishCellEdit(false);
        }

        /// <summary>
        /// Finish the cell edit operation, writing changed data back to the model object
        /// </summary>
        /// <remarks>This method does not trigger a Validating event, so it always finishes
        /// the cell edit.</remarks>
        /// <param name="expectingCellEdit">True if it is likely that another cell is going to be 
        /// edited immediately after this cell finishes editing</param>
        public virtual void FinishCellEdit(bool expectingCellEdit) {
            if (!this.IsCellEditing)
            {
                return;
            }

            this.CellEditEventArgs.Cancel = false;
            this.CellEditEventArgs.NewValue = this.GetControlValue(this.cellEditor);
            this.OnCellEditFinishing(this.CellEditEventArgs);

            // If someone doesn't cancel the editing process, write the value back into the model
            if (!this.CellEditEventArgs.Cancel) {
                this.CellEditEventArgs.Column.PutValue(this.CellEditEventArgs.RowObject, this.CellEditEventArgs.NewValue);
                this.RefreshItem(this.CellEditEventArgs.ListViewItem);
            }

            this.CleanupCellEdit(expectingCellEdit, this.CellEditEventArgs.AutoDispose);

            // Tell the world that the cell has been edited
            this.OnCellEditFinished(this.CellEditEventArgs);
        }

        /// <summary>
        /// Remove all trace of any existing cell edit operation
        /// </summary>
        /// <param name="expectingCellEdit">True if it is likely that another cell is going to be 
        /// edited immediately after this cell finishes editing</param>
        /// <param name="disposeOfCellEditor">True if the cell editor should be disposed </param>
        protected virtual void CleanupCellEdit(bool expectingCellEdit, bool disposeOfCellEditor) {
            if (this.cellEditor == null)
            {
                return;
            }

            this.cellEditor.Validating -= new CancelEventHandler(CellEditor_Validating);

            Control soonToBeOldCellEditor = this.cellEditor;
            this.cellEditor = null;

            // Delay cleaning up the cell editor so that if we are immediately going to 
            // start a new cell edit (because the user pressed Tab) the new cell editor
            // has a chance to grab the focus. Without this, the ListView gains focus
            // momentarily (after the cell editor is remove and before the new one is created)
            // causing the list's selection to flash momentarily.
            EventHandler toBeRun = null;
            toBeRun = delegate(object sender, EventArgs e) {
                Application.Idle -= toBeRun;
                this.Controls.Remove(soonToBeOldCellEditor);
                if (disposeOfCellEditor)
                {
                    soonToBeOldCellEditor.Dispose();
                }

                this.Invalidate();

                if (!this.IsCellEditing) {
                    if (this.Focused)
                    {
                        this.Select();
                    }

                    this.PauseAnimations(false);
                }
            };

            // We only want to delay the removal of the control if we are expecting another cell
            // to be edited. Otherwise, we remove the control immediately.
            if (expectingCellEdit)
            {
                this.RunWhenIdle(toBeRun);
            }
            else
            {
                toBeRun(null, null);
            }
        }

        #endregion
    }
}
