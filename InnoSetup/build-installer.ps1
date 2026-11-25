#Requires -Version 5.1
<#
.SYNOPSIS
    编译 MastercardHost（Release）并生成 Inno Setup 安装包。

.DESCRIPTION
    1. dotnet build -c <Configuration>
    2. 生成 InnoSetup\payload：构建输出（含 zh-CN 卫星资源）+ 工程根目录 Config 配置文件
    3. 调用 Inno Setup 命令行编译器（ISCC.exe）生成安装包

    安装包输出到本脚本所在目录，文件名与大小由 setup.iss 中的
    OutputBaseFilename / OutputDir 决定。

.PARAMETER Configuration
    构建配置，默认 Release。

.PARAMETER IsccPath
    ISCC.exe 的完整路径；省略时依次从 PATH、注册表安装信息、默认安装目录查找。

.EXAMPLE
    pwsh -File .\InnoSetup\build-installer.ps1

.EXAMPLE
    pwsh -File .\InnoSetup\build-installer.ps1 -Configuration Release -IsccPath 'E:\Inno Setup 6\ISCC.exe'
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$IsccPath = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Find-Iscc {
    $command = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    $candidates = New-Object System.Collections.Generic.List[string]
    foreach ($base in @($env:ProgramFiles, ${env:ProgramFiles(x86)})) {
        if (-not [string]::IsNullOrWhiteSpace($base)) {
            $candidates.Add((Join-Path $base 'Inno Setup 6\ISCC.exe'))
        }
    }

    $uninstallKeys = @(
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
        'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1'
    )
    foreach ($key in $uninstallKeys) {
        if (Test-Path -LiteralPath $key) {
            $location = (Get-ItemProperty -LiteralPath $key -ErrorAction SilentlyContinue).InstallLocation
            if (-not [string]::IsNullOrWhiteSpace($location)) {
                $candidates.Add((Join-Path $location 'ISCC.exe'))
            }
        }
    }

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw '未找到 Inno Setup 编译器 ISCC.exe。请安装 Inno Setup 6，或用 -IsccPath 指定路径。'
}

$scriptDir   = $PSScriptRoot
$projectRoot = Split-Path -Parent $scriptDir
$projectFile = Join-Path $projectRoot 'MastercardHost.csproj'
$buildDir    = Join-Path $projectRoot (Join-Path 'bin' (Join-Path $Configuration 'net48'))
$configDir   = Join-Path $projectRoot 'Config'
$payloadDir  = Join-Path $scriptDir 'payload'
$issFile     = Join-Path $scriptDir 'setup.iss'

if (-not (Test-Path -LiteralPath $configDir)) {
    throw "未找到配置文件目录：$configDir"
}

# ---------------------------------------------------------------------------
# 1. 编译
# ---------------------------------------------------------------------------
Write-Host "==> [1/3] dotnet build -c $Configuration" -ForegroundColor Cyan
& dotnet build $projectFile -c $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build 失败，退出码 $LASTEXITCODE"
}

$exePath = Join-Path $buildDir 'MastercardHost.exe'
if (-not (Test-Path -LiteralPath $exePath)) {
    throw "未找到构建输出：$exePath"
}

# ---------------------------------------------------------------------------
# 2. 准备打包目录
# ---------------------------------------------------------------------------
Write-Host "==> [2/3] 生成 payload 目录：$payloadDir" -ForegroundColor Cyan
if (Test-Path -LiteralPath $payloadDir) {
    Remove-Item -LiteralPath $payloadDir -Recurse -Force
}
New-Item -ItemType Directory -Path $payloadDir | Out-Null

# 构建输出：exe、依赖 dll、*.config、nlog.config、pdb 以及 zh-CN 卫星资源
Copy-Item -Path (Join-Path $buildDir '*') -Destination $payloadDir -Recurse -Force
# 运行时配置文件：AppDomain.SetupInformation.ApplicationBase + "Config\Config|CAPK|Revocation_CAPK"
Copy-Item -LiteralPath $configDir -Destination (Join-Path $payloadDir 'Config') -Recurse -Force

foreach ($required in @(
        (Join-Path $payloadDir 'MastercardHost.exe'),
        (Join-Path $payloadDir 'MastercardHost.exe.config'),
        (Join-Path $payloadDir 'nlog.config'),
        (Join-Path $payloadDir 'Config\Config'),
        (Join-Path $payloadDir 'Config\CAPK\PAYPASS_CAPK.json'),
        (Join-Path $payloadDir 'Config\Revocation_CAPK\PAYPASS_Revokey.json'),
        (Join-Path $payloadDir 'zh-CN\MastercardHost.resources.dll'))) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "payload 缺少必需内容：$required"
    }
}

$payloadFiles = @(Get-ChildItem -LiteralPath $payloadDir -Recurse -File)
$configFiles  = @(Get-ChildItem -LiteralPath (Join-Path $payloadDir 'Config') -Recurse -File)
$payloadSize  = [math]::Round((($payloadFiles | Measure-Object -Property Length -Sum).Sum / 1MB), 2)
Write-Host ("    payload 共 {0} 个文件 / {1} MB，其中 Config 配置文件 {2} 个" -f $payloadFiles.Count, $payloadSize, $configFiles.Count)

# ---------------------------------------------------------------------------
# 3. Inno Setup 编译
# ---------------------------------------------------------------------------
Write-Host "==> [3/3] Inno Setup 编译" -ForegroundColor Cyan
if ([string]::IsNullOrWhiteSpace($IsccPath)) {
    $IsccPath = Find-Iscc
}
Write-Host "    ISCC: $IsccPath"

# Inno Setup 6 只有在脚本带 UTF-8 BOM 时才按 Unicode 解析，否则 [Code] 段中的
# 中文字符串会编译失败（String error）。这里自动补齐 BOM，避免编辑器保存差异。
$issBytes = [System.IO.File]::ReadAllBytes($issFile)
$hasBom   = $issBytes.Length -ge 3 -and $issBytes[0] -eq 0xEF -and $issBytes[1] -eq 0xBB -and $issBytes[2] -eq 0xBF
if (-not $hasBom) {
    Write-Warning 'setup.iss 缺少 UTF-8 BOM，已自动补写（否则中文字符串无法编译）。'
    $issText = [System.IO.File]::ReadAllText($issFile, [System.Text.Encoding]::UTF8)
    [System.IO.File]::WriteAllText($issFile, $issText, (New-Object System.Text.UTF8Encoding($true)))
}

& $IsccPath "/Qp" $issFile
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup 编译失败，退出码 $LASTEXITCODE"
}

$outputBaseName = (Select-String -LiteralPath $issFile -Pattern '^\s*OutputBaseFilename\s*=\s*(\S+)\s*$').Matches[0].Groups[1].Value
$installerPath  = Join-Path $scriptDir ($outputBaseName + '.exe')
if (-not (Test-Path -LiteralPath $installerPath)) {
    throw "未找到生成的安装包：$installerPath"
}

$installer = Get-Item -LiteralPath $installerPath
Write-Host ("==> 完成：{0} ({1} MB)" -f $installer.FullName, [math]::Round(($installer.Length / 1MB), 2)) -ForegroundColor Green
