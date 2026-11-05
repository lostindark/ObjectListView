using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BrightIdeasSoftware {
    // Render native geometry to a composited surface, retaining Windows input and accessibility.
    internal static class NativeListViewTheme {
        private const uint Invisible = 0x8000;
        private const uint Offscreen = 0x10000;
        private const uint Unavailable = 1;
        private const uint Pressed = 8;

        internal static bool ShouldPaint {
            get {
                if (SystemInformation.HighContrast)
                {
                    return false;
                }
                bool overrides = ObjectListViewTheme.HasScrollBarColorOverrides;
#if NET10_0_OR_GREATER
                if (Application.IsDarkModeEnabled && !overrides)
                {
                    return false;
                }
#endif
                return ObjectListViewTheme.IsDarkMode || overrides;
            }
        }

        internal static bool IsPaintMessage(int message) {
            return message == 0x0F || message == 0x05 || message == 0x0A
                || message == 0x85 || message == 0x86 || message == 0xA0 || message == 0xA1 || message == 0xA2
                || message == 0x114 || message == 0x115 || message == 0x118
                || message == 0x202 || message == 0x20A || message == 0x20E
                || message == 0x215 || message == 0x2A2 || message == 0x2A3
                || message == 0x31A || message == 0x317;
        }

        internal static IOverlay CreateOverlay() => new ScrollBarOverlay();

        internal static Rectangle GetScreenBounds(Control list) {
            if (GetWindowRect(list.Handle, out NativeMethods.RECT bounds))
            {
                return Rectangle.FromLTRB(bounds.left, bounds.top, bounds.right, bounds.bottom);
            }
            Trace.TraceWarning("ObjectListView could not get its overlay bounds: {0}.", Marshal.GetLastWin32Error());
            return Rectangle.Empty;
        }

        private sealed class ScrollBarOverlay : AbstractOverlay {
            public ScrollBarOverlay() {
                this.Transparency = 255;
            }

            public override void Draw(ObjectListViewBase list, Graphics graphics, Rectangle bounds) {
                DrawControlChrome(list, graphics);
            }
        }

        internal static void DrawControlChrome(Control control, Graphics graphics) {
            IntPtr dc = graphics.GetHdc();
            try {
                _ = Paint(control, dc, true, false);
            } finally {
                graphics.ReleaseHdc(dc);
            }
        }

        internal static bool Paint(Control list, IntPtr printContext, bool paintScrollBars, bool paintEmptyClient) {
            if (!GetWindowRect(list.Handle, out NativeMethods.RECT window)) {
                Trace.TraceWarning("ObjectListView could not get native scrollbar window bounds: {0}.", Marshal.GetLastWin32Error());
                return false;
            }
            // Standalone scrollbar controls expose their geometry as OBJID_CLIENT, not non-client bars.
            ScrollBarInfo horizontal = ReadInfo(list.Handle, list is HScrollBar ? -4 : -6);
            ScrollBarInfo vertical = ReadInfo(list.Handle, list is VScrollBar ? -4 : -5);
            bool hasHorizontal = IsVisible(horizontal);
            bool hasVertical = IsVisible(vertical);
            IntPtr dc = printContext == IntPtr.Zero ? GetWindowDC(list.Handle) : printContext;
            if (dc == IntPtr.Zero) {
                Trace.TraceWarning("ObjectListView could not acquire the scrollbar drawing context: {0}.", Marshal.GetLastWin32Error());
                return false;
            }

            int savedContext = 0;
            try {
                savedContext = SaveDC(dc);
                if (savedContext == 0) {
                    Trace.TraceWarning("ObjectListView could not save the native drawing context: {0}.", Marshal.GetLastWin32Error());
                    return false;
                }
                uint layout = GetLayout(dc);
                if (layout == uint.MaxValue) {
                    Trace.TraceWarning("ObjectListView could not read the native drawing layout: {0}.", Marshal.GetLastWin32Error());
                    return false;
                }
                if ((layout & 1) != 0) {
                    if (SetLayout(dc, layout & ~1u) == uint.MaxValue) {
                        Trace.TraceWarning("ObjectListView could not set the native drawing layout: {0}.", Marshal.GetLastWin32Error());
                        return false;
                    }
                }
                using (Graphics graphics = Graphics.FromHdc(dc)) {
                    Rectangle h = RelativeBounds(horizontal, window);
                    Rectangle v = RelativeBounds(vertical, window);
                    int width = window.right - window.left;
                    int height = window.bottom - window.top;
                    int frameX = Math.Max(0, (width - list.ClientSize.Width - (hasVertical ? v.Width : 0)) / 2);
                    int frameY = Math.Max(0, (height - list.ClientSize.Height - (hasHorizontal ? h.Height : 0)) / 2);
                    if (paintEmptyClient && list is ObjectListViewBase emptyList) {
                        GraphicsState state = graphics.Save();
                        try {
                            int clientX = frameX + (hasVertical && v.Left < width / 2 ? v.Width : 0);
                            graphics.TranslateTransform(clientX, frameY);
                            emptyList.DrawEmptyListBackground(graphics);
                        } finally {
                            graphics.Restore(state);
                        }
                    }
                    if (!paintScrollBars)
                    {
                        return true;
                    }
                    Point pointer = Cursor.Position;
                    pointer.Offset(-window.left, -window.top);
                    if (hasHorizontal)
                    {
                        PaintBufferedBar(graphics, horizontal, h, false, list.Enabled, pointer);
                    }
                    if (hasVertical)
                    {
                        PaintBufferedBar(graphics, vertical, v, true, list.Enabled, pointer);
                    }
                    if (hasHorizontal && hasVertical) {
                        using (Brush brush = new SolidBrush(ObjectListViewTheme.Current.ScrollBarTrackColor))
                        {
                            graphics.FillRectangle(brush, v.Left, h.Top, v.Width, h.Height);
                        }
                    }
                    BorderStyle border = list is ListView view ? view.BorderStyle
                        : list is TextBoxBase text ? text.BorderStyle : BorderStyle.None;
                    if (border != BorderStyle.None) {
                        using (Pen pen = new Pen(ObjectListViewTheme.Current.ControlDarkColor)) {
                            for (int i = 0; i < Math.Min(frameX, frameY); i++)
                            {
                                graphics.DrawRectangle(pen, i, i, width - i * 2 - 1, height - i * 2 - 1);
                            }
                        }
                    }
                }
                return true;
            } finally {
                if (savedContext != 0 && !RestoreDC(dc, savedContext))
                {
                    Trace.TraceWarning("ObjectListView could not restore the native drawing context: {0}.", Marshal.GetLastWin32Error());
                }
                if (printContext == IntPtr.Zero && ReleaseDC(list.Handle, dc) == 0)
                {
                    Trace.TraceWarning("ObjectListView could not release the native drawing context.");
                }
            }
        }

        private static void PaintBufferedBar(Graphics target, ScrollBarInfo info, Rectangle bounds, bool vertical, bool enabled, Point pointer) {
            using (BufferedGraphics buffer = BufferedGraphicsManager.Current.Allocate(target, bounds))
            {
                PaintBar(buffer.Graphics, info, bounds, vertical, enabled, pointer);
                buffer.Render(target);
            }
        }

        private static ScrollBarInfo ReadInfo(IntPtr window, int id) {
            ScrollBarInfo info = new ScrollBarInfo { Size = Marshal.SizeOf<ScrollBarInfo>(), States = new uint[6] };
            if (!GetScrollBarInfo(window, id, ref info))
            {
                info.States[0] = Invisible;
            }
            return info;
        }

        private static bool IsVisible(ScrollBarInfo info) {
            return (info.States[0] & (Invisible | Offscreen)) == 0
                && info.Bounds.right > info.Bounds.left && info.Bounds.bottom > info.Bounds.top;
        }

        private static Rectangle RelativeBounds(ScrollBarInfo info, NativeMethods.RECT window) {
            return Rectangle.FromLTRB(info.Bounds.left - window.left, info.Bounds.top - window.top,
                info.Bounds.right - window.left, info.Bounds.bottom - window.top);
        }

        private static void PaintBar(Graphics graphics, ScrollBarInfo info, Rectangle bounds, bool vertical, bool enabled, Point pointer) {
            using (Brush track = new SolidBrush(ObjectListViewTheme.Current.ScrollBarTrackColor))
            {
                graphics.FillRectangle(track, bounds);
            }

            int length = vertical ? bounds.Height : bounds.Width;
            int button = Math.Max(0, Math.Min(info.LineButton, length / 2));
            Rectangle leading = vertical ? new Rectangle(bounds.X, bounds.Y, bounds.Width, button)
                : new Rectangle(bounds.X, bounds.Y, button, bounds.Height);
            Rectangle trailing = vertical ? new Rectangle(bounds.X, bounds.Bottom - button, bounds.Width, button)
                : new Rectangle(bounds.Right - button, bounds.Y, button, bounds.Height);
            bool pointerInTrack = bounds.Contains(pointer);
            if (pointerInTrack)
            {
                DrawArrow(graphics, leading, vertical, false, enabled && (info.States[1] & Unavailable) == 0);
                DrawArrow(graphics, trailing, vertical, true, enabled && (info.States[5] & Unavailable) == 0);
            }

            if ((info.States[3] & (Invisible | Offscreen)) != 0)
            {
                return;
            }
            int start = Math.Max(button, info.ThumbStart);
            int end = Math.Min(length - button, info.ThumbEnd);
            if (end <= start)
            {
                return;
            }
            Rectangle thumb = vertical ? new Rectangle(bounds.X, bounds.Y + start, bounds.Width, end - start)
                : new Rectangle(bounds.X + start, bounds.Y, end - start, bounds.Height);
            bool hovered = thumb.Contains(pointer);
            int crossSize = vertical ? bounds.Width : bounds.Height;
            bool expanded = enabled && pointerInTrack;
            int thickness = Math.Max(3, crossSize / (expanded ? 2 : 4));
            if (vertical)
            {
                thumb.X += (thumb.Width - thickness) / 2;
                thumb.Width = thickness;
                thumb.Inflate(0, -1);
            }
            else
            {
                thumb.Y += (thumb.Height - thickness) / 2;
                thumb.Height = thickness;
                thumb.Inflate(-1, 0);
            }
            if (thumb.Width <= 0 || thumb.Height <= 0)
            {
                return;
            }
            Color color = !enabled || (info.States[3] & Unavailable) != 0 ? ObjectListViewTheme.Current.ControlDarkColor
                : (info.States[3] & Pressed) != 0 || hovered
                    ? ControlPaint.Light(ObjectListViewTheme.Current.ScrollBarThumbColor) : ObjectListViewTheme.Current.ScrollBarThumbColor;
            int diameter = Math.Min(thumb.Width, thumb.Height);
            using (GraphicsPath path = new GraphicsPath())
            using (Brush brush = new SolidBrush(color)) {
                path.AddArc(thumb.Left, thumb.Top, diameter, diameter, 180, 90);
                path.AddArc(thumb.Right - diameter, thumb.Top, diameter, diameter, 270, 90);
                path.AddArc(thumb.Right - diameter, thumb.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(thumb.Left, thumb.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.FillPath(brush, path);
                graphics.SmoothingMode = SmoothingMode.Default;
            }
        }

        private static void DrawArrow(Graphics graphics, Rectangle bounds, bool vertical, bool forward, bool enabled) {
            if (bounds.Width < 4 || bounds.Height < 4)
            {
                return;
            }
            float x = bounds.Left + bounds.Width / 2f;
            float y = bounds.Top + bounds.Height / 2f;
            float radius = Math.Max(2, Math.Min(bounds.Width, bounds.Height) / 4f);
            float direction = forward ? 1 : -1;
            PointF[] points = vertical
                ? new[] { new PointF(x - radius, y - direction * radius * 0.75f), new PointF(x, y + direction * radius * 0.75f), new PointF(x + radius, y - direction * radius * 0.75f) }
                : new[] { new PointF(x - direction * radius * 0.75f, y - radius), new PointF(x + direction * radius * 0.75f, y), new PointF(x - direction * radius * 0.75f, y + radius) };
            var state = graphics.Save();
            try {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Brush brush = new SolidBrush(enabled ? ObjectListViewTheme.Current.ScrollBarArrowColor : ObjectListViewTheme.Current.ControlDarkColor))
                {
                    graphics.FillPolygon(brush, points);
                }
            } finally {
                graphics.Restore(state);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ScrollBarInfo {
            public int Size;
            public NativeMethods.RECT Bounds;
            public int LineButton;
            public int ThumbStart;
            public int ThumbEnd;
            public int Reserved;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public uint[] States;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr window, out NativeMethods.RECT bounds);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetScrollBarInfo(IntPtr window, int id, ref ScrollBarInfo info);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowDC(IntPtr window);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern uint GetLayout(IntPtr dc);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern uint SetLayout(IntPtr dc, uint layout);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern int SaveDC(IntPtr dc);
        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RestoreDC(IntPtr dc, int savedContext);
    }
}
