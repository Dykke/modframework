# 🚀 Software Inc Modding - Project Setup Guide

## 📁 Project Structure

```
SoftwareIncMods.sln
├── ModFramework/              ← Shared library for all mods
│   ├── ModFramework.cs       ← UIHelper, ModLogger, Notifications, etc.
│   ├── ModFramework.csproj
│   └── README.md
│
├── CompatibilityChecker/      ← Your first mod
│   ├── CompatibilityChecker.cs
│   ├── CompatibilityCheckerBehaviour.cs
│   ├── ModDiagnosticsUI.cs
│   └── CompatibilityChecker.csproj
│
└── YourNewMod/               ← Future mods go here
    ├── YourNewModMeta.cs
    ├── YourNewModBehaviour.cs
    └── YourNewMod.csproj
```

## ✅ What Was Set Up

### 1. **ModFramework Project**
- Centralized framework with reusable components
- All mods copy `ModFramework.cs` during build
- Located at: `ModFramework/ModFramework.cs`

### 2. **Updated CompatibilityChecker.csproj**
- Now references ModFramework
- Post-build event copies all `.cs` files to game folder
- Automatically deploys on each build

### 3. **Build Process**
When you build in Visual Studio:
1. All mod `.cs` files are copied to `<YOUR_GAME_DIR>\DLLMods\YourModName\`
2. `ModFramework.cs` is copied to each mod folder
3. Software Inc compiles them at runtime

## How to Create a New Mod

### Recommended: Use the Scaffolding Script

The scaffolding script generates a complete mod project with all references configured:

```powershell
# First run - provide your game install path (cached for future runs)
.\ModFramework\Scaffolding\CreateMod.ps1 -ModName "MyAwesomeMod" -GameDir "C:\SteamLibrary\steamapps\common\Software Inc"

# Subsequent runs - path is remembered
.\ModFramework\Scaffolding\CreateMod.ps1 -ModName "AnotherMod"
```

What it generates:
- `MyAwesomeModBehaviour.cs` - Main mod class with lifecycle hooks
- `ModMeta.json` - Mod metadata
- `MyAwesomeMod.csproj` - Project file with all HintPaths pointing to YOUR game directory
- `meta.tyd` - Required by Software Inc for mod discovery

The script validates your game path (checks for `Assembly-CSharp.dll`) and caches it in `.game-directory` so you only enter it once.

### Manual Setup

If you prefer manual setup:

### Step 1: Create New Project
1. Right-click solution > Add > New Project
2. Choose "Class Library (.NET Framework 4.8)"
3. Name it (e.g., "MyAwesomeMod")

### Step 2: Configure Project
Copy these settings to your new `.csproj`, replacing `<YOUR_GAME_DIR>` with your actual game installation path (e.g., `E:\SteamLibrary\steamapps\common\Software Inc`):

```xml
<!-- Add references to Unity/Software Inc DLLs -->
<ItemGroup>
  <Reference Include="Assembly-CSharp">
    <HintPath><YOUR_GAME_DIR>\Software Inc_Data\Managed\Assembly-CSharp.dll</HintPath>
    <Private>False</Private>
  </Reference>
  <Reference Include="UnityEngine.CoreModule">
    <HintPath><YOUR_GAME_DIR>\Software Inc_Data\Managed\UnityEngine.CoreModule.dll</HintPath>
    <Private>False</Private>
  </Reference>
  <Reference Include="UnityEngine.UI">
    <HintPath><YOUR_GAME_DIR>\Software Inc_Data\Managed\UnityEngine.UI.dll</HintPath>
    <Private>False</Private>
  </Reference>
</ItemGroup>

<!-- Link ModFramework -->
<ItemGroup>
  <Content Include="..\ModFramework\ModFramework.cs">
    <Link>ModFramework.cs</Link>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>

<!-- Post-build: Copy to game folder -->
<PropertyGroup>
  <PostBuildEvent>
if not exist "<YOUR_GAME_DIR>\DLLMods\MyAwesomeMod\" mkdir "<YOUR_GAME_DIR>\DLLMods\MyAwesomeMod\"
xcopy /Y /R "$(ProjectDir)*.cs" "<YOUR_GAME_DIR>\DLLMods\MyAwesomeMod\"
xcopy /Y /R "$(ProjectDir)..\ModFramework\ModFramework.cs" "<YOUR_GAME_DIR>\DLLMods\MyAwesomeMod\"
  </PostBuildEvent>
</PropertyGroup>
```

### Step 3: Create Mod Files

**MyAwesomeModMeta.cs:**
```csharp
using UnityEngine;
using UnityEngine.UI;

