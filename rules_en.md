# Slay the Spire 2 Mod Development Rules

## Project Structure

```
src/
├── ModEntry.cs              # Mod entry — only modify ModId and ModName, do not change logic
├── Hooks/
│   ├── CombatHooks.cs       # Combat-related Hooks
│   ├── MapHooks.cs          # Map-related Hooks
│   └── ...                  # Split by feature module
├── <ModName>.csproj         # Project file — do not modify
└── <ModName>.json           # Mod manifest — id/has_dll/has_pck must be correct
```

## Mandatory Rules

### 1. ModEntry.cs Must Not Be Modified
- The `Initialize()` method and `Logger` class code logic **cannot be modified**
- Only `ModId` and `ModName` constants can be changed
- `ModId` must match the manifest `id`, and equal the folder name/DLL filename

### 2. Hook Files Split by Feature
- Create multiple `.cs` files in `src/Hooks/` directory
- Each file handles one feature module (e.g., `CombatHooks.cs`, `MapHooks.cs`, `RewardHooks.cs`)
- Single file must not exceed 200 lines; split further if needed
- Filenames use PascalCase, ending with `Hooks`

### 3. Hook Implementation
- Use `[HarmonyPatch]` + `Postfix`/`Prefix` for injection
- Must use `Logger.Log()` to record key operations
- Every method signature must be verified via ILSpy MCP decompilation, **guessing signatures is forbidden**
- Postfix method names for different Hooks cannot duplicate (compilation error)
- Recommend using nested classes to wrap patches for same-named methods (avoid method name conflicts)

### 4. Manifest Rules
- `id` must equal the folder name, DLL filename, and PCK filename
- `has_dll: true` and `has_pck: true` must be set, otherwise the game won't load
- `mod_manifest.json` does not need an `id` field (game will error but it's harmless)
- Game loads DLL from path `{mod_folder}/{id}.dll`

### 5. Hook Target Class
All Hooks are defined on the `MegaCrit.Sts2.Core.Hooks.Hook` static class, which is a virtual method placeholder class that Harmony patches to inject logic.

### 6. Value Modification Hooks
When modifying return values, use `ref` parameter:

```csharp
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Hooks.Hook), nameof(MegaCrit.Sts2.Core.Hooks.Hook.ModifyDamage))]
public static void Postfix(ref decimal __result)
{
    __result *= 2;
}
```

Parameter signatures must match ILSpy MCP decompilation results.

### 7. Event Notification Hooks
No return value, only used for receiving notifications:

```csharp
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Hooks.Hook), nameof(MegaCrit.Sts2.Core.Hooks.Hook.AfterCardPlayed))]
public static void Postfix()
{
    Logger.Log("[Hook] AfterCardPlayed triggered");
}
```

## Prohibitions

- Do not modify `Initialize()` method and `Logger` class in `ModEntry.cs`
- Do not modify `<ModName>.csproj` (except GameDir, managed by install.ps1)
- Do not guess parameter signatures in Hook methods, must verify with ILSpy MCP
- Do not add NuGet package reference for Harmony (use game's bundled `0Harmony.dll`)
- Do not include malicious logic in code
- Do not pile all Hooks in a single file (must split by feature)
