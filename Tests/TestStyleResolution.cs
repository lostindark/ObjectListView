using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#if NETFRAMEWORK
using System.CodeDom;
using System.CodeDom.Compiler;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.IO;
using Microsoft.CSharp;
#endif

namespace BrightIdeasSoftware.Tests {
    [STATestClass]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "TestCleanup disposes the fixture form and its owned controls.")]
    public class TestStyleResolution {
        private static readonly string[] Rows = { "one", "two" };
        private static readonly string[] TileRows = { "tile" };
        private Form form;
        private StyleList list;

        [TestInitialize]
        public void SetUp() {
            form = new Form { ClientSize = new Size(420, 220), Opacity = 0, ShowInTaskbar = false };
            list = new StyleList {
                View = View.Details, ShowGroups = false, OwnerDraw = true,
                FullRowSelect = true, Dock = DockStyle.Fill
            };
            foreach (string title in new[] { "First", "Second" }) {
                OLVColumn column = new OLVColumn(title, "") { AspectGetter = x => x, Width = 180 };
                list.AllColumns.Add(column);
                list.Columns.Add(column);
            }
            form.Controls.Add(list);
            form.Show();
            list.SetObjects(Rows);
        }

        [TestCleanup]
        public void TearDown() {
            form.Dispose();
        }

        [TestMethod]
        public void NormalValuesInheritPerPropertyAndFormattingCallbacksWin() {
            list.RowAppearance.Normal.ForeColor = Color.Red;
            list.RowAppearance.Normal.BackColor = Color.Beige;
            list.GetColumn(1).CellAppearance.Normal.ForeColor = Color.Blue;
            list.GetColumn(1).CellAppearance.Normal.Padding = new Rectangle(2, 3, 4, 5);
            list.RebuildColumns();
            OLVListItem item = list.GetItem(0);
            Assert.AreEqual(Color.Red, item.GetSubItem(0).ForeColor);
            Assert.AreEqual(Color.Blue, item.GetSubItem(1).ForeColor);
            Assert.AreEqual(Color.Beige, item.GetSubItem(1).BackColor);
            Assert.AreEqual(new Rectangle(2, 3, 4, 5), item.GetSubItem(1).CellPadding);

            list.FormatRow += (sender, e) => e.Item.ForeColor = Color.Green;
            list.UseCellFormatEvents = true;
            list.FormatCell += (sender, e) => {
                if (e.ColumnIndex == 1)
                    e.SubItem.ForeColor = Color.Purple;
            };
            list.RebuildColumns();
            item = list.GetItem(0);
            Assert.AreEqual(Color.Green, item.GetSubItem(0).ForeColor);
            Assert.AreEqual(Color.Purple, item.GetSubItem(1).ForeColor);
            Assert.AreEqual(Color.Beige, item.GetSubItem(1).BackColor);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FormatRowKeepsExplicitCellFormattingWithAndWithoutSharedAppearance(bool sharedAppearance) {
            if (sharedAppearance) {
                list.RowAppearance.Normal.ForeColor = Color.Red;
                list.GetColumn(1).CellAppearance.Normal.ForeColor = Color.Blue;
            }
            list.FormatRow += (sender, e) => {
                e.Item.UseItemStyleForSubItems = false;
                e.Item.ForeColor = Color.Green;
                e.Item.BackColor = Color.Beige;
                e.Item.SubItems[1].ForeColor = Color.Purple;
                e.Item.SubItems[1].BackColor = Color.Gold;
            };
            list.RebuildColumns();
            OLVListItem item = list.GetItem(0);
            Assert.AreEqual(Color.Green, item.GetSubItem(0).ForeColor);
            Assert.AreEqual(Color.Purple, item.GetSubItem(1).ForeColor);
            Assert.AreEqual(Color.Gold, item.GetSubItem(1).BackColor);
        }

        [TestMethod]
        public void FormatRowExplicitSameValueOverridesColumnsButTypedCellAssignmentsStillWin() {
            list.RowAppearance.Normal.ForeColor = Color.Red;
            list.GetColumn(1).CellAppearance.Normal.ForeColor = Color.Blue;
            list.FormatRow += (sender, e) => e.Item.ForeColor = Color.Red;
            list.RebuildColumns();
            Assert.AreEqual(Color.Red, list.GetItem(0).GetSubItem(1).ForeColor);

            list.FormatRow += (sender, e) => {
                e.Item.ForeColor = Color.Green;
                e.Item.GetSubItem(0).ForeColor = Color.Purple;
                e.Item.GetSubItem(1).ForeColor = Color.Blue;
            };
            list.RebuildColumns();
            Assert.AreEqual(Color.Purple, list.GetItem(0).GetSubItem(0).ForeColor);
            Assert.AreEqual(Color.Blue, list.GetItem(0).GetSubItem(1).ForeColor);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void NormalAppearanceIsAppliedToRegularAndVirtualRows(bool virtualMode) {
            using (ObjectListViewBase control = virtualMode ?
                (ObjectListViewBase)new FastObjectListView() : new ObjectListView()) {
                control.View = View.Details;
                control.ShowGroups = false;
                OLVColumn column = new OLVColumn("Value", "") { AspectGetter = x => x, Width = 100 };
                control.Columns.Add(column);
                control.AllColumns.Add(column);
                control.RowAppearance.Normal.BackColor = Color.Beige;
                column.CellAppearance.Normal.ForeColor = Color.Blue;
                control.ModelObjects = new object[] { "value" };
                form.Controls.Add(control);
                Assert.AreEqual(Color.Blue, control.GetItem(0).GetSubItem(0).ForeColor);
                Assert.AreEqual(Color.Beige, control.GetItem(0).GetSubItem(0).BackColor);
            }
        }

        [TestMethod]
        public void TileAppearanceUsesTheActualTileColumnsRatherThanSubItemIndexes() {
            OLVColumn hiddenTileColumn = new OLVColumn("Not a tile", "") { AspectGetter = x => x, Width = 60 };
            hiddenTileColumn.CellAppearance.Normal.ForeColor = Color.Red;
            list.Columns.Insert(1, hiddenTileColumn);
            list.AllColumns.Insert(1, hiddenTileColumn);
            list.GetColumn(2).IsTileViewColumn = true;
            list.GetColumn(2).CellAppearance.Normal.ForeColor = Color.Blue;
            list.View = View.Tile;
            list.SetObjects(TileRows);
            OLVListItem item = list.GetItem(0);
            Assert.AreEqual(2, item.SubItems.Count);
            Assert.AreEqual(Color.Blue, item.GetSubItem(1).ForeColor);
        }

        [TestMethod]
        [DataRow(0, false)]
        [DataRow(1, false)]
        [DataRow(3, true)]
        [DataRow(7, true)]
        [DataRow(19, false)]
        public void CombinedStateRulesRequireAllFlagsAndHonorExclusions(int flags, bool matches) {
            StyleRule rule = Rule(StyleState.Selected | StyleState.Focused, Color.Coral);
            rule.ExcludedState = StyleState.Disabled;
            list.RowAppearance.States.Add(rule);
            StyleValues defaults = new StyleValues { ForeColor = Color.Black, BackColor = Color.White };
            StyleValues result = list.ResolveStyle(StyleTarget.Cell, list.GetColumn(0), list.GetItem(0),
                Rectangle.Empty, (StyleState)flags, defaults);
            Assert.AreEqual(matches ? Color.Coral : Color.Black, result.ForeColor);
            Assert.AreEqual(Color.White, result.BackColor);
            Assert.AreEqual(Color.Black, defaults.ForeColor);
            Assert.AreEqual(Color.Coral, rule.Values.ForeColor);
        }

        [TestMethod]
        public void ColumnRowCellAndFinalCallbackOverrideOnlySpecifiedStateProperties() {
            OLVListItem item = list.GetItem(0);
            list.RowAppearance.States.Add(Rule(StyleState.Selected, Color.Red));
            list.RowAppearance.States[0].Values.BackColor = Color.Beige;
            list.GetColumn(1).CellAppearance.States.Add(Rule(StyleState.Selected, Color.Blue));
            item.Appearance.States.Add(Rule(StyleState.Selected, Color.Green));
            item.GetSubItem(1).Appearance.States.Add(Rule(StyleState.Selected, Color.Purple));
            Rectangle bounds = new Rectangle(3, 4, 100, 25);
            int calls = 0;
            list.StyleResolving += (sender, e) => {
                calls++;
                Assert.AreEqual(Color.Purple, e.Values.ForeColor);
                Assert.AreSame(item.RowObject, e.Context.Model);
                Assert.AreSame(list.GetColumn(1), e.Context.Column);
                Assert.AreEqual(bounds, e.Context.Bounds);
                Assert.AreEqual(StyleState.Selected, e.Context.State);
                Assert.AreEqual(StyleTarget.Cell, e.Context.Target);
                Assert.AreEqual(list.RightToLeft, e.Context.RightToLeft);
                Assert.IsGreaterThan(0, e.Context.Dpi);
                e.Values.BackColor = Color.Gold;
            };
            StyleValues result = list.ResolveStyle(StyleTarget.Cell, list.GetColumn(1), item, bounds,
                StyleState.Selected, new StyleValues());
            Assert.AreEqual(1, calls);
            Assert.AreEqual(Color.Purple, result.ForeColor);
            Assert.AreEqual(Color.Gold, result.BackColor);
            Assert.AreEqual(Color.Beige, list.RowAppearance.States[0].Values.BackColor);
        }

        [TestMethod]
        public void LaterRulesCanExplicitlyResetRegularZeroAndTransparentValues() {
            StyleRule first = Rule(StyleState.Hot, Color.Red);
            first.Values.FontStyle = FontStyle.Bold;
            first.Values.BorderWidth = 3;
            first.Values.Padding = new Rectangle(4, 4, 4, 4);
            list.RowAppearance.States.Add(first);
            StyleRule reset = Rule(StyleState.Hot | StyleState.Pressed, Color.Transparent);
            reset.Values.FontStyle = FontStyle.Regular;
            reset.Values.BorderWidth = 0;
            reset.Values.Padding = Rectangle.Empty;
            list.RowAppearance.States.Add(reset);
            StyleValues result = list.ResolveStyle(StyleTarget.Cell, list.GetColumn(0), list.GetItem(0),
                Rectangle.Empty, StyleState.Hot | StyleState.Pressed, new StyleValues { Font = list.Font });
            Assert.AreEqual(FontStyle.Regular, result.Font.Style);
            Assert.AreEqual(0f, result.BorderWidth);
            Assert.AreEqual(Rectangle.Empty, result.Padding);
            Assert.AreEqual(Color.Transparent, result.ForeColor);
        }

        [TestMethod]
        public void RendererUsesLiveStateRulesWithoutMutatingRowsAndClearsCachedState() {
            list.RowAppearance.States.Add(Rule(StyleState.Selected, Color.Magenta));
            list.RowAppearance.States[0].Values.BackColor = Color.Navy;
            list.RowAppearance.States[0].Values.Padding = new Rectangle(5, 6, 7, 8);
            list.RowAppearance.States[0].Values.HorizontalAlignment = HorizontalAlignment.Right;
            list.RowAppearance.States[0].Values.VerticalAlignment = StringAlignment.Far;
            list.RowAppearance.States[0].Values.BorderColor = Color.Yellow;
            list.RowAppearance.States[0].Values.BorderWidth = 2;
            OLVListItem item = list.GetItem(0);
            Color original = item.ForeColor;
            item.Selected = true;
            BaseRenderer renderer = new BaseRenderer();
            using (Bitmap bitmap = new Bitmap(180, 40))
            using (Graphics graphics = Graphics.FromImage(bitmap)) {
                Configure(renderer, graphics, item, 0, bitmap.Size);
                Assert.AreEqual(Color.Magenta, renderer.GetForegroundColor());
                Assert.AreEqual(Color.Navy, renderer.GetBackgroundColor());
                Assert.AreEqual(HorizontalAlignment.Right, renderer.CellHorizontalAlignment);
                Assert.AreEqual(new Rectangle(5, 6, 168, 26),
                    renderer.ApplyCellPadding(new Rectangle(Point.Empty, bitmap.Size)));
                renderer.Render(graphics, new Rectangle(Point.Empty, bitmap.Size));
                Assert.AreEqual(Color.Yellow.ToArgb(), bitmap.GetPixel(1, 15).ToArgb());
                Assert.AreEqual(original, item.ForeColor);
                item.Selected = false;
                Configure(renderer, graphics, item, 0, bitmap.Size);
                Assert.AreEqual(original, renderer.GetForegroundColor());
            }
        }

        [TestMethod]
        public void PressedStyleEndsOnCaptureLossWithoutDependingOnGlobalMouseButtons() {
            list.RowAppearance.States.Add(Rule(StyleState.Hot, Color.Red));
            list.RowAppearance.States.Add(Rule(StyleState.Hot | StyleState.Pressed, Color.Blue));
            OLVListItem item = list.GetItem(0);
            Point location = new Point(item.Bounds.Left + 20, item.Bounds.Top + item.Bounds.Height / 2);
            list.Press(location);
            BaseRenderer renderer = new BaseRenderer();
            using (Bitmap bitmap = new Bitmap(180, 40))
            using (Graphics graphics = Graphics.FromImage(bitmap)) {
                Configure(renderer, graphics, item, 0, bitmap.Size);
                Assert.AreEqual(Color.Blue, renderer.GetForegroundColor());
                list.LoseCapture();
                Configure(renderer, graphics, item, 0, bitmap.Size);
                Assert.AreEqual(Color.Red, renderer.GetForegroundColor());
            }
        }

        [TestMethod]
        public void RendererRechecksEmptyStylesAndCallbacksForEachConfiguredCell() {
            OLVListItem item = list.GetItem(0);
            using (BaseRenderer renderer = new BaseRenderer())
            using (Bitmap bitmap = new Bitmap(180, 40))
            using (Graphics graphics = Graphics.FromImage(bitmap)) {
                Configure(renderer, graphics, item, 0, bitmap.Size);
                Assert.AreEqual(item.ForeColor, renderer.GetForegroundColor());
                list.RowAppearance.States.Add(Rule(StyleState.Normal, Color.Magenta));
                Configure(renderer, graphics, item, 0, bitmap.Size);
                Assert.AreEqual(Color.Magenta, renderer.GetForegroundColor());
                list.RowAppearance.States.Clear();
                int calls = 0;
                EventHandler<StyleResolvingEventArgs> callback = (sender, e) => {
                    calls++;
                    e.Values.ForeColor = Color.Blue;
                };
                list.StyleResolving += callback;
                Configure(renderer, graphics, item, 0, bitmap.Size);
                Assert.AreEqual(Color.Blue, renderer.GetForegroundColor());
                _ = renderer.Font;
                _ = renderer.GetBackgroundColor();
                Assert.AreEqual(1, calls);
                list.StyleResolving -= callback;
                Configure(renderer, graphics, item, 0, bitmap.Size);
                Assert.AreEqual(item.ForeColor, renderer.GetForegroundColor());
            }
        }

        [TestMethod]
        [DataRow(StyleTarget.Cell, StyleState.Normal, true, KnownColor.WindowText, KnownColor.Window)]
        [DataRow(StyleTarget.Cell, StyleState.Selected, true, KnownColor.WindowText, KnownColor.Window)]
        [DataRow(StyleTarget.Cell, StyleState.Selected, false, KnownColor.HighlightText, KnownColor.Highlight)]
        [DataRow(StyleTarget.Cell, StyleState.Selected | StyleState.Focused, true, KnownColor.HighlightText, KnownColor.Highlight)]
        [DataRow(StyleTarget.Cell, StyleState.Selected | StyleState.Disabled, false, KnownColor.GrayText, KnownColor.Highlight)]
        [DataRow(StyleTarget.Header, StyleState.Disabled | StyleState.Hot, true, KnownColor.GrayText, KnownColor.Control)]
        [DataRow(StyleTarget.ToolTip, StyleState.Selected | StyleState.Disabled, false, KnownColor.InfoText, KnownColor.Info)]
        public void HighContrastOverridesApplicationColorsWithoutChangingGeometry(
            StyleTarget target, StyleState state, bool hideSelection, KnownColor expectedFore, KnownColor expectedBack) {
            StyleValues values = new StyleValues {
                ForeColor = Color.Red, BackColor = Color.Blue, BorderColor = Color.Yellow,
                Padding = new Rectangle(2, 3, 4, 5), BorderWidth = 2
            };
            typeof(ObjectListViewBase).GetMethod("ApplyHighContrastColors", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { values, target, state, hideSelection });
            Assert.AreEqual(Color.FromKnownColor(expectedFore), values.ForeColor);
            Assert.AreEqual(Color.FromKnownColor(expectedBack), values.BackColor);
            Assert.AreEqual(SystemColors.WindowText, values.BorderColor);
            Assert.AreEqual(new Rectangle(2, 3, 4, 5), values.Padding);
            Assert.AreEqual(2f, values.BorderWidth);
        }

        [TestMethod]
        public void LegacyHeaderPropertiesInheritAcrossStatesAndColumnsIncludingExplicitZero() {
            list.HeaderUsesThemes = false;
            list.HeaderFormatStyle = new HeaderFormatStyle();
            list.HeaderFormatStyle.Normal.ForeColor = Color.Red;
            list.HeaderFormatStyle.Normal.BackColor = Color.Beige;
            list.HeaderFormatStyle.Normal.FrameColor = Color.Black;
            list.HeaderFormatStyle.Normal.FrameWidth = 3;
            list.HeaderFormatStyle.Hot.ForeColor = Color.Orange;
            OLVColumn column = list.GetColumn(1);
            column.HeaderFormatStyle = new HeaderFormatStyle();
            column.HeaderFormatStyle.Normal.ForeColor = Color.Blue;
            column.HeaderFormatStyle.Pressed.FrameWidth = 0;
            HeaderProbe header = new HeaderProbe(list);
            HeaderStateStyle style = header.Resolve(column, true, true);
            Assert.AreEqual(Color.Blue, style.ForeColor);
            Assert.AreEqual(Color.Beige, style.BackColor);
            Assert.AreEqual(Color.Black, style.FrameColor);
            Assert.AreEqual(0f, style.FrameWidth);
            column.HeaderFormatStyle.Pressed.ResetFrameWidth();
            Assert.AreEqual(3f, header.Resolve(column, true, true).FrameWidth);
            Assert.AreEqual(Color.Orange, list.HeaderFormatStyle.Hot.ForeColor);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SharedHeaderColorsAreDrawnRegardlessOfHeaderThemeBackend(bool themes) {
            list.HeaderUsesThemes = themes;
            list.HeaderAppearance.Normal.BackColor = Color.Navy;
            list.HeaderAppearance.Normal.ForeColor = Color.Yellow;
            list.GetColumn(1).HeaderAppearance.Normal.BorderColor = Color.Red;
            list.GetColumn(1).HeaderAppearance.Normal.BorderWidth = 2;
            Rectangle bounds = list.HeaderControl.GetItemRect(1);
            using (Bitmap bitmap = new Bitmap(list.Width, Math.Max(40, bounds.Bottom)))
            using (Graphics graphics = Graphics.FromImage(bitmap)) {
                typeof(HeaderControl).GetMethod("CustomDrawHeaderCell", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(list.HeaderControl, new object[] { graphics, 1, 0 });
                Assert.AreEqual(Color.Navy.ToArgb(), bitmap.GetPixel(bounds.Right - 15, bounds.Top + 8).ToArgb());
                Assert.IsTrue(ContainsColor(bitmap, Color.Yellow, bounds));
                Assert.IsTrue(ContainsColor(bitmap, Color.Red, bounds));
            }
        }

        [TestMethod]
        public void ToolTipInheritsSharedValuesAndRestoresNativeSettingsAfterPop() {
            ToolTipControl tooltip = list.CellToolTip;
            Color originalFore = tooltip.ForeColor;
            Color originalBack = tooltip.BackColor;
            Rectangle originalPadding = tooltip.Padding;
            bool originalBorder = tooltip.HasBorder;
            list.ToolTipAppearance.Normal.ForeColor = Color.Purple;
            list.ToolTipAppearance.Normal.BackColor = Color.Beige;
            list.ToolTipAppearance.Normal.Padding = new Rectangle(2, 3, 4, 5);
            list.GetColumn(1).ToolTipAppearance.Normal.ForeColor = Color.Green;
            list.GetColumn(1).ToolTipAppearance.Normal.BorderWidth = 0;
            StyleValues result = list.ResolveStyle(StyleTarget.ToolTip, list.GetColumn(1), list.GetItem(0),
                Rectangle.Empty, StyleState.Normal, new StyleValues { Font = tooltip.Font });
            Assert.AreEqual(Color.Green, result.ForeColor);
            Assert.AreEqual(Color.Beige, result.BackColor);
            ToolTipShowingEventArgs args = new ToolTipShowingEventArgs {
                ForeColor = result.ForeColor, BackColor = result.BackColor, Font = result.Font,
                Padding = result.Padding, HasBorder = result.BorderWidth > 0
            };
            typeof(ToolTipControl).GetMethod("ApplyEventFormatting", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(tooltip, new object[] { args });
            Assert.AreEqual(Color.Green.ToArgb(), tooltip.ForeColor.ToArgb());
            Assert.AreEqual(Color.Beige.ToArgb(), tooltip.BackColor.ToArgb());
            Assert.AreEqual(new Rectangle(2, 3, 4, 5), tooltip.Padding);
            Assert.IsFalse(tooltip.HasBorder);
            tooltip.PopSettings();
            Assert.AreEqual(originalFore.ToArgb(), tooltip.ForeColor.ToArgb());
            Assert.AreEqual(originalBack.ToArgb(), tooltip.BackColor.ToArgb());
            Assert.AreEqual(originalPadding, tooltip.Padding);
            Assert.AreEqual(originalBorder, tooltip.HasBorder);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ToolTipSettingsAndBorrowedFontsSurviveHandleRecreation(bool customFont) {
            using (Font callerFont = new Font(list.Font, FontStyle.Italic)) {
                ToolTipControl tooltip = list.CellToolTip;
                tooltip.AutoPopDelay = 12345;
                tooltip.InitialDelay = 2345;
                tooltip.ReshowDelay = 345;
                tooltip.Padding = new Rectangle(2, 3, 4, 5);
                tooltip.HasBorder = false;
                if (customFont)
                    tooltip.Font = callerFont;
                Font original = tooltip.Font;
                list.RecreateNativeHandle();
                Application.DoEvents();
                Assert.AreSame(tooltip, list.CellToolTip);
                Assert.AreEqual(12345, tooltip.AutoPopDelay);
                Assert.AreEqual(2345, tooltip.InitialDelay);
                Assert.AreEqual(345, tooltip.ReshowDelay);
                Assert.AreEqual(new Rectangle(2, 3, 4, 5), tooltip.Padding);
                Assert.IsFalse(tooltip.HasBorder);
                Assert.AreEqual(0, GetWindowLong(tooltip.Handle, -16) & 0x00800000,
                    "The replacement HWND must not retain WS_BORDER.");
                Assert.AreSame(original, tooltip.Font);
                using (Bitmap bitmap = new Bitmap(100, 30))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                    Assert.IsGreaterThan(0f, graphics.MeasureString("restored", tooltip.Font).Width);
                list.Dispose();
                Assert.AreEqual(FontStyle.Italic, callerFont.Style);
            }
        }

        [TestMethod]
        public void DerivedFontsAreReusedAndCallerFontsRemainUsableAfterDisposal() {
            using (Font callerFont = new Font(list.Font, FontStyle.Italic)) {
                list.HeaderAppearance.Normal.Font = callerFont;
                list.HeaderAppearance.Normal.FontStyle = FontStyle.Bold;
                StyleValues first = list.ResolveStyle(StyleTarget.Header, list.GetColumn(0), null,
                    Rectangle.Empty, StyleState.Normal, new StyleValues());
                StyleValues second = list.ResolveStyle(StyleTarget.Header, list.GetColumn(0), null,
                    Rectangle.Empty, StyleState.Normal, new StyleValues());
                Assert.AreSame(first.Font, second.Font);
                Assert.AreEqual(FontStyle.Bold, first.Font.Style);
                Assert.AreEqual(FontStyle.Italic, callerFont.Style);
                list.Dispose();
                using (Bitmap bitmap = new Bitmap(100, 30))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                    Assert.IsGreaterThan(0f, graphics.MeasureString("caller font", callerFont).Width);
            }
        }

        [TestMethod]
        public void LegacyRegularIsExplicitAndCanBeResetToInheritance() {
            using (Font bold = new Font(list.Font, FontStyle.Bold)) {
                OLVListItem item = list.GetItem(0);
                item.Font = bold;
                using (SimpleItemStyle style = new SimpleItemStyle()) {
                    list.ApplyRowStyle(item, style);
                    Assert.AreEqual(FontStyle.Bold, item.Font.Style);
                    style.FontStyle = FontStyle.Regular;
                    Assert.IsTrue(TypeDescriptor.GetProperties(style)["FontStyle"].ShouldSerializeValue(style));
                    list.ApplyRowStyle(item, style);
                    Assert.AreEqual(FontStyle.Regular, item.Font.Style);
                    style.ResetFontStyle();
                    Assert.IsFalse(TypeDescriptor.GetProperties(style)["FontStyle"].ShouldSerializeValue(style));
                }
            }
        }

        [TestMethod]
        public void DesignerMetadataPreservesExplicitZeroRegularAndNestedContent() {
            StyleValues values = new StyleValues();
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(values);
            Assert.IsFalse(properties[nameof(StyleValues.FontStyle)].ShouldSerializeValue(values));
            Assert.IsFalse(properties[nameof(StyleValues.BorderWidth)].ShouldSerializeValue(values));
            values.FontStyle = FontStyle.Regular;
            values.BorderWidth = 0;
            values.Padding = Rectangle.Empty;
            Assert.IsTrue(properties[nameof(StyleValues.FontStyle)].ShouldSerializeValue(values));
            Assert.IsTrue(properties[nameof(StyleValues.BorderWidth)].ShouldSerializeValue(values));
            Assert.IsTrue(properties[nameof(StyleValues.Padding)].ShouldSerializeValue(values));
            foreach (object owner in new object[] { list, list.GetColumn(0), new StyleSheet(), new StyleRule() }) {
                string property = owner is ObjectListViewBase ? "RowAppearance" :
                    owner is OLVColumn ? "CellAppearance" : owner is StyleSheet ? "Normal" : "Values";
                Assert.AreEqual(DesignerSerializationVisibility.Content,
                    ((DesignerSerializationVisibilityAttribute)TypeDescriptor.GetProperties(owner)[property]
                        .Attributes[typeof(DesignerSerializationVisibilityAttribute)]).Visibility);
            }
        }

#if NETFRAMEWORK
        [TestMethod]
        public void FrameworkDesignerSerializesNestedRulesAndExplicitDefaults() {
            using (DesignSurface surface = new DesignSurface(typeof(Form)))
            using (CSharpCodeProvider provider = new CSharpCodeProvider())
            using (StringWriter writer = new StringWriter()) {
                IDesignerHost host = (IDesignerHost)surface.GetService(typeof(IDesignerHost));
                ObjectListView styledList = (ObjectListView)host.CreateComponent(typeof(ObjectListView), "styledList");
                ((Form)host.RootComponent).Controls.Add(styledList);
                styledList.RowAppearance.Normal.FontStyle = FontStyle.Regular;
                styledList.RowAppearance.Normal.BorderWidth = 0;
                styledList.RowAppearance.States.Add(Rule(StyleState.Selected | StyleState.Focused, Color.Blue));
                DesignerSerializationManager manager = new DesignerSerializationManager(host);
                using (manager.CreateSession()) {
                    CodeDomSerializer serializer = (CodeDomSerializer)manager.GetSerializer(
                        typeof(ObjectListView), typeof(CodeDomSerializer));
                    CodeStatementCollection statements = (CodeStatementCollection)serializer.Serialize(manager, styledList);
                    foreach (CodeStatement statement in statements)
                        provider.GenerateCodeFromStatement(statement, writer, new CodeGeneratorOptions());
                    string code = writer.ToString();
                    Assert.AreEqual(0, manager.Errors.Count, code);
                    StringAssert.Contains(code, ".RowAppearance.Normal.FontStyle");
                    StringAssert.Contains(code, "System.Drawing.FontStyle.Regular");
                    StringAssert.Contains(code, ".RowAppearance.Normal.BorderWidth");
                    StringAssert.Contains(code, ".RowAppearance.States.Add");
                    StringAssert.Contains(code, ".Values.ForeColor");
                }
            }
        }
#endif

        private static StyleRule Rule(StyleState state, Color color) {
            StyleRule rule = new StyleRule { RequiredState = state };
            rule.Values.ForeColor = color;
            return rule;
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong(IntPtr handle, int index);

        private void Configure(BaseRenderer renderer, Graphics graphics, OLVListItem item, int column, Size size) {
            Rectangle bounds = new Rectangle(Point.Empty, size);
            renderer.ConfigureSubItem(new DrawListViewSubItemEventArgs(graphics, bounds, item, item.SubItems[column],
                item.Index, column, list.GetColumn(column), ListViewItemStates.Default), bounds, item.RowObject);
        }

        private static bool ContainsColor(Bitmap bitmap, Color color, Rectangle bounds) {
            for (int y = Math.Max(0, bounds.Top); y < Math.Min(bitmap.Height, bounds.Bottom); y++)
                for (int x = Math.Max(0, bounds.Left); x < Math.Min(bitmap.Width, bounds.Right); x++)
                    if (bitmap.GetPixel(x, y).ToArgb() == color.ToArgb())
                        return true;
            return false;
        }

        private sealed class StyleList : ObjectListView {
            public void Press(Point location) =>
                this.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0));
            public void LoseCapture() => this.OnMouseCaptureChanged(EventArgs.Empty);
            public void RecreateNativeHandle() => this.RecreateHandle();
        }

        private sealed class HeaderProbe : HeaderControl {
            public HeaderProbe(ObjectListViewBase list) : base(list) { }
            public HeaderStateStyle Resolve(OLVColumn column, bool hot, bool pressed) =>
                this.CalculateStateStyle(column, hot, pressed);
        }
    }
}
