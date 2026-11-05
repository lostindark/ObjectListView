using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests {
    [STATestClass]
    public class TestFormTheme : ThemeTestFixture {
#if NETFRAMEWORK
        [TestMethod]
        [DataRow(TabDrawMode.Normal)]
        [DataRow(TabDrawMode.OwnerDrawFixed)]
        public void Test_DarkCreatedTabs_PreserveConfiguredNativeSettingsAcrossThemeChanges(TabDrawMode drawMode) {
            ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Dark;
            using (var form = new Form { Opacity = 0, ShowInTaskbar = false })
            using (var tabs = new ThemedTabProbe { Size = new Size(280, 150), Padding = new Point(20, 8), DrawMode = drawMode }) {
                tabs.TabPages.Add("First");
                tabs.TabPages.Add("Second");
                int customDraws = 0;
                tabs.DrawItem += (sender, e) => customDraws++;
                form.Controls.Add(tabs);
                FormTheme theme = FormTheme.Attach(form);
                form.Show();
                Application.DoEvents();
                Assert.IsTrue(tabs.UsesManagedPainting);

                theme.Apply(new ThemePalette(false, false));
                Assert.AreEqual(new Point(20, 8), tabs.Padding);
                Assert.AreEqual(drawMode, tabs.DrawMode);
                Assert.IsFalse(tabs.UsesManagedPainting);
                if (drawMode == TabDrawMode.OwnerDrawFixed) {
                    customDraws = 0;
                    using (var bitmap = new Bitmap(tabs.Width, tabs.Height))
                    {
                        tabs.DrawToBitmap(bitmap, tabs.ClientRectangle);
                    }
                    Assert.IsTrue(customDraws > 0);
                }
                theme.Apply(new ThemePalette(true, false));
                theme.Apply(new ThemePalette(true, true));
                Assert.AreEqual(new Point(20, 8), tabs.Padding);
                Assert.AreEqual(drawMode, tabs.DrawMode);
                Assert.IsFalse(tabs.UsesManagedPainting);
            }
        }

        [TestMethod]
        public void Test_FrameworkTabs_ReconfigureForDarkLightAndHighContrastTransitions() {
            ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Light;
            using (var form = new Form { Opacity = 0, ShowInTaskbar = false })
            using (var tabs = new ThemedTabProbe { Size = new Size(280, 150), Padding = new Point(14, 7) }) {
                tabs.TabPages.Add("First");
                tabs.TabPages.Add("Second");
                form.Controls.Add(tabs);
                FormTheme theme = FormTheme.Attach(form);
                form.Show();
                Application.DoEvents();
                Assert.AreEqual(TabDrawMode.Normal, tabs.DrawMode);
                Assert.IsFalse(tabs.UsesManagedPainting);
                FormTheme.SetColorMode(ObjectListViewColorMode.Dark);
                Assert.AreEqual(TabDrawMode.OwnerDrawFixed, tabs.DrawMode);
                Assert.IsTrue(tabs.UsesManagedPainting);
                using (var bitmap = new Bitmap(tabs.Width, tabs.Height)) {
                    tabs.DrawToBitmap(bitmap, tabs.ClientRectangle);
                    Rectangle bounds = tabs.GetTabRect(0);
                    Assert.AreEqual(ObjectListViewTheme.Current.ControlLightColor.ToArgb(),
                        bitmap.GetPixel(bounds.Left + 3, bounds.Top + 3).ToArgb());
                }
                FormTheme.SetColorMode(ObjectListViewColorMode.Light);
                Assert.AreEqual(TabDrawMode.Normal, tabs.DrawMode);
                Assert.AreEqual(new Point(14, 7), tabs.Padding);
                Assert.IsFalse(tabs.UsesManagedPainting);
                FormTheme.SetColorMode(ObjectListViewColorMode.Dark);
                Assert.IsTrue(tabs.UsesManagedPainting);
                theme.Apply(new ThemePalette(true, true));
                Assert.AreEqual(TabDrawMode.Normal, tabs.DrawMode);
                Assert.AreEqual(new Point(14, 7), tabs.Padding);
                Assert.IsFalse(tabs.UsesManagedPainting);
            }
        }

        private sealed class ThemedTabProbe : ThemedTabControl {
            public bool UsesManagedPainting => this.GetStyle(ControlStyles.UserPaint);
        }
#endif

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_VisitedLinks_UseTheirOwnPaletteColorAndPreserveExplicitStyles(bool explicitColor) {
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.LinkColor), Color.Cyan);
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.VisitedLinkColor), Color.Magenta);
            using (var form = new Form())
            using (var link = new LinkLabel { Text = "Visited", LinkVisited = true }) {
                if (explicitColor)
                {
                    link.VisitedLinkColor = Color.Gold;
                }
                form.Controls.Add(link);
                FormTheme theme = FormTheme.Attach(form);
                Assert.AreEqual(Color.Cyan, link.LinkColor);
                Assert.AreEqual(explicitColor ? Color.Gold : Color.Magenta, link.VisitedLinkColor);
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.VisitedLinkColor), Color.LightGreen);
                theme.Apply(new ThemePalette(true, false));
                Assert.AreEqual(explicitColor ? Color.Gold : Color.LightGreen, link.VisitedLinkColor);
            }
        }

        [TestMethod]
        public void Test_DarkOverrides_RejectUnknownAndNonColorNames() {
            Assert.ThrowsExactly<ArgumentNullException>(() => ObjectListViewTheme.SetDarkColor(null, Color.Navy));
            Assert.ThrowsExactly<ArgumentException>(() => ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.Dark), Color.Navy));
            Assert.ThrowsExactly<ArgumentException>(() => ObjectListViewTheme.SetDarkColor("NotAColor", Color.Navy));
        }

        [TestMethod]
        public void Test_DarkOverrides_AreAvailableThroughTheColorInterfaceOnlyInDarkContexts() {
#if NET10_0_OR_GREATER
            SystemColorMode original = Application.ColorMode;
            Application.SetColorMode(SystemColorMode.Classic);
            try {
#endif
                ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Light;
                var definedDark = new ThemePalette(true, false);
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.WindowColor), Color.Navy);
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ButtonColor), Color.Maroon);
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.InputColor), Color.DarkGreen);
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.VisitedLinkColor), Color.LightGreen);
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.HighlightColor), Color.Crimson);
                IThemeColors light = ObjectListViewTheme.Current;
                ThemePalette lightSnapshot = new ThemePalette(false, false);
                Assert.IsFalse(light.Dark);
                Assert.AreEqual(SystemColors.Window.ToArgb(), light.WindowColor.ToArgb());
                Assert.AreEqual(SystemColors.Control.ToArgb(), light.ButtonColor.ToArgb());
                Assert.AreEqual(SystemColors.Window.ToArgb(), light.InputColor.ToArgb());
                Assert.AreEqual(SystemColors.HotTrack.ToArgb(), light.VisitedLinkColor.ToArgb());
                Assert.AreEqual(SystemColors.Window.ToArgb(), ObjectListViewTheme.Current.WindowColor.ToArgb());

                ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Dark;
                IThemeColors dark = ObjectListViewTheme.Current;
                ThemePalette darkSnapshot = new ThemePalette(true, false);
                Assert.IsTrue(dark.Dark);
                Assert.IsTrue(light.Dark, "The retained Current reader must follow mode changes.");
                Assert.AreEqual(Color.Navy, dark.WindowColor);
                Assert.AreEqual(Color.Maroon, dark.ButtonColor);
                Assert.AreEqual(Color.DarkGreen, dark.InputColor);
                Assert.AreEqual(Color.LightGreen, dark.VisitedLinkColor);
                Assert.AreEqual(Color.Crimson, dark.AccentColor);
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.AccentColor), Color.Gold);
                Assert.AreEqual(Color.Gold, ObjectListViewTheme.Current.AccentColor);
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.AccentColor), Color.Empty);
                Assert.AreEqual(Color.Crimson, ObjectListViewTheme.Current.AccentColor);

                var highContrast = new ThemePalette(true, true);
                Assert.IsTrue(highContrast.HighContrast);
                Assert.IsFalse(highContrast.Dark);
                Assert.AreEqual(SystemColors.Window.ToArgb(), highContrast.WindowColor.ToArgb());
                Assert.AreEqual(SystemColors.Control.ToArgb(), highContrast.ButtonColor.ToArgb());
                Assert.AreEqual(SystemColors.Window.ToArgb(), highContrast.InputColor.ToArgb());
                Assert.AreEqual(SystemColors.Highlight.ToArgb(), highContrast.AccentColor.ToArgb());

                ObjectListViewTheme.ResetColors();
                IThemeColors restored = ObjectListViewTheme.Current;
                Assert.AreEqual(definedDark.WindowColor, restored.WindowColor);
                Assert.AreEqual(definedDark.ButtonColor, restored.ButtonColor);
                Assert.AreEqual(definedDark.InputColor, restored.InputColor);
                Assert.AreEqual(Color.Navy, darkSnapshot.WindowColor, "Explicit snapshots must remain immutable.");
                Assert.AreEqual(SystemColors.Window.ToArgb(), lightSnapshot.WindowColor.ToArgb());
                Assert.AreEqual(definedDark.WindowColor, dark.WindowColor, "The retained Current reader must follow override changes.");
