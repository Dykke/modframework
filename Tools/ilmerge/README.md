# ILMerge — what is this folder?

This folder holds `ILMerge.exe` so the post-build step in `ModFramework.csproj` can
run during a Visual Studio / MSBuild build.

## What is ILMerge?

ILMerge is a Microsoft tool that takes multiple .NET assemblies and merges them
into a single output assembly. We use it to bake `0Harmony.dll` and
`Newtonsoft.Json.dll` into `ModFramework.dll` so that:

1. Mods only need to reference **one** DLL (`ModFramework.dll`).
2. Publishing a mod to Steam Workshop becomes a single-DLL operation
   (no "drop the DLL in the managed folder" steps for the player).

## Setup

**No setup required.** `ILMerge.exe` ships in this folder (ILMerge 3.0.41, MIT-licensed, see `ILMerge.LICENSE.txt`).

The build expects the file at exactly this path:
```
Tools/ilmerge/ILMerge.exe
```

If you ever delete it, restore it from the upstream NuGet package:
```powershell
$tmp = Join-Path $env:TEMP 'ilmerge_dl.nupkg'
Invoke-WebRequest 'https://www.nuget.org/api/v2/package/ilmerge/3.0.41' -OutFile $tmp -UseBasicParsing
$zip = [System.IO.Path]::ChangeExtension($tmp, '.zip')
Copy-Item $tmp $zip -Force
Expand-Archive $zip $env:TEMP\ilmerge_x -Force
Copy-Item "$env:TEMP\ilmerge_x\tools\net452\ILMerge.exe" "$PSScriptRoot\ILMerge.exe" -Force
```
Then drop `ILMerge.LICENSE.txt` next to it (see upstream: https://github.com/dotnet/ILMerge/blob/master/LICENSE).

## Verifying it works

Build the project in **Release** configuration. In the build output you should see:

```
=== ILMerge step: merging 0Harmony and Newtonsoft.Json INTO ModFramework.dll ===
=== ILMerge complete. ModFramework.dll is the single shippable artifact. ===
```

The output `bin/Release/ModFramework.dll` should grow from ~150 KB to ~1.5–2.5 MB
and `0Harmony.dll` / `Newtonsoft.Json.dll` should be **removed** from `bin/Release/`.

## Disabling ILMerge

If you don't want to merge (e.g., while developing the framework itself), set
the MSBuild property `ILMergeEnabled=false`, or comment out the `ILMergeAssemblies`
target in `ModFramework.csproj`. The framework will then ship as multiple DLLs,
which still works for testing but breaks the single-DLL Steam-Workshop path.

## Troubleshooting

- **"ILMerge is enabled but ILMerge.exe was not found"** — you didn't extract
  ILMerge.exe into this folder. See step 3 above.
- **`Unresolved assembly reference` errors** — the target framework is wrong.
  Edit the `/targetplatform:v4` flag in `ModFramework.csproj` if needed.
- **`Type 'X' exists in both ModFramework and 0Harmony`** — you have duplicate
  types. Check that you didn't accidentally include both `Harmony\0Harmony.dll`
  and a copy of Harmony in `Vendor/`.
