# ObjectListView.WinForms

A maintained fork of ObjectListView for Windows Forms, with model-based rows,
sorting, grouping, filtering, checkboxes, cell editing, and tree views.
The namespace is `BrightIdeasSoftware`; the assembly is `ObjectListView.dll`.

## Requirements

Windows with .NET Framework 4.6.2 or later, or .NET 10 for Windows. Modern
Designer SDK integration is included as a preview; complete modern designer
parity still requires interactive Visual Studio acceptance checks. Use Visual
Studio 2026 for modern designer evaluation. Runtime deployment does not require
Visual Studio or the Designer SDK. There is no .NET 8 target.
Native header-divider dragging in the modern designer is a known preview
limitation; set column widths through the Columns editor or Properties instead.

## Getting started

Install **ObjectListView.WinForms** using Visual Studio's NuGet Package Manager.
Enable **Include prerelease** to install a preview version.

With `BrightIdeasSoftware` and `System.Windows.Forms` imported, add this to a
form's constructor after `InitializeComponent()`:

```csharp
var list = new ObjectListView {
    Dock = DockStyle.Fill,
    View = View.Details,
    ShowGroups = false
};
list.Columns.Add(new OLVColumn("Name", "Name"));
this.Controls.Add(list);
list.SetObjects(new[] {
    new { Name = "Ada" },
    new { Name = "Grace" }
});
```

Use `TypedObjectListView<T>` for typed model access, `FastObjectListView` for
large lists, or `TreeListView` for hierarchical models.

## Upgrading from ObjectListView 2.x

Version 3 changes public APIs and requires recompiling consumers. In particular,
`TypedObjectListView<T>` is now a control rather than an adapter. See the
[migration guide](https://github.com/lostindark/ObjectListView/blob/main/docs/v3-migration.md)
before upgrading.

Saved layouts now use a versioned, runtime-independent format. Existing
BinaryFormatter layouts are rejected by `RestoreState()` and must be recreated.
`SaveState()` and `RestoreState(byte[])` retain their signatures, and new layouts
can be exchanged between the Framework and .NET 10 builds.

## Documentation and support

- [Recipes](https://github.com/lostindark/ObjectListView/blob/main/docs/recipes.md)
- [Class reference](https://github.com/lostindark/ObjectListView/blob/main/docs/major-classes.md)
- [Report an issue](https://github.com/lostindark/ObjectListView/issues)

Licensed under the
[GNU General Public License, version 3](https://github.com/lostindark/ObjectListView/blob/main/LICENSE).
