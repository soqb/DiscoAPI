using System;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using DiscoAPI.Runtime.Components;
using DiscoAPI.Runtime.SaveSystem;
using HarmonyLib;

namespace DiscoAPI.Runtime;

public static class DiscoRunner
{
    public static GlobalDiscoConfig globalConfig = new();

    public static ModEntity<World>? world;

    public static ModSaveSystem saveSystem = new();

    public static ManualLogSource Log => DiscoAPIPlugin.Instance.Log;

    public static DiscoManager manager = new();
    public static DiscoSource? GetSource(string guid) => manager[guid];

    internal static DiscoHook<Action> update = new("update");
    internal static DiscoHook<Action> load = new("load");
    internal static DiscoHook<Action> sceneLoad = new("scene-load");
    internal static DiscoHook<Action> dialogueLoad = new("dialogue-load");
    internal static DiscoHook<Action> preDialogueLoad = new("pre-dialogue-load");
    internal static DiscoHook<Action<ModSaveSystem>> saveGame = new("save-game");
    internal static DiscoHook<Action<ModSaveSystem>> loadSavedGame = new("load-saved-game");

    public static Harmony Harmony { get; } = new Harmony(DiscoAPIPlugin.GUID);

    public static DiscoSource SourceFromPlugin(BasePlugin plugin) => SourceFromPlugin(plugin, new());
    public static DiscoSource SourceFromPlugin(BasePlugin plugin, DiscoSource.Config cfg)
    {
        string guid = BepInEx.MetadataHelper.GetMetadata(plugin).GUID;

        if (cfg.location == null) cfg.location = Location.GetFromAssembly(plugin.GetType().Assembly, guid);
        if (cfg.log == null) cfg.log = plugin.Log;
        if (cfg.configFile == null) cfg.configFile = plugin.Config;

        return manager.CreateSource(guid, cfg);
    }

    public static void OnLoad()
    {
        IL2CPPChainloader.Instance.Finished += () => load.Invoke();

        FortressOccident.SceneTransitionManager.readyEvent.Add((Il2CppSystem.Action)DiscoRunner.OnSceneLoad);

        InherentProvider.Provide();

        InitLogDictionary();
    }

    private static void InitLogDictionary()
    {
        // global::Log.COMPONENT[] missing = new global::Log.COMPONENT[global::Log.componentState.Count];

        // int i = 0;
        // foreach (var entry in global::Log.componentState)
        //     if (!entry.Value)
        //     {
        //         missing[i++] = entry.Key;
        //         DiscoRunner.Log.LogInfo($"missing '{entry.Key}'");
        //     }

        // for (int j = 0; j < i; i++) global::Log.ToggleComponent(missing[j]);
    }

    public static void OnDialogueBundleLoad()
    {
        Log.LogInfo("loaded dialogue bundle..");

        if (manager.WasBundleLoaded) return;
        InitLogDictionary();

        manager.OnDialogueBundleLoad();

        // Several vanilla methods use initialDatabase, but they should really be using the master database.
        DialogueBridgePixelCrushers.DialogueSystem.initialDatabase = manager.Dialogue.pcDatabase;

        Voidforge.UpdateManager.normalEvent.Add((Il2CppSystem.Action)OnUpdate);

        preDialogueLoad.Invoke();
        dialogueLoad.Invoke();

        SkillUtils.OnDialogueBundleLoad();

        if (DiscoAPISettings.DumpSourcesOnStartup) DumpDiscoSources.FullDump();

    }

    public static void OnSceneLoad()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        DiscoAPIPlugin.Instance.Log.LogInfo($"scene '{sceneName}' loaded..");

        var w = global::World.Singleton;
        if (w == null) return;
        if (world == null) world = WorldComponents.Of(w);

        sceneLoad.Invoke();
        if (sceneName == "Lobby") LobbyLoadExecutor.OnLoadLobbyPlease();
    }

    public static void OnUpdate()
    {
        update.Invoke();

        MainThreadExecutor.DequeueOnMainThreadPlease();
    }
}