#if NET10_0_OR_GREATER
            } finally {
                Application.SetColorMode(original);
            }
#endif
        }

#if NET10_0_OR_GREATER
        [TestMethod]
        public void Test_LightFormDefaults_StayLightWithoutChangingANativeDarkHost() {
            SystemColorMode original = Application.ColorMode;
            try {
                Application.SetColorMode(SystemColorMode.Classic);
                var light = new ThemePalette(false, false);
                Application.SetColorMode(SystemColorMode.Dark);
                if (!Application.IsDarkModeEnabled) {
                    Assert.Inconclusive("Native dark mode is not supported on this Windows version.");
                }
                ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Light;
                using (var form = new Form())
                using (var label = new Label())
                using (var button = new Button())
                using (var input = new TextBox())
                using (var list = new ListBox()) {
                    form.Controls.AddRange(new Control[] { label, button, input, list });
                    FormTheme theme = FormTheme.Attach(form);
                    foreach (bool dark in new[] { false, true, false }) {
                        theme.Apply(new ThemePalette(dark, false));
                        if (!dark) {
                            Assert.AreEqual(light.ControlColor.ToArgb(), form.BackColor.ToArgb());
                            Assert.AreEqual(light.ControlColor.ToArgb(), label.BackColor.ToArgb());
                            Assert.AreEqual(light.TextColor.ToArgb(), label.ForeColor.ToArgb());
                            Assert.AreEqual(light.ButtonColor.ToArgb(), button.BackColor.ToArgb());
                            Assert.AreEqual(light.InputColor.ToArgb(), input.BackColor.ToArgb());
                            Assert.AreEqual(light.WindowColor.ToArgb(), list.BackColor.ToArgb());
                        }
                        Assert.AreEqual(SystemColorMode.Dark, Application.ColorMode);
                    }
                }
            } finally {
                Application.SetColorMode(original);
            }
        }

        [TestMethod]
        public void Test_PaletteSnapshots_DoNotFollowLaterNativeModeChangesOrChangeTheConfiguredMode() {
            SystemColorMode original = Application.ColorMode;
            try {
                Application.SetColorMode(SystemColorMode.Classic);
                ObjectListViewTheme.ColorMode = ObjectListViewColorMode.System;
                ThemePalette light = new ThemePalette(false, false);
                int background = light.ControlColor.ToArgb();
                int surface = light.WindowColor.ToArgb();
                int text = light.TextColor.ToArgb();
                ThemePalette definedDark = new ThemePalette(true, false);
                Application.SetColorMode(SystemColorMode.Dark);
                bool selectedDark = ObjectListViewTheme.IsDarkMode;
                ThemePalette dark = new ThemePalette(true, false);
                Assert.AreEqual(definedDark.WindowColor, dark.WindowColor);
                Assert.AreEqual(definedDark.ButtonColor, dark.ButtonColor);
                Assert.AreEqual(background, light.ControlColor.ToArgb());
                Assert.AreEqual(surface, light.WindowColor.ToArgb());
                Assert.AreEqual(text, light.TextColor.ToArgb());
                Assert.AreEqual(ObjectListViewColorMode.System, ObjectListViewTheme.ColorMode);
                Assert.AreEqual(selectedDark, ObjectListViewTheme.IsDarkMode);

                int darkSurface = dark.WindowColor.ToArgb();
                Application.SetColorMode(SystemColorMode.Classic);
                Assert.AreEqual(darkSurface, dark.WindowColor.ToArgb());
                Assert.AreEqual(Application.IsDarkModeEnabled, ObjectListViewTheme.IsDarkMode);
            } finally {
                Application.SetColorMode(original);
            }
        }