namespace MyAwesomeMod
{
    public class MyAwesomeModMeta : ModMeta
    {
        public override string Name
        {
            get { return "My Awesome Mod"; }
        }

        public override void ConstructOptionsScreen(RectTransform parent, bool inGame)
        {
            Text label = WindowManager.SpawnLabel();
            label.text = "My Awesome Mod v1.0";
            label.color = Color.black;
            WindowManager.AddElementToElement(label.gameObject, parent.gameObject, 
                new Rect(0, 0, 400, 120), new Rect(0.01f, 0.01f, 0, 0));
        }
    }
}
```

**MyAwesomeModBehaviour.cs:**
```csharp
using UnityEngine;
using ModFramework;

namespace MyAwesomeMod
{
    public class MyAwesomeModBehaviour : ModBehaviour
    {
        private void Awake()
        {
            ModLogger.SetPrefix("MyAwesomeMod");
            ModSettings.SetPrefix("MyAwesomeMod");
        }

        public override void OnActivate()
        {
            ModLogger.LogSuccess("MY AWESOME MOD ACTIVATED!");
            Notifications.ShowSuccess("MyAwesomeMod is running!");
        }

        public override void OnDeactivate()
        {
            ModLogger.Log("Mod deactivated");
        }
    }
}
```

### Step 4: Create meta.tyd
In `<YOUR_GAME_DIR>\DLLMods\MyAwesomeMod\meta.tyd`:

```tyd
Name        MyAwesomeMod
Description "Does awesome things!"
Author      YourName
SteamName   MyAwesomeMod
```

### Step 5: Build and Test
1. Build in Visual Studio (Ctrl+Shift+B)
2. Launch Software Inc
3. Enable your mod in the mod menu
4. Test it!

## 📚 Using ModFramework

Add to any mod file:
```csharp
using ModFramework;
```

Available components:
- **UIHelper** - Create windows, buttons, labels, etc.
- **ModLogger** - Color-coded logging system
- **Notifications** - In-game popup messages
- **ModSettings** - Save/load player preferences
- **ModUtils** - String formatting, number formatting, etc.

See the main `README.md` for complete API documentation.

## 🔧 Troubleshooting

1. **Mod not showing?** Check `Player.log` for compilation errors
2. **Changes not applying?** Reload mods in-game (Code mods menu)
3. **Errors?** Make sure all `.cs` files are in the DLLMods folder
4. **Framework issues?** Verify `ModFramework.cs` is copied to your mod folder

## 🎯 Next Steps

Your setup is complete! You can now:
1. Test the CompatibilityChecker mod (already working)
2. Create your next mod using the guide above
3. Leverage ModFramework to save development time

Happy modding! 🚀


---

---

## Core Utilities

### ModLogger

Buffered logging with severity levels. All output goes to the game's console (F12).

```csharp
using ModFramework.Core;

ModLogger.Log("General message");
ModLogger.LogSuccess("Operation completed!");     // Prefixed with checkmark
ModLogger.LogWarning("Something unexpected");      // Prefixed with warning icon
ModLogger.LogError("Critical failure: " + ex.Message);
```

### ModSettings

Persistent key-value settings stored on disk (Base64 encoded, under `Application.persistentDataPath/ModSettings/`).

**Two APIs are available:**

#### Legacy Static API (simple, but has a gotcha)

The static API uses a global prefix set with `SetPrefix()`. This works perfectly when only your mod is loaded, but if multiple DLL mods all call `SetPrefix()`, the last one wins and the others silently read/write to the wrong file.

```csharp
// Set up your mod's prefix (call in Awake)
ModSettings.SetPrefix("MyMod");

// Save settings
ModSettings.SetFloat("multiplier", 1.5f);
ModSettings.SetBool("feature_on", true);
ModSettings.SetString("hotkey", "F3");
ModSettings.SetInt("count", 42);

// Load settings (with default fallback)
float mult = ModSettings.GetFloat("multiplier", 1.0f);
bool enabled = ModSettings.GetBool("feature_on", false);
string key = ModSettings.GetString("hotkey", "F5");
int count = ModSettings.GetInt("count", 10);
```

#### Scoped API (recommended for multi-mod safety)

The scoped API creates an instance that carries its own prefix, so it always reads/writes to the correct file regardless of what other mods do. This is especially important for Harmony patches and background code.

```csharp
// Create a scope once (store as a static field)
private static readonly ModSettingsScope Settings = ModSettings.ForMod("MyMod");

// Use it exactly like the static API
Settings.SetFloat("multiplier", 1.5f);
float mult = Settings.GetFloat("multiplier", 1.0f);

Settings.SetBool("feature_on", true);
bool enabled = Settings.GetBool("feature_on", false);

