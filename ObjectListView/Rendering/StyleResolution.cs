using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BrightIdeasSoftware {
    [Flags]
    internal enum CellStyleChanges { None = 0, Font = 1, ForeColor = 2, BackColor = 4, Tracking = 8 }

    /// <summary>Independent visual states. Rules may require any combination.</summary>
    [Flags]
    public enum StyleState {
        /// <summary>No state flags.</summary>
        Normal = 0,
        /// <summary>The row is selected.</summary>
        Selected = 1,
        /// <summary>The list has keyboard focus.</summary>
        Focused = 2,
        /// <summary>The pointer is over the target.</summary>
        Hot = 4,
        /// <summary>The target is being pressed.</summary>
        Pressed = 8,
        /// <summary>The control or row is disabled.</summary>
        Disabled = 16
    }

    /// <summary>The surface being styled.</summary>
    public enum StyleTarget {
        /// <summary>A row or cell.</summary>
        Cell,
        /// <summary>A column header.</summary>
        Header,
        /// <summary>A cell or header tooltip.</summary>
        ToolTip
    }

    /// <summary>
    /// Per-property overrides. Null means inherit; zero, Regular and transparent colors
    /// are explicit values. Fonts supplied by applications remain application-owned.
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class StyleValues {
        /// <summary>Borrowed font, or null to inherit.</summary>
        [DefaultValue(null)]
        public Font Font { get; set; }
        /// <summary>Font style replacement, including an explicit Regular.</summary>
        [DefaultValue(null)]
        public FontStyle? FontStyle { get; set; }
        /// <summary>Text color override.</summary>
        [DefaultValue(null)]
        public Color? ForeColor { get; set; }
        /// <summary>Background color override.</summary>
        [DefaultValue(null)]
        public Color? BackColor { get; set; }
        /// <summary>Left, top, right and bottom insets, in device pixels.</summary>
        [DefaultValue(null)]
        public Rectangle? Padding { get; set; }
        /// <summary>Horizontal content alignment.</summary>
        [DefaultValue(null)]
        public HorizontalAlignment? HorizontalAlignment { get; set; }
        /// <summary>Vertical content alignment.</summary>
        [DefaultValue(null)]
        public StringAlignment? VerticalAlignment { get; set; }
        /// <summary>Border color for custom drawing.</summary>
        [DefaultValue(null)]
        public Color? BorderColor { get; set; }
        /// <summary>Nonnegative border width in device pixels; zero suppresses it.</summary>
        [DefaultValue(null)]
        public float? BorderWidth {
            get { return this.borderWidth; }
            set {
                if (value.HasValue && (value.Value < 0 || float.IsNaN(value.Value) || float.IsInfinity(value.Value)))
                    throw new ArgumentOutOfRangeException(nameof(value));
                this.borderWidth = value;
            }
        }
        private float? borderWidth;

        /// <summary>Whether all properties inherit.</summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsEmpty => Font == null && !FontStyle.HasValue &&
            !ForeColor.HasValue && !BackColor.HasValue && !Padding.HasValue &&
            !HorizontalAlignment.HasValue && !VerticalAlignment.HasValue &&
            !BorderColor.HasValue && !BorderWidth.HasValue;

        /// <summary>Overlay only specified properties, leaving the source unchanged.</summary>
        public void Overlay(StyleValues values) {
            if (values == null)
                return;
            Font = values.Font ?? Font;
            FontStyle = values.FontStyle ?? FontStyle;
            ForeColor = values.ForeColor ?? ForeColor;
            BackColor = values.BackColor ?? BackColor;
            Padding = values.Padding ?? Padding;
            HorizontalAlignment = values.HorizontalAlignment ?? HorizontalAlignment;
            VerticalAlignment = values.VerticalAlignment ?? VerticalAlignment;
            BorderColor = values.BorderColor ?? BorderColor;
            BorderWidth = values.BorderWidth ?? BorderWidth;
        }
    }

    /// <summary>A state override. Later matching rules win per property.</summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class StyleRule {
        /// <summary>All these flags must be present.</summary>
        [DefaultValue(StyleState.Normal)]
        public StyleState RequiredState { get; set; }
        /// <summary>None of these flags may be present.</summary>
        [DefaultValue(StyleState.Normal)]
        public StyleState ExcludedState { get; set; }
        /// <summary>Overrides applied when the rule matches.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public StyleValues Values { get; } = new StyleValues();

        internal bool Matches(StyleState state) =>
            (state & RequiredState) == RequiredState && (state & ExcludedState) == 0;
    }

    /// <summary>Normal values followed by ordered state overrides.</summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class StyleSheet {
        /// <summary>Values inherited before state overrides.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public StyleValues Normal { get; } = new StyleValues();
        /// <summary>Matching rules apply in collection order.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<StyleRule> States { get; } = new Collection<StyleRule>();

        /// <summary>Whether the sheet has neither values nor state rules.</summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsEmpty => Normal.IsEmpty && States.Count == 0;

        internal void Apply(StyleValues result, StyleState state, bool includeNormal) {
            if (includeNormal)
                result.Overlay(Normal);
            for (int i = 0; i < States.Count; i++) {
                StyleRule rule = States[i];
                if (rule.Matches(state))
                    result.Overlay(rule.Values);
            }
        }
    }

    /// <summary>Read-only information supplied to the final style callback.</summary>
    public sealed class StyleContext {
        internal StyleContext(ObjectListViewBase listView, StyleTarget target, OLVColumn column,
            OLVListItem item, Rectangle bounds, StyleState state) {
            ListView = listView;
            Target = target;
            Column = column;
            Item = item;
            Bounds = bounds;
            State = state;
            Dpi = listView.StyleDpi;
            HighContrast = SystemInformation.HighContrast;
        }

        /// <summary>The control being styled.</summary>
        public ObjectListViewBase ListView { get; }
        /// <summary>The target surface.</summary>
        public StyleTarget Target { get; }
        /// <summary>The target column, if any.</summary>
        public OLVColumn Column { get; }
        /// <summary>The target row, if any.</summary>
        public OLVListItem Item { get; }
        /// <summary>The target model, or null for headers.</summary>
        public object Model => Item?.RowObject;
        /// <summary>Target bounds in control client coordinates.</summary>
        public Rectangle Bounds { get; }
        /// <summary>Combined visual state.</summary>
        public StyleState State { get; }
        /// <summary>Control DPI at resolution time.</summary>
        public int Dpi { get; }
        /// <summary>Control text direction.</summary>
        public RightToLeft RightToLeft => ListView.RightToLeft;
        /// <summary>Whether Windows high contrast is active.</summary>
        public bool HighContrast { get; }
    }

    /// <summary>Final per-property overrides; never mutate the source styles.</summary>
    public sealed class StyleResolvingEventArgs : EventArgs {
        internal StyleResolvingEventArgs(StyleContext context, StyleValues values) {
            Context = context;
            Values = values;
        }
        /// <summary>The target information.</summary>
        public StyleContext Context { get; }
        /// <summary>Independent mutable values for this resolution only.</summary>
        public StyleValues Values { get; }
    }
}
