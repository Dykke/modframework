# Tools folder

This folder holds external build tools that the framework can optionally use.
The tools themselves are NOT committed to git — this README documents how to
fetch them.

## Contents

| Subfolder | Tool | Required? | What it does |
|---|---|---|---|
| `ilmerge/` | [ILMerge.exe](https://www.nuget.org/packages/ilmerge/) | Optional but recommended | Merges 0Harmony.dll + Newtonsoft.Json.dll INTO ModFramework.dll so mods ship a single DLL. See `ilmerge/README.md`. |
