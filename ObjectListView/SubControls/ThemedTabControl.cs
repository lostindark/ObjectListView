#if NETFRAMEWORK
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BrightIdeasSoftware {
    /// <summary>A buffered dark-mode tab-control fallback for .NET Framework.</summary>
    public class ThemedTabControl : TabControl, IThemeAware {
        private const ControlStyles PaintingStyles = ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw;
        private static readonly ControlStyles[] IndividualPaintingStyles = {
            ControlStyles.UserPaint, ControlStyles.AllPaintingInWmPaint, ControlStyles.OptimizedDoubleBuffer, ControlStyles.ResizeRedraw
        };
        private IThemeColors colors;
        private bool darkRendering;
        private TabDrawMode nativeDrawMode;
        private Point nativePadding;
        private ControlStyles nativePaintingStyles;
        private int hotTab = -1;

        /// <summary>Creates a tab control using native light rendering or buffered dark rendering.</summary>
        public ThemedTabControl() {
            // Capture native settings only after designer/application initialization.
            this.colors = ObjectListViewTheme.Current;
        }

        /// <inheritdoc />
        protected override void OnCreateControl() {
            this.ApplyTheme(this.colors);
            base.OnCreateControl();
        }

        /// <inheritdoc />
        public void ApplyTheme(IThemeColors colors) {
            ArgumentNullException.ThrowIfNull(colors, nameof(colors));
            this.colors = colors;
            bool useDark = colors.Dark && !colors.HighContrast;
            if (useDark != this.darkRendering) {
                this.darkRendering = useDark;
                if (useDark) {
                    this.nativeDrawMode = this.DrawMode;
                    this.nativePadding = this.Padding;
                    this.nativePaintingStyles = 0;
                    foreach (ControlStyles style in IndividualPaintingStyles)
                    {
                        if (this.GetStyle(style))
                        {
                            this.nativePaintingStyles |= style;
                        }
                    }
                    this.SetStyle(PaintingStyles, true);
                    this.DrawMode = TabDrawMode.OwnerDrawFixed;
                    this.Padding = new Point(10, 6);
                } else {
                    this.SetStyle(PaintingStyles, false);
                    this.SetStyle(this.nativePaintingStyles, true);
                    this.DrawMode = this.nativeDrawMode;
                    this.Padding = this.nativePadding;
                    this.hotTab = -1;
                }
                this.UpdateStyles();
            }
            this.Invalidate(true);
        }

        /// <inheritdoc />
        protected override void OnDrawItem(DrawItemEventArgs e) {
            if (!this.darkRendering) {
                using (Brush brush = new SolidBrush(this.colors.ControlColor))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }

                TextRenderer.DrawText(e.Graphics, this.TabPages[e.Index].Text, e.Font, e.Bounds,
                    this.colors.TextColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                base.OnDrawItem(e);
                return;
            }
            this.DrawDarkTab(e.Graphics, e.Index, this.GetDarkTabBounds(e.Index));
        }

        /// <inheritdoc />
        protected override void OnSelectedIndexChanged(EventArgs e) {
            base.OnSelectedIndexChanged(e);
            this.InvalidateHeader();
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (!this.darkRendering)
            {
                return;
            }

            int index = -1;
            for (int i = 0; i < this.TabCount; i++) {
                if (this.GetDarkTabBounds(i).Contains(e.Location)) {
                    index = i;
                    break;
                }
            }
            if (index != this.hotTab) {
                this.hotTab = index;
                this.InvalidateHeader();
            }
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            if (this.hotTab != -1) {
                this.hotTab = -1;
                this.InvalidateHeader();
            }
        }

        private void InvalidateHeader() {
            this.Invalidate(new Rectangle(0, 0, this.ClientSize.Width, Math.Max(0, this.DisplayRectangle.Top)));
        }

        private Rectangle GetDarkTabBounds(int index) {
            return this.GetTabRect(index);
        }

        private void DrawDarkTab(Graphics graphics, int index, Rectangle bounds) {
            bool selected = index == this.SelectedIndex;
            Rectangle textBounds = bounds;
            if (selected)
            {
                bounds.Height = Math.Max(bounds.Height, this.DisplayRectangle.Top - bounds.Top);
            }

            Color background = selected ? this.colors.ControlLightColor :
                index == this.hotTab ? this.colors.HoverColor : this.colors.ControlColor;
            using (Brush brush = new SolidBrush(background))
            {
                graphics.FillRectangle(brush, bounds);
            }

            int right = bounds.Right - 1;
            int bottom = bounds.Bottom - 1;
            using (Pen border = new Pen(this.colors.ControlDarkColor)) {
                graphics.DrawLine(border, bounds.Left + 1, bounds.Top, right - 1, bounds.Top);
                graphics.DrawLine(border, bounds.Left, bounds.Top + 1, bounds.Left, bottom);
                graphics.DrawLine(border, right, bounds.Top + 1, right, bottom);
                if (!selected)
                {
                    graphics.DrawLine(border, bounds.Left, bottom, right, bottom);
                }
            }
            int textInset = Math.Max(4, (int)Math.Ceiling(4 * graphics.DpiX / 96f));
            textBounds.Inflate(-textInset, -1);
            TextRenderer.DrawText(graphics, this.TabPages[index].Text, this.Font, textBounds,
                selected ? this.colors.TextColor : this.colors.SecondaryTextColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping);
        }

        /// <inheritdoc />
        protected override void OnPaintBackground(PaintEventArgs pevent) {
            if (!this.GetStyle(ControlStyles.UserPaint)) {
                base.OnPaintBackground(pevent);
                return;
            }
            using (Brush brush = new SolidBrush(this.colors.ControlColor))
            {
                pevent.Graphics.FillRectangle(brush, pevent.ClipRectangle);
            }
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if (this.GetStyle(ControlStyles.UserPaint))
            {
                this.DrawDarkFrame(e.Graphics);
            }
        }

        private void DrawDarkFrame(Graphics graphics) {
            Rectangle client = this.ClientRectangle;
            Rectangle page = this.DisplayRectangle;
            using (Brush brush = new SolidBrush(this.colors.ControlColor)) {
                var state = graphics.Save();
                try {
                    Rectangle header = new Rectangle(0, 0, client.Width, Math.Max(0, page.Top));
                    graphics.SetClip(header, System.Drawing.Drawing2D.CombineMode.Intersect);
                    graphics.FillRectangle(brush, header);
                    using (Pen border = new Pen(this.colors.ControlDarkColor))
                    {
                        graphics.DrawLine(border, 0, page.Top - 1, client.Width, page.Top - 1);
                    }

                    for (int i = 0; i < this.TabCount; i++)
                    {
                        if (i != this.SelectedIndex)
                        {
                            this.DrawDarkTab(graphics, i, this.GetDarkTabBounds(i));
                        }
                    }

                    if (this.SelectedIndex >= 0)
                    {
                        this.DrawDarkTab(graphics, this.SelectedIndex, this.GetDarkTabBounds(this.SelectedIndex));
                    }
                } finally {
                    graphics.Restore(state);
                }
                graphics.FillRectangle(brush, 0, page.Top, page.Left, Math.Max(0, client.Bottom - page.Top));
                graphics.FillRectangle(brush, page.Right, page.Top, Math.Max(0, client.Right - page.Right), Math.Max(0, client.Bottom - page.Top));
                graphics.FillRectangle(brush, 0, page.Bottom, client.Width, Math.Max(0, client.Bottom - page.Bottom));
            }
        }
    }
}
#endif
