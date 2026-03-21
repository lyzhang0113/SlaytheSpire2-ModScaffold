using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Saves;

namespace STS2MenuControl;

internal static class MenuActionService
{
    private static string? _lastHostError;
    private static bool _hostInProgress;
    private static bool _hostCompleted;
    private static NetHostGameService? _pendingHostService;

    public static Dictionary<string, object> Execute(string action, int? optionIndex, Dictionary<string, JsonElement>? data = null)
    {
        return action.ToLowerInvariant() switch
        {
            "open_character_select" => OpenCharacterSelect(),
            "open_multiplayer_host" => OpenMultiplayerHost(data),
            "select_character" => SelectCharacter(optionIndex),
            "embark" => Embark(),
            "set_ready" => SetReady(true),
            "set_unready" => SetReady(false),
            "get_lobby_status" => GetLobbyStatus(),
            "continue_run" => ContinueRun(),
            "abandon_run" => AbandonRun(),
            "open_timeline" => OpenTimeline(),
            "choose_timeline_epoch" => ChooseTimelineEpoch(optionIndex),
            "confirm_timeline_overlay" => ConfirmTimelineOverlay(),
            "close_main_menu_submenu" => CloseMainMenuSubmenu(),
            "return_to_main_menu" => ReturnToMainMenu(),
            "confirm_modal" => ConfirmModal(),
            "dismiss_modal" => DismissModal(),
            _ => throw new MenuActionException(409, $"Unknown action: {action}",
                new() { ["action"] = action })
        };
    }

    private static Dictionary<string, object> Success(string action)
    {
        var state = MenuStateService.BuildState();
        state["action"] = action;
        state["message"] = "Action completed.";
        return state;
    }

    private static Dictionary<string, object> OpenCharacterSelect()
    {
        var screen = ActiveScreenContext.Instance?.GetCurrentScreen();
        if (screen is not NMainMenu mainMenu || !MenuStateService.CanOpenCharacterSelect(screen))
            throw new MenuActionException(409, "Cannot open character select",
                new() { ["action"] = "open_character_select", ["screen"] = MenuStateService.ResolveScreen(screen) });

        var charSelect = mainMenu.SubmenuStack.GetSubmenuType<NCharacterSelectScreen>();
        charSelect.InitializeSingleplayer();
        mainMenu.SubmenuStack.Push(charSelect);
        return Success("open_character_select");
    }

    private static Dictionary<string, object> SelectCharacter(int? optionIndex)
    {
        if (!optionIndex.HasValue)
            throw new MenuActionException(400, "select_character requires option_index",
                new() { ["action"] = "select_character" });

        var screen = ActiveScreenContext.Instance?.GetCurrentScreen();
        if (screen is not NCharacterSelectScreen)
            throw new MenuActionException(409, "Not on character select screen",
                new() { ["action"] = "select_character", ["screen"] = MenuStateService.ResolveScreen(screen) });

        var buttons = MenuStateService.GetCharacterButtons(screen);
        if (optionIndex < 0 || optionIndex >= buttons.Count)
            throw new MenuActionException(409, $"option_index out of range (0-{buttons.Count - 1})",
                new() { ["action"] = "select_character", ["option_index"] = optionIndex });

        var button = buttons[optionIndex.Value];
        if (button.IsLocked)
            throw new MenuActionException(409, "Character is locked",
                new() { ["action"] = "select_character", ["option_index"] = optionIndex });

        button.Select();
        return Success("select_character");
    }

    private static Dictionary<string, object> Embark()
    {
        var screen = ActiveScreenContext.Instance?.GetCurrentScreen();
        if (screen is not NCharacterSelectScreen charScreen || !MenuStateService.CanEmbark(screen))
            throw new MenuActionException(409, "Cannot embark",
                new() { ["action"] = "embark", ["screen"] = MenuStateService.ResolveScreen(screen) });

        var btn = charScreen.GetNodeOrNull("ConfirmButton");
        if (btn != null)
        {
            charScreen.Call("OnEmbarkPressed", btn);
        }
        return Success("embark");
    }

    private static Dictionary<string, object> ContinueRun()
    {
        var screen = ActiveScreenContext.Instance?.GetCurrentScreen();
        if (screen is not NMainMenu mainMenu || !MenuStateService.CanContinueRun(screen))
            throw new MenuActionException(409, "Cannot continue run",
                new() { ["action"] = "continue_run", ["screen"] = MenuStateService.ResolveScreen(screen) });

        var btn = mainMenu.GetNodeOrNull("MainMenuTextButtons/ContinueButton");
        if (btn != null) mainMenu.Call("OnContinueButtonPressed", btn);
        return Success("continue_run");
    }

