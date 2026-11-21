[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Destination,

    [switch]$Minimal,

    [string]$NewRemoteUrl = "",

    [string]$ProjectName = ""
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

    if ([string]::IsNullOrWhiteSpace($ProjectName))
    {
        $ProjectName = Split-Path -Leaf $destinationPath
    }

    $readme = @'
# {0}

本项目从通用世界游戏模板创建。

## 开始

- Unity 工程：`demo02/UnityProject`
- 设计文档：`demo02/docs`
- 工程规范：`CONTRIBUTING.md`
- 模板使用说明：`TEMPLATE.md`

建议第一步打开 `Boot.unity`，确认标题、存档和广陵灰盒流程可以运行，再替换项目专属内容。
'@ -f $ProjectName
    Set-Content -LiteralPath (Join-Path $destinationPath "README.md") -Value $readme -Encoding utf8

    $unityReadmePath = Join-Path $destinationPath "demo02/README.md"
    if (Test-Path -LiteralPath $unityReadmePath)
    {
        $unityReadme = Get-Content -LiteralPath $unityReadmePath -Raw
        $unityReadme = $unityReadme -replace `
            '旧方向的 2D 原型位于.*', `
            '本项目从通用世界模板创建，后续在 `UnityProject/Assets/Demo02` 中替换项目专属内容。'
        Set-Content -LiteralPath $unityReadmePath -Value $unityReadme -Encoding utf8
    }

    git -C $destinationPath add -A
    if ($LASTEXITCODE -ne 0)
    {
        throw "Failed to stage template cleanup."
    }

    git -C $destinationPath commit -m "chore: initialize project from world template"
    if ($LASTEXITCODE -ne 0)
    {
        throw "Failed to create the initial project commit."
    }
}

Write-Host "New world project created at: $destinationPath"
Write-Host "Branch: main"
Write-Host "Open: $destinationPath\demo02\UnityProject"
