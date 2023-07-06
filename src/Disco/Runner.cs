using System.Collections.Generic;

namespace DiscoAPI;

public class DiscoRunner
{
    public static DiscoManager manager = new();
    private static List<DiscoProvider> plugins = new();
    public static void Register(DiscoProvider plugin)
    {
        DiscoAPIPlugin.Instance.Log.LogInfo($"registered plugin \"{plugin.Guid}\"");
        plugins.Add(plugin);
    }

    static bool alreadyLoadedDialogue = false;
    public static void OnDialogueBundleLoad()
    {
        if (alreadyLoadedDialogue) return;
        alreadyLoadedDialogue = true;

        manager.EnsureInitialized();
        foreach (var plugin in plugins)
        {
            var source = manager.MakeSource(plugin.Guid);
            plugin.OnDialogueBundleLoad(source);
        }

        // Several vanilla methods use initialDatabase, but they should really be using the master database.
        DialogueBridgePixelCrushers.DialogueSystem.initialDatabase = manager.dialogue.pcDatabase;
    }

    public static void OnSceneLoad()
    {
        manager.EnsureInitialized();
        foreach (var plugin in plugins)
        {
            var source = manager.MakeSource(plugin.Guid);
            plugin.OnSceneLoad(source);
        }
    }
}