[CmdletBinding(DefaultParameterSetName='Create')]
param(
    [Parameter(Mandatory=$false, ParameterSetName='Create')]
    [string]$ModName,

    [Parameter(Mandatory=$false, ParameterSetName='Create')]
    [string]$GameDir,

    [Parameter(Mandatory=$false, ParameterSetName='Create')]
    [string]$ModDir,

    [Parameter(Mandatory=$false, ParameterSetName='Create')]
    [switch]$Build,

    [Parameter(Mandatory=$false, ParameterSetName='Create')]
    [switch]$NonInteractive,

    [Parameter(Mandatory=$true, ParameterSetName='Help')]
    [switch]$Help
)

$ErrorActionPreference = "Stop"

function Read-HostOrExit {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Prompt
    )

    try {
        $value = Read-Host $Prompt
    } catch {
        Write-Host "`nInput cancelled. Exiting." -ForegroundColor Yellow
        exit 1
    }

    if ($null -eq $value) {
        Write-Host "`nInput cancelled. Exiting." -ForegroundColor Yellow
        exit 1
    }

    return $value
}

function NormalizePathInput {
    param([string]$PathValue)

    if ($null -eq $PathValue) {
        return ""
    }

    $normalized = $PathValue.Trim()
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return ""
    }

    while (
        $normalized.Length -ge 2 -and
        (
            (($normalized.StartsWith('"')) -and ($normalized.EndsWith('"'))) -or
            (($normalized.StartsWith("'")) -and ($normalized.EndsWith("'")))
        )
    ) {
        $normalized = $normalized.Substring(1, $normalized.Length - 2).Trim()
    }

    return $normalized
}

function Write-PromptGuidance {
    Write-Host "For help, run: .\CreateMod.ps1 -Help" -ForegroundColor Yellow
    Write-Host "Press Ctrl+C to exit." -ForegroundColor Yellow
}

function Test-ValidModName {
    param([string]$Name)
    return ($Name -match '^[A-Za-z_][A-Za-z0-9_]*$')
}

function Resolve-ModName {
    param([string]$InitialValue)

    if (Test-ValidModName $InitialValue) {
        return $InitialValue
    }

    Write-Host "ERROR: Valid ModName Required." -ForegroundColor Red
    if (-not [string]::IsNullOrWhiteSpace($InitialValue)) {
        Write-Host "ERROR: ModName must be a valid C# identifier (letters, numbers, underscores, cannot start with a number)." -ForegroundColor Red
        Write-Host "Got: '$InitialValue'" -ForegroundColor Yellow
    }
    Write-PromptGuidance

    while ($true) {
        $candidate = Read-HostOrExit "Enter ModName"
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            Write-Host "ERROR: ModName can't be blank and must be valid" -ForegroundColor Red
            continue
        }

        if (Test-ValidModName $candidate) {
            return $candidate
        }

        Write-Host "ERROR: ModName must be a valid C# identifier (letters, numbers, underscores, cannot start with a number)." -ForegroundColor Red
        Write-Host "Got: '$candidate'" -ForegroundColor Yellow
    }
}

function Test-ValidDir {
    param(
        [string]$Path,
        [string]$RequiredRelativePath,
        [ref]$ExpectedPath
    )

    $candidate = NormalizePathInput -PathValue $Path
    if ([string]::IsNullOrWhiteSpace($candidate)) {
        return $null
    }

    $candidate = $candidate.TrimEnd([char[]]@([IO.Path]::DirectorySeparatorChar, '/'))

    if ([string]::IsNullOrWhiteSpace($RequiredRelativePath)) {
        if (Test-Path $candidate -PathType Container) {
            $ExpectedPath.Value = $candidate
            return $candidate
        }

        $ExpectedPath.Value = $candidate
        return $null
    }

    $ExpectedPath.Value = Join-Path $candidate $RequiredRelativePath
    if (Test-Path $ExpectedPath.Value) {
        return $candidate
    }

    return $null
}

