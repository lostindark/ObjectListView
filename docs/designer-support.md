# Windows Forms designer support

[Documentation index](README.md) | [Getting started](getting-started.md) | [Migration guide](v3-migration.md)

V3 provides design-time integration for **.NET Framework 4.6.2** and
**.NET 10 for Windows**. The verified IDE is **Visual Studio 2026** with the
.NET desktop development workload. V3 is still in development.

## Supported configurations

| Consumer | .NET Framework | .NET 10 |
| --- | --- | --- |
| NuGet `ObjectListView.WinForms` | Designer included; also verified with a `net48` host | Client/server designer included |
| Runtime project reference | Import the Framework designer target below | Runtime supported; custom designer discovery is not supported |
| Explicit runtime DLL reference | Also reference the matching Framework designer DLL, with Copy Local disabled | Does not install the custom designer |

Rendering a control in the designer does not by itself prove that its custom
designer, column editor, or smart tags have loaded. Older Visual Studio versions,
ARM64, and every Windows/DPI configuration are not covered by the current
acceptance checks.

## Setup

### NuGet consumers

Install **ObjectListView.WinForms** into the Windows Forms application using
Visual Studio's NuGet Package Manager. Enable **Include prerelease** when choosing
a preview version. Restore packages and build the application before opening
its form designer.

The package contains the matching runtime and designer assets; no separate
designer package is needed. Framework consumers receive a non-copy-local
designer reference through the package's `build`/`buildTransitive` targets.
Modern .NET uses the packaged `Design\WinForms` client/server layout.

### Framework source/project references

Keep the runtime reference and import the supplied target in the consuming
application's project file. Adjust these paths relative to that project:

```xml
<ItemGroup>
  <ProjectReference Include="..\ObjectListView\ObjectListView.csproj" />
</ItemGroup>
<Import Project="..\ObjectListView.Design.Framework\ObjectListView.Design.Framework.targets" />
```

The import adds a reference to the `net462` designer project only for Framework
targets, including compatible `net48` hosts. It sets `Private="false"` and
`PrivateAssets="all"` so this design-time dependency is not copied into the
application output or propagated as a package dependency. Runtime-only consumers
do not need the import. The repository demos already include it.

For explicit DLL references, reference both the matching runtime
`ObjectListView.dll` and `ObjectListView.Design.Framework.dll`, setting **Copy
Local = False** on the designer reference. Projects deriving from the moved
designer/editor classes also need that assembly reference.

This Framework import does not enable modern project-reference-only designer
discovery. Use NuGet for the .NET 10 custom designer.

## Using the designer

Select the control and use its **Columns** editor to add, remove, and configure
`OLVColumn` components. Smart-tag options include column editing, **View**, and
image lists. Save the form to serialize changes into the normal designer code.

For header resizing, choose **Details** view with a visible header, select the
control, and drag a column divider. Each completed gesture records one Undo/Redo
operation. **Escape** cancels the preview and restores the original width;
losing capture or destroying the control's handle also cancels the gesture.
Fixed-width and free-space-filling columns are not draggable, and previews
respect column width limits.

ListViewPrinter brush and pen property editors use modal dialogs. Accepting
applies the edited value; Cancel leaves the original unchanged. If using
ListViewPrinter, reference its matching runtime assembly from its normal build
output, not from a designer directory.

## How it works

The classic Framework and modern .NET designers are separate implementations,
not two names for the same assembly:

| Project | Target | Responsibility |
| --- | --- | --- |
| `ObjectListView.Design.Framework` | `net462` | Classic in-process designer and column collection editor |
| `ObjectListView.Design.Client` | `net472` | Modern designer's Visual Studio-side editors |
| `ObjectListView.Design.Server` | `net10.0-windows` | Modern designer beside the actual controls in the design server |
| `ObjectListView.Design.Protocol` | `net472;net10.0-windows` | Modern designer communication contracts |

```text
Classic Framework designer host
  Consumer -> ObjectListView.Design.Framework -> ObjectListView runtime
           -> ObjectListView runtime

Modern designer
  Visual Studio                         .NET 10 design-server process
    Design.Client <--- SDK/protocol ---> Design.Server -> runtime controls
```

Framework runtime metadata names the external designer/editor assembly without
referencing it as a runtime dependency. Runtime-used overlay converters remain
in `ObjectListView.dll`. Neither runtime target depends on `System.Design`,
`WindowsBase`, or the Designer SDK.

The modern client targets Framework because it runs on the Visual Studio side;
this does **not** raise the application's runtime requirement. It must not
reference or ship Framework ObjectListView/ListViewPrinter runtime DLLs alongside
the modern designer: those copies can leak into the server's load context and
create incompatible runtime type identities.

Instead, shared printer-editor sources are compiled under
`BrightIdeasSoftware.Design.Client.Printing` using `OLV_DESIGN_CLIENT`. That
symbol identifies the client build role; `NETFRAMEWORK` cannot distinguish the
client from the classic runtime. Printer edits cross the boundary as copied
state, not client-only types serialized into application code.

Header glyphs and resize transactions are linked from
`ObjectListView.Design.Shared` into the host-specific projects. Framework Escape
handling uses a UI-thread-scoped message hook only while a resize is active;
it is not a global keyboard hook.

### Package layout and deployment

```text
lib\
  net462\
    ObjectListView.dll
    Design\WinForms\ObjectListView.Design.Framework.dll
  net10.0-windows7.0\
    ObjectListView.dll
    Design\WinForms\
      ObjectListView.Design.Client.dll
      ObjectListView.Design.Protocol.dll
      Server\
        ObjectListView.Design.Server.dll
        ObjectListView.Design.Protocol.dll
build\ObjectListView.WinForms.targets
buildTransitive\ObjectListView.WinForms.targets
```

Designer assemblies are development assets, not normal application deployment
dependencies. Do not copy the `Design` directories into the application's output
or publish directory, or add Designer SDK reference assemblies to the application.
Code that intentionally invokes design-time editors at runtime must explicitly
deploy their dependencies; that is outside normal designer use.

## Troubleshooting and verification

| Symptom | Check |
| --- | --- |
| Control renders but custom editing is missing | Confirm the supported reference setup above, restore/build, and reopen the form designer |
| Framework designer cannot be found | Check that `ObjectListView.Design.Framework` resolves as a reference and Copy Local remains disabled; source consumers need the target import |
| Modern project reference lacks smart tags/editors | Use the NuGet package; a runtime reference alone does not install its client/server assets |
| Cast errors between identically named ObjectListView types | Remove manually staged Framework runtime DLLs from modern designer directories and use matching package assets |
| Header divider cannot be dragged | Select the control, use Details view with a visible header, and check fixed/fill width settings |
| Designer DLL cannot load from a deeply nested cache | Try a shorter package-cache path; the Framework client has encountered MAX_PATH loading limits |

Build the whole solution before packing the runtime project, since packing
collects the separately built designer assets. See the [build instructions](../README.md#build).

Automated delivery checks live in `.github\scripts\Test-Package.ps1` and
`.github\scripts\Test-ProjectReference.ps1`. They check resolved designer
references, package isolation, runtime consumers, and clean application/publish
output. Framework source checks cover `net462` and `net48`; package checks also
cover a legacy non-SDK consumer.

Interactive acceptance is still necessary: resize and save a width, verify one
Undo/Redo operation, cancel another resize with Escape, change View and return to
Details, edit columns, and accept/cancel printer edits. Rebuild and run the saved
form afterward. The [implementation plan](v3-plan.md#nuget-designer-acceptance)
records the current acceptance scope; automated rendering is not a substitute
for exercising Visual Studio's actual designer.
