# AGENTS.md - AI Agent 规范

本文件是 AI Agent（如 OpenCode）在此脚手架项目中的工作规范。

**在开始开发之前，必须先阅读 [`rules.md`](rules.md)。**

## 项目概览

这是一个 **Slay the Spire 2 Mod 脚手架**，使用 C# + Harmony 进行 Mod 开发。

- **开发规范**: `rules.md` — 必读，约束所有开发行为
- **入口文件**: `src/ModEntry.cs` — 禁止修改
- **Hook 文件**: `src/Hooks/` 目录 — 按 功能模块拆分文件
- **清单文件**: `src/com.vibecoding.sts2mod.json` — Mod 元数据（id、名称、版本等）
- **项目文件**: `src/Sts2ModScaffold.csproj` — .NET 10 类库

## 执行流程

### 0. 初始化 Mod 信息（首次必须执行）

**每个新 Mod 的第一步，必须先修改以下文件中的 Mod 身份信息：**

1. `src/com.vibecoding.sts2mod.json` — 修改 `id`（必须与文件夹名/DLL名一致）、`name`、`author`、`description`、`pck_name`
2. `src/ModEntry.cs` — 修改 `ModId`（与 manifest 的 `id` 一致）和 `ModName` 常量
3. 运行 `tools/rename-scaffold.ps1 -NewModName "你的Mod名"` 自动重命名 csproj、namespace、manifest 文件

**规则：**
- `id` 必须与文件夹名、DLL文件名、PCK文件名一致（详见 `rules.md`）
- `ModEntry.cs` 中的 `ModId`（Harmony 补丁 ID）必须与 manifest 的 `id` 一致
- 如果不执行此步骤，Mod 将以 "Sts2ModScaffold" 身份加载，可能与其他 Mod 冲突

### 1. 理解需求

当用户描述一个 Mod 需求时：
1. 使用 **ILSpy MCP** 反编译 `sts2.dll`，找到目标 Hook 的位置和签名
   - `decompile_type`: 查看类结构
   - `decompile_method`: 查看方法签名
   - **不要猜测签名，必须用 MCP 验证**
2. 查阅 `Modding-Tutorial/` 目录中的示例
3. 在 `docs/plans/` 下创建实现计划

### 2. 制定计划

**必须先在 `docs/plans/` 下创建计划文件，再编写代码。**

文件命名：`docs/plans/[功能名].md`

```markdown
# [功能名] 实现计划

## 目标
用户想要...

## 需要的 Hooks
1. Hook 名称 — 用途说明

## 实现步骤
1. 步骤描述

## 验证方式
- 如何测试这个功能
```

### 3. 编写代码

1. 编辑 `src/Hooks/` 下的 Hook 文件
2. 代码必须与计划一一对应
3. 使用 `Logger.Log()` 记录关键操作

### 4. 代码 Review

编写完代码后，逐条对照计划检查：
- [ ] 每个计划中的 Hook 都已实现
- [ ] 方法签名与 ILSpy MCP 反编译结果一致
- [ ] 代码逻辑与计划一致

### 5. 编译与安装

```powershell
install-mod.bat
```

### 6. 启动游戏并自动测试

**Agent 必须自行完成全部测试流程，使用脚本和 STS2MCP 操作游戏。**

**前置条件：Steam 必须已启动并登录，否则游戏无法启动。启动前先检查 Steam 状态。**

#### 6.1 关闭已运行的游戏

```bash
powershell -ExecutionPolicy Bypass -File "tools\close_sts2.ps1"
```

#### 6.2 清除旧日志

```bash
powershell -Command "Remove-Item \"$env:APPDATA\SlayTheSpire2\logs\mod_log.txt\" -Force -ErrorAction SilentlyContinue; Remove-Item \"$env:APPDATA\SlayTheSpire2\logs\godot.log\" -Force -ErrorAction SilentlyContinue"
```

#### 6.3 启动游戏

**必须使用脚本启动游戏：**

```bash
powershell -ExecutionPolicy Bypass -File "tools\launch_sts2.ps1"
```

#### 6.4 等待游戏启动

```bash
powershell -ExecutionPolicy Bypass -File "tools\wait_sts2.ps1"
```

#### 6.5 使用 STS2MCP 操作游戏

等待 MCP Server 连接后，使用 STS2MCP 工具（HTTP API on localhost:15526）：

1. **获取游戏状态**：调用 `get_game_state()` 确认游戏已加载
2. **进入战斗并出牌**：调用 `combat_play_card(card_index, target?)` 打出一张牌触发 Hook
3. 根据测试需要重复操作（出多张牌、结束战斗等）

#### 6.6 关闭游戏

```bash
powershell -ExecutionPolicy Bypass -File "tools\close_sts2.ps1"
```

等待进程完全退出：

