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
- [v3 implementation plan](docs/v3-plan.md)

## Build

Install Visual Studio with the .NET desktop development workload, the .NET Framework 4.6.2 targeting pack, and a compatible .NET SDK. The .NET 10 SDK requires MSBuild 18 or later (Visual Studio 2026). From a Visual Studio Developer Command Prompt:

```powershell
msbuild ObjectListView.sln /restore /p:Configuration=Debug /p:Platform="Any CPU"
```

All six projects use SDK-style project files and target .NET Framework 4.6.2. The solution includes ObjectListView, its demos, ListViewPrinter, SparkleLibrary, and the MSTest tests. Build outputs are under each project's `bin\<Configuration>\net462` directory. Test dependencies use `PackageReference` and restore into the global NuGet cache; no repository-local `packages` directory is needed.

## Tests

On Windows, build the MSTest 4.5.1 suite with full-framework MSBuild, then run it with `dotnet test`:

```powershell
msbuild Tests\Tests.csproj /restore /p:Configuration=Release
dotnet test Tests\Tests.csproj --configuration Release --no-build --no-restore
```

Full-framework MSBuild is required to compile the existing non-string WinForms resources without adding a runtime resource-reader dependency. The MSTest adapter supports both `dotnet test` and Visual Studio Test Explorer. Test classes use `STATestClass`, and assembly-level `DoNotParallelize` keeps execution serial. Each UI test gets a fresh, visually invisible WinForms fixture that does not activate or appear in the taskbar and is disposed after execution. The suite still requires a Windows UI-capable environment.

To run a focused group:

```powershell
dotnet test Tests\Tests.csproj --configuration Release --no-build --no-restore --filter "FullyQualifiedName~TestMunger"
```

## License

See [COPYING](COPYING) for the GNU General Public License, version 3.