function Resolve-Dir {
    param(
        [string]$InitialValue,
        [string]$Prompt,
        [bool]$WasSupplied,
        [bool]$AllowMissing,
        [string]$RequiredRelativePath,
        [string]$RequiredErrorMessage,
        [string]$BlankSuppliedErrorMessage,
        [string]$BlankInputErrorMessage,
        [string]$InvalidPathErrorTemplate,
        [string[]]$Examples
    )

    if (-not $WasSupplied -and $AllowMissing) {
        return $null
    }

    $expectedPath = ""
    $normalizedInitial = NormalizePathInput -PathValue $InitialValue
    $validated = Test-ValidDir -Path $InitialValue -RequiredRelativePath $RequiredRelativePath -ExpectedPath ([ref]$expectedPath)
    if ($validated) {
        return $validated
    }

    if ($WasSupplied -and [string]::IsNullOrWhiteSpace($normalizedInitial) -and -not [string]::IsNullOrWhiteSpace($BlankSuppliedErrorMessage)) {
        Write-Host $BlankSuppliedErrorMessage -ForegroundColor Red
    } elseif (-not [string]::IsNullOrWhiteSpace($normalizedInitial)) {
        if (-not [string]::IsNullOrWhiteSpace($InvalidPathErrorTemplate)) {
            Write-Host ([string]::Format($InvalidPathErrorTemplate, $normalizedInitial)) -ForegroundColor Red
        }
        if (-not [string]::IsNullOrWhiteSpace($RequiredRelativePath)) {
            Write-Host "Expected to find: $expectedPath" -ForegroundColor Yellow
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($RequiredErrorMessage)) {
        Write-Host $RequiredErrorMessage -ForegroundColor Red
    }

    Write-PromptGuidance
    foreach ($example in $Examples) {
        Write-Host $example -ForegroundColor Yellow
    }

    while ($true) {
        $candidate = Read-HostOrExit $Prompt
        $normalizedCandidate = NormalizePathInput -PathValue $candidate

        if ([string]::IsNullOrWhiteSpace($normalizedCandidate)) {
            if (-not [string]::IsNullOrWhiteSpace($BlankInputErrorMessage)) {
                Write-Host $BlankInputErrorMessage -ForegroundColor Red
            }
            continue
        }

        $validated = Test-ValidDir -Path $normalizedCandidate -RequiredRelativePath $RequiredRelativePath -ExpectedPath ([ref]$expectedPath)
        if ($validated) {
            return $validated
        }

        if (-not [string]::IsNullOrWhiteSpace($InvalidPathErrorTemplate)) {
            Write-Host ([string]::Format($InvalidPathErrorTemplate, $normalizedCandidate)) -ForegroundColor Red
        }
        if (-not [string]::IsNullOrWhiteSpace($RequiredRelativePath)) {
            Write-Host "Expected to find: $expectedPath" -ForegroundColor Yellow
        }
    }
}

function Resolve-GameDir {
    param([string]$InitialValue)

    return Resolve-Dir `
        -InitialValue $InitialValue `
        -Prompt "Enter GameDir" `
        -WasSupplied $true `
        -AllowMissing $false `
        -RequiredRelativePath "Software Inc_Data\Managed\Assembly-CSharp.dll" `
        -RequiredErrorMessage "ERROR: Valid GameDir Required." `
        -BlankSuppliedErrorMessage $null `
        -BlankInputErrorMessage $null `
        -InvalidPathErrorTemplate "ERROR: '{0}' does not look like a Software Inc installation." `
        -Examples @(
            'Example: .\CreateMod.ps1 -ModName MyMod -GameDir "E:\SteamLibrary\steamapps\common\Software Inc"',
            'Optional: .\CreateMod.ps1 -ModName MyMod -GameDir "E:\SteamLibrary\steamapps\common\Software Inc" -ModDir "C:\MyMod"'
        )
}

function Resolve-ModDir {
    param(
        [string]$InitialValue,
        [bool]$WasSupplied
    )

    return Resolve-Dir `
        -InitialValue $InitialValue `
        -Prompt "Enter ModDir" `
        -WasSupplied $WasSupplied `
        -AllowMissing $true `
        -RequiredRelativePath "" `
        -RequiredErrorMessage $null `
        -BlankSuppliedErrorMessage "ERROR: ModDir was supplied but is blank." `
        -BlankInputErrorMessage "ERROR: ModDir cannot be blank when supplied." `
        -InvalidPathErrorTemplate "ERROR: ModDir does not exist: {0}" `
        -Examples @()
}

function Invoke-ModFrameworkBuild {
    param(
        [Parameter(Mandatory=$true)]
        [string]$ProjectRoot,

        [Parameter(Mandatory=$true)]
        [string]$BuildFile
    )

    Write-Host "Building ModFramework..." -ForegroundColor Cyan
    Push-Location $ProjectRoot
    try {
        & dotnet build -c Release
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR: ModFramework build failed with exit code $LASTEXITCODE." -ForegroundColor Red
            Write-Host "Skipping ModFramework build and continuing mod scaffolding." -ForegroundColor Yellow
            Write-Host "Fix the build errors and rerun this script with the -Build flag or build ModFramework.csproj manually." -ForegroundColor Yellow
            return $false
        }
    } finally {
        Pop-Location
    }

    Write-Host "ModFramework build complete." -ForegroundColor Green
    Set-Content $BuildFile $true -Encoding UTF8
    return $true
}

