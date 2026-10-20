param(
    [Parameter(Mandatory)][string]$SourceCommit,
    [Parameter(Mandatory)][ValidatePattern('^3\.\d+\.\d+(-preview)?$')][string]$PackageVersion,
    [Parameter(Mandatory)][string]$Repository,
    [Parameter(Mandatory)][string]$BaselineCommit,
    [Parameter(Mandatory)][string]$OutputPath,
    [object[]]$Releases = @()
)

$ErrorActionPreference = 'Stop'

function Invoke-Git {
    param([string[]]$Arguments)
    $result = & git @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Git failed: git $($Arguments -join ' ')" }
    $result
}

function Test-Ancestor {
    param([string]$Ancestor, [string]$Descendant)
    & git merge-base --is-ancestor $Ancestor $Descendant
    if ($LASTEXITCODE -eq 0) { return $true }
    if ($LASTEXITCODE -eq 1) { return $false }
    throw "Could not check ancestry of $Ancestor and $Descendant."
}

$source = Invoke-Git @('rev-parse', '--verify', "$SourceCommit^{commit}")
$baseline = Invoke-Git @('rev-parse', '--verify', "$BaselineCommit^{commit}")
if (!(Test-Ancestor $baseline $source)) {
    throw "The selected CI commit does not include the v3 baseline $baseline."
}

$version = [version]($PackageVersion -replace '-preview$', '')
$isPreview = $PackageVersion.EndsWith('-preview')
$candidates = foreach ($release in $Releases) {
    if ($release.draft -or (!$isPreview -and $release.prerelease)) { continue }
    if ($release.tag_name -cnotmatch '^v(3\.\d+\.\d+)(-preview)?$') { continue }
    $candidateVersion = [version]$Matches[1]
    $candidatePreview = [bool]$Matches[2]
    if ($candidateVersion -ge $version -or (!$isPreview -and $candidatePreview)) { continue }
    [pscustomobject]@{
        Tag = $release.tag_name
        Version = $candidateVersion
        Preview = $candidatePreview
    }
}

$previous = $null
foreach ($candidate in ($candidates | Sort-Object @{ Expression = 'Version'; Descending = $true }, Preview)) {
    $commit = Invoke-Git @('rev-parse', '--verify', "$($candidate.Tag)^{commit}")
    if ((Test-Ancestor $baseline $commit) -and (Test-Ancestor $commit $source)) {
        $previous = $candidate
        break
    }
}

if ($previous) {
    $start = $previous.Tag
    $comparison = "Changes since [$start](https://github.com/$Repository/releases/tag/$start)."
} else {
    $start = Invoke-Git @('rev-parse', '--verify', "$baseline^")
    $comparison = "Changes starting at the [v3 baseline](https://github.com/$Repository/commit/$baseline)."
}

$paths = @(
    ':(glob)ObjectListView/**/*.cs',
    ':(glob)ObjectListView/**/*.resx',
    ':(glob)ObjectListView/**/*.resources',
    ':(glob)ObjectListView/**/*.png',
    ':(glob)ObjectListView/**/*.jpg',
    ':(glob)ObjectListView/**/*.ico',
    ':(glob)ObjectListView/**/*.bmp',
    ':(glob)ObjectListView/**/*.gif',
    ':(glob)ObjectListView/**/*.cur'
)
$commits = @(Invoke-Git (@('log', '--full-history', '--no-merges', '--reverse', '--format=%H%x09%s', "$start..$source", '--') + $paths))
$notes = @('## Library changes', '', $comparison, '')
if ($commits.Count -eq 0) {
    $notes += if ($previous) { 'No library changes since the previous release.' } else { 'No library changes since the v3 baseline.' }
} else {
    foreach ($commit in $commits) {
        $sha, $subject = $commit -split "`t", 2
        $subject = $subject -replace '([\\`*_\[\]<>])', '\$1'
        $notes += "- $subject ([$($sha.Substring(0, 7))](https://github.com/$Repository/commit/$sha))"
    }
}
$notes | Set-Content -LiteralPath $OutputPath -Encoding utf8
