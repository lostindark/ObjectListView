# ObjectListView

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

GitHub Actions builds the full solution in Release mode and runs the Windows
tests for pushes to `main`, pull requests, and manual CI runs. Successful runs
upload `ObjectListView.WinForms` NuGet packages as the `nuget-package` artifact;
test results are uploaded separately. The workflows use Windows Server 2025,
Visual Studio 2026 MSBuild, and the .NET 10 SDK to build the existing `net462`
target. They do not add a modern .NET target.

Package metadata is defined in `ObjectListView\ObjectListView.csproj`, replacing
the old upstream `ObjectListView.Official` NuGet manifest. To pack locally after
building Release:

```powershell
dotnet pack ObjectListView\ObjectListView.csproj --configuration Release --no-build --no-restore -p:PackageVersion=3.0.1-preview --output artifacts\packages
```

CI assigns `3.0.<CI run number>` automatically: `3.0.1`, `3.0.2`, and so on.
Each successful build packages both that version and its `-preview` variant
from the same tested binaries. Failed, cancelled, and pull request runs can
leave gaps in the publishable sequence; reruns retain their original number.
The package version is independent of the existing legacy assembly version.

Publishing is manual only and never rebuilds the package. By default it selects
the latest successful CI build for the selected branch; optionally supply a
specific **CI build number** (the run number, not the long run ID from its URL).
Only push and manual CI runs can be published, not pull request runs.
The **Preview** checkbox defaults to enabled: checked publishes `3.0.N-preview`,
unchecked publishes `3.0.N`. Republishing an existing version skips it rather
than overwriting it. CI artifacts must still be available; if expired, rerun CI
before publishing.

Before publishing:

1. Ensure your nuget.org account can publish `ObjectListView.WinForms`.
2. In GitHub repository **Settings > Environments**, create the `nuget`
   environment. Configure required reviewers and deployment branch/tag restrictions
   as appropriate, so package publishing requires approval.
3. Add an Actions variable named `NUGET_USER` containing your nuget.org profile
   username (not an email address), either at repository level or in `nuget`.
4. In nuget.org **Trusted Publishing**, add a GitHub policy with owner
   `lostindark`, repository `ObjectListView`, workflow filename `publish.yml`,
   and environment `nuget`. Scope it to `ObjectListView.WinForms`, allowing
   new packages for the first publication and new versions thereafter.

No long-lived NuGet API key is needed. The publishing job requests an OIDC token
only after CI passes and the environment's protection rules are satisfied.
To publish, open **Actions > Publish NuGet > Run workflow**, choose the intended
branch, optionally enter a build number, choose **Preview**, and run it.
Alternatively, use the GitHub CLI:

```powershell
gh workflow run publish.yml --ref main -f preview=true
gh workflow run publish.yml --ref main -f build-number=42 -f preview=false
```

## License

See [COPYING](COPYING) for the GNU General Public License, version 3.