Settings.SetInt("count", 42);
int count = Settings.GetInt("count", 10);

Settings.SetString("hotkey", "F3");
string key = Settings.GetString("hotkey", "F5");
```

**When to use which:**
- For `ConstructOptionsScreen` callbacks where the game calls `SetPrefix` for you: the legacy static API is fine
- For Harmony patches, background tasks, or any code shared between multiple mods: use the scoped API

### UIHelper Settings Helpers

High-level methods for building mod settings screens in `ConstructOptionsScreen`. Each creates a label + widget + status feedback, auto-persists via ModSettings, and returns the updated yOffset for vertical stacking.

```csharp
// In your ModMeta's ConstructOptionsScreen:
public override void ConstructOptionsScreen(RectTransform parent, bool isInitial)
{
    float y = 0f;

    // Slider setting (best for small ranges like 0-100%)
    y = UIHelper.AddSettingSlider(parent, y,
        "Speed Multiplier",     // display name
        "speed_mult",           // settings key
        1.0f,                   // default value
        0.5f, 3.0f,             // min / max
        false                   // wholeNumbers
    );

    // Text input setting (best for wide ranges like 1-999)
    y = UIHelper.AddSettingInput(parent, y,
        "Max Employees",        // display name
        "max_employees",        // settings key
        100f,                   // default value
        1f, 999f,               // min / max
        true,                   // wholeNumbers
        suffix: ""              // optional suffix for display
    );
}
```

**Scoped overloads** (recommended when multiple mods are loaded):

```csharp
private static readonly ModSettingsScope Settings = ModSettings.ForMod("MyMod");

public override void ConstructOptionsScreen(RectTransform parent, bool isInitial)
{
    float y = 0f;

    // Same API, just pass the scope as an extra parameter
    y = UIHelper.AddSettingSlider(parent, y,
        "Speed Multiplier", "speed_mult", 1.0f,
        0.5f, 3.0f, false,
        Settings   // scoped settings instance
    );

    y = UIHelper.AddSettingInput(parent, y,
        "Max Employees", "max_employees", 100f,
        1f, 999f, true,
        Settings   // scoped settings instance
    );
}
```

### ModEvents

Publish/subscribe event bus for decoupled communication between mod components.

```csharp
using ModFramework.Core;

// Subscribe
ModEvents.OnGameLoaded += () => { /* initialize */ };

// Custom events
ModEvents.Subscribe("mymod.refresh", () => RebuildUI());
ModEvents.Publish("mymod.refresh");
```

### Notifications

In-game toast notifications (appears in the game's notification area).

```csharp
using ModFramework.Core;

Notifications.ShowSuccess("Settings saved!");
Notifications.ShowWarning("Low funds detected");
Notifications.ShowError("Failed to load config");
```

### ModUtils

General-purpose utility functions.

```csharp
using ModFramework.Core;

string path = ModUtils.GetSafeFilePath("config.json");
```

---

## Native XML Integration (The V5 Architecture)

ModFramework v5 replaced the massive custom C# UI library with a lightweight **Native XML Integration** system. The game already has a highly performant, C++ backed XML parser built directly into `WindowManager.cs`. ModFramework hooks into this native parser to allow you to build UI entirely in XML.

### 1. Register Custom Tags
To use ModFramework's advanced UI elements (charts, accordions, node graphs, etc.) inside your XML files, you must register them in your mod's `OnActivate` or `Awake` method.

```csharp
public override void OnActivate()
{
    ModFramework.UI.AccordionElement.Register();
    ModFramework.UI.CardLayoutElement.Register();
    ModFramework.UI.SplitPaneElement.Register();
    ModFramework.UI.ContextMenuElement.Register();
    ModFramework.UI.CustomCharts.Register();
    ModFramework.UI.NodeGraphElement.Register();
}
```

### 2. Write your UI.xml
Create a `UI.xml` file in your mod project.

```xml
<Window MinSize="600,800" NonLocTitle="My Settings Window" anchor="middle,center">
  <VerticalLayout padding="10,10,10,10" spacing="8">
      <Label height="24">Settings</Label>
      <Checkbox id="devToggle">Developer Mode</Checkbox>
      <Button id="saveBtn" color="4CAF50" onClick="OnSaveBtnClick()">Save Settings</Button>
  </VerticalLayout>
</Window>
```

### 3. Parse and Bind in C#
Use the game's native `WindowManager.GenerateUI` method to instantly generate the UI dictionary. 

```csharp
// Load the raw XML
var nodes = ParentMod.LoadFullXMLFile("UI.xml");

// Generate UI. 'this' acts as the target for onClick methods.
Dictionary<string, GameObject> _uiElements = WindowManager.GenerateUI(nodes, null, this);

