using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using BrightIdeasSoftware.Design;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    [TestClass]
    public class TestPrinterEditorState
    {
        private sealed class CustomPenData : PenData { }
        private sealed class CustomBrushData : SolidBrushData { }

        [TestMethod]
        public void Test_PrinterEditorTransport_RejectsUnsupportedTypesWithoutLosingCustomState() {
            Assert.ThrowsExactly<NotSupportedException>(() => PrinterEditorState.Encode(new CustomPenData(), true));
            Assert.ThrowsExactly<NotSupportedException>(() => PrinterEditorState.Encode(new CustomBrushData(), false));
            Assert.ThrowsExactly<NotSupportedException>(() =>
                PrinterEditorState.Encode(new PenData(new TextureBrushData()), true));
        }

        [TestMethod]
        public void Test_PrinterEditorTransport_ClonesPenWithoutChangingOriginal() {
            var original = new PenData(new SolidBrushData { Color = Color.CornflowerBlue, Alpha = 123 }) {
                Width = 2.5f, StartCap = LineCap.Round, EndCap = LineCap.ArrowAnchor,
                DashCap = DashCap.Triangle, DashStyle = DashStyle.DashDot, LineJoin = LineJoin.Bevel
            };
            var copy = (PenData)PrinterEditorState.Decode(PrinterEditorState.Encode(original, true), true);
            Assert.AreEqual(original.Width, copy.Width);
            Assert.AreEqual(original.StartCap, copy.StartCap);
            Assert.AreEqual(original.EndCap, copy.EndCap);
            Assert.AreEqual(original.DashCap, copy.DashCap);
            Assert.AreEqual(original.DashStyle, copy.DashStyle);
            Assert.AreEqual(original.LineJoin, copy.LineJoin);
            var brush = (SolidBrushData)copy.Brush;
            Assert.AreEqual(Color.CornflowerBlue, brush.Color);
            Assert.AreEqual(123, brush.Alpha);
            brush.Color = Color.Red;
            Assert.AreEqual(Color.CornflowerBlue, ((SolidBrushData)original.Brush).Color);
        }

        [TestMethod]
        public void Test_PrinterEditorTransport_GradientHatchAndNull() {
            var gradient = new LinearGradientBrushData {
                FromColor = Color.FromArgb(32, 45, 67, 89), ToColor = Color.Empty,
                GradientMode = LinearGradientMode.BackwardDiagonal
            };
            var copy = (LinearGradientBrushData)PrinterEditorState.Decode(PrinterEditorState.Encode(gradient, false), false);
            Assert.AreEqual(gradient.FromColor, copy.FromColor);
            Assert.AreEqual(gradient.ToColor, copy.ToColor);
            Assert.AreEqual(gradient.GradientMode, copy.GradientMode);
            var hatch = new HatchBrushData {
                ForegroundColor = Color.Aqua, BackgroundColor = Color.Transparent, HatchStyle = HatchStyle.DiagonalCross
            };
            var hatchCopy = (HatchBrushData)PrinterEditorState.Decode(PrinterEditorState.Encode(hatch, false), false);
            Assert.AreEqual(hatch.ForegroundColor, hatchCopy.ForegroundColor);
            Assert.AreEqual(hatch.BackgroundColor, hatchCopy.BackgroundColor);
            Assert.AreEqual(hatch.HatchStyle, hatchCopy.HatchStyle);
            Assert.IsNull(PrinterEditorState.Decode(PrinterEditorState.Encode(null, true), true));
            Assert.IsNull(PrinterEditorState.Decode(PrinterEditorState.Encode(null, false), false));
        }
    }
}
