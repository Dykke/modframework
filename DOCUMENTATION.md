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

- **v6.1.2** (2026-07-17) - In-game Settings window no longer clips content. `OptionsWindow.AddModOption` sizes each mod's scroll region once from the parent's direct children; fixed with zero-size tab containers, `CanvasGroup.alpha` tab toggling (both tabs stay active so both are measured), and per-label self-sizing to `preferredHeight`. **Verified in-game 2026-07-17.**
- **v6.1.1** (2026-07-17) - Settings persistence + Newtonsoft removed. The ILMerged Newtonsoft.Json 13.0.3 static ctor throws under the game's Unity 2018.4 Mono runtime, so every `WriteJson` failed silently. `ModFileAccess` now uses Unity's `JsonUtility`; Newtonsoft is dropped from both ILMerge paths (DLL ~700 KB smaller, ~2.56 MB). **Settings classes must be `[Serializable]` with public fields** — no `Dictionary`, properties, or top-level arrays.
- **v6.1.0** (2026-07-17) - Closes the v5.x `[Obsolete]` back-compat permission bypass: the 6 v4.x single-file classes are now `internal` and the 28 `[Obsolete]` v5.x wrappers in `Core/` are deleted (they had skipped `RequirePermission`). Adds the in-game **Mod Audit Log** + **Mod permissions** windows via the `ModFrameworkSettings` shim mod. `AssemblyVersion` 6.0.1.0 → 6.1.0.0; strong-name token unchanged. Workshop CS mods must pin to v6.0.0. See the **v6.1 — hardening, in-game UI, and the four barriers** section below. **Breaking**: v5.x wrappers removed.
- **v6.0.1** (July 2026) - Convenience overloads that accept the v6.0+ handle (`ModBehaviour` / `ModIdentity`) instead of the lower-level `ModController.DLLMod`. `ModFrameworkActivator.OnActivate(ModBehaviour)` looks up the matching DLLMod via `ModController.Instance.Mods` and dispatches to the existing `OnActivate(ModController.DLLMod, ...)` overload. `ModDependencies.VerifyOrWarn(ModIdentity, ...)` looks up the registered DLLMod via `ModRegistry.GetDLLMod(identity)` and dispatches to the existing `VerifyOrWarn(ModController.DLLMod, ...)`. Both additions unblock the `ModFrameworkExample` canonical reference mod from compiling. **Breaking**: none — purely additive.
- **v6.0** (July 2026) - Security-focused overhaul. Per-mod identity (`ModIdentity`), path safety (`SafePath`), unforgeable event/service handles (`EventKey` / `ServiceToken`), 22 fine-grained permission flags + 4 named presets, framework strong-name signing, per-day audit log, hard-name-protected `OnActivate` / `OnDeactivate` lifecycle. The v5.x API is preserved as `[Obsolete]` back-compat — existing v5.x mods keep working with a compile warning. See the **v6.0 — Security-focused overhaul** section below for the new API. v6.0 is a breaking-change major: new code should target the v6.0 API.
- **v5.2** (July 2026) - Optional addendum DLL bridge (`ModServiceBridge` / `ModServiceHost` / `ModDependency.ServiceObjectName`) for Nexus / local DLL mods. See the v5.2 section below.
- **v5.1** (June 2026) - `ModDependencies`, `ModFileAccess`, `ModLoader`, `ModHarmony` + ILMerge single-DLL distribution. See the v5.1 section below.
- **v5.0** (May 2026) - Massive UI Overhaul: Deprecated C# programmatic UI builder (31 files removed). Replaced with lightweight Native XML Integration API hooking into `WindowManager`. Replaced `DOCUMENTATION.md` UI guide with comprehensive XML manual.
- **v4.1** (April 2026) - Bundled Harmony DLL (no NuGet required), generalized game paths with `{GAME_DIRECTORY}` placeholder, added `-GameDir` to scaffolding script with path validation and caching, scoped ModSettings API, UIHelper settings helpers
- **v4.0** (March 2026) - Accessible DLL modding: Game Data Wrappers, Lifecycle Hooks, Error Safety, Harmony Helpers, Project Scaffolding
- **v3.0** (March 2026) - Complete Custom UI system (31 files), replaced legacy UIHelper as primary UI approach, added Resize, Hotkeys, and Node Graphs
- **v2.0** (October 2025) - Core split into 5 files, added UIHelper
- **v1.0** (September 2025) - Initial single-file ModFramework.cs

---
---

# v5.1 New Modules - TechFrontier Support

v5.1 adds four runtime helper classes to `ModFramework.Core` and merges Harmony + Newtonsoft.Json into a single `ModFramework.dll` so your mod ships as **one file**.

All new code lives in `ModFramework/Core/`. All new code is **additive** - existing v5.0 mods work unchanged.

## ModDependencies

Declare which files, folders, or other mods your code needs. Missing required deps produce an in-game dialog and return `false` from `VerifyOrWarn` so you can short-circuit your `Initialize()`.

```csharp
using ModFramework.Core;

public override void Initialize(ModController.DLLMod parentMod)
{
    bool ok = ModDependencies.VerifyOrWarn(parentMod,
        new ModDependency {
            Name = "0Harmony.dll",
            Kind = ModDependencyKind.File,
            Severity = ModDependencySeverity.Required,
            DownloadUrl = "https://github.com/pardeike/Harmony"
        },
        new ModDependency {
            Name = "SomeOtherMod",
            Kind = ModDependencyKind.Mod,
            Severity = ModDependencySeverity.Optional  // log only, no dialog
        }
    );

    if (!ok) {
        Debug.LogWarning("[MyMod] Skipping - missing dependencies.");
        return;
    }
    // ...normal init...
}
```

`ModDependencyKind` options: `File`, `Folder`, `Mod`, `ManagedAssembly`.
`ModDependencySeverity` options: `Required`, `Optional`.

