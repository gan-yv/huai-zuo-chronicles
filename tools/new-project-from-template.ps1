[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Destination,

    [switch]$Minimal,

    [string]$NewRemoteUrl = ""
)

$ErrorActionPreference = "Stop"

$sourceRepository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$destinationPath = if ([IO.Path]::IsPathRooted($Destination))
{
    [IO.Path]::GetFullPath($Destination)
}
else
{
    [IO.Path]::GetFullPath((Join-Path (Get-Location) $Destination))
}

if (Test-Path -LiteralPath $destinationPath)
{
    throw "Destination already exists: $destinationPath"
}

$parentDirectory = Split-Path -Parent $destinationPath
if (-not (Test-Path -LiteralPath $parentDirectory))
{
    New-Item -ItemType Directory -Path $parentDirectory | Out-Null
}

git -C $sourceRepository rev-parse --verify template/world-rpg-base | Out-Null
if ($LASTEXITCODE -ne 0)
{
    throw "Template branch 'template/world-rpg-base' was not found."
}

git clone --branch template/world-rpg-base --single-branch $sourceRepository $destinationPath
if ($LASTEXITCODE -ne 0)
{
    throw "Failed to create the project from the template branch."
}

git -C $destinationPath branch --move main
if ($LASTEXITCODE -ne 0)
{
    throw "Failed to rename the template branch to main."
}

git -C $destinationPath branch --unset-upstream 2>$null
git -C $destinationPath remote remove origin 2>$null

if (-not [string]::IsNullOrWhiteSpace($NewRemoteUrl))
{
    git -C $destinationPath remote add origin $NewRemoteUrl
    if ($LASTEXITCODE -ne 0)
    {
        throw "Failed to add the new remote repository."
    }
}

if ($Minimal)
{
    $legacyDemo = Join-Path $destinationPath "demo01"
    if (Test-Path -LiteralPath $legacyDemo)
    {
        Remove-Item -LiteralPath $legacyDemo -Recurse -Force
    }

    $earlyArchive = Join-Path $destinationPath "demo02/docs/archive"
    if (Test-Path -LiteralPath $earlyArchive)
    {
        Remove-Item -LiteralPath $earlyArchive -Recurse -Force
    }
}

Write-Host "New world project created at: $destinationPath"
Write-Host "Branch: main"
Write-Host "Open: $destinationPath\demo02\UnityProject"