// Access specific elements using their "id" attribute
if (_uiElements.TryGetValue("devToggle", out var toggleObj))
{
    var toggle = toggleObj.GetComponent<UnityEngine.UI.Toggle>();
    toggle.isOn = true;
}
```

```csharp
// The method defined in onClick="OnSaveBtnClick()"
public void OnSaveBtnClick() 
{
    ModLogger.LogSuccess("Settings Saved!");
}
```

---

## Native UI Tag Reference

The game's native XML parser provides a robust set of standard tags. You can use these immediately without registering anything.

### Layouts & Containers

#### `<Window>`
The root element for creating standard game windows.
*   `MinSize="width,height"`: Minimum drag size for the window.
*   `NonLocTitle="String"`: The title text displayed in the header.
*   `anchor="middle,center"`: The starting position.

#### `<VerticalLayout>` / `<HorizontalLayout>`
Stacks children sequentially.
*   `width`, `height`: Explicit sizing.
*   `spacing="10"`: Gap between children.
*   `padding="10,10,10,10"`: Inner padding (Left, Right, Top, Bottom).
*   `childForceExpandWidth="False"`: Prevents children from stretching.
*   `childControlWidth="False"`: Allows children to set their own width.

#### `<GridLayout>`
Places children in a rigid grid.
*   `cellSize="150,40"`: Width and height of each cell.
*   `spacing="10,10"`: Gap between cells (X, Y).

#### `<ScrollView>`
Creates a scrollable masking area. Add a layout group inside it.
*   `anchor="fill"`: Typically set to fill its parent area.
*   `padding="8,8,8,8"`

### Basic Controls

> **Important XML Rule:** The native parser crashes if you use self-closing tags (`<Input />`). Always explicitly close them (`<Input></Input>`).

#### `<Label>`
Text display component.
*   `fontSize="24"`: Font size override.
*   `style="bold"`: Bold formatting.
*   `alignment="MiddleCenter"`: Text alignment.
*   `color="FFFFFF"`: Hex color string.
*   **Do not use** `<Header>`, use `<Label style="bold">` instead.

#### `<Button>`
Clickable button.
*   `color="4CAF50"`: Tint color.
*   `onClick="MethodName()"`: Binds to a method in the class passed to `GenerateUI`.

#### `<Input>`
Text entry field.
*   To retrieve text in C#: `gameObject.GetComponent<InputField>().text`

#### `<Checkbox>`
Boolean toggle.
*   To read/write state in C#: `gameObject.GetComponent<Toggle>().isOn`

#### `<Slider>` / `<Progressbar>`
Ranged value controls.
*   `minValue="0"`
*   `maxValue="100"`
*   `value="65"` (For progress bar, usually `0.0` to `1.0`)

#### `<Combo>`
Dropdown selection box.
*   `OnSelectedChanged="MethodName(this)"`: Triggers when an option is chosen.

#### `<Panel>` / `<Image>` / `<RawImage>`
Visual backgrounds and graphics.
*   `color="333333"`: Tint color.

#### `<Empty>`
A transparent spacer element used for creating exact pixel gaps between layouts.
*   `width`, `height`

---

## Custom ModFramework Tag Reference

After calling their `Register()` methods, these advanced tags become available in your XML.

### `<accordion>`
Creates a collapsible "drawer" that contains child elements.
*   `width`: The width of the accordion header.
```xml
<accordion width="550">Advanced Settings
  <Checkbox height="24">Enable Feature X</Checkbox>
  <Button height="30">Reset</Button>
</accordion>
```

### `<contextmenu>`
Creates a right-click popup menu. Contains `<Button>` elements for the menu options.
*   `id`: Required to access the context menu in C# and attach it to trigger elements.
```xml
<contextmenu id="myContextMenu">
  <Button height="24" onClick="OnCopyClicked()">Copy</Button>
  <Button height="24" onClick="OnPasteClicked()">Paste</Button>
</contextmenu>
```

### `<SplitPane>`
Creates two resizable panels separated by a draggable vertical divider. Must contain exactly two `<Panel>` children.
```xml
<SplitPane width="550" height="120">
  <Panel width="200" color="444444">
    <Label>Left Side</Label>
  </Panel>
  <Panel width="340" color="555555">
    <Label>Right Side</Label>
  </Panel>
</SplitPane>
```

### `<CardLayout>`
Creates an elevated visual card with a drop shadow, perfect for displaying items or profiles.
```xml
<CardLayout width="170">Product A
  <Label height="24">Rev: $1.2M</Label>
</CardLayout>
```

### `<nodegraph>`
Creates an interactive, draggable node-based canvas (useful for tech trees or relationship graphs).
```xml
<nodegraph id="myNodeGraph" width="550" height="300"></nodegraph>
```

### Data Visualization Charts

Wrappers for the game's internal data visualization tools.

#### `<piechart>`
Radial pie chart.
```xml
<piechart id="myPieChart" width="240" height="155"></piechart>
```

#### `<barchart>`
Bar chart.
```xml
<barchart id="myBarChart" width="240" height="155"></barchart>
```

#### `<linechart>`
Smooth line graph.
```xml
<linechart id="myLineChart" width="530" height="155"></linechart>
```

---

## Updating UI Elements in C#

To dynamically update XML elements at runtime, use the dictionary returned by `WindowManager.GenerateUI`.

```csharp
// Give your XML element an ID
// <Label id="statusLabel" width="200" height="24">Ready</Label>

if (_uiElements.TryGetValue("statusLabel", out var labelObj))
{
    // Fetch the native Unity component
    var textComponent = labelObj.GetComponent<UnityEngine.UI.Text>();
    
    // Update it dynamically
    textComponent.text = "Loading...";
    textComponent.color = Color.yellow;
}
```

```csharp
// <Slider id="volumeSlider" minValue="0" maxValue="100" />
if (_uiElements.TryGetValue("volumeSlider", out var sliderObj))
{
    var slider = sliderObj.GetComponent<UnityEngine.UI.Slider>();
    slider.value = 85f;
    
    // Listen for changes
    slider.onValueChanged.AddListener((float val) => {
        ModLogger.Log("Volume changed to: " + val);
    });
}
```

---

## Safely Accessing Game Data

Game objects (companies, employees, products) can be garbage collected mid-game. Always null-check:

```csharp
foreach (uint id in trackedIds.ToList()) {
    if (market.Companies.TryGetValue(id, out var company) && company != null && !company.Bankrupt) {
        // Safe to use
    } else {
        trackedIds.Remove(id);  // Cleanup
    }
}
```

---

## Architecture (V5)

```
ModFramework/
|-- Core/                          (2 files - Utilities)
|   |-- ModSafety.cs               Error safety wrappers and Assertions
|   |-- ModUtils.cs                General utilities
|
|-- GameData/                      (4 files - Safe Data Wrappers)
|   |-- ModCompanyHelper.cs        Company data (player, rivals, revenue)
|   |-- ModProductHelper.cs        Product data (type, quality, bugs)
|   |-- ModEmployeeHelper.cs       Employee and team data
|   |-- ModMarketHelper.cs         Market state, dates, game speed
|
|-- UI/
|   |-- CustomUIParser.cs          Injects custom tags into WindowManager XML parser
|   |-- CustomAccordion.cs         Drawer panels (<accordion>)
|   |-- CustomCardLayout.cs        Elevated item cards (<CardLayout>)
|   |-- CustomCharts.cs            Native visual charts (<piechart>, <barchart>, <linechart>)
|   |-- CustomContextMenu.cs       Right-click menus (<contextmenu>)
|   |-- CustomNodeGraph.cs         Interactive drag canvases (<nodegraph>)
|   |-- CustomSplitPane.cs         Draggable dividers (<SplitPane>)
```

---

## Production Examples

These mods in this workspace use ModFramework in production:

| Mod | UI Components Used | Notes |
|-----|-------------------|-------|
| **RivalRadar** | ModWindow, ModListView, ModSearchField, ModPanel, ModScrollView | Full Custom UI showcase. Competitor tracking window with tabs, search, and live data. |
| **AIRevolution** | ModWindow, ModButton, ModLabel, ModToggle, ModSlider | AI automation management center with multiple modules. |
| **FoundersPlus** | Legacy UIHelper (CS mod) | CS script mod, cannot use Custom UI. |
| **ImmortalFounder** | Raw Unity Canvas + ModFramework Core | Custom ScrollRect + RectMask2D for 1200+ employee list. |
| **MegaPlots** | Core only (ModLogger, ModSettings) | No custom UI needed, settings via ModMeta screen. |

**Best reference implementation:** `ModFrameworkTest/UI.xml` - Shows how to build a complete window with charts, graphs, context menus, and accordions.

## Deferred Features / Known Limitations

### Emoji Support (TextMeshPro)
The ModFramework intentionally uses the standard `UnityEngine.UI.Text` component because it perfectly mirrors how the game renders its native UI (via `WindowManager.SpawnLabel`). 

Because of this, **full-color Emojis are not supported**. While the game's code does contain `Unity.TextMeshPro.dll`, switching the framework to `TextMeshProUGUI` would mean we can no longer use the game's built-in `GameFont`. We would have to bundle our own `TMP_FontAsset`, which breaks visual consistency with the base game and creates severe risks of breaking foreign language translations (like Chinese or Russian) if the bundled font doesn't contain those glyphs. 

To ensure maximum stability and localization support, standard `Text` is retained.


## ModFramework Core Features

v4 introduced tools that make DLL modding accessible to developers who have never touched the game's internals, and these tools remain the backbone of **v5**. You do not need to open dnSpy, read decompiled code, or understand the game's internal class hierarchy. Everything is wrapped in safe, easy-to-use helper methods.

### What is in ModFramework Core?

| Feature | What it solves |
|---------|---------------|
| **Scaffolding** | Creates a ready-to-build mod project in one command |
| **Game Data Wrappers** | Read company, product, employee, and market data safely |
| **Lifecycle Hooks** | Know exactly when the game is ready, when a day passes, etc. |
| **Error Safety** | Prevents your mod from crashing the entire game |
| **Harmony Helpers** | Apply code patches with a single line |

---

### Scaffolding - Create a New Mod in 30 Seconds

**What is this?** A PowerShell script that generates an entire mod project for you, with all the references, build settings, and deployment automation already configured.

**How to use it:**

1. Open PowerShell (press Win+R, type `powershell`, press Enter)
2. Navigate to your repository folder:
   ```powershell
   cd "C:\Users\YourName\Documents\Visual Studio 2022\SoftwareIncMods\SoftwareIncMods"
   ```
3. Run the scaffolding script:
   ```powershell
   .\ModFramework\Scaffolding\CreateMod.ps1 -ModName "MyAwesomeMod"
   ```
4. Open Visual Studio, right-click your Solution, choose "Add Existing Project", and select `MyAwesomeMod\MyAwesomeMod.csproj`
5. Build the project (Ctrl+Shift+B). The DLL is automatically copied to your game's Mods folder.

**What gets generated:**

| File | Purpose |
|------|---------|
| `MyAwesomeModBehaviour.cs` | Your mod's main entry point, with step-by-step comments |
| `MyAwesomeMod.csproj` | Pre-configured project file with all DLL references |
| `ModMeta.json` | Metadata file describing your mod |
| `meta.tyd` | Required by Software Inc for mod discovery in the mod menu |

The generated `.csproj` includes a **post-build event** that automatically copies your compiled DLL and `ModMeta.json` to the game's local mods folder. You just hit Build, then launch the game.

---

### Game Data Wrappers - Read Game Data Without Crashing

**What is this?** Four helper classes that let you safely read game simulation data (companies, products, employees, market info) without worrying about null references, missing data, or crashes.

**Why do you need this?** Without these wrappers, reading the player's company looks like this:

```csharp
// WITHOUT wrappers (risky, can crash if any part is null)
Company player = GameSettings.Instance.MyCompany;
float cash = (float)player.Money;
```

With wrappers, the same code is completely safe:

```csharp
// WITH wrappers (safe, returns 0 if anything is wrong)
float cash = ModCompanyHelper.GetPlayerCash();
```

#### ModCompanyHelper

Provides safe access to company data.

```csharp
using ModFramework.GameData;

