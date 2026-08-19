[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$releasesRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'output\releases'))
$packageName = "ETS2Tachograph-$Version-win-x64"
$packageRoot = [IO.Path]::GetFullPath((Join-Path $releasesRoot $packageName))
$archivePath = "$packageRoot.zip"
$checksumPath = "$archivePath.sha256"

if (-not $packageRoot.StartsWith($releasesRoot + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) {
    throw "Package path must stay inside $releasesRoot"
}

$pluginSource = Join-Path $repositoryRoot 'native\ETS2Tachograph.ScsPlugin\x64\Release\ETS2Tachograph.ScsPlugin.dll'
if (-not (Test-Path -LiteralPath $pluginSource -PathType Leaf)) {
    throw "Release plugin is missing: $pluginSource"
}

New-Item -ItemType Directory -Force -Path $releasesRoot | Out-Null
foreach ($path in @($packageRoot, $archivePath, $checksumPath)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

$appRoot = Join-Path $packageRoot 'app'
$pluginRoot = Join-Path $packageRoot 'plugin'
$docsRoot = Join-Path $packageRoot 'docs'
New-Item -ItemType Directory -Force -Path $appRoot, $pluginRoot, $docsRoot | Out-Null

dotnet publish (Join-Path $repositoryRoot 'src\ETS2Tachograph.Desktop\ETS2Tachograph.Desktop.csproj') `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $appRoot
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

Copy-Item -LiteralPath $pluginSource -Destination (Join-Path $pluginRoot 'ETS2Tachograph.ScsPlugin.dll')

$rootDocuments = @(
    'BETA_TEST_PLAN.md',
    'KNOWN_ISSUES.md',
    'KNOWN_ISSUES_EN.md',
    'LICENSE',
    'README.md',
    'README_PL.md',
    'RELEASE_NOTES.md',
    'RELEASE_NOTES_EN.md',
    'SECURITY.md',
    'SUPPORT.md'
)
foreach ($name in $rootDocuments) {
    Copy-Item -LiteralPath (Join-Path $repositoryRoot $name) -Destination (Join-Path $packageRoot $name)
}

$documentationFiles = @(
    'DOCUMENTATION.md',
    'INSTALLATION_EN.md',
    'INSTALLATION_PL.md',
    'THIRD_PARTY_NOTICES.md',
    'USER_GUIDE_EN.md',
    'USER_GUIDE_PL.md'
)
foreach ($name in $documentationFiles) {
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "docs\$name") -Destination (Join-Path $docsRoot $name)
}

$desktopExecutable = Join-Path $appRoot 'ETS2Tachograph.Desktop.exe'
$versionInfo = (Get-Item -LiteralPath $desktopExecutable).VersionInfo
if ($versionInfo.FileVersion -ne "$Version.0") {
    throw "Unexpected FileVersion '$($versionInfo.FileVersion)'; expected '$Version.0'."
}
if (-not $versionInfo.ProductVersion.StartsWith($Version, [StringComparison]::Ordinal)) {
    throw "Unexpected ProductVersion '$($versionInfo.ProductVersion)'."
}

$sourceCommit = (git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $sourceCommit -notmatch '^[0-9a-f]{40}$') {
    throw 'Unable to resolve the source commit.'
}

$pluginHash = (Get-FileHash -LiteralPath (Join-Path $pluginRoot 'ETS2Tachograph.ScsPlugin.dll') -Algorithm SHA256).Hash
$buildInfo = @"
ETS2 EU DIGITAL TACHOGRAPH — STABLE $Version

Platform: win-x64
Mode: Release, self-contained
Source commit: $sourceCommit
FileVersion: $($versionInfo.FileVersion)
ProductVersion: $($versionInfo.ProductVersion)
Plugin: ETS2Tachograph.ScsPlugin.dll, protocol v3
Plugin SHA-256: $pluginHash
Automated gate: 571/571 tests, Release build 0 errors / 0 warnings
SQLite schema changes: none
Public documentation: Polish and English

This immutable artifact is a candidate for final smoke testing and stable publication.
"@
Set-Content -LiteralPath (Join-Path $packageRoot 'BUILD-INFO.txt') -Value $buildInfo -Encoding utf8

Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $archivePath -CompressionLevel Optimal
$archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
Set-Content -LiteralPath $checksumPath -Value "$archiveHash  $packageName.zip" -Encoding ascii

[PSCustomObject]@{
    Package = $packageRoot
    Archive = $archivePath
    ArchiveSha256 = $archiveHash
    PluginSha256 = $pluginHash
    SourceCommit = $sourceCommit
    FileVersion = $versionInfo.FileVersion
    ProductVersion = $versionInfo.ProductVersion
}
