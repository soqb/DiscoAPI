using System.Collections.Generic;

namespace DiscoAPI.Runtime;

public class DiscoRunner
{
    public static DiscoManager manager = new();
    private static List<DiscoProvider> plugins = new();
    public static void Register(DiscoProvider plugin)
    {
        DiscoAPIPlugin.Instance.Log.LogInfo($"registered plugin \"{plugin.Guid}\"");
        plugins.Add(plugin);
    }

    private static bool alreadyLoadedDialogue = false;
    public static void OnDialogueBundleLoad()
    {
        DiscoAPIPlugin.Instance.Log.LogInfo("loading dialogue bundle..");

        if (alreadyLoadedDialogue) return;
        alreadyLoadedDialogue = true;

        manager.EnsureInitialized();
        // try
        // {
        //     BundleLoader.LoadBundle(Path.Join(Application.dataPath, "..", "BepInEx", "discoPlugins", "mod"));
        // }
        // finally
        // {
        //     Application.Quit(0);
        // }
        // Dumps.DiscoDumper.FullDump(manager.Disco);
        foreach (var plugin in plugins)
        {
            var source = manager.CreateSource(plugin.Guid);
            source.LogInfo("initializing dialogue..");
            plugin.OnDialogueBundleLoad(source);
        }

        // Several vanilla methods use initialDatabase, but they should really be using the master database.
        DialogueBridgePixelCrushers.DialogueSystem.initialDatabase = manager.Dialogue.pcDatabase;
    }

    public static void OnSceneLoad()
    {
        manager.EnsureInitialized();
        foreach (var plugin in plugins)
        {
            var source = manager.CreateSource(plugin.Guid);
            plugin.OnSceneLoad(source);
        }
    }
}