// Get the player's company object (returns null if not in a game)
Company myCompany = ModCompanyHelper.GetPlayerCompany();

// Get the player's cash balance (returns 0 if not in a game)
float cash = ModCompanyHelper.GetPlayerCash();

// Get all active AI companies (excludes bankrupt ones)
List<SimulatedCompany> rivals = ModCompanyHelper.GetActiveCompanies();

// Find a company by name (case-insensitive)
SimulatedCompany target = ModCompanyHelper.FindByName("Macrosoft");

// Check if a company is bankrupt
bool broke = ModCompanyHelper.IsBankrupt(someCompany);

// Get companies sorted by revenue (richest first)
List<SimulatedCompany> leaderboard = ModCompanyHelper.GetByRevenue();

// Check if the player is currently in a game (not on main menu)
bool inGame = ModCompanyHelper.IsInGame();
```

#### ModProductHelper

Provides safe access to software product data.

```csharp
using ModFramework.GameData;

// Get all products the player has released
List<SoftwareProduct> myProducts = ModProductHelper.GetPlayerProducts();

// Get ALL products on the market (from every company)
List<SoftwareProduct> allProducts = ModProductHelper.GetAllProducts();

// Get all products of a specific type (e.g. "Operating System")
List<SoftwareProduct> osList = ModProductHelper.GetByType("Operating System");

