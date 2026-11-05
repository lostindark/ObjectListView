# Dark mode

[Documentation index](README.md) | [Getting started](getting-started.md)

Configure the palette before creating controls on either runtime:

```csharp
ObjectListViewTheme.ColorMode = ObjectListViewColorMode.System; // Or Light / Dark
```

`System` is the default automatic mode. On .NET 10 it follows the application's
current WinForms mode without changing it. On Framework it samples Windows'
preference initially and whenever System is assigned. `Light` and `Dark` are the
only explicit modes; high contrast always takes precedence.
Light and high contrast use native Windows/WinForms colors on both runtimes, with
no shared color overrides. Dark custom drawing uses the defined dark colors on
both runtimes and allows overrides. Standard controls retain native dark rendering
on .NET 10 where supported; Framework uses the dark rendering fallbacks.
Explicit per-control styles, custom renderers, and unrelated host controls remain
application-controlled.

Both main-demo builds offer **Follow system**, **Light**, and **Dark** in the
status-bar Theme menu. Switching reloads examples and resets edits, preserving
window position and the selected tab. Native chrome support depends on Windows.
For dark startup and theme reloads, the demo opts into shared first-frame
preparation rather than implementing its own form-display override.

Both ListViewPrinter demo builds also use the shared form service. Their
settings, property grid, information banner, and list follow the UI palette;
Framework tabs reuse the shared buffered fallback. Report formatting and printed
page colors remain independent of the UI theme.

## Design and ownership

Theming has three independent layers:

| Layer | Responsibility |
| --- | --- |
| `ObjectListViewTheme` | Application-wide OLV mode and shared color roles; no host-control traversal or WinForms application-mode changes |
| `ObjectListViewBase.RefreshTheme()` | Reapply that palette to one existing list, including native colors, headers, scrollbar composition, and formatted rows |
| `FormTheme` | Optional standard-control styling, system notifications, and extension callbacks for an opted-in form or subtree |

Applications can use their own theme manager without attaching `FormTheme`.
Its standard-control support is a convenience, not a prerequisite for OLV.
The palette is application-wide, not a separate theme per list or form.
First-frame presentation is also independent: `FormFirstFrame` can be used without
the palette or form service.

Color mode and rendering backend are separate decisions:

| Mode | Standard controls | OLV/custom drawing | Shared overrides |
| --- | --- | --- | --- |
| Light | Native | System-defined colors | None |
| Dark | Native on supported .NET 10 hosts; fallback on Framework | Defined dark colors, independent of backend | Dark only |
| High contrast | Native accessibility rendering | System-defined colors | None |

One central map uses `nameof(IThemeColors.WindowColor)`-style keys for each
named color's system value and dark default (or another dark color). A single
dark override dictionary uses the same keys. There is no color-role enum,
separate light RGB table, or fallback theme.

`ThemePalette` is the single `IThemeColors` implementation. It backs the live
`Current` reader and, when publicly constructed, an immutable snapshot. Both use
the same resolution function; creating a snapshot does not change the configured
mode or the host's WinForms mode. Attached forms resample snapshots on Windows
color/theme notifications, even when the effective Light/Dark mode is unchanged.

Built-in alternate-row, hover, and light hyperlink defaults now map to native
semantic roles rather than illustrative yellow/blue colors. Explicit application
styles and the demo's intentionally illustrative colors are preserved.

Demo default styling also consumes this map without Light/Dark color branches.
Business-card defaults read the current control/text colors at draw time; explicit
renderer colors still take precedence. Information banners and positive/negative
text use `InformationBackColor`, `InformationTextColor`, `PositiveTextColor`, and
`NegativeTextColor`. Tabs implement `IThemeAware` so these roles update on refresh.
Alternate rows use the control's theme-bound default instead of assigning a
resolved color during construction.

## Using another theme manager

Translate the application's mode and colors into the OLV palette, then refresh
each existing list on its UI thread:

```csharp
ObjectListViewTheme.ColorMode = appUsesDarkMode
    ? ObjectListViewColorMode.Dark : ObjectListViewColorMode.Light;
ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.WindowColor), appDarkContentBackground);
ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.TextColor), appDarkTextColor);
objectListView.RefreshTheme();
```

Dark colors can be configured in any mode; they take effect only in Dark.
Switching to Light or high contrast ignores them without discarding them.

`RefreshTheme()` does not attach a form service, change `Application.ColorMode`,
or style unrelated controls. It preserves the list's explicit colors, custom
renderers, owner-drawing configuration, selection, and models. The host remains
responsible for detecting theme changes and refreshing its lists. Framework hosts
using System should reassign `ObjectListViewTheme.ColorMode` to resample Windows'
preference before refreshing. On .NET 10, System reads the host's WinForms mode
directly. Lists created afterward use the current palette automatically.
Explicit Light remains light even inside a native-dark .NET 10 host: OLV reads
Windows' native light system colors without changing the host application mode.
The default empty-list message also refreshes its theme-owned text, background,
and border colors. Explicit assignments to any of those colors remain
application-owned, even if they equal the previous default; replacement overlays
are left untouched.

