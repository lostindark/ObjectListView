param(
    [Parameter(Mandatory)][string]$PackageDirectory,
    [Parameter(Mandatory)][string]$PackageVersion,
    [string]$OutputDirectory = 'artifacts\PackageSmoke'
)

$ErrorActionPreference = 'Stop'
$packageRoot = (Resolve-Path -LiteralPath $PackageDirectory).Path
$package = Join-Path $packageRoot "ObjectListView.WinForms.$PackageVersion.nupkg"
$archive = [System.IO.Compression.ZipFile]::OpenRead($package)
try {
    $files = @($archive.Entries | ForEach-Object FullName)
    foreach ($required in @(
        'README.md', 'LICENSE',
        'lib/net462/ObjectListView.dll', 'lib/net462/ObjectListView.xml',
        'lib/net10.0-windows7.0/ObjectListView.dll', 'lib/net10.0-windows7.0/ObjectListView.xml',
        'lib/net10.0-windows7.0/Design/WinForms/ObjectListView.Design.Client.dll',
        'lib/net10.0-windows7.0/Design/WinForms/ObjectListView.Design.Protocol.dll',
        'lib/net10.0-windows7.0/Design/WinForms/ListViewPrinter.dll',
        'lib/net10.0-windows7.0/Design/WinForms/ObjectListView.dll',
        'lib/net10.0-windows7.0/Design/WinForms/Server/ObjectListView.Design.Server.dll',
        'lib/net10.0-windows7.0/Design/WinForms/Server/ObjectListView.Design.Protocol.dll'
    )) {
        if ($required -notin $files) { throw "Missing package asset: $required" }
    }
    if ($files | Where-Object { $_ -match 'Microsoft\.DotNet\.DesignTools.*\.dll$' }) {
        throw 'Designer SDK reference assemblies must not be redistributed.'
    }
    if ('lib/net10.0-windows7.0/Design/WinForms/Server/ListViewPrinter.dll' -in $files) {
        throw 'A runtime printer assembly in the server design directory creates duplicate runtime type identities.'
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

$project = Join-Path $PSScriptRoot '..\smoke\PackageConsumer.csproj'
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$output = (Resolve-Path -LiteralPath $OutputDirectory).Path
$packages = Join-Path $output ('packages\' + (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash)
dotnet restore $project "-p:RestoreAdditionalProjectSources=$packageRoot" `
    "-p:ObjectListViewPackageVersion=$PackageVersion" "-p:RestorePackagesPath=$packages"
if ($LASTEXITCODE -ne 0) { throw 'Package consumer restore failed.' }
dotnet build $project --configuration Release --no-restore `
    "-p:ObjectListViewPackageVersion=$PackageVersion" "-p:RestorePackagesPath=$packages" --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Package consumer build failed.' }

$consumerRoot = Join-Path $PSScriptRoot '..\smoke\bin\Release'
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
