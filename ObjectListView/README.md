# ObjectListView.WinForms

A maintained fork of ObjectListView for Windows Forms, with model-based rows,
sorting, grouping, filtering, checkboxes, cell editing, and tree views.
The namespace is `BrightIdeasSoftware`; the assembly is `ObjectListView.dll`.

## Requirements

Windows and .NET Framework 4.6.2 or later. This package does not provide a modern
.NET target or modern WinForms designer support.

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

## Documentation and support

- [Recipes](https://github.com/lostindark/ObjectListView/blob/main/docs/recipes.md)
- [Class reference](https://github.com/lostindark/ObjectListView/blob/main/docs/major-classes.md)
- [Report an issue](https://github.com/lostindark/ObjectListView/issues)

Licensed under the
[GNU General Public License, version 3](https://github.com/lostindark/ObjectListView/blob/main/LICENSE).