## Reading colors for custom rendering

`IThemeColors` exposes every defined color as a named read-only property:

```csharp
IThemeColors colors = ObjectListViewTheme.Current;
Color background = colors.WindowColor;
Color foreground = colors.TextColor;
Color border = colors.ControlDarkColor;
```

In Light these are system colors; in Dark they are the defined dark colors plus
dark overrides; in high contrast they are Windows accessibility colors.
`Current` is a live, allocation-free reader: a retained reference follows later
mode and override changes. Reading colors does not require `FormTheme` and does
not change the host's WinForms mode. For stable colors across a rendering operation
or background task, capture an explicit immutable snapshot:

```csharp
IThemeColors snapshot = new ThemePalette(colors.Dark, colors.HighContrast);
```

`ThemePalette` implements the same interface, so `IThemeAware` controls and
`FormTheme` callbacks can pass their supplied palette to shared rendering code.

## Shared form theming

Form theming is optional. Configure the mode before creating controls, then
attach the shared service after `InitializeComponent`:

```csharp
#if NET10_0_OR_GREATER
// The host opts into Windows preference tracking.
Application.SetColorMode(SystemColorMode.System);
#endif
FormTheme.SetColorMode(ObjectListViewColorMode.System);

// In the form constructor, after InitializeComponent():
FormTheme.Attach(this);
```

The service styles standard controls and menus, tracks newly added controls and
Windows theme notifications, and reapplies styling after handle recreation.
Disposing the form detaches it automatically. Explicit control colors are
preserved, except when high contrast requires Windows accessibility colors.
`FormTheme.Apply(control)` can also style a standalone control subtree.

### Preparing the first dark frame

First-frame preparation is opt-in. Enable it before the form's first `Load` or
`Show` to avoid exposing incomplete light control surfaces during dark startup:

```csharp
FormTheme.Attach(this, prepareFirstFrame: true);
```

The shared helper temporarily suppresses opacity around the first native show
message and paints the visible control hierarchy before restoring the configured
opacity in a `finally` block. An aborted `Load` cannot leave a surviving form
transparent. Owned chrome is refreshed at `Shown`. There is no timer or fixed
startup delay. Light mode, explicitly transparent forms, and subsequent hide/show
operations are unchanged.

Applications using another theme manager can opt into the same presentation
behavior without attaching `FormTheme` or changing the OLV palette:

```csharp
FormFirstFrame.Attach(this, () => myTheme.IsDark);
```

The predicate is sampled before the first native presentation; omit it to prepare unconditionally.
Handlers detach automatically after the first display or form disposal. Dispose
the returned helper to cancel preparation and restore any pending opacity.

### Styling ownership

Explicit system-color assignments such as `SystemColors.Info` are application
styles too; they are not treated as unconfigured defaults. Later `BackColor` and
`ForeColor` changes also remain application-owned across refreshes and detach.
Assign `Color.Empty` to return those properties to theme defaults, or use the
customization callback for theme-dependent changes. Light defaults are resolved
independently even when the host retains native WinForms dark mode.

Application-owned ToolStrip renderers (including a custom `ToolStripManager`
renderer) and owner-drawn combo boxes retain their rendering. Configure these
before attachment; the service supplies combo item painting only for controls
that originally use `DrawMode.Normal`. Custom renderers remain responsible for
their own theme and high-contrast behavior.

Removing a control subtree detaches its handlers and restores its captured
appearance before another form service can take ownership. Shared context menus
remain themed while another tracked control still references them. Disposing the service
does the same for all controls and generated menus it still tracks, without
disposing the controls themselves. Tracking does not keep otherwise unused
controls or menus alive.

On .NET 10, `FormTheme.SetColorMode(Light/Dark)` also configures the native WinForms
application mode. Selecting System preserves the host's existing mode. To follow
Windows rather than an explicitly selected host mode, the application opts into
`Application.SetColorMode(SystemColorMode.System)` before creating controls.
Attached forms resample that Windows preference on system notifications when
the host has chosen WinForms' System mode. The demo makes this host-level choice
for its **Follow system** menu.
When native dark mode is enabled, the service retains native
button and combo-box rendering rather than replacing it with Framework-compatible
rendering. Dropdown-only combo boxes use a flat frame so their closed face respects
the palette instead of retaining a white native surface. Their selection and
dropdown behavior remain native. Assigning `ObjectListViewTheme.ColorMode` directly still changes only
the list palette. Some native dialogs and control chrome remain Windows-controlled.
The Framework tab fallback implements `IThemeAware`, enabling buffered rendering
when Dark becomes active and restoring native drawing/layout on Light or high
contrast transitions. Initial fallback activation is deferred until control
creation or explicit theme application, so configured native padding and owner
drawing are captured after application initialization.

