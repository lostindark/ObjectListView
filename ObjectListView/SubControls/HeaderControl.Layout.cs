using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BrightIdeasSoftware.Properties;

namespace BrightIdeasSoftware {
    public partial class HeaderControl {
        private sealed class HeaderLayout {
            public CellLayout Cell { get; set; }
            public Rectangle SortIndicator { get; set; }
            public Rectangle FilterIndicator { get; set; }
            public Font Font { get; set; }
            public TextFormatFlags TextFlags { get; set; }
            public Rectangle TextArea { get; set; }
        }

        /// <summary>
        /// Gets the current header content geometry in header-client coordinates.
        /// </summary>
        public CellLayout GetCellLayout(OLVColumn column) {
            using (Graphics g = this.ListView.CreateGraphics()) {
                bool overCheckBox = column.Index == this.lastCheckBoxUnderCursor;
                HeaderStateStyle style = this.CalculateStateStyle(column,
                    column.Index == this.ColumnIndexUnderCursor && !overCheckBox,
                    column.Index == this.ColumnIndexUnderCursor && ObjectListViewBase.IsLeftMouseDown && !overCheckBox);
                return this.CalculateHeaderLayout(g, column, style).Cell;
            }
        }

        internal Rectangle GetToolTipTargetBounds(OLVColumn column) {
            CellLayout layout = this.GetCellLayout(column);
            Point point = this.ScrolledCursorPosition;
            HitTestLocation target = layout.CheckBoxBounds.Contains(point) ? HitTestLocation.CheckBox :
                layout.ImageBounds.Contains(point) ? HitTestLocation.Image :
                layout.TextBounds.Contains(point) ? HitTestLocation.Text : HitTestLocation.Header;
            return layout.GetHitBounds(target);
        }