```bash
powershell -Command "for ($i=1; $i -le 10; $i++) { Start-Sleep -Seconds 1; if (-not (Get-Process -Name 'SlayTheSpire2' -ErrorAction SilentlyContinue)) { Write-Host 'Game closed'; break } }"
```

### 7. 检查日志确认 Mod 加载成功

```bash
powershell -ExecutionPolicy Bypass -File "tools\read_sts2_logs.ps1"
```

需要确认两点：
1. **godot.log** 中有 `Finished mod initialization for` — Mod 被加载
2. **mod_log.txt** 中有 `[Hook]` 开头的日志 — Hook 被触发

## MCP 工具使用

### ILSpy MCP — 代码反编译

用于理解游戏内部实现和验证 Hook 签名。**不要猜测签名，必须用 MCP 验证。**

所有工具都需要传 `assemblyPath` 参数，指向 `references/sts2.dll`。

- `decompile_type(assembly_path, type_name)` — 查看类结构
- `decompile_method(assembly_path, type_name, method_name)` — 查看具体方法签名
- `get_type_members(assembly_path, type_name)` — 快速查看成员列表
- `list_assembly_types(assembly_path)` — 列出程序集中的所有类型

### STS2MCP — 游戏控制

用于自动化测试（需要游戏运行中）。**Agent 必须使用此 MCP 操作游戏进行测试，不要让用户手动操作。**

- `get_game_state(format?)` — 获取当前游戏状态
- `combat_play_card(card_index, target?)` — 出牌
- `combat_end_turn()` — 结束回合
- `rewards_claim(reward_index)` — 领取奖励
- `rewards_pick_card(card_index)` / `rewards_skip_card()` — 选择/跳过卡牌奖励
- `map_choose_node(node_index)` — 选择地图节点
- `rest_choose_option(option_index)` — 休息站选项
- `shop_purchase(item_index)` — 商店购买
- `event_choose_option(option_index)` — 事件选项
- `deck_select_card(card_index)` / `deck_confirm_selection()` — 卡牌选择确认
- `proceed_to_map()` — 返回地图

## 文件规则

- **只修改** `src/Hooks/` 目录（和新建的计划文件）
- **首次使用时修改** `src/com.vibecoding.sts2mod.json`、`src/ModEntry.cs` 中的 Mod 身份信息
- **禁止修改** `src/ModEntry.cs` 的代码逻辑（Initialize 方法和 Logger）
- **禁止修改** `src/Sts2ModScaffold.csproj`
- **Mod 清单** 在 `src/*.json`
- **项目代码** 全部在 `src/` 目录下

## 编译与安装

### 首次安装（环境配置）

```powershell
.\install.bat
```

自动安装 .NET 10 SDK、.NET 8 运行时、Godot、ILSpy、STS2MCP，配置 MCP 服务器。只需运行一次。

### 日常开发

```powershell
install-mod.bat
```

重新构建 Mod DLL/PCK 并安装到游戏。可加参数：`install-mod.bat "游戏路径"`

### 辅助脚本

| 脚本 | 用途 |
|------|------|
| `tools/rename-scaffold.ps1` | 重命名脚手架（自动修改 csproj、namespace、manifest） |
| `tools/launch_sts2.ps1` | 启动游戏 |
| `tools/close_sts2.ps1` | 关闭游戏 |
| `tools/wait_sts2.ps1` | 等待游戏启动（轮询检测） |
| `tools/read_sts2_logs.ps1` | 查看日志 |
| `uninstall-mod.bat` | 从游戏目录卸载 mod 和 STS2MCP |

## Mod 文件结构

游戏加载 Mod 时的结构要求见 [`rules.md`](rules.md) 规则4。

## 错误排查

### Mod 未加载
- 检查 `godot.log` 中的 `ERROR` 和 `WARNING`
- 确认 `你的Mod名.json` 中 `id` 字段与文件夹名、DLL文件名一致
- 确认 `has_dll: true` 和 `has_pck: true` 已设置
- 确认 `mod_manifest.json` 包含 `pck_name` 字段
- 确认 DLL 和 PCK 文件都在游戏 `mods/` 目录的子文件夹下
- 注意：首次启动新 Mod 时游戏可能提示 "mods warning"，需在游戏中确认后才加载

### Hook 不生效
- 用 ILSpy MCP 验证方法签名是否匹配
- 检查 `[HarmonyPatch]` 属性是否正确
- 检查 `Logger.Log()` 是否在 `mod_log.txt` 中出现（确认 Hook 被调用）

### 编译失败
- 确认已运行过 `install.bat`（自动安装 .NET 10 SDK）
- 检查 `references/` 下是否有 `sts2.dll`、`0Harmony.dll`、`GodotSharp.dll`
- 检查 csproj 中的 `GameDir` 路径是否指向正确的游戏目录