function PromptBuild {
    param(
        [Parameter(Mandatory=$true)]
        [string]$ProjectRoot,

        [Parameter(Mandatory=$true)]
        [string]$BuildFile,

        [Parameter(Mandatory=$true)]
        [ref]$SkippedBuild,

        [Parameter(Mandatory=$true)]
        [ref]$BuildPromptShown,

        [switch]$NonInteractive
    )

    if ((Test-Path $BuildFile) -or $SkippedBuild.Value -or $BuildPromptShown.Value) {
        return
    }

    if ($NonInteractive) {
        Write-Host "Skipping ModFramework build (non-interactive mode). Build it later with -Build or via Visual Studio." -ForegroundColor Yellow
        $SkippedBuild.Value = $true
        return
    }

    $BuildPromptShown.Value = $true
    $response = Read-HostOrExit "Would you like to build ModFramework now? (Y/N)"
    $normalizedResponse = $response.Trim()
    if (@('Y', 'Yes', 'y', 'yes') -contains $normalizedResponse) {
        if (-not (Invoke-ModFrameworkBuild -ProjectRoot $ProjectRoot -BuildFile $BuildFile)) {
            $SkippedBuild.Value = $true
        }
        return
    }

    Write-Host "Skipping ModFramework build. Remember to build it before building your mod!" -ForegroundColor Yellow
    Write-Host "You can build ModFramework later by running this script with the -Build flag or by building the ModFramework.csproj in Visual Studio." -ForegroundColor Cyan
    $SkippedBuild.Value = $true
}

function Show-HelpMenu {
    Write-Host "CreateMod.ps1 - ModFramework scaffolding tool that generates a complete, ready-to-build mod project with all references pre-configured." -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\CreateMod.ps1 -ModName <Name> [-GameDir <Path>] [-ModDir <Path>] [-Build]"
    Write-Host "  .\CreateMod.ps1 -Help"
    Write-Host ""
    Write-Host "Parameters:" -ForegroundColor Yellow
    Write-Host "  -ModName  Required. New mod name (must be a valid C# identifier)."
    Write-Host "  -GameDir  Optional after first run. Software Inc install folder."
    Write-Host "  -ModDir   Optional. Output base directory for generated mod files."
    Write-Host "  -Build    Optional flag. Build ModFramework immediately."
    Write-Host "  -Help     Show this help menu."
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host '  .\CreateMod.ps1 -ModName MyMod -GameDir "S:\SteamLibrary\steamapps\common\Software Inc\"'
    Write-Host '  .\CreateMod.ps1 -ModName MyMod -GameDir "S:\SteamLibrary\steamapps\common\Software Inc\" -Build'
    Write-Host '  .\CreateMod.ps1 -ModName MyMod -GameDir "S:\SteamLibrary\steamapps\common\Software Inc\" -ModDir "C:\MyMods\"'
    Write-Host '  .\CreateMod.ps1 -ModName MyMod -GameDir "S:\SteamLibrary\steamapps\common\Software Inc\" -ModDir "C:\MyMods\" -Build'
    Write-Host '  .\CreateMod.ps1 -ModName MyMod -ModDir "C:\Mods\"'
}

