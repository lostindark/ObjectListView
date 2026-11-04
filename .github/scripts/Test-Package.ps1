#Requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$PackageDirectory,
    [Parameter(Mandatory)][string]$PackageVersion,
    [string]$OutputDirectory = 'artifacts\PackageSmoke'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Reflection.Metadata
$packageRoot = (Resolve-Path -LiteralPath $PackageDirectory).Path
$package = Join-Path $packageRoot "ObjectListView.WinForms.$PackageVersion.nupkg"
$archive = [System.IO.Compression.ZipFile]::OpenRead($package)
try {
    $files = @($archive.Entries | ForEach-Object FullName)
    foreach ($required in @(
        'README.md', 'LICENSE',
        'lib/net462/ObjectListView.dll', 'lib/net462/ObjectListView.xml',
        'lib/net462/Design/WinForms/ObjectListView.Design.Framework.dll',
        'buildTransitive/ObjectListView.WinForms.targets',
        'build/ObjectListView.WinForms.targets',
        'lib/net10.0-windows7.0/ObjectListView.dll', 'lib/net10.0-windows7.0/ObjectListView.xml',
        'lib/net10.0-windows7.0/Design/WinForms/ObjectListView.Design.Client.dll',
        'lib/net10.0-windows7.0/Design/WinForms/ObjectListView.Design.Protocol.dll',
        'lib/net10.0-windows7.0/Design/WinForms/Server/ObjectListView.Design.Server.dll',
        'lib/net10.0-windows7.0/Design/WinForms/Server/ObjectListView.Design.Protocol.dll'
    )) {
        if ($required -notin $files) { throw "Missing package asset: $required" }
    }
    if ($files | Where-Object { $_ -match 'Microsoft\.DotNet\.DesignTools.*\.dll$' }) {
        throw 'Designer SDK reference assemblies must not be redistributed.'
    }
    if ($files | Where-Object { $_ -match '/Design/WinForms/(Server/)?(ObjectListView|ListViewPrinter)\.dll$' }) {
        throw 'Runtime assemblies in the design directories create duplicate runtime type identities.'
    }
    $client = [System.IO.MemoryStream]::new()
    $entry = $archive.GetEntry('lib/net10.0-windows7.0/Design/WinForms/ObjectListView.Design.Client.dll').Open()
    try { $entry.CopyTo($client) } finally { $entry.Dispose() }
    $client.Position = 0
    $assembly = [System.Reflection.PortableExecutable.PEReader]::new($client)
    try {
        $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($assembly)
        foreach ($reference in $metadata.AssemblyReferences) {
            $name = $metadata.GetString($metadata.GetAssemblyReference($reference).Name)
            if ($name -in @('ObjectListView', 'ListViewPrinter')) {
                throw "Designer client references runtime assembly $name."
            }
        }
        foreach ($definition in $metadata.TypeDefinitions) {
            $type = $metadata.GetTypeDefinition($definition)
            if ($metadata.GetString($type.Namespace) -eq 'BrightIdeasSoftware') {
                throw 'Designer client defines runtime-namespaced types instead of isolated editor data.'
            }
        }
    } finally {
        $assembly.Dispose()
        $client.Dispose()
    }
    $reader = [System.IO.StreamReader]::new($archive.GetEntry('ObjectListView.WinForms.nuspec').Open())
    try { [xml]$manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
    if ($manifest.SelectNodes("//*[local-name()='dependency']") | Where-Object {
        $_.id -match 'Designer|DesignTools|Serialization\.Formatters'
    }) {
        throw 'Design-only or BinaryFormatter packages leaked into runtime dependencies.'
    }
} finally {
    $archive.Dispose()
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$output = (Resolve-Path -LiteralPath $OutputDirectory).Path
# The Framework designer client cannot load assemblies from paths over MAX_PATH.
$packageHash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash
$packages = Join-Path $output ('packages\' + $packageHash.Substring(0, 16))
$consumerDirectory = Join-Path $output 'consumer'
New-Item -ItemType Directory -Force -Path $consumerDirectory | Out-Null
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot '..\smoke') -File |
    Where-Object { $_.Extension -in @('.cs', '.csproj', '.targets') } |
    Copy-Item -Destination $consumerDirectory -Force
$project = Join-Path $consumerDirectory 'PackageConsumer.csproj'
[xml]$consumerProject = Get-Content -LiteralPath $project -Raw
$consumerProject.Project.ItemGroup.PackageReference.Version = "[$PackageVersion]"
foreach ($property in @(
    @{ Name = 'RestoreAdditionalProjectSources'; Value = $packageRoot },
    @{ Name = 'RestorePackagesPath'; Value = $packages }
)) {
    $element = $consumerProject.CreateElement($property.Name)
    $element.InnerText = $property.Value
    $consumerProject.Project.PropertyGroup.AppendChild($element) | Out-Null
}
$consumerProject.Save($project)
dotnet new sln --name PackageConsumer --format sln --output $consumerDirectory --force
if ($LASTEXITCODE -ne 0) { throw 'Package consumer solution creation failed.' }
dotnet sln (Join-Path $consumerDirectory 'PackageConsumer.sln') add $project
if ($LASTEXITCODE -ne 0) { throw 'Package consumer solution setup failed.' }
dotnet restore $project
if ($LASTEXITCODE -ne 0) { throw 'Package consumer restore failed.' }
foreach ($configuration in @('Debug', 'Release')) {
    dotnet build $project --configuration $configuration --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "Package consumer $configuration build failed." }
}

$consumerRoot = Join-Path $consumerDirectory 'bin\Release'
foreach ($framework in @('net462', 'net48', 'net10.0-windows')) {
    if (Get-ChildItem -LiteralPath (Join-Path $consumerRoot $framework) -Filter 'ObjectListView.Design.*.dll') {
        throw "Designer assemblies leaked into the $framework application output."
    }
}
foreach ($framework in @('net462', 'net48')) {
    $publish = Join-Path $output "publish\$framework"
    dotnet publish $project --configuration Release --framework $framework --no-build --no-restore --output $publish --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "$framework package consumer publish failed." }
    if (Get-ChildItem -LiteralPath $publish -Filter 'ObjectListView.Design.*.dll') {
        throw "Designer assemblies leaked into the $framework publish output."
    }
}
$frameworkExe = Join-Path $consumerRoot 'net462\PackageConsumer.exe'
$modernDll = Join-Path $consumerRoot 'net10.0-windows\PackageConsumer.dll'
$frameworkLayout = Join-Path $output 'framework-layout.bin'
$modernLayout = Join-Path $output 'modern-layout.bin'
& $frameworkExe write $frameworkLayout '.NETFramework,Version=v4.6.2'
if ($LASTEXITCODE -ne 0) { throw 'Framework package consumer failed.' }
dotnet $modernDll read $frameworkLayout '.NETCoreApp,Version=v10.0'
if ($LASTEXITCODE -ne 0) { throw 'Modern consumer could not read Framework layout.' }
dotnet $modernDll write $modernLayout '.NETCoreApp,Version=v10.0'
if ($LASTEXITCODE -ne 0) { throw 'Modern package consumer failed.' }
& $frameworkExe read $modernLayout '.NETFramework,Version=v4.6.2'
if ($LASTEXITCODE -ne 0) { throw 'Framework consumer could not read modern layout.' }
$compatibleFrameworkExe = Join-Path $consumerRoot 'net48\PackageConsumer.exe'
& $compatibleFrameworkExe read $modernLayout '.NETFramework,Version=v4.6.2'
if ($LASTEXITCODE -ne 0) { throw 'Compatible Framework host selected incorrect runtime assets.' }
$assets = Get-Content -LiteralPath (Join-Path $consumerDirectory 'obj\project.assets.json') -Raw | ConvertFrom-Json
$library = $assets.libraries.PSObject.Properties |
    Where-Object { $_.Name -like 'ObjectListView.WinForms/*' } | Select-Object -First 1
$packageDirectory = Join-Path $packages ($library.Value.path.Replace('/', '\'))
$legacyProject = Join-Path $consumerDirectory 'LegacyConsumer.csproj'
dotnet build $legacyProject --configuration Release --no-restore "-p:ObjectListViewPackageDirectory=$packageDirectory" --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Legacy Framework package consumer build failed.' }
$legacyOutput = Join-Path $consumerDirectory 'bin\Legacy\Release'
if (Get-ChildItem -LiteralPath $legacyOutput -Filter 'ObjectListView.Design.*.dll') {
    throw 'Designer assemblies leaked into the legacy application output.'
}
& (Join-Path $legacyOutput 'LegacyConsumer.exe') read $modernLayout '.NETFramework,Version=v4.6.2'
if ($LASTEXITCODE -ne 0) { throw 'Legacy Framework package consumer failed at runtime.' }
