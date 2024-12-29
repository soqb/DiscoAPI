using System;
using System.Collections.Generic;
using Voidforge;

namespace DiscoAPI.Runtime;

public static class DiscoRunner
{
    public static ModWorld? world;

    public static DiscoManager manager = new();
    private static List<DiscoPlugin> plugins = new();
    public static void Register(DiscoPlugin plugin)
    {
        DiscoAPIPlugin.Instance.Log.LogInfo($"registered plugin \"{plugin.Guid}\"");
        var source = manager.CreateSource(plugin);
        plugins.Add(plugin);
        GuardHook(plugin, plugin.OnRegister);
    }

    private static bool alreadyLoadedDialogue = false;
    public static void OnDialogueBundleLoad()
    {
        DiscoAPIPlugin.Instance.Log.LogInfo("loading dialogue bundle..");

        if (alreadyLoadedDialogue) return;
        alreadyLoadedDialogue = true;

        manager.OnDialogueBundleLoad();

        foreach (var plugin in plugins)
        {
            plugin.Log.LogInfo("initializing dialogue..");
            GuardHook(plugin, plugin.OnDialogueBundleLoad);
        }

        // Several vanilla methods use initialDatabase, but they should really be using the master database.
        DialogueBridgePixelCrushers.DialogueSystem.initialDatabase = manager.Dialogue.pcDatabase;
    }

    public static void OnSceneLoad()
    {
        DiscoAPIPlugin.Instance.Log.LogInfo("scene loaded..");

        var w = SingletonComponent<global::World>.Singleton;
        if (world == null && w != null) world = new(w);

        foreach (var plugin in plugins)
        {
            GuardHook(plugin, plugin.OnSceneLoad);
        }
    }

    public static void GuardHook(DiscoPlugin plugin, Action hook)
    {
        try
        {
            hook();
        }
        catch (Exception e)
        {
            plugin.Log.LogError(e);
        }
    }
}
