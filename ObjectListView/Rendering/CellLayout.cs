using System;
using System.Drawing;
using System.Windows.Forms;

namespace BrightIdeasSoftware {
    /// <summary>
    /// A snapshot of a renderer's geometry in list-view client coordinates.
    /// Recalculate after scrolling, resizing, changing DPI, or changing styles.
    /// </summary>
    public sealed class CellLayout {
        /// <summary>
        /// Creates a layout for the standard checkbox, image, and text arrangement.
        /// </summary>
        public CellLayout(Rectangle cellBounds, Rectangle contentBounds, Rectangle checkBoxBounds,
            Rectangle imageBounds, Rectangle textBounds, Rectangle expandButtonBounds = default, Rectangle buttonBounds = default) {
            this.CellBounds = cellBounds;
            this.ContentBounds = contentBounds;
            this.CheckBoxBounds = checkBoxBounds;
            this.ImageBounds = imageBounds;
            this.TextBounds = textBounds;
            this.ExpandButtonBounds = expandButtonBounds;
            this.ButtonBounds = buttonBounds;
        }

        /// <summary>Gets the complete cell bounds.</summary>
        public Rectangle CellBounds { get; }
        /// <summary>Gets the padded, indented area available to content and editors.</summary>
        public Rectangle ContentBounds { get; }
        /// <summary>Gets the checkbox glyph bounds, excluding spacing.</summary>
        public Rectangle CheckBoxBounds { get; }
        /// <summary>Gets the image bounds, excluding spacing.</summary>
        public Rectangle ImageBounds { get; }
        /// <summary>Gets the text drawing bounds.</summary>
        public Rectangle TextBounds { get; }
        /// <summary>Gets the tree expansion target, or an empty rectangle.</summary>
        public Rectangle ExpandButtonBounds { get; }
        /// <summary>Gets the button target, or an empty rectangle.</summary>
        public Rectangle ButtonBounds { get; }

        internal Rectangle GetHitBounds(HitTestLocation location) {
            Rectangle bounds;
            switch (location) {
                case HitTestLocation.CheckBox: bounds = this.CheckBoxBounds; break;
                case HitTestLocation.Image: bounds = this.ImageBounds; break;
                case HitTestLocation.Text: bounds = this.TextBounds; break;
                case HitTestLocation.ExpandButton: bounds = this.ExpandButtonBounds; break;
                case HitTestLocation.Button: bounds = this.ButtonBounds; break;
                default: bounds = this.CellBounds; break;
            }
            return Rectangle.Intersect(this.CellBounds, bounds);
        }
    }

    public partial class BaseRenderer {
        /// <summary>
        /// Calculates layout without painting. Custom renderers with a different arrangement
        /// can override this method and the protected layout calculation.
        /// </summary>
        public virtual CellLayout GetCellLayout(Graphics g, Rectangle cellBounds, OLVListItem item, int subItemIndex) {
            this.ConfigureCell(cellBounds, item, subItemIndex);
            return this.CalculateCellLayout(g, cellBounds);
        }

        private void ConfigureCell(Rectangle cellBounds, OLVListItem item, int subItemIndex) {
            this.ClearState();
            this.ListView = (ObjectListViewBase)item.ListView;
            this.ListItem = item;
            this.SubItem = item.GetSubItem(subItemIndex);
            this.Column = this.ListView.GetColumn(subItemIndex);
            this.RowObject = item.RowObject;
            this.IsItemSelected = item.Selected && item.Enabled;
            this.Bounds = cellBounds;
        }

        /// <summary>Gets whether content runs from right to left.</summary>
        protected bool IsRightToLeft => this.ListView.RightToLeft == RightToLeft.Yes;

        /// <summary>Gets the padded and indented area used by all standard layout consumers.</summary>
        protected virtual Rectangle GetContentBounds(Rectangle cellBounds) {
            Rectangle r = cellBounds;
            if (this.ColumnIsPrimary && this.CellHorizontalAlignment == HorizontalAlignment.Left) {
                r.X += this.IsRightToLeft ? 0 : 3;
                r.Width = Math.Max(0, r.Width - 3);
            }
            r = this.ApplyCellPadding(r);
            if (this.ColumnIsPrimary) {
                int indent = Math.Min(r.Width, this.ListView.SmallImageSize.Width * this.ListItem.IndentCount);
                if (!this.IsRightToLeft)
                    r.X += indent;
                r.Width -= indent;
            }
            return r;
        }

        /// <summary>Calculates the standard layout from the complete cell bounds.</summary>
        protected virtual CellLayout CalculateCellLayout(Graphics g, Rectangle cellBounds) {
            Rectangle content = this.GetContentBounds(cellBounds);
            return this.CalculateImageAndTextLayout(g, this.CalculateAlignedRectangle(g, content), content);
        }

        /// <summary>Partitions aligned content using the same measurements as painting.</summary>
        protected virtual CellLayout CalculateImageAndTextLayout(Graphics g, Rectangle alignedBounds, Rectangle contentBounds) {
            Rectangle remaining = alignedBounds;
            Rectangle checkBox = Rectangle.Empty;
            if (this.ColumnIsPrimary && this.ListView.CheckBoxes) {
                Size size = this.CalculateCheckBoxSize(g);
                checkBox = new Rectangle(remaining.X, this.AlignVertically(remaining, size.Height), size.Width, size.Height);
                int width = Math.Min(remaining.Width, this.CalculatePrimaryCheckBoxSize(g).Width);
                remaining.X += width;
                remaining.Width -= width;
            }

            Size imageSize = this.CalculateImageSize(g, this.GetImageSelector());
            Rectangle image = imageSize.IsEmpty ? Rectangle.Empty :
                new Rectangle(remaining.X, this.AlignVertically(remaining, Math.Min(remaining.Height, imageSize.Height)),
                    imageSize.Width, imageSize.Height);
            int imageWidth = Math.Min(remaining.Width, imageSize.Width);
            remaining.X += imageWidth;
            remaining.Width -= imageWidth;
            Rectangle text = remaining;

            if (this.IsRightToLeft) {
                checkBox = Mirror(checkBox, alignedBounds);
                image = Mirror(image, alignedBounds);
                text = Mirror(text, alignedBounds);
            }
            return new CellLayout(this.Bounds, contentBounds, checkBox, image, text);
        }

        internal static Rectangle Mirror(Rectangle bounds, Rectangle container) {
            if (bounds.IsEmpty)
                return bounds;
            return new Rectangle(container.Left + container.Right - bounds.Right, bounds.Y, bounds.Width, bounds.Height);
        }

        /// <summary>Draws the standard components from a previously calculated layout.</summary>
        protected virtual void DrawCellLayout(Graphics g, CellLayout layout) {
            if (layout.CheckBoxBounds.Width > 0 && layout.CheckBoxBounds.Height > 0)
                this.DrawCheckBox(g, layout.CheckBoxBounds);
            if (layout.ImageBounds.Width > 0 && layout.ImageBounds.Height > 0)
                this.DrawImage(g, layout.ImageBounds, this.GetImageSelector());
            if (layout.TextBounds.Width > 0 && layout.TextBounds.Height > 0)
                this.DrawText(g, layout.TextBounds, this.GetText());
        }
    }
}
