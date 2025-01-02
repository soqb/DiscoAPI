using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace DiscoAPI.Runtime;

public static class DiscoRunner
{
    public static ModWorld? world;

    public static ManualLogSource Log => DiscoAPIPlugin.Instance.Log;

    public static DiscoManager manager = new();
    private static List<IDiscoProvider> plugins = new();
    public static void Register(IDiscoProvider plugin)
    {
        Log.LogInfo($"registered plugin \"{plugin.Guid}\"");
        plugins.Add(plugin);
        GuardHook("register", plugin, plugin.OnRegister);
    }

    public static IDiscoProvider? GetPlugin(string guid) => plugins.Find(p => p.Guid == guid);

    private static bool alreadyLoadedDialogue = false;
    public static void OnDialogueBundleLoad()
    {
        Log.LogInfo("loaded dialogue bundle..");

        if (alreadyLoadedDialogue) return;
        alreadyLoadedDialogue = true;

        manager.OnDialogueBundleLoad();

        foreach (var plugin in plugins)
        {
            GuardHook("dialogue-bundle-load", plugin, plugin.OnDialogueBundleLoad);
        }

        // Several vanilla methods use initialDatabase, but they should really be using the master database.
        DialogueBridgePixelCrushers.DialogueSystem.initialDatabase = manager.Dialogue.pcDatabase;

        Voidforge.UpdateManager.normalEvent.Add((Il2CppSystem.Action)OnUpdate);
    }

    private static void OnMarshalledSceneLoad()
    {
        foreach (var plugin in plugins)
        {
            GuardHook("scene-load", plugin, plugin.OnSceneLoad);
        }
    }

    public static void OnSceneLoad()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        DiscoAPIPlugin.Instance.Log.LogInfo($"scene '{sceneName}' loaded..");

        var w = global::World.Singleton;
        if (w == null) return;
        if (world == null) world = new(w);

        world.MarshallSceneLoad(OnMarshalledSceneLoad);
    }

    public static void OnUpdate()
    {
        foreach (var plugin in plugins)
        {
            GuardHook("update", plugin, plugin.OnUpdate);
        }

        MainThreadExecutor.DequeueOnMainThread();
    }

    public static void GuardHook(string hookname, IDiscoProvider plugin, Action hook)
    {
        try
        {
            hook();
        }
        catch (Exception e)
        {
            Log.LogError($"error in hook '{hookname}' for plugin '{plugin.Guid}':");
            Log.LogError(e);
        }
    }
}
