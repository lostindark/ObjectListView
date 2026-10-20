$ErrorActionPreference = 'Stop'
$generator = Join-Path $PSScriptRoot 'New-ReleaseNotes.ps1'
$fixture = Join-Path ([IO.Path]::GetTempPath()) ("olv-release-notes-" + [guid]::NewGuid())
$null = New-Item -ItemType Directory -Path $fixture

function Invoke-TestGit {
    param([string[]]$Arguments)
    $result = & git @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Fixture Git command failed: $($Arguments -join ' ')" }
    $result
}

function Add-Commit {
    param([string]$Title, [string[]]$Paths)
    foreach ($path in $Paths) {
        $fullPath = Join-Path $fixture $path
        $null = New-Item -ItemType Directory -Path (Split-Path $fullPath) -Force
        $Title | Add-Content -LiteralPath $fullPath
    }
    Invoke-TestGit @('add', '--all')
    Invoke-TestGit @('-c', 'user.name=Release Notes Test', '-c', 'user.email=test@example.invalid', 'commit', '--quiet', '-m', $Title)
    Invoke-TestGit @('rev-parse', 'HEAD')
}

function Assert-Notes {
    param([string]$Version, [object[]]$Releases, [string[]]$Included, [string[]]$Excluded)
    & $generator -SourceCommit $source -PackageVersion $Version -Repository 'example/library' -BaselineCommit $baseline -OutputPath $notesPath -Releases $Releases
    $notes = Get-Content -Raw -LiteralPath $notesPath
    foreach ($text in $Included) {
        if (!$notes.Contains($text)) { throw "Missing release note: $text" }
    }
    foreach ($text in $Excluded) {
        if ($notes.Contains($text)) { throw "Unexpected release note: $text" }
    }
}

Push-Location $fixture
try {
    Invoke-TestGit @('init', '--quiet', '--initial-branch=main')
    $upstream = Add-Commit 'Upstream history' @('ObjectListView\Widget.cs')
    $baseline = Add-Commit 'V3 baseline change' @('ObjectListView\Widget.cs')
    Invoke-TestGit @('tag', 'v3.0.1', $baseline)
    $feature = Add-Commit 'Library feature' @('ObjectListView\Widget.cs')
    Invoke-TestGit @('tag', 'v3.0.2-preview', $feature)
    $null = Add-Commit 'Mixed library and CI change' @('ObjectListView\Widget.cs', '.github\workflows\ci.yml')
    $resources = Add-Commit 'Library resources' @('ObjectListView\Properties\Resources.resx')
    Invoke-TestGit @('tag', 'v3.0.3-preview', $resources)
    $source = Add-Commit 'Repository infrastructure only' @(
        '.github\workflows\ci.yml', 'README.md', 'Tests\Test.cs', 'Demo\Example.cs',
        'ObjectListView\README.md', 'ObjectListView\ObjectListView.csproj'
    )
    $notesPath = Join-Path $fixture 'notes.md'
    $stable = [pscustomobject]@{ tag_name = 'v3.0.1'; draft = $false; prerelease = $false }
    $preview = [pscustomobject]@{ tag_name = 'v3.0.2-preview'; draft = $false; prerelease = $true }
    $unwanted = @('Upstream history', 'Repository infrastructure only')

    Assert-Notes '3.0.4-preview' @() @('V3 baseline change', 'Library feature', 'Mixed library and CI change', 'Library resources') $unwanted
    Assert-Notes '3.0.4-preview' @($stable, $preview) @('v3.0.2-preview', 'Mixed library and CI change', 'Library resources') ($unwanted + @('V3 baseline change', 'Library feature'))
    Assert-Notes '3.0.4' @($stable, $preview) @('v3.0.1', 'Library feature', 'Mixed library and CI change', 'Library resources') ($unwanted + @('V3 baseline change'))

    $draft = [pscustomobject]@{ tag_name = 'v3.0.3-preview'; draft = $true; prerelease = $true }
    Assert-Notes '3.0.4-preview' @($preview, $draft) @('Library resources') $unwanted
    $published = [pscustomobject]@{ tag_name = 'v3.0.3-preview'; draft = $false; prerelease = $true }
    Assert-Notes '3.0.4-preview' @($published) @('No library changes since the previous release.') @('Library resources', 'Repository infrastructure only')

    Invoke-TestGit @('checkout', '--quiet', '-b', 'unrelated', $upstream)
    $null = Add-Commit 'Unrelated branch feature' @('ObjectListView\Widget.cs')
    Invoke-TestGit @('tag', 'v3.0.2')
    Invoke-TestGit @('checkout', '--quiet', 'main')
    $unrelated = [pscustomobject]@{ tag_name = 'v3.0.2'; draft = $false; prerelease = $false }
    Assert-Notes '3.0.4' @($stable, $unrelated) @('Library feature') @('Unrelated branch feature')

    $rejected = $false
    try {
        & $generator -SourceCommit $upstream -PackageVersion '3.0.4' -Repository 'example/library' -BaselineCommit $baseline -OutputPath $notesPath
    } catch {
        $rejected = $_.Exception.Message -like '*does not include the v3 baseline*'
    }
    if (!$rejected) { throw 'A source commit before the v3 baseline was accepted.' }
    'Release note baseline, library filtering, release channels, drafts, ancestry, and empty changes verified.'
} finally {
    Pop-Location
    Remove-Item -LiteralPath $fixture -Recurse -Force
}

exit 0
