# Dark mode

[Documentation index](README.md) | [Getting started](getting-started.md)

Configure the palette before creating controls on either runtime:

```csharp
ObjectListViewTheme.ColorMode = ObjectListViewColorMode.System; // Or Light / Dark
```

`Default` follows WinForms on .NET 10 and preserves classic Framework behavior.
`System` samples the Windows preference when assigned; high contrast takes precedence.
The palette uses stock .NET 10 colors with Framework fallbacks. Explicit colors,
custom renderers, and unrelated host controls remain application-controlled.

Both main-demo builds offer **Follow system**, **Light**, and **Dark** in the
status-bar Theme menu. Switching reloads examples and resets edits, preserving
window position and the selected tab. Native chrome support depends on Windows.
