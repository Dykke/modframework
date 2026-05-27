# ModFramework for Software Inc

![Software Inc](https://img.shields.io/badge/Game-Software_Inc-blue?style=for-the-badge&logo=steam) ![Unity 2019.4](https://img.shields.io/badge/Unity-2019.4-black?style=for-the-badge&logo=unity) ![License](https://img.shields.io/badge/License-MIT-success?style=for-the-badge)

A complete UI, utility, and gameplay framework for building Software Inc mods.

### What is this and why use it?
Modding Software Inc UI traditionally required building tedious C# hierarchies or fighting with Unity's UI systems.

**ModFramework v5** solves this by providing:
- **Native XML Parsing Integration** - Instead of building UI in C#, write simple `UI.xml` files and let the game's engine render them instantly.
- **Custom XML Tags** - Extends the game's parser to support complex widgets like `<accordion>`, `<splitpane>`, `<cardlayout>`, `<piechart>`, `<barchart>`, `<linechart>`, and `<nodegraph>`.
- **Game Data Wrappers** - Safely read company, product, employee, and market data without null references.
- **Error Safety** - Tools that prevent your mod from crashing the game.
- **Harmony Helpers** - Bundle and inject Harmony easily.
- **Project Scaffolding** - Create a new mod project in 30 seconds.

**Namespace:** `ModFramework.UI` (UI) / `ModFramework.Core` (utilities) / `ModFramework.GameData` (data wrappers)
**Requires:** Unity 2019.4 (game's engine), .NET Framework 4.8
**Dependencies:** Harmony 2.4.1 (bundled, MIT license)
**License:** MIT - Free to use in any Software Inc mod

---

## Quick Start - Create a New Mod (v5)

You can scaffold a new mod using either the CLI script or the GUI script.

### Option 1: CLI

Show CLI help (parameters, usage, examples):

```powershell
.\ModFramework\Scaffolding\CreateMod.ps1 -Help
```

Create a mod from CLI:

```powershell
# First run - provide your game install path (cached for future runs)
.\Scaffolding\CreateMod.ps1 -ModName "MyAwesomeMod" -GameDir "C:\SteamLibrary\steamapps\common\Software Inc"

# After first run - path is remembered
.\Scaffolding\CreateMod.ps1 -ModName "AnotherMod"
```

This generates a complete, ready-to-build mod project with all references configured to your local game installation and a post-build event that automatically copies your DLL to the game's mod folder.

## Quick Start - UI Example (v5 XML Method)

**1. Create your UI.xml file:**
```xml
<Window MinSize="400,300" NonLocTitle="My Mod Settings" anchor="middle,center">
  <VerticalLayout padding="10,10,10,10" spacing="10">
    <Label height="30" style="bold">My Mod Settings</Label>
    <Button height="30" id="applyBtn">Apply Changes</Button>
    <accordion width="380">Advanced Settings
       <Checkbox height="24" id="devToggle">Developer Mode</Checkbox>
    </accordion>
  </VerticalLayout>
</Window>
```

**2. Load and Bind in C#:**
```csharp
using ModFramework.UI;

// In your ModBehaviour's OnActivate():
AccordionElement.Register(); // Register custom tags needed

// Load the XML and let the game generate the UI
var nodes = ParentMod.LoadFullXMLFile("UI.xml");
Dictionary<string, GameObject> ui = WindowManager.GenerateUI(nodes, null, this);

// Bind logic using IDs
if (ui.TryGetValue("applyBtn", out var btnObj)) {
    btnObj.GetComponent<Button>().onClick.AddListener(() => {
        Debug.Log("Button Clicked!");
    });
}
```

## Quick Start - Game Data Example

Read game data safely without any null-reference worries:

```csharp
using ModFramework.GameData;

// These will never crash your mod, even if the game isn't fully loaded yet
float cash = ModCompanyHelper.GetPlayerCash();
int employees = ModEmployeeHelper.GetPlayerEmployeeCount();
string money = ModMarketHelper.FormatMoney(cash);  // "$2.5M"
```

---

## Installation

**Use the scaffolding script** (see Quick Start above). It generates a complete `.csproj` with all references pointing to your local game installation. No manual path editing needed.

**Harmony is bundled** in `Harmony/0Harmony.dll` so you do not need to install it via NuGet or download it separately.

If you prefer manual setup, add framework files to your `.csproj`:

```xml
<!-- Core utilities -->
<Compile Include="..\ModFramework\Core\ModSafety.cs" Link="ModFramework\Core\ModSafety.cs" />
<Compile Include="..\ModFramework\Core\ModUtils.cs" Link="ModFramework\Core\ModUtils.cs" />

<!-- UI Extensions -->
<Compile Include="..\ModFramework\UI\CustomUIParser.cs" Link="ModFramework\UI\CustomUIParser.cs" />
<Compile Include="..\ModFramework\UI\CustomAccordion.cs" Link="ModFramework\UI\CustomAccordion.cs" />
<!-- Add other components as needed -->
```

Everything compiles directly into your mod's DLL. Do NOT deploy `.cs` files alongside the DLL.

## Documentation
For the complete setup guide, full XML API reference, and Game Data Wrappers, please see the [Documentation](DOCUMENTATION.md).

---

## Third-Party Licenses

### Harmony

This project bundles [Harmony](https://github.com/pardeike/Harmony) v2.4.1 by Andreas Pardeike for runtime method patching.

**License:** MIT
**Copyright:** (c) 2017 Andreas Pardeike
**Full license:** [Harmony/LICENSE](Harmony/LICENSE)

---

*ModFramework is authored by Zicarius.*
