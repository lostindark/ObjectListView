# ObjectListView

ObjectListView is a Windows Forms control that presents model objects in a .NET `ListView`, with support for sorting, grouping, filtering, cell editing, and tree views.

## Documentation

The [documentation index](docs/README.md) includes the converted upstream guides, screenshots, recipes, and technical articles. GitHub renders these Markdown files directly; no documentation build is required.

- [Getting started](docs/getting-started.md)
- [Cookbook](docs/recipes.md)
- [Features](docs/features.md)
- [Examples](docs/samples.md)
- [FAQ](docs/faq.md)
- [Class reference](docs/major-classes.md)

## Build

Install Visual Studio 2022 or later with the .NET desktop development workload and the .NET Framework 4.6.2 targeting pack. From a Visual Studio Developer Command Prompt:

```powershell
msbuild ObjectListView.sln /restore /p:RestorePackagesConfig=true /p:Configuration=Debug /p:Platform="Any CPU"
```

The solution targets .NET Framework 4.6.2 and includes ObjectListView, its demos, ListViewPrinter, SparkleLibrary, and the NUnit tests. NuGet restores test dependencies into the ignored `packages` directory.

## License

See [COPYING](COPYING) for the GNU General Public License, version 3.