#endif

        [TestMethod]
        public void Test_NativeColorNotifications_RefreshSnapshotsEvenWhenTheModeHasNotChanged() {
            using (Form form = new Form { Opacity = 0, ShowInTaskbar = false })
            using (ListBox list = new ListBox())
            {
                form.Controls.Add(list);
                int refreshes = 0;
                FormTheme.Attach(form, themeChanged: palette => refreshes++);
                form.Show();
                Application.DoEvents();
                int previous = refreshes;

                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.WindowColor), Color.Navy);
                SendMessage(form.Handle, 0x0015, IntPtr.Zero, IntPtr.Zero);
                Application.DoEvents();

                Assert.IsTrue(refreshes > previous);
                Assert.AreEqual(Color.Navy, list.BackColor);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_FirstFramePreparation_IsOptInAndOnlyAffectsTheFirstDisplay(bool prepare) {
            using (Form form = new Form { Opacity = 0.5, ShowInTaskbar = false })
            {
                FormTheme.Attach(form, prepareFirstFrame: prepare);
                double? firstPaintOpacity = null;
                form.Paint += (sender, e) => firstPaintOpacity ??= form.Opacity;
                form.Show();
                Application.DoEvents();

                Assert.AreEqual(prepare ? 0.0 : 0.5, firstPaintOpacity);
                Assert.AreEqual(0.5, form.Opacity);
                firstPaintOpacity = null;
                form.Hide();
                form.Show();
                form.Refresh();
                Application.DoEvents();
                Assert.AreEqual(0.5, firstPaintOpacity);
                Assert.AreEqual(0.5, form.Opacity);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_FormFirstFrame_SupportsAnIndependentThemeManager(bool dark) {
            ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Light;
            using (Form form = new Form { BackColor = Color.Navy, Opacity = 0.5, ShowInTaskbar = false })
            {
                FormFirstFrame.Attach(form, () => dark);
                double? firstPaintOpacity = null;
                form.Paint += (sender, e) => firstPaintOpacity ??= form.Opacity;
                form.Show();
                Application.DoEvents();

                Assert.AreEqual(dark ? 0.0 : 0.5, firstPaintOpacity);
                Assert.AreEqual(0.5, form.Opacity);
                Assert.AreEqual(Color.Navy, form.BackColor);
                Assert.AreEqual(ObjectListViewColorMode.Light, ObjectListViewTheme.ColorMode);
            }
        }

        [TestMethod]
        public void Test_CancelingFirstFramePreparation_RestoresPendingOpacityWithoutDisposingTheForm() {
            using (Form form = new Form { Opacity = 0.5, ShowInTaskbar = false })
            {
                FormFirstFrame preparation = FormFirstFrame.Attach(form);
                bool loaded = false;
                form.Load += (sender, e) => {
                    loaded = true;
                    Assert.AreEqual(0, form.Opacity);
                    preparation.Dispose();
                };
                form.Show();
                Application.DoEvents();

                Assert.IsTrue(loaded);
                Assert.AreEqual(0.5, form.Opacity);
                Assert.IsFalse(form.IsDisposed);
                Assert.IsTrue(form.Visible);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_LaterApplicationColors_SurviveRefreshEnableChangesAndDetach(bool buttonControl) {
            using (var form = new Form())
            using (Control control = buttonControl ? (Control)new Button() : new Label()) {
                form.Controls.Add(control);
                FormTheme theme = FormTheme.Attach(form);
                control.BackColor = Color.Red;
                control.ForeColor = Color.Gold;
                foreach (bool dark in new[] { true, false, true }) {
                    theme.Apply(new ThemePalette(dark, false));
                    control.Enabled = !control.Enabled;
                    Assert.AreEqual(Color.Red, control.BackColor);
                    Assert.AreEqual(Color.Gold, control.ForeColor);
                }
                theme.Apply(new ThemePalette(true, true));
                theme.Apply(new ThemePalette(true, false));
                Assert.AreEqual(Color.Red, control.BackColor);
                Assert.AreEqual(Color.Gold, control.ForeColor);
                theme.Dispose();
                Assert.AreEqual(Color.Red, control.BackColor);
                Assert.AreEqual(Color.Gold, control.ForeColor);
                if (control is Button button) {
                    Assert.IsFalse(button.UseVisualStyleBackColor);
                }
            }
        }

        [TestMethod]
        public void Test_ClearingLaterApplicationColors_RebindsTheThemeDefaults() {
            using (var form = new Form())
            using (var button = new Button()) {
                form.Controls.Add(button);
                FormTheme theme = FormTheme.Attach(form);
                button.BackColor = Color.Red;
                button.ForeColor = Color.Gold;
                button.BackColor = Color.Empty;
                button.ForeColor = Color.Empty;
                theme.Apply(new ThemePalette(true, false));
                Assert.AreEqual(ObjectListViewTheme.Current.ButtonColor.ToArgb(), button.BackColor.ToArgb());
                Assert.AreEqual(ObjectListViewTheme.Current.ButtonTextColor.ToArgb(), button.ForeColor.ToArgb());
            }
        }

        [TestMethod]
        public void Test_SharedContextMenu_AcrossFormsKeepsOneOriginalAppearanceUntilTheLastDetach() {
            using (var first = new Form())
            using (var second = new Form())
            using (var menu = new ContextMenuStrip()) {
                ToolStripRenderMode originalMode = menu.RenderMode;
                ToolStripItem item = menu.Items.Add("Shared");
                var input = new TextBox();
                menu.Items.Add(new ToolStripControlHost(input));
                first.ContextMenuStrip = menu;
                second.ContextMenuStrip = menu;
                FormTheme firstTheme = FormTheme.Attach(first);
                FormTheme secondTheme = FormTheme.Attach(second);
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.TextColor), Color.Gold);
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.InputColor), Color.Navy);
                firstTheme.Apply(new ThemePalette(true, false));
                secondTheme.Apply(new ThemePalette(true, false));
                Assert.AreEqual(Color.Gold, item.ForeColor);
                Assert.AreEqual(Color.Gold, input.ForeColor);
                Assert.AreEqual(Color.Navy, input.BackColor);

                ToolStripRenderer renderer = menu.Renderer;
                firstTheme.Dispose();
                Assert.AreSame(renderer, menu.Renderer);
                Assert.AreEqual(Color.Gold, item.ForeColor);
                Assert.AreEqual(Color.Navy, input.BackColor);
                var light = new ThemePalette(false, false);
                secondTheme.Apply(light);
                Assert.AreEqual(light.TextColor.ToArgb(), item.ForeColor.ToArgb());
                Assert.AreEqual(light.InputColor.ToArgb(), input.BackColor.ToArgb());
                secondTheme.Apply(new ThemePalette(true, false));
                Assert.AreEqual(Color.Gold, item.ForeColor);
                secondTheme.Dispose();
                Assert.AreEqual(originalMode, menu.RenderMode);
                Assert.AreNotEqual(Color.Gold, item.ForeColor);
            }
        }

        [TestMethod]
        public void Test_SharedContextMenu_RemainsThemedUntilItsLastTrackedOwnerIsRemoved() {
            using (var form = new Form())
            using (var first = new TextBox())
            using (var second = new TextBox())
            using (var menu = new ContextMenuStrip()) {
                ToolStripRenderMode originalMode = menu.RenderMode;
                ToolStripItem item = menu.Items.Add("Shared");
                form.ContextMenuStrip = menu;
                first.ContextMenuStrip = menu;
                second.ContextMenuStrip = menu;
                form.Controls.AddRange(new Control[] { first, second });
                FormTheme theme = FormTheme.Attach(form);
                ToolStripRenderer renderer = menu.Renderer;

                form.Controls.Remove(first);
                Assert.AreSame(renderer, menu.Renderer);
                Assert.AreEqual(ObjectListViewTheme.Current.TextColor, item.ForeColor);
                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.TextColor), Color.Gold);
                theme.Apply(new ThemePalette(true, false));
                Assert.AreEqual(Color.Gold, item.ForeColor);

                form.ContextMenuStrip = null;
                form.Controls.Remove(second);
                Assert.AreEqual(originalMode, menu.RenderMode);
                Assert.AreNotEqual(Color.Gold, item.ForeColor);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_AbortedLoad_DoesNotLeaveASurvivingFormTransparent(bool createHandle) {
            using (var form = new LoadProbe { Opacity = 0.5, ShowInTaskbar = false })
            using (FormFirstFrame preparation = FormFirstFrame.Attach(form)) {
                if (createHandle) {
                    _ = form.Handle;
                }
                form.Load += (sender, e) => { throw new InvalidOperationException("Expected load failure"); };
                Assert.ThrowsExactly<InvalidOperationException>(() => form.InvokeLoad());
                Assert.AreEqual(0.5, form.Opacity);
                Assert.IsFalse(form.IsDisposed);
            }
        }

        private sealed class LoadProbe : Form {
            public void InvokeLoad() => this.OnLoad(EventArgs.Empty);
        }

        [TestMethod]
        public void Test_ExplicitSystemColors_SurviveAttachmentAndRefresh() {
            using (Form form = new Form())
            using (Label label = new Label { BackColor = SystemColors.Info, ForeColor = SystemColors.InfoText })
            using (Button button = new Button { BackColor = SystemColors.Info, ForeColor = SystemColors.InfoText })
            using (ComboBox combo = new ComboBox { BackColor = SystemColors.Info, ForeColor = SystemColors.InfoText })
            using (LinkLabel link = new LinkLabel { LinkColor = SystemColors.HotTrack })
            using (ContextMenuStrip menu = new ContextMenuStrip { BackColor = SystemColors.Info })
            using (DataGridView grid = new DataGridView {
                BackgroundColor = SystemColors.Info, GridColor = SystemColors.InfoText,
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = SystemColors.Info, ForeColor = SystemColors.InfoText },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = SystemColors.Info }
            }) {
                ToolStripItem item = menu.Items.Add("Custom");
                item.ForeColor = SystemColors.InfoText;
                form.ContextMenuStrip = menu;
                form.Controls.AddRange(new Control[] { label, button, combo, link, grid });
                FormTheme theme = FormTheme.Attach(form);
                foreach (bool dark in new[] { true, false, true })
                {
                    theme.Apply(new ThemePalette(dark, false));
                    foreach (Control control in new Control[] { label, button, combo })
                    {
                        Assert.AreEqual(SystemColors.Info, control.BackColor);
                        Assert.AreEqual(SystemColors.InfoText, control.ForeColor);
                    }
                    button.Enabled = !button.Enabled;
                    Assert.AreEqual(SystemColors.Info, button.BackColor);
                    Assert.AreEqual(SystemColors.HotTrack, link.LinkColor);
                    Assert.AreEqual(SystemColors.Info, menu.BackColor);
                    Assert.AreEqual(SystemColors.InfoText, item.ForeColor);
                    Assert.AreEqual(SystemColors.Info, grid.BackgroundColor);
                    Assert.AreEqual(SystemColors.InfoText, grid.GridColor);
                    Assert.AreEqual(SystemColors.Info, grid.DefaultCellStyle.BackColor);
                    Assert.AreEqual(SystemColors.InfoText, grid.DefaultCellStyle.ForeColor);
                    Assert.AreEqual(SystemColors.Info, grid.ColumnHeadersDefaultCellStyle.BackColor);
                }
            }
        }

        [TestMethod]
        [DataRow(DrawMode.OwnerDrawFixed)]
        [DataRow(DrawMode.OwnerDrawVariable)]
        public void Test_ApplicationOwnedDrawing_RemainsInControl(DrawMode drawMode) {
            using (Form form = new Form())
            using (OwnerDrawComboBox combo = new OwnerDrawComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DrawMode = drawMode })
            using (MenuStrip strip = new MenuStrip())
            using (ContextMenuStrip menu = new ContextMenuStrip())
            using (Bitmap bitmap = new Bitmap(100, 30))
            using (Graphics graphics = Graphics.FromImage(bitmap)) {
                ToolStripRenderer renderer = new ToolStripProfessionalRenderer(new ProfessionalColorTable());
                strip.Renderer = renderer;
                menu.Renderer = renderer;
                form.ContextMenuStrip = menu;
                combo.Items.Add("Custom");
                int draws = 0;
                combo.DrawItem += (sender, e) => {
                    draws++;
                    e.Graphics.Clear(Color.Magenta);
                };
                form.Controls.AddRange(new Control[] { combo, strip });
                FormTheme theme = FormTheme.Attach(form);
                foreach (bool dark in new[] { true, false, true })
                {
                    theme.Apply(new ThemePalette(dark, false));
                    Assert.AreEqual(drawMode, combo.DrawMode);
                    Assert.AreSame(renderer, strip.Renderer);
                    Assert.AreSame(renderer, menu.Renderer);
                    combo.Draw(graphics, new Rectangle(0, 0, 100, 30));
                    Assert.AreEqual(Color.Magenta.ToArgb(), bitmap.GetPixel(99, 29).ToArgb());
                }
                Assert.AreEqual(3, draws);
            }
        }

        [TestMethod]
        public void Test_ApplicationWideToolStripRenderer_IsNotReplaced() {
            ToolStripManagerRenderMode originalMode = ToolStripManager.RenderMode;
            ToolStripRenderer originalRenderer = ToolStripManager.Renderer;
            try {
                ToolStripRenderer renderer = new ToolStripProfessionalRenderer(new ProfessionalColorTable());
                ToolStripManager.Renderer = renderer;
                using (Form form = new Form())
                using (MenuStrip strip = new MenuStrip()) {
                    form.Controls.Add(strip);
                    FormTheme theme = FormTheme.Attach(form);
                    theme.Apply(new ThemePalette(true, false));
                    Assert.AreEqual(ToolStripRenderMode.ManagerRenderMode, strip.RenderMode);
                    Assert.AreSame(renderer, strip.Renderer);
                }
            } finally {
                if (originalMode == ToolStripManagerRenderMode.Custom)
                {
                    ToolStripManager.Renderer = originalRenderer;
                }
                else
                {
                    ToolStripManager.RenderMode = originalMode;
                }
            }
        }

        [TestMethod]
        public void Test_RemovedSubtree_DetachesHandlersAndRestoresOriginalStyling() {
            using (Form form = new Form())
            using (Panel panel = new Panel())
            using (Button button = new Button()) {
                FlatStyle originalStyle = button.FlatStyle;
                bool visualStyle = button.UseVisualStyleBackColor;
                panel.Controls.Add(button);
                form.Controls.Add(panel);
                int callbacks = 0;
                FormTheme theme = FormTheme.Attach(form, (control, palette) => callbacks++);
                form.Controls.Remove(panel);
                Assert.AreEqual(originalStyle, button.FlatStyle);
                Assert.AreEqual(visualStyle, button.UseVisualStyleBackColor);
                theme.Dispose();

                int captured = callbacks;
                button.BackColor = Color.Red;
                button.Enabled = false;
                using (Label late = new Label())
                {
                    panel.Controls.Add(late);
                    Assert.AreEqual(captured, callbacks);
                }
                Assert.AreEqual(Color.Red, button.BackColor);
            }
        }

        [TestMethod]
        public void Test_Reparenting_TransfersStylingAndCallbacksToTheNewTheme() {
            using (Form source = new Form())
            using (Form destination = new Form())
            using (Panel panel = new Panel())
            using (Button button = new Button()) {
                FlatStyle originalStyle = button.FlatStyle;
                bool visualStyle = button.UseVisualStyleBackColor;
                panel.Controls.Add(button);
                source.Controls.Add(panel);
                int oldCallbacks = 0;
                int newCallbacks = 0;
                FormTheme oldTheme = FormTheme.Attach(source, (control, palette) => oldCallbacks++);
                ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Light;
                FormTheme.Attach(destination, (control, palette) => newCallbacks++);
                destination.Controls.Add(panel);
                Assert.AreEqual(originalStyle, button.FlatStyle);
                Assert.AreEqual(visualStyle, button.UseVisualStyleBackColor);
                Assert.AreEqual(destination.BackColor, panel.BackColor);
                oldTheme.Dispose();

                int captured = oldCallbacks;
                int previous = newCallbacks;
                using (Label late = new Label())
                {
                    panel.Controls.Add(late);
                    Assert.AreEqual(captured, oldCallbacks);
                    Assert.AreEqual(previous + 1, newCallbacks);
                }
                button.BackColor = Color.Red;
                button.Enabled = false;
                Assert.AreEqual(Color.Red, button.BackColor);
            }
        }

        [TestMethod]
        public void Test_Dispose_DetachesGeneratedMenusOutsideTheControlTree() {
            using (Form form = new Form())
            using (ContextMenuStrip menu = new ContextMenuStrip()) {
                ToolStripRenderMode renderMode = menu.RenderMode;
                ToolStripItem item = menu.Items.Add("Custom");
                Color foreground = item.ForeColor;
                FormTheme theme = FormTheme.Attach(form);
                theme.ApplyMenu(menu);
                Assert.AreNotEqual(renderMode, menu.RenderMode);

                theme.Dispose();
                Assert.AreEqual(renderMode, menu.RenderMode);
                Assert.AreEqual(foreground, item.ForeColor);
            }
        }

        private sealed class OwnerDrawComboBox : ComboBox {
            public void Draw(Graphics graphics, Rectangle bounds) {
                this.OnDrawItem(new DrawItemEventArgs(graphics, this.Font, bounds, 0, DrawItemState.None));
            }
        }

        [TestMethod]
        [DataRow(false)]
