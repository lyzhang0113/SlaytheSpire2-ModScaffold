# 杀戮尖塔 2 Mod 开发脚手架

零配置杀戮尖塔 2 Mod 开发脚手架，支持 AI 辅助的 Vibe Coding。克隆、运行 `install.bat`、开始编码。

## 致谢

本项目使用以下开源项目：

- **[STS2MCP](https://github.com/Gennadiyev/STS2MCP)** - MCP 服务器，用于程序化游戏控制（出牌、读取状态、导航菜单）
- **[ILSpy-Mcp](https://github.com/maces/ILSpy-Mcp)** - MCP 服务器，用于 .NET 程序集反编译和代码探索
- **[Modding-Tutorial](https://github.com/fresh-milkshake/Modding-Tutorial)** - 杀戮尖塔 2 Mod 开发参考文档和示例
- **[STS2-Agent](https://github.com/CharTyr/STS2-Agent)** - 主菜单 API 参考（角色选择、时间线、开始游戏）

## 快速开始

```powershell
# 双击或在 PowerShell 中运行：
.\install.bat
```

就这么简单。这一条命令会：

1. 安装 .NET 10 SDK + .NET 8 运行时（隔离到 `tools/dotnet/`）
2. 下载 Godot 4.5.1 Mono、ILSpy
3. 构建 ILSpy MCP Server 和 STS2MCP
4. 在 `opencode.jsonc` 中配置 MCP 服务器
5. 构建 Mod 并安装到游戏
6. **启动游戏并验证 Mod 加载**（深度验证）

**注意**：深度验证需要 Steam 已启动并登录。

## 项目结构

```
SlaytheSpire2ModVibeCoding/
├── src/
│   ├── ModEntry.cs              # Mod 入口点 - 禁止修改逻辑
│   ├── Hooks/                   # 在这里写你的代码（按功能拆分）
│   │   └── .gitkeep
│   ├── Sts2ModScaffold.csproj   # .NET 10 项目 - 禁止修改
│   └── com.vibecoding.sts2mod.json  # Mod 清单
├── tools/
│   ├── dotnet/                  # 隔离的 .NET SDK/运行时
│   ├── godot/                   # Godot 4.5.1 Mono
│   ├── ilspy/                   # ILSpy 反编译器
│   ├── ILSpy-Mcp/               # ILSpy MCP 服务器
│   ├── STS2MCP/                 # 游戏控制 MCP
│   ├── uv/                      # Python 包管理器
│   └── pck_builder/             # PCK 构建脚本
├── references/                  # 游戏 DLL（sts2.dll, 0Harmony.dll, GodotSharp.dll）
├── docs/plans/                  # 实现计划目录
├── rules.md                     # 开发规范（必读）
├── AGENTS.md                    # AI Agent 工作流指南
├── install.bat                  # 一键安装
└── install-mod.bat              # 重新构建并安装 Mod
```

## 日常工作流

```powershell
# 1. 编辑 src/Hooks/ 下的文件

# 2. 重新构建并安装
.\install-mod.bat

# 3. 启动游戏
.\tools\launch_sts2.ps1

# 4. 查看日志
.\tools\read_sts2_logs.ps1
```

## MCP 服务器

`opencode.jsonc` 中预配置了两个 MCP 服务器：

| 服务器 | 用途 |
|--------|------|
| **ILSpy MCP** | 反编译 `sts2.dll` 探索游戏内部结构、验证 Hook 签名 |
| **STS2MCP** | 程序化控制游戏（出牌、读取状态、导航菜单） |
| **STS2MenuControl** | 主菜单控制（开始新游戏、选择角色、时间线、放弃存档） |

### ILSpy MCP - 代码反编译

**永远不要猜测方法签名 - 必须用 ILSpy MCP 验证。**

```python
# 查看类结构
decompile_type(assembly_path="references/sts2.dll", type_name="MegaCrit.Sts2.Core.Hooks.Hook")

# 查看方法签名
decompile_method(assembly_path="references/sts2.dll", type_name="...", method_name="...")
```

### STS2MCP - 游戏控制

用于自动化测试（需要游戏运行中）：

```python
get_game_state()              # 获取当前游戏状态
combat_play_card(0)           # 打出索引 0 的卡牌
combat_end_turn()             # 结束回合
rewards_claim(0)              # 领取奖励
map_choose_node(0)            # 选择地图节点
```

### STS2MenuControl - 主菜单控制

HTTP API 在 `localhost:8081` 上控制主菜单（补充 STS2MCP 的功能）：

```
GET  /api/v1/menu                    # 获取当前菜单状态
POST /api/v1/menu                    # 执行菜单操作
```

| 操作 | 说明 |
|------|------|
| `open_character_select` | 打开角色选择界面 |
| `select_character` (option_index) | 选择角色 |
| `embark` | 开始新游戏 |
| `continue_run` | 继续存档 |
| `abandon_run` | 放弃存档 |
| `open_timeline` | 打开时间线 |
| `choose_timeline_epoch` (option_index) | 选择时间线时代 |
| `confirm_timeline_overlay` | 确认时间线弹窗 |
| `close_main_menu_submenu` | 关闭子菜单 |
| `return_to_main_menu` | 返回主菜单（从 Game Over） |
| `confirm_modal` / `dismiss_modal` | 弹窗交互 |

## Vibe Coding with AI

本脚手架专为 AI 辅助开发设计（OpenCode、Claude 等）：

1. 用自然语言描述你想要的 Mod
2. AI 使用 ILSpy MCP 找到正确的 Hook 并验证签名
3. AI 在 `src/Hooks/` 中编写代码
4. AI 通过 `install-mod.bat` 构建并安装
5. AI 使用 STS2MCP 进行测试
6. 你在游戏中验证

**重要**：AI Agent 必须在开发前阅读 `rules.md` 和 `AGENTS.md`。

## Hook 参考

所有 Hook 定义在 `MegaCrit.Sts2.Core.Hooks.Hook` 上。完整 Hook 列表见下方。

### Hook 类型

1. **事件 Hook**（返回 `Task`，使用 Postfix）
   - `AfterCardPlayed`、`BeforeCombatStart`、`AfterDamageReceived` 等

2. **数值修改 Hook**（有返回值，使用 `ref __result`）
   - `ModifyDamage`、`ModifyBlock`、`ModifyEnergyCostInCombat` 等

3. **布尔门控 Hook**（返回 `bool`，允许/拒绝）
   - `ShouldDie`、`ShouldPlay`、`ShouldDraw` 等

### Hook 示例

```csharp
// src/Hooks/CombatHooks.cs
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Hooks.Hook), nameof(MegaCrit.Sts2.Core.Hooks.Hook.ModifyDamage))]
public static class ModifyDamagePatch
{
    public static void Postfix(ref decimal __result)
    {
        __result *= 2; // 所有伤害翻倍
    }
}
```

## Hook 完整列表

### 事件 Hook（返回 Task，使用 Postfix）

| 分类 | Hook | 说明 |
|------|------|------|
| **战斗** | `BeforeCombatStart` | 战斗开始前 |
| | `AfterCombatEnd` | 战斗结束后 |
| | `AfterCombatVictory` | 战斗胜利后 |
| | `AfterCreatureAddedToCombat` | 生物加入战斗时 |
| | `BeforeSideTurnStart` / `AfterSideTurnStart` | 侧面回合开始 |
| | `BeforePlayPhaseStart` | 出牌阶段前 |
| **卡牌** | `BeforeCardPlayed` / `AfterCardPlayed` | 出牌生命周期 |
| | `BeforeCardAutoPlayed` | 自动出牌时 |
| | `AfterCardDrawn` | 抽牌后 |
| | `AfterCardDiscarded` | 丢弃后 |
| | `AfterCardExhausted` | 消耗后 |
| | `AfterCardRetained` | 保留（回合结束） |
| | `AfterCardEnteredCombat` | 卡牌进入战斗 |
| | `AfterCardGeneratedForCombat` | 卡牌生成时 |
| | `AfterCardChangedPiles` | 卡牌移动到其他牌堆 |
| | `AfterHandEmptied` | 手牌清空时 |
| | `AfterHandDraw` / `BeforeHandDraw` | 抽牌阶段 |
| | `AfterFlush` / `BeforeFlush` | 清空手牌 |
| | `AfterShuffle` | 洗牌后 |
| **伤害** | `BeforeAttack` / `AfterAttack` | 攻击生命周期 |
| | `BeforeDamageReceived` / `AfterDamageReceived` | 受伤生命周期 |
| | `AfterDamageGiven` | 造成伤害后 |
| **回合** | `AfterPlayerTurnStart` | 玩家回合开始 |
| | `BeforeTurnEnd` / `AfterTurnEnd` | 回合结束 |
| | `ShouldTakeExtraTurn` | 额外回合 |
| | `AfterTakingExtraTurn` | 额外回合结束后 |
| | `AfterEnergyReset` / `AfterEnergySpent` | 能量管理 |
| **死亡** | `BeforeDeath` / `AfterDeath` | 死亡生命周期 |
| | `AfterPreventingDeath` | 死亡被阻止时 |
| | `AfterDiedToDoom` | Doom 击杀触发 |
| | `ShouldCreatureBeRemovedFromCombatAfterDeath` | 死亡后清理检查 |
| **格挡** | `BeforeBlockGained` / `AfterBlockGained` | 获得格挡 |
| | `AfterBlockBroken` | 格挡被打碎 |
| | `AfterBlockCleared` / `AfterPreventingBlockClear` | 格挡清除 |
| **能力** | `BeforePowerAmountChanged` / `AfterPowerAmountChanged` | 能力层数变化 |
| | `ShouldPowerBeRemovedOnDeath` | 死亡后能力是否移除 |
| **宝珠** | `AfterOrbChanneled` / `AfterOrbEvoked` | 宝珠生命周期 |
| | `ModifyOrbValue` / `ModifyOrbPassiveTriggerCount` | 宝珠数值 |
| **药水** | `BeforePotionUsed` / `AfterPotionUsed` | 使用药水 |
| | `AfterPotionDiscarded` / `AfterPotionProcured` | 药水管理 |
| **奖励** | `BeforeRewardsOffered` / `AfterRewardTaken` | 奖励选择 |
| | `ModifyRewards` | 修改奖励列表 |
| | `ModifyCardRewardOptions` | 卡牌奖励选项 |
| | `ModifyCardRewardCreationOptions` | 卡牌奖励生成 |
| | `ModifyCardRewardUpgradeOdds` | 卡牌升级概率 |
| | `ModifyCardRewardAlternatives` | 卡牌备选项 |
| | `AfterStarsGained` / `AfterStarsSpent` | 星币货币 |
| **地图** | `AfterMapGenerated` | 地图生成 |
| | `ModifyGeneratedMap` / `ModifyGeneratedMapLate` | 修改地图 |
| | `ModifyUnknownMapPointRoomTypes` | 修改地图房间类型 |
| | `ShouldProceedToNextMapPoint` | 地图推进 |
| **房间** | `BeforeRoomEntered` / `AfterRoomEntered` | 房间进入 |
| | `AfterActEntered` | 幕进入 |
| | `AfterItemPurchased` | 商店购买 |
| | `AfterRestSiteHeal` / `AfterRestSiteSmith` | 休息点 |
| | `ModifyRestSiteOptions` / `ModifyRestSiteHealRewards` | 休息点选项 |
| **生命值** | `AfterCurrentHpChanged` | 生命值变化 |
| | `ModifyHealAmount` / `ModifyHpLostBeforeOsty` / `ModifyHpLostAfterOsty` | 生命值修改 |
| | `AfterOstyRevived` | Osty 复活 |
| **经济** | `AfterGoldGained` / `ShouldGainGold` | 金币 |
| | `ModifyMerchantPrice` / `ModifyMerchantCardPool` | 商店 |
| | `ShouldAllowMerchantCardRemoval` / `ShouldRefillMerchantEntry` | 商店逻辑 |
| **事件** | `ModifyNextEvent` / `ShouldAllowAncient` | 事件选择 |
| **锻造** | `AfterForge` | 锻造机制 |
| **召唤** | `AfterSummon` / `ModifySummonAmount` | 召唤机制 |

### 数值修改 Hook（有返回值，使用 `ref __result`）

| Hook | 返回值 | 说明 |
|------|--------|------|
| `ModifyDamage` | `decimal` | 修改造成的伤害 |
| `ModifyBlock` | `decimal` | 修改获得的格挡 |
| `ModifyEnergyCostInCombat` | `decimal` | 修改卡牌能量费用 |
| `ModifyMaxEnergy` | `decimal` | 修改每回合最大能量 |
| `ModifyStarCost` | `decimal` | 修改星币费用 |
| `ModifyHandDraw` | `decimal` | 修改初始抽牌数 |
| `ModifyHealAmount` | `decimal` | 修改治疗量 |
| `ModifyAttackHitCount` | `decimal` | 修改多次攻击次数 |
| `ModifyCardPlayCount` | `int` | 修改卡牌可使用次数 |
| `ModifyOrbPassiveTriggerCount` | `int` | 修改宝珠被动触发次数 |
| `ModifyXValue` | `int` | 修改卡牌 X 值 |
| `ModifyRestSiteHealAmount` | `decimal` | 修改休息点治疗量 |
| `ModifyCardRewardUpgradeOdds` | `decimal` | 修改卡牌升级概率 |

### 布尔门控 Hook（返回 bool，允许或拒绝）

| Hook | 说明 |
|------|------|
| `ShouldDie` | 允许或阻止死亡 |
| `ShouldPlay` | 允许或阻止出牌 |
| `ShouldFlush` | 允许或阻止清空手牌 |
| `ShouldDraw` | 允许或阻止抽牌 |
| `ShouldClearBlock` | 允许或阻止清除格挡 |
| `ShouldAllowHitting` | 允许或阻止攻击命中 |
| `ShouldAllowTargeting` | 允许或阻止选择目标 |
| `ShouldStopCombatFromEnding` | 阻止战斗结束 |
| `ShouldGainGold` | 允许或阻止获得金币 |
| `ShouldGainStars` | 允许或阻止获得星币 |
| `ShouldAfflict` | 允许或阻止施加负面状态 |
| `ShouldAddToDeck` | 允许或阻止将卡牌加入牌组 |
| `ShouldEtherealTrigger` | 控制虚无行为 |
| `ShouldPlayerResetEnergy` | 控制能量重置 |
| `ShouldPayExcessEnergyCostWithStars` | 超额能量用星币支付 |
| `ShouldForcePotionReward` | 强制药水奖励 |
| `ShouldAllowAncient` | 允许远古事件 |
| `ShouldGenerateTreasure` | 允许生成宝藏 |
| `ShouldProcurePotion` | 允许获取药水 |
| `ShouldAllowSelectingMoreCardRewards` | 允许选择更多卡牌奖励 |
| `ShouldAllowMerchantCardRemoval` | 允许商店移除卡牌 |

## Mod 文件结构

游戏要求 `mods/` 中的目录结构：

```
mods/YourModName/
├── YourModName.dll        # 编译后的程序集
├── YourModName.pck        # Godot 资源
├── YourModName.json       # Mod 清单 (id, has_pck, has_dll)
└── mod_manifest.json      # Godot 清单 (pck_name)
```

**关键**：`YourModName.json` 中的 `id` 必须与文件夹名、DLL 名、PCK 名一致。

## 脚本参考

| 脚本 | 用途 |
|------|------|
| `install.bat` | 完整环境安装（仅首次运行） |
| `install-mod.bat` | 重新构建并安装 Mod（日常使用） |
| `uninstall-mod.bat` | 从游戏卸载 Mod 和 STS2MCP |
| `tools/launch_sts2.ps1` | 通过 Steam 启动游戏 |
| `tools/close_sts2.ps1` | 强制关闭游戏 |
| `tools/wait_sts2.ps1` | 等待游戏进程 |
| `tools/read_sts2_logs.ps1` | 查看游戏日志 |
| `tools/rename-scaffold.ps1` | 将脚手架重命名为你的 Mod 名称 |

## 重命名脚手架

开始新 Mod 时，重命名所有内容：

```powershell
.\tools\rename-scaffold.ps1 -NewModName "MyAwesomeMod"
```

这会更新：
- `.csproj` 文件名和命名空间
- `.json` 清单 id 和 pck_name
- `ModEntry.cs` ModId 和 ModName

## 环境要求

- **Windows**（Godot 和游戏仅支持 Windows）
- **Git**（需在 PATH 中）
- **杀戮尖塔 2** Steam 版
- **Steam** 已登录（用于启动游戏和深度验证）

其他所有依赖（.NET、Godot、uv、ILSpy）都会自动安装。

> **注意**：本脚手架仅在 Windows 平台测试过，其他平台（macOS、Linux）未进行测试。

## 日志

```csharp
Logger.Log("[Hook] 你的消息");
```

日志写入：`%APPDATA%\SlayTheSpire2\logs\mod_log.txt`

查看日志：`.\tools\read_sts2_logs.ps1`

## 故障排查

### Mod 未加载

1. 检查 `godot.log` 中的 `ERROR` 和 `WARNING`
2. 确认清单中的 `id` 与文件夹/DLL/PCK 名称一致
3. 确认 `has_dll: true` 和 `has_pck: true` 已设置
4. 首次安装 Mod 可能显示 "mods warning" - 在游戏中确认

### Hook 不生效

1. 用 ILSpy MCP 验证签名 - **永远不要猜测**
2. 检查 `[HarmonyPatch]` 属性是否正确
3. 通过 `Logger.Log()` 输出确认 Hook 被触发

### 编译失败

1. 先运行 `install.bat`
2. 确认 `references/` 包含 `sts2.dll`、`0Harmony.dll`、`GodotSharp.dll`
3. 检查 `.csproj` 中的 `GameDir` 指向正确的游戏目录

## 文档

- **`rules.md`** - 开发规范和约束（必读）
- **`AGENTS.md`** - AI Agent 工作流指南
- **`docs/plans/`** - 实现计划目录

## 许可证

MIT