Title-bar styling probes Windows' dark-mode attribute directly, so Framework hosts
without a Windows-version compatibility manifest can still use it when supported.
Built-in filter checklists share OLV's checkbox palette, and submenu arrows use
the menu foreground rather than an unreadable default black.

Framework-compatible disabled-caption painting is limited to text-only button
layouts. Image/text buttons retain their native painting; application-specific
image layouts and other unsupported controls can be extended with the callback.

## Extending control support

Implement `IThemeAware` on custom controls to receive the resolved palette
automatically, without registering each control type:

```csharp
public class MyDetailsPanel : Panel, IThemeAware
{
    public void ApplyTheme(IThemeColors palette)
    {
        BackColor = palette.WindowColor;
        ForeColor = palette.TextColor;
    }
}
```

`ApplyTheme` runs after standard styling and child controls, including on newly
added controls and every theme refresh. Honor `palette.HighContrast` in custom
rendering.
Theme-aware controls, customization callbacks, and menu renderer adapters consume
`IThemeColors`; only snapshot creation requires the concrete `ThemePalette` type.
Explicit data-formatting examples may still choose colors from their data and
adjust contrast against the actual background. That is widget-state formatting,
not a second theme-selection policy.

Extensions receive the same palette as built-in controls. They do not need to
replace the traversal or register a second theme service. Configure owner-drawn
controls before attachment so their drawing remains application-owned; use the
callback for visual roles or controls that cannot implement `IThemeAware`.

The optional customization callback runs **after** `IThemeAware.ApplyTheme`,
so it can further override styling. Use it for third-party controls you cannot
modify, or application-specific visual roles:

```csharp
FormTheme.Attach(this, customizeControl: (control, palette) =>
{
    if (control is MyDetailsPanel details)
    {
        details.ApplyColors(palette.ControlColor, palette.TextColor, palette.LinkColor);
    }
}, themeChanged: palette => UpdateStatusColors(palette));
```

The per-control callback also runs for newly added controls and every theme refresh. The final
`themeChanged` callback runs after the complete subtree, allowing applications
to refresh formatted content without duplicating control traversal or native
window handling. Honor `palette.HighContrast` in custom rendering.

## Custom dark colors

Override any subset of the dark roles; unspecified roles retain their defined
dark defaults:

```csharp
ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.WindowColor), Color.FromArgb(25, 30, 45));
ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.ControlColor), Color.FromArgb(20, 24, 35));
ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.TextColor), Color.Gainsboro);
ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.HighlightColor), Color.DarkSlateBlue);
ObjectListViewTheme.SetDarkColor(nameof(IThemeColors.HighlightTextColor), Color.White);
FormTheme.RefreshAll();
```

Overrides are shared by ObjectListView rendering and the form service. They
apply only in Dark. Use `nameof(IThemeColors.WindowColor)`, for example, as the
key; unknown names are rejected and name matching is ordinal/case-sensitive.
Pass `Color.Empty` to `SetDarkColor` to clear one override, or call `ResetColors()`
to restore all dark defaults. Derived dark colors follow their base color unless
separately overridden.
Per-control explicit styles and customization callbacks remain application-owned;
they are separate from shared theme overrides. Light and high contrast never use
the dark override dictionary.
For hosts using another theme manager, call `RefreshTheme()` on their lists
instead of `FormTheme.RefreshAll()`, which refreshes only opted-in roots.

Native group captions use a distinct accent and a subtle separator in dark
mode. Use `nameof(IThemeColors.GroupHeaderTextColor)` and
`nameof(IThemeColors.GroupHeaderSeparatorColor)` with `SetDarkColor` to override
them. The Framework-compatible scrollbar fallback also uses `ScrollBarTrackColor`,
`ScrollBarThumbColor`, and `ScrollBarArrowColor`. It uses the library's existing
click-through glass-panel infrastructure to composite opaque scrollbar pixels
above native painting, rather than repeatedly painting over a native scrollbar.
Windows still owns scrollbar geometry, input, and accessibility. The fallback
leaves native .NET 10 dark scrollbars alone unless scrollbar colors are overridden.
High contrast always retains Windows rendering.
Composed scrollbar surfaces follow ancestor movement, resizing, and scrolling,
including the visible clipping region; those subscriptions detach with the surface.

The thumb stays slim and arrows stay hidden while idle. Entering the native
track expands the thumb and reveals both arrows; native hit areas are unchanged.
Attached forms also apply the same scrollbar
composition to multiline text controls, including read-only rich-text details
panels, and to standard scrollbar controls, including DataGridView's child
scrollbars and their intersection. Native selection, caret behavior, and scrolling
are retained.
