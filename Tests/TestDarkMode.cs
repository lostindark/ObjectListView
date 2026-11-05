using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    [STATestClass]
    public class TestDarkMode : ThemeTestFixture
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_RefreshTheme_UpdatesEmptyListDefaultsWithoutReplacingExplicitColors(bool customize) {
            ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Light;
            using (var list = new ObjectListView { EmptyListMsg = "Loading" }) {
                var overlay = (TextOverlay)list.EmptyListMsgOverlay;
                Color originalText = overlay.TextColor;
                if (customize) {
                    overlay.TextColor = originalText;
                    overlay.BackColor = Color.Navy;
                    overlay.BorderColor = Color.Orange;
                }
                foreach (ObjectListViewColorMode mode in new[] { ObjectListViewColorMode.Dark, ObjectListViewColorMode.Light }) {
                    ObjectListViewTheme.ColorMode = mode;
                    ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.EmptyListBackgroundColor), Color.Maroon);
                    list.RefreshTheme();
                    Assert.AreSame(overlay, list.EmptyListMsgOverlay);
                    Assert.AreEqual("Loading", overlay.Text);
                    Assert.AreEqual(customize ? originalText.ToArgb() : ObjectListViewTheme.Current.EmptyListTextColor.ToArgb(),
                        overlay.TextColor.ToArgb());
                    Assert.AreEqual(customize ? Color.Navy.ToArgb() : ObjectListViewTheme.Current.EmptyListBackgroundColor.ToArgb(),
                        overlay.BackColor.ToArgb());
                    Assert.AreEqual(customize ? Color.Orange.ToArgb() : ObjectListViewTheme.Current.ControlDarkColor.ToArgb(),
                        overlay.BorderColor.ToArgb());
                }
            }
        }

        [TestMethod]
        public void Test_RefreshTheme_DoesNotRecolorAnApplicationOwnedEmptyListOverlay() {
            using (var list = new ObjectListView())
            using (var overlay = new TextOverlay { TextColor = Color.Ivory, BackColor = Color.Navy, BorderColor = Color.Orange }) {
                list.EmptyListMsgOverlay = overlay;
                ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Light;
                list.RefreshTheme();
                Assert.AreSame(overlay, list.EmptyListMsgOverlay);
                Assert.AreEqual(Color.Ivory, overlay.TextColor);
                Assert.AreEqual(Color.Navy, overlay.BackColor);
                Assert.AreEqual(Color.Orange, overlay.BorderColor);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_ScrollbarChrome_FollowsAncestorMovementScrollingAndClipping(bool textControl) {
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ScrollBarTrackColor), Color.Navy);
            using (var form = new Form { ClientSize = new Size(520, 360), Opacity = 0.5, ShowInTaskbar = false })
            using (var viewport = new Panel { Bounds = new Rectangle(10, 10, 480, 300), AutoScroll = true, AutoScrollMinSize = new Size(800, 600) })
            using (var container = new Panel { Bounds = new Rectangle(20, 25, 400, 250) })
            using (Control control = textControl
                ? (Control)new RichTextBox { Text = string.Join(Environment.NewLine, Enumerable.Range(0, 60)), ScrollBars = RichTextBoxScrollBars.Vertical }
                : new ObjectListView { View = View.Details, ShowGroups = false }) {
                control.Bounds = new Rectangle(15, 15, 240, 150);
                if (control is ObjectListView list) {
                    list.Columns.Add(new OLVColumn("Name", "Name") { Width = 500 });
                    list.SetObjects(Enumerable.Range(0, 40).Select(i => new { Name = "Row " + i }).ToArray());
                }
                container.Controls.Add(control);
                viewport.Controls.Add(container);
                form.Controls.Add(viewport);
                FormTheme.Attach(form);
                form.Show();
                Application.DoEvents();
                control.Refresh();
                Form chrome = form.OwnedForms.Single(window => window.Name == "ObjectListViewChrome");
                Assert.AreEqual(new Rectangle(container.PointToScreen(control.Location), control.Size), chrome.Bounds);

                container.Location = new Point(container.Left + 35, container.Top + 40);
                Application.DoEvents();
                Assert.AreEqual(new Rectangle(container.PointToScreen(control.Location), control.Size), chrome.Bounds);
                viewport.AutoScrollPosition = new Point(70, 80);
                Application.DoEvents();
                Assert.AreEqual(new Rectangle(container.PointToScreen(control.Location), control.Size), chrome.Bounds);
                viewport.Size = new Size(200, 140);
                Application.DoEvents();
                Rectangle visible = chrome.Bounds;
                for (Control ancestor = control.Parent; ancestor != null; ancestor = ancestor.Parent)
                {
                    visible.Intersect(ancestor.RectangleToScreen(ancestor.ClientRectangle));
                }
                visible.Offset(-chrome.Left, -chrome.Top);
                using (var bitmap = new Bitmap(1, 1))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    Assert.AreEqual(visible, Rectangle.Round(chrome.Region.GetBounds(graphics)));
                }
            }
        }

        [TestMethod]
        public void Test_RefreshTheme_UpdatesCellDefaultsWithoutReloadingValuesOrLosingExplicitBackgrounds() {
            ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Light;
            using (var list = new ObjectListView { View = View.Details, ShowGroups = false, UseCellFormatEvents = true }) {
                int reads = 0;
                for (int i = 0; i < 3; i++) {
                    list.Columns.Add(new OLVColumn("Name", null) {
                        AspectGetter = model => { reads++; return "Cell"; }
                    });
                }
                list.FormatCell += (sender, e) => e.SubItem.ForeColor = ObjectListViewTheme.Current.TextColor;
                list.SetObjects(new[] { new object() });
                list.GetItem(0).GetSubItem(2).BackColor = Color.Magenta;
                int previousReads = reads;

                foreach (ObjectListViewColorMode mode in new[] { ObjectListViewColorMode.Dark, ObjectListViewColorMode.Light }) {
                    ObjectListViewTheme.ColorMode = mode;
                    list.RefreshTheme();
                    Assert.AreEqual(previousReads, reads);
                    Assert.AreEqual(ObjectListViewTheme.Current.WindowColor.ToArgb(), list.GetItem(0).GetSubItem(1).BackColor.ToArgb());
                    Assert.AreEqual(ObjectListViewTheme.Current.TextColor.ToArgb(), list.GetItem(0).GetSubItem(1).ForeColor.ToArgb());
                    Assert.AreEqual(Color.Magenta, list.GetItem(0).GetSubItem(2).BackColor);
                }
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_RefreshTheme_HyperlinkDefaultsFollowThePaletteButExplicitStylesDoNot(bool customize) {
            ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Light;
            using (var list = new ObjectListView { View = View.Details, ShowGroups = false, UseHyperlinks = true }) {
                const string normal = "https://example.test/normal";
                const string visited = "https://example.test/visited";
                list.Columns.Add(new OLVColumn("URL", null) { AspectGetter = model => model, Hyperlink = true });
                if (customize) {
                    list.HyperlinkStyle.Normal.ForeColor = Color.Red;
                    list.HyperlinkStyle.Visited = new CellStyle { ForeColor = Color.Magenta };
                }
                list.MarkUrlVisited(visited);
                list.SetObjects(new[] { normal, visited });
                ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Dark;
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.LinkColor), Color.Yellow);
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.VisitedLinkColor), Color.Cyan);
                list.RefreshTheme();
                Assert.AreEqual(customize ? Color.Red : Color.Yellow, list.GetItem(0).ForeColor);
                Assert.AreEqual(customize ? Color.Magenta : Color.Cyan, list.GetItem(1).ForeColor);
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.LinkColor), Color.LightGreen);
                list.RefreshTheme();
                Assert.AreEqual(customize ? Color.Red : Color.LightGreen, list.GetItem(0).ForeColor);
            }
        }

        [TestMethod]
        public void Test_RefreshTheme_WithNoRowsAndHotItemStyling_DoesNotRequireAnItem() {
            using (var list = new ObjectListView { UseHotItem = true, UseHyperlinks = true }) {
                list.RefreshTheme();
                Assert.AreEqual(0, list.GetItemCount());
            }
        }

        [TestMethod]
        public void Test_RefreshTheme_DoesNotReloadTreeChildrenOrReplaceTheirSelection() {
            using (var form = new Form())
            using (var tree = new TreeListView { View = View.Details, ShowGroups = false }) {
                object root = new { Name = "Root" };
                int childLoads = 0;
                int valueReads = 0;
                tree.Columns.Add(new OLVColumn("Name", null) {
                    AspectGetter = model => { valueReads++; return model.ToString(); }
                });
                tree.CanExpandGetter = model => ReferenceEquals(model, root);
                tree.ChildrenGetter = model => {
                    childLoads++;
                    return new[] { new { Name = "Child" } };
                };
                form.Controls.Add(tree);
                _ = tree.Handle;
                tree.Roots = new[] { root };
                tree.Expand(root);
                object child = tree.GetModelObject(1);
                tree.SelectObject(child);
                int previousLoads = childLoads;
                int previousReads = valueReads;

                foreach (ObjectListViewColorMode mode in new[] { ObjectListViewColorMode.Light, ObjectListViewColorMode.Dark }) {
                    ObjectListViewTheme.ColorMode = mode;
                    tree.RefreshTheme();
                    Assert.AreEqual(previousLoads, childLoads);
                    Assert.AreEqual(previousReads, valueReads, "Refreshing the theme must not read row values eagerly.");
                    Assert.AreEqual(2, tree.GetItemCount());
                    Assert.IsTrue(tree.IsExpanded(root));
                    Assert.AreSame(child, tree.GetModelObject(1));
                    Assert.AreSame(child, tree.SelectedObject);
                    Assert.AreEqual(ObjectListViewTheme.Current.TextColor.ToArgb(), tree.GetItem(1).ForeColor.ToArgb());
                    previousReads = valueReads;
                }
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_RefreshTheme_UpdatesAnIndependentListWithoutTakingOverTheHost(bool explicitColors) {
            Application.EnableVisualStyles();
            ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Light;
            using (Form form = new Form { BackColor = Color.Purple })
            using (ObjectListView list = new ObjectListView {
                View = View.Details, ShowGroups = false, Size = new Size(240, 150), OwnerDraw = false
            })
            using (Bitmap bitmap = new Bitmap(240, 150)) {
                OLVColumn column = new OLVColumn("Name", "Name") { Width = 180 };
                list.Columns.Add(column);
                object row = new { Name = "Row" };
                list.SetObjects(new[] { row });
                form.Controls.Add(list);
                _ = list.Handle;
                list.OwnerDraw = false;
                list.SelectObject(row);
                if (explicitColors)
                {
                    list.BackColor = SystemColors.Info;
                    list.ForeColor = SystemColors.InfoText;
                }
#if NET10_0_OR_GREATER
                SystemColorMode hostMode = Application.ColorMode;
#endif
                foreach (ObjectListViewColorMode mode in new[] { ObjectListViewColorMode.Dark, ObjectListViewColorMode.Light })
                {
                    ObjectListViewTheme.ColorMode = mode;
                    list.RefreshTheme();

                    Color background = explicitColors ? SystemColors.Info : ObjectListViewTheme.Current.WindowColor;
                    Color foreground = explicitColors ? SystemColors.InfoText : ObjectListViewTheme.Current.TextColor;
                    Assert.AreEqual(ColorTranslator.ToWin32(background),
                        NativeMessage(list.Handle, 0x1000, IntPtr.Zero, IntPtr.Zero).ToInt32());
                    Assert.AreEqual(ColorTranslator.ToWin32(foreground),
                        NativeMessage(list.Handle, 0x1023, IntPtr.Zero, IntPtr.Zero).ToInt32());
                    Assert.AreEqual(background.ToArgb(), list.GetItem(0).BackColor.ToArgb());
                    Assert.AreEqual(foreground.ToArgb(), list.GetItem(0).ForeColor.ToArgb());
                    Assert.AreSame(row, list.SelectedObject);
                    Assert.IsFalse(list.OwnerDraw);
                    Assert.IsFalse(list.UseExplorerTheme);
                    Assert.AreEqual(Color.Purple, form.BackColor);
#if NET10_0_OR_GREATER
                    Assert.AreEqual(hostMode, Application.ColorMode);
#endif
                    list.DrawToBitmap(bitmap, new Rectangle(Point.Empty, list.Size));
                    if (mode == ObjectListViewColorMode.Dark)
                    {
                        Assert.AreEqual(ObjectListViewTheme.Current.ControlColor.ToArgb(), bitmap.GetPixel(100, 5).ToArgb());
                    }
                    else
                    {
                        Assert.IsTrue(bitmap.GetPixel(100, 5).GetBrightness() > 0.5f);
                    }
                }
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_Scrollbar_ExpandsThumbAndShowsArrowsOnlyWhenPointerEntersItsTrack(bool vertical) {
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ScrollBarThumbColor), Color.Magenta);
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ScrollBarArrowColor), Color.Lime);
            Type theme = typeof(ObjectListView).Assembly.GetType("BrightIdeasSoftware.NativeListViewTheme");
            MethodInfo paint = theme.GetMethod("PaintBar", BindingFlags.Static | BindingFlags.NonPublic);
            Type infoType = paint.GetParameters()[1].ParameterType;
            object info = Activator.CreateInstance(infoType);
            infoType.GetField("LineButton").SetValue(info, 10);
            infoType.GetField("ThumbStart").SetValue(info, 20);
            infoType.GetField("ThumbEnd").SetValue(info, 80);
            infoType.GetField("States").SetValue(info, new uint[6]);
            Size size = vertical ? new Size(20, 100) : new Size(100, 20);
            using (Bitmap idle = new Bitmap(size.Width, size.Height))
            using (Bitmap hover = new Bitmap(size.Width, size.Height)) {
                using (Graphics graphics = Graphics.FromImage(idle))
                    paint.Invoke(null, new object[] { graphics, info, new Rectangle(Point.Empty, size), vertical, true, new Point(-10, -10) });
                using (Graphics graphics = Graphics.FromImage(hover))
                    paint.Invoke(null, new object[] { graphics, info, new Rectangle(Point.Empty, size), vertical, true, vertical ? new Point(2, 40) : new Point(40, 2) });
                int idleWidth = ThumbWidth(idle, vertical);
                int hoverWidth = ThumbWidth(hover, vertical);
                Assert.IsTrue(idleWidth > 0);
                Assert.IsTrue(hoverWidth > idleWidth, "Hovering anywhere in the native track should expand the thin thumb.");
                Rectangle leading = new Rectangle(0, 0, vertical ? 20 : 10, vertical ? 10 : 20);
                Rectangle trailing = vertical ? new Rectangle(0, 90, 20, 10) : new Rectangle(90, 0, 10, 20);
                foreach (Rectangle arrow in new[] { leading, trailing }) {
                    Assert.IsFalse(ContainsColor(idle, Color.Lime, arrow), "Idle scrollbars should hide both arrows.");
                    Assert.IsTrue(ContainsColor(hover, Color.Lime, arrow), "Hovering anywhere in the track should show both arrows.");
                }
            }
        }

        private static int ThumbWidth(Bitmap bitmap, bool vertical) {
            int width = 0;
            for (int i = 0; i < (vertical ? bitmap.Width : bitmap.Height); i++) {
                Color pixel = vertical ? bitmap.GetPixel(i, 40) : bitmap.GetPixel(40, i);
                if (pixel.R > 200 && pixel.B > 200 && pixel.G < 200)
                    width++;
            }
            return width;
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_CheckboxStateImages_SurviveThemedHandleCreationBeforeReassignment(bool designerInitialization) {
            Application.EnableVisualStyles();
            using (Form form = new Form())
            using (DarkListProbe list = new DarkListProbe { View = View.Details }) {
                var initialization = (ISupportInitialize)list;
                if (designerInitialization)
                {
                    initialization.BeginInit();
                }
                OLVColumn column = new OLVColumn("Name", "Name");
                list.AllColumns.Add(column);
                list.Columns.Add(column);
                list.CheckBoxes = true;
                if (designerInitialization)
                {
                    initialization.EndInit();
                }
                form.Controls.Add(list);
                FormTheme.Attach(form);
                list.PrimarySortColumn = column;
                list.PrimarySortOrder = SortOrder.Ascending;
                _ = list.Handle;

                list.CheckBoxes = true;

                Assert.IsTrue(list.StateImageList.Images.ContainsKey(ObjectListViewBase.UNCHECKED_KEY));
                Assert.IsTrue(list.StateImageList.Images.ContainsKey(ObjectListViewBase.CHECKED_KEY));
                list.RecreateTestHandle();
                list.CheckBoxes = true;
                Assert.AreEqual(2, list.StateImageList.Images.Count);
            }
        }

        [TestMethod]
        public void Test_ThemeChanges_PreservePrimaryCheckboxImagesAndStates() {
            Application.EnableVisualStyles();
#if NET10_0_OR_GREATER
            SystemColorMode original = Application.ColorMode;
#endif
            try {
                using (Form form = new Form { Opacity = 0, ShowInTaskbar = false })
                using (DarkListProbe list = new DarkListProbe { View = View.Details, CheckBoxes = true, TriStateCheckBoxes = true }) {
                    var first = new { Name = "First" };
                    var second = new { Name = "Second" };
                    list.Columns.Add(new OLVColumn("Name", "Name"));
                    list.SetObjects(new[] { first, second });
                    form.Controls.Add(list);
                    FormTheme.Attach(form);
                    form.Show();
                    list.CheckObject(first);
                    list.CheckIndeterminateObject(second);
                    ImageList images = list.StateImageList;
                    foreach (ObjectListViewColorMode mode in new[] { ObjectListViewColorMode.Light, ObjectListViewColorMode.Dark }) {
                        FormTheme.SetColorMode(mode);
                        Assert.AreSame(images, list.StateImageList);
                        Assert.AreEqual(3, images.Images.Count);
                        Assert.IsTrue(list.IsChecked(first));
                        Assert.IsTrue(list.IsCheckedIndeterminate(second));
                    }
                }
            } finally {
#if NET10_0_OR_GREATER
                Application.SetColorMode(original);
#endif
            }
        }

        [TestMethod]
        public void Test_ScrollbarFallback_UsesAnOpaqueClickThroughCompositedSurface() {
            Application.EnableVisualStyles();
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ScrollBarTrackColor), ObjectListViewTheme.Current.ControlColor);
            using (Form form = new Form { Opacity = 0, ShowInTaskbar = false })
            using (DarkListProbe list = new DarkListProbe { View = View.Details, ShowGroups = false, Size = new Size(240, 150) }) {
                list.Columns.Add(new OLVColumn("Name", "Name") { Width = 500 });
                object[] rows = new object[30];
                for (int i = 0; i < rows.Length; i++)
                {
                    rows[i] = new { Name = "Driver " + i };
                }
                list.SetObjects(rows);
                form.Controls.Add(list);
                form.Show();
                Application.DoEvents();
                list.Refresh();
                Form overlay = form.OwnedForms.Single(window => window.Name == "ObjectListViewChrome");
                Assert.AreEqual(1.0, overlay.Opacity);
                int styles = GetWindowLong(overlay.Handle, -20);
                Assert.AreEqual(0x00080020, styles & 0x00080020,
                    "The scrollbar must be composited separately and pass pointer input to the native control.");
                Assert.AreNotEqual(0, styles & 0x08000000);
                Assert.AreEqual((IntPtr)(-1), NativeMessage(overlay.Handle, 0x84, IntPtr.Zero, IntPtr.Zero));
                overlay.Opacity = 0;
                overlay.Bounds = new Rectangle(form.PointToScreen(list.Location), list.Size);
                using (Bitmap surface = new Bitmap(240, 150))
                {
                    overlay.DrawToBitmap(surface, new Rectangle(Point.Empty, surface.Size));
                    Assert.AreEqual(ObjectListViewTheme.Current.ScrollBarTrackColor.ToArgb(), surface.GetPixel(232, 142).ToArgb(),
                        "The opaque scrollbar layer must cover the native white corner.");
                }
                list.RecreateTestHandle();
                list.Refresh();
                Assert.AreSame(overlay, form.OwnedForms.Single(window => window.Name == "ObjectListViewChrome"));

                ObjectListViewTheme.ResetColors();
                ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Light;
                list.UseExplorerTheme = true;
                Application.DoEvents();
                Assert.IsTrue(overlay.Left < -1000, "Switching back to native light styling must hide the chrome overlay.");
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_ScrollbarThumb_StaysNarrowWithoutReducingTheNativeHitArea(bool rtl) {
            Application.EnableVisualStyles();
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ScrollBarThumbColor), Color.Magenta);
            using (Form form = new Form { Opacity = 0, ShowInTaskbar = false })
            using (DarkListProbe list = new DarkListProbe {
                View = View.Details, ShowGroups = false, Size = new Size(240, 150),
                RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No, RightToLeftLayout = rtl
            })
            using (Bitmap bitmap = new Bitmap(240, 150)) {
                list.Columns.Add(new OLVColumn("Name", "Name") { Width = 500 });
                object[] rows = new object[50];
                for (int i = 0; i < rows.Length; i++)
                {
                    rows[i] = new { Name = "Driver " + i };
                }
                list.SetObjects(rows);
                form.Controls.Add(list);
                form.Show();
                Application.DoEvents();
                list.DrawToBitmap(bitmap, new Rectangle(Point.Empty, list.Size));

                int barWidth = SystemInformation.VerticalScrollBarWidth;
                int left = rtl ? 2 : bitmap.Width - barWidth - 2;
                int widest = 0;
                for (int y = 20; y < bitmap.Height - 30; y++) {
                    int pixels = 0;
                    for (int x = left; x < left + barWidth; x++) {
                        Color pixel = bitmap.GetPixel(x, y);
                        if (pixel.R > 200 && pixel.B > 200 && pixel.G < 200)
                        {
                            pixels++;
                        }
                    }
                    widest = Math.Max(widest, pixels);
                }
                Assert.IsTrue(widest > 0, "The thumb must still be visible.");
                Assert.IsTrue(widest <= Math.Max(3, barWidth / 4) + 2, "The visual thumb must be narrower than half the native hit area.");
                NativeMessage(list.Handle, 0x115, (IntPtr)1, IntPtr.Zero);
                Assert.IsTrue(list.TopItemIndex > 0);
            }
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW", ExactSpelling = true)]
        private static extern int GetWindowLong(IntPtr window, int index);

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_ScrollbarComposition_RetainsNativeHoverAndScrolling(bool dark) {
            Application.EnableVisualStyles();
            ObjectListViewTheme.ColorMode = dark ? ObjectListViewColorMode.Dark : ObjectListViewColorMode.Light;
            if (dark)
            {
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ScrollBarTrackColor), ObjectListViewTheme.Current.ControlColor);
            }
            using (Form form = new Form { Opacity = 0, ShowInTaskbar = false })
            using (DarkListProbe list = new DarkListProbe { View = View.Details, ShowGroups = false, Size = new Size(240, 150) }) {
                list.Columns.Add(new OLVColumn("Name", "Name") { Width = 500 });
                object[] rows = new object[30];
                for (int i = 0; i < rows.Length; i++)
                {
                    rows[i] = new { Name = "Driver " + i };
                }
                list.SetObjects(rows);
                form.Controls.Add(list);
                form.Show();
                Application.DoEvents();
                list.NativeNonClientPaints = 0;
                list.NativeNonClientMoves = 0;

                NativeMessage(list.Handle, 0x85, (IntPtr)1, IntPtr.Zero);
                NativeMessage(list.Handle, 0xA0, (IntPtr)7, IntPtr.Zero);
                NativeMessage(list.Handle, 0x200, IntPtr.Zero, (IntPtr)(30 << 16 | 30));
                Application.DoEvents();

                Assert.IsTrue(list.NativeNonClientPaints > 0);
                Assert.IsTrue(list.NativeNonClientMoves > 0,
                    "The click-through surface must leave native scrollbar behavior intact.");
            }
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void Test_EmptyLoadingList_UsesDarkNativeBackgroundBeforeRowsExist(bool disabled, bool rtl) {
            Application.EnableVisualStyles();
            using (Form form = new Form { Opacity = 0, ShowInTaskbar = false })
            using (DarkListProbe list = new DarkListProbe {
                View = View.Details, ShowGroups = false, Size = new Size(240, 150),
                Enabled = !disabled, EmptyListMsg = "Loading",
                RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No, RightToLeftLayout = rtl
            })
            using (Bitmap bitmap = new Bitmap(240, 150)) {
                list.Columns.Add(new OLVColumn("Name", "Name") { Width = 180 });
                form.Controls.Add(list);
                FormTheme.Attach(form);
                form.Show();
                Application.DoEvents();

                list.DrawToBitmap(bitmap, new Rectangle(Point.Empty, list.Size));
                const int LVM_GETBKCOLOR = 0x1000;
                Assert.AreEqual(ColorTranslator.ToWin32(list.BackColor), NativeMessage(list.Handle, LVM_GETBKCOLOR, IntPtr.Zero, IntPtr.Zero).ToInt32());
                Assert.AreEqual(ObjectListViewTheme.Current.WindowColor.ToArgb(), bitmap.GetPixel(10, 100).ToArgb());
                if (disabled)
                {
                    Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.Current.TextColor, new Rectangle(10, 40, 220, 100)),
                        "The loading message must remain visible after repainting the disabled background.");
                }

                list.RecreateTestHandle();
                list.DrawToBitmap(bitmap, new Rectangle(Point.Empty, list.Size));
                Assert.AreEqual(ObjectListViewTheme.Current.WindowColor.ToArgb(), bitmap.GetPixel(10, 100).ToArgb());
            }
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        public void Test_NativeScrollbarsAndCorner_RemainDarkAndScrollNormally(bool rtl, bool customColors) {
            Application.EnableVisualStyles();
            if (customColors)
            {
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ScrollBarTrackColor), Color.MidnightBlue);
            }
            using (Form form = new Form { Opacity = 0, ShowInTaskbar = false })
            using (DarkListProbe list = new DarkListProbe {
                View = View.Details, ShowGroups = false, Size = new Size(240, 150),
                RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No, RightToLeftLayout = rtl
            })
            using (Bitmap bitmap = new Bitmap(240, 150)) {
                list.Columns.Add(new OLVColumn("Name", "Name") { Width = 500 });
                var rows = new object[50];
                for (int i = 0; i < rows.Length; i++)
                {
                    rows[i] = new { Name = "Driver " + i };
                }
                list.SetObjects(rows);
                form.Controls.Add(list);
                FormTheme.Attach(form);
                form.Show();
                Application.DoEvents();
                list.DrawToBitmap(bitmap, new Rectangle(Point.Empty, list.Size));

                int scrollbarX = rtl ? 8 : bitmap.Width - 8;
                Assert.IsTrue(bitmap.GetPixel(scrollbarX, bitmap.Height - 8).GetBrightness() < 0.5f,
                    "The scrollbar intersection must not retain a white native fill.");
                if (customColors)
                {
                    Assert.AreEqual(Color.MidnightBlue.ToArgb(), bitmap.GetPixel(scrollbarX, bitmap.Height - 8).ToArgb());
                }
                Assert.IsTrue(bitmap.GetPixel(scrollbarX, bitmap.Height / 2).GetBrightness() < 0.8f);
                Assert.IsTrue(bitmap.GetPixel(bitmap.Width / 2, bitmap.Height - 8).GetBrightness() < 0.8f);

                NativeMessage(list.Handle, 0x115, (IntPtr)1, IntPtr.Zero);
                Assert.IsTrue(list.TopItemIndex > 0, "Native scrollbar interaction must remain unchanged.");
                list.RecreateTestHandle();
                list.DrawToBitmap(bitmap, new Rectangle(Point.Empty, list.Size));
                Assert.IsTrue(bitmap.GetPixel(scrollbarX, bitmap.Height - 8).GetBrightness() < 0.5f);
            }
        }

        [DllImport("user32.dll", EntryPoint = "SendMessageW", ExactSpelling = true)]
        private static extern IntPtr NativeMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        [TestMethod]
        [DataRow(false, false, false, false)]
        [DataRow(true, false, false, false)]
        [DataRow(false, true, false, false)]
        [DataRow(false, false, true, false)]
        [DataRow(false, false, false, true)]
        public void Test_NativeGroupCaptions_UseReadableColorsAndPreserveNativeGroups(bool explorerTheme, bool rtl, bool customColors, bool customGroupColor) {
            Application.EnableVisualStyles();
            if (customGroupColor)
            {
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.GroupHeaderTextColor), Color.PaleGreen);
            }
            using (Form form = new Form { ClientSize = new Size(400, 240), Opacity = 0, ShowInTaskbar = false })
            using (DarkListProbe list = new DarkListProbe {
                View = View.Details, ShowGroups = true, Size = new Size(350, 200),
                UseExplorerTheme = explorerTheme, RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No,
                RightToLeftLayout = rtl
            })
            using (Bitmap bitmap = new Bitmap(350, 200)) {
                if (customColors) {
                    list.ForeColor = Color.PaleGreen;
                    list.BackColor = Color.Navy;
                }
                OLVColumn column = new OLVColumn("Name", "Name") { Width = 280 };
                list.Columns.Add(column);
                list.PrimarySortColumn = column;
                list.PrimarySortOrder = SortOrder.Ascending;
                list.SetObjects(new[] { new { Name = "Universal Serial Bus controllers" }, new { Name = "Universal Serial Bus devices" } });
                form.Controls.Add(list);
                form.Show();
                Application.DoEvents();
                Assert.AreEqual(2, list.OLVGroups.Count);
                OLVGroup first = list.OLVGroups[0];
                OLVGroup second = list.OLVGroups[1];
                Color foreground = customColors ? Color.PaleGreen : ObjectListViewTheme.Current.GroupHeaderTextColor;

                list.DrawToBitmap(bitmap, list.ClientRectangle);
                Assert.IsTrue(ContainsColor(bitmap, foreground, GetGroupLabelBounds(list, first.GroupId)));
                Assert.IsTrue(ContainsColor(bitmap, foreground, GetGroupLabelBounds(list, second.GroupId)));
                if (!rtl)
                {
                    Rectangle caption = GetGroupLabelBounds(list, first.GroupId);
                    caption.Width = Math.Max(caption.Width, list.ClientSize.Width - caption.Height - caption.Left);
                    Assert.IsFalse(ContainsNativeCaptionResidue(bitmap, caption, list.BackColor),
                        "The native caption must not remain visible behind or after the recolored text.");
                }

                list.RecreateTestHandle();
                Application.DoEvents();
                list.DrawToBitmap(bitmap, list.ClientRectangle);
                Assert.IsTrue(ContainsColor(bitmap, foreground, GetGroupLabelBounds(list, list.OLVGroups[0].GroupId)));
                Assert.IsTrue(ContainsColor(bitmap, foreground, GetGroupLabelBounds(list, list.OLVGroups[1].GroupId)));
                Assert.AreEqual(2, list.GetItemCount());

                first = list.OLVGroups[0];
                first.Collapsible = true;
                first.Collapsed = true;
                list.DrawToBitmap(bitmap, list.ClientRectangle);
                Assert.IsTrue(first.Collapsed);
                Assert.IsTrue(ContainsColor(bitmap, foreground, GetGroupLabelBounds(list, first.GroupId)));
                first.Collapsed = false;
                Assert.IsFalse(first.Collapsed);
            }
        }

        private static Rectangle GetGroupLabelBounds(ObjectListViewBase list, int groupId) {
            const int LVM_GETGROUPRECT = 0x1000 + 98;
            NativeRectangle rectangle = new NativeRectangle { Top = 2 };
            Assert.AreNotEqual(IntPtr.Zero, SendMessage(list.Handle, LVM_GETGROUPRECT, groupId, ref rectangle));
            Rectangle bounds = Rectangle.FromLTRB(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
            // Group coordinates follow the mirrored native DC; bitmap pixels are physical.
            if (list.RightToLeftLayout && list.RightToLeft == RightToLeft.Yes)
            {
                bounds.X = list.ClientSize.Width - bounds.Right;
            }
            return bounds;
        }

        private static bool ContainsNativeCaptionResidue(Bitmap bitmap, Rectangle bounds, Color background) {
            bounds.Intersect(new Rectangle(Point.Empty, bitmap.Size));
            for (int y = bounds.Top; y < bounds.Bottom; y++) {
                for (int x = bounds.Left; x < bounds.Right; x++) {
                    Color pixel = bitmap.GetPixel(x, y);
                    if (pixel.ToArgb() == background.ToArgb())
                        continue;
                    if (pixel.R < 24 && pixel.G < 24 && pixel.B < 24)
                        return true;
                }
            }
            return false;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRectangle {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", EntryPoint = "SendMessageW", ExactSpelling = true)]
        private static extern IntPtr SendMessage(IntPtr window, int message, int groupId, ref NativeRectangle rectangle);

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_RejectedRowHeightImageCopy_IsDisposedAndKeepsThePreviousList(bool attachBeforeThrow) {
            using (var source = new ImageList())
            using (var bitmap = new Bitmap(16, 16))
            using (var list = new RejectingImageListView { View = View.Details }) {
                source.Images.Add(bitmap);
                list.SmallImageList = source;
                list.RowHeight = 32;
                ImageList previous = list.BaseSmallImageList;
                list.RejectReplacement = true;
                list.AttachBeforeThrow = attachBeforeThrow;
                Assert.ThrowsExactly<InvalidOperationException>(() => list.RowHeight = 40);
                Assert.IsTrue(list.CopyDisposed);
                Assert.AreSame(previous, list.BaseSmallImageList);
                Assert.AreSame(source, list.SmallImageList);
                list.Dispose();
                Assert.IsTrue(list.CopyDisposed);
                Assert.AreEqual(1, source.Images.Count);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_RejectedCheckboxImageCopy_IsDisposedAndCanBeRetried(bool attachBeforeThrow) {
            using (var source = new ImageList())
            using (var bitmap = new Bitmap(16, 16))
            using (var list = new RejectingImageListView { View = View.Details }) {
                source.Images.Add("asset", bitmap);
                list.SmallImageList = source;
                list.RejectReplacement = true;
                list.AttachBeforeThrow = attachBeforeThrow;
                Assert.ThrowsExactly<InvalidOperationException>(() => list.UseSubItemCheckBoxes = true);
                Assert.IsTrue(list.CopyDisposed);
                Assert.AreSame(source, list.SmallImageList);
                Assert.AreSame(source, list.BaseSmallImageList);

                list.RejectReplacement = false;
                list.SetupSubItemCheckBoxes();
                Assert.AreEqual(0, list.SmallImageList.Images.IndexOfKey("asset"));
                Assert.IsTrue(list.SmallImageList.Images.ContainsKey(ObjectListViewBase.CHECKED_KEY));
                Assert.AreEqual(1, source.Images.Count);
            }
        }

        [TestMethod]
        public void Test_RowHeightImageCopies_AreDisposedWithoutDisposingTheSource() {
            using (var source = new ImageList())
            using (var bitmap = new Bitmap(16, 16)) {
                source.Images.Add(bitmap);
                bool sourceDisposed = false;
                source.Disposed += delegate { sourceDisposed = true; };
                bool currentDisposed = false;
                using (var list = new ObjectListView { View = View.Details, SmallImageList = source, RowHeight = 32 }) {
                    ImageList previous = list.BaseSmallImageList;
                    bool previousDisposed = false;
                    previous.Disposed += delegate { previousDisposed = true; };
                    list.RowHeight = 40;
                    Assert.IsTrue(previousDisposed);
                    Assert.IsFalse(sourceDisposed);
                    list.BaseSmallImageList.Disposed += delegate { currentDisposed = true; };
                }
                Assert.IsTrue(currentDisposed);
                Assert.IsFalse(sourceDisposed);
            }
        }

        [TestMethod]
        public void Test_DefaultRowColors_UseDarkPaletteAndPreserveOverrides() {
            using (var list = new ObjectListView { View = View.Details, ShowGroups = false, UseAlternatingBackColors = true }) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                list.SetObjects(new[] { new { Name = "First" }, new { Name = "Second" } });
                Assert.IsLessThan(0.5f, list.BackColor.GetBrightness());
                Assert.IsGreaterThan(0.5f, list.ForeColor.GetBrightness());
                Assert.AreEqual(ObjectListViewTheme.Current.ControlColor, list.GetItem(1).BackColor);
                Assert.IsLessThan(0.5f, list.HotItemStyleOrDefault.BackColor.GetBrightness());

                list.AlternateRowBackColor = Color.Purple;
                list.HotItemStyle = new HotItemStyle { BackColor = Color.Teal };
                list.SelectedBackColor = Color.Navy;
                list.SelectedForeColor = Color.Yellow;
                list.SetObjects(new[] { new { Name = "First" }, new { Name = "Second" } });
                Assert.AreEqual(Color.Purple, list.GetItem(1).BackColor);
                Assert.AreEqual(Color.Teal, list.HotItemStyleOrDefault.BackColor);
                Assert.AreEqual(Color.Navy, list.SelectedBackColorOrDefault);
                Assert.AreEqual(Color.Yellow, list.SelectedForeColorOrDefault);
            }
        }
        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        public void Test_CustomHeader_UsesDarkBackgroundAndReadableText(bool hot, bool pressed) {
            using (var list = new ObjectListView { View = View.Details, ShowGroups = false })
            using (var bitmap = new Bitmap(200, 40))
            using (var graphics = Graphics.FromImage(bitmap)) {
                var column = new OLVColumn("Header", "Name") { Width = 200 };
                list.Columns.Add(column);
                list.CreateControl();
                var header = new DarkHeaderProbe(list);
                try {
                    Rectangle bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                    header.DrawBackground(graphics, bounds, hot, pressed, new HeaderStateStyle());
                    Color expected = pressed ? ObjectListViewTheme.Current.ControlDarkColor :
                        hot ? ObjectListViewTheme.Current.ControlLightColor : ObjectListViewTheme.Current.ControlColor;
                    Assert.AreEqual(expected.ToArgb(), bitmap.GetPixel(2, 2).ToArgb());
                    header.DrawText(graphics, bounds, column, new HeaderStateStyle());
                    Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.Current.TextColor));

                    header.DrawBackground(graphics, bounds, hot, pressed,
                        new HeaderStateStyle { BackColor = Color.Purple });
                    Assert.AreEqual(Color.Purple.ToArgb(), bitmap.GetPixel(2, 2).ToArgb());
                    header.DrawText(graphics, bounds, column, new HeaderStateStyle { ForeColor = Color.Black });
                    Assert.IsTrue(ContainsColor(bitmap, Color.Black));
                } finally {
                    header.ReleaseHandle();
                }
            }
        }
        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public void Test_HeaderPainting_RemainsDarkAfterHandleRecreation(bool explorerTheme, bool wordWrap) {
            using (var list = new DarkListProbe {
                View = View.Details, ShowGroups = false, HeaderWordWrap = wordWrap,
                UseExplorerTheme = explorerTheme, Size = new Size(200, 100)
            })
            using (var bitmap = new Bitmap(200, 100)) {
                list.Columns.Add(new OLVColumn("Header", "Name") { Width = 180 });
                list.SetObjects(new[] { new { Name = "Row" } });
                list.CreateControl();
                Application.DoEvents();
                list.DrawToBitmap(bitmap, list.ClientRectangle);
                Assert.AreEqual(ObjectListViewTheme.Current.ControlColor.ToArgb(), bitmap.GetPixel(100, 5).ToArgb());
                Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.Current.TextColor, new Rectangle(0, 0, 180, 20)));
                Assert.AreEqual(ObjectListViewTheme.Current.ControlColor.ToArgb(), bitmap.GetPixel(190, 5).ToArgb());

                list.RecreateTestHandle();
                Application.DoEvents();
                list.DrawToBitmap(bitmap, list.ClientRectangle);
                Assert.AreEqual(ObjectListViewTheme.Current.ControlColor.ToArgb(), bitmap.GetPixel(100, 5).ToArgb());
                Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.Current.TextColor, new Rectangle(0, 0, 180, 20)));
                Assert.AreEqual(!explorerTheme, list.OwnerDraw);
            }
        }

        [TestMethod]
        public void Test_Hyperlinks_HaveReadableDefaultsAndPreserveOverrides() {
            using (var style = new HyperlinkStyle()) {
                Assert.AreEqual(ObjectListViewTheme.Current.LinkColor, style.Normal.ForeColor);
                Assert.IsGreaterThan(0.5f, style.Visited.ForeColor.GetBrightness());
                style.Normal.ForeColor = Color.Orange;
                style.Visited.ForeColor = Color.Yellow;
                using (var list = new ObjectListView { UseHyperlinks = true, HyperlinkStyle = style }) {
                    Assert.AreEqual(Color.Orange, list.HyperlinkStyle.Normal.ForeColor);
                    Assert.AreEqual(Color.Yellow, list.HyperlinkStyle.Visited.ForeColor);
                }
            }
        }

        [TestMethod]
        [DataRow(CheckBoxState.UncheckedNormal)]
        [DataRow(CheckBoxState.UncheckedHot)]
        [DataRow(CheckBoxState.UncheckedPressed)]
        [DataRow(CheckBoxState.UncheckedDisabled)]
        [DataRow(CheckBoxState.CheckedNormal)]
        [DataRow(CheckBoxState.CheckedDisabled)]
        [DataRow(CheckBoxState.MixedNormal)]
        public void Test_CheckBoxes_DoNotUseAWhiteFill(CheckBoxState state) {
            using (var list = new ObjectListView { View = View.Details, ShowGroups = false })
            using (var bitmap = new Bitmap(40, 40))
            using (var graphics = Graphics.FromImage(bitmap)) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                list.SetObjects(new[] { new { Name = "Row" } });
                var item = list.GetItem(0);
                var renderer = new DarkCheckBoxProbe {
                    ListView = list, Column = list.GetColumn(0), ListItem = item,
                    SubItem = item.GetSubItem(0), State = state, UseCustomCheckboxImages = false
                };
                graphics.Clear(Color.Magenta);
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                renderer.Draw(graphics, new Rectangle(0, 0, 40, 40));
                Assert.IsLessThan(0.5f, bitmap.GetPixel(2, 15).GetBrightness());
                if (state == CheckBoxState.UncheckedNormal)
                {
                    Assert.IsGreaterThan(0.5f, bitmap.GetPixel(0, 15).GetBrightness());
                }

                if (state < CheckBoxState.CheckedNormal)
                {
                    Assert.IsFalse(ContainsColor(bitmap, Color.White));
                }
                else if (state != CheckBoxState.CheckedDisabled)
                {
                    Assert.IsTrue(ContainsColor(bitmap, Color.White));
                }

                if (state == CheckBoxState.CheckedNormal || state == CheckBoxState.MixedNormal)
                {
                    Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.Current.AccentColor));
                }
            }
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public void Test_Buttons_KeepDarkFillAndImageContent(bool withImage, bool disabled) {
            using (var list = new ObjectListView { View = View.Details, ShowGroups = false })
            using (var image = new Bitmap(16, 16))
            using (var imageGraphics = Graphics.FromImage(image))
            using (var bitmap = new Bitmap(140, 50))
            using (var graphics = Graphics.FromImage(bitmap)) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                list.SetObjects(new[] { new { Name = "Start" } });
                var item = list.GetItem(0);
                if (disabled) {
                    list.DisableObject(item.RowObject);
                    item = list.GetItem(0);
                }
                imageGraphics.Clear(Color.Magenta);
                if (withImage)
                {
                    item.ImageSelector = image;
                }

                var renderer = new DarkButtonProbe {
                    ListView = list, Column = list.GetColumn(0), ListItem = item, SubItem = item.GetSubItem(0)
                };
                graphics.Clear(Color.Black);
                renderer.Draw(graphics, new Rectangle(5, 5, 130, 40));
                Assert.IsLessThan(0.5f, bitmap.GetPixel(7, 7).GetBrightness());
                Assert.IsTrue(ContainsColor(bitmap, disabled ? ObjectListViewTheme.Current.ButtonDisabledTextColor : ObjectListViewTheme.Current.ButtonTextColor));
                if (withImage)
                {
                    Assert.IsTrue(ContainsColor(bitmap, Color.Magenta));
                }

                Assert.IsTrue(ContainsColor(bitmap, disabled ? ObjectListViewTheme.Current.ButtonDisabledColor : ObjectListViewTheme.Current.ButtonColor));
            }
        }

        [TestMethod]
        public void Test_TaskDescriptions_UseReadableDefaultsAndPreserveOverrides() {
            using (var list = new ObjectListView { View = View.Details, ShowGroups = false })
            using (var bitmap = new Bitmap(300, 100))
            using (var graphics = Graphics.FromImage(bitmap)) {
                list.Columns.Add(new OLVColumn("Title", "Title"));
                var model = new { Title = "Task", Description = "Readable description" };
                list.SetObjects(new[] { model });
                var item = list.GetItem(0);
                var renderer = new DescribedTaskRenderer {
                    ListView = list, Column = list.GetColumn(0), ListItem = item,
                    SubItem = item.GetSubItem(0), RowObject = model, DescriptionAspectName = "Description"
                };
                Rectangle bounds = new Rectangle(0, 0, 300, 100);
                var args = new DrawListViewSubItemEventArgs(graphics, bounds, item, item.GetSubItem(0),
                    0, 0, list.GetColumn(0), ListViewItemStates.Default);
                renderer.RenderSubItem(args, graphics, bounds, model);
                Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.Current.SecondaryTextColor));
                renderer.DescriptionColor = Color.Orange;
                renderer.RenderSubItem(args, graphics, bounds, model);
                Assert.IsTrue(ContainsColor(bitmap, Color.Orange));
                var overlay = (TextOverlay)list.EmptyListMsgOverlay;
                Assert.IsLessThan(0.5f, overlay.BackColor.GetBrightness());
                Assert.IsGreaterThan(0.5f, overlay.TextColor.GetBrightness());
            }
        }

        [TestMethod]
        [DataRow(false, false, false)]
        [DataRow(false, false, true)]
        [DataRow(true, false, false)]
        [DataRow(true, true, false)]
        public void Test_CellEditor_UsesDarkDefaultsWithoutReplacingCustomColors(bool customEditor, bool systemColors, bool overrideInput) {
            if (overrideInput)
            {
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.InputColor), Color.DarkGreen);
            }
            using (var list = new ObjectListView { View = View.Details, ShowGroups = false }) {
                var column = new OLVColumn("Name", "Name");
                Color customBackColor = systemColors ? SystemColors.Info : Color.Purple;
                Color customForeColor = systemColors ? SystemColors.InfoText : Color.Yellow;
                if (customEditor)
                {
                    column.EditorCreator = delegate { return new TextBox { BackColor = customBackColor, ForeColor = customForeColor }; };
                }

                list.Columns.Add(column);
                list.SetObjects(new[] { new { Name = "Row" } });
                list.CreateControl();
                Control editor = null;
                list.CellEditStarting += delegate(object sender, CellEditEventArgs e) {
                    editor = e.Control;
                    e.Cancel = true;
                };
                try {
                    list.StartCellEdit(list.GetItem(0), 0);
                    Assert.IsNotNull(editor);
                    Assert.AreEqual(customEditor ? customBackColor : ObjectListViewTheme.Current.InputColor, editor.BackColor);
                    Assert.AreEqual(customEditor ? customForeColor : ObjectListViewTheme.Current.TextColor, editor.ForeColor);
                } finally {
                    if (editor != null)
                    {
                        editor.Dispose();
                    }
                }
            }
        }

        [TestMethod]
        public void Test_ExplicitRowColors_AreNotReplacedByThePalette() {
            using (var list = new ObjectListView {
                BackColor = Color.Purple, ForeColor = Color.Yellow, View = View.Details, ShowGroups = false
            }) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                list.SetObjects(new[] { new { Name = "Row" } });
                Assert.AreEqual(Color.Purple, list.GetItem(0).BackColor);
                Assert.AreEqual(Color.Yellow, list.GetItem(0).ForeColor);
            }
        }