For `File` and `Folder` kinds, the framework searches:
1. `<modFolder>/Dependencies/<Name>` (Steam-Workshop convention)
2. `<modFolder>/<Name>`
3. `<GameRoot>/Software Inc_Data/Managed/<Name>` (game's Managed folder)

## ModFileAccess

Safe file I/O. All methods are **non-throwing** - they log a warning and return `false` / `null` / `default` on failure. JSON uses Newtonsoft.Json (ILMerged), so `Dictionary<,>`, `HashSet<>`, and properties all work.

```csharp
using ModFramework.Core;
using System.Collections.Generic;

public class MyModState {
    public int Version = 1;
    public Dictionary<string, int> Counters = new Dictionary<string, int>();
    public string LastSave = "";
}

public override void OnActivate()
{
    string path = ModFileAccess.GetModDataPath(ParentMod, "state.json");
    MyModState state = ModFileAccess.TryReadJson<MyModState>(path, out bool ok);
    if (!ok) state = new MyModState();
    _state = state;
}

public override void OnDeactivate()
{
    string path = ModFileAccess.GetModDataPath(ParentMod, "state.json");
    ModFileAccess.WriteJson(path, _state);
}
```

Path helpers:
- `ModFileAccess.GetModFolder(parentMod)` - mod root
- `ModFileAccess.GetModDataPath(parentMod, "sub", "file.json")` - `<modRoot>/Data/sub/file.json`, auto-creates parent
- `ModFileAccess.GetManagedFolder()` - `<game>/Software Inc_Data/Managed`
- `ModFileAccess.GetGameRoot()` - game's install root

Other ops: `Exists`, `DirectoryExists`, `EnsureDirectory`, `ReadText`, `WriteText`, `AppendText`, `ReadBytes`, `WriteBytes`, `Delete`, `DeleteIfExists`, `ToJsonString<T>` (no I/O, just serialization).

## ModLoader

Ask questions about other mods at runtime, no reflection required.

```csharp
if (ModLoader.IsModLoaded("SomeOtherMod")) {
    var other = ModLoader.FindMod("SomeOtherMod");
    string folder = ModLoader.GetModFolder("SomeOtherMod", ParentMod);
    Debug.Log($"SomeOtherMod is at {folder}");
}

foreach (var name in ModLoader.GetAllLoadedModNames()) {
    Debug.Log($"Loaded mod: {name}");
}
```

## ModHarmony

Centralized Harmony wrapper with clean unpatch support.

```csharp
private HarmonyLib.Harmony _harmony;

public override void Initialize(ModController.DLLMod parentMod)
{
    _harmony = ModHarmony.CreateAndPatchAll("com.yourname.yourmod");
}

public override void OnDeactivate()
{
    ModHarmony.UnpatchAll(_harmony);
}
```

The patcher-id is auto-normalized: `"yourmod"` becomes `"com.yourmod"`.

## ILMerge - Single-DLL Distribution

In Release builds, the post-build step in `ModFramework.csproj` merges `0Harmony.dll` and `Newtonsoft.Json.dll` INTO `ModFramework.dll`. The standalone DLLs are removed from `bin\Release\`.

**Setup (one-time):** drop `ILMerge.exe` into `Tools/ilmerge/`. See `Tools/ilmerge/README.md` for instructions.

After ILMerge is set up:
- Mods only reference `ModFramework.dll`
- No `Dependencies/` folder in the mod's install dir
- Steam Workshop publishing is single-DLL safe

**Skipping ILMerge** (e.g. while iterating on the framework itself): build with `Debug` configuration, or set `<ILMergeEnabled>false</ILMergeEnabled>` in the .csproj. The build still succeeds, but the mod will need to ship `0Harmony.dll` and `Newtonsoft.Json.dll` separately.

## Updated Scaffolding Templates

`CreateMod.ps1` now generates a `Mod.csproj` that:
- Only references `ModFramework.dll` (not `0Harmony.dll`)
- Does not create a `Dependencies/` folder in the post-build step
- The generated `Meta.cs` calls `ModDependencies.VerifyOrWarn(...)`
- The generated `Behaviour.cs` shows `ModFileAccess`/`ModLoader` examples

## Backward Compatibility

- v5.0 mods work unchanged - v5.1 is purely additive
- v5.0 mods that ship `0Harmony.dll` separately continue to work; the framework's `AppDomain.AssemblyResolve` no longer needs to be patched in mod code (Harmony is now inside `ModFramework.dll`)
- `using ModFramework.Core;` is the only new import required

## File Layout After v5.1

```
ModFramework/
+- ModFramework.cs              # v5.1 header comment
+- ModFramework.csproj          # +Newtonsoft.Json ref, +ILMerge target
+- Build-ModFramework.ps1       # NEW - standalone build + ILMerge script
+- README.md                    # v5.1 section added
+- DOCUMENTATION.md             # this file
|
+- Core/
|  +- ModSafety.cs
|  +- ModUtils.cs
|  +- ModDependencies.cs       # NEW
|  +- ModFileAccess.cs         # NEW
|  +- ModLoader.cs             # NEW
|  \- ModHarmony.cs            # NEW
|
+- GameData/                    # unchanged
+- UI/                          # unchanged
|
+- Harmony/
|  \- 0Harmony.dll
|
+- Vendor/                      # NEW in v5.1
|  +- Newtonsoft.Json.dll      # 13.0.3
|  +- Newtonsoft.Json.xml
|  +- LICENSE.txt              # MIT
|  \- README.md
|
+- Tools/                       # NEW in v5.1
|  +- README.md
|  \- ilmerge/
|     +- README.md            # Setup instructions
|     \- ilmerge.exclude      # (empty by default)
|
\- Scaffolding/
   +- CreateMod.ps1            # unchanged
   +- CreateModGUI.ps1         # unchanged
   \- Templates/
      +- MainMeta.cs_template     # v5.1: uses ModDependencies + ModHarmony
      +- MainBehaviour.cs_template # v5.1: uses ModFileAccess + ModLoader
      +- Mod.csproj_template      # v5.1: no 0Harmony ref
      +- meta.tyd_template
      +- UI.xml_template
      \- MainMeta.cs.guide
```

---

# v5.2 — Optional addendum DLL bridge (Nexus / local DLL mods)

v5.2 adds `ModServiceBridge` / `ModServiceHost` and extends `ModDependency` for the same pattern as LogoScape's optional native file-browser dependency — but for **ModFramework DLL mods** (not Workshop CS).

## When to use

| Scenario | Approach |
|---|---|
| Main mod on Nexus as DLL | ModFramework consumer API |
| Optional feature needs System.IO / Win32 | Separate addendum DLL + `GiveMeFreedom` |
| No compile-time reference between mods | `ServiceObjectName` + `SendMessage` |
| Main mod on Steam Workshop as CS | Use `_SteamTemplates` + `NativeFileBrowser` pattern in `steam-workshop-mods.md` — not ModFramework |

## Declare an optional addendum dependency

```csharp
var nativeBrowser = new ModDependency
{
    Name = "LogoScape - Native File Browser",   // ModMeta.Name (display)
    FolderName = "LogoScapeDependency",         // DLLMods folder name
    ServiceObjectName = "LogoScapeNativeBrowser", // live GameObject.Find check
    Kind = ModDependencyKind.Mod,
    Severity = ModDependencySeverity.Optional,
    DownloadUrl = "https://www.nexusmods.com/softwareinc/mods/"
};

// Required deps: check once in Initialize (unchanged).
// Optional service deps: re-check at use time or on GameReady — never cache at Initialize.
ModServiceBridge.WhenDependencyReady(ParentMod, nativeBrowser, ready =>
{
    if (ready) Debug.Log("Native feature available.");
});
```

`ModDependencies.IsPresent` now:
1. Returns `true` immediately if `ServiceObjectName` exists (live).
2. Matches mod by `Meta.Name`, `FileName`, or **folder leaf name**.
3. Falls back to `FolderName` for installed-but-not-loaded folder checks.

## Provider (addendum DLL)

```csharp
public override void OnActivate()
{
    ModServiceHost.Register("LogoScapeNativeBrowser", go =>
    {
        if (go.GetComponent<NativeBrowserService>() == null)
            go.AddComponent<NativeBrowserService>();
    });
}

public override void OnDeactivate()
{
    ModServiceHost.Unregister("LogoScapeNativeBrowser");
}
```

Template: `Scaffolding/Templates/ServiceDependency.cs_template`

## Consumer (main DLL mod)

```csharp
if (ModServiceBridge.IsAvailable("LogoScapeNativeBrowser"))
{
    ModServiceBridge.Send("LogoScapeNativeBrowser", "PickImage", args,
        SendMessageOptions.DontRequireReceiver);
}
```

## Rules (same lessons as LogoScape 2026-07-13)

1. **Do not cache** optional service availability at `Initialize` — dependency may activate later.
2. **Align names:** `ModMeta.Name`, `FolderName`, and `ServiceObjectName` must match what the consumer declares.
3. **Check live** in UI / feature code via `ModServiceBridge.IsAvailable` or `IsDependencyReady`.
4. Addendum mod ships as its own DLL + `meta.tyd` on Nexus — not bundled inside the main mod DLL.

See also: `cursor-stuff/notes/steam-workshop-mods.md` (Workshop CS variant of the same idea).

---

---

# v6.0 — Security-focused overhaul (Nexus DLL mods only)

v6.0 is a **breaking-change major**. The v5.x API is preserved as `[Obsolete]` back-compat — your existing v5.x mods keep working with a compile warning — but **new code should target the v6.0 API**. The change is large because the v5.x API had no per-mod identity, no path safety, no permission model, and no audit log. v6.0 adds all four.

## What's new in v6.0

1. **Per-mod identity.** `ModFrameworkActivator.OnActivate` mints an unforgeable `ModIdentity` for your mod at startup. Every privileged framework call takes your identity as the first argument.
2. **Path safety.** `ModFileAccess` no longer accepts raw `string` paths. The only way to get a path is from a `SafePath` factory — four of them, one for each security policy.
3. **22 fine-grained permission flags** + **4 named presets** (`ReadOnly`, `Patcher`, `ServiceProvider`, `ServiceConsumer`). Declared in `meta.tyd`. The framework rejects any privileged call whose required flag is not set.
4. **Unforgeable event and service handles.** `EventKey` and `ServiceToken` are `readonly struct`s with `internal` constructors. Only the publisher can `Trigger` its own event. Only the registered service name can be claimed.
5. **Framework strong-name signing.** The DLL is signed with a private key held by the author; the public key is committed. `FrameworkSignatureCheck` refuses to load the framework if the public key token doesn't match. Players can verify the framework DLL is the one published on Nexus.
6. **Per-day audit log.** Every privileged call writes a line to `%persistentDataPath%/ModFramework/audit-YYYY-MM-DD.log` with the `modId`, the operation, the target, and the result. Visible in `output_log.txt` (via `Debug.Log` echo) and on disk. 30-day retention.

## Installation

v6.0 keeps the v5.1 single-DLL distribution model. One file: `ModFramework.dll` (~3.3 MB, signed, with 0Harmony 2.3.3 + Newtonsoft.Json 13.0.3 ILMerged). Drop it into:

```
<game>/Software Inc_Data/Managed/ModFramework.dll
```

The game auto-loads DLLs from `Managed/` on launch. No installer, no per-mod deployment, no NuGet.

> **v6.1.1+:** the size above is the v6.0 build. As of v6.1.1 Newtonsoft.Json is removed (the framework uses Unity's `JsonUtility`) and the signed DLL is **~2.56 MB**. See the **v6.1 — hardening, in-game UI, and the four barriers** section below for the current installation and author rules.

## Lifecycle: OnActivate / OnDeactivate

Every `ModBehaviour` subclass implements `OnActivate()` (no parameters) and `OnDeactivate()` (no parameters). The v6.0 lifecycle is the v5.x one with `ModFrameworkActivator` calls added:

```csharp
using ModFramework.Core;
using HarmonyLib;

public class MyModBehaviour : ModBehaviour
{
    private ModIdentity _id;
    private Harmony _harmony;
    private EventKey _onPlayerJoinedKey;
    private ServiceToken _myServiceToken;

    public override void OnActivate()
    {
        // 1. Register with the framework. Returns an unforgeable ModIdentity.
        _id = ModFrameworkActivator.OnActivate(this);
        if (_id == null) return;  // framework refused (missing permission, etc.)

        // 2. Set up your privileged operations using _id.
        _harmony = ModHarmony.CreateAndPatchAll(_id);

        // 3. Subscribe to whitelisted global events (e.g. OnGameSaved).
        ModEvents.SubscribeGlobal(_id, GlobalEventKind.OnGameSaved, OnGameSaved);

        // 4. Publish your own event (give the key to consumers via a static field).
        _onPlayerJoinedKey = ModEvents.Publish(_id, "OnPlayerJoined");

        // 5. Register a service (give the token to consumers via a static field).
        _myServiceToken = ModServiceHost.Register(_id, "MyService", go => {
            go.AddComponent<MyServiceComponent>();
        });
    }

    public override void OnDeactivate()
    {
        // Tear down in reverse order.
        if (_id != null)
        {
            if (_myServiceToken.OwnerModId == _id.ModId)
                ModServiceHost.Unregister(_id, _myServiceToken);
            if (_harmony != null)
                ModHarmony.UnpatchAll(_id, _harmony);
            ModFrameworkActivator.OnDeactivate(_id);
        }
    }

    private void OnGameSaved(string savePath) {
        Debug.Log("[MyMod] Save detected at " + savePath);
    }
}

public static class MyModState
{
    // Consumers reach your events/services through these static fields.
    public static EventKey PlayerJoinedKey;
    public static ServiceToken MyServiceToken;
}
```

> **Note:** The `OnActivate(this)` call resolves to `ModFrameworkActivator.OnActivate(ModController.DLLMod)`. If you hit a compile error, your mod is using an older `ModBehaviour` inheritance that doesn't bridge to `ModController.DLLMod` — see the `MODFRAMEWORK_MEMORY.md` v6.0.1 backlog for the planned `OnActivate(ModBehaviour)` convenience overload.

## The 22 permission flags

A mod's `meta.tyd` declares which flags it needs. The framework rejects any privileged call whose required flag is not set.

| Group | Flag | Required by |
|---|---|---|
| File | `FileRead` | `ModFileAccess.ReadText`, `ReadBytes`, `ReadJson`, `TryReadJson`, `ToJsonString` |
| File | `FileWrite` | `ModFileAccess.WriteText`, `WriteBytes`, `WriteJson` |
| File | `FileAppend` | `ModFileAccess.AppendText` |
| File | `FileDelete` | `ModFileAccess.Delete`, `DeleteIfExists` |
| File | `FileDirectoryList` | `ModFileAccess.Exists`, `DirectoryExists`, `EnsureDirectory` |
| File | `FileUserApproved` | `SafePath.GetUserApprovedPath` |
| Harmony | `HarmonyRead` | `ModHarmony.CreateInstance`, `PatchCount` |
| Harmony | `HarmonyPatch` | `ModHarmony.CreateAndPatchAll` |
| Harmony | `HarmonyUnpatch` | `ModHarmony.UnpatchAll` |
| Event | `EventSubscribe` | `ModEvents.Subscribe`, `SubscribeGlobal` |
| Event | `EventPublish` | `ModEvents.Publish` |
| Service | `ServiceRegister` | `ModServiceHost.Register`, `Unregister` |
| Service | `ServiceConsume` | `ModServiceBridge.IsAvailable`, `Find`, `Send` |
| Settings | `SettingsRead` | `ModSettings.Get*` |
| Settings | `SettingsWrite` | `ModSettings.Set*` |
| Settings | `SettingsDelete` | `ModSettings.DeleteAll` |
| Misc | `GameReflection` | `ModUtils.GetSingleton` (deprecation planned v6.0.1) |
| Misc | `GameEventWhitelist` | `ModEvents.PublishGlobal` |
| Misc | `AuditLogRead` | (in-game audit log viewer) |
| Misc | `AuditLogExport` | (in-game audit log export) |
| Misc | `UserDialogPrompt` | `SafePath.GetUserApprovedPath` dialog |
| Misc | `NetworkAccess` | (reserved for future HTTP/socket use — no v6.0 method) |

### 4 named presets

If your mod fits one of the four common cases, use a preset name in `meta.tyd`:

| Preset | Flags | Typical mod |
|---|---|---|
| `ReadOnly` | `FileRead, FileDirectoryList, SettingsRead` | Info display, dashboard, statistics |
| `Patcher` | `FileRead, FileWrite, FileDirectoryList, HarmonyRead, HarmonyPatch, HarmonyUnpatch, SettingsRead, SettingsWrite` | Most mods — game patches + per-mod data |
| `ServiceProvider` | `ServiceRegister, SettingsRead, SettingsWrite` | Mod that exposes a service for other mods |
| `ServiceConsumer` | `ServiceConsume, EventSubscribe, SettingsRead` | Mod that uses another mod's service |

You can mix a preset with extra flags: `Permissions: Patcher, ServiceRegister, EventPublish`.

If your `meta.tyd` has no `Permissions:` line, the framework defaults to `Patcher` and emits a `Debug.LogWarning`. v5.x mods that never declared permissions keep working.

## Core types

### ModIdentity

```csharp
public sealed class ModIdentity
{
    public string ModId { get; }         // e.g. "com.zicarius.mymod"
    public string DisplayName { get; }   // e.g. "My Mod"
    public string AssemblyHash { get; }  // SHA-256 of the calling mod's .dll
    public Guid SessionNonce { get; }    // per-session GUID
    public DateTime IssuedAt { get; }    // UTC time of OnActivate
    public Permission Permissions { get; }
}
```

The `ModIdentity` constructor is `internal`. Consuming code cannot `new ModIdentity(...)` — the only way to get one is `ModFrameworkActivator.OnActivate(...)`. The `SessionNonce` rotates every game launch (replay-attack prevention); the `AssemblyHash` detects mod DLL tampering.

### SafePath + SafePathKind

A `SafePath` is a validated file-system path. The only way to get one is from a factory method. Raw `string` paths are not accepted by `ModFileAccess`.

```csharp
public sealed class SafePath
{
    public string ResolvedAbsolute { get; }   // fully-resolved, no ".."
    public SafePathKind Kind { get; }
}

public static SafePath GetModDataPathSafe(ModIdentity id, params string[] subPaths);
public static SafePath GetPersistentDataPathSafe(ModIdentity id, params string[] subPaths);
public static SafePath GetManagedPathSafe(ModIdentity id, string fileName);
public static SafePath GetUserApprovedPath(ModIdentity id, string filePath);
```

| Factory | Resolves to |
|---|---|
| `GetModDataPathSafe(id, "sub", "file.txt")` | `<modFolder>/Data/sub/file.txt` |
| `GetPersistentDataPathSafe(id, "sub", "file.txt")` | `%persistentDataPath%/Mods/<modId>/sub/file.txt` |
| `GetManagedPathSafe(id, "0Harmony.dll")` | `<game>/Software Inc_Data/Managed/0Harmony.dll` (read-only) |
| `GetUserApprovedPath(id, "C:\\...")` | Whatever the user approved via dialog (STUB in v6.0 — auto-grants with a warning) |

The factory `subPaths` cannot contain `: * ? " < > |` or any path separators that would escape the root. The `ResolvedAbsolute` is always fully resolved with `Path.GetFullPath` and cannot contain `..`. Throws `ModPathException` on invalid input.

### EventKey

Unforgeable event identifier. A mod that publishes an event gets back an `EventKey`; only that mod can `Trigger` it later.

```csharp
public readonly struct EventKey : IEquatable<EventKey>
{
    public string OwnerModId { get; }
    // ==, !=, Equals, GetHashCode built in
}
```

Construction is internal. `ModEvents.Trigger(id, key, data)` throws `ModSecurityException` if `key.OwnerModId != id.ModId` — fixes the v5.x "any mod can fire any event" loophole.

### ServiceToken

Unforgeable service handle. A mod that registers a service gets back a `ServiceToken`; other mods must hold that token to find/send the service.

```csharp
public readonly struct ServiceToken : IEquatable<ServiceToken>
{
    public string ServiceName { get; }    // e.g. "MyService"
    public string OwnerModId { get; }     // who registered it
}
```

`ModServiceHost.Register` throws `ModSecurityException` if a service with the same name is already registered by a different mod — fixes the v5.x `GameObject.Find` name-collision hijack.

## API surface by module

### ModFrameworkActivator

The single entry point. Call `OnActivate` in your `OnActivate` and `OnDeactivate` in your `OnDeactivate`.

```csharp
public static ModIdentity OnActivate(
    ModController.DLLMod dllMod,
    Assembly callingAssembly = null,
    Permission explicitPermissions = Permission.None);

public static void OnDeactivate(ModIdentity identity);
```

- `dllMod` — the game's `ModController.DLLMod` instance. In a `ModBehaviour` subclass, this is `this`.
- `callingAssembly` — the mod's main assembly. Defaults to `Assembly.GetCallingAssembly()`.
- `explicitPermissions` — bypass `meta.tyd` reading and use these flags directly. Useful for test mods.

`OnActivate` reads `meta.tyd` to extract the `Permissions:` line, mints a `ModIdentity`, and registers it. Throws `ArgumentNullException` if `dllMod` is null. Throws `FrameworkSignatureException` if the framework DLL's public key token doesn't match `e0967644e3ffec06`. `OnDeactivate` is idempotent (safe to call with a null `identity`).

### ModFileAccess

File I/O with per-op permission check. All v6.0 methods take `ModIdentity` + `SafePath` as the first two arguments.

| Method | Requires | Returns |
|---|---|---|
| `ReadText(id, path)` | `FileRead` | `string` (null on error) |
| `ReadBytes(id, path)` | `FileRead` | `byte[]` (null on error) |
| `ReadJson<T>(id, path)` | `FileRead` | `T` (null on error) |
| `TryReadJson<T>(id, path, out result)` | `FileRead` | `bool` |
| `WriteText(id, path, content, createDirIfMissing=true)` | `FileWrite` | `bool` |
| `AppendText(id, path, content, createDirIfMissing=true)` | `FileAppend` | `bool` |
| `WriteBytes(id, path, data, createDirIfMissing=true)` | `FileWrite` | `bool` |
| `WriteJson<T>(id, path, data, prettyPrint=true)` | `FileWrite` | `bool` |
| `ToJsonString<T>(id, data, prettyPrint=true)` | `FileRead` (cheapest) | `string` |
| `Delete(id, path)` | `FileDelete` | `bool` |
| `DeleteIfExists(id, path)` | `FileDelete` | `bool` |
| `Exists(id, path)` | `FileDirectoryList` | `bool` |
| `DirectoryExists(id, path)` | `FileDirectoryList` | `bool` |
| `EnsureDirectory(id, path)` | `FileDirectoryList` | `void` |

All write methods call `EnsureDirectory` first (if `createDirIfMissing=true`). All errors are caught internally, logged via `Debug.LogWarning`, and return `false` / `null` — the framework never throws on I/O failure, only on permission or path errors.

Example:
```csharp
SafePath path = SafePath.GetModDataPathSafe(_id, "session.json");
if (!ModFileAccess.WriteJson(_id, path, new SessionData { Day = 42 }))
{
    Debug.LogWarning("Failed to write session data");
}
```

**v5.x back-compat:** All the old `string`-based overloads are kept with `[Obsolete]` attributes. They still work (no compile error) but emit a `CS0618` warning. They deliberately skip the permission check — they are emergency back-compat only. See `MIGRATION_v5_to_v6.md` for the v6.0 replacements.

### ModHarmony

Centralized Harmony wrapper. v6.0 methods take `ModIdentity`; the harmony ID is derived from the identity.

| Method | Requires | Returns |
|---|---|---|
| `CreateInstance(id)` | `HarmonyRead` | `Harmony` (null on error) |
| `CreateAndPatchAll(id, assembly=null)` | `HarmonyPatch` | `Harmony` (null on error) |
| `UnpatchAll(id, harmony)` | `HarmonyUnpatch` | `void` |
| `PatchCount(id, harmony)` | `HarmonyRead` | `int` |

Harmony ID derivation: `NormalizeId(id.ModId)` — prefixes with `com.` if not already present. So `id.ModId = "zicarius.mymod"` becomes `"com.zicarius.mymod"`.

`PatchAll` uses Harmony's `PatchClassProcessor` internally, which has a known issue with **static methods on `static class` targets** (e.g. `GameData.GetProjectEffectiveness`). If you hit this, switch to manual `Harmony.Patch()` calls. The `LimitlessTeams` v1.1.4 release has a working pattern.

Example:
```csharp
_harmony = ModHarmony.CreateAndPatchAll(_id);
Debug.Log("Patched " + ModHarmony.PatchCount(_id, _harmony) + " methods");
```

### ModEvents + GlobalEventKind

Pub/sub event bus. v6.0 uses `EventKey` (mod-to-mod) and `GlobalEventKind` (whitelisted game lifecycle events).

| Method | Requires |
|---|---|
| `Publish(id, eventName, data=null)` | `EventPublish` (returns `EventKey`) |
| `Subscribe(id, key, handler)` | `EventSubscribe` |
| `Unsubscribe(id, key, handler)` | (any) |
| `Trigger(id, key, data=null)` | (any — but throws `ModSecurityException` if `key.OwnerModId != id.ModId`) |
| `PublishGlobal(id, kind, data=null)` | `GameEventWhitelist` |
| `SubscribeGlobal(id, kind, handler)` | `EventSubscribe` |
| `UnsubscribeGlobal(id, kind, handler)` | (any) |

**Whitelisted global event kinds:** `OnGameSaved`, `OnGameLoaded`, `OnCompanyFounded`, `OnSoftwareReleased`, `OnDayPassed`, `OnMonthPassed`.

`Publish` fires the event once at publish time (if `data != null`). Subscribers that `Subscribe` after `Publish` will not receive the initial fire — events are point-in-time, not retained. This matches v5.x semantics.

Example (publisher):
```csharp
EventKey key = ModEvents.Publish(_id, "OnPlayerJoined");
// Give the key to consumers via a public static field on your mod class
public static EventKey PlayerJoinedKey;
```

Example (consumer):
```csharp
ModEvents.Subscribe(myId, MyOtherMod.PlayerJoinedKey, args => {
    Debug.Log("Player joined!");
});
```

### ModServiceHost + ModServiceBridge

Cross-mod service registry. v6.0 uses `ServiceToken` to prevent name-collision spoofing.

**Provider (`ModServiceHost`):**

| Method | Requires |
|---|---|
| `Register(id, serviceName, configure=null)` | `ServiceRegister` (returns `ServiceToken`) |
| `Unregister(id, token)` | `ServiceRegister` |

**Consumer (`ModServiceBridge`):**

| Method | Requires |
|---|---|
| `IsAvailable(id, token)` | `ServiceConsume` |
| `Find(id, token)` | `ServiceConsume` |
| `Send(id, token, methodName, arg=null, options=RequireReceiver)` | `ServiceConsume` |

`Register` creates a new `GameObject(serviceName)` if none exists and runs the optional `configure` callback on it. Throws `ModSecurityException` on name collision. The `Send` method uses Unity's `GameObject.SendMessage` — the receiver must have a method with the matching name.

### ModSafety, ModUtils, ModDependencies, ModLoader

These four v5.1 utility classes got `[ModFrameworkPublicAPI("v6.0")]` markers in v6.0 but their **public API is unchanged from v5.1**. See the v5.1 section above for usage. `ModSafety.Try` and `ModDependencies.ShowMissingMessage` now also write to the audit log when they catch a failure.

### AuditLog

Append-only log of every privileged framework call. Located at `%persistentDataPath%/ModFramework/audit-YYYY-MM-DD.log` (one file per day, 30-day retention). Every privileged call writes a line; the line is also `Debug.Log`'d so it shows up in `output_log.txt`.

```csharp
public static class AuditLog
{
    public static void Log(string modId, string displayName, string operation, string target, string result, string notes);
    public static string GetLogPath(DateTime day);
    public static void PurgeOldLogs();
}
```

You can call `AuditLog.Log` directly to log your own mod's non-framework events (e.g. "MYMOD_POLL_START"). This makes the audit log a single source of truth for "what did this mod do?".

**Line format:** `[timestamp] [modId] [displayName] operation target result notes`

Example line:
```
[2026-07-16 14:23:01] [com.zicarius.limitlessteams] [LimitlessTeams] FILE_WRITE [C:\...\Mods\LimitlessTeams\Data\settings.json] OK [124 bytes]
```

An in-game "Mod Audit Log" window is planned for v6.0.1. Today the log is on disk + in `Debug.Log`.

### SecurityGuards + FrameworkSignatureCheck

These are internal. You don't call them directly. `SecurityGuards.RequirePermission` is the per-op permission check (24 call sites across 4 v6.0 API surface files). `FrameworkSignatureCheck.RequireValid` is the static-ctor check that compares the loaded framework DLL's public key token against the hard-coded `e0967644e3ffec06`.

## Exception types

| Exception | Thrown by | Cause |
|---|---|---|
| `ModPathException` | `SafePath` factories | Path is relative, contains illegal characters, contains `..`, or doesn't resolve |
| `ModPermissionException` | `SecurityGuards.RequirePermission` | Identity lacks the required `Permission` flag |
| `ModSecurityException` | `ModEvents.Trigger` (owner mismatch), `ModServiceHost.Register` (name collision) | Tried to use another mod's `EventKey` / `ServiceToken` |
| `FrameworkSignatureException` | `FrameworkSignatureCheck.RequireValid` | Framework DLL's public key token doesn't match |

`ModPermissionException` carries rich data — `RequiredPermission`, `GrantedPermissions`, and the identity's `ModId`. Log these to your in-game error UI so the player can see which mod is missing which permission.

## Best practices

1. **Always use a `ModIdentity` for the lifetime of your mod.** Store `_id` in a field on your `ModBehaviour`. Call `OnActivate` in `OnActivate` and `OnDeactivate` in `OnDeactivate`. Never pass `null` to a privileged framework method.
2. **Use the smallest permission set.** Start with the closest preset (`Patcher` for most mods). Add only the flags you actually use. Fewer flags = more trust from the player.
3. **Wrap risky code in `ModSafety.Try`.** The number one rule of modding Software Inc: don't break the game's loops. Wrap your `Update()` logic, UI callbacks, and risky API calls in `ModSafety.Try` (or `ThrottledErrorHandler` for per-frame code).
4. **Cache `EventKey` / `ServiceToken` in public static fields.** Other mods need to subscribe to your events and find your services. Publish the keys/tokens as `public static` readonly fields on your mod class.
5. **Don't store unencrypted secrets in mod data.** The audit log can see every file write. Don't write passwords, API tokens, or other secrets.
6. **C# 5 portability for the Workshop.** The framework's own code is C# 5 (no `?.`, no `$"..."`, no `nameof`, no auto-property initializers). Your consumer code can be any C# version.
7. **Always call `OnDeactivate`.** Forgetting it leaves your identity in `ModRegistry` and your patches live. If the player disables and re-enables your mod in the in-game mod manager, the stale identity causes a permission mismatch.
8. **Use `SafePath` sub-paths, not absolute paths.** `SafePath.GetModDataPathSafe(id, "saves", "session.json")` is portable across machines and mod install locations.
9. **Audit-log your own non-framework events.** Use `AuditLog.Log(myId.ModId, myId.DisplayName, "MYMOD_POLL_START", "", "OK", "")` for your own mod events. The audit log becomes a single source of truth.
10. **Don't call v4.x `[Obsolete]` classes in new code.** The `[Obsolete]` back-compat is for emergency hotfixes on v5.x mods only. New code uses the v6.0 API. The v4.x single-file classes (`UIHelper`, `ModLogger`, `ModSettings` v4.x, `Notifications`) are on the removal track for v6.1.

## Build, deploy, verify

### Build (framework authors)

```powershell
cd modframework
.\Build-ModFramework.ps1
```

The build script:
1. Pre-checks that `_secure/ModFramework.snk` exists.
2. Runs `msbuild ModFramework.csproj /p:Configuration=Release` (signs with the strong-name key).
3. ILMerges 0Harmony 2.3.3.0 and Newtonsoft.Json 13.0.3 into the signed DLL. Uses `/ndebug` to avoid the `ISymUnmanagedWriter.Close()` catastrophic failure when `DebugType=pdbonly` is set on Release.
4. Post-checks the public key token is `e0967644e3ffec06` via `sn.exe -T`.
5. Outputs to `modframework/bin/Release/ModFramework.dll`.

### Deploy (you, for your mod)

```powershell
Copy-Item modframework/bin/Release/ModFramework.dll "E:\SteamLibrary\steamapps\common\Software Inc\Software Inc_Data\Managed\ModFramework.dll"
```

The game auto-loads DLLs from `Managed/` on launch.

### Verify (in-game)

The framework self-verifies on first privileged call:
1. `FrameworkSignatureCheck.RequireValid` confirms the public key token matches.
2. `ModFrameworkActivator.OnActivate` reads your mod's `meta.tyd` and checks the `Permissions:` line.
3. Every privileged call writes an audit log line.
4. The audit log is at `%persistentDataPath%/ModFramework/audit-YYYY-MM-DD.log`. Open the latest file to see your mod's operations.

# v6.1 — hardening, in-game UI, and the four barriers

v6.1 builds on v6.0. It closes the last `[Obsolete]` bypass, ships the in-game
audit UI, removes the Newtonsoft dependency, and — most importantly for mod
authors — makes explicit the four barriers every v6.1 mod must clear.

## What changed since v6.0

- **v6.1.0** — the v5.x `[Obsolete]` back-compat wrappers (28 of them in `Core/`) are **deleted**, and the 6 v4.x single-file classes (`UIHelper`, `ModLogger`, `ModEvents`, `Notifications`, `ModSettings`, `ModUtils`) are now `internal`. The wrappers had skipped `SecurityGuards.RequirePermission`, so a malicious DLL could do file I/O / events / services with no declared permission. That bypass is now closed (verified by `tests/ModFrameworkV61BypassTest.cs`, which produces 20 expected compile errors). `AssemblyVersion` 6.0.1.0 → 6.1.0.0; strong-name token unchanged (`e0967644e3ffec06`). Workshop CS mods that relied on the wrappers must pin to v6.0.0 — see [MIGRATION_v5_to_v6.md](MIGRATION_v5_to_v6.md).
- **v6.1.0** — in-game **Mod Audit Log** + **Mod permissions** windows, hosted by a tiny shim mod (`ModFrameworkSettings/`). The framework does all the UI work; the shim just registers the Mods-tab entry.
- **v6.1.1** — **Newtonsoft.Json removed.** Its 13.x static initializer throws under the game's Unity 2018.4 Mono runtime, so every `WriteJson` failed silently and settings never persisted. `ModFileAccess` now uses Unity's engine-native `JsonUtility`. The DLL is ~700 KB smaller (~2.56 MB). **Settings classes must be `[Serializable]` with public fields** — no `Dictionary`, no properties, no top-level arrays (wrap arrays in a `[Serializable]` container class).
- **v6.1.2** — in-game Settings window no longer clips long permission lists / audit logs (`OptionsWindow.AddModOption` measures the scroll region once from direct children; fixed with zero-size containers, `CanvasGroup.alpha` tab toggling, and per-label self-sizing). Verified in-game 2026-07-17.

## Installation (v6.1)

One file: `ModFramework.dll` (signed, **~2.56 MB** as of v6.1.1, with 0Harmony 2.3.3 ILMerged — Newtonsoft is no longer bundled). Drop it into:

```
<game>/Software Inc_Data/Managed/ModFramework.dll
```

Optionally add the `ModFrameworkSettings` shim mod to `DLLMods/` for the in-game audit/permissions windows.

## Working with the v6.1 Security Barriers

v6.1 is stricter than v5.x on purpose. Coming from the old API, here are the
four barriers you now have to satisfy — and exactly how to clear each.

### Barrier 1 — Your mod MUST be a pre-compiled DLL mod

The game's in-game C# compiler (`DynamicCSharp`) has a hardcoded assembly
whitelist that does **not** include `ModFramework.dll`. A **source mod** that
does `using ModFramework.Core;` fails to compile with:

```
CS0246: The type or namespace name 'ModFramework' could not be found
```

**Fix:** ship your mod as a **pre-compiled DLL**. Build a `.csproj` that
references `ModFramework.dll` from your local `Managed/` folder, then drop the
built DLL in `DLLMods/<YourMod>/` alongside its `meta.tyd`. The regular .NET
loader resolves the reference and the in-game compiler is bypassed entirely.
The `ModFrameworkExample` and `ModFrameworkSettings` mods both ship this way —
use `ModFrameworkExample/` as your copy-paste starting point.

### Barrier 2 — Declare permissions in meta.tyd, or the call throws

Every privileged call checks a permission flag before doing anything. Call
`ModFileAccess.WriteText(...)` without `FileWrite` declared and you get a
`ModPermissionException` at runtime (audit-logged as a DENY). Declare what you
need in `meta.tyd`:

```
Permissions: Patcher
```

Presets expand to flag sets. `Patcher` =
`FileRead, FileWrite, FileDirectoryList, HarmonyRead, HarmonyPatch, HarmonyUnpatch, SettingsRead, SettingsWrite`
— note it does **NOT** include `FileDelete`. If your mod deletes files, append
the flag explicitly:

```
Permissions: Patcher, FileDelete, ServiceRegister
```

If `meta.tyd` has no `Permissions:` line, the framework defaults to `Patcher`
and logs a warning. See "The 22 permission flags" above for the full table.

### Barrier 3 — Get a ModIdentity, pass it to every privileged call

You can't call the file / event / service / Harmony APIs anonymously anymore.
Call `ModFrameworkActivator.OnActivate(this)` once in `OnActivate`. It:

1. reads your `meta.tyd` `Permissions:` line,
2. verifies the framework's strong-name signature,
3. mints a per-session `ModIdentity` (unforgeable — `internal` constructor), and
4. writes an `ACTIVATE` line to the audit log.

Pass that identity as the **first argument** of every privileged call. If it
returns `null`, the framework refused you (bad signature, or the caller isn't a
registered DLL mod) — bail out immediately:

```csharp
myId = ModFrameworkActivator.OnActivate(this);
if (myId == null) return;   // refused — do not proceed
```

### Barrier 4 — File I/O needs a SafePath, not a raw string

`ModFileAccess` no longer accepts raw path strings. You get a `SafePath` from
one of four allowlisted factories:

| Factory | Root | Access |
|---|---|---|
| `SafePath.GetModDataPathSafe(id, ...)` | your mod's `Data/` folder | read/write |
| `SafePath.GetPersistentDataPathSafe(id, ...)` | `%persistentDataPath%/Mods/<modId>/` | read/write |
| `SafePath.GetManagedPathSafe(id, file)` | game's `Managed/` folder | read-only |
| `SafePath.GetUserApprovedPath(id, path)` | a location the user explicitly approves | read/write |

Any path containing `..`, a drive-relative segment, or one of `: * ? " < > |`,
or resolving outside these roots, throws `ModPathException`. This is what closes
the "write to `C:\Windows\System32`" attack — you literally cannot construct a
`SafePath` pointing there.

### The payoff

Once you clear the four barriers, every file write, event, service call, and
Harmony patch your mod makes is **audit-logged** — visible in the in-game Audit
Log tab and in `%persistentDataPath%/ModFramework/audit-YYYY-MM-DD.log` — so
users can see exactly what your mod did and revoke trust if they don't like it.

Serialize settings with Unity's `JsonUtility`: mark the settings class
`[Serializable]`, use public fields only (no `Dictionary`, no properties, no
top-level arrays). The framework no longer bundles Newtonsoft.

## File Layout After v6.0

```
ModFramework/
+- ModFramework.cs              # v6.0 header comment
+- ModFramework.csproj          # +strong-name signing, +v6.0 Core/ entries
+- Build-ModFramework.ps1       # standalone build + ILMerge script
+- README.md
+- DOCUMENTATION.md             # this file
+- MIGRATION_v5_to_v6.md        # v5.x -> v6.0 migration guide
+- MODFRAMEWORK_MEMORY.md       # design decisions + lessons learned
+- NEXUS_DESCRIPTION.bbcode     # raw BBCode for the Nexus page
|
+- Core/
|  +- ModFrameworkPublicAPI.cs  # the [ModFrameworkPublicAPI("v6.0")] attribute
|  +- ModIdentity.cs            # the unforgeable per-mod identity
|  +- Permission.cs             # the 22 fine-grained flags
|  +- PermissionPresets.cs      # the 4 named presets
|  +- SafePath.cs               # the validated path wrapper
|  +- EventKey.cs               # the unforgeable event handle
|  +- ServiceToken.cs           # the unforgeable service handle
|  +- ModRegistry.cs            # internal registry of identities
|  +- SecurityGuards.cs         # the per-op permission check
|  +- FrameworkSignatureCheck.cs# the public key token check
|  +- AuditLog.cs               # the per-day audit log writer
|  +- ModFrameworkActivator.cs  # the single OnActivate / OnDeactivate entry point
|  +- ModFileAccess.cs          # the file I/O API
|  +- ModHarmony.cs             # the Harmony wrapper
|  +- ModEvents.cs              # the EventKey + GlobalEventKind API
|  +- ModServiceHost.cs         # the service registration (provider side)
|  +- ModServiceBridge.cs       # the service consumption (consumer side)
|  +- ModSafety.cs              # unchanged from v5.1
|  +- ModUtils.cs               # unchanged from v5.1
|  +- ModDependencies.cs        # unchanged from v5.1
|  +- ModLoader.cs              # unchanged from v5.1
|
+- _secure/
|  \- ModFramework.snk          # private strong-name key (gitignored)
+- keys/
|  +- ModFramework.pub          # public strong-name key (committed)
|  +- README.md                 # verification instructions
|
+- Properties/
|  \- AssemblyInfo.cs           # v6.0.0.0
|
+- meta.tyd_template            # template with Permissions: Patcher
|
+- GameData/                    # unchanged from v5.x
+- UI/                          # unchanged from v5.x
+- Harmony/                     # 0Harmony 2.3.3 (source for ILMerge)
+- Vendor/                      # Newtonsoft.Json 13.0.3 (source for ILMerge)
+- Tools/                       # ILMerge setup
\- Scaffolding/                 # CreateMod.ps1, CreateModGUI.ps1, Templates/
```

## Need help?

- Migrating from v5.x? See [`MIGRATION_v5_to_v6.md`](MIGRATION_v5_to_v6.md) — the before/after guide for every API change.
- Looking for a working mod that uses every v6.0 API? See [`../ModFrameworkExample/`](../ModFrameworkExample/) — a real published Nexus mod scaffolded as the canonical reference.

