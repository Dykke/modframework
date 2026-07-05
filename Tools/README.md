# Tools folder

This folder holds external build tools that the framework uses during the
Release build. Tools that have a redistribution-friendly license (ILMerge)
are committed alongside the source. Tools without one will be documented here
as "user installs" when added.

## Contents

| Subfolder | Tool | Required? | What it does |
|---|---|---|---|
| `ilmerge/` | [ILMerge.exe](https://www.nuget.org/packages/ilmerge/) v3.0.41 | Yes (bundled) | Merges 0Harmony.dll + Newtonsoft.Json.dll INTO ModFramework.dll so mods ship a single DLL. See `ilmerge/README.md`. License: `ilmerge/ILMerge.LICENSE.txt` (MIT). |
