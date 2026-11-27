$ErrorActionPreference = "Stop"

$validationDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$demoRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $validationDir "../..")
)
$repoRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $demoRoot "..")
)
$errors = [System.Collections.Generic.List[string]]::new()

$versionPath = Join-Path $demoRoot "version.json"
$versionData = Get-Content $versionPath -Raw | ConvertFrom-Json
$version = [string]$versionData.version
$parsed = $null
if (-not [System.Version]::TryParse($version, [ref]$parsed)) {
    $errors.Add("version.json has invalid version: $version")
}

$settingsPath = Join-Path $demoRoot "UnityProject/ProjectSettings/ProjectSettings.asset"
$settings = Get-Content $settingsPath -Raw
if ($settings -notmatch "bundleVersion:\s*$([regex]::Escape($version))") {
    $errors.Add("ProjectSettings bundleVersion does not match $version.")
}

$demoReadme = Get-Content (Join-Path $demoRoot "README.md") -Raw
$rootReadme = Get-Content (Join-Path $repoRoot "README.md") -Raw
$changelog = Get-Content (Join-Path $repoRoot "CHANGELOG.md") -Raw
if (-not $demoReadme.Contains($version)) {
    $errors.Add("demo02/README.md does not display $version.")
}
if (-not $rootReadme.Contains($version)) {
    $errors.Add("Root README does not display $version.")
}
if ($changelog -notmatch "(?m)^## .*v$([regex]::Escape($version))") {
    $errors.Add("CHANGELOG.md has no heading for v$version.")
}

$runtimeFiles = Get-ChildItem (
    Join-Path $demoRoot "UnityProject/Assets/Demo02/Scripts"
) -Recurse -Filter *.cs |
    Where-Object {
        $_.FullName -notmatch "[\\/]Editor[\\/]" -and
        $_.Name -ne "RuntimeLaunchOptions.cs"
    }
foreach ($file in $runtimeFiles) {
    $source = Get-Content $file.FullName -Raw
    if ($source.Contains("Environment.GetCommandLineArgs")) {
        $errors.Add("Runtime reads command line directly: $($file.FullName)")
    }
    if ($source.Contains("using Demo02.Editor") -or
        $source.Contains("Demo02.Editor.")) {
        $errors.Add("Runtime references editor code: $($file.FullName)")
    }
}

$asmdefNames = @(
    Get-ChildItem (
        Join-Path $demoRoot "UnityProject/Assets/Demo02"
    ) -Recurse -Filter *.asmdef |
        ForEach-Object {
            (Get-Content $_.FullName -Raw | ConvertFrom-Json).name
        }
)
foreach ($required in @(
    "Demo02.Foundation",
    "Demo02.Runtime",
    "Demo02.Editor",
    "Demo02.Tests.EditMode",
    "Demo02.Tests.PlayMode"
)) {
    if ($required -notin $asmdefNames) {
        $errors.Add("Missing assembly definition: $required")
    }
}

$markdownFiles = Get-ChildItem $repoRoot -Recurse -File -Filter *.md |
    Where-Object {
        $_.FullName -notmatch "\\(Library|Temp|obj|Builds|\.git|\.tools)\\"
    }
foreach ($file in $markdownFiles) {
    $text = Get-Content $file.FullName -Raw
    foreach ($match in [regex]::Matches(
        $text,
        '\[[^\]]+\]\(([^)]+)\)'
    )) {
        $target = $match.Groups[1].Value.Trim()
        if ($target -match "^(https?://|mailto:|#|<)") {
            continue
        }
        $target = ($target -split "#")[0]
        if ([string]::IsNullOrWhiteSpace($target)) {
            continue
        }
        $resolved = Join-Path $file.DirectoryName (
            $target.Replace("/", [System.IO.Path]::DirectorySeparatorChar)
        )
        if (-not (Test-Path $resolved)) {
            $errors.Add(
                "Broken Markdown link in $($file.FullName): $target"
            )
        }
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Host "::error::$_" }
    throw "Repository validation failed with $($errors.Count) error(s)."
}

Write-Host (
    "Repository validation passed: v$version, " +
    "$($markdownFiles.Count) Markdown files, $($runtimeFiles.Count) " +
    "runtime source files."
)
