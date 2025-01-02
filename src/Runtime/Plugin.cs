using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.LowLevel;

namespace DiscoAPI.Runtime;

[BepInPlugin(
    GUID,
    "Disco Elysium Modding API",
    "0.0.1"
)]
[BepInProcess("disco.exe")]
public class DiscoAPIPlugin : BasePlugin
{
    public const string GUID = "io.github.soqb.disco-api";
    public static DiscoAPIPlugin Instance = null!;
    private static Harmony harmony = new Harmony(GUID);
    private static ManualLogSource mockUnityLogger = new ManualLogSource("Unity");

    public DiscoAPISettings settings = null!;

    public DiscoAPIPlugin()
    {
        Instance = this;
    }

    public override void Load()
    {
        BepInEx.Logging.Logger.Sources.Add(mockUnityLogger);

        harmony.PatchAll(typeof(DiscoAPIPlugin));
        harmony.PatchAll(typeof(Patches.DialoguePatches));
        harmony.PatchAll(typeof(Patches.PagesPatches));
        harmony.PatchAll(typeof(Patches.CharacterPatches));
        harmony.PatchAll(typeof(Patches.MiscPatches));
        AddUnityListener(DialogueBundleLoader.bundleWasLoaded, DiscoRunner.OnDialogueBundleLoad);

        FortressOccident.SceneTransitionManager.readyEvent.Add((Il2CppSystem.Action)DiscoRunner.OnSceneLoad);

        DiscoRunner.Register(new InherentProvider());

        settings = new(Config);
    }

    public void AddUnityListener(UnityEvent evt, System.Action action) => evt.AddListener(action);

    // This patch makes debugging much friendlier since BepInEx chooses to ignoer stacktraces...
    [HarmonyPatch(typeof(BepInEx.Unity.IL2CPP.Logging.IL2CPPUnityLogSource), nameof(BepInEx.Unity.IL2CPP.Logging.IL2CPPUnityLogSource.UnityLogCallback))]
    [HarmonyPostfix]
    private static void UnityErrorStacktrace(string exception, LogType type)
    {
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                mockUnityLogger.LogError(exception);
                break;
            default:
                break;
        }
    }
}
