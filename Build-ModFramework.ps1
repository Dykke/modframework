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
#   PS> .\Build-ModFramework.ps1 -SkipKeyCheck         # Skip the .snk/.pub cross-check
#
# Requirements:
#   - MSBuild in PATH (Visual Studio 2022 Community or Build Tools for VS 2022)
#   - For -Configuration Release: ILMerge.exe at ..\Tools\ilmerge\ILMerge.exe
#   - For v6.0: _secure\ModFramework.snk (private key) + keys\ModFramework.pub
#     (public key, must match the .snk)

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$SkipMerge,

    [switch]$SkipKeyCheck
)

$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir '..')).Path
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

function Find-Sn {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\sn.exe"
        "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.2 Tools\sn.exe"
        "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.1 Tools\sn.exe"
        "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.2 Tools\sn.exe"
        "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\sn.exe"
    )
    foreach ($p in $candidates) { if (Test-Path $p) { return $p } }
    $cmd = Get-Command 'sn.exe' -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

$msbuild = Find-MSBuild
Write-Host "Using MSBuild: $msbuild" -ForegroundColor Cyan

# ---- Strong-name keypair check (v6.0) ----
$snkPath = Join-Path $RepoRoot '_secure\ModFramework.snk'
$pubPath = Join-Path $ScriptDir 'keys\ModFramework.pub'

if (-not $SkipKeyCheck) {
    if (-not (Test-Path $snkPath)) {
        throw @"
_STRONG-NAME KEYPAIR MISSING_

Expected: $snkPath

The v6.0 framework is strong-name signed. To generate the keypair:
  1. sn.exe -k _secure\ModFramework.snk     (private key, gitignored)
  2. sn.exe -p _secure\ModFramework.snk modframework\keys\ModFramework.pub

If you have already generated the keypair but the .snk is on another machine,
copy it to _secure\ from your backup. NEVER commit the .snk to git.

To skip this check (not recommended for production builds):
  .\Build-ModFramework.ps1 -SkipKeyCheck
"@
    }

    if (-not (Test-Path $pubPath)) {
        throw @"
_PUBLIC KEY MISSING_

Expected: $pubPath

The public key is the committed source of truth for verifying the signed
ModFramework.dll. Extract it from the .snk with:
  sn.exe -p _secure\ModFramework.snk modframework\keys\ModFramework.pub

Then commit the .pub to git and never commit the .snk.
"@
    }

    # Cross-check: the .pub must have been extracted from the .snk we have now.
    $snExe = Find-Sn
    if ($snExe) {
        Write-Host "Cross-checking .pub was extracted from current .snk..." -ForegroundColor Cyan
        # sn.exe can't read a .snk directly with -t. Workaround: re-extract the
        # pub into %TEMP% and compare tokens. If both .pub files give the same
        # token, the committed .pub was extracted from the .snk we have now.
        $tmpPub = Join-Path $env:TEMP ("mfpub_" + $PID + ".pub")
        $null = & $snExe -p $snkPath $tmpPub 2>&1
        # sn.exe output format: "Public key token is XXXXXXXX" (preceded by a 3-line
        # banner and a blank line). Extract the 16-hex token from that line.
        $tokFromSnk = (& $snExe -t $tmpPub 2>&1 | Where-Object { $_ -match 'Public key token is\s+[0-9a-fA-F]+' } | Select-String 'Public key token is\s+([0-9a-fA-F]+)' | ForEach-Object { $_.Matches[0].Groups[1].Value }) | Select-Object -First 1
        Remove-Item $tmpPub -ErrorAction SilentlyContinue
        $tokFromPub = (& $snExe -t $pubPath 2>&1 | Where-Object { $_ -match 'Public key token is\s+[0-9a-fA-F]+' } | Select-String 'Public key token is\s+([0-9a-fA-F]+)' | ForEach-Object { $_.Matches[0].Groups[1].Value }) | Select-Object -First 1
        if (-not $tokFromSnk -or -not $tokFromPub) {
            Write-Host "WARNING: Could not extract tokens. Check sn.exe output above." -ForegroundColor Yellow
        } elseif ($tokFromSnk.Trim() -ne $tokFromPub.Trim()) {
            throw @"
_KEYPAIR MISMATCH_

The .snk and .pub do not match.
  .snk token: $tokFromSnk
  .pub token: $tokFromPub

This usually means the .pub was extracted from a DIFFERENT keypair than the
.snk on disk. To fix:
  1. sn.exe -p _secure\ModFramework.snk modframework\keys\ModFramework.pub
  2. Commit the new .pub
  3. Re-run this build
"@
        } else {
            Write-Host "Keypair OK (token: $($tokFromSnk.Trim()))" -ForegroundColor Green
        }
    } else {
        Write-Host "sn.exe not found — skipping cross-check. Install Windows SDK to enable." -ForegroundColor Yellow
    }
}

