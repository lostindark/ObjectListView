using System.Drawing;
using System.Drawing.Drawing2D;

#if OLV_DESIGN_CLIENT
namespace BrightIdeasSoftware.Design.Client.Printing
#else
namespace BrightIdeasSoftware
#endif
{
    internal static class BrushPenDrawing
    {
        public static Brush PrepareBrush(Brush value, RectangleF bounds) {
            if (value is not LinearGradientBrush gradient)
            {
                return value;
            }

            var resized = new LinearGradientBrush(bounds, gradient.LinearColors[0], gradient.LinearColors[1], 0.0);
            resized.Blend = gradient.Blend;
            resized.WrapMode = gradient.WrapMode;
            resized.Transform = gradient.Transform;
            return resized;
        }

        public static Brush PrepareBrush(Brush value, Rectangle bounds) {
            if (value is not LinearGradientBrush gradient)
            {
                return value;
            }

            var resized = new LinearGradientBrush(bounds, gradient.LinearColors[0], gradient.LinearColors[1], 0.0);
            resized.Blend = gradient.Blend;
            resized.WrapMode = gradient.WrapMode;
            resized.Transform = gradient.Transform;
            return resized;
        }

        public static Pen PreparePen(Pen value, RectangleF bounds) {
            if (bounds.Height == 0)
            {
                bounds.Height = value.Width;
            }
            if (bounds.Width == 0)
            {
                bounds.Width = value.Width;
            }
            value.Brush = PrepareBrush(value.Brush, bounds);
            return value;
        }

        public static Pen PreparePen(Pen value, Rectangle bounds) {
            value.Brush = PrepareBrush(value.Brush, bounds);
            return value;
        }
    }
}
