param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDirectory "../../..")).Path
$sourceRoot = Join-Path $repoRoot "demo02/SourceAssets/ThirdParty/PolyHaven"
$runtimeRoot = Join-Path $repoRoot "demo02/UnityProject/Assets/ThirdParty/PolyHaven/Models"
$manifestPath = Join-Path $repoRoot "demo02/SourceAssets/ThirdParty/ASSET_MANIFEST.csv"
$assetsRoot = Join-Path $repoRoot "demo02/UnityProject/Assets"

$assetIds = @(
    "chinese_screen_panels",
    "chinese_armchair",
    "chinese_tea_table",
    "chinese_cabinet"
)

New-Item -ItemType Directory -Path $sourceRoot -Force | Out-Null
New-Item -ItemType Directory -Path $runtimeRoot -Force | Out-Null

$catalogPath = Join-Path $sourceRoot "polyhaven-model-catalog.json"
if ($Force -or -not (Test-Path $catalogPath)) {
    Invoke-WebRequest `
        -UseBasicParsing `
        -Uri "https://api.polyhaven.com/assets?t=models" `
        -OutFile $catalogPath
}
$catalog = Get-Content $catalogPath -Raw | ConvertFrom-Json

$manifest = @(Import-Csv $manifestPath)

foreach ($assetId in $assetIds) {
    $assetSource = Join-Path $sourceRoot $assetId
    $assetRuntime = Join-Path $runtimeRoot $assetId
    New-Item -ItemType Directory -Path $assetSource -Force | Out-Null
    New-Item -ItemType Directory -Path $assetRuntime -Force | Out-Null

    $filesApi = "https://api.polyhaven.com/files/$assetId"
    $files = Invoke-RestMethod -Uri $filesApi
    $gltf = $files.gltf."1k".gltf
    if ($null -eq $gltf -or [string]::IsNullOrWhiteSpace($gltf.url)) {
        throw "Poly Haven asset '$assetId' has no 1K glTF download."
    }

    $downloads = @(
        [pscustomobject]@{
            RelativePath = "$assetId`_1k.gltf"
            Url = $gltf.url
            Md5 = $gltf.md5
        }
    )
    foreach ($property in $gltf.include.PSObject.Properties) {
        $relativePath = $property.Name.Replace('/', [IO.Path]::DirectorySeparatorChar)
        $downloads += [pscustomobject]@{
            RelativePath = $relativePath
            Url = $property.Value.url
            Md5 = $property.Value.md5
        }
    }

    $downloaded = New-Object System.Collections.Generic.List[object]
    foreach ($download in $downloads) {
        $target = Join-Path $assetSource $download.RelativePath
        New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force |
            Out-Null
        if ($Force -or -not (Test-Path $target)) {
            Invoke-WebRequest `
                -UseBasicParsing `
                -Uri $download.Url `
                -OutFile $target
        }

        $sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $target).Hash
        $downloaded += [pscustomobject]@{
            Path = $download.RelativePath
            Url = $download.Url
            Md5 = $download.Md5
            Sha256 = $sha256
            Size = (Get-Item -LiteralPath $target).Length
        }
    }

    Copy-Item -Path (Join-Path $assetSource "*") -Destination $assetRuntime -Recurse -Force
    $asset = $catalog.PSObject.Properties[$assetId].Value
    if ($null -eq $asset) {
        throw "Poly Haven catalog entry is missing: $assetId"
    }
    $authors = ($asset.authors.PSObject.Properties.Name -join "; ")
    $sourceUrl = "https://polyhaven.com/a/$assetId"
    $sourceSha256 = $downloaded[0].Sha256

    $sourceDocument = @"
# $($asset.name)

- Provider: Poly Haven
- Author: $authors
- Source: $sourceUrl
- License: CC0-1.0
- Download: glTF 1K

## Files

$(
    ($downloaded | ForEach-Object {
        "- ``$($_.Path)`` - MD5 $($_.Md5), SHA256 $($_.Sha256)"
    }) -join "`n"
)
"@
    Set-Content `
        -LiteralPath (Join-Path $assetSource "SOURCE.md") `
        -Value $sourceDocument `
        -Encoding utf8
    Copy-Item `
        -LiteralPath (Join-Path $assetSource "SOURCE.md") `
        -Destination (Join-Path $assetRuntime "SOURCE.md") `
        -Force

    $assetKey = "polyhaven_$assetId"
    $newRow = [pscustomobject]@{
        asset_id = $assetKey
        category = "interior_prop"
        provider = "Poly Haven"
        author = $authors
        source_url = $sourceUrl
        license = "CC0-1.0"
        source_path = "demo02/SourceAssets/ThirdParty/PolyHaven/$assetId/$assetId`_1k.gltf"
        runtime_path = "demo02/UnityProject/Assets/ThirdParty/PolyHaven/Models/$assetId"
        replaces = "traditional interior dressing"
        modifications = "downloaded 1K glTF set; no mesh edits"
        status = "downloaded"
        sha256 = $sourceSha256
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

    Write-Host "Downloaded $assetId ($($downloaded.Count) files)."
}

$manifest | Export-Csv -NoTypeInformation -Encoding utf8 -LiteralPath $manifestPath

Write-Host "Poly Haven downloads are ready in $runtimeRoot"