#if NET10_0_OR_GREATER
        [TestMethod]
        [DataRow(ObjectListViewColorMode.System)]
        [DataRow(ObjectListViewColorMode.Dark)]
        public void Test_CustomDrawing_UsesTheSameDarkColorsWithEitherNativeRenderingBackend(ObjectListViewColorMode mode) {
            SystemColorMode original = Application.ColorMode;
            try {
                Application.SetColorMode(SystemColorMode.Classic);
                ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Dark;
                var defined = new ThemePalette(true, false);
                Application.SetColorMode(SystemColorMode.Dark);
                ObjectListViewTheme.ColorMode = mode;
                IThemeColors nativeContext = ObjectListViewTheme.Current;
                foreach (PropertyInfo property in typeof(IThemeColors).GetProperties().Where(property => property.PropertyType == typeof(Color)))
                {
                    Color expected = (Color)property.GetValue(defined);
                    Color actual = (Color)property.GetValue(nativeContext);
                    Assert.AreEqual(expected.ToArgb(), actual.ToArgb(),
                        property.Name + " must not depend on the native rendering backend.");
                }
                using (var list = new ObjectListView { View = View.Details, ShowGroups = false, UseAlternatingBackColors = true }) {
                    list.Columns.Add(new OLVColumn("Name", "Name"));
                    list.SetObjects(new[] { new { Name = "First" }, new { Name = "Second" } });
                    Assert.AreEqual(defined.WindowColor.ToArgb(), list.GetItem(0).BackColor.ToArgb());
                    Assert.AreEqual(defined.TextColor.ToArgb(), list.GetItem(0).ForeColor.ToArgb());
                    Assert.AreEqual(defined.ControlColor.ToArgb(), list.GetItem(1).BackColor.ToArgb());
                    Assert.AreEqual(defined.HighlightColor.ToArgb(), list.SelectedBackColorOrDefault.ToArgb());
                    Assert.AreEqual(defined.HighlightTextColor.ToArgb(), list.SelectedForeColorOrDefault.ToArgb());
                    Assert.AreEqual(SystemColorMode.Dark, Application.ColorMode);
                }

            } finally {
                Application.SetColorMode(original);
            }
        }

        [TestMethod]
        public void Test_ExplicitLightPalette_OverridesDarkWinFormsRowAndHeaderDefaults() {
            SystemColorMode original = Application.ColorMode;
            try {
                Application.SetColorMode(SystemColorMode.Classic);
                int window = SystemColors.Window.ToArgb();
                int text = SystemColors.WindowText.ToArgb();
                int control = SystemColors.Control.ToArgb();
                int controlText = SystemColors.ControlText.ToArgb();
                int buttonHover = SystemColors.ControlLight.ToArgb();
                int link = SystemColors.HotTrack.ToArgb();
                Application.SetColorMode(SystemColorMode.Dark);
                ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Light;
                using (var list = new ObjectListView { View = View.Details, ShowGroups = false, Size = new Size(200, 100) })
                using (var bitmap = new Bitmap(200, 100)) {
                    list.Columns.Add(new OLVColumn("Name", "Name") { Width = 180 });
                    list.SetObjects(new[] { new { Name = "Row" } });
                    list.CreateControl();
                    Application.DoEvents();
                    Assert.AreEqual(window, list.GetItem(0).BackColor.ToArgb());
                    Assert.AreEqual(text, list.GetItem(0).ForeColor.ToArgb());
                    Assert.AreEqual(control, list.UnfocusedSelectedBackColorOrDefault.ToArgb());
                    Assert.AreEqual(controlText, list.UnfocusedSelectedForeColorOrDefault.ToArgb());
                    Assert.AreEqual(buttonHover, ObjectListViewTheme.Current.ButtonHoverColor.ToArgb());
                    Assert.AreEqual(link, ObjectListViewTheme.Current.LinkColor.ToArgb());
                    ThemePalette palette = new ThemePalette(false, false);
                    Assert.AreEqual(window, palette.WindowColor.ToArgb());
                    Assert.AreEqual(text, palette.TextColor.ToArgb());
                    Assert.AreEqual(SystemColorMode.Dark, Application.ColorMode);
                    Assert.AreEqual(ObjectListViewColorMode.Light, ObjectListViewTheme.ColorMode);
                    list.UnfocusedSelectedForeColor = Color.Purple;
                    Assert.AreEqual(Color.Purple, list.UnfocusedSelectedForeColorOrDefault);
                    list.DrawToBitmap(bitmap, list.ClientRectangle);
                    Assert.IsGreaterThan(0.5f, bitmap.GetPixel(100, 5).GetBrightness());
                }
            } finally {
                Application.SetColorMode(original);
            }
        }

        [TestMethod]
        [DataRow(SystemColorMode.Classic)]
        [DataRow(SystemColorMode.Dark)]
        public void Test_SystemMode_FollowsTheHostWithoutChangingItsWinFormsMode(SystemColorMode hostMode) {
            SystemColorMode original = Application.ColorMode;
            try {
                Application.SetColorMode(hostMode);
                FormTheme.SetColorMode(ObjectListViewColorMode.System);
                bool expectedDark = Application.IsDarkModeEnabled;
                Assert.AreEqual(hostMode, Application.ColorMode);
                Assert.AreEqual(expectedDark, ObjectListViewTheme.IsDarkMode);
                IThemeColors colors = ObjectListViewTheme.Current;
                Assert.AreEqual(expectedDark, colors.Dark);
                using (var list = new ObjectListView()) {
                    Assert.AreEqual(colors.WindowColor.ToArgb(), list.BackColor.ToArgb());
                    Assert.AreEqual(colors.TextColor.ToArgb(), list.ForeColor.ToArgb());
                }
            } finally {
                Application.SetColorMode(original);
            }
        }