        private HeaderLayout CalculateHeaderLayout(Graphics g, OLVColumn column, HeaderStateStyle style,
            Rectangle? contentBounds = null, bool includeAdornments = true) {
            Rectangle bounds = this.GetItemRect(column.Index);
            Rectangle content = contentBounds ?? this.GetHeaderDrawRect(column.Index);
            if (style.ResolvedValues?.Padding is Rectangle padding) {
                content = new Rectangle(content.X + padding.X, content.Y + padding.Y,
                    Math.Max(0, content.Width - padding.Right), Math.Max(0, content.Height - padding.Bottom));
            }
            content.Width = Math.Max(0, content.Width);
            content.Height = Math.Max(0, content.Height);
            Rectangle remaining = content;
            Font font = style.Font ?? this.ListView.Font;
            HeaderLayout layout = new HeaderLayout { Font = font };
            if (includeAdornments && this.HasSortIndicator(column)) {
                int width = Math.Min(remaining.Width, this.CalculateSortIndicatorWidth(g, column, font));
                layout.SortIndicator = new Rectangle(remaining.Right - width, remaining.Top, width, remaining.Height);
                remaining.Width -= width;
            }
            if (includeAdornments && this.HasFilterIndicator(column)) {
                int width = CalculateFilterIndicatorWidth(remaining);
                layout.FilterIndicator = width == 0 ? Rectangle.Empty :
                    new Rectangle(remaining.Right - width, remaining.Top + (remaining.Height - Resources.ColumnFilterIndicator.Height) / 2,
                        Resources.ColumnFilterIndicator.Width, Resources.ColumnFilterIndicator.Height);
                remaining.Width -= width;
            }
            Rectangle checkBox = Rectangle.Empty;
            if (includeAdornments && HasCheckBox(column)) {
                checkBox = CalculateCheckBoxBounds(g, remaining);
                int width = Math.Min(remaining.Width, checkBox.Right + 3 - remaining.Left);
                remaining.X += width;
                remaining.Width -= width;
            }

            HorizontalAlignment horizontal = style.ResolvedValues?.HorizontalAlignment ?? column.HeaderTextAlignOrDefault;
            StringAlignment vertical = style.ResolvedValues?.VerticalAlignment ?? StringAlignment.Center;
            TextFormatFlags flags = this.TextFormatFlags | TextFormatFlags.Top | TextFormatFlags.PreserveGraphicsClipping;
            if (!column.IsHeaderVertical) {
                if (horizontal == HorizontalAlignment.Center)
                    flags |= TextFormatFlags.HorizontalCenter;
                else if (horizontal == HorizontalAlignment.Right)
                    flags |= TextFormatFlags.Right;
            }
            if (this.ListView.RightToLeft == RightToLeft.Yes)
                flags |= TextFormatFlags.RightToLeft;
            layout.TextFlags = flags;
            Rectangle image = Rectangle.Empty;
            Rectangle text = remaining;
            Size imageSize = column.HasHeaderImage ? column.ImageList.ImageSize : Size.Empty;
            int gap = imageSize.IsEmpty || !column.ShowTextInHeader ? 0 : 3;
            layout.TextArea = new Rectangle(remaining.X, remaining.Y,
                Math.Max(0, remaining.Width - (column.IsHeaderVertical ? 0 : imageSize.Width + gap)), remaining.Height);
            if (remaining.Width > 0 && remaining.Height > 0) {
                Size proposed = column.IsHeaderVertical ? new Size(remaining.Height, remaining.Width) : remaining.Size;
                if (!column.IsHeaderVertical)
                    proposed.Width = Math.Max(1, proposed.Width - imageSize.Width - gap);
                Size measured = column.ShowTextInHeader ?
                    TextRenderer.MeasureText(g, column.Text, font, proposed, flags) : Size.Empty;
                Size textSize = column.IsHeaderVertical ? new Size(measured.Height, measured.Width) : measured;
                Size groupSize = column.IsHeaderVertical ?
                    new Size(Math.Max(imageSize.Width, textSize.Width), imageSize.Height + gap + textSize.Height) :
                    new Size(imageSize.Width + gap + textSize.Width, Math.Max(imageSize.Height, textSize.Height));
                groupSize = new Size(Math.Min(remaining.Width, groupSize.Width), Math.Min(remaining.Height, groupSize.Height));
                int x = horizontal == HorizontalAlignment.Right ? remaining.Right - groupSize.Width :
                    horizontal == HorizontalAlignment.Center ? remaining.Left + (remaining.Width - groupSize.Width) / 2 : remaining.Left;
                int y = vertical == StringAlignment.Far ? remaining.Bottom - groupSize.Height :
                    vertical == StringAlignment.Center ? remaining.Top + (remaining.Height - groupSize.Height) / 2 : remaining.Top;
                Rectangle group = new Rectangle(new Point(x, y), groupSize);
                if (column.IsHeaderVertical) {
                    if (!imageSize.IsEmpty)
                        image = new Rectangle(group.Left + (group.Width - imageSize.Width) / 2, group.Top, imageSize.Width, imageSize.Height);
                    text = new Rectangle(group.Left, group.Top + imageSize.Height + gap, group.Width,
                        Math.Max(0, group.Height - imageSize.Height - gap));
                } else {
                    if (!imageSize.IsEmpty)
                        image = new Rectangle(group.Left, group.Top + (group.Height - imageSize.Height) / 2, imageSize.Width, imageSize.Height);
                    int textHeight = Math.Min(group.Height, textSize.Height);
                    int textTop = vertical == StringAlignment.Far ? group.Bottom - textHeight :
                        vertical == StringAlignment.Center ? group.Top + (group.Height - textHeight) / 2 : group.Top;
                    text = new Rectangle(group.Left + imageSize.Width + gap, textTop,
                        Math.Max(0, group.Width - imageSize.Width - gap), textHeight);
                    if (this.ListView.RightToLeft == RightToLeft.Yes) {
                        image = BaseRenderer.Mirror(image, group);
                        text = BaseRenderer.Mirror(text, group);
                    }
                }
            }
            if (this.ListView.RightToLeft == RightToLeft.Yes) {
                // Reserve adornments from the leading/trailing edge without mirroring the text itself.
                Rectangle mirrored = BaseRenderer.Mirror(remaining, content);
                int dx = mirrored.X - remaining.X;
                if (!image.IsEmpty)
                    image.Offset(dx, 0);
                text.Offset(dx, 0);
                if (!checkBox.IsEmpty)
                    checkBox = BaseRenderer.Mirror(checkBox, content);
                if (!layout.SortIndicator.IsEmpty)
                    layout.SortIndicator = BaseRenderer.Mirror(layout.SortIndicator, content);
                if (!layout.FilterIndicator.IsEmpty)
                    layout.FilterIndicator = BaseRenderer.Mirror(layout.FilterIndicator, content);
            }
            layout.Cell = new CellLayout(bounds, content, checkBox, image, text);
            return layout;
        }
    }
}
