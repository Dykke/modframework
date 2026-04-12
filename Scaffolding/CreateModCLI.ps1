param(
    [Parameter(Mandatory=$true)]
    [string]$ModName,

    [Parameter(Mandatory=$false)]
    [string]$GameDir,

    [Parameter(Mandatory=$false)]
    [switch]$Build
)

$ErrorActionPreference = "Stop"

$ConfiguredFlag = Join-Path $PSScriptRoot ".framework-configured"

# Load cached GameDir if not provided
if (-not $GameDir -and (Test-Path $ConfiguredFlag)) {
    $GameDir = (Get-Content $ConfiguredFlag -Raw).Trim()
    Write-Host "Using cached GameDir: $GameDir" -ForegroundColor DarkGray
}

if (-not $GameDir) {
    Write-Host "GameDir is required on first run. Please re-run providing -GameDir <path>" -ForegroundColor Red
    exit 1
}

$TemplatesDir = Join-Path $PSScriptRoot "Templates"
$TargetDir = Join-Path (Split-Path (Split-Path $PSScriptRoot)) $ModName

if (Test-Path $TargetDir) {
    Write-Host "Directory $TargetDir already exists. Please choose a different mod name." -ForegroundColor Red
    exit 1
}

Write-Host "Creating ModFramework Mod: $ModName" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null

$Guid = [guid]::NewGuid().ToString().ToUpper()

Write-Host "Copying templates..."
# 1. Main Behaviour
$BehaviourContent = Get-Content (Join-Path $TemplatesDir "MainBehaviour.cs_template") -Raw
$BehaviourContent = $BehaviourContent -replace '{MOD_NAME}', $ModName
Set-Content (Join-Path $TargetDir "${ModName}Behaviour.cs") $BehaviourContent

# 2. ModMeta.json
$MetaContent = Get-Content (Join-Path $TemplatesDir "ModMeta.json_template") -Raw
$MetaContent = $MetaContent -replace '{MOD_NAME}', $ModName
Set-Content (Join-Path $TargetDir "ModMeta.json") $MetaContent

# 3. .csproj
$CsprojContent = Get-Content (Join-Path $TemplatesDir "Mod.csproj_template") -Raw
$CsprojContent = $CsprojContent -replace '{MOD_NAME}', $ModName
$CsprojContent = $CsprojContent -replace '{NEW_GUID}', $Guid
$CsprojContent = $CsprojContent -replace '{GAME_DIRECTORY}', $GameDir
Set-Content (Join-Path $TargetDir "${ModName}.csproj") $CsprojContent

# 4. meta.tyd (required by Software Inc for mod discovery)
$TydContent = Get-Content (Join-Path $TemplatesDir "meta.tyd_template") -Raw
$TydContent = $TydContent -replace '{MOD_NAME}', $ModName
Set-Content (Join-Path $TargetDir "meta.tyd") $TydContent

#5. ModFramework.csproj (only on first run)
$FrameworkCsproj = Join-Path (Split-Path $PSScriptRoot) "ModFramework.csproj"
if (-not (Test-Path $ConfiguredFlag)) {
    $CsprojContent2 = Get-Content (Join-Path $TemplatesDir "ModFramework.csproj_template") -Raw
    $CsprojContent2 = $CsprojContent2 -replace '{GAME_DIRECTORY}', $GameDir
    Set-Content $FrameworkCsproj $CsprojContent2
    Set-Content $ConfiguredFlag $GameDir
    Write-Host "Successfully mapped framework to Game Installation at $GameDir !" -ForegroundColor Green
}

Write-Host ""
Write-Host "Successfully generated '$ModName' at $TargetDir !" -ForegroundColor Green
Write-Host "Successfully mapped '$ModName' to Game Installation at $GameDir !" -ForegroundColor Green

$BuiltFlag = Join-Path $PSScriptRoot ".framework-built"
if (-not $Build -and -not (Test-Path $BuiltFlag)) {
    $response = Read-Host "Would you like to build ModFramework now? (y/n)"
    if ($response -eq 'y') {
        $Build = $true
    }
}

if ($Build) {
    Write-Host "Building ModFramework..." -ForegroundColor Cyan
    $FrameworkDir = Split-Path $PSScriptRoot
    $BuildResult = & dotnet build $FrameworkDir -c Release 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "ModFramework built successfully!" -ForegroundColor Green
        New-Item -ItemType File -Force -Path (Join-Path $PSScriptRoot ".framework-built") | Out-Null
    } else {
        Write-Host "ModFramework build failed:" -ForegroundColor Red
        Write-Host ($BuildResult | Out-String)
        exit 1
    }
    Write-Host ""
}

Write-Host "Next Steps:"
if (-not $Build) {
    Write-Host "1. Add existing project $ModName.csproj to your Visual Studio Solution."
    Write-Host "2. Make sure ModFramework is built first."
    Write-Host "3. Build your new mod. The post-build event will automatically copy it to your local game's Mods folder."
} else {
    Write-Host "1. Add existing project $ModName.csproj to your Visual Studio Solution."
    Write-Host "2. Build your new mod. The post-build event will automatically copy it to your local game's Mods folder."
}
Write-Host ""