// Get details about a specific product
string typeName = ModProductHelper.GetTypeName(product);      // "Operating System"
string category = ModProductHelper.GetCategoryName(product);  // "Business"
float quality = ModProductHelper.GetQuality(product);         // 0.0 to 1.0
int bugs = ModProductHelper.GetBugCount(product);             // number of bugs
string name = ModProductHelper.GetName(product);              // product name
Company dev = ModProductHelper.GetDeveloper(product);         // who made it
```

#### ModEmployeeHelper

Provides safe access to employee and team data.

```csharp
using ModFramework.GameData;

// Get all employees working for the player
List<Actor> employees = ModEmployeeHelper.GetPlayerEmployees();

// Get employee count (quick shortcut)
int headcount = ModEmployeeHelper.GetPlayerEmployeeCount();

// Get all teams in the player's company
List<Team> teams = ModEmployeeHelper.GetPlayerTeams();

// Get an employee's name
string name = ModEmployeeHelper.GetName(someActor);

// Find which team an employee belongs to
string teamName = ModEmployeeHelper.GetTeamName(someActor);
```

#### ModMarketHelper

Provides safe access to market and game-state information.

```csharp
using ModFramework.GameData;

// Get the current in-game date
SDateTime today = ModMarketHelper.GetCurrentDate();

// Get the current game speed (1 = normal, 2 = fast, etc.)
int speed = ModMarketHelper.GetGameSpeed();

// Check if the game is paused
bool paused = ModMarketHelper.IsPaused();

// Get all software types in the game (Operating System, Antivirus, etc.)
List<SoftwareType> types = ModMarketHelper.GetSoftwareTypes();

// Get all software categories across all types (Business, Home, etc.)
List<SoftwareCategory> categories = ModMarketHelper.GetCategories();

// Format a money value into a readable string
string display = ModMarketHelper.FormatMoney(2500000);  // "$2.5M"
```

---

### Lifecycle Hooks - Know When Things Happen

**What is this?** A set of events you can subscribe to that fire at specific moments in the game. Instead of writing complex `Update()` loops that check game state every frame, you simply tell ModFramework "call my method when the game is ready" or "call my method every day."

**Why do you need this?** Without lifecycle hooks, you have to manually check if the game has loaded every single frame. With hooks, the framework does this for you.

```csharp
using ModFramework.Core;

// In your mod's Awake() method:
ModLifecycle.OnGameReady += () => {
    // This code runs ONCE when the player loads a save or starts a new game.
    // It is safe to read any game data here.
    ModLogger.Log("The game is loaded! Player company: " + ModCompanyHelper.GetPlayerCompany()?.Name);
};

ModLifecycle.OnGameExit += () => {
    // This code runs when the player goes back to the main menu.
    // Clean up your windows, caches, or state here.
    ModLogger.Log("Player left the game.");
};

ModLifecycle.OnDayPassed += () => {
    // This code runs once every in-game day at midnight.
    // Great for periodic checks without burning CPU in Update().
    var date = ModMarketHelper.GetCurrentDate();
    if (date.Day == 1) {
        ModLogger.Log("New month started!");
    }
};

ModLifecycle.OnMonthPassed += () => {
    // This code runs once at the start of each in-game month.
    int employees = ModEmployeeHelper.GetPlayerEmployeeCount();
    ModLogger.Log("Monthly report: " + employees + " employees on payroll.");
};

ModLifecycle.OnYearPassed += () => {
    // This code runs once at the start of each in-game year.
    ModLogger.Log("Happy New Year!");
};
```

---

### Error Safety - Never Crash the Game

**What is this?** Utility methods that wrap your code in try/catch blocks so that if your mod encounters an error, the game keeps running normally. The error is logged to the console instead of freezing everything.

#### ModSafety.Try()

Wrap any risky operation. Returns `true` if it ran successfully, `false` if it crashed.

```csharp
using ModFramework.Core;

// Basic usage: wrap risky code
bool success = ModSafety.Try(() => {
    var companies = ModCompanyHelper.GetActiveCompanies();
    BuildLeaderboardUI(companies);
}, "Building Leaderboard");

if (!success) {
    ModLogger.LogWarning("Leaderboard failed to load, using cached data.");
}
```

#### ModSafety.TryGet()

Same as Try, but for functions that return a value. If it crashes, returns a fallback value.

```csharp
// Get player cash safely (returns 0 if anything goes wrong)
float cash = ModSafety.TryGet(() => ModCompanyHelper.GetPlayerCash(), 0f, "Getting Cash");
```

#### ModSafety.ThrottledErrorHandler()

Wraps an action so it only logs errors **once** instead of spamming every frame. Essential for code that runs in Update() loops.

```csharp
private Action _safeUpdate;

void Awake() {
    _safeUpdate = ModSafety.ThrottledErrorHandler("MyMod Update", () => {
        // This code runs every frame, but if it crashes,
        // it only logs the error once and then stops trying.
        UpdateDashboard();
    });
}

