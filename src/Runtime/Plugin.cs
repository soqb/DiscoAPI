using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

namespace DiscoAPI.Runtime;

[BepInPlugin(
    GUID,
    "Disco Elysium Modding API",
    "0.0.1"
)]
[BepInProcess("disco.exe")]
public class DiscoAPIPlugin : BasePlugin
{
    public const string GUID = "discoapi";
    public static DiscoAPIPlugin Instance = null!;
    private static Harmony harmony = new Harmony(GUID);
    private static ManualLogSource mockUnityLogger = new ManualLogSource("Unity");

    public DiscoAPISettings settings = null!;

    public DiscoAPIPlugin()
    {
        Instance = this;
    }

    public void PatchAll(Type type)
    {
        try
        {
            harmony.PatchAll(type);
        }
        catch (Exception e)
        {
            DiscoRunner.Log.LogError($"patching with type {type} failed!");
            DiscoRunner.Log.LogError(e);
        }
    }


    public override void Load()
    {
        settings = new(Config);

        BepInEx.Logging.Logger.Sources.Add(mockUnityLogger);

        PatchAll(typeof(DiscoAPIPlugin));
        PatchAll(typeof(Patches.DialoguePatches));
        PatchAll(typeof(Patches.PagesPatches));
        PatchAll(typeof(Patches.CharacterPatches));
        PatchAll(typeof(Patches.MiscPatches));
        // PatchAll(typeof(Patches.VirtualTexturePatches));
        AddUnityListener(DialogueBundleLoader.bundleWasLoaded, DiscoRunner.OnDialogueBundleLoad);

        DiscoRunner.OnLoad();
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
