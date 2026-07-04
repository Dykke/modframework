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

## Setup (one-time)

1. Download the latest `ilmerge` from <https://www.nuget.org/packages/ilmerge/>
   (file: `ilmerge.console.<version>.nupkg`).
2. Rename it to `ilmerge.zip` and extract it.
3. Copy the inner `tools/net452/ILMerge.exe` (and `ILMerge.exe.config`) to this
   folder. The result should look like:

   ```
   Tools/ilmerge/
     ILMerge.exe
     ILMerge.exe.config
     README.md  ← (this file)
   ```

4. (Optional) Drop a `ilmerge.exclude` file in this folder if you want to
   internalize non-public types. Without it, ILMerge keeps everything public.

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
