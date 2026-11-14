using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace BrightIdeasSoftware {
    public abstract partial class ObjectListViewBase {
        /// <summary>Inherited row/cell values. Rebuild rows after changing normal values.</summary>
        [Category("Appearance"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public StyleSheet RowAppearance { get; } = new StyleSheet();

        /// <summary>Shared header values, independent of the native theme backend.</summary>
        [Category("Appearance"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public StyleSheet HeaderAppearance { get; } = new StyleSheet();

        /// <summary>Shared native tooltip font and colors.</summary>
        [Category("Appearance"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public StyleSheet ToolTipAppearance { get; } = new StyleSheet();

        /// <summary>Runs after inheritance and state rules, before accessibility colors.</summary>
        [Category("Appearance")]
        public event EventHandler<StyleResolvingEventArgs> StyleResolving;

        private readonly Dictionary<Tuple<Font, FontStyle>, Font> styleFonts =
            new Dictionary<Tuple<Font, FontStyle>, Font>();

        internal int StyleDpi => this.IsHandleCreated ? this.GetWindowDpi() : 96;
        private int pressedStyleRowIndex = -1;
        private bool? rowStyleHighContrast;
        private bool? customStyleResolver;

        private bool HasCustomStyleResolver {
            get {
                if (StyleResolving != null)
                    return true;
                if (!this.customStyleResolver.HasValue)
                    this.customStyleResolver = this.GetType().GetMethod(nameof(OnStyleResolving),
                        BindingFlags.Instance | BindingFlags.NonPublic, null,
                        new[] { typeof(StyleResolvingEventArgs) }, null).DeclaringType != typeof(ObjectListViewBase);
                return this.customStyleResolver.Value;
            }
        }

        internal Font ResolveStyleFont(Font font, FontStyle? style) {
            font = font ?? this.Font;
            if (!style.HasValue || font.Style == style.Value)
                return font;
            var key = Tuple.Create(font, style.Value);
            if (!styleFonts.TryGetValue(key, out Font result)) {
                result = new Font(font, style.Value);
                styleFonts.Add(key, result);
            }
            return result;
        }

        private static bool HasItemFontStyle(IItemStyle style) =>
            style is SimpleItemStyle simple ? simple.ShouldSerializeFontStyle() :
            style is CellStyle cell ? cell.ShouldSerializeFontStyle() :
            style.FontStyle != FontStyle.Regular;

        private void DisposeStyleFonts() {
            foreach (Font font in styleFonts.Values)
                font.Dispose();
            styleFonts.Clear();
        }

        internal StyleState GetStyleState(OLVListItem item) {
            StyleState state = StyleState.Normal;
            if (this.Focused)
                state |= StyleState.Focused;
            if (!this.Enabled || (item != null && !item.Enabled))
                state |= StyleState.Disabled;
            if (item != null) {
                if (item.Selected)
                    state |= StyleState.Selected;
                if (item.Index >= 0 && item.Index == this.HotRowIndex)
                    state |= StyleState.Hot;
                if (item.Index >= 0 && item.Index == this.pressedStyleRowIndex &&
                    (state & StyleState.Hot) != 0)
                    state |= StyleState.Pressed;
            }
            return state;
        }

        /// <summary>
        /// Resolve an independent snapshot from defaults, normal values, state rules and callbacks.
        /// </summary>
        public StyleValues ResolveStyle(StyleTarget target, OLVColumn column, OLVListItem item,
            Rectangle bounds, StyleState state, StyleValues defaults) =>
            ResolveStyle(target, column, item, bounds, state, defaults, true);

        internal StyleValues ResolveStyle(StyleTarget target, OLVColumn column, OLVListItem item,
            Rectangle bounds, StyleState state, StyleValues defaults, bool includeCellNormal) {
            if (defaults == null)
                throw new ArgumentNullException(nameof(defaults));
            StyleValues result = new StyleValues();
            result.Overlay(defaults);
            switch (target) {
                case StyleTarget.Cell:
                    if (includeCellNormal) {
                        result.Overlay(RowAppearance.Normal);
                        result.Overlay(column?.CellAppearanceOrNull?.Normal);
                        result.Overlay(item?.AppearanceOrNull?.Normal);
                        result.Overlay(item?.GetSubItem(column?.Index ?? 0)?.AppearanceOrNull?.Normal);
                    }
                    RowAppearance.Apply(result, state, false);
                    column?.CellAppearanceOrNull?.Apply(result, state, false);
                    item?.AppearanceOrNull?.Apply(result, state, false);
                    item?.GetSubItem(column?.Index ?? 0)?.AppearanceOrNull?.Apply(result, state, false);
                    break;
                case StyleTarget.Header:
                    HeaderAppearance.Apply(result, state, true);
                    column?.HeaderAppearance.Apply(result, state, true);
                    break;
                case StyleTarget.ToolTip:
                    ToolTipAppearance.Apply(result, state, true);
                    column?.ToolTipAppearance.Apply(result, state, true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target));
            }
            this.OnStyleResolving(new StyleResolvingEventArgs(
                new StyleContext(this, target, column, item, bounds, state), result));
            if (SystemInformation.HighContrast)
                ApplyHighContrastColors(result, target, state, this.HideSelection);
            result.Font = ResolveStyleFont(result.Font, result.FontStyle);
            result.FontStyle = null;
            return result;
        }

        /// <summary>Raise the final style override callback.</summary>
        protected virtual void OnStyleResolving(StyleResolvingEventArgs e) {
            StyleResolving?.Invoke(this, e);
        }

        private static void ApplyHighContrastColors(StyleValues result, StyleTarget target,
            StyleState state, bool hideSelection) {
            bool selected = target == StyleTarget.Cell && (state & StyleState.Selected) != 0 &&
                ((state & StyleState.Focused) != 0 || !hideSelection);
            result.ForeColor = target == StyleTarget.ToolTip ? SystemColors.InfoText :
                (state & StyleState.Disabled) != 0 ? SystemColors.GrayText :
                selected ? SystemColors.HighlightText : SystemColors.WindowText;
            result.BackColor = target == StyleTarget.ToolTip ? SystemColors.Info :
                selected ? SystemColors.Highlight :
                target == StyleTarget.Header ? SystemColors.Control : SystemColors.Window;
            result.BorderColor = SystemColors.WindowText;
        }

        private void ApplyNativeStyleAccessibility(OLVListItem item) {
            if (!(this.rowStyleHighContrast ?? SystemInformation.HighContrast))
                return;
            StyleValues values = new StyleValues();
            ApplyHighContrastColors(values, StyleTarget.Cell,
                item.Enabled && this.Enabled ? StyleState.Normal : StyleState.Disabled, this.HideSelection);
            foreach (ListViewItem.ListViewSubItem cell in item.SubItems) {
                cell.ForeColor = values.ForeColor.Value;
                cell.BackColor = values.BackColor.Value;
            }
        }

        private bool ApplyNormalAppearance(OLVListItem item) {
            bool hasStyle = !RowAppearance.Normal.IsEmpty || item.AppearanceOrNull?.Normal.IsEmpty == false;
            for (int i = 0; i < item.SubItems.Count; i++) {
                OLVColumn column = this.GetAppearanceColumn(i);
                OLVListSubItem cell = item.GetSubItem(i);
                hasStyle |= column?.CellAppearanceOrNull?.Normal.IsEmpty == false ||
                    cell?.AppearanceOrNull?.Normal.IsEmpty == false;
            }
            if (!hasStyle)
                return false;

            PropagateFormatFromRowToCells(item);
            for (int i = 0; i < item.SubItems.Count; i++) {
                ListViewItem.ListViewSubItem cell = item.SubItems[i];
                OLVListSubItem olvCell = cell as OLVListSubItem;
                StyleValues values = new StyleValues {
                    Font = cell.Font, ForeColor = cell.ForeColor, BackColor = cell.BackColor
                };
                values.Overlay(RowAppearance.Normal);
                values.Overlay(this.GetAppearanceColumn(i)?.CellAppearanceOrNull?.Normal);
                values.Overlay(item.AppearanceOrNull?.Normal);
                values.Overlay(olvCell?.AppearanceOrNull?.Normal);
                cell.Font = ResolveStyleFont(values.Font, values.FontStyle);
                cell.ForeColor = values.ForeColor.Value;
                cell.BackColor = values.BackColor.Value;
                if (olvCell != null) {
                    olvCell.CellPadding = values.Padding ?? olvCell.CellPadding;
                    olvCell.CellVerticalAlignment = values.VerticalAlignment ?? olvCell.CellVerticalAlignment;
                }
            }
            return true;
        }

        private struct CellFormattingSnapshot {
            internal ListViewItem.ListViewSubItem Cell;
            internal Font Font;
            internal Color ForeColor;
            internal Color BackColor;
        }

        private bool FormatRowWithAppearance(int rowIndex, int displayIndex, OLVListItem item) {
            if (!this.ApplyNormalAppearance(item))
                return this.TriggerFormatRowEvent(rowIndex, displayIndex, item);

            var snapshots = new CellFormattingSnapshot[item.SubItems.Count];
            for (int i = 0; i < snapshots.Length; i++) {
                ListViewItem.ListViewSubItem cell = item.SubItems[i];
                snapshots[i] = new CellFormattingSnapshot {
                    Cell = cell, Font = cell.Font, ForeColor = cell.ForeColor, BackColor = cell.BackColor
                };
                if (cell is OLVListSubItem olvCell)
                    olvCell.FormattingChanges = CellStyleChanges.Tracking;
            }
            StyleValues rowOverrides = new StyleValues();
            item.FormattingOverrides = rowOverrides;
            try {
                bool result = this.TriggerFormatRowEvent(rowIndex, displayIndex, item);
                if (!item.UseItemStyleForSubItems) {
                    for (int i = 0; i < snapshots.Length; i++) {
                        ListViewItem.ListViewSubItem cell = snapshots[i].Cell;
                        CellStyleChanges changes = (cell as OLVListSubItem)?.FormattingChanges ?? CellStyleChanges.None;
                        if (rowOverrides.Font != null && (changes & CellStyleChanges.Font) == 0 &&
                            Equals(cell.Font, snapshots[i].Font))
                            cell.Font = rowOverrides.Font;
                        if (rowOverrides.ForeColor.HasValue && (changes & CellStyleChanges.ForeColor) == 0 &&
                            cell.ForeColor == snapshots[i].ForeColor)
                            cell.ForeColor = rowOverrides.ForeColor.Value;
                        if (rowOverrides.BackColor.HasValue && (changes & CellStyleChanges.BackColor) == 0 &&
                            cell.BackColor == snapshots[i].BackColor)
                            cell.BackColor = rowOverrides.BackColor.Value;
                    }
                }
                return result;
            } finally {
                item.FormattingOverrides = null;
                foreach (CellFormattingSnapshot snapshot in snapshots) {
                    if (snapshot.Cell is OLVListSubItem cell)
                        cell.FormattingChanges = CellStyleChanges.None;
                }
            }
        }

        private OLVColumn GetAppearanceColumn(int subItemIndex) {
            if (this.View != View.Tile || subItemIndex == 0)
                return this.GetColumn(subItemIndex);
            int tileIndex = 0;
            for (int i = 1; i < this.Columns.Count; i++) {
                OLVColumn column = this.GetColumn(i);
                if (column.IsTileViewColumn && ++tileIndex == subItemIndex)
                    return column;
            }
            return null;
        }

        /// <inheritdoc />
        protected override void OnMouseCaptureChanged(EventArgs e) {
            base.OnMouseCaptureChanged(e);
            if (this.pressedStyleRowIndex >= 0) {
                this.pressedStyleRowIndex = -1;
                this.Invalidate();
            }
        }

        /// <inheritdoc />
        protected override void OnGotFocus(EventArgs e) {
            base.OnGotFocus(e);
            this.Invalidate();
        }

        /// <inheritdoc />
        protected override void OnLostFocus(EventArgs e) {
            base.OnLostFocus(e);
            this.Invalidate();
        }

        internal void ApplyCellGeometry(StyleValues values, OLVColumn column,
            OLVListItem item, OLVListSubItem cell) {
            values.BorderColor = null;
            values.BorderWidth = null;
            OverlayCellGeometry(values, RowAppearance.Normal);
            OverlayCellGeometry(values, column?.CellAppearanceOrNull?.Normal);
            OverlayCellGeometry(values, item.AppearanceOrNull?.Normal);
            OverlayCellGeometry(values, cell?.AppearanceOrNull?.Normal);
        }

        private static void OverlayCellGeometry(StyleValues values, StyleValues geometry) {
            if (geometry == null)
                return;
            values.HorizontalAlignment = geometry.HorizontalAlignment ?? values.HorizontalAlignment;
            values.BorderColor = geometry.BorderColor ?? values.BorderColor;
            values.BorderWidth = geometry.BorderWidth ?? values.BorderWidth;
        }

        internal bool HasHeaderAppearance(OLVColumn column) =>
            !HeaderAppearance.IsEmpty || (column != null && !column.HeaderAppearance.IsEmpty) ||
            HasCustomStyleResolver || SystemInformation.HighContrast;

        internal bool HasCellAppearance(OLVColumn column, OLVListItem item, OLVListSubItem cell) =>
            !RowAppearance.IsEmpty || column?.CellAppearanceOrNull?.IsEmpty == false ||
            item.AppearanceOrNull?.IsEmpty == false || cell?.AppearanceOrNull?.IsEmpty == false ||
            HasCustomStyleResolver || SystemInformation.HighContrast;

        private void ApplyToolTipAppearance(ToolTipShowingEventArgs e) {
            if (ToolTipAppearance.IsEmpty && (e.Column == null || e.Column.ToolTipAppearance.IsEmpty) &&
                StyleResolving == null && !SystemInformation.HighContrast)
                return;
            ToolTipControl tooltip = e.ToolTipControl;
            StyleValues values = ResolveStyle(StyleTarget.ToolTip, e.Column, e.Item,
                e.TargetBounds,
                GetStyleState(e.Item), new StyleValues {
                    Font = tooltip.Font, ForeColor = tooltip.ForeColor, BackColor = tooltip.BackColor
                });
            e.Font = values.Font;
            e.ForeColor = values.ForeColor;
            e.BackColor = values.BackColor;
            e.Padding = values.Padding;
            if (values.BorderWidth.HasValue)
                e.HasBorder = values.BorderWidth.Value > 0;
            e.RightToLeft = this.RightToLeft;
        }

        private static void ApplyToolTipAccessibility(ToolTipShowingEventArgs e) {
            if (SystemInformation.HighContrast) {
                e.ForeColor = SystemColors.InfoText;
                e.BackColor = SystemColors.Info;
            }
        }
    }


}
