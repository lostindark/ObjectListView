using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BrightIdeasSoftware {
    /// <summary>
    /// Selects the default palette used by ObjectListView controls created by an application.
    /// SetDarkColor configures dark-mode overrides only. Light and high contrast always use native colors.
    /// Color.Empty clears an individual dark override; ResetColors clears all dark overrides.
    /// Call ObjectListViewBase.RefreshTheme after changing settings on an existing list,
    /// or FormTheme.RefreshAll for forms using the optional form service.
    /// </summary>
    public static class ObjectListViewTheme {
        private static ObjectListViewColorMode colorMode = ObjectListViewColorMode.System;
#if NETFRAMEWORK
        private static bool systemDarkMode = ReadSystemDarkMode();
#endif
        private static HotItemStyle lightHotItemStyle;
        private static HotItemStyle darkHotItemStyle;

        private static readonly Dictionary<string, ColorDefinition> ColorMap = new Dictionary<string, ColorDefinition>(StringComparer.Ordinal) {
            [nameof(IThemeColors.WindowColor)] = new(SystemColors.Window, Color.FromArgb(50, 50, 50)),
            [nameof(IThemeColors.TextColor)] = new(SystemColors.WindowText, Color.FromArgb(240, 240, 240)),
            [nameof(IThemeColors.ControlColor)] = new(SystemColors.Control, Color.FromArgb(32, 32, 32)),
            [nameof(IThemeColors.InputColor)] = new(SystemColors.Window, nameof(IThemeColors.ControlLightColor)),
            [nameof(IThemeColors.ControlLightColor)] = new(SystemColors.ControlLight, Color.FromArgb(46, 46, 46)),
            [nameof(IThemeColors.ControlLightLightColor)] = new(SystemColors.ControlLightLight, Color.FromArgb(31, 31, 31)),
            [nameof(IThemeColors.ControlDarkColor)] = new(SystemColors.ControlDark, Color.FromArgb(74, 74, 74)),
            [nameof(IThemeColors.GrayTextColor)] = new(SystemColors.GrayText, Color.FromArgb(150, 150, 150)),
            [nameof(IThemeColors.HighlightColor)] = new(SystemColors.Highlight, Color.FromArgb(40, 100, 180)),
            [nameof(IThemeColors.HighlightTextColor)] = new(SystemColors.HighlightText, Color.Black),
            [nameof(IThemeColors.AccentColor)] = new(SystemColors.Highlight, nameof(IThemeColors.HighlightColor)),
            [nameof(IThemeColors.ButtonColor)] = new(SystemColors.Control, Color.FromArgb(51, 51, 51)),
            [nameof(IThemeColors.ButtonDisabledColor)] = new(SystemColors.Control, Color.FromArgb(32, 32, 32)),
            [nameof(IThemeColors.ButtonHoverColor)] = new(SystemColors.ControlLight, Color.FromArgb(69, 69, 69)),
            [nameof(IThemeColors.ButtonPressedColor)] = new(SystemColors.ControlDark, Color.FromArgb(102, 102, 102)),
            [nameof(IThemeColors.ButtonTextColor)] = new(SystemColors.ControlText, Color.FromArgb(240, 240, 240)),
            [nameof(IThemeColors.ButtonDisabledTextColor)] = new(SystemColors.GrayText, Color.FromArgb(150, 150, 150)),
            [nameof(IThemeColors.HoverColor)] = new(SystemColors.ControlLight, nameof(IThemeColors.ControlLightColor)),
            [nameof(IThemeColors.InactiveSelectionColor)] = new(SystemColors.Control, nameof(IThemeColors.ControlColor)),
            [nameof(IThemeColors.InactiveSelectionTextColor)] = new(SystemColors.ControlText, Color.FromArgb(240, 240, 240)),
            [nameof(IThemeColors.SecondaryTextColor)] = new(SystemColors.GrayText, nameof(IThemeColors.GrayTextColor)),
            [nameof(IThemeColors.PositiveTextColor)] = new(SystemColors.HotTrack, Color.LightGreen),
            [nameof(IThemeColors.NegativeTextColor)] = new(SystemColors.HotTrack, Color.LightCoral),
            [nameof(IThemeColors.InformationBackColor)] = new(SystemColors.Info, Color.FromArgb(53, 42, 5)),
            [nameof(IThemeColors.InformationTextColor)] = new(SystemColors.InfoText, Color.FromArgb(255, 225, 138)),
            [nameof(IThemeColors.LinkColor)] = new(SystemColors.HotTrack, Color.LightSkyBlue),
            [nameof(IThemeColors.VisitedLinkColor)] = new(SystemColors.HotTrack, Color.FromArgb(197, 134, 192)),
            [nameof(IThemeColors.MenuColor)] = new(SystemColors.Menu, Color.FromArgb(55, 55, 55)),
            [nameof(IThemeColors.MenuHighlightColor)] = new(SystemColors.MenuHighlight, Color.FromArgb(42, 128, 210)),
            [nameof(IThemeColors.GroupHeaderTextColor)] = new(SystemColors.HotTrack, nameof(IThemeColors.LinkColor)),
            [nameof(IThemeColors.GroupHeaderSeparatorColor)] = new(SystemColors.ControlDark, nameof(IThemeColors.ControlDarkColor)),
            [nameof(IThemeColors.ScrollBarTrackColor)] = new(SystemColors.Control, nameof(IThemeColors.ControlColor)),
            [nameof(IThemeColors.ScrollBarThumbColor)] = new(SystemColors.GrayText, nameof(IThemeColors.GrayTextColor)),
            [nameof(IThemeColors.ScrollBarArrowColor)] = new(SystemColors.WindowText, nameof(IThemeColors.TextColor)),
            [nameof(IThemeColors.EmptyListTextColor)] = new(SystemColors.WindowText, nameof(IThemeColors.TextColor)),
            [nameof(IThemeColors.EmptyListBackgroundColor)] = new(SystemColors.Window, nameof(IThemeColors.ControlLightColor))
        };
        private static readonly Dictionary<string, Color> DarkOverrides = new Dictionary<string, Color>(StringComparer.Ordinal);
        private static readonly ThemePalette CurrentColors = ThemePalette.CreateCurrent();

        internal static bool HasColorOverrides => IsDarkMode && DarkOverrides.Count != 0;
        internal static bool HasButtonColorOverrides => IsDarkMode && (DarkOverrides.ContainsKey(nameof(IThemeColors.ButtonColor))
            || DarkOverrides.ContainsKey(nameof(IThemeColors.ButtonDisabledColor)) || DarkOverrides.ContainsKey(nameof(IThemeColors.ButtonHoverColor))
            || DarkOverrides.ContainsKey(nameof(IThemeColors.ButtonPressedColor)));
        internal static bool HasDisabledButtonTextOverride => IsDarkMode && DarkOverrides.ContainsKey(nameof(IThemeColors.ButtonDisabledTextColor));
        internal static bool HasMutedTextOverride => IsDarkMode && DarkOverrides.ContainsKey(nameof(IThemeColors.GrayTextColor));
        internal static bool HasGroupHeaderColorOverrides => IsDarkMode && (DarkOverrides.ContainsKey(nameof(IThemeColors.GroupHeaderTextColor))
            || DarkOverrides.ContainsKey(nameof(IThemeColors.GroupHeaderSeparatorColor)));
        internal static bool HasScrollBarColorOverrides => IsDarkMode && (DarkOverrides.ContainsKey(nameof(IThemeColors.ScrollBarTrackColor))
            || DarkOverrides.ContainsKey(nameof(IThemeColors.ScrollBarThumbColor)) || DarkOverrides.ContainsKey(nameof(IThemeColors.ScrollBarArrowColor)));

        /// <summary>Gets an allocation-free, read-only color reader that follows the current mode and overrides.</summary>
        public static IThemeColors Current => CurrentColors;

        /// <summary>Restores all defined dark colors without changing the selected color mode.</summary>
        public static void ResetColors() {
            DarkOverrides.Clear();
        }

        /// <summary>
        /// Sets a dark-only override keyed by an IThemeColors property name.
        /// Use nameof(IThemeColors.WindowColor), for example. Color.Empty restores the defined default.
        /// </summary>
        public static void SetDarkColor(string colorName, Color color) {
            ArgumentNullException.ThrowIfNull(colorName, nameof(colorName));
            if (!ColorMap.ContainsKey(colorName))
            {
                throw new ArgumentException("Unknown theme color.", nameof(colorName));
            }
            if (color.IsEmpty)
            {
                DarkOverrides.Remove(colorName);
            }
            else
            {
                DarkOverrides[colorName] = color;
            }
        }

        /// <summary>
        /// Gets or sets the application-wide ObjectListView color mode.
        /// System follows the app's WinForms mode on .NET 10. On Framework it samples
        /// the Windows preference initially and whenever this property is assigned System.
        /// </summary>
        public static ObjectListViewColorMode ColorMode {
            get => colorMode;
            set {
#if NETFRAMEWORK
                if (!Enum.IsDefined(typeof(ObjectListViewColorMode), value))
#else
                if (!Enum.IsDefined(value))
#endif
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

#if NETFRAMEWORK
                if (value == ObjectListViewColorMode.System)
                {
                    systemDarkMode = ReadSystemDarkMode();
                }
#endif

                colorMode = value;
            }
        }

        /// <summary>
        /// Gets whether the selected mode uses a dark palette. High contrast always takes precedence.
        /// </summary>
        public static bool IsDarkMode => !SystemInformation.HighContrast && (colorMode switch {
            ObjectListViewColorMode.Dark => true,
#if NET10_0_OR_GREATER
            ObjectListViewColorMode.System => Application.IsDarkModeEnabled,
#else
            ObjectListViewColorMode.System => systemDarkMode,
#endif
            _ => false
        });

#if NET10_0_OR_GREATER
        internal static bool SystemPrefersDark => !SystemInformation.HighContrast && ReadSystemDarkMode();
#endif

        internal static bool UsesCustomHeaderColors => !SystemInformation.HighContrast && (IsDarkMode || HasColorOverrides || NeedsNativeLightColors);
        private static bool UsesCustomEditorColors => IsDarkMode || HasColorOverrides || NeedsNativeLightColors;
        internal static HotItemStyle DefaultHotItemStyle => GetDefaultHotItemStyle();

        private static HotItemStyle GetDefaultHotItemStyle() {
            HotItemStyle style = IsDarkMode
                ? (darkHotItemStyle ??= new HotItemStyle())
                : (lightHotItemStyle ??= new HotItemStyle());
            style.BackColor = Current.HoverColor;
            return style;
        }

        internal static void ApplyCellEditorColors(Control editor) {
            if (!UsesCustomEditorColors)
            {
                return;
            }

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(editor);
            PropertyDescriptor backColor = properties[nameof(Control.BackColor)];
            PropertyDescriptor foreColor = properties[nameof(Control.ForeColor)];
            if (backColor != null && !backColor.ShouldSerializeValue(editor))
            {
                editor.BackColor = Current.InputColor;
            }

            if (foreColor != null && !foreColor.ShouldSerializeValue(editor))
            {
                editor.ForeColor = Current.TextColor;
            }
        }

        private static bool NeedsNativeLightColors =>
#if NET10_0_OR_GREATER
            !IsDarkMode && Application.IsDarkModeEnabled;
#else
            false;
#endif

        internal static Dictionary<string, Color> ResolveColors(bool dark, bool highContrast) {
            var colors = new Dictionary<string, Color>(ColorMap.Count, StringComparer.Ordinal);
            foreach (string colorName in ColorMap.Keys)
            {
                Color color = ResolveColor(colorName, dark, highContrast);
                colors.Add(colorName, color.IsSystemColor ? Color.FromArgb(color.ToArgb()) : color);
            }
            return colors;
        }

        internal static Color ResolveColor(string colorName, bool dark, bool highContrast) {
            if (!ColorMap.TryGetValue(colorName, out ColorDefinition definition))
            {
                throw new ArgumentException("Unknown theme color.", nameof(colorName));
            }
            if (!dark || highContrast)
            {
                return NativeColor(definition.Native);
            }
            if (DarkOverrides.TryGetValue(colorName, out Color color))
            {
                return color;
            }
            return definition.DarkBase != null ? ResolveColor(definition.DarkBase, true, false) : definition.Dark;
        }

        private readonly struct ColorDefinition {
            internal ColorDefinition(Color native, Color dark) {
                this.Native = native;
                this.Dark = dark;
                this.DarkBase = null;
            }

            internal ColorDefinition(Color native, string darkBase) {
                this.Native = native;
                this.Dark = Color.Empty;
                this.DarkBase = darkBase;
            }

            internal Color Native { get; }
            internal Color Dark { get; }
            internal string DarkBase { get; }
        }

        private static Color NativeColor(Color color) {
#if NET10_0_OR_GREATER
            if (Application.IsDarkModeEnabled)
            {
                // WinForms remaps SystemColors in dark mode; Windows still supplies native light/accessibility colors.
                return ColorTranslator.FromWin32(GetSysColor(ColorTranslator.ToOle(color) & 0xFF));
            }
#endif
            return color;
        }

#if NET10_0_OR_GREATER
        [DllImport("user32.dll")]
        private static extern int GetSysColor(int index);
#endif

        private static bool ReadSystemDarkMode() {
            try {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")) {
                    object value = key == null ? null : key.GetValue("AppsUseLightTheme");
                    return value is int && (int)value == 0;
                }
            } catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException || ex is IOException) {
                Trace.TraceWarning("ObjectListView could not read the Windows color preference: {0}", ex.Message);
                return false;
            }
        }
    }
}
