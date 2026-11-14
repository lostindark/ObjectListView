using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace BrightIdeasSoftware {
    public partial class ObjectListViewBase {
        /// <summary>Resizes a column to fit every row in the current view, including offscreen rows.</summary>
        /// <remarks>
        /// This operation measures all current rows on the control thread. For virtual lists,
        /// it fetches every displayed model and can be expensive for large or remote sources.
        /// Native AutoResizeColumn and ColumnHeader.AutoResize behavior is unchanged.
        /// </remarks>
        /// <param name="columnIndex">The column's collection index, not its display index.</param>
        public virtual void AutoResizeColumnToContents(int columnIndex) {
            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate { this.AutoResizeColumnToContents(columnIndex); });
                return;
            }
            if (columnIndex < 0 || columnIndex >= this.Columns.Count)
                throw new ArgumentOutOfRangeException(nameof(columnIndex));
            if (this.View != View.Details)
                throw new InvalidOperationException("Column content sizing requires Details view.");

            OLVColumn column = this.GetColumn(columnIndex);
            int width = 0;
            int count = this.GetItemCount();
            using (Graphics graphics = this.CreateGraphics())
                for (int i = 0; i < count; i++)
                    width = Math.Max(width, this.CalculateColumnContentWidth(graphics, this.GetItem(i), column));
            column.Width = width;
        }

        private void ResizeOwnerDrawnColumnToContents(OLVColumn column) {
            int width = 0;
            using (Graphics g = this.CreateGraphics()) {
                if (this.VirtualMode) {
                    bool found = false;
                    foreach (OLVListItem row in this.GetVisibleRowsForContentSizing()) {
                        found = true;
                        width = Math.Max(width, this.CalculateColumnContentWidth(g, row, column));
                    }
                    if (!found)
                        return;
                } else {
                    for (int i = 0; i < this.GetItemCount(); i++)
                        width = Math.Max(width, this.CalculateColumnContentWidth(g, this.GetItem(i), column));
                }
            }
            column.Width = width;
        }

        private IEnumerable<OLVListItem> GetVisibleRowsForContentSizing() {
            List<int> probePositions = new List<int>();
            HashSet<int> visited = new HashSet<int>();
            Rectangle client = this.ClientRectangle;
            foreach (OLVColumn column in this.Columns) {
                Point sides = NativeMethods.GetScrolledColumnSides(this, column.Index);
                int left = Math.Max(client.Left, sides.X);
                int right = Math.Min(client.Right, sides.Y);
                if (left < right)
                    probePositions.Add(Math.Min(right - 1, left + 4));
            }
            if (!this.FullRowSelect) {
                // Native targets can start beyond tree indentation, not at the column's leading inset.
                HashSet<int> preferredPositions = new HashSet<int>(probePositions);
                for (int x = client.Left; x < client.Right; x++)
                    if (!preferredPositions.Contains(x))
                        probePositions.Add(x);
            }
            int top = this.HeaderStyle == ColumnHeaderStyle.None ? client.Top :
                Math.Min(client.Bottom, client.Top + this.HeaderControl.ClientRectangle.Height);
            for (int y = top; y < client.Bottom; y++)
                foreach (int x in probePositions) {
                    NativeMethods.LVHITTESTINFO hit = new NativeMethods.LVHITTESTINFO { pt_x = x, pt_y = y };
                    int index = NativeMethods.HitTest(this, ref hit);
                    if (index >= 0 && (hit.flags & (int)HitTestLocationEx.LVHT_EX_GROUP) == 0) {
                        if (visited.Add(index)) {
                            OLVListItem row = this.GetItem(index);
                            y = Math.Max(y, row.Bounds.Bottom - 1);
                            yield return row;
                        }
                        break;
                    }
                }
        }

        /// <summary>
        /// Measures a cell's full content width. Owner-drawn cells use their renderer's measurements.
        /// Override this for custom renderer layouts that do not use BaseRenderer.
        /// </summary>
        protected virtual int CalculateColumnContentWidth(Graphics graphics, OLVListItem item, OLVColumn column) {
            BaseRenderer renderer = this.GetCellRenderer(item.RowObject, column) as BaseRenderer;
            if (this.OwnerDraw && renderer != null)
                return renderer.GetPreferredWidth(graphics, item, column.Index);

            OLVListSubItem subItem = item.GetSubItem(column.Index);
            Font font = (item.UseItemStyleForSubItems ? item.Font : subItem.Font) ?? this.Font;
            int width = TextRenderer.MeasureText(graphics, subItem.Text, font,
                new Size(Int32.MaxValue, Int32.MaxValue),
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;

            object selector = column.Index == 0 ? item.ImageSelector :
                (this.OwnerDraw || this.ShowImagesOnSubItems ? subItem.ImageSelector : null);
            ImageList images = renderer != null && renderer.ImageList != null ? renderer.ImageList : this.SmallImageList;
            Image image = selector as Image;
            if (image != null)
            {
                width += image.Width;
            }
            else if (images != null && ((selector is int && (int)selector >= 0 && (int)selector < images.Images.Count) ||
                (selector is string && images.Images.ContainsKey((string)selector))))
            {
                width += images.ImageSize.Width;
            }

            if (column.Index == 0) {
                width += item.IndentCount * this.SmallImageSize.Width;
                if (this.CheckBoxes)
                {
                    width += (this.StateImageList == null ? CheckBoxRenderer.GetGlyphSize(graphics, CheckBoxState.UncheckedNormal).Width :
                        this.StateImageList.ImageSize.Width) + 6;
                }
            } else if (column.CheckBoxes) {
                width += CheckBoxRenderer.GetGlyphSize(graphics, CheckBoxState.UncheckedNormal).Width;
            }

            Rectangle? padding = subItem.CellPadding ?? item.CellPadding ?? column.CellPadding ?? this.CellPadding;
            if (padding.HasValue)
            {
                width += padding.Value.X + padding.Value.Width;
            }

            return width + (int)Math.Ceiling(6 * graphics.DpiX / 96);
        }
    }
}
