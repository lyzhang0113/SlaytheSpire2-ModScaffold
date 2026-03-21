# STS2MenuControl 实现计划

## 目标
创建独立的 Mod，通过 HTTP API 控制游戏主菜单操作（开始新游戏、选择角色、时间线等），弥补 STS2MCP 不支持主菜单操作的不足。

## 架构
- 独立 Mod，端口 8081（STS2MCP 使用 15526）
- 同步 HTTP API（与 STS2MCP 模式一致）
- 不需要 NuGet 依赖

## 文件结构
```
tools/STS2MenuControl/
├── MenuControlMod.cs          # 入口点 + HTTP 服务器
├── MenuStateService.cs        # 菜单状态读取
├── MenuActionService.cs       # 菜单操作执行
├── STS2MenuControl.csproj     # 项目文件
└── mod_manifest.json          # Mod 清单
```

## API 端点
- GET / → 健康检查
- GET /api/v1/menu → 获取菜单状态
- POST /api/v1/menu → 执行菜单操作

## 支持的操作
1. open_character_select → 打开角色选择
2. select_character (option_index) → 选择角色
3. embark → 开始游戏
4. continue_run → 继续存档
5. abandon_run → 放弃存档
6. open_timeline → 打开时间线
7. choose_timeline_epoch (option_index) → 选择时代
8. confirm_timeline_overlay → 确认时间线弹窗
9. close_main_menu_submenu → 关闭子菜单
10. return_to_main_menu → 返回主菜单（从 Game Over）
11. confirm_modal → 确认弹窗
12. dismiss_modal → 关闭弹窗

## 参考来源
- STS2-Agent: GameActionService.cs, GameStateService.cs
- STS2MCP: McpMod.cs (HTTP 服务器模式)