#if NET10_0_OR_GREATER
        [DataRow(true)]
#endif
        public void Test_ClosedDropDownList_KeepsADarkFaceAndNativeSelection(bool nativeDark) {
            Application.EnableVisualStyles();
#if NET10_0_OR_GREATER
            SystemColorMode original = Application.ColorMode;
            Application.SetColorMode(nativeDark ? SystemColorMode.Dark : SystemColorMode.Classic);
            try {
#endif
                using (Form form = new Form { Opacity = 0, ShowInTaskbar = false })
                using (ComboBox combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 }) {
                    combo.Items.Add("First");
                    combo.Items.Add("Second");
                    combo.SelectedIndex = 0;
                    form.Controls.Add(combo);
                    FormTheme.Attach(form);
                    form.Show();
                    Application.DoEvents();
                    combo.Refresh();
                    using (Bitmap bitmap = new Bitmap(combo.Width, combo.Height))
                    {
                        combo.DrawToBitmap(bitmap, combo.ClientRectangle);
                        Assert.IsTrue(bitmap.GetPixel(combo.Width - 40, combo.Height / 2).GetBrightness() < 0.5f,
                            "The closed face must not retain the native white item surface.");
                        if (nativeDark)
                        {
                            Assert.AreEqual(DrawMode.Normal, combo.DrawMode);
                            Assert.IsTrue(bitmap.GetPixel(combo.Width - 8, 3).GetBrightness() < 0.5f,
                                "The native dark dropdown button must not retain a light background.");
                        }
                    }
                    combo.SelectedIndex = 1;
                    Assert.AreEqual("Second", combo.Text);
                }
#if NET10_0_OR_GREATER
            } finally {
                Application.SetColorMode(original);
            }
