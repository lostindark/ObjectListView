using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests {
    [STATestClass]
    [SuppressMessage("Design", "CA1001", Justification = "TestCleanup disposes the hosted form and image list.")]
    public class TestRenderingLayout {
        private static readonly string[] Rows = { "row" };
        private static readonly string[] WrappedRows = { "several words that cannot fit on a single line" };
        private static readonly string[] AlignmentRows = { "WWWWWWWW\nI" };
        private static readonly int[] TestDpis = { 96, 144, 192 };
        private Form form;
        private ImageList images;
        private Bitmap image;

        [TestInitialize]
        public void SetUp() {
            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            form = new Form {
                StartPosition = FormStartPosition.Manual,
                Bounds = new Rectangle(screen.Location, new Size(Math.Min(520, screen.Width), Math.Min(280, screen.Height))),
                ShowInTaskbar = false
            };
            images = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
            image = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(image)) {
                g.Clear(Color.Magenta);
                images.Images.Add("image", image);
            }
        }

        [TestCleanup]
        public void TearDown() {
            form.Dispose();
            images.Dispose();
            image.Dispose();
        }

        [TestMethod]
        [DataRow(false, false, HorizontalAlignment.Left)]
        [DataRow(false, false, HorizontalAlignment.Center)]
        [DataRow(false, false, HorizontalAlignment.Right)]
        [DataRow(false, true, HorizontalAlignment.Left)]
        [DataRow(false, true, HorizontalAlignment.Center)]
        [DataRow(false, true, HorizontalAlignment.Right)]
        [DataRow(true, false, HorizontalAlignment.Center)]
        [DataRow(true, true, HorizontalAlignment.Right)]
        public void PaintingHitTestingAndEditingShareLiveCellGeometry(bool virtualMode, bool rtl, HorizontalAlignment alignment) {
            using (ObjectListViewBase list = virtualMode ? (ObjectListViewBase)new FastObjectListView() : new LayoutList())
            using (RecordingRenderer renderer = new RecordingRenderer()) {
                ConfigureList(list, rtl);
                list.CheckBoxes = true;
                list.RowHeight = 48;
                list.RowAppearance.Normal.Padding = new Rectangle(9, 4, 7, 6);
                list.RowAppearance.Normal.HorizontalAlignment = alignment;
                list.RowAppearance.Normal.VerticalAlignment = StringAlignment.Far;
                list.DefaultRenderer = renderer;
                list.GetColumn(0).ImageGetter = x => "image";
                Show(list);
                list.ModelObjects = new object[] { "some text", "second" };
                list.GetColumn(1).DisplayIndex = 0;
                list.LowLevelScroll(35, 0);
                list.Refresh();
                Application.DoEvents();

                OLVListItem item = list.GetItem(0);
                CellLayout painted = renderer.Layouts[0];
                using (Graphics g = list.CreateGraphics()) {
                    CellLayout layout = renderer.GetCellLayout(g, item.GetSubItemBounds(0), item, 0);
                    AssertLayoutEqual(layout, painted);
                    AssertHit(list, layout.CheckBoxBounds, HitTestLocation.CheckBox);
                    AssertHit(list, layout.ImageBounds, HitTestLocation.Image);
                    AssertHit(list, layout.TextBounds, HitTestLocation.Text);
                    Rectangle editor = renderer.GetEditRectangle(g, layout.CellBounds, item, 0, new Size(100, 23));
                    Assert.AreEqual(rtl ? layout.TextBounds.Right : layout.TextBounds.Left, rtl ? editor.Right : editor.Left);
                    Assert.IsTrue(layout.ContentBounds.Left <= editor.Left && editor.Right <= layout.ContentBounds.Right);
                    Assert.AreEqual(editor, list.CalculateCellEditorBounds(item, 0, new Size(100, 23)));
                    Assert.AreEqual(layout.CellBounds, list.CalculateCellBounds(item, 0));
                    Assert.IsFalse(editor.IntersectsWith(layout.CheckBoxBounds));
                    Assert.IsFalse(editor.IntersectsWith(layout.ImageBounds));
                }
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void TreeIndentExpansionAndEditorUseThePaintedLayout(bool rtl) {
            using (TreeListView tree = new TreeListView()) {
                ConfigureList(tree, rtl);
                tree.RowHeight = 46;
                tree.CheckBoxes = true;
                tree.CellPadding = new Rectangle(10, 4, 6, 5);
                tree.CanExpandGetter = x => (string)x != "leaf";
                tree.ChildrenGetter = x => new[] { (string)x == "root" ? "child" : "leaf" };
                tree.GetColumn(0).ImageGetter = x => "image";
                RecordingTreeRenderer renderer = new RecordingTreeRenderer();
                tree.TreeColumnRenderer = renderer;
                Show(tree);
                tree.Roots = new[] { "root" };
                tree.Expand("root");
                tree.Expand("child");
                tree.GetColumn(1).DisplayIndex = 0;
                tree.LowLevelScroll(30, 0);
                tree.Refresh();
                OLVListItem item = tree.ModelToItem("child");
                using (Graphics g = tree.CreateGraphics()) {
                    CellLayout layout = renderer.GetCellLayout(g, item.GetSubItemBounds(0), item, 0);
                    Assert.AreEqual(layout.ExpandButtonBounds, renderer.Glyphs[item.RowObject]);
                    AssertLayoutEqual(layout, renderer.Layouts[item.RowObject]);
                    AssertHit(tree, layout.ExpandButtonBounds, HitTestLocation.ExpandButton);
                    AssertHit(tree, layout.CheckBoxBounds, HitTestLocation.CheckBox);
                    AssertHit(tree, layout.ImageBounds, HitTestLocation.Image);
                    AssertHit(tree, layout.TextBounds, HitTestLocation.Text);
                    Rectangle editor = tree.CalculateCellEditorBounds(item, 0, new Size(100, 22));
                    Assert.AreEqual(rtl ? layout.TextBounds.Right : layout.TextBounds.Left, rtl ? editor.Right : editor.Left);
                    Assert.IsFalse(editor.IntersectsWith(layout.ExpandButtonBounds));
                    tree.Collapse("child");
                    Assert.AreEqual(2, tree.GetItemCount());
                    Assert.AreEqual(AccessibleRole.List, tree.AccessibilityObject.Role);
                }
            }
        }

        [TestMethod]
        public void CheckboxAndButtonTargetsApplyPaddingExactlyOnce() {
            using (LayoutList list = new LayoutList()) {
                ConfigureList(list, false);
                list.RowHeight = 52;
                OLVColumn column = list.GetColumn(1);
                column.CellPadding = new Rectangle(17, 7, 12, 9);
                column.CheckBoxes = true;
                column.AspectGetter = x => true;
                Show(list);
                list.SetObjects(Rows);
                OLVListItem item = list.GetItem(0);
                using (Graphics g = list.CreateGraphics()) {
                    BaseRenderer renderer = (BaseRenderer)list.GetCellRenderer(item.RowObject, column);
                    CellLayout layout = renderer.GetCellLayout(g, item.GetSubItemBounds(1), item, 1);
                    AssertHit(list, layout.CheckBoxBounds, HitTestLocation.CheckBox);
                    Point padding = new Point(layout.CellBounds.Left + 2, layout.CellBounds.Top + 2);
                    Assert.AreEqual(HitTestLocation.InCell, list.OlvHitTest(padding.X, padding.Y).HitTestLocation);
                }

                column.CheckBoxes = false;
                column.AspectGetter = x => x;
                using (ColumnButtonRenderer renderer = new ColumnButtonRenderer {
                    SizingMode = OLVColumn.ButtonSizingMode.FixedBounds, ButtonSize = new Size(90, 30)
                }) {
                    column.Renderer = renderer;
                    list.RebuildColumns();
                    item = list.GetItem(0);
                    Rectangle cell = item.GetSubItemBounds(1);
                    Rectangle painted;
                    using (Graphics g = list.CreateGraphics())
                        painted = renderer.GetCellLayout(g, cell, item, 1).ButtonBounds;
                    AssertHit(list, painted, HitTestLocation.Button);
                    Point edge = new Point(painted.Left + 1, painted.Top + painted.Height / 2);
                    Assert.AreEqual(HitTestLocation.Button, list.OlvHitTest(edge.X, edge.Y).HitTestLocation);
                }
            }
        }

        [TestMethod]
        public void TooltipTargetsComeFromHitGeometryAndReachStyleCallbacks() {
            using (LayoutList list = new LayoutList()) {
                ConfigureList(list, false);
                list.GetColumn(0).ImageGetter = x => "image";
                Show(list);
                list.SetObjects(Rows);
                OLVListItem item = list.GetItem(0);
                BaseRenderer renderer = (BaseRenderer)list.GetCellRenderer(item.RowObject, list.GetColumn(0));
                using (Graphics g = list.CreateGraphics()) {
                    CellLayout layout = renderer.GetCellLayout(g, item.GetSubItemBounds(0), item, 0);
                    list.ForcedHitPoint = Center(layout.ImageBounds);
                    list.CellToolTipGetter = (column, model) => "tip";
                    Rectangle styleBounds = Rectangle.Empty;
                    list.StyleResolving += (sender, e) => {
                        if (e.Context.Target == StyleTarget.ToolTip)
                            styleBounds = e.Context.Bounds;
                    };
                    ToolTipShowingEventArgs args = list.GetToolTipArgs();
                    Assert.AreEqual(layout.ImageBounds, args.TargetBounds);
                    Assert.AreEqual(layout.ImageBounds, styleBounds);
                }
            }
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public void HeaderImagesVerticalTextAndCheckboxTargetsHonorSharedAlignment(bool rtl, bool vertical) {
            using (LayoutList list = new LayoutList()) {
                ConfigureList(list, rtl);
                list.HeaderMinimumHeight = 100;
                list.HeaderMaximumHeight = 100;
                OLVColumn column = list.GetColumn(0);
                column.HeaderImageKey = "image";
                column.HeaderCheckBox = true;
                column.IsHeaderVertical = vertical;
                column.HeaderAppearance.Normal.Padding = new Rectangle(11, 6, 9, 8);
                column.HeaderAppearance.Normal.HorizontalAlignment = HorizontalAlignment.Right;
                column.HeaderAppearance.Normal.VerticalAlignment = StringAlignment.Far;
                column.HeaderTextAlign = HorizontalAlignment.Left;
                Show(list);
                list.SetObjects(Rows);
                list.GetColumn(1).DisplayIndex = 0;
                list.LowLevelScroll(30, 0);
                list.Sort(column, SortOrder.Ascending);
                list.Refresh();
                HeaderControl header = list.HeaderControl;
                CellLayout layout = header.GetCellLayout(column);
                Assert.AreEqual(Rectangle.Intersect(layout.CellBounds, layout.CheckBoxBounds), header.GetCheckBoxBounds(column));
                Point point = Center(layout.CheckBoxBounds);
                ClientToScreen(header.Handle, ref point);
                point = list.PointToClient(point);
                OlvListViewHitTestInfo hit = list.OlvHitTest(point.X, point.Y);
                Assert.AreEqual(HitTestLocation.HeaderCheckBox, hit.HitTestLocation);
                Assert.IsFalse(layout.ImageBounds.IntersectsWith(layout.TextBounds));
                Assert.IsTrue(layout.ContentBounds.Bottom >= layout.TextBounds.Bottom);
                if (vertical)
                    Assert.IsTrue(layout.ImageBounds.Bottom <= layout.TextBounds.Top);
                else
                    Assert.IsTrue(rtl ? layout.ImageBounds.Left >= layout.TextBounds.Right : layout.ImageBounds.Right <= layout.TextBounds.Left);
                using (Bitmap bitmap = new Bitmap(Math.Max(1, header.ClientRectangle.Width), 120))
                using (Graphics g = Graphics.FromImage(bitmap)) {
                    g.Clear(Color.White);
                    g.TranslateTransform(2, 3);
                    using (Matrix before = g.Transform) {
                        typeof(HeaderControl).GetMethod("CustomDrawHeaderCell", BindingFlags.Instance | BindingFlags.NonPublic)
                            .Invoke(header, new object[] { g, column.Index, 0 });
                        using (Matrix after = g.Transform)
                            CollectionAssert.AreEqual(before.Elements, after.Elements);
                    }
                    Assert.IsTrue(ContainsColor(bitmap, Color.Magenta, new Rectangle(layout.ImageBounds.X + 2,
                        layout.ImageBounds.Y + 3, layout.ImageBounds.Width, layout.ImageBounds.Height)));
                }
                column.HeaderAppearance.Normal.HorizontalAlignment = HorizontalAlignment.Left;
                column.HeaderAppearance.Normal.VerticalAlignment = StringAlignment.Near;
                CellLayout moved = header.GetCellLayout(column);
                Assert.IsTrue(moved.ImageBounds.Left < layout.ImageBounds.Left);
                Assert.IsTrue(moved.ImageBounds.Top < layout.ImageBounds.Top);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SharedTargetsPreserveCheckboxKeyboardSortingResizingAndEditorInteraction(bool rtl) {
            using (LayoutList list = new LayoutList()) {
                ConfigureList(list, rtl);
                list.CellEditActivation = ObjectListViewBase.CellEditActivateMode.DoubleClick;
                list.CheckBoxes = true;
                list.RowHeight = 42;
                list.CellPadding = new Rectangle(8, 3, 5, 4);
                Show(list);
                list.SetObjects(Rows);
                OLVListItem item = list.GetItem(0);
                BaseRenderer renderer = (BaseRenderer)list.GetCellRenderer(item.RowObject, list.GetColumn(0));
                using (Graphics g = list.CreateGraphics()) {
                    CellLayout layout = renderer.GetCellLayout(g, item.GetSubItemBounds(0), item, 0);
                    list.ClickTarget(Center(layout.CheckBoxBounds));
                    Assert.AreEqual(CheckState.Checked, list.GetItem(0).CheckState);
                    item.Selected = true;
                    list.PressSpace();
                    Assert.AreEqual(CheckState.Unchecked, list.GetItem(0).CheckState);
                    list.ClickHeader(0);
                    Assert.AreSame(list.GetColumn(0), list.SortDescriptions[0].Column);
                    bool resized = false;
                    list.ColumnWidthChanged += (sender, e) => resized = true;
                    list.GetColumn(0).Width += 25;
                    Assert.IsTrue(resized);
                    item = list.GetItem(0);
                    list.EditSubItem(item, 0);
                    Assert.IsTrue(list.IsCellEditing);
                    Rectangle expected = list.CalculateCellEditorBounds(item, 0, list.CellEditor.PreferredSize);
                    Assert.AreEqual(expected, list.CellEditor.Bounds);
                    list.CancelCellEdit();
                    Assert.IsFalse(list.IsCellEditing);
                    Assert.AreEqual(AccessibleRole.List, list.AccessibilityObject.Role);
                }
                list.CellPadding = null;
                list.OwnerDraw = false;
                list.Refresh();
                Assert.IsFalse(list.OwnerDraw);
            }
        }

        [TestMethod]
        public void LayoutIsRecomputedForDpiFontImageAndPaddingChangesAndHandlesNarrowCells() {
            using (LayoutList list = new LayoutList())
            using (RecordingRenderer renderer = new RecordingRenderer()) {
                ConfigureList(list, false);
                list.DefaultRenderer = renderer;
                list.CheckBoxes = true;
                list.GetColumn(0).ImageGetter = x => "image";
                Show(list);
                list.SetObjects(Rows);
                OLVListItem item = list.GetItem(0);
                Font originalFont = list.Font;
                using (Bitmap bitmap = new Bitmap(400, 150)) {
                    foreach (int dpi in TestDpis) {
                        bitmap.SetResolution(dpi, dpi);
                        using (Graphics g = Graphics.FromImage(bitmap))
                        using (Font font = new Font(originalFont.FontFamily, 9f * dpi / 96f)) {
                            list.Font = font;
                            Rectangle cell = new Rectangle(5, 6, 300, 100);
                            CellLayout layout = renderer.GetCellLayout(g, cell, item, 0);
                            renderer.ConfigureSubItem(new DrawListViewSubItemEventArgs(g, cell, item, item.SubItems[0],
                                0, 0, list.GetColumn(0), ListViewItemStates.Default), cell, item.RowObject);
                            renderer.Render(g, cell);
                            AssertLayoutEqual(layout, renderer.Layouts[0]);
                            list.Font = originalFont;
                        }
                    }
                }
                images.ImageSize = new Size(24, 24);
                images.Images.Add("image", image);
                using (Graphics g = list.CreateGraphics()) {
                    CellLayout layout = renderer.GetCellLayout(g, item.GetSubItemBounds(0), item, 0);
                    Assert.AreEqual(images.ImageSize, layout.ImageBounds.Size);
                }
                list.CellPadding = new Rectangle(20, 20, 20, 20);
                list.GetColumn(0).Width = 12;
                using (Graphics g = list.CreateGraphics()) {
                    CellLayout layout = renderer.GetCellLayout(g, item.GetSubItemBounds(0), item, 0);
                    Rectangle editor = renderer.GetEditRectangle(g, layout.CellBounds, item, 0, new Size(80, 20));
                    Assert.IsGreaterThanOrEqualTo(0, layout.TextBounds.Width);
                    Assert.IsGreaterThanOrEqualTo(0, editor.Width);
                    Assert.IsTrue(editor.Left >= layout.CellBounds.Left && editor.Right <= layout.CellBounds.Right);
                }
            }

        }

        [TestMethod]
        [DataRow(true, false)]
        [DataRow(false, false)]
        [DataRow(false, true)]
        public void WrappedAndEllipsizedTextUseTheSameMeasurementForPaintAndHitTest(bool gdi, bool wrap) {
            using (LayoutList list = new LayoutList())
            using (RecordingRenderer renderer = new RecordingRenderer { UseGdiTextRendering = gdi, CanWrap = wrap }) {
                ConfigureList(list, false);
                list.DefaultRenderer = renderer;
                list.RowHeight = 100;
                list.GetColumn(0).Width = 100;
                list.CellVerticalAlignment = StringAlignment.Far;
                Show(list);
                list.SetObjects(WrappedRows);
                list.Refresh();
                OLVListItem item = list.GetItem(0);
                using (Graphics g = list.CreateGraphics()) {
                    CellLayout layout = renderer.GetCellLayout(g, item.GetSubItemBounds(0), item, 0);
                    AssertLayoutEqual(layout, renderer.Layouts[0]);
                    AssertHit(list, layout.TextBounds, HitTestLocation.Text);
                    Assert.IsTrue(layout.TextBounds.Width <= layout.ContentBounds.Width);
                    if (wrap)
                        Assert.IsTrue(layout.TextBounds.Height > list.Font.Height * 2);
                    else
                        Assert.IsTrue(layout.TextBounds.Height <= list.Font.Height * 2);
                }
            }
        }

        [TestMethod]
        public void ScrollingToAnotherRowDoesNotReusePreviousPaintCoordinates() {
            using (LayoutList list = new LayoutList())
            using (RecordingRenderer renderer = new RecordingRenderer { RowIndex = 12 }) {
                ConfigureList(list, false);
                list.DefaultRenderer = renderer;
                list.RowHeight = 44;
                list.GetColumn(1).ImageGetter = x => "image";
                List<string> rows = new List<string>();
                for (int i = 0; i < 30; i++)
                    rows.Add("Row " + i);
                Show(list);
                list.SetObjects(rows);
                list.EnsureVisible(12);
                list.Refresh();
                OLVListItem item = list.GetItem(12);
                using (Graphics g = list.CreateGraphics()) {
                    CellLayout layout = renderer.GetCellLayout(g, item.GetSubItemBounds(1), item, 1);
                    AssertLayoutEqual(layout, renderer.Layouts[1]);
                    AssertHit(list, layout.ImageBounds, HitTestLocation.Image);
                    AssertHit(list, layout.TextBounds, HitTestLocation.Text);
                    Point point = Center(layout.TextBounds);
                    Assert.AreEqual(12, list.OlvHitTest(point.X, point.Y).RowIndex);
                }
            }
        }

        [TestMethod]
        [DataRow(StringAlignment.Near)]
        [DataRow(StringAlignment.Center)]
        [DataRow(StringAlignment.Far)]
        public void CustomCheckboxImagesAreDrawnAtTheAdvertisedGlyphBounds(StringAlignment alignment) {
            using (LayoutList list = new LayoutList())
            using (ImageList checkImages = new ImageList { ImageSize = new Size(16, 16) })
            using (RecordingRenderer renderer = new RecordingRenderer { UseCustomCheckboxImages = true }) {
                ConfigureList(list, false);
                checkImages.Images.Add(image);
                list.CheckBoxes = true;
                list.StateImageList = checkImages;
                list.RowHeight = 48;
                list.CellVerticalAlignment = alignment;
                list.DefaultRenderer = renderer;
                Show(list);
                list.SetObjects(Rows);
                list.Refresh();
                OLVListItem item = list.GetItem(0);
                using (Graphics g = list.CreateGraphics()) {
                    CellLayout layout = renderer.GetCellLayout(g, item.GetSubItemBounds(0), item, 0);
                    Assert.AreEqual(layout.CheckBoxBounds, renderer.DrawnCheckBoxBounds);
                    AssertHit(list, layout.CheckBoxBounds, HitTestLocation.CheckBox);
                }
            }
        }

        [TestMethod]
        public void NarrowHeaderImagesCannotPaintIntoTheNextColumn() {
            using (LayoutList list = new LayoutList()) {
                ConfigureList(list, false);
                OLVColumn column = list.GetColumn(0);
                column.Width = 10;
                column.HeaderImageKey = "image";
                column.ShowTextInHeader = false;
                column.HeaderAppearance.Normal.Padding = Rectangle.Empty;
                Show(list);
                HeaderControl header = list.HeaderControl;
                Rectangle cell = header.GetItemRect(0);
                using (Bitmap bitmap = new Bitmap(400, 100))
                using (Graphics g = Graphics.FromImage(bitmap)) {
                    g.Clear(Color.White);
                    typeof(HeaderControl).GetMethod("CustomDrawHeaderCell", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(header, new object[] { g, 0, 0 });
                    Assert.IsTrue(ContainsColor(bitmap, Color.Magenta, cell));
                    Assert.IsFalse(ContainsColor(bitmap, Color.Magenta, new Rectangle(cell.Right, cell.Top, 30, cell.Height)));
                }
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void NativeHeaderHeightMeasuresTheConfiguredHeaderFont(bool columnFont) {
            ObjectListViewColorMode previousMode = ObjectListViewTheme.ColorMode;
            try {
                ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Light;
                using (Font font = new Font(Control.DefaultFont.FontFamily, 32))
                using (LayoutList list = new LayoutList()) {
                    ConfigureList(list, false);
                    list.HeaderUsesThemes = true;
                    HeaderFormatStyle style = new HeaderFormatStyle();
                    style.Normal.Font = font;
                    if (columnFont)
                        list.GetColumn(0).HeaderFormatStyle = style;
                    else
                        list.HeaderFormatStyle = style;
                    Show(list);
                    HeaderControl header = list.HeaderControl;
                    Assert.IsFalse((bool)typeof(HeaderControl).GetMethod("NeedsCustomDraw", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(header, null));
                    using (Graphics g = list.CreateGraphics()) {
                        int height = (int)typeof(HeaderControl).GetMethod("CalculateHeight", BindingFlags.Instance | BindingFlags.NonPublic)
                            .Invoke(header, new object[] { g });
                        Assert.IsTrue(height >= font.Height);
                    }
                    Assert.IsTrue(header.ClientRectangle.Height >= font.Height);
                }
            } finally {
                ObjectListViewTheme.ColorMode = previousMode;
            }
        }

        [TestMethod]
        [DataRow(false, HorizontalAlignment.Center)]
        [DataRow(false, HorizontalAlignment.Right)]
        [DataRow(true, HorizontalAlignment.Center)]
        [DataRow(true, HorizontalAlignment.Right)]
        public void WrappedHeaderLinesHonorLegacyAndSharedHorizontalAlignment(bool shared, HorizontalAlignment alignment) {
            using (Font font = new Font(Control.DefaultFont.FontFamily, 18))
            using (LayoutList list = new LayoutList()) {
                ConfigureList(list, false);
                list.HeaderUsesThemes = false;
                list.HeaderWordWrap = true;
                list.HeaderMinimumHeight = 100;
                list.HeaderMaximumHeight = 100;
                list.HeaderFormatStyle = new HeaderFormatStyle();
                list.HeaderFormatStyle.Normal.Font = font;
                list.HeaderFormatStyle.Normal.ForeColor = Color.Black;
                list.HeaderFormatStyle.Normal.BackColor = Color.White;
                OLVColumn column = list.GetColumn(0);
                column.Text = "WWWWWWWW\nI";
                if (shared)
                    column.HeaderAppearance.Normal.HorizontalAlignment = alignment;
                else
                    column.HeaderTextAlign = alignment;
                Show(list);
                HeaderControl header = list.HeaderControl;
                CellLayout layout = header.GetCellLayout(column);
                using (Bitmap bitmap = new Bitmap(list.ClientSize.Width, 120))
                using (Graphics g = Graphics.FromImage(bitmap)) {
                    g.Clear(Color.White);
                    typeof(HeaderControl).GetMethod("CustomDrawHeaderCell", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(header, new object[] { g, column.Index, 0 });
                    Rectangle first = FindInkBounds(bitmap, new Rectangle(layout.TextBounds.Left, layout.TextBounds.Top,
                        layout.TextBounds.Width, font.Height));
                    Rectangle second = FindInkBounds(bitmap, new Rectangle(layout.TextBounds.Left, layout.TextBounds.Top + font.Height,
                        layout.TextBounds.Width, font.Height));
                    Assert.IsFalse(first.IsEmpty);
                    Assert.IsFalse(second.IsEmpty);
                    int firstAnchor = alignment == HorizontalAlignment.Right ? first.Right : first.Left + first.Width / 2;
                    int secondAnchor = alignment == HorizontalAlignment.Right ? second.Right : second.Left + second.Width / 2;
                    Assert.IsTrue(Math.Abs(firstAnchor - secondAnchor) <= 4, $"Line anchors differ: {first}, {second}");
                }
            }
        }

        private static Rectangle FindInkBounds(Bitmap bitmap, Rectangle bounds) {
            bounds.Intersect(new Rectangle(Point.Empty, bitmap.Size));
            Rectangle ink = Rectangle.Empty;
            for (int y = bounds.Top; y < bounds.Bottom; y++)
                for (int x = bounds.Left; x < bounds.Right; x++) {
                    Color color = bitmap.GetPixel(x, y);
                    if (color.R < 100 && color.G < 100 && color.B < 100) {
                        Rectangle pixel = new Rectangle(x, y, 1, 1);
                        ink = ink.IsEmpty ? pixel : Rectangle.Union(ink, pixel);
                    }
                }
            return ink;
        }

        [TestMethod]
        [DataRow(false, false, false)]
        [DataRow(false, true, false)]
        [DataRow(true, false, false)]
        [DataRow(true, true, false)]
        [DataRow(false, false, true)]
        [DataRow(false, true, true)]
        public void DividerAutosizingFitsRendererTextImagesCheckboxesAndPadding(bool virtualMode, bool rtl, bool gdi) {
            using (ObjectListViewBase list = virtualMode ? (ObjectListViewBase)new FastObjectListView() : new LayoutList())
            using (HighlightTextRenderer renderer = new HighlightTextRenderer { CanWrap = !gdi, UseGdiTextRendering = gdi }) {
                ConfigureList(list, rtl);
                list.DefaultRenderer = renderer;
                list.CheckBoxes = true;
                list.CellPadding = new Rectangle(11, 3, 9, 3);
                list.GetColumn(0).ImageGetter = x => gdi ? null : "image";
                Show(list);
                const string name = "Jonathan Phillip Piper";
                list.ModelObjects = new object[] { name };
                OLVColumn column = list.GetColumn(0);
                column.Width = 50;
                column.DisplayIndex = 1;
                ResizeViaDivider(list, column.Index);
                list.RowHeight = 90;
                list.Refresh();
                OLVListItem item = list.GetItem(0);
                using (Graphics g = list.CreateGraphics()) {
                    CellLayout layout = renderer.GetCellLayout(g, item.GetSubItemBounds(0), item, 0);
                    float textWidth = gdi ? TextRenderer.MeasureText(g, name, list.Font, Size.Empty,
                        TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width : g.MeasureString(name, list.Font).Width;
                    Assert.IsTrue(layout.TextBounds.Width >= textWidth);
                    int singleLineHeight = Math.Max(images.ImageSize.Height, (int)Math.Ceiling(g.MeasureString(name, list.Font).Height) + 1);
                    Assert.IsTrue(layout.TextBounds.Height <= singleLineHeight,
                        $"Autosized text still wraps in {layout.TextBounds}.");
                    Assert.IsFalse(layout.TextBounds.IntersectsWith(layout.ImageBounds));
                    Assert.IsFalse(layout.TextBounds.IntersectsWith(layout.CheckBoxBounds));
                }
            }
        }

        [TestMethod]
        public void DividerAutosizingVirtualRowsStaysBoundedButExplicitSizingIncludesOffscreenRows() {
            using (FastObjectListView list = new FastObjectListView()) {
                ConfigureList(list, false);
                List<string> rows = new List<string>();
                for (int i = 0; i < 199; i++)
                    rows.Add("short");
                rows.Add(new string('W', 100));
                Show(list);
                list.SetObjects(rows);
                ResizeViaDivider(list, 0);
                int visibleWidth = list.GetColumn(0).Width;
                list.AutoResizeColumnToContents(0);
                Assert.IsTrue(list.GetColumn(0).Width > visibleWidth);
            }
        }

        [TestMethod]
        [DataRow(OLVColumn.ButtonSizingMode.CellBounds)]
        [DataRow(OLVColumn.ButtonSizingMode.FixedBounds)]
        public void DividerAutosizingStretchButtonsMeasuresTheirIntrinsicContent(OLVColumn.ButtonSizingMode sizing) {
            using (LayoutList list = new LayoutList())
            using (ColumnButtonRenderer renderer = new ColumnButtonRenderer { SizingMode = sizing }) {
                ConfigureList(list, false);
                list.GetColumn(0).Renderer = renderer;
                Show(list);
                list.SetObjects(Rows);
                ResizeViaDivider(list, 0);
                Assert.IsTrue(list.GetColumn(0).Width >= TextRenderer.MeasureText(Rows[0], list.Font).Width);
                Assert.IsTrue(list.GetColumn(0).Width < 200);
            }
        }

        [TestMethod]
        public void DividerAutosizingGroupedVirtualRowsMeasuresTheScrolledViewport() {
            using (FastObjectListView list = new FastObjectListView()) {
                ConfigureList(list, false);
                OLVColumn column = list.GetColumn(0);
                column.GroupKeyGetter = x => ((string)x).StartsWith("first", StringComparison.Ordinal) ? "A" : "B";
                list.ShowGroups = true;
                List<string> rows = new List<string>();
                for (int i = 0; i < 50; i++)
                    rows.Add("first " + i);
                string wide = "last " + new string('W', 60);
                rows.Add(wide);
                Show(list);
                list.SetObjects(rows);
                list.Sort(column, SortOrder.Ascending);
                OLVListItem item = list.ModelToItem(wide);
                list.EnsureVisible(item.Index);
                list.Refresh();
                Assert.AreEqual(0, list.TopItemIndex);
                Assert.IsTrue(item.Bounds.IntersectsWith(list.ClientRectangle));
                ResizeViaDivider(list, 0);
                Assert.IsTrue(column.Width >= TextRenderer.MeasureText(wide, list.Font).Width);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DividerAutosizingDoesNotMaterializeCollapsedVirtualGroupMembers(bool fullRowSelect) {
            using (FastObjectListView list = new FastObjectListView()) {
                ConfigureList(list, false);
                list.FullRowSelect = fullRowSelect;
                OLVColumn column = list.GetColumn(0);
                column.GroupKeyGetter = x => ((string)x).StartsWith("visible", StringComparison.Ordinal) ? "A" : "B";
                list.ShowGroups = true;
                List<string> rows = new List<string>();
                for (int i = 0; i < 5; i++)
                    rows.Add("visible " + i);
                for (int i = 0; i < 1000; i++)
                    rows.Add("hidden " + i);
                Show(list);
                list.SetObjects(rows);
                list.Sort(column, SortOrder.Ascending);
                list.OLVGroups[1].Collapsed = true;
                list.Refresh();
                Application.DoEvents();
                int collapsedReads = 0;
                list.RowFormatter = item => {
                    if (((string)item.RowObject).StartsWith("hidden", StringComparison.Ordinal))
                        collapsedReads++;
                };
                ResizeViaDivider(list, 0);
                Assert.AreEqual(0, collapsedReads);
                Assert.IsTrue(column.Width >= TextRenderer.MeasureText("visible 0", list.Font).Width);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DividerAutosizingSingleColumnTreesFindsIndentedRowsWithoutFullRowSelection(bool rtl) {
            using (TreeListView tree = new TreeListView()) {
                ConfigureList(tree, rtl);
                tree.Columns.RemoveAt(1);
                tree.AllColumns.RemoveAt(1);
                tree.FullRowSelect = false;
                tree.GetColumn(0).Width = 80;
                string root = "root " + new string('W', 30);
                string child = "child " + new string('W', 60);
                tree.CanExpandGetter = model => (string)model == root;
                tree.ChildrenGetter = model => new[] { child };
                Show(tree);
                tree.Roots = new[] { root };
                tree.Expand(root);
                ResizeViaDivider(tree, 0);
                OLVListItem item = tree.ModelToItem(child);
                using (Graphics g = tree.CreateGraphics()) {
                    CellLayout layout = tree.TreeColumnRenderer.GetCellLayout(g, item.GetSubItemBounds(0), item, 0);
                    int textWidth = TextRenderer.MeasureText(g, child, tree.Font, Size.Empty,
                        TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;
                    Assert.IsTrue(layout.TextBounds.Width >= textWidth,
                        $"Indented child still clips after divider sizing: {layout.TextBounds}.");
                }
            }
        }

        [TestMethod]
        [DataRow(false, HorizontalAlignment.Left)]
        [DataRow(false, HorizontalAlignment.Center)]
        [DataRow(false, HorizontalAlignment.Right)]
        [DataRow(true, HorizontalAlignment.Left)]
        [DataRow(true, HorizontalAlignment.Center)]
        [DataRow(true, HorizontalAlignment.Right)]
        public void WrappedGdiPlusLinesPreservePhysicalAlignmentInRtl(bool rtl, HorizontalAlignment alignment) {
            using (Font font = new Font(Control.DefaultFont.FontFamily, 18))
            using (LayoutList list = new LayoutList())
            using (BaseRenderer renderer = new BaseRenderer { CanWrap = true, UseGdiTextRendering = false }) {
                ConfigureList(list, rtl);
                list.Font = font;
                list.RowHeight = 100;
                list.RowAppearance.Normal.ForeColor = Color.Black;
                list.RowAppearance.Normal.BackColor = Color.White;
                list.GetColumn(0).CellAppearance.Normal.HorizontalAlignment = alignment;
                list.DefaultRenderer = renderer;
                Show(list);
                list.SetObjects(AlignmentRows);
                OLVListItem item = list.GetItem(0);
                using (Bitmap bitmap = new Bitmap(list.ClientSize.Width, list.ClientSize.Height))
                using (Graphics g = Graphics.FromImage(bitmap)) {
                    g.Clear(Color.White);
                    Rectangle cell = item.GetSubItemBounds(0);
                    CellLayout layout = renderer.GetCellLayout(g, cell, item, 0);
                    renderer.Render(g, cell);
                    int lineHeight = (int)Math.Ceiling(font.GetHeight(g));
                    Rectangle first = FindInkBounds(bitmap, new Rectangle(layout.TextBounds.Left, layout.TextBounds.Top,
                        layout.TextBounds.Width, lineHeight));
                    Rectangle second = FindInkBounds(bitmap, new Rectangle(layout.TextBounds.Left, layout.TextBounds.Top + lineHeight,
                        layout.TextBounds.Width, lineHeight));
                    Assert.IsFalse(first.IsEmpty);
                    Assert.IsFalse(second.IsEmpty);
                    int firstAnchor = alignment == HorizontalAlignment.Right ? first.Right :
                        alignment == HorizontalAlignment.Left ? first.Left : first.Left + first.Width / 2;
                    int secondAnchor = alignment == HorizontalAlignment.Right ? second.Right :
                        alignment == HorizontalAlignment.Left ? second.Left : second.Left + second.Width / 2;
                    Assert.IsTrue(Math.Abs(firstAnchor - secondAnchor) <= 4, $"Line anchors differ: {first}, {second}");
                }
            }
        }

        private static void ResizeViaDivider(ObjectListViewBase list, int column) {
            HeaderNotification notification = new HeaderNotification {
                Header = new NotificationHeader { Window = list.HeaderControl.Handle, Code = -325 },
                Column = column
            };
            IntPtr buffer = Marshal.AllocHGlobal(Marshal.SizeOf<HeaderNotification>());
            try {
                Marshal.StructureToPtr(notification, buffer, false);
                Assert.AreEqual((IntPtr)1, SendMessage(list.Handle, 0x4e, IntPtr.Zero, buffer));
            } finally {
                Marshal.FreeHGlobal(buffer);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NotificationHeader {
            public IntPtr Window;
            public IntPtr Id;
            public int Code;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HeaderNotification {
            public NotificationHeader Header;
            public int Column;
            public int Button;
            public IntPtr Item;
        }

        private void ConfigureList(ObjectListViewBase list, bool rtl) {
            list.View = View.Details;
            list.ShowGroups = false;
            list.OwnerDraw = true;
            list.FullRowSelect = true;
            list.SmallImageList = images;
            list.RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No;
            list.RightToLeftLayout = rtl;
            list.Dock = DockStyle.Fill;
            for (int i = 0; i < 2; i++) {
                OLVColumn column = new OLVColumn("Header", "") { AspectGetter = x => x, Width = 260 };
                list.Columns.Add(column);
                list.AllColumns.Add(column);
            }
        }

        private void Show(ObjectListViewBase list) {
            form.Controls.Add(list);
            form.Show();
            list.Refresh();
            Application.DoEvents();
        }

        private static Point Center(Rectangle bounds) => new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);

        private static void AssertHit(ObjectListViewBase list, Rectangle bounds, HitTestLocation expected) {
            Point point = Center(bounds);
            OlvListViewHitTestInfo hit = list.OlvHitTest(point.X, point.Y);
            Assert.AreEqual(expected, hit.HitTestLocation, $"Hit at {point} in {bounds}");
            Assert.AreEqual(Rectangle.Intersect(hit.Item.GetSubItemBounds(hit.ColumnIndex), bounds), hit.HitBounds);
        }

        private static void AssertLayoutEqual(CellLayout expected, CellLayout actual) {
            Assert.AreEqual(expected.CellBounds, actual.CellBounds);
            Assert.AreEqual(expected.ContentBounds, actual.ContentBounds);
            Assert.AreEqual(expected.CheckBoxBounds, actual.CheckBoxBounds);
            Assert.AreEqual(expected.ImageBounds, actual.ImageBounds);
            Assert.AreEqual(expected.TextBounds, actual.TextBounds);
        }

        private static bool ContainsColor(Bitmap bitmap, Color color, Rectangle bounds) {
            bounds.Intersect(new Rectangle(Point.Empty, bitmap.Size));
            for (int y = bounds.Top; y < bounds.Bottom; y++)
                for (int x = bounds.Left; x < bounds.Right; x++)
                    if (bitmap.GetPixel(x, y).ToArgb() == color.ToArgb())
                        return true;
            return false;
        }

        private sealed class RecordingRenderer : BaseRenderer {
            [DefaultValue(0)]
            public int RowIndex { get; set; }
            public Dictionary<int, CellLayout> Layouts { get; } = new Dictionary<int, CellLayout>();
            public Rectangle DrawnCheckBoxBounds { get; private set; }
            protected override void DrawCellLayout(Graphics g, CellLayout layout) {
                if (this.ListItem.Index == this.RowIndex)
                    Layouts[this.Column.Index] = layout;
                base.DrawCellLayout(g, layout);
            }
            protected override int DrawImage(Graphics g, Rectangle bounds, object selector) {
                if (selector is Image)
                    DrawnCheckBoxBounds = bounds;
                return base.DrawImage(g, bounds, selector);
            }

            protected override Rectangle HandleGetEditRectangle(Graphics g, Rectangle bounds, OLVListItem item, int column, Size preferredSize) =>
                this.StandardGetEditRectangle(g, bounds, preferredSize);
        }

        private sealed class RecordingTreeRenderer : TreeListView.TreeRenderer {
            public Dictionary<object, CellLayout> Layouts { get; } = new Dictionary<object, CellLayout>();
            public Dictionary<object, Rectangle> Glyphs { get; } = new Dictionary<object, Rectangle>();
            protected override void DrawCellLayout(Graphics g, CellLayout layout) {
                Layouts[this.RowObject] = layout;
                base.DrawCellLayout(g, layout);
            }
            protected override void DrawExpansionGlyph(Graphics g, Rectangle bounds, bool expanded) {
                Glyphs[this.RowObject] = bounds;
                base.DrawExpansionGlyph(g, bounds, expanded);
            }
        }

        private sealed class LayoutList : ObjectListView {
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Point? ForcedHitPoint { get; set; }
            public override OlvListViewHitTestInfo OlvHitTest(int x, int y) =>
                ForcedHitPoint.HasValue ? base.OlvHitTest(ForcedHitPoint.Value.X, ForcedHitPoint.Value.Y) : base.OlvHitTest(x, y);
            public void ClickTarget(Point point) => this.ProcessLButtonDown(this.OlvHitTest(point.X, point.Y));
            public void ClickHeader(int column) => this.HandleColumnClick(new ColumnClickEventArgs(column), Keys.None);
            public void PressSpace() {
                Message message = Message.Create(this.Handle, 0x100, (IntPtr)Keys.Space, IntPtr.Zero);
                this.HandleKeyDown(ref message);
            }
            public ToolTipShowingEventArgs GetToolTipArgs() {
                ToolTipShowingEventArgs args = new ToolTipShowingEventArgs();
                typeof(ToolTipShowingEventArgs).GetProperty(nameof(ToolTipShowingEventArgs.ToolTipControl))
                    .SetValue(args, this.CellToolTip);
                this.HandleCellToolTipShowing(this, args);
                return args;
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ClientToScreen(IntPtr window, ref Point point);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
    }
}
