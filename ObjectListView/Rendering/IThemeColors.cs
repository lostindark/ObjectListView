using System.Drawing;

namespace BrightIdeasSoftware
{
    /// <summary>A resolved, read-only color context for control styling and custom rendering.</summary>
    public interface IThemeColors
    {
        /// <summary>Gets whether the context uses the defined dark colors.</summary>
        bool Dark { get; }
        /// <summary>Gets whether Windows accessibility colors take precedence.</summary>
        bool HighContrast { get; }
        /// <summary>Gets the list and content background.</summary>
        Color WindowColor { get; }
        /// <summary>Gets the normal foreground.</summary>
        Color TextColor { get; }
        /// <summary>Gets the form, container, header, and alternate-row background.</summary>
        Color ControlColor { get; }
        /// <summary>Gets the text-input background.</summary>
        Color InputColor { get; }
        /// <summary>Gets the light control surface.</summary>
        Color ControlLightColor { get; }
        /// <summary>Gets the focused-input surface.</summary>
        Color ControlLightLightColor { get; }
        /// <summary>Gets the control border and separator.</summary>
        Color ControlDarkColor { get; }
        /// <summary>Gets the muted and disabled foreground.</summary>
        Color GrayTextColor { get; }
        /// <summary>Gets the selected background.</summary>
        Color HighlightColor { get; }
        /// <summary>Gets the selected foreground.</summary>
        Color HighlightTextColor { get; }
        /// <summary>Gets the focus, checkbox, and active-tab accent.</summary>
        Color AccentColor { get; }
        /// <summary>Gets the normal button background.</summary>
        Color ButtonColor { get; }
        /// <summary>Gets the disabled-button background.</summary>
        Color ButtonDisabledColor { get; }
        /// <summary>Gets the hovered-button background.</summary>
        Color ButtonHoverColor { get; }
        /// <summary>Gets the pressed-button background.</summary>
        Color ButtonPressedColor { get; }
        /// <summary>Gets the button foreground.</summary>
        Color ButtonTextColor { get; }
        /// <summary>Gets the disabled-button foreground.</summary>
        Color ButtonDisabledTextColor { get; }
        /// <summary>Gets the hovered-row background.</summary>
        Color HoverColor { get; }
        /// <summary>Gets the inactive selection background.</summary>
        Color InactiveSelectionColor { get; }
        /// <summary>Gets the inactive selection foreground.</summary>
        Color InactiveSelectionTextColor { get; }
        /// <summary>Gets the description and secondary foreground.</summary>
        Color SecondaryTextColor { get; }
        /// <summary>Gets the foreground for positive or successful content.</summary>
        Color PositiveTextColor { get; }
        /// <summary>Gets the foreground for negative or missing content.</summary>
        Color NegativeTextColor { get; }
        /// <summary>Gets the information-panel background.</summary>
        Color InformationBackColor { get; }
        /// <summary>Gets the information-panel foreground.</summary>
        Color InformationTextColor { get; }
        /// <summary>Gets the hyperlink foreground.</summary>
        Color LinkColor { get; }
        /// <summary>Gets the visited hyperlink foreground.</summary>
        Color VisitedLinkColor { get; }
        /// <summary>Gets the menu background.</summary>
        Color MenuColor { get; }
        /// <summary>Gets the selected menu background.</summary>
        Color MenuHighlightColor { get; }
        /// <summary>Gets the native group-caption foreground.</summary>
        Color GroupHeaderTextColor { get; }
        /// <summary>Gets the native group-caption separator.</summary>
        Color GroupHeaderSeparatorColor { get; }
        /// <summary>Gets the scrollbar track and corner fill.</summary>
        Color ScrollBarTrackColor { get; }
        /// <summary>Gets the scrollbar thumb color.</summary>
        Color ScrollBarThumbColor { get; }
        /// <summary>Gets the scrollbar arrow color.</summary>
        Color ScrollBarArrowColor { get; }
        /// <summary>Gets the empty-list message foreground.</summary>
        Color EmptyListTextColor { get; }
        /// <summary>Gets the empty-list message background.</summary>
        Color EmptyListBackgroundColor { get; }
    }
}