#endif
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_FilterChecklist_GlyphsUseTheSharedPaletteWithoutChangingCheckEvents(bool rtl) {
            Application.EnableVisualStyles();
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ControlLightColor), Color.Navy);
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.AccentColor), Color.Maroon);
            using (Form form = new Form())
            using (ContextMenuStrip menu = new ContextMenuStrip())
            {
                var host = new ToolStripCheckedListBox();
                host.AddItem("First", CheckState.Unchecked);
                host.AddItem("Second", CheckState.Checked);
                host.AddItem("Third", CheckState.Indeterminate);
                CheckedListBox list = host.CheckedListBoxControl;
                list.Size = new Size(180, 90);
                list.RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No;
                list.SelectedIndex = -1;
                menu.Items.Add(host);
                FormTheme.Attach(form).ApplyMenu(menu);
                using (Bitmap bitmap = new Bitmap(list.Width, list.Height))
                {
                    list.DrawToBitmap(bitmap, list.ClientRectangle);
                    for (int i = 0; i < 3; i++)
                    {
                        Rectangle row = list.GetItemRectangle(i);
                        Rectangle glyph = new Rectangle(rtl ? list.ClientSize.Width - 20 : 0, row.Top, 20, row.Height);
                        Assert.IsTrue(ContainsColor(bitmap, i == 0 ? Color.Navy : Color.Maroon, glyph),
                            "Filter checklist states must use dark glyphs, including checked and mixed states.");
                    }
                }
                int checks = 0;
                host.ItemCheck += (sender, e) => checks++;
                list.SetItemCheckState(0, CheckState.Checked);
                Assert.AreEqual(1, checks);
                Assert.AreEqual(CheckState.Checked, host.GetItemCheckState(0));
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Test_SubmenuArrows_UseReadableEnabledAndDisabledColors(bool enabled) {
            using (Form form = new Form())
            using (ContextMenuStrip menu = new ContextMenuStrip())
            using (Bitmap bitmap = new Bitmap(24, 24))
            using (Graphics graphics = Graphics.FromImage(bitmap)) {
                var item = new ToolStripMenuItem("Filtering") { Enabled = enabled };
                item.DropDownItems.Add("Apply");
                menu.Items.Add(item);
                form.ContextMenuStrip = menu;
                FormTheme.Attach(form);
                graphics.Clear(Color.Magenta);
                menu.Renderer.DrawArrow(new ToolStripArrowRenderEventArgs(
                    graphics, item, new Rectangle(0, 0, 24, 24), Color.Black, ArrowDirection.Right));
                Assert.IsTrue(ContainsColor(bitmap, enabled ? ObjectListViewTheme.Current.TextColor : ObjectListViewTheme.Current.GrayTextColor));
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_GridScrollbars_UseSharedChromeAndKeepNativeScrolling(bool rtl) {
            Application.EnableVisualStyles();
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ScrollBarTrackColor), Color.Navy);
            using (Form form = new Form { ClientSize = new Size(280, 180), Opacity = 0, ShowInTaskbar = false })
            using (DataGridView grid = new DataGridView { Dock = DockStyle.Fill, RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No }) {
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", Width = 500 });
                for (int i = 0; i < 40; i++)
                {
                    grid.Rows.Add("Row " + i);
                }
                form.Controls.Add(grid);
                FormTheme theme = FormTheme.Attach(form);
                form.Show();
                Application.DoEvents();
                ScrollBar[] bars = grid.Controls.OfType<ScrollBar>().Where(bar => bar.Visible).ToArray();
                Assert.AreEqual(2, bars.Length);
                foreach (ScrollBar bar in bars)
                {
                    using (Bitmap bitmap = new Bitmap(bar.Width, bar.Height))
                    {
                        bar.DrawToBitmap(bitmap, bar.ClientRectangle);
                        Assert.AreEqual(Color.Navy.ToArgb(), bitmap.GetPixel(1, 1).ToArgb());
                    }
                }
                HScrollBar horizontal = bars.OfType<HScrollBar>().Single();
                VScrollBar vertical = bars.OfType<VScrollBar>().Single();
                using (Bitmap bitmap = new Bitmap(grid.Width, grid.Height))
                {
                    grid.DrawToBitmap(bitmap, grid.ClientRectangle);
                    Assert.AreEqual(Color.Navy.ToArgb(),
                        bitmap.GetPixel(vertical.Left + vertical.Width / 2, horizontal.Top + horizontal.Height / 2).ToArgb());
                }
                int firstRow = grid.FirstDisplayedScrollingRowIndex;
                SendMessage(grid.Handle, 0x115, (IntPtr)1, vertical.Handle);
                Assert.IsTrue(grid.FirstDisplayedScrollingRowIndex > firstRow);
                Form[] chrome = form.OwnedForms.Where(window => window.Name == "ObjectListViewChrome").ToArray();
                Assert.AreEqual(2, chrome.Length);
                theme.Dispose();
                Assert.IsTrue(chrome.All(window => window.IsDisposed));
            }
        }

        [TestMethod]
        public void Test_TitleBar_FollowsThePaletteAndHandleRecreationWhenSupported() {
            using (TitleBarForm form = new TitleBarForm())
            {
                FormTheme theme = FormTheme.Attach(form);
                if (DwmGetWindowAttribute(form.Handle, 20, out int dark, sizeof(int)) < 0)
                {
                    Assert.Inconclusive("This Windows version does not support dark title-bar styling.");
                }
                Assert.AreEqual(1, dark);
                theme.Apply(new ThemePalette(false, false));
                Assert.AreEqual(0, DwmGetWindowAttribute(form.Handle, 20, out dark, sizeof(int)));
                Assert.AreEqual(0, dark);
                theme.Apply(new ThemePalette(true, false));
                form.RecreateTestHandle();
                Assert.AreEqual(0, DwmGetWindowAttribute(form.Handle, 20, out dark, sizeof(int)));
                Assert.AreEqual(1, dark);
            }
        }

        private sealed class TitleBarForm : Form {
            public void RecreateTestHandle() => this.RecreateHandle();
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out int value, int size);

        [TestMethod]
        public void Test_DropdownMenu_SeparatesTheIconGutterFromTheItemSurface() {
            using (Form form = new Form())
            using (ContextMenuStrip menu = new ContextMenuStrip())
            {
                menu.Items.Add(new ToolStripMenuItem("Select packages"));
                form.ContextMenuStrip = menu;
                FormTheme.Attach(form);
                menu.PerformLayout();
                using (Bitmap bitmap = new Bitmap(menu.Width, menu.Height))
                {
                    menu.DrawToBitmap(bitmap, new Rectangle(Point.Empty, menu.Size));
                    Assert.AreEqual(ObjectListViewTheme.Current.ControlLightColor.ToArgb(), bitmap.GetPixel(10, 10).ToArgb());
                    Assert.AreEqual(ObjectListViewTheme.Current.MenuColor.ToArgb(), bitmap.GetPixel(menu.Width - 12, 10).ToArgb());
                    Assert.AreNotEqual(bitmap.GetPixel(10, 10).ToArgb(), bitmap.GetPixel(menu.Width - 12, 10).ToArgb());
                }
            }
        }

        [TestMethod]
        public void Test_MenuBar_UsesTheLighterMenuSurfaceRatherThanTheWindowBackground() {
            using (Form form = new Form())
            using (MenuStrip menu = new MenuStrip { Size = new Size(300, 24), AutoSize = false })
            using (Bitmap bitmap = new Bitmap(300, 24)) {
                menu.Items.Add(new ToolStripMenuItem("File"));
                form.Controls.Add(menu);
                FormTheme.Attach(form);
                menu.DrawToBitmap(bitmap, new Rectangle(Point.Empty, menu.Size));

                Assert.AreEqual(ObjectListViewTheme.Current.MenuColor.ToArgb(), bitmap.GetPixel(200, 10).ToArgb());
                Assert.AreNotEqual(ObjectListViewTheme.Current.ControlColor.ToArgb(), bitmap.GetPixel(200, 10).ToArgb());
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_RichTextBox_UsesSharedCompositedScrollbarsWithoutChangingTextOrScrolling(bool removeBeforeDisposal) {
            Application.EnableVisualStyles();
            using (Form form = new Form { Opacity = 0, ShowInTaskbar = false })
            using (RichTextBox text = new RichTextBox {
                ReadOnly = true, BorderStyle = BorderStyle.None, ScrollBars = RichTextBoxScrollBars.Vertical,
                Size = new Size(240, 150), Text = string.Join(Environment.NewLine, Enumerable.Range(0, 100).Select(i => "Driver detail " + i))
            })
            using (Bitmap bitmap = new Bitmap(240, 150)) {
                form.Controls.Add(text);
                _ = text.Handle;
                string original = text.Text;
                FormTheme theme = FormTheme.Attach(form);
                form.Show();
                text.Select(0, 0);
                text.ScrollToCaret();
                Application.DoEvents();
                text.Refresh();
                Assert.AreEqual(1, form.OwnedForms.Count(window => window.Name == "ObjectListViewChrome"));
                text.DrawToBitmap(bitmap, new Rectangle(Point.Empty, text.Size));
                Assert.IsTrue(bitmap.GetPixel(232, 75).GetBrightness() < 0.8f,
                    "The details scrollbar should use the same dark composition as the list.");
                int firstLine = SendMessage(text.Handle, 0xCE, IntPtr.Zero, IntPtr.Zero).ToInt32();
                SendMessage(text.Handle, 0x115, (IntPtr)1, IntPtr.Zero);
                Assert.IsTrue(SendMessage(text.Handle, 0xCE, IntPtr.Zero, IntPtr.Zero).ToInt32() > firstLine);
                Assert.AreEqual(original, text.Text);
                Assert.IsTrue(text.ReadOnly);
                Form chrome = form.OwnedForms.Single(window => window.Name == "ObjectListViewChrome");
                if (removeBeforeDisposal)
                {
                    form.Controls.Remove(text);
                    Assert.IsTrue(chrome.IsDisposed);
                }
                theme.Dispose();
                Assert.IsTrue(chrome.IsDisposed);
            }
        }

        [DllImport("user32.dll", EntryPoint = "SendMessageW", ExactSpelling = true)]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        [TestMethod]
        public void Test_StatusStripDivider_UsesTheMutedBorderColor() {
            using (Form form = new Form())
            using (StatusStrip status = new StatusStrip { Size = new Size(300, 24), AutoSize = false })
            using (Bitmap bitmap = new Bitmap(300, 24)) {
                status.Items.Add(new ToolStripStatusLabel("Ready"));
                form.Controls.Add(status);
                FormTheme.Attach(form);
                status.DrawToBitmap(bitmap, new Rectangle(Point.Empty, status.Size));

                Assert.AreEqual(ObjectListViewTheme.Current.ControlDarkColor.ToArgb(), bitmap.GetPixel(150, 0).ToArgb());
            }
        }

        [TestMethod]
        public void Test_GroupBoxBorder_UsesTheMutedPaletteInsteadOfTheNativeLightOutline() {
            Application.EnableVisualStyles();
            using (Form form = new Form())
            using (GroupBox group = new GroupBox { Text = "Operations", Size = new Size(240, 100) })
            using (Bitmap bitmap = new Bitmap(240, 100)) {
                form.Controls.Add(group);
                FormTheme.Attach(form);
                group.DrawToBitmap(bitmap, group.ClientRectangle);
                Assert.AreEqual(ObjectListViewTheme.Current.ControlDarkColor.ToArgb(), bitmap.GetPixel(0, 40).ToArgb());
                Assert.AreEqual(ObjectListViewTheme.Current.ControlDarkColor.ToArgb(), bitmap.GetPixel(239, 40).ToArgb());
                Assert.AreEqual(ObjectListViewTheme.Current.ControlDarkColor.ToArgb(), bitmap.GetPixel(100, 99).ToArgb());
            }
        }

        [TestMethod]
        public void Test_PartialPaletteAndCallback_AffectRenderedControlsWithoutReplacingExplicitStyles() {
            Application.EnableVisualStyles();
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.WindowColor), Color.Navy);
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ControlColor), Color.DarkSlateGray);
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.TextColor), Color.Ivory);
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ButtonColor), Color.Maroon);
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.MenuColor), Color.DimGray);
            using (Form form = new Form { ClientSize = new Size(420, 260), Opacity = 0, ShowInTaskbar = false })
            using (ObjectListView list = new ObjectListView { View = View.Details, ShowGroups = false, Size = new Size(300, 150) })
            using (Button button = new Button { Text = "Action", Top = 160 })
            using (Label explicitLabel = new Label { ForeColor = Color.Purple, Top = 190 })
            using (Label customLabel = new Label { Name = "custom", Top = 215 })
            using (Bitmap bitmap = new Bitmap(300, 150)) {
                list.Columns.Add(new OLVColumn("Name", "Name") { Width = 180 });
                list.SetObjects(new[] { new { Name = "Row" } });
                form.Controls.AddRange(new Control[] { list, button, explicitLabel, customLabel });
                bool sawStandardStyling = false;
                FormTheme.Attach(form, (control, palette) => {
                    if (control.Name == "custom") {
                        sawStandardStyling = control.ForeColor == palette.TextColor;
                        control.ForeColor = Color.Lime;
                    }
                });
                form.Show();
                Application.DoEvents();

                Assert.IsTrue(sawStandardStyling);
                Assert.AreEqual(Color.Lime, customLabel.ForeColor);
                Assert.AreEqual(Color.Purple, explicitLabel.ForeColor);
                Assert.AreEqual(Color.Maroon, button.BackColor);
                list.DrawToBitmap(bitmap, list.ClientRectangle);
                Assert.AreEqual(Color.DarkSlateGray.ToArgb(), bitmap.GetPixel(100, 5).ToArgb());
                Assert.AreEqual(Color.Navy.ToArgb(), bitmap.GetPixel(250, 100).ToArgb());

                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ButtonColor), Color.Empty);
                FormTheme.RefreshAll();
                Assert.AreEqual(ObjectListViewTheme.Current.ButtonColor, button.BackColor);
                Assert.AreEqual(Color.Navy, list.BackColor);

                ObjectListViewTheme.ResetColors();
                FormTheme.RefreshAll();
                Assert.AreEqual(ObjectListViewTheme.Current.ButtonColor, button.BackColor);
                Assert.AreEqual(ObjectListViewTheme.Current.WindowColor.ToArgb(), list.GetItem(0).BackColor.ToArgb());
                Assert.AreEqual(Color.Purple, explicitLabel.ForeColor);
                Assert.AreEqual(Color.Lime, customLabel.ForeColor);
            }
        }

        [TestMethod]
        public void Test_LightForm_IgnoresDarkOverridesAndRetainsNativeControlStyling() {
            ObjectListViewTheme.ColorMode = ObjectListViewColorMode.Light;
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ButtonColor), Color.Maroon);
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.WindowColor), Color.Navy);
            using (Form form = new Form())
            using (Button button = new Button())
            using (ListBox list = new ListBox()) {
                FlatStyle originalStyle = button.FlatStyle;
                bool visualStyle = button.UseVisualStyleBackColor;
                Color originalBackground = button.BackColor;
                form.Controls.AddRange(new Control[] { button, list });
                FormTheme theme = FormTheme.Attach(form);
                Assert.AreEqual(originalBackground, button.BackColor);
                Assert.AreEqual(originalStyle, button.FlatStyle);
                Assert.AreEqual(visualStyle, button.UseVisualStyleBackColor);
                Assert.AreEqual(SystemColors.Window.ToArgb(), list.BackColor.ToArgb());

                theme.Apply(new ThemePalette(true, false));
                Assert.AreEqual(Color.Maroon, button.BackColor);
                Assert.AreEqual(FlatStyle.Flat, button.FlatStyle);
                Assert.AreEqual(Color.Navy, list.BackColor);

                theme.Apply(new ThemePalette(false, false));
                Assert.AreEqual(originalBackground, button.BackColor);
                Assert.AreEqual(originalStyle, button.FlatStyle);
                Assert.AreEqual(visualStyle, button.UseVisualStyleBackColor);
                Assert.AreEqual(SystemColors.Window.ToArgb(), list.BackColor.ToArgb());
                Assert.AreEqual(Color.Maroon, new ThemePalette(true, false).ButtonColor);
            }
        }

        [TestMethod]
        public void Test_Callbacks_RunAfterChildrenAndApplyToLateControls() {
            using (Form form = new Form())
            using (Panel panel = new Panel { Name = "panel" })
            using (TextBox text = new TextBox { Name = "text" }) {
                panel.Controls.Add(text);
                form.Controls.Add(panel);
                List<string> order = new List<string>();
                int completed = 0;
                FormTheme theme = FormTheme.Attach(form, (control, palette) => {
                    order.Add(control.Name);
                    if (control is TextBox) {
                        Assert.AreEqual(palette.InputColor, control.BackColor);
                        control.ForeColor = Color.Lime;
                    }
                }, palette => completed++);

                Assert.IsTrue(order.IndexOf("text") < order.IndexOf("panel"));
                Assert.AreEqual(1, completed);
                TextBox late = new TextBox { Name = "late" };
                panel.Controls.Add(late);
                Assert.AreEqual(Color.Lime, late.ForeColor);
                Assert.AreEqual(ObjectListViewTheme.Current.ControlLightColor, late.BackColor);

                theme.Dispose();
                Label untouched = new Label { ForeColor = Color.Purple };
                panel.Controls.Add(untouched);
                Assert.AreEqual(Color.Purple, untouched.ForeColor);
            }
        }

        [TestMethod]
        public void Test_ThemeAware_RunsAfterChildrenBeforeCallbacksAndReceivesRefreshes() {
            using (Form form = new Form())
            using (ThemeAwarePanel panel = new ThemeAwarePanel())
            using (Label child = new Label()) {
                panel.Controls.Add(child);
                form.Controls.Add(panel);
                int callbacks = 0;
                int completed = 0;
                FormTheme theme = FormTheme.Attach(form, (control, palette) => {
                    if (control == panel) {
                        Assert.AreSame(palette, panel.LastPalette);
                        Assert.AreEqual(palette.TextColor, panel.StandardForeColor);
                        Assert.AreEqual(palette.TextColor, child.ForeColor);
                        Assert.AreEqual(palette.LinkColor, panel.ForeColor);
                        panel.ForeColor = Color.Lime;
                        callbacks++;
                    }
                }, palette => {
                    Assert.AreEqual(Color.Lime, panel.ForeColor);
                    completed++;
                });
                Assert.AreEqual(1, panel.ApplyCount);
                Assert.AreEqual(1, callbacks);
                Assert.AreEqual(1, completed);

                ThemePalette highContrast = new ThemePalette(true, true);
                theme.Apply(highContrast);
                Assert.AreSame(highContrast, panel.LastPalette);
                Assert.AreEqual(2, panel.ApplyCount);
                Assert.AreEqual(2, callbacks);
                Assert.AreEqual(2, completed);
            }
        }

        [TestMethod]
        public void Test_ThemeAware_AppliesToStandaloneAndLateControlsWithoutACallback() {
            using (Panel root = new Panel())
            using (ThemeAwarePanel child = new ThemeAwarePanel())
            using (ThemeAwarePanel late = new ThemeAwarePanel()) {
                root.Controls.Add(child);
                FormTheme theme = FormTheme.Apply(root);
                Assert.AreEqual(1, child.ApplyCount);
                Assert.AreEqual(child.LastPalette.LinkColor, child.ForeColor);

                root.Controls.Add(late);
                Assert.AreEqual(1, late.ApplyCount);
                Assert.AreSame(child.LastPalette, late.LastPalette);
                Assert.AreEqual(late.LastPalette.LinkColor, late.ForeColor);

                theme.Dispose();
                root.Controls.Remove(late);
                root.Controls.Add(late);
                Assert.AreEqual(1, late.ApplyCount);
            }
        }

        private sealed class ThemeAwarePanel : Panel, IThemeAware {
            internal int ApplyCount { get; private set; }
            internal IThemeColors LastPalette { get; private set; }
            internal Color StandardForeColor { get; private set; }

            public void ApplyTheme(IThemeColors palette) {
                this.ApplyCount++;
                this.LastPalette = palette;
                this.StandardForeColor = this.ForeColor;
                foreach (Control child in this.Controls) {
                    Assert.AreEqual(palette.TextColor, child.ForeColor);
                }
                this.ForeColor = palette.LinkColor;
            }
        }

        [TestMethod]
        public void Test_HighContrastSnapshot_IgnoresCustomPaletteAndExplicitControlColors() {
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.WindowColor), Color.Red);
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.TextColor), Color.Lime);
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ButtonColor), Color.Purple);
            using (Form form = new Form())
            using (TextBox text = new TextBox { BackColor = Color.Red, ForeColor = Color.Lime }) {
                form.Controls.Add(text);
                FormTheme theme = FormTheme.Attach(form);
                theme.Apply(new ThemePalette(true, true));

                Assert.AreEqual(SystemColors.Control.ToArgb(), form.BackColor.ToArgb());
                Assert.AreEqual(SystemColors.Window.ToArgb(), text.BackColor.ToArgb());
                Assert.AreEqual(SystemColors.WindowText.ToArgb(), text.ForeColor.ToArgb());
            }
        }

        [TestMethod]
        public void Test_Button_DistinguishesDisabledStateAndRestoresWhenEnabled() {
            using (Form form = new Form())
            using (Button button = new Button { Text = "Driver action", Size = new Size(180, 40) })
            using (Bitmap bitmap = new Bitmap(180, 40)) {
                form.Controls.Add(button);
                Color originalBackground = button.BackColor;
                FormTheme theme = FormTheme.Attach(form);
                button.DrawToBitmap(bitmap, button.ClientRectangle);
                Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.Current.ButtonTextColor));
                Color enabledBackground = bitmap.GetPixel(10, 10);

                button.Enabled = false;
                button.DrawToBitmap(bitmap, button.ClientRectangle);
                Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.Current.ButtonDisabledTextColor));
                Assert.IsFalse(ContainsColor(bitmap, ObjectListViewTheme.Current.ButtonTextColor));
                Assert.AreEqual(ObjectListViewTheme.Current.ButtonDisabledColor.ToArgb(), bitmap.GetPixel(10, 10).ToArgb());
                Assert.IsTrue(enabledBackground.GetBrightness() - bitmap.GetPixel(10, 10).GetBrightness() >= 0.05f,
                    "Disabled button backgrounds should be visibly darker than enabled backgrounds.");
                Assert.IsTrue(ObjectListViewTheme.Current.ButtonTextColor.GetBrightness()
                    - ObjectListViewTheme.Current.ButtonDisabledTextColor.GetBrightness() >= 0.25f,
                    "Disabled captions must be visibly dimmer, not nearly as bright as enabled captions.");

                button.Enabled = true;
                button.DrawToBitmap(bitmap, button.ClientRectangle);
                Assert.IsTrue(ContainsColor(bitmap, ObjectListViewTheme.Current.ButtonTextColor));
                Assert.IsFalse(ContainsColor(bitmap, ObjectListViewTheme.Current.ButtonDisabledTextColor));
                Assert.AreEqual(enabledBackground.ToArgb(), bitmap.GetPixel(10, 10).ToArgb());

                theme.Apply(new ThemePalette(false, false));
                Assert.AreEqual(originalBackground, button.BackColor);
                button.Enabled = false;
                Assert.AreEqual(originalBackground, button.BackColor);
                theme.Apply(new ThemePalette(true, true));
                Assert.AreEqual(SystemColors.Control.ToArgb(), button.BackColor.ToArgb());
            }
        }

        [TestMethod]
        public void Test_DisabledButtonBackground_PreservesExplicitColorsAndSupportsOverrides() {
            ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ButtonDisabledColor), Color.Navy);
            using (Form form = new Form())
            using (Button button = new Button { Enabled = false })
            using (Button explicitButton = new Button { Enabled = false, BackColor = Color.Maroon }) {
                form.Controls.AddRange(new Control[] { button, explicitButton });
                FormTheme theme = FormTheme.Attach(form);
                Assert.AreEqual(Color.Navy, button.BackColor);
                Assert.AreEqual(Color.Maroon, explicitButton.BackColor);
                explicitButton.Enabled = true;
                explicitButton.Enabled = false;
                Assert.AreEqual(Color.Maroon, explicitButton.BackColor);

                ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ButtonDisabledColor), Color.Empty);
                theme.Apply(new ThemePalette(true, false));
                Assert.AreEqual(ObjectListViewTheme.Current.ButtonDisabledColor, button.BackColor);
                theme.Apply(new ThemePalette(true, true));
                Assert.AreEqual(SystemColors.Control.ToArgb(), explicitButton.BackColor.ToArgb());
            }
        }

        [TestMethod]
        public void Test_DisabledImageButtons_DoNotLoseTheirImageToCaptionPainting() {
            using (Form form = new Form())
            using (Bitmap image = new Bitmap(12, 12))
            using (Button button = new Button {
                Text = "Action", Enabled = false, Size = new Size(140, 40),
                Image = image, ImageAlign = ContentAlignment.MiddleLeft, TextAlign = ContentAlignment.MiddleRight
            })
            using (Bitmap withImage = new Bitmap(140, 40))
            using (Bitmap withoutImage = new Bitmap(140, 40)) {
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    graphics.Clear(Color.Magenta);
                }
                form.Controls.Add(button);
                FormTheme.Attach(form);
                button.DrawToBitmap(withImage, button.ClientRectangle);
                button.Image = null;
                button.DrawToBitmap(withoutImage, button.ClientRectangle);

                bool imageVisible = false;
                for (int y = 10; y < 30; y++) {
                    for (int x = 3; x < 20; x++) {
                        imageVisible |= withImage.GetPixel(x, y).ToArgb() != withoutImage.GetPixel(x, y).ToArgb();
                    }
                }
                Assert.IsTrue(imageVisible, "Disabled text styling must not erase the native image layout.");
            }
        }
    }
}