void Update() {
    _safeUpdate();
}
```

#### ModSafety.Assert()

Quick sanity check during development. Logs a warning if a condition is false. Does NOT throw an exception.

```csharp
var company = ModCompanyHelper.GetPlayerCompany();
ModSafety.Assert(company != null, "Expected the player's company to exist");
// If company is null, you'll see: "[ModFramework] ASSERT FAILED: Expected the player's company to exist"
```

---

### Harmony Helpers - Patch Game Methods Safely

**What is this?** Harmony is a library that lets you modify ("patch") the game's methods without changing the game's actual files. ModFramework wraps Harmony so you can apply patches with minimal boilerplate.

**Before you start:** Make sure `0Harmony.dll` is referenced in your `.csproj`. If you used the scaffolding script, it is already included.

#### Basic Usage (Attribute-Based Patching)

The recommended approach is to use Harmony's `[HarmonyPatch]` attributes on your patch classes, then call `ModPatching.PatchAll()` to apply them all at once:

```csharp
using ModFramework.Core;
using HarmonyLib;

// In your mod's Awake():
if (ModPatching.IsHarmonyAvailable()) {
    ModPatching.PatchAll("com.yourname.mymod");
} else {
    ModLogger.LogWarning("Harmony is not available. Patches will not be applied.");
}

// Define your patches as separate classes:
[HarmonyPatch(typeof(SomeGameClass), "SomeMethod")]
public class MyPatch
{
    static void Postfix()
    {
        // This runs AFTER SomeGameClass.SomeMethod() finishes
        ModLogger.Log("SomeMethod was called!");
    }
}
```

#### Cleanup

Remove all your patches when the mod is deactivated:

```csharp
ModLifecycle.OnGameExit += () => {
    ModPatching.UnpatchAll("com.yourname.mymod");
};
```

---

### Putting It All Together - Complete Example

Here is a complete minimal mod that uses all v4/v5 features:

```csharp
using System;
using UnityEngine;
using ModFramework.Core;
using ModFramework.GameData;

namespace MyFirstMod
{
    public class MyFirstModBehaviour : ModBehaviour
    {
        private void Awake()
        {
            ModLogger.SetPrefix("MyFirstMod");
            ModSettings.SetPrefix("MyFirstMod");

            // Subscribe to lifecycle events
            ModLifecycle.OnGameReady += OnGameReady;
            ModLifecycle.OnMonthPassed += OnMonthPassed;

            ModLogger.Log("Mod loaded!");
        }

        private void OnGameReady()
        {
            // Safely read company data
            ModSafety.Try(() => {
                var company = ModCompanyHelper.GetPlayerCompany();
                float cash = ModCompanyHelper.GetPlayerCash();
                int employees = ModEmployeeHelper.GetPlayerEmployeeCount();

                ModLogger.Log("Company: " + company?.Name);
                ModLogger.Log("Cash: " + ModMarketHelper.FormatMoney(cash));
                ModLogger.Log("Employees: " + employees);
            }, "Initial Company Report");
        }

        private void OnMonthPassed()
        {
            // Monthly competitor scan
            var rivals = ModCompanyHelper.GetActiveCompanies();
            ModLogger.Log("Active competitors: " + rivals.Count);
        }
    }
}
```

---

## Third-Party Licenses

### Harmony
This project bundles [Harmony](https://github.com/pardeike/Harmony) v2.4.1 by Andreas Pardeike for runtime method patching.
License: MIT | Copyright (c) 2017 Andreas Pardeike | Full text: [Harmony/LICENSE](Harmony/LICENSE)

---

## Changelog

- **v5.0** (May 2026) - Massive UI Overhaul: Deprecated C# programmatic UI builder (31 files removed). Replaced with lightweight Native XML Integration API hooking into `WindowManager`. Replaced `DOCUMENTATION.md` UI guide with comprehensive XML manual.
- **v4.1** (April 2026) - Bundled Harmony DLL (no NuGet required), generalized game paths with `{GAME_DIRECTORY}` placeholder, added `-GameDir` to scaffolding script with path validation and caching, scoped ModSettings API, UIHelper settings helpers
- **v4.0** (March 2026) - Accessible DLL modding: Game Data Wrappers, Lifecycle Hooks, Error Safety, Harmony Helpers, Project Scaffolding
- **v3.0** (March 2026) - Complete Custom UI system (31 files), replaced legacy UIHelper as primary UI approach, added Resize, Hotkeys, and Node Graphs
- **v2.0** (October 2025) - Core split into 5 files, added UIHelper
- **v1.0** (September 2025) - Initial single-file ModFramework.cs