    private static Dictionary<string, object> AbandonRun()
    {
        var screen = ActiveScreenContext.Instance?.GetCurrentScreen();
        if (screen is not NMainMenu mainMenu || !MenuStateService.CanAbandonRun(screen))
            throw new MenuActionException(409, "Cannot abandon run",
                new() { ["action"] = "abandon_run", ["screen"] = MenuStateService.ResolveScreen(screen) });

        var btn = mainMenu.GetNodeOrNull("MainMenuTextButtons/AbandonRunButton");
        if (btn != null) mainMenu.Call("OnAbandonRunButtonPressed", btn);
        return Success("abandon_run");
    }

    private static Dictionary<string, object> OpenTimeline()
    {
        var screen = ActiveScreenContext.Instance?.GetCurrentScreen();
        if (screen is not NMainMenu mainMenu || !MenuStateService.CanOpenTimeline(screen))
            throw new MenuActionException(409, "Cannot open timeline",
                new() { ["action"] = "open_timeline", ["screen"] = MenuStateService.ResolveScreen(screen) });

        mainMenu.SubmenuStack.PushSubmenuType<NTimelineScreen>();
        return Success("open_timeline");
    }

    private static Dictionary<string, object> ChooseTimelineEpoch(int? optionIndex)
    {
        if (!optionIndex.HasValue)
            throw new MenuActionException(400, "choose_timeline_epoch requires option_index",
                new() { ["action"] = "choose_timeline_epoch" });

        var screen = ActiveScreenContext.Instance?.GetCurrentScreen();
        if (screen is not NTimelineScreen || !MenuStateService.CanChooseTimelineEpoch(screen))
            throw new MenuActionException(409, "Cannot choose timeline epoch",
                new() { ["action"] = "choose_timeline_epoch" });

        var slots = MenuStateService.GetEpochSlots(screen);
        if (optionIndex < 0 || optionIndex >= slots.Count)
            throw new MenuActionException(409, $"option_index out of range (0-{slots.Count - 1})",
                new() { ["action"] = "choose_timeline_epoch", ["option_index"] = optionIndex });

        MenuStateService.ClickNode(slots[optionIndex.Value]);
        return Success("choose_timeline_epoch");
    }

    private static Dictionary<string, object> ConfirmTimelineOverlay()
    {
        var screen = ActiveScreenContext.Instance?.GetCurrentScreen();
        if (screen is not NTimelineScreen || !MenuStateService.CanConfirmTimelineOverlay(screen))
            throw new MenuActionException(409, "Cannot confirm timeline overlay",
                new() { ["action"] = "confirm_timeline_overlay" });

        var unlockBtn = MenuStateService.GetTimelineUnlockConfirmButton(screen);
        if (unlockBtn != null) { MenuStateService.ClickNode(unlockBtn); return Success("confirm_timeline_overlay"); }

        var closeBtn = MenuStateService.GetTimelineInspectCloseButton(screen);
        if (closeBtn != null) { MenuStateService.ClickNode(closeBtn); return Success("confirm_timeline_overlay"); }

        throw new MenuActionException(503, "No confirm/close button found",
            new() { ["action"] = "confirm_timeline_overlay" });
    }

    private static Dictionary<string, object> CloseMainMenuSubmenu()
    {
        var screen = ActiveScreenContext.Instance?.GetCurrentScreen();
        if (screen is not NSubmenu)
            throw new MenuActionException(409, "Not on a submenu",
                new() { ["action"] = "close_main_menu_submenu", ["screen"] = MenuStateService.ResolveScreen(screen) });

        var stack = GetSubmenuStack(screen as Node);
        if (stack == null || !stack.SubmenusOpen)
            throw new MenuActionException(409, "No submenu open",
                new() { ["action"] = "close_main_menu_submenu" });

        stack.Pop();
        return Success("close_main_menu_submenu");
    }

    private static Dictionary<string, object> ReturnToMainMenu()
    {
        var screen = ActiveScreenContext.Instance?.GetCurrentScreen();
        if (screen is not NGameOverScreen)
            throw new MenuActionException(409, "Not on game over screen",
                new() { ["action"] = "return_to_main_menu", ["screen"] = MenuStateService.ResolveScreen(screen) });

        var goScreen = (NGameOverScreen)screen;
        goScreen.Call("ReturnToMainMenu");
        return Success("return_to_main_menu");
    }

