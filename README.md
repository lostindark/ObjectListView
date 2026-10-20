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

The current target remains **.NET Framework 4.6.2** with the existing WinForms
designer. **.NET 8+ runtime and modern designer support are deferred.**

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

## CI and NuGet releases

GitHub Actions builds and tests the solution on Windows for pushes to `main`,
pull requests, and manual CI runs. Successful builds upload test results and
`ObjectListView.WinForms` packages numbered `3.0.<CI run number>`, with matching
`-preview` packages. Build numbers may have gaps.

Publishing to [nuget.org](https://www.nuget.org/packages/ObjectListView.WinForms)
is manual: open **Actions > Publish NuGet > Run workflow**, select a branch, and
optionally enter a CI build number. Leave it empty for the latest successful
build. **Preview** defaults to enabled; uncheck it to publish `3.0.N` instead of
`3.0.N-preview`. Publishing reuses CI artifacts; pull request builds are excluded.
The workflow tags the CI build's commit as `v3.0.N` or `v3.0.N-preview` and prepares
a draft GitHub Release with generated change notes and the package attached.
It publishes to NuGet before publishing the GitHub Release; if NuGet fails, the
release stays a draft. Preview versions become prereleases. Reruns preserve
existing tags and release notes.

One-time setup for maintainers (OIDC; no permanent API key):

1. Create a GitHub environment named `nuget`, with approval rules as needed.
2. Set the Actions variable `NUGET_USER` to your nuget.org username.
3. Add a nuget.org **Trusted Publishing** policy for `lostindark/ObjectListView`,
   workflow `publish.yml`, environment `nuget`, and package `ObjectListView.WinForms`.
   Allow publishing new packages and new versions.

## License

See [LICENSE](LICENSE) for the GNU General Public License, version 3.
