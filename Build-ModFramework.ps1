# Build-ModFramework.ps1
#
# Standalone build script for ModFramework. Does the same thing as a Visual
# Studio "Build Solution" but from a PowerShell prompt. After the C# build
# runs, this script also runs ILMerge to produce a single ModFramework.dll
# containing 0Harmony and Newtonsoft.Json internally.
#
# USAGE:
#   PS> .\Build-ModFramework.ps1                      # Release build, then ILMerge
#   PS> .\Build-ModFramework.ps1 -Configuration Debug  # Debug build, no ILMerge
#   PS> .\Build-ModFramework.ps1 -SkipMerge            # Build only, no ILMerge
#
# Requirements:
#   - MSBuild in PATH (Visual Studio 2022 Community or Build Tools for VS 2022)
#   - For -Configuration Release: ILMerge.exe at ..\Tools\ilmerge\ILMerge.exe

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$SkipMerge
)

$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectFile = Join-Path $ScriptDir 'ModFramework.csproj'

# Pick MSBuild (prefer VS 2022 Community, then Build Tools, then dev shell)
function Find-MSBuild {
    $candidates = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    )
    foreach ($p in $candidates) { if (Test-Path $p) { return $p } }
    # Fallback: assume msbuild is in PATH
    $cmd = Get-Command 'msbuild' -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    throw "MSBuild not found. Install Visual Studio 2022 or Build Tools for VS 2022."
}

$msbuild = Find-MSBuild
Write-Host "Using MSBuild: $msbuild" -ForegroundColor Cyan

# 1) C# build
Write-Host ""
Write-Host "=== Building ModFramework ($Configuration) ===" -ForegroundColor Cyan
& $msbuild $ProjectFile /t:Build /p:Configuration=$Configuration /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw "MSBuild failed (exit $LASTEXITCODE)" }

# 2) ILMerge for Release
$outDir = Join-Path $ScriptDir "bin\$Configuration"
$mergedDll = Join-Path $outDir 'ModFramework.dll'

if ($SkipMerge -or $Configuration -ne 'Release') {
    Write-Host ""
    Write-Host "Skipping ILMerge (either -SkipMerge was passed or Configuration=Debug)." -ForegroundColor Yellow
    return
}

$ilmergeExe = Join-Path $ScriptDir 'Tools\ilmerge\ILMerge.exe'
if (-not (Test-Path $ilmergeExe)) {
    Write-Host ""
    Write-Host "ILMerge.exe not found at $ilmergeExe" -ForegroundColor Yellow
    Write-Host "Download it from https://www.nuget.org/packages/ilmerge/ and drop it in Tools\ilmerge\" -ForegroundColor Yellow
    Write-Host "Skipping merge step — output will be multi-DLL." -ForegroundColor Yellow
    return
}

$harmony = Join-Path $outDir '0Harmony.dll'
$newtonsoft = Join-Path $outDir 'Newtonsoft.Json.dll'

if (-not (Test-Path $harmony)) {
    Write-Host "0Harmony.dll not found in $outDir — was the build incomplete?" -ForegroundColor Yellow
}
if (-not (Test-Path $newtonsoft)) {
    Write-Host "Newtonsoft.Json.dll not found in $outDir — was the build incomplete?" -ForegroundColor Yellow
}

$exclude = Join-Path $ScriptDir 'Tools\ilmerge\ilmerge.exclude'
$excludeArg = ''
if (Test-Path $exclude) { $excludeArg = "/internalize:`"$exclude`"" }

Write-Host ""
Write-Host "=== ILMerge: merging 0Harmony + Newtonsoft.Json into ModFramework.dll ===" -ForegroundColor Cyan
& $ilmergeExe /out:"$mergedDll" /targetplatform:v4 $excludeArg /nologo `
    "$mergedDll" "$harmony" "$newtonsoft"
if ($LASTEXITCODE -ne 0) { throw "ILMerge failed (exit $LASTEXITCODE)" }

# Clean up the now-redundant standalone DLLs
Remove-Item -Path $harmony -ErrorAction SilentlyContinue
Remove-Item -Path $newtonsoft -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Green
Write-Host "Output: $mergedDll"
Write-Host "Size:   $((Get-Item $mergedDll).Length) bytes"