    private static Dictionary<string, object> ConfirmModal()
    {
        var modal = NModalContainer.Instance?.OpenModal;
        if (modal == null)
            throw new MenuActionException(409, "No modal open",
                new() { ["action"] = "confirm_modal" });

        var modalNode = modal as Node;
        if (modalNode == null)
            throw new MenuActionException(409, "Modal is not a Node",
                new() { ["action"] = "confirm_modal" });

        var yesBtn = modalNode.GetNodeOrNull("VerticalPopup/YesButton")
            ?? modalNode.GetNodeOrNull("%YesButton");

        if (yesBtn != null) { yesBtn.Call("ForceClick"); return Success("confirm_modal"); }

        var confirmBtn = modalNode.GetNodeOrNull("ConfirmButton")
            ?? modalNode.GetNodeOrNull("%ConfirmButton");

        if (confirmBtn != null) { MenuStateService.ClickNode(confirmBtn); return Success("confirm_modal"); }

        throw new MenuActionException(409, "No confirm button on modal",
            new() { ["action"] = "confirm_modal" });
    }

    private static Dictionary<string, object> DismissModal()
    {
        var modal = NModalContainer.Instance?.OpenModal;
        if (modal == null)
            throw new MenuActionException(409, "No modal open",
                new() { ["action"] = "dismiss_modal" });

        var modalNode = modal as Node;
        if (modalNode == null)
            throw new MenuActionException(409, "Modal is not a Node",
                new() { ["action"] = "dismiss_modal" });

        var noBtn = modalNode.GetNodeOrNull("VerticalPopup/NoButton")
            ?? modalNode.GetNodeOrNull("%NoButton");

        if (noBtn != null) { noBtn.Call("ForceClick"); return Success("dismiss_modal"); }

        var cancelBtn = modalNode.GetNodeOrNull("CancelButton")
            ?? modalNode.GetNodeOrNull("%CancelButton");

        if (cancelBtn != null) { MenuStateService.ClickNode(cancelBtn); return Success("dismiss_modal"); }

        NModalContainer.Instance?.Clear();
        return Success("dismiss_modal");
    }

    private static NMainMenuSubmenuStack? GetSubmenuStack(Node? node)
    {
        var current = node;
        while (current != null)
        {
            if (current is NMainMenuSubmenuStack stack) return stack;
            current = current.GetParent();
        }
        return null;
    }

    private static Dictionary<string, object> OpenMultiplayerHost(Dictionary<string, JsonElement>? data)
    {
        var screen = ActiveScreenContext.Instance?.GetCurrentScreen();
        if (screen is not NMainMenu mainMenu || !MenuStateService.CanOpenCharacterSelect(screen))
            throw new MenuActionException(409, "Cannot open multiplayer host from current screen",
                new() { ["action"] = "open_multiplayer_host", ["screen"] = MenuStateService.ResolveScreen(screen) });

        if (_hostInProgress)
            throw new MenuActionException(409, "Host operation already in progress",
                new() { ["action"] = "open_multiplayer_host" });

        var mode = "lan";
        var maxPlayers = 4;
        var port = 33771;

        if (data != null)
        {
            if (data.TryGetValue("mode", out var modeElem)) mode = modeElem.GetString() ?? "lan";
            if (data.TryGetValue("max_players", out var mpElem)) maxPlayers = (int)mpElem.GetDouble();
            if (data.TryGetValue("port", out var portElem)) port = (int)portElem.GetDouble();
        }

        if (maxPlayers < 2) maxPlayers = 2;
        if (maxPlayers > 4) maxPlayers = 4;

        _hostInProgress = true;
        _hostCompleted = false;
        _lastHostError = null;
        _pendingHostService = null;

        MenuControlMod.RunOnMainThreadAsync(() =>
        {
            try
            {
                var hostService = new NetHostGameService();
                _pendingHostService = hostService;

                if (mode == "steam")
                {
                    _ = StartSteamHostAsync(hostService, mainMenu, maxPlayers);
                }
                else
                {
                    var error = hostService.StartENetHost((ushort)port, maxPlayers);
                    if (error != null)
                    {
                        _lastHostError = $"LAN host failed: {error}";
                        GD.PrintErr($"[STS2MenuControl] {_lastHostError}");
                        _hostCompleted = true;
                        _hostInProgress = false;
                        return;
                    }

                    var charSelect = mainMenu.SubmenuStack.GetSubmenuType<NCharacterSelectScreen>();
                    charSelect.InitializeMultiplayerAsHost(hostService, maxPlayers);
                    mainMenu.SubmenuStack.Push(charSelect);
                    _hostCompleted = true;
                    _hostInProgress = false;
                }
            }
            catch (Exception ex)
            {
                _lastHostError = $"Host failed: {ex.Message}";
                GD.PrintErr($"[STS2MenuControl] {_lastHostError}");
                _hostCompleted = true;
                _hostInProgress = false;
            }
        });

        return new Dictionary<string, object>
        {
            ["status"] = "ok",
            ["action"] = "open_multiplayer_host",
            ["message"] = mode == "steam" ? "Starting Steam host (async)..." : "Starting LAN host...",
            ["mode"] = mode,
            ["max_players"] = maxPlayers,
            ["port"] = port
        };
    }