function Show-NextSteps {
    param(
        [Parameter(Mandatory=$true)]
        [bool]$IsModFrameworkBuilt,

        [Parameter(Mandatory=$true)]
        [string]$ModName
    )

    if ($IsModFrameworkBuilt) {
        Write-Host "Next Steps:" -ForegroundColor Green
        Write-Host "1. Add existing project $ModName.csproj to your Visual Studio Solution."
        Write-Host "2. Build your new mod. The post-build event will automatically copy it to your local game's Mods folder."
    } else {
        Write-Host "Next Steps:"
        Write-Host "1. Add existing project $ModName.csproj to your Visual Studio Solution."
        Write-Host "2. Make sure ModFramework is built."
        Write-Host "3. Build your new mod. The post-build event will automatically copy it to your local game's Mods folder."
    }

    Write-Host ""
}

function Set-TemplatedFile {
    param(
        [Parameter(Mandatory=$true)]
        [string]$TemplatePath,

        [Parameter(Mandatory=$true)]
        [string]$OutputPath,

        [Parameter(Mandatory=$false)]
        [hashtable]$Tokens
    )

    $content = Get-Content $TemplatePath -Raw
    if ($Tokens) {
        foreach ($key in $Tokens.Keys) {
            $content = $content.Replace("{$key}", [string]$Tokens[$key])
        }
    }

    Set-Content $OutputPath $content -Encoding UTF8
}

function Show-Summary {
    param(
        [Parameter(Mandatory=$true)]
        [string]$ModName,

        [Parameter(Mandatory=$true)]
        [string]$TargetDir,

        [Parameter(Mandatory=$true)]
        [string]$GameDir,

        [Parameter(Mandatory=$true)]
        [bool]$IsModFrameworkBuilt,

        [Parameter(Mandatory=$false)]
        [string]$ModFrameworkBin,

        [Parameter(Mandatory=$true)]
        [bool]$ModFrameworkCsprojUpdated
    )

    Write-Host "Summary:" -ForegroundColor Green
    Write-Host "- Generated mod project: $ModName"
    Write-Host "- $ModName generated at: $TargetDir"
    Write-Host "- $ModName references Game at: $GameDir"
    Write-Host "- $ModName references ModFramework  and 0Harmony at $ModFrameworkBin"

    if ($ModFrameworkCsprojUpdated) {
        Write-Host "- ModFramework updated to reference game at: $GameDir"
    }

    if ($IsModFrameworkBuilt) {
        Write-Host "- ModFramework build status: Built"
    } else {
        Write-Host "- ModFramework build status: Not built" -ForegroundColor Yellow
    }

    Write-Host ""
}

if ($Help) {
    Show-HelpMenu
    exit 0
}

$ModName = Resolve-ModName -InitialValue $ModName

$ConfigFile = Join-Path $PSScriptRoot ".game-directory"
$IsFirstRun = -not (Test-Path $ConfigFile)
$BuildFile = Join-Path $PSScriptRoot ".modframework-built"
$ModFrameworkDll = Join-Path (Split-Path $PSScriptRoot) "bin\Release\ModFramework.dll"
$SkippedBuild = $false
$BuildPromptShown = $false
$ModFrameworkCsprojUpdated = $false

if (Test-Path $ModFrameworkDll) {
    Set-Content $BuildFile $true -Encoding UTF8
}

if (-not $GameDir -and (Test-Path $ConfigFile)) {
    $GameDir = NormalizePathInput -PathValue ((Get-Content $ConfigFile -Raw).Trim())
    Write-Host "Using cached game directory: $GameDir" -ForegroundColor DarkGray
}

$GameDir = Resolve-GameDir -InitialValue $GameDir
Set-Content $ConfigFile $GameDir -Encoding UTF8

