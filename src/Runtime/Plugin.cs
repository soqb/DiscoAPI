using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace DiscoAPI.Runtime;

[BepInPlugin(
    GUID,
    "Disco Elysium Modding API",
    "0.1.0-indev"
)]
[BepInProcess("disco.exe")]
public class DiscoAPIPlugin : BasePlugin
{
    public const string GUID = "discoapi";
    public static DiscoAPIPlugin Instance = null!;

    public DiscoAPISettings settings = null!;

    public DiscoAPIPlugin()
    {
        Instance = this;
    }

    internal void PatchAll(Type type)
    {
        try
        {
            DiscoRunner.Harmony.PatchAll(type);
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
        PatchAll(typeof(Patches.MoraleHealthPatches));
        PatchAll(typeof(Patches.MiscPatches));
        PatchAll(typeof(Patches.PersistencePatches));
        PatchAll(typeof(Patches.ArchetypePatches));
        PatchAll(typeof(Patches.AreaPatches));
        PatchAll(typeof(Patches.VirtualTexturePatches));
        PatchAll(typeof(Patches.NewGamePatches));
        PatchAll(typeof(Patches.CharacterEffectPatches));
        PatchAll(typeof(Patches.ThoughtPatches));
        PatchAll(typeof(Patches.ReputationPatches));
        PatchAll(typeof(Patches.ItemPatches));
        DialogueBundleLoader.bundleWasLoaded.AddListener((Action)DiscoRunner.OnDialogueBundleLoad);
        InjectManagedClassAttribute.InjectClasses();
        
        DiscoRunner.OnLoad();
    }
}
