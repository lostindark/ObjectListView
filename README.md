# ObjectListView

[![CI](https://github.com/lostindark/ObjectListView/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/lostindark/ObjectListView/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/ObjectListView.WinForms)](https://www.nuget.org/packages/ObjectListView.WinForms)

ObjectListView is a Windows Forms control that presents model objects in a .NET
`ListView`, with sorting, grouping, filtering, cell editing, and tree views.

This repository is a fork of the
[original ObjectListView](https://objectlistview.sourceforge.net/cs/index.html).

## Version 3 development

**V3 is a work in progress, not a completed release.** It targets
**.NET Framework 4.6.2 and .NET 10 for Windows** and introduces breaking API changes
that require updating consumer code. **V3 is not binary compatible with V2:**
users must fix code affected by the breaking changes and recompile their applications.

See the [implementation plan](docs/v3-plan.md) for progress and the
[migration guide](docs/v3-migration.md) before upgrading.

## Documentation

- [Installation and package overview](ObjectListView/README.md)
- [Getting started](docs/getting-started.md)
- [Designer support](docs/designer-support.md)
- [Dark mode](docs/dark-mode.md)
- [Documentation index](docs/README.md) — features, examples, cookbook, and class reference
- [Report an issue](https://github.com/lostindark/ObjectListView/issues)

## Build

Install Visual Studio 2026's .NET desktop workload, the .NET 10 SDK, and the
.NET Framework 4.6.2 and 4.7.2 targeting packs (4.7.2 is for the designer client).
From a Visual Studio Developer Command Prompt using MSBuild 18 or later:

```powershell
msbuild ObjectListView.sln /restore /p:Configuration=Debug /p:Platform="Any CPU"
```

Outputs are in each project's `bin\<Configuration>\<TargetFramework>` directory.
All projects use C# 14, including the .NET Framework targets.
Build the whole solution before packing `ObjectListView\ObjectListView.csproj`
to include the required designer assets.

## Source code analysis

`Directory.Build.props` enables the .NET 10 SDK's recommended .NET analyzers for
all projects and target frameworks, both during builds and live in Visual Studio.
Analyzer diagnostics are warnings by default; no additional analyzer package is
required. The shared settings are available under Solution Items.

## Tests

Build with full-framework MSBuild, then run the Windows UI tests sequentially:

```powershell
msbuild Tests\Tests.csproj /restore /p:Configuration=Release
dotnet test Tests\Tests.csproj --configuration Release --framework net462 --no-build --no-restore
dotnet test Tests\Tests.csproj --configuration Release --framework net10.0-windows --no-build --no-restore
```

Add `--filter "FullyQualifiedName~TestMunger"` to run a focused group.
CI also checks package contents and fresh consumers with PowerShell 7 and
`.github\scripts\Test-Package.ps1`. Modern designer acceptance still requires
interactive Visual Studio checks. The script leaves a reopenable, version-pinned
consumer solution under `artifacts\PackageSmoke\consumer`; see the
[designer acceptance steps](docs/v3-plan.md#continuing-an-iteration).

## License

See [LICENSE](LICENSE) for the GNU General Public License, version 3.