if ($IsFirstRun -or $PSBoundParameters.ContainsKey('GameDir')) {
    if ($IsFirstRun) {
        Write-Host "First run detected. Setting up ModFramework.csproj..." -ForegroundColor Cyan
    } else {
        Write-Host "GameDir provided. Refreshing ModFramework.csproj HintPaths..." -ForegroundColor Cyan
    }

    $RootDir = Split-Path $PSScriptRoot
    $CsprojTemplate = Join-Path $PSScriptRoot "Templates\ModFramework.csproj_template"
    $RootCsprojPath = Join-Path $RootDir "ModFramework.csproj"

    if (Test-Path $CsprojTemplate) {
        $CsprojContent = (Get-Content $CsprojTemplate -Raw).Replace('{GAME_DIRECTORY}', $GameDir)
        Set-Content $RootCsprojPath $CsprojContent -Encoding UTF8
        $ModFrameworkCsprojUpdated = $true
        Write-Host "Updated ModFramework.csproj with game directory: $GameDir" -ForegroundColor Green

        if (-not $Build) {
            PromptBuild -ProjectRoot $RootDir -BuildFile $BuildFile -SkippedBuild ([ref]$SkippedBuild) -BuildPromptShown ([ref]$BuildPromptShown) -NonInteractive:$NonInteractive
        }
    } else {
        Write-Host "WARNING: Could not find ModFramework.csproj_template at $CsprojTemplate" -ForegroundColor Yellow
    }
}

$TemplatesDir = Join-Path $PSScriptRoot "Templates"
$ModDir = Resolve-ModDir -InitialValue $ModDir -WasSupplied $PSBoundParameters.ContainsKey('ModDir')

while ($true) {
    if ($ModDir) {
        $TargetDir = Join-Path $ModDir $ModName
    } else {
        $TargetDir = Join-Path (Split-Path (Split-Path $PSScriptRoot)) $ModName
    }

    if (-not (Test-Path $TargetDir)) {
        break
    }

    Write-PromptGuidance
    Write-Host "ERROR: Directory $TargetDir already exists. Please choose a different mod name." -ForegroundColor Red
    $ModName = Resolve-ModName -InitialValue $null
}

Write-Host "Creating ModFramework Mod: $ModName" -ForegroundColor Cyan
Write-Host "Game Directory: $GameDir" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null

$Guid = [guid]::NewGuid().ToString().ToUpper()

Write-Host "Copying templates..."
# 1. Main Behaviour
$BehaviourContent = Get-Content (Join-Path $TemplatesDir "MainBehaviour.cs_template") -Raw
$BehaviourContent = $BehaviourContent.Replace('{MOD_NAME}', $ModName)
Set-Content (Join-Path $TargetDir "${ModName}Behaviour.cs") $BehaviourContent -Encoding UTF8

# 2. ModMeta (required by Software Inc DLL loader)
$MetaContent = Get-Content (Join-Path $TemplatesDir "MainMeta.cs_template") -Raw
$MetaContent = $MetaContent.Replace('{MOD_NAME}', $ModName)
Set-Content (Join-Path $TargetDir "${ModName}Meta.cs") $MetaContent -Encoding UTF8

# 3. .csproj (with game directory substitution)
$CsprojContent = Get-Content (Join-Path $TemplatesDir "Mod.csproj_template") -Raw
$CsprojContent = $CsprojContent.Replace('{MOD_NAME}', $ModName)
$CsprojContent = $CsprojContent.Replace('{NEW_GUID}', $Guid)
$CsprojContent = $CsprojContent.Replace('{GAME_DIRECTORY}', $GameDir)
Set-Content (Join-Path $TargetDir "${ModName}.csproj") $CsprojContent -Encoding UTF8

# 4. meta.tyd (required by Software Inc for mod discovery)
$TydContent = Get-Content (Join-Path $TemplatesDir "meta.tyd_template") -Raw
$TydContent = $TydContent.Replace('{MOD_NAME}', $ModName)
Set-Content (Join-Path $TargetDir "meta.tyd") $TydContent -Encoding UTF8

# 5. UI.xml (starter XML for native UI)
$UIContent = Get-Content (Join-Path $TemplatesDir "UI.xml_template") -Raw
$UIContent = $UIContent.Replace('{MOD_NAME}', $ModName)
Set-Content (Join-Path $TargetDir "UI.xml") $UIContent -Encoding UTF8

Write-Host ""
Write-Host "Successfully generated '$ModName' at $TargetDir !" -ForegroundColor Green
Write-Host "Game references point to: $GameDir" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:"
Write-Host "1. Add existing project $ModName.csproj to your Visual Studio Solution."
Write-Host "2. Build your new mod. The post-build event will automatically copy it to your local game's Mods folder."
Write-Host ""
