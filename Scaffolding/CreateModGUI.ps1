Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName Microsoft.VisualBasic

# Hide the PowerShell console window
Add-Type -Name Win32 -Namespace Native -MemberDefinition '
    [DllImport("kernel32.dll")] public static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
'
[Native.Win32]::ShowWindow([Native.Win32]::GetConsoleWindow(), 0) | Out-Null

$ErrorActionPreference = "Stop"

$ConfiguredFlag = Join-Path $PSScriptRoot ".framework-configured"

# Load cached GameDir or prompt via folder browser on first run
$GameDir = $null
if (Test-Path $ConfiguredFlag) {
    $GameDir = (Get-Content $ConfiguredFlag -Raw).Trim()
} else {
    $folderBrowser = New-Object System.Windows.Forms.FolderBrowserDialog
    $folderBrowser.Description = "Select the Software Inc game installation directory"
    $folderBrowser.ShowNewFolderButton = $false
    if ($folderBrowser.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
        $GameDir = $folderBrowser.SelectedPath
    } else {
        [System.Windows.Forms.MessageBox]::Show("Game directory selection cancelled.", "Cancelled", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Warning)
        exit 1
    }
}

# Prompt for Mod Name via input box
$ModName = [Microsoft.VisualBasic.Interaction]::InputBox("Enter the name for your new mod:", "Mod Name")
if ([string]::IsNullOrWhiteSpace($ModName)) {
    [System.Windows.Forms.MessageBox]::Show("Mod name cannot be empty.", "Error", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error)
    exit 1
}

$TemplatesDir = Join-Path $PSScriptRoot "Templates"
$TargetDir = Join-Path (Split-Path (Split-Path $PSScriptRoot)) $ModName

if (Test-Path $TargetDir) {
    [System.Windows.Forms.MessageBox]::Show("Directory '$ModName' already exists. Please choose a different mod name.", "Error", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error)
    exit 1
}

New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null

$Guid = [guid]::NewGuid().ToString().ToUpper()
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
    $config = "Successfully mapped framework to Game Installation at $GameDir !"
}

$BuiltFlag = Join-Path $PSScriptRoot ".framework-built"
$DoBuild = $false

if (-not (Test-Path $BuiltFlag)) {
    $buildResponse = [System.Windows.Forms.MessageBox]::Show(
        "Would you like to build ModFramework now?",
        "Build ModFramework",
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Question
    )
    if ($buildResponse -eq [System.Windows.Forms.DialogResult]::Yes) {
        $DoBuild = $true
    }
}

if ($DoBuild) {
    $FrameworkDir = Split-Path $PSScriptRoot
    $BuildResult = & dotnet build $FrameworkDir -c Release 2>&1
    if ($LASTEXITCODE -eq 0) {
        New-Item -ItemType File -Force -Path $BuiltFlag | Out-Null
        $nextSteps = "Next Steps:`n1. Add existing project $ModName.csproj to your Visual Studio Solution.`n2. Build your new mod. The post-build event will automatically copy it to your local game's Mods folder."
        $msg = "Successfully generated '$ModName' at:`n$TargetDir`nSuccessfully mapped '$ModName' to Game Installation at $GameDir !`n`nModFramework built successfully!`n`n$nextSteps"
        if ($config) { $fullMsg = $config + "`n" + $msg } else { $fullMsg = $msg }
        [System.Windows.Forms.MessageBox]::Show($fullMsg, "Success", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information)
    } else {
        $errorText = ($BuildResult | Out-String)
        [System.Windows.Forms.MessageBox]::Show("ModFramework build failed:`n`n$errorText", "Build Failed", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error)
        exit 1
    }
} else {
    $nextSteps = "Next Steps:`n1. Add existing project $ModName.csproj to your Visual Studio Solution.`n2. Make sure ModFramework is built first.`n3. Build your new mod. The post-build event will automatically copy it to your local game's Mods folder."
    $msg = "Successfully generated '$ModName' at:`n$TargetDir`nSuccessfully mapped '$ModName' to Game Installation at $GameDir !`n`n$nextSteps"
    if ($config) { $fullMsg = $config + "`n" + $msg } else { $fullMsg = $msg }
    [System.Windows.Forms.MessageBox]::Show($fullMsg, "Success", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information)
}
Write-Host ""