# 1) C# build
Write-Host ""
Write-Host "=== Building ModFramework ($Configuration) ===" -ForegroundColor Cyan
# /p:ILMergeEnabled=false: the .csproj's AfterTargets="Build" ILMerge target would
# otherwise fire and eat 0Harmony/Newtonsoft from bin\Release before our manual
# step below runs. We do the merge in this script instead.
& $msbuild $ProjectFile /t:Build /p:Configuration=$Configuration /p:ILMergeEnabled=false /v:minimal /nologo
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

if (-not (Test-Path $harmony)) {
    Write-Host "0Harmony.dll not found in $outDir — was the build incomplete?" -ForegroundColor Yellow
}

$exclude = Join-Path $ScriptDir 'Tools\ilmerge\ilmerge.exclude'
$excludeArg = ''
if (Test-Path $exclude) { $excludeArg = "/internalize:`"$exclude`"" }

# Game's Managed folder — ILMerge needs this so it can resolve UnityEngine.* and
# Assembly-CSharp references at merge time. Override with $env:ManagedDir if
# your Steam install lives elsewhere.
$managedDir = $env:ManagedDir
if (-not $managedDir) { $managedDir = 'E:\SteamLibrary\steamapps\common\Software Inc\Software Inc_Data\Managed' }
if (-not (Test-Path $managedDir)) {
    Write-Host "ManagedDir not found at $managedDir" -ForegroundColor Yellow
    Write-Host "Set `$env:ManagedDir so ILMerge can resolve Unity refs." -ForegroundColor Yellow
}
$libArg = "/lib:`"$managedDir`""

Write-Host ""
Write-Host "=== ILMerge: merging 0Harmony into ModFramework.dll ===" -ForegroundColor Cyan
# /keyfile: tells ILMerge to re-sign the merged output with our strong-name key.
# Without this, ILMerge produces an unsigned assembly even if the input was
# signed. This is the v6.0 fix for the "ILMerge strips the signature" gotcha.
# /ndebug: skip PDB writing. ILMerge's PDB writer (ISymUnmanagedWriter.Close())
# throws a Catastrophic Failure (0x8000FFFF E_UNEXPECTED) on modern .NET
# Framework when SourceLink or deterministic builds are involved. We don't
# need ILMerge's PDB anyway — the source is open and the PDB is for the
# merged binary's benefit, not ours.
& $ilmergeExe $libArg /ndebug /keyfile:"$snkPath" /out:"$mergedDll" /targetplatform:v4 $excludeArg `
    "$mergedDll" "$harmony"
if ($LASTEXITCODE -ne 0) { throw "ILMerge failed (exit $LASTEXITCODE)" }

# Clean up the now-redundant standalone DLLs
Remove-Item -Path $harmony -ErrorAction SilentlyContinue

# ---- Post-ILMerge: verify the signed token ----
if (-not $SkipKeyCheck) {
    $snExe = Find-Sn
    if ($snExe) {
        Write-Host ""
        Write-Host "=== Verifying signed assembly matches expected pubkey token ===" -ForegroundColor Cyan
        $tokFromDll = (& $snExe -T $mergedDll 2>&1 | Where-Object { $_ -match 'Public key token is\s+[0-9a-fA-F]+' } | Select-String 'Public key token is\s+([0-9a-fA-F]+)' | ForEach-Object { $_.Matches[0].Groups[1].Value }) | Select-Object -First 1
        $tokFromPub = (& $snExe -t $pubPath 2>&1 | Where-Object { $_ -match 'Public key token is\s+[0-9a-fA-F]+' } | Select-String 'Public key token is\s+([0-9a-fA-F]+)' | ForEach-Object { $_.Matches[0].Groups[1].Value }) | Select-Object -First 1
        if ($tokFromDll -and $tokFromPub -and ($tokFromDll.Trim() -eq $tokFromPub.Trim())) {
            Write-Host "Signed assembly token matches: $($tokFromDll.Trim())" -ForegroundColor Green
        } elseif ($tokFromDll) {
            throw "Signed assembly token ($tokFromDll) does not match committed public key token ($tokFromPub). Build aborted."
        } else {
            Write-Host "WARNING: Could not extract token from $mergedDll." -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Green
Write-Host "Output: $mergedDll"
$mergedSize = (Get-Item $mergedDll).Length
Write-Host "Size:   $mergedSize bytes"
