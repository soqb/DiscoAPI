using System;
using System.Collections.Generic;
using Voidforge;

namespace DiscoAPI.Runtime;

public static class DiscoRunner
{
    public static ModWorld? world;

    public static DiscoManager manager = new();
    private static List<(DiscoProvider, DiscoSource)> plugins = new();
    public static void Register(DiscoProvider plugin)
    {
        DiscoAPIPlugin.Instance.Log.LogInfo($"registered plugin \"{plugin.Guid}\"");
        var source = manager.CreateSource(plugin.Guid);
        plugins.Add((plugin, source));
        GuardHook(source, plugin.OnRegister);
    }

    private static bool alreadyLoadedDialogue = false;
    public static void OnDialogueBundleLoad()
    {
        DiscoAPIPlugin.Instance.Log.LogInfo("loading dialogue bundle..");

        if (alreadyLoadedDialogue) return;
        alreadyLoadedDialogue = true;

        manager.OnDialogueBundleLoad();

        foreach (var (plugin, source) in plugins)
        {
            source.LogInfo("initializing dialogue..");
            GuardHook(source, plugin.OnDialogueBundleLoad);
        }

        // Several vanilla methods use initialDatabase, but they should really be using the master database.
        DialogueBridgePixelCrushers.DialogueSystem.initialDatabase = manager.Dialogue.pcDatabase;
    }

    public static void OnSceneLoad()
    {
        DiscoAPIPlugin.Instance.Log.LogInfo("scene loaded..");

        var w = SingletonComponent<global::World>.Singleton;
        if (world == null && w != null) world = new(w);

        foreach (var (plugin, source) in plugins)
        {
            GuardHook(source, plugin.OnSceneLoad);
        }
    }

    public static void GuardHook(DiscoSource source, Action<DiscoSource> hook)
    {
        try
        {
            hook(source);
        }
        catch (Exception e)
        {
            source.log.LogError(e);
        }
    }
}