#endif

#if NETFRAMEWORK
        [TestMethod]
        public void Test_SystemMode_UsesTheWindowsPreferenceOnFramework() {
            bool windowsDark;
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                windowsDark = key?.GetValue("AppsUseLightTheme") is int value && value == 0;
            }
            ObjectListViewTheme.ColorMode = ObjectListViewColorMode.System;
            Assert.AreEqual(windowsDark, ObjectListViewTheme.IsDarkMode);
            Assert.AreEqual(windowsDark, ObjectListViewTheme.Current.Dark);
        }
#endif

        [TestMethod]
        public void Test_LightPalette_UsesNativeRowAndLinkRoles() {
#if NET10_0_OR_GREATER
            SystemColorMode original = Application.ColorMode;
            Application.SetColorMode(SystemColorMode.Classic);
            try {
#endif
                ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Light;
                using (var list = new ObjectListView()) {
                    Assert.AreEqual(SystemColors.Control.ToArgb(), list.AlternateRowBackColorOrDefault.ToArgb());
                    Assert.AreEqual(SystemColors.ControlLight.ToArgb(), list.HotItemStyleOrDefault.BackColor.ToArgb());
                }
                using (var style = new HyperlinkStyle()) {
                    Assert.AreEqual(SystemColors.HotTrack.ToArgb(), style.Normal.ForeColor.ToArgb());
                    Assert.AreEqual(SystemColors.HotTrack.ToArgb(), style.Visited.ForeColor.ToArgb());
                }
#if NET10_0_OR_GREATER
            } finally {
                Application.SetColorMode(original);
            }
#endif
        }

        private sealed class RejectingImageListView : ObjectListView {
            public bool RejectReplacement;
            public bool AttachBeforeThrow;
            public bool CopyDisposed;

            public override ImageList BaseSmallImageList {
                get { return base.BaseSmallImageList; }
                set {
                    if (this.RejectReplacement) {
                        value.Disposed += delegate { this.CopyDisposed = true; };
                        if (this.AttachBeforeThrow)
                        {
                            base.BaseSmallImageList = value;
                        }

                        throw new InvalidOperationException("The replacement was rejected.");
                    }
                    base.BaseSmallImageList = value;
                }
            }
        }

        private sealed class DarkHeaderProbe : HeaderControl
        {
            public DarkHeaderProbe(ObjectListViewBase list) : base(list) { }

            public void DrawBackground(Graphics graphics, Rectangle bounds, bool hot, bool pressed, HeaderStateStyle style) {
                this.DrawUnthemedBackground(graphics, bounds, 0, pressed, hot, style);
            }

            public void DrawText(Graphics graphics, Rectangle bounds, OLVColumn column, HeaderStateStyle style) {
                this.DrawHeaderImageAndText(graphics, bounds, column, style);
            }
        }

        private sealed class DarkListProbe : ObjectListView
        {
            public int NativeNonClientPaints;
            public int NativeNonClientMoves;

            protected override void DefWndProc(ref Message m) {
                if (m.Msg == 0x85)
                {
                    this.NativeNonClientPaints++;
                }
                if (m.Msg == 0xA0)
                {
                    this.NativeNonClientMoves++;
                }
                base.DefWndProc(ref m);
            }

            public void RecreateTestHandle() {
                this.RecreateHandle();
            }
        }

        private sealed class DarkCheckBoxProbe : BaseRenderer {
            public CheckBoxState State;
            protected override CheckBoxState GetCheckBoxState(CheckState state) {
                return this.State;
            }
            public void Draw(Graphics graphics, Rectangle bounds) {
                this.DrawCheckBox(graphics, bounds);
            }
        }

        private sealed class DarkButtonProbe : ColumnButtonRenderer {
            public void Draw(Graphics graphics, Rectangle bounds) {
                this.DrawImageAndText(graphics, bounds);
            }
        }
    }

}
