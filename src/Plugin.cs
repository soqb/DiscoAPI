using BepInEx;
using BepInEx.Unity.IL2CPP;
using FortressOccident;
using HarmonyLib;
using Pages.Gameplay.Dialogue;
using DiscoAPI.Dialogue;
using Sunshine.Hack;
using Sunshine.Metric;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using PC = PixelCrushers.DialogueSystem;
using Sunshine;
using System.Diagnostics;
using UnityEngine.Events;
using System.Runtime.InteropServices;

namespace DiscoAPI;

[BepInPlugin(
    GUID,
    "Disco Elysium Modding API",
    "0.0.1"
)]
[BepInProcess("disco.exe")]
public class DiscoAPIPlugin : BasePlugin
{
    public const string GUID = "io.github.soqb.disco-api";
    private static Harmony harmony = new Harmony(GUID);
    public static DiscoAPIPlugin Instance = null!;

    public DiscoAPIPlugin()
    {
        Instance = this;
    }

    public override void Load()
    {
        harmony.PatchAll(typeof(DiscoAPIPlugin));
        harmony.PatchAll(typeof(Patches.DialoguePatches));
        AddUnityListener(DialogueBundleLoader.bundleWasLoaded, DiscoRunner.OnDialogueBundleLoad);
    }

    public void AddUnityListener(UnityEvent evt, System.Action action) => evt.AddListener(action);

    // This patch makes debugging much friendlier since DE suppresses errors to avoid crashes.
    [HarmonyPatch(typeof(BepInEx.Unity.IL2CPP.Logging.IL2CPPUnityLogSource), nameof(BepInEx.Unity.IL2CPP.Logging.IL2CPPUnityLogSource.UnityLogCallback))]
    [HarmonyPostfix]
    private static void UnityErrorStacktrace(string exception, LogType type)
    {
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                Instance.Log.LogError(exception);
                break;
            default:
                break;
        }
    }
}