    private static async Task StartSteamHostAsync(NetHostGameService hostService, NMainMenu mainMenu, int maxPlayers)
    {
        var error = await hostService.StartSteamHost(maxPlayers);
        if (error != null)
        {
            _lastHostError = $"Steam host failed: {error}";
            GD.PrintErr($"[STS2MenuControl] {_lastHostError}");
            _hostCompleted = true;
            _hostInProgress = false;
            return;
        }

        MenuControlMod.RunOnMainThreadAsync(() =>
        {
            try
            {
                var charSelect = mainMenu.SubmenuStack.GetSubmenuType<NCharacterSelectScreen>();
                charSelect.InitializeMultiplayerAsHost(hostService, maxPlayers);
                mainMenu.SubmenuStack.Push(charSelect);
            }
            catch (Exception ex)
            {
                _lastHostError = $"Failed to open character select: {ex.Message}";
                GD.PrintErr($"[STS2MenuControl] {_lastHostError}");
            }
            _hostCompleted = true;
            _hostInProgress = false;
        });
    }

    private static Dictionary<string, object> SetReady(bool ready)
    {
        var screen = ActiveScreenContext.Instance?.GetCurrentScreen();
        if (screen is not NCharacterSelectScreen charScreen)
            throw new MenuActionException(409, "Not on character select screen",
                new() { ["action"] = ready ? "set_ready" : "set_unready" });

        var lobby = charScreen.Lobby;
        if (lobby == null)
            throw new MenuActionException(409, "No lobby active",
                new() { ["action"] = ready ? "set_ready" : "set_unready" });

        lobby.SetReady(ready);
        return Success(ready ? "set_ready" : "set_unready");
    }

    private static Dictionary<string, object> GetLobbyStatus()
    {
        var result = new Dictionary<string, object> { ["status"] = "ok" };

        if (_hostInProgress)
        {
            result["host_status"] = "in_progress";
        }
        else if (_hostCompleted && _lastHostError != null)
        {
            result["host_status"] = "failed";
            result["error"] = _lastHostError;
            _lastHostError = null;
            _hostCompleted = false;
        }
        else if (_hostCompleted)
        {
            result["host_status"] = "completed";
            _hostCompleted = false;
        }

        var screen = ActiveScreenContext.Instance?.GetCurrentScreen();
        if (screen is NCharacterSelectScreen charScreen && charScreen.Lobby != null)
        {
            var lobby = charScreen.Lobby;
            var players = new List<Dictionary<string, object>>();
            foreach (var p in lobby.Players)
            {
                players.Add(new Dictionary<string, object>
                {
                    ["id"] = p.id,
                    ["character"] = p.character?.Id?.Entry ?? "none",
                    ["is_ready"] = p.isReady,
                    ["is_local"] = p.id == lobby.LocalPlayer.id
                });
            }
            result["lobby"] = new Dictionary<string, object>
            {
                ["max_players"] = lobby.MaxPlayers,
                ["ascension"] = lobby.Ascension,
                ["game_mode"] = lobby.GameMode.ToString(),
                ["net_type"] = lobby.NetService.Type.ToString(),
                ["is_connected"] = lobby.NetService.IsConnected,
                ["local_player_id"] = lobby.LocalPlayer.id,
                ["players"] = players
            };
        }
        else
        {
            result["lobby"] = (string?)null;
        }

        return result;
    }
}
