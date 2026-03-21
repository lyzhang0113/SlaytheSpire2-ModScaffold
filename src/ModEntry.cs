using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Sts2ModScaffold;

[ModInitializer("Initialize")]
public static class ModEntry
{
    public static readonly string ModId = "Sts2ModScaffold";
    public static readonly string ModName = "Sts2ModScaffold";

    public static void Initialize()
    {
        var harmony = new Harmony(ModId);
        harmony.PatchAll();
        Logger.Log($"[{ModName}] Mod loaded! Hooks active.");
    }
}

public static class Logger
{
    private static readonly string LogPath;

    static Logger()
    {
        LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SlayTheSpire2", "logs", "mod_log.txt"
        );
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
    }

    public static void Log(string message)
    {
        File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }
}
