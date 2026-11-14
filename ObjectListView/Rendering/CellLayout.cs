using System;
using System.Drawing;
using System.Reflection;
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
        private bool? customCellLayoutHooks;

        private bool HasCustomCellLayoutHooks {
            get {
                if (!this.customCellLayoutHooks.HasValue) {
                    Type type = this.GetType();
                    this.customCellLayoutHooks =
                        type.GetMethod(nameof(CalculateImageAndTextLayout), BindingFlags.Instance | BindingFlags.NonPublic,
                            null, new[] { typeof(Graphics), typeof(Rectangle), typeof(Rectangle) }, null).DeclaringType != typeof(BaseRenderer) ||
                        type.GetMethod(nameof(DrawCellLayout), BindingFlags.Instance | BindingFlags.NonPublic,
                            null, new[] { typeof(Graphics), typeof(CellLayout) }, null).DeclaringType != typeof(BaseRenderer);
                }
                return this.customCellLayoutHooks.Value;
            }
        }

        private readonly struct StandardCellLayout {
            internal StandardCellLayout(Rectangle content, Rectangle checkBox, Rectangle image, Rectangle text) {
                this.Content = content;
                this.CheckBox = checkBox;
                this.Image = image;
                this.Text = text;
            }

            internal Rectangle Content { get; }
            internal Rectangle CheckBox { get; }
            internal Rectangle Image { get; }
            internal Rectangle Text { get; }
        }

        /// <summary>
        /// Calculates layout without painting. Custom renderers with a different arrangement
        /// can override this method and the protected layout calculation.
        /// </summary>
        public virtual CellLayout GetCellLayout(Graphics g, Rectangle cellBounds, OLVListItem item, int subItemIndex) {
            this.ConfigureCell(cellBounds, item, subItemIndex);
            return this.CalculateCellLayout(g, cellBounds);
        }

        /// <summary>Measures the width needed to display the complete cell content without wrapping.</summary>
        public virtual int GetPreferredWidth(Graphics g, OLVListItem item, int subItemIndex) {
            this.ConfigureCell(item.GetSubItemBounds(subItemIndex), item, subItemIndex);
            Rectangle cell = new Rectangle(0, 0, int.MaxValue / 2, int.MaxValue / 2);
            Rectangle content = this.GetContentBounds(cell);
            return this.CalculatePreferredContentSize(g, content).Width + cell.Width - content.Width + 2;
        }

        /// <summary>Measures intrinsic content size independently of a renderer's stretch-to-cell policy.</summary>
        protected virtual Size CalculatePreferredContentSize(Graphics g, Rectangle contentBounds) {
            return this.CalculateContentSize(g, contentBounds);
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
            StandardCellLayout layout = this.CalculateStandardCellLayout(g, alignedBounds, contentBounds);
            return new CellLayout(this.Bounds, layout.Content, layout.CheckBox, layout.Image, layout.Text);
        }

        private StandardCellLayout CalculateStandardCellLayout(Graphics g, Rectangle alignedBounds, Rectangle contentBounds) {
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
            return new StandardCellLayout(contentBounds, checkBox, image, text);
        }

        internal static Rectangle Mirror(Rectangle bounds, Rectangle container) {
            if (bounds.IsEmpty)
                return bounds;
            return new Rectangle(container.Left + container.Right - bounds.Right, bounds.Y, bounds.Width, bounds.Height);
        }

        /// <summary>Draws the standard components from a previously calculated layout.</summary>
        protected virtual void DrawCellLayout(Graphics g, CellLayout layout) {
            this.DrawStandardCellLayout(g, new StandardCellLayout(layout.ContentBounds,
                layout.CheckBoxBounds, layout.ImageBounds, layout.TextBounds));
        }

        private void DrawStandardCellLayout(Graphics g, StandardCellLayout layout) {
            if (layout.CheckBox.Width > 0 && layout.CheckBox.Height > 0)
                this.DrawCheckBox(g, layout.CheckBox);
            if (layout.Image.Width > 0 && layout.Image.Height > 0)
                this.DrawImage(g, layout.Image, this.GetImageSelector());
            if (layout.Text.Width > 0 && layout.Text.Height > 0)
                this.DrawText(g, layout.Text, this.GetText());
        }
    }
}
