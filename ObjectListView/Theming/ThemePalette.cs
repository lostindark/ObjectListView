using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace BrightIdeasSoftware
{
    /// <summary>Named theme colors. Publicly constructed instances are immutable snapshots.</summary>
    public sealed class ThemePalette : IThemeColors
    {
        private readonly Dictionary<string, Color> colors;
        private readonly bool dark;
        private readonly bool highContrast;

        private ThemePalette() { }

        internal static ThemePalette CreateCurrent() => new ThemePalette();

        /// <summary>Resolves colors for a mode, with Windows high contrast taking precedence.</summary>
        public ThemePalette(bool dark, bool highContrast)
        {
            this.highContrast = highContrast || SystemInformation.HighContrast;
            this.dark = dark && !this.highContrast;
            this.colors = ObjectListViewTheme.ResolveColors(this.dark, this.highContrast);
        }

        private Color GetColor([CallerMemberName] string colorName = null) => this.colors == null
            ? ObjectListViewTheme.ResolveColor(colorName, this.Dark, this.HighContrast)
            : this.colors[colorName];

        /// <inheritdoc />
        public bool Dark => this.colors == null ? ObjectListViewTheme.IsDarkMode : this.dark;
        /// <inheritdoc />
        public bool HighContrast => this.colors == null ? SystemInformation.HighContrast : this.highContrast;
        /// <inheritdoc />
        public Color WindowColor => this.GetColor();
        /// <inheritdoc />
        public Color TextColor => this.GetColor();
        /// <inheritdoc />
        public Color ControlColor => this.GetColor();
        /// <inheritdoc />
        public Color InputColor => this.GetColor();
        /// <inheritdoc />
        public Color ControlLightColor => this.GetColor();
        /// <inheritdoc />
        public Color ControlLightLightColor => this.GetColor();
        /// <inheritdoc />
        public Color ControlDarkColor => this.GetColor();
        /// <inheritdoc />
        public Color GrayTextColor => this.GetColor();
        /// <inheritdoc />
        public Color HighlightColor => this.GetColor();
        /// <inheritdoc />
        public Color HighlightTextColor => this.GetColor();
        /// <inheritdoc />
        public Color AccentColor => this.GetColor();
        /// <inheritdoc />
        public Color ButtonColor => this.GetColor();
        /// <inheritdoc />
        public Color ButtonDisabledColor => this.GetColor();
        /// <inheritdoc />
        public Color ButtonHoverColor => this.GetColor();
        /// <inheritdoc />
        public Color ButtonPressedColor => this.GetColor();
        /// <inheritdoc />
        public Color ButtonTextColor => this.GetColor();
        /// <inheritdoc />
        public Color ButtonDisabledTextColor => this.GetColor();
        /// <inheritdoc />
        public Color HoverColor => this.GetColor();
        /// <inheritdoc />
        public Color InactiveSelectionColor => this.GetColor();
        /// <inheritdoc />
        public Color InactiveSelectionTextColor => this.GetColor();
        /// <inheritdoc />
        public Color SecondaryTextColor => this.GetColor();
        /// <inheritdoc />
        public Color PositiveTextColor => this.GetColor();
        /// <inheritdoc />
        public Color NegativeTextColor => this.GetColor();
        /// <inheritdoc />
        public Color InformationBackColor => this.GetColor();
        /// <inheritdoc />
        public Color InformationTextColor => this.GetColor();
        /// <inheritdoc />
        public Color LinkColor => this.GetColor();
        /// <inheritdoc />
        public Color VisitedLinkColor => this.GetColor();
        /// <inheritdoc />
        public Color MenuColor => this.GetColor();
        /// <inheritdoc />
        public Color MenuHighlightColor => this.GetColor();
        /// <inheritdoc />
        public Color GroupHeaderTextColor => this.GetColor();
        /// <inheritdoc />
        public Color GroupHeaderSeparatorColor => this.GetColor();
        /// <inheritdoc />
        public Color ScrollBarTrackColor => this.GetColor();
        /// <inheritdoc />
        public Color ScrollBarThumbColor => this.GetColor();
        /// <inheritdoc />
        public Color ScrollBarArrowColor => this.GetColor();
        /// <inheritdoc />
        public Color EmptyListTextColor => this.GetColor();
        /// <inheritdoc />
        public Color EmptyListBackgroundColor => this.GetColor();
    }
}
