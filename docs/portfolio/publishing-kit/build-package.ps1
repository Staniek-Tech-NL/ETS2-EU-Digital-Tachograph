[CmdletBinding()]
param(
    [string]$Destination
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
if ([string]::IsNullOrWhiteSpace($Destination)) {
    $Destination = Join-Path $repositoryRoot 'output\portfolio-publishing-kit'
}

$destinationPath = [IO.Path]::GetFullPath($Destination)
$expectedOutputRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'output'))
if (-not $destinationPath.StartsWith($expectedOutputRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Destination must stay inside the repository output directory: $expectedOutputRoot"
}

$coversPath = Join-Path $destinationPath 'covers'
$screenshotsPath = Join-Path $destinationPath 'screenshots'
$textsPath = Join-Path $destinationPath 'texts'

New-Item -ItemType Directory -Force $coversPath, $screenshotsPath, $textsPath | Out-Null

$coverSources = @(
    'activity-gap-reconstruction-cover.png',
    'full-application-cover.png',
    'journey-planner-cover.png',
    'reporting-analytics-cover.png'
)
foreach ($name in $coverSources) {
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "docs\images\portfolio\$name") `
        -Destination (Join-Path $coversPath $name) -Force
}

$screenshotSources = @(
    'dashboard.png',
    'journey-planner-mockup.png',
    'manual-entry-mockup.png',
    'overlay-s1.png',
    'report-pdf.png',
    'reports-dashboard.png',
    'social-preview.jpg'
)
foreach ($name in $screenshotSources) {
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "docs\images\$name") `
        -Destination (Join-Path $screenshotsPath $name) -Force
}

$textSources = @(
    'README.md',
    'asset-manifest.md',
    'descriptions-en.md',
    'descriptions-pl.md',
    'links.md'
)
foreach ($name in $textSources) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) `
        -Destination (Join-Path $textsPath $name) -Force
}

$archivePath = "$destinationPath.zip"
Compress-Archive -Path (Join-Path $destinationPath '*') -DestinationPath $archivePath -Force

Write-Output "Portfolio package: $destinationPath"
Write-Output "ZIP archive: $archivePath"
