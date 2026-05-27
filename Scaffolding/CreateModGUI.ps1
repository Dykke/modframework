Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = "Stop"

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

function Test-ValidModName {
    param([string]$Name)
    return ($Name -match '^[A-Za-z_][A-Za-z0-9_]*$')
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

function WriteLogLine {
    param(
        [Parameter(Mandatory=$true)]
        [System.Windows.Forms.TextBox]$OutputBox,

        [AllowEmptyString()]
        [Parameter(Mandatory=$true)]
        [string]$Message
    )

    $OutputBox.AppendText($Message + "`r`n")
    $OutputBox.SelectionStart = $OutputBox.TextLength
    $OutputBox.ScrollToCaret()
    [System.Windows.Forms.Application]::DoEvents()
}

function ConvertTo-ProcessArgumentString {
    param([string[]]$Arguments)

    if ($null -eq $Arguments -or $Arguments.Count -eq 0) {
        return ""
    }

    # Use a classic argument string for broad compatibility with Windows PowerShell/.NET Framework.
    return (($Arguments | ForEach-Object {
        '"' + ($_ -replace '"', '\\"') + '"'
    }) -join ' ')
}

function Show-HelpDialog {
    $helpText = @"
Create Mod (GUI) - Help

GUI Commands:
    1. Mod Name
         Enter a valid C# identifier (letters, numbers, underscores; cannot start with a number).

    2. Game Directory
         Choose your Software Inc install directory.
         Validation checks for: Software Inc_Data\Managed\Assembly-CSharp.dll

    3. Mod Directory (optional)
         Optional base folder where the new mod folder is created.
         If empty, output defaults to the parent of this repository.

    4. Build ModFramework immediately (-Build)
         Checked: runs ModFramework build after scaffolding.
         Unchecked: scaffolds only.

    5. Run CreateMod
         Runs scaffolding and streams CreateMod.ps1 output into the CLI Output pane.

Tips:
    - Use Browse buttons to avoid path typos.
    - Leave Mod Directory blank to use the default output location.
    - Use F1 to open this help quickly.
"@

    [System.Windows.Forms.MessageBox]::Show(
        $helpText,
        "Create Mod Help",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Information
    ) | Out-Null
}

$form = New-Object System.Windows.Forms.Form
$form.Text = "Create Mod (GUI)"
$form.StartPosition = "CenterScreen"
$form.Size = New-Object System.Drawing.Size(980, 680)
$form.MinimumSize = New-Object System.Drawing.Size(980, 680)
$form.Font = New-Object System.Drawing.Font("Segoe UI", 9)

$menuStrip = New-Object System.Windows.Forms.MenuStrip
$menuHelp = New-Object System.Windows.Forms.ToolStripMenuItem("Help")
$menuUsage = New-Object System.Windows.Forms.ToolStripMenuItem("Usage")
$menuAbout = New-Object System.Windows.Forms.ToolStripMenuItem("About")
$menuUsage.ShortcutKeys = [System.Windows.Forms.Keys]::F1
$menuUsage.Add_Click({ Show-HelpDialog })
$menuAbout.Add_Click({
    [System.Windows.Forms.MessageBox]::Show(
        "Create Mod (GUI)\nFront-end for CreateMod.ps1",
        "About",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Information
    ) | Out-Null
})
$menuHelp.DropDownItems.Add($menuUsage) | Out-Null
$menuHelp.DropDownItems.Add($menuAbout) | Out-Null
$menuStrip.Items.Add($menuHelp) | Out-Null
$form.MainMenuStrip = $menuStrip
$form.Controls.Add($menuStrip)

$labelModName = New-Object System.Windows.Forms.Label
$labelModName.Location = New-Object System.Drawing.Point(18, 44)
$labelModName.Size = New-Object System.Drawing.Size(130, 22)
$labelModName.Text = "Mod Name"
$form.Controls.Add($labelModName)

$textModName = New-Object System.Windows.Forms.TextBox
$textModName.Location = New-Object System.Drawing.Point(160, 42)
$textModName.Size = New-Object System.Drawing.Size(600, 24)
$form.Controls.Add($textModName)

$labelGameDir = New-Object System.Windows.Forms.Label
$labelGameDir.Location = New-Object System.Drawing.Point(18, 82)
$labelGameDir.Size = New-Object System.Drawing.Size(130, 22)
$labelGameDir.Text = "Game Directory"
$form.Controls.Add($labelGameDir)

$textGameDir = New-Object System.Windows.Forms.TextBox
$textGameDir.Location = New-Object System.Drawing.Point(160, 80)
$textGameDir.Size = New-Object System.Drawing.Size(600, 24)
$form.Controls.Add($textGameDir)

$btnBrowseGame = New-Object System.Windows.Forms.Button
$btnBrowseGame.Location = New-Object System.Drawing.Point(780, 78)
$btnBrowseGame.Size = New-Object System.Drawing.Size(160, 28)
$btnBrowseGame.Text = "Browse Game Dir"
$form.Controls.Add($btnBrowseGame)

$labelModDir = New-Object System.Windows.Forms.Label
$labelModDir.Location = New-Object System.Drawing.Point(18, 120)
$labelModDir.Size = New-Object System.Drawing.Size(130, 22)
$labelModDir.Text = "Mod Directory"
$form.Controls.Add($labelModDir)

$textModDir = New-Object System.Windows.Forms.TextBox
$textModDir.Location = New-Object System.Drawing.Point(160, 118)
$textModDir.Size = New-Object System.Drawing.Size(600, 24)
$form.Controls.Add($textModDir)

$btnBrowseMod = New-Object System.Windows.Forms.Button
$btnBrowseMod.Location = New-Object System.Drawing.Point(780, 116)
$btnBrowseMod.Size = New-Object System.Drawing.Size(160, 28)
$btnBrowseMod.Text = "Browse Mod Dir"
$form.Controls.Add($btnBrowseMod)

$checkBuild = New-Object System.Windows.Forms.CheckBox
$checkBuild.Location = New-Object System.Drawing.Point(160, 154)
$checkBuild.Size = New-Object System.Drawing.Size(360, 24)
$checkBuild.Text = "Build ModFramework immediately (-Build)"
$form.Controls.Add($checkBuild)

$btnRun = New-Object System.Windows.Forms.Button
$btnRun.Location = New-Object System.Drawing.Point(160, 188)
$btnRun.Size = New-Object System.Drawing.Size(180, 34)
$btnRun.Text = "Run CreateMod"
$form.Controls.Add($btnRun)

$btnClose = New-Object System.Windows.Forms.Button
$btnClose.Location = New-Object System.Drawing.Point(352, 188)
$btnClose.Size = New-Object System.Drawing.Size(140, 34)
$btnClose.Text = "Close"
$form.Controls.Add($btnClose)

$labelOutput = New-Object System.Windows.Forms.Label
$labelOutput.Location = New-Object System.Drawing.Point(18, 240)
$labelOutput.Size = New-Object System.Drawing.Size(130, 22)
$labelOutput.Text = "CLI Output"
$form.Controls.Add($labelOutput)

$textOutput = New-Object System.Windows.Forms.TextBox
$textOutput.Location = New-Object System.Drawing.Point(18, 266)
$textOutput.Size = New-Object System.Drawing.Size(922, 380)
$textOutput.Multiline = $true
$textOutput.ReadOnly = $true
$textOutput.ScrollBars = "Vertical"
$textOutput.WordWrap = $false
$form.Controls.Add($textOutput)

$folderDialog = New-Object System.Windows.Forms.FolderBrowserDialog

$btnBrowseGame.Add_Click({
    if ($folderDialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
        $textGameDir.Text = $folderDialog.SelectedPath
    }
})

$btnBrowseMod.Add_Click({
    if ($folderDialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
        $textModDir.Text = $folderDialog.SelectedPath
    }
})

$btnClose.Add_Click({
    $form.Close()
})

$btnRun.Add_Click({
    $btnRun.Enabled = $false
    $textOutput.Clear()

    try {
        $modName = $textModName.Text.Trim()
        $gameDir = NormalizePathInput -PathValue $textGameDir.Text
        $modDirRaw = $textModDir.Text
        $modDir = NormalizePathInput -PathValue $modDirRaw

        if (-not (Test-ValidModName $modName)) {
            WriteLogLine -OutputBox $textOutput -Message "ERROR: Valid ModName Required."
            if (-not [string]::IsNullOrWhiteSpace($modName)) {
                WriteLogLine -OutputBox $textOutput -Message "ERROR: ModName must be a valid C# identifier (letters, numbers, underscores, cannot start with a number)."
                WriteLogLine -OutputBox $textOutput -Message "Got: '$modName'"
            }
            return
        }

        $expectedGamePath = ""
        $resolvedGameDir = Test-ValidDir -Path $gameDir -RequiredRelativePath "Software Inc_Data\Managed\Assembly-CSharp.dll" -ExpectedPath ([ref]$expectedGamePath)
        if (-not $resolvedGameDir) {
            if (-not [string]::IsNullOrWhiteSpace($gameDir)) {
                WriteLogLine -OutputBox $textOutput -Message ("ERROR: '{0}' does not look like a Software Inc installation." -f $gameDir)
                WriteLogLine -OutputBox $textOutput -Message ("Expected to find: {0}" -f $expectedGamePath)
            }
            WriteLogLine -OutputBox $textOutput -Message "ERROR: Valid GameDir Required."
            return
        }

        $useModDir = $false
        $resolvedModDir = ""

        if (-not [string]::IsNullOrWhiteSpace($modDirRaw)) {
            $useModDir = $true
            if ([string]::IsNullOrWhiteSpace($modDir)) {
                WriteLogLine -OutputBox $textOutput -Message "ERROR: ModDir was supplied but is blank."
                return
            }

            $expectedModPath = ""
            $resolvedModDir = Test-ValidDir -Path $modDir -RequiredRelativePath "" -ExpectedPath ([ref]$expectedModPath)
            if (-not $resolvedModDir) {
                WriteLogLine -OutputBox $textOutput -Message ("ERROR: ModDir does not exist: {0}" -f $modDir)
                return
            }
        }

        $scriptRoot = $PSScriptRoot
        $repoRoot = Split-Path $scriptRoot
        $targetDir = if ($useModDir) {
            Join-Path $resolvedModDir $modName
        } else {
            Join-Path (Split-Path $repoRoot) $modName
        }

        if (Test-Path $targetDir) {
            WriteLogLine -OutputBox $textOutput -Message "ERROR: Directory $targetDir already exists. Please choose a different mod name."
            return
        }

        $createModScript = Join-Path $scriptRoot "CreateMod.ps1"
        if (-not (Test-Path $createModScript)) {
            WriteLogLine -OutputBox $textOutput -Message "ERROR: Could not find CreateMod.ps1 next to this GUI script."
            return
        }

        $argList = New-Object System.Collections.Generic.List[string]
        $argList.Add("-NoProfile")
        $argList.Add("-ExecutionPolicy")
        $argList.Add("Bypass")
        $argList.Add("-File")
        $argList.Add($createModScript)
        $argList.Add("-ModName")
        $argList.Add($modName)
        $argList.Add("-GameDir")
        $argList.Add($resolvedGameDir)

        if ($useModDir) {
            $argList.Add("-ModDir")
            $argList.Add($resolvedModDir)
        }

        $buildRequested = $checkBuild.Checked
        if ($buildRequested) {
            $argList.Add("-Build")
        }

        # Always pass -NonInteractive so CreateMod.ps1 never blocks on a Read-Host prompt.
        # All required values are pre-validated and supplied by the GUI.
        $argList.Add("-NonInteractive")

        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = "powershell.exe"
        $psi.UseShellExecute = $false
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $psi.RedirectStandardInput = $true
        $psi.CreateNoWindow = $true
        $psi.WorkingDirectory = $scriptRoot

        $psi.Arguments = ConvertTo-ProcessArgumentString -Arguments $argList.ToArray()

        # Thread-safe queue for output lines produced by async stream handlers
        # (OutputDataReceived/ErrorDataReceived fire on threadpool threads, drained on UI thread).
        $outputQueue = [System.Collections.Concurrent.ConcurrentQueue[string]]::new()

        $outputHandler = {
            param($sender, $eventArgs)
            if ($null -ne $eventArgs.Data) {
                $Event.MessageData.Enqueue($eventArgs.Data)
            }
        }
        $errorHandler = {
            param($sender, $eventArgs)
            if ($null -ne $eventArgs.Data) {
                $Event.MessageData.Enqueue($eventArgs.Data)
            }
        }

        $proc = New-Object System.Diagnostics.Process
        $proc.StartInfo = $psi

        $stdoutSub = Register-ObjectEvent -InputObject $proc -EventName OutputDataReceived -Action $outputHandler -MessageData $outputQueue
        $stderrSub = Register-ObjectEvent -InputObject $proc -EventName ErrorDataReceived  -Action $errorHandler  -MessageData $outputQueue

        try {
            [void]$proc.Start()
            $proc.BeginOutputReadLine()
            $proc.BeginErrorReadLine()
            $proc.StandardInput.Close()

            # Drain queue while child runs; never block on a stream read.
            $line = $null
            while (-not $proc.HasExited) {
                while ($outputQueue.TryDequeue([ref]$line)) {
                    WriteLogLine -OutputBox $textOutput -Message $line
                }
                [System.Windows.Forms.Application]::DoEvents()
                [void]$proc.WaitForExit(25)
            }

            # Ensure all buffered async data has been flushed, then drain remaining queue.
            $proc.WaitForExit()
            while ($outputQueue.TryDequeue([ref]$line)) {
                WriteLogLine -OutputBox $textOutput -Message $line
            }
        } finally {
            Unregister-Event -SubscriptionId $stdoutSub.Id -ErrorAction SilentlyContinue
            Unregister-Event -SubscriptionId $stderrSub.Id -ErrorAction SilentlyContinue
            if ($stdoutSub) { Remove-Job -Job $stdoutSub -Force -ErrorAction SilentlyContinue }
            if ($stderrSub) { Remove-Job -Job $stderrSub -Force -ErrorAction SilentlyContinue }
        }

        if ($proc.ExitCode -ne 0) {
            WriteLogLine -OutputBox $textOutput -Message ("CreateMod.ps1 exited with code {0}" -f $proc.ExitCode)
            return
        }

        WriteLogLine -OutputBox $textOutput -Message ""
        WriteLogLine -OutputBox $textOutput -Message "GUI run completed successfully."
    } catch {
        WriteLogLine -OutputBox $textOutput -Message ("ERROR: {0}" -f $_.Exception.Message)
    } finally {
        $btnRun.Enabled = $true
    }
})

$configFile = Join-Path $PSScriptRoot ".game-directory"
if (Test-Path $configFile) {
    $textGameDir.Text = NormalizePathInput -PathValue ((Get-Content $configFile -Raw).Trim())
}

[void]$form.ShowDialog()
