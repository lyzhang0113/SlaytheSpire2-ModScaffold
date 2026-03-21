# Slay the Spire 2 Mod Scaffold

A zero-config scaffold for Slay the Spire 2 mod development with AI-assisted vibe coding. Clone, run `install.bat`, start coding.

## Acknowledgments

This project uses the following open-source projects:

- **[STS2MCP](https://github.com/Gennadiyev/STS2MCP)** - MCP server for programmatic game control (play cards, read state, navigate menus)
- **[ILSpy-Mcp](https://github.com/maces/ILSpy-Mcp)** - MCP server for .NET assembly decompilation and code exploration
- **[Modding-Tutorial](https://github.com/fresh-milkshake/Modding-Tutorial)** - Reference documentation and examples for Slay the Spire 2 mod development
- **[STS2-Agent](https://github.com/CharTyr/STS2-Agent)** - Reference for main menu API (character select, timeline, embark)

## Quick Start

```powershell
# Double-click or run in PowerShell:
.\install.bat
```

That's it. This single command:

1. Installs .NET 10 SDK + .NET 8 runtime (isolated to `tools/dotnet/`)
2. Downloads Godot 4.5.1 Mono, ILSpy
3. Builds ILSpy MCP Server and STS2MCP
4. Configures MCP servers in `opencode.jsonc`
5. Builds your mod and installs it to the game
6. **Launches the game and verifies mod loading** (Deep Verification)

**Note**: Steam must be running and logged in for Deep Verification.

## Project Structure

```
SlaytheSpire2ModVibeCoding/
├── src/
│   ├── ModEntry.cs              # Mod entry point - DO NOT modify logic
│   ├── Hooks/                   # YOUR CODE GOES HERE (split by feature)
│   │   └── .gitkeep
│   ├── Sts2ModScaffold.csproj   # .NET 10 project - DO NOT modify
│   └── com.vibecoding.sts2mod.json  # Mod manifest
├── tools/
│   ├── dotnet/                  # Isolated .NET SDK/runtime
│   ├── godot/                   # Godot 4.5.1 Mono
│   ├── ilspy/                   # ILSpy decompiler
│   ├── ILSpy-Mcp/               # ILSpy MCP Server
│   ├── STS2MCP/                 # Game control MCP
│   ├── uv/                      # Python package manager
│   └── pck_builder/             # PCK builder script
├── references/                  # Game DLLs (sts2.dll, 0Harmony.dll, GodotSharp.dll)
├── docs/plans/                  # Implementation plans
├── rules.md                     # Development rules (MUST READ)
├── AGENTS.md                    # AI Agent workflow guide
├── install.bat                  # One-click setup
└── install-mod.bat              # Rebuild & install mod
```

## Daily Workflow

```powershell
# 1. Edit files in src/Hooks/

# 2. Rebuild and install
.\install-mod.bat

# 3. Launch game
.\tools\launch_sts2.ps1

# 4. Check logs
.\tools\read_sts2_logs.ps1
```

## MCP Servers

Two MCP servers are pre-configured in `opencode.jsonc`:

| Server | Purpose |
|--------|---------|
| **ILSpy MCP** | Decompile `sts2.dll` to explore game internals and verify hook signatures |
| **STS2MCP** | Control the game programmatically (play cards, read state, navigate menus) |
| **STS2MenuControl** | Main menu control (start new game, select character, timeline, abandon run) |

### ILSpy MCP - Code Decompilation

**Never guess method signatures - always verify with ILSpy MCP.**

```python
# View class structure
decompile_type(assembly_path="references/sts2.dll", type_name="MegaCrit.Sts2.Core.Hooks.Hook")

# View method signature
decompile_method(assembly_path="references/sts2.dll", type_name="...", method_name="...")
```

### STS2MCP - Game Control

For automated testing (requires game running):

```python
get_game_state()              # Get current game state
combat_play_card(0)           # Play card at index 0
combat_end_turn()             # End turn
rewards_claim(0)              # Claim reward
map_choose_node(0)            # Choose map node
```

### STS2MenuControl - Main Menu Control

HTTP API on `localhost:8081` for controlling the main menu (complements STS2MCP):

```
GET  /api/v1/menu                    # Get current menu state
POST /api/v1/menu                    # Execute menu action
```

| Action | Description |
|--------|-------------|
| `open_character_select` | Open character selection screen |
| `select_character` (option_index) | Select a character |
| `embark` | Start a new game |
| `continue_run` | Continue existing save |
| `abandon_run` | Abandon current save |
| `open_timeline` | Open timeline screen |
| `choose_timeline_epoch` (option_index) | Select a timeline epoch |
| `confirm_timeline_overlay` | Confirm timeline dialog |
| `close_main_menu_submenu` | Close current submenu |
| `return_to_main_menu` | Return to main menu (from game over) |
| `confirm_modal` / `dismiss_modal` | Interact with modal dialogs |

## Vibe Coding with AI

This scaffold is designed for AI-assisted development (OpenCode, Claude, etc.):

1. Describe your mod in natural language
2. AI uses ILSpy MCP to find correct hooks and verify signatures
3. AI writes code in `src/Hooks/`
4. AI builds and installs via `install-mod.bat`
5. AI tests using STS2MCP
6. You verify in-game

**Important**: AI agents must read `rules.md` and `AGENTS.md` before development.

## Hooks Reference

All hooks are defined on `MegaCrit.Sts2.Core.Hooks.Hook`. See the full hook list in existing README sections.

### Hook Types

1. **Event Hooks** (return `Task`, use Postfix)
   - `AfterCardPlayed`, `BeforeCombatStart`, `AfterDamageReceived`, etc.

2. **Value Modify Hooks** (return value, use `ref __result`)
   - `ModifyDamage`, `ModifyBlock`, `ModifyEnergyCostInCombat`, etc.

3. **Boolean Gate Hooks** (return `bool`, allow/deny)
   - `ShouldDie`, `ShouldPlay`, `ShouldDraw`, etc.

### Example Hook

```csharp
// src/Hooks/CombatHooks.cs
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Hooks.Hook), nameof(MegaCrit.Sts2.Core.Hooks.Hook.ModifyDamage))]
public static class ModifyDamagePatch
{
    public static void Postfix(ref decimal __result)
    {
        __result *= 2; // Double all damage
    }
}
```

## Mod File Structure

The game expects this layout in `mods/`:

```
mods/YourModName/
├── YourModName.dll        # Compiled assembly
├── YourModName.pck        # Godot resources
├── YourModName.json       # Mod manifest (id, has_pck, has_dll)
└── mod_manifest.json      # Godot manifest (pck_name)
```

**Critical**: `id` in `YourModName.json` must match folder name, DLL name, and PCK name.

## Scripts Reference

| Script | Purpose |
|--------|---------|
| `install.bat` | Full environment setup (first run only) |
| `install-mod.bat` | Rebuild & install mod (daily use) |
| `uninstall-mod.bat` | Remove mod and STS2MCP from game |
| `tools/launch_sts2.ps1` | Launch game via Steam |
| `tools/close_sts2.ps1` | Force-close game |
| `tools/wait_sts2.ps1` | Wait for game process |
| `tools/read_sts2_logs.ps1` | View game logs |
| `tools/rename-scaffold.ps1` | Rename scaffold to your mod name |

## Rename Scaffold

When starting a new mod, rename everything:

```powershell
.\tools\rename-scaffold.ps1 -NewModName "MyAwesomeMod"
```

This updates:
- `.csproj` filename and namespace
- `.json` manifest id and pck_name
- `ModEntry.cs` ModId and ModName

## Requirements

- **Windows** (Godot and game are Windows-only)
- **Git** (must be in PATH)
- **Slay the Spire 2** via Steam
- **Steam** logged in (for game launch and Deep Verification)

Everything else (.NET, Godot, uv, ILSpy) is installed automatically.

> **Note**: This scaffold has only been tested on Windows. Other platforms (macOS, Linux) have not been tested.

## Logging

```csharp
Logger.Log("[Hook] Your message here");
```

Logs written to: `%APPDATA%\SlayTheSpire2\logs\mod_log.txt`

View logs: `.\tools\read_sts2_logs.ps1`

## Troubleshooting

### Mod Not Loading

1. Check `godot.log` for `ERROR` and `WARNING`
2. Verify `id` in manifest matches folder/DLL/PCK names
3. Confirm `has_dll: true` and `has_pck: true` are set
4. First-time mods may show "mods warning" - confirm in-game

### Hooks Not Working

1. Verify signature with ILSpy MCP - **never guess**
2. Check `[HarmonyPatch]` attribute is correct
3. Confirm hook is triggered via `Logger.Log()` output

### Build Failed

1. Run `install.bat` first
2. Verify `references/` contains `sts2.dll`, `0Harmony.dll`, `GodotSharp.dll`
3. Check `GameDir` in `.csproj` points to correct game directory

## Documentation

- **`rules.md`** - Development rules and constraints (MUST READ)
- **`AGENTS.md`** - AI Agent workflow guide
- **`docs/plans/`** - Implementation plans directory

## License

MIT
