# ObjectListView.WinForms

A Windows Forms ListView control with model-based rows, sorting, grouping,
filtering, checkboxes, cell editing, and tree views.
The namespace is `BrightIdeasSoftware`; the assembly is `ObjectListView.dll`.

This project is a fork of the
[original ObjectListView](https://objectlistview.sourceforge.net/cs/index.html).

**V3 is not binary compatible with V2:** users must fix code affected by the
breaking API changes and recompile their applications. Read the
[migration guide](https://github.com/lostindark/ObjectListView/blob/main/docs/v3-migration.md)
before upgrading.
V3 builds use the original signing key with assembly identity `3.0.0.0`.
Preview versions can still introduce documented breaking changes.

## Install

Install **ObjectListView.WinForms** using Visual Studio's NuGet Package Manager.
Enable **Include prerelease** for preview versions.

Supports **.NET Framework 4.6.2** and **.NET 10 for Windows**.
The package includes Visual Studio 2026 designer support; see the
[designer support guide](https://github.com/lostindark/ObjectListView/blob/main/docs/designer-support.md)
for current limitations.
The classic Framework designer is delivered separately from the runtime DLL and
is referenced automatically without copying it into application output.

## Documentation

- [What's New in v3](https://github.com/lostindark/ObjectListView/blob/main/docs/whats-new.md#version-3)
- [Getting started](https://github.com/lostindark/ObjectListView/blob/main/docs/getting-started.md)
- [Designer support](https://github.com/lostindark/ObjectListView/blob/main/docs/designer-support.md)
- [Features and examples](https://github.com/lostindark/ObjectListView/blob/main/docs/features.md)
- [Cookbook](https://github.com/lostindark/ObjectListView/blob/main/docs/recipes.md)
- [Class reference](https://github.com/lostindark/ObjectListView/blob/main/docs/major-classes.md)
- [Dark mode](https://github.com/lostindark/ObjectListView/blob/main/docs/dark-mode.md)
- [Documentation index](https://github.com/lostindark/ObjectListView/blob/main/docs/README.md)
- [Source, build instructions, and tests](https://github.com/lostindark/ObjectListView)
- [Report an issue](https://github.com/lostindark/ObjectListView/issues)

Licensed under the
[GNU General Public License, version 3](https://github.com/lostindark/ObjectListView/blob/main/LICENSE).
