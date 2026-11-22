param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path
)

$ErrorActionPreference = "Stop"
$manifestPath = Join-Path $RepositoryRoot "demo02/SourceAssets/ThirdParty/ASSET_MANIFEST.csv"
$unityAssets = Join-Path $RepositoryRoot "demo02/UnityProject/Assets"
$creditsPath = Join-Path $RepositoryRoot "demo02/UnityProject/Assets/ThirdParty/CREDITS.md"
$issues = [System.Collections.Generic.List[string]]::new()

if (-not (Test-Path $manifestPath)) {
    $issues.Add("Missing asset manifest: $manifestPath")
}
else {
    $manifest = Import-Csv $manifestPath
    foreach ($row in $manifest) {
        foreach ($property in @("asset_id", "source_path", "runtime_path", "license")) {
            if ([string]::IsNullOrWhiteSpace($row.$property)) {
                $issues.Add("Manifest row '$($row.asset_id)' is missing '$property'.")
            }
        }

        foreach ($property in @("source_path", "runtime_path")) {
            $relativePath = $row.$property
            if ($relativePath -and -not (Test-Path (Join-Path $RepositoryRoot $relativePath))) {
                $issues.Add("Manifest path does not exist: $relativePath")
            }
        }

        if ($row.sha256 -and $row.source_path -and (Test-Path (Join-Path $RepositoryRoot $row.source_path))) {
            $actualHash = (Get-FileHash (Join-Path $RepositoryRoot $row.source_path) -Algorithm SHA256).Hash
            if ($actualHash -ne $row.sha256.ToUpperInvariant()) {
                $issues.Add("SHA256 mismatch for $($row.source_path).")
            }
        }
    }
}

if (-not (Test-Path $creditsPath)) {
    $issues.Add("Missing credits file: $creditsPath")
}

$runtimeMetadata = @(
    "demo02/UnityProject/Assets/ThirdParty/Kenney/FantasyTownKit/SOURCE.md",
    "demo02/UnityProject/Assets/ThirdParty/Kenney/FantasyTownKit/LICENSE.md",
    "demo02/UnityProject/Assets/ThirdParty/Kenney/BlockyCharacters/SOURCE.md",
    "demo02/UnityProject/Assets/ThirdParty/Kenney/BlockyCharacters/LICENSE.md",
    "demo02/UnityProject/Assets/ThirdParty/Kenney/AnimatedCharactersProtagonists/SOURCE.md",
    "demo02/UnityProject/Assets/ThirdParty/Kenney/AnimatedCharactersProtagonists/LICENSE.md",
    "demo02/UnityProject/Assets/ThirdParty/Kenney/CityKitCommercial/SOURCE.md",
    "demo02/UnityProject/Assets/ThirdParty/Kenney/CityKitCommercial/LICENSE.md",
    "demo02/UnityProject/Assets/ThirdParty/PolyHaven/LICENSE.md",
    "demo02/UnityProject/Assets/ThirdParty/PolyHaven/grey_plaster_02/SOURCE.md",
    "demo02/UnityProject/Assets/ThirdParty/PolyHaven/old_planks_02/SOURCE.md",
    "demo02/UnityProject/Assets/ThirdParty/PolyHaven/clay_roof_tiles_02/SOURCE.md",
    "demo02/UnityProject/Assets/ThirdParty/PolyHaven/cobblestone_05/SOURCE.md",
    "demo02/UnityProject/Assets/ThirdParty/PolyHaven/grey_roof_tiles_02/SOURCE.md"
)
foreach ($relativePath in $runtimeMetadata) {
    if (-not (Test-Path (Join-Path $RepositoryRoot $relativePath))) {
        $issues.Add("Missing runtime provenance file: $relativePath")
    }
}

$archivesInUnity = Get-ChildItem $unityAssets -Recurse -File -Include *.zip,*.7z,*.rar -ErrorAction SilentlyContinue
foreach ($archive in $archivesInUnity) {
    $issues.Add("Archive should stay outside Unity Assets: $($archive.FullName)")
}

if ($issues.Count -gt 0) {
    $issues | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Third-party art metadata and paths are valid."

