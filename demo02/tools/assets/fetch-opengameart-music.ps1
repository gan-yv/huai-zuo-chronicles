param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDirectory "../../..")).Path
$sourceRoot = Join-Path $repoRoot "demo02/SourceAssets/ThirdParty/OpenGameArt/Liyan"
$runtimeRoot = Join-Path `
    $repoRoot `
    "demo02/UnityProject/Assets/ThirdParty/OpenGameArt/Audio"
$manifestPath = Join-Path `
    $repoRoot `
    "demo02/SourceAssets/ThirdParty/ASSET_MANIFEST.csv"

New-Item -ItemType Directory -Path $sourceRoot -Force | Out-Null
New-Item -ItemType Directory -Path $runtimeRoot -Force | Out-Null

$sourceUrl = "https://opengameart.org/content/liyan"
$downloadUrl = "https://opengameart.org/sites/default/files/liyan.mp3"
$sourceFile = Join-Path $sourceRoot "liyan.mp3"

if ($Force -or -not (Test-Path $sourceFile)) {
    Invoke-WebRequest `
        -UseBasicParsing `
        -Uri $downloadUrl `
        -OutFile $sourceFile
}

$sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $sourceFile).Hash
$sourceDocument = @"
# Liyan

- Provider: OpenGameArt
- Author: elerya
- Source: $sourceUrl
- License: CC-BY 3.0
- Runtime use: exploration music

## File

- ``liyan.mp3`` - SHA256 $sha256

Attribution must remain visible in the game credits and portfolio.
"@
Set-Content `
    -LiteralPath (Join-Path $sourceRoot "SOURCE.md") `
    -Value $sourceDocument `
    -Encoding utf8
Copy-Item `
    -LiteralPath $sourceFile `
    -Destination (Join-Path $runtimeRoot "liyan.mp3") `
    -Force
Copy-Item `
    -LiteralPath (Join-Path $sourceRoot "SOURCE.md") `
    -Destination (Join-Path $runtimeRoot "SOURCE.md") `
    -Force

$manifest = @(Import-Csv $manifestPath)
$assetKey = "opengameart_liyan_music"
$newRow = [pscustomobject]@{
    asset_id = $assetKey
    category = "exploration_music"
    provider = "OpenGameArt"
    author = "elerya"
    source_url = $sourceUrl
    license = "CC-BY-3.0"
    source_path = "demo02/SourceAssets/ThirdParty/OpenGameArt/Liyan/liyan.mp3"
    runtime_path = "demo02/UnityProject/Assets/ThirdParty/OpenGameArt/Audio/liyan.mp3"
    replaces = "exploration ambience"
    modifications = "none"
    status = "integrated"
    sha256 = $sha256
}
$existingRow = $manifest | Where-Object asset_id -eq $assetKey
if ($null -eq $existingRow) {
    $manifest += $newRow
}
else {
    foreach ($property in $newRow.PSObject.Properties) {
        $existingRow.($property.Name) = $property.Value
    }
}
$manifest | Export-Csv -NoTypeInformation -Encoding utf8 -LiteralPath $manifestPath

Write-Host "OpenGameArt music is ready in $runtimeRoot"
