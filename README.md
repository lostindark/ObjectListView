# ObjectListView

[![CI](https://github.com/lostindark/ObjectListView/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/lostindark/ObjectListView/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/ObjectListView.WinForms)](https://www.nuget.org/packages/ObjectListView.WinForms)

ObjectListView is a Windows Forms control that presents model objects in a .NET `ListView`, with support for sorting, grouping, filtering, cell editing, and tree views.

## Version 3 development

ObjectListView v3 is a work in progress, not a completed release. It modernizes
the control API and internal design while preserving existing behavior and
source compatibility where practical. The priorities are clear collection
semantics, extensibility, designer support, and low-overhead model operations.

Implemented milestones include:

- Enumerable-friendly collection commands that accept LINQ queries and iterators.
- A real `TypedObjectListView<T>` control backed by `List<T>`, with shared UI
  behavior and `ObjectListView` as its non-generic `<object>` specialization.
- Snapshot collection getters and a read-only `Rows` view for inexpensive
  display-order access, including lazy virtual lists.
- Typed adapters for existing controls, unified model/state dispatch, and
  explicit source and storage extension points.

Existing fast, virtual, tree, and data-bound controls retain their specialized
data sources. Planned work includes simpler grouping configuration, unified
styling, and shared rendering/hit-testing layout.

The runtime projects target **.NET Framework 4.6.2 and .NET 10 for Windows**.
The Framework designer is retained. Modern Designer SDK integration is included,
but full modern designer support remains a preview until interactive Visual Studio
acceptance checks are complete. There is no .NET 8 target.
Native header-divider dragging in the modern designer remains a failing
acceptance check; use the Columns editor or Properties to change column widths.

V3 includes breaking API changes and requires recompiling consumers; it is not
a drop-in binary replacement. See the [migration guide](docs/v3-migration.md)
for changed contracts and examples, and the [implementation plan](docs/v3-plan.md)
for scope, milestones, and compatibility policy.

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

Install Visual Studio 2026 with the .NET desktop development workload, the .NET Framework 4.6.2 and 4.7.2 targeting packs, and the .NET 10 SDK. The 4.7.2 pack is for the modern designer's Visual Studio client, not a new consumer requirement. Use MSBuild 18 or later from a Visual Studio Developer Command Prompt:

```powershell
msbuild ObjectListView.sln /restore /p:Configuration=Debug /p:Platform="Any CPU"
```

The six runtime/test projects use SDK-style project files and target `net462;net10.0-windows`. The solution also builds the modern designer client, server, and protocol projects. Outputs are under each project's `bin\<Configuration>\<TargetFramework>` directory. Dependencies restore into the global NuGet cache.

Build the whole solution before packing `ObjectListView\ObjectListView.csproj`;
packing fails explicitly if required designer outputs are missing. Modern designer
assets are isolated under the package's `Design\WinForms` directories and are not
runtime dependencies. A plain DLL reference does not install these assets.

## Tests

On Windows, build the MSTest 4.5.1 suite with full-framework MSBuild, then run it with `dotnet test`:

```powershell
msbuild Tests\Tests.csproj /restore /p:Configuration=Release
dotnet test Tests\Tests.csproj --configuration Release --framework net462 --no-build --no-restore
dotnet test Tests\Tests.csproj --configuration Release --framework net10.0-windows --no-build --no-restore
```

Full-framework MSBuild compiles the existing Framework WinForms resources. Modern targets use preserialized resources supported by .NET 10, without a BinaryFormatter opt-in. The MSTest adapter supports both `dotnet test` and Visual Studio Test Explorer. UI tests use `STATestClass`, and assembly-level `DoNotParallelize` keeps execution serial. Run target frameworks sequentially, since clipboard state is shared. Each UI test gets a fresh, visually invisible WinForms fixture that does not activate or appear in the taskbar and is disposed after execution. The suite still requires a Windows UI-capable environment. The Framework-only DesignSurface test is not a substitute for modern Visual Studio designer checks.

To run a focused group:

```powershell
dotnet test Tests\Tests.csproj --configuration Release --framework net10.0-windows --no-build --no-restore --filter "FullyQualifiedName~TestMunger"
```

CI also verifies package contents, fresh package consumers, and layout interchange
between the two runtimes using `.github\scripts\Test-Package.ps1`.

## License

See [LICENSE](LICENSE) for the GNU General Public License, version 3.

[Original ObjectListView website](https://objectlistview.sourceforge.net/cs/index.html)
