# Vendor Folder

Third-party dependencies that are ILMerged into `ModFramework.dll`.

## Contents

| File | Source | Version | License |
|---|---|---|---|
| `Newtonsoft.Json.dll` | https://www.nuget.org/packages/Newtonsoft.Json/ | 13.0.3 | MIT (see LICENSE.txt) |
| `Newtonsoft.Json.xml` | (XML doc, copied from NuGet) | 13.0.3 | MIT |

## Purpose

When the build runs the ILMerge post-build step, all types from these
assemblies are merged into `ModFramework.dll`. Mods that reference
`ModFramework.dll` automatically get access to all of these without needing to
ship any extra DLLs.

## Updating a dependency

1. Download the new version from the source above
2. Replace the DLL + XML file in this folder
3. Re-run the build (ILMerge step will pick up the new version)
4. Bump the version in the table above
5. Re-test the merged DLL in-game (especially Harmony patches — they sometimes
   break after a Harmony version bump)
