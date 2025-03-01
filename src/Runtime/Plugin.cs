using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace DiscoAPI.Runtime;

[BepInPlugin(
    GUID,
    "Disco Elysium Modding API",
    "0.0.1"
)]
[BepInProcess("disco.exe")]
public class DiscoAPIPlugin : BasePlugin
{
    public const string GUID = "DiscoAPI";
    public static DiscoAPIPlugin Instance = null!;
    private static Harmony harmony = new Harmony(GUID);

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

        PatchAll(typeof(DiscoAPIPlugin));
        PatchAll(typeof(Patches.ManagedPatches));
        PatchAll(typeof(Patches.DialoguePatches));
        PatchAll(typeof(Patches.PagesPatches));
        PatchAll(typeof(Patches.CharacterPatches));
        PatchAll(typeof(Patches.MiscPatches));
        // PatchAll(typeof(Patches.VirtualTexturePatches));
        DialogueBundleLoader.bundleWasLoaded.AddListener((Action)DiscoRunner.OnDialogueBundleLoad);

        DiscoRunner.OnLoad();
    }
}
