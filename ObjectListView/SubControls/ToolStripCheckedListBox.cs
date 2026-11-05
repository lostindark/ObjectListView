/*
 * ToolStripCheckedListBox - Puts a CheckedListBox into a tool strip menu item
 *
 * Author: Phillip Piper
 * Date: 4-March-2011 11:59 pm
 *
 * Change log:
 * 2011-03-04  JPP  - First version
 * 
 * Copyright (C) 2011-2014 Phillip Piper
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
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Forms.VisualStyles;

namespace BrightIdeasSoftware {

    /// <summary>
    /// Instances of this class put a CheckedListBox into a tool strip menu item.
    /// </summary>
    public class ToolStripCheckedListBox : ToolStripControlHost {

        /// <summary>
        /// Create a ToolStripCheckedListBox
        /// </summary>
        public ToolStripCheckedListBox()
            : base(new ThemedCheckedListBox()) {
            this.CheckedListBoxControl.MaximumSize = new Size(400, 700);
            this.CheckedListBoxControl.ThreeDCheckBoxes = true;
            this.CheckedListBoxControl.CheckOnClick = true;
            this.CheckedListBoxControl.SelectionMode = SelectionMode.One;
        }

        /// <summary>
        /// Gets the control embedded in the menu
        /// </summary>
        public CheckedListBox CheckedListBoxControl {
            get {
                return Control as CheckedListBox;
            }
        }

        /// <summary>
        /// Gets the items shown in the checkedlistbox
        /// </summary>
        public CheckedListBox.ObjectCollection Items {
            get {
                return this.CheckedListBoxControl.Items;
            }
        }

        /// <summary>
        /// Gets or sets whether an item should be checked when it is clicked
        /// </summary>
        [System.ComponentModel.DefaultValue(true)]
        public bool CheckedOnClick {
            get {
                return this.CheckedListBoxControl.CheckOnClick;
            }
            set {
                this.CheckedListBoxControl.CheckOnClick = value;
            }
        }

        /// <summary>
        /// Gets a collection of the checked items
        /// </summary>
        public CheckedListBox.CheckedItemCollection CheckedItems {
            get {
                return this.CheckedListBoxControl.CheckedItems;
            }
        }

        /// <summary>
        /// Add a possibly checked item to the control
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isChecked"></param>
        public void AddItem(object item, bool isChecked) {
            this.Items.Add(item);
            if (isChecked)
            {
                this.CheckedListBoxControl.SetItemChecked(this.Items.Count - 1, true);
            }
        }

        /// <summary>
        /// Add an item with the given state to the control
        /// </summary>
        /// <param name="item"></param>
        /// <param name="state"></param>
        public void AddItem(object item, CheckState state) {
            this.Items.Add(item);
            this.CheckedListBoxControl.SetItemCheckState(this.Items.Count - 1, state);
        }

        /// <summary>
        /// Gets the checkedness of the i'th item
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public CheckState GetItemCheckState(int i) {
            return this.CheckedListBoxControl.GetItemCheckState(i);
        }

        /// <summary>
        /// Set the checkedness of the i'th item
        /// </summary>
        /// <param name="i"></param>
        /// <param name="checkState"></param>
        public void SetItemState(int i, CheckState checkState) {
            if (i >= 0 && i < this.Items.Count)
            {
                this.CheckedListBoxControl.SetItemCheckState(i, checkState);
            }
        }

        /// <summary>
        /// Check all the items in the control
        /// </summary>
        public void CheckAll() {
            for (int i = 0; i < this.Items.Count; i++)
            {
                this.CheckedListBoxControl.SetItemChecked(i, true);
            }
        }

        /// <summary>
        /// Unchecked all the items in the control
        /// </summary>
        public void UncheckAll() {
            for (int i = 0; i < this.Items.Count; i++)
            {
                this.CheckedListBoxControl.SetItemChecked(i, false);
            }
        }

        #region Events

        /// <summary>
        /// Listen for events on the underlying control
        /// </summary>
        /// <param name="control"></param>
        protected override void OnSubscribeControlEvents(Control control) {
            base.OnSubscribeControlEvents(control);

            CheckedListBox checkedListBox = (CheckedListBox)control;
            checkedListBox.ItemCheck += new ItemCheckEventHandler(OnItemCheck);
        }

        /// <summary>
        /// Stop listening for events on the underlying control
        /// </summary>
        /// <param name="control"></param>
        protected override void OnUnsubscribeControlEvents(Control control) {
            base.OnUnsubscribeControlEvents(control);

            CheckedListBox checkedListBox = (CheckedListBox)control;
            checkedListBox.ItemCheck -= new ItemCheckEventHandler(OnItemCheck);
        }

        /// <summary>
        /// Tell the world that an item was checked
        /// </summary>
        public event ItemCheckEventHandler ItemCheck;

        /// <summary>
        /// Trigger the ItemCheck event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnItemCheck(object sender, ItemCheckEventArgs e) {
            if (ItemCheck != null) {
                ItemCheck(this, e);
            }
        }

        private sealed class ThemedCheckedListBox : CheckedListBox {
            protected override void OnDrawItem(DrawItemEventArgs e) {
                if (!ObjectListViewTheme.IsDarkMode || e.Index < 0)
                {
                    base.OnDrawItem(e);
                    return;
                }

                bool selected = (e.State & DrawItemState.Selected) != 0;
                Color background = selected ? ObjectListViewTheme.Current.HighlightColor : this.BackColor;
                Color foreground = !this.Enabled ? ObjectListViewTheme.Current.GrayTextColor
                    : selected ? ObjectListViewTheme.Current.HighlightTextColor : this.ForeColor;
                using (Brush brush = new SolidBrush(background))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }

                CheckState checkState = this.GetItemCheckState(e.Index);
                CheckBoxState state = checkState == CheckState.Checked
                    ? this.Enabled ? CheckBoxState.CheckedNormal : CheckBoxState.CheckedDisabled
                    : checkState == CheckState.Indeterminate
                        ? this.Enabled ? CheckBoxState.MixedNormal : CheckBoxState.MixedDisabled
                        : this.Enabled ? CheckBoxState.UncheckedNormal : CheckBoxState.UncheckedDisabled;
                Size glyphSize = CheckBoxRenderer.GetGlyphSize(e.Graphics, state);
                bool rtl = this.RightToLeft == RightToLeft.Yes;
                int spacing = (int)Math.Ceiling(3 * e.Graphics.DpiX / 96f);
                Point location = new Point(rtl ? e.Bounds.Right - glyphSize.Width - 1 : e.Bounds.Left + 1,
                    e.Bounds.Top + Math.Max(0, (e.Bounds.Height - glyphSize.Height) / 2));
                ThemedRendering.DrawCheckBox(e.Graphics, location, state);

                Rectangle text = e.Bounds;
                text.Width = Math.Max(0, text.Width - glyphSize.Width - spacing);
                if (!rtl)
                {
                    text.X += glyphSize.Width + spacing;
                }
                TextRenderer.DrawText(e.Graphics, this.GetItemText(this.Items[e.Index]), e.Font, text, foreground,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
                    | TextFormatFlags.EndEllipsis | TextFormatFlags.PreserveGraphicsClipping
                    | (rtl ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left));
                e.DrawFocusRectangle();
            }
        }

        #endregion
    }
}
