#Requires -Version 7.0
param(
    [string]$OutputDirectory = 'artifacts\ProjectReferenceSmoke',
    [string]$Configuration = 'Release',
    [string]$Framework = 'net462',
    [string]$MSBuild = 'msbuild'
)

$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$runtimeProject = Join-Path $repository 'ObjectListView\ObjectListView.csproj'
$designerProject = Join-Path $repository 'ObjectListView.Design.Framework\ObjectListView.Design.Framework.csproj'
$designerTargets = Join-Path $repository 'ObjectListView.Design.Framework\ObjectListView.Design.Framework.targets'
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$output = (Resolve-Path -LiteralPath $OutputDirectory).Path
$consumerDirectory = Join-Path $output ('consumer-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Path $consumerDirectory | Out-Null
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot '..\smoke') -File |
    Where-Object { $_.Extension -in @('.cs', '.csproj', '.targets') } |
    Copy-Item -Destination $consumerDirectory
$project = Join-Path $consumerDirectory 'PackageConsumer.csproj'
[xml]$consumerProject = Get-Content -LiteralPath $project -Raw
$consumerProject.Project.PropertyGroup.TargetFrameworks = $Framework
$references = $consumerProject.Project.ItemGroup
$references.RemoveChild($references.PackageReference) | Out-Null
$reference = $consumerProject.CreateElement('ProjectReference')
$reference.SetAttribute('Include', $runtimeProject)
$references.AppendChild($reference) | Out-Null
$import = $consumerProject.CreateElement('Import')
$import.SetAttribute('Project', $designerTargets)
$consumerProject.Project.AppendChild($import) | Out-Null
$consumerProject.Save($project)
dotnet new sln --name PackageConsumer --format sln --output $consumerDirectory
if ($LASTEXITCODE -ne 0) { throw 'Project-reference solution creation failed.' }
$solution = Join-Path $consumerDirectory 'PackageConsumer.sln'
dotnet sln $solution add $project $runtimeProject $designerProject
if ($LASTEXITCODE -ne 0) { throw 'Project-reference solution setup failed.' }
& $MSBuild $project -restore "-p:Configuration=$Configuration" -verbosity:minimal -nologo
if ($LASTEXITCODE -ne 0) { throw 'Project-reference consumer build failed.' }
$consumerRoot = Join-Path $consumerDirectory "bin\$Configuration\$Framework"
if (Get-ChildItem -LiteralPath $consumerRoot -Filter 'ObjectListView.Design.*.dll') {
    throw 'Designer assemblies leaked into the project-reference application output.'
}
$publish = Join-Path $consumerDirectory 'publish'
dotnet publish $project --configuration $Configuration --framework $Framework --no-build --no-restore --output $publish --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Project-reference consumer publish failed.' }
if (Get-ChildItem -LiteralPath $publish -Filter 'ObjectListView.Design.*.dll') {
    throw 'Designer assemblies leaked into the project-reference publish output.'
}
$executable = Join-Path $consumerRoot 'PackageConsumer.exe'
& $executable write (Join-Path $consumerDirectory 'project-layout.bin') '.NETFramework,Version=v4.6.2'
if ($LASTEXITCODE -ne 0) { throw 'Project-reference runtime consumer failed.' }
Write-Output "Framework designer project-reference consumer: $solution"
