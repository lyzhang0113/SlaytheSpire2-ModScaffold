# 杀戮尖塔 2 Mod 开发规范

## 项目结构

```
src/
├── ModEntry.cs              # Mod 入口 — 只修改 ModId 和 ModName，不要改动逻辑
├── Hooks/
│   ├── CombatHooks.cs       # 战斗相关 Hook
│   ├── MapHooks.cs          # 地图相关 Hook
│   └── ...                  # 按功能模块拆分
├── <ModName>.csproj         # 项目文件 — 不要修改
└── <ModName>.json           # Mod 清单 — id/has_dll/has_pck 必须正确
```

## 必须遵守的规则

### 1. ModEntry.cs 禁止修改
- `Initialize()` 方法和 `Logger` 类的代码逻辑**不可修改**
- 只允许修改 `ModId` 和 `ModName` 两个常量
- `ModId` 必须与 manifest 的 `id` 一致，且等于文件夹名/DLL 文件名

### 2. Hook 文件按功能拆分
- 在 `src/Hooks/` 目录下创建多个 `.cs` 文件
- 每个文件负责一个功能模块（如 `CombatHooks.cs`、`MapHooks.cs`、`RewardHooks.cs`）
- 单个文件不超过 200 行，超过时必须继续拆分
- 文件名用 PascalCase，以 `Hooks` 结尾

### 3. Hook 写法
- 使用 `[HarmonyPatch]` + `Postfix`/`Prefix` 注入
- 必须用 `Logger.Log()` 记录关键操作
- 每个方法签名必须通过 ILSpy MCP 反编译验证，**禁止猜测签名**
- 不同 Hook 的 Postfix 方法名不能重复（编译报错）
- 建议用嵌套类包裹同名方法的 Patch（避免方法名冲突）

### 4. Manifest 清单规则
- `id` 必须等于文件夹名、DLL 文件名、PCK 文件名
- `has_dll: true` 和 `has_pck: true` 必须设置，否则游戏不会加载
- `mod_manifest.json` 不需要 `id` 字段（游戏会报错但无害）
- 游戏从路径 `{mod_folder}/{id}.dll` 加载 DLL

### 5. Hook 目标类
所有 Hook 定义在 `MegaCrit.Sts2.Core.Hooks.Hook` 静态类上，这是一个虚方法占位符类，Harmony 对它们打补丁以注入逻辑。

### 6. 数值修改类 Hook
修改返回值时，使用 `ref` 参数：

```csharp
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Hooks.Hook), nameof(MegaCrit.Sts2.Core.Hooks.Hook.ModifyDamage))]
public static void Postfix(ref decimal __result)
{
    __result *= 2;
}
```

参数签名必须与 ILSpy MCP 反编译结果一致。

### 7. 事件通知类 Hook
无返回值，仅用于接收通知：

```csharp
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Hooks.Hook), nameof(MegaCrit.Sts2.Core.Hooks.Hook.AfterCardPlayed))]
public static void Postfix()
{
    Logger.Log("[Hook] AfterCardPlayed triggered");
}
```

## 禁止事项

- 禁止修改 `ModEntry.cs` 的 `Initialize()` 方法和 `Logger` 类
- 禁止修改 `<ModName>.csproj`（GameDir 除外，由 install.ps1 自动管理）
- 禁止在 Hook 方法中猜测参数签名，必须用 ILSpy MCP 验证
- 禁止添加 NuGet 包引用 Harmony（使用游戏自带的 `0Harmony.dll`）
- 禁止在代码中包含恶意逻辑
- 禁止将所有 Hook 堆积在单个文件中（必须按功能拆分）
