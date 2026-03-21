using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline.UnlockScreens;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace STS2MenuControl;

internal static class MenuStateService
{
    public static Dictionary<string, object> BuildState()
    {
        var currentScreen = ActiveScreenContext.Instance?.GetCurrentScreen();
        var screen = ResolveScreen(currentScreen);
        var actions = GetAvailableActions(currentScreen, screen);
        var result = new Dictionary<string, object>
        {
            ["status"] = "ok",
            ["screen"] = screen,
            ["available_actions"] = actions
        };

        if (screen == "CHARACTER_SELECT")
            result["characters"] = GetCharacters(currentScreen);

        if (screen == "TIMELINE")
            result["epochs"] = GetEpochs(currentScreen);

        return result;
    }

    public static string ResolveScreen(IScreenContext? currentScreen)
    {
        if (NModalContainer.Instance?.OpenModal != null)
            return "MODAL";

        if (currentScreen == null) return "UNKNOWN";

        if (currentScreen is NCharacterSelectScreen) return "CHARACTER_SELECT";
        if (currentScreen is NTimelineScreen) return "TIMELINE";
        if (currentScreen is NGameOverScreen) return "GAME_OVER";
        if (currentScreen is NMainMenu || currentScreen is NSubmenu) return "MAIN_MENU";
        return "IN_GAME";
    }

    private static List<string> GetAvailableActions(IScreenContext? currentScreen, string screen)
    {
        var actions = new List<string>();

        if (screen == "MAIN_MENU")
        {
            if (CanOpenCharacterSelect(currentScreen)) actions.Add("open_character_select");
            if (CanContinueRun(currentScreen)) actions.Add("continue_run");
            if (CanAbandonRun(currentScreen)) actions.Add("abandon_run");
            if (CanOpenTimeline(currentScreen)) actions.Add("open_timeline");
        }
        else if (screen == "CHARACTER_SELECT")
        {
            if (CanSelectCharacter(currentScreen)) actions.Add("select_character");
            if (CanEmbark(currentScreen)) actions.Add("embark");
            actions.Add("close_main_menu_submenu");
        }
        else if (screen == "TIMELINE")
        {
            if (CanChooseTimelineEpoch(currentScreen)) actions.Add("choose_timeline_epoch");
            if (CanConfirmTimelineOverlay(currentScreen)) actions.Add("confirm_timeline_overlay");
            actions.Add("close_main_menu_submenu");
        }
        else if (screen == "GAME_OVER")
        {
            actions.Add("return_to_main_menu");
        }
        else if (screen == "MODAL")
        {
            actions.Add("confirm_modal");
            actions.Add("dismiss_modal");
        }

        return actions;
    }

    private static bool IsNodeReady(Node? node) => node != null && node.IsInsideTree();

    private static bool IsButtonReady(Node? node)
    {
        if (node == null || !node.IsInsideTree()) return false;
        if (node is BaseButton bb) return !bb.Disabled;
        return true;
    }

    public static bool CanOpenCharacterSelect(IScreenContext? currentScreen)
    {
        if (currentScreen is not NMainMenu mainMenu || !mainMenu.IsInsideTree())
            return false;
        if (mainMenu.SubmenuStack?.SubmenusOpen == true)
            return false;

        var btn = mainMenu.GetNodeOrNull("MainMenuTextButtons/SingleplayerButton");
        if (IsButtonReady(btn))
            return true;

        return !CanContinueRun(currentScreen) && !CanAbandonRun(currentScreen);
    }

    public static bool CanContinueRun(IScreenContext? currentScreen)
    {
        if (currentScreen is not NMainMenu mainMenu || !mainMenu.IsInsideTree())
            return false;
        if (mainMenu.SubmenuStack?.SubmenusOpen == true)
            return false;

        var btn = mainMenu.GetNodeOrNull("MainMenuTextButtons/ContinueButton");
        return IsButtonReady(btn);
    }

    public static bool CanAbandonRun(IScreenContext? currentScreen)
    {
        if (currentScreen is not NMainMenu mainMenu || !mainMenu.IsInsideTree())
            return false;
        if (mainMenu.SubmenuStack?.SubmenusOpen == true)
            return false;

        var btn = mainMenu.GetNodeOrNull("MainMenuTextButtons/AbandonRunButton");
        return IsButtonReady(btn);
    }

