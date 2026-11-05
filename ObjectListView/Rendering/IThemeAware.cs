namespace BrightIdeasSoftware
{
    /// <summary>Provides control-specific styling when applied by <see cref="FormTheme"/>.</summary>
    public interface IThemeAware
    {
        /// <summary>
        /// Applies the resolved palette after standard styling and child controls,
        /// before the optional per-control customization callback.
        /// Honor <see cref="IThemeColors.HighContrast"/> in custom rendering.
        /// </summary>
        void ApplyTheme(IThemeColors colors);
    }
}