    public static bool CanOpenTimeline(IScreenContext? currentScreen)
    {
        if (currentScreen is not NMainMenu mainMenu || !mainMenu.IsInsideTree())
            return false;
        if (mainMenu.SubmenuStack?.SubmenusOpen == true)
            return false;

        var btn = mainMenu.GetNodeOrNull("MainMenuTextButtons/TimelineButton");
        return IsButtonReady(btn);
    }

    public static bool CanSelectCharacter(IScreenContext? currentScreen)
    {
        if (currentScreen is not NCharacterSelectScreen)
            return false;
        return GetCharacterButtons(currentScreen).Any(b => !b.IsLocked);
    }

    public static bool CanEmbark(IScreenContext? currentScreen)
    {
        if (currentScreen is not NCharacterSelectScreen screen)
            return false;
        var btn = screen.GetNodeOrNull("ConfirmButton");
        return IsButtonReady(btn);
    }

    public static bool CanChooseTimelineEpoch(IScreenContext? currentScreen)
    {
        if (currentScreen is not NTimelineScreen)
            return false;
        return GetEpochSlots(currentScreen).Count > 0;
    }

    public static bool CanConfirmTimelineOverlay(IScreenContext? currentScreen)
    {
        if (currentScreen is not NTimelineScreen)
            return false;

        var unlockBtn = GetTimelineUnlockConfirmButton(currentScreen);
        if (IsButtonReady(unlockBtn))
            return true;

        var closeBtn = GetTimelineInspectCloseButton(currentScreen);
        return IsButtonReady(closeBtn);
    }

    internal static List<Dictionary<string, object>> GetCharacters(IScreenContext? currentScreen)
    {
        var result = new List<Dictionary<string, object>>();
        int index = 0;
        foreach (var btn in GetCharacterButtons(currentScreen))
        {
            result.Add(new Dictionary<string, object>
            {
                ["index"] = index++,
                ["character_id"] = btn.Character?.Id?.Entry ?? "unknown",
                ["locked"] = btn.IsLocked,
                ["enabled"] = btn.IsInsideTree() && !btn.IsLocked
            });
        }
        return result;
    }

    internal static List<Dictionary<string, object>> GetEpochs(IScreenContext? currentScreen)
    {
        var result = new List<Dictionary<string, object>>();
        int index = 0;
        foreach (var slot in GetEpochSlots(currentScreen))
        {
            result.Add(new Dictionary<string, object>
            {
                ["index"] = index++,
                ["state"] = slot.State.ToString()
            });
        }
        return result;
    }

    internal static IReadOnlyList<NCharacterSelectButton> GetCharacterButtons(IScreenContext? currentScreen)
    {
        if (currentScreen is not NCharacterSelectScreen screen)
            return Array.Empty<NCharacterSelectButton>();

        return FindDescendants<NCharacterSelectButton>(screen)
            .Where(GodotObject.IsInstanceValid)
            .OrderBy(n => n.GlobalPosition.Y).ThenBy(n => n.GlobalPosition.X)
            .ToArray();
    }

    internal static IReadOnlyList<NEpochSlot> GetEpochSlots(IScreenContext? currentScreen)
    {
        if (currentScreen is not NTimelineScreen ts || !ts.IsVisibleInTree())
            return Array.Empty<NEpochSlot>();

        return FindDescendants<NEpochSlot>(ts)
            .Where(s => s.IsVisibleInTree() && s.model != null && s.State != EpochSlotState.NotObtained)
            .OrderBy(s => s.GlobalPosition.X).ThenBy(s => s.GlobalPosition.Y)
            .ToArray();
    }

    internal static Button? GetTimelineUnlockConfirmButton(IScreenContext? currentScreen)
    {
        if (currentScreen is not NTimelineScreen ts)
            return null;
        var unlock = FindDescendants<NUnlockScreen>(ts).FirstOrDefault(s => s.IsVisibleInTree());
        return unlock?.GetNodeOrNull<Button>("ConfirmButton");
    }

    internal static Button? GetTimelineInspectCloseButton(IScreenContext? currentScreen)
    {
        if (currentScreen is not NTimelineScreen ts)
            return null;
        var inspect = ts.GetNodeOrNull<NEpochInspectScreen>("%EpochInspectScreen");
        if (inspect?.Visible != true) return null;
        return inspect.GetNodeOrNull<Button>("%CloseButton");
    }

    internal static void ClickNode(Node node)
    {
        node.Call("ForceClick");
    }

    private static List<T> FindDescendants<T>(Node root) where T : Node
    {
        var results = new List<T>();
        var stack = new Stack<Node>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (node is T typed) results.Add(typed);
            foreach (var child in node.GetChildren()) stack.Push(child);
        }
        return results;
    }
}
