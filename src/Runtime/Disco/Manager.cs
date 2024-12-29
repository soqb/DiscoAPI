using System.Collections.Generic;
using DiscoAPI.Runtime.Dialogue;
using DiscoAPI.Common;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Runtime.Assets;
using DiscoAPI.Common.Assets;
using System;
using BepInEx.Logging;

namespace DiscoAPI.Runtime;

public class DiscoManager : IDiscoManager
{
    private DialogueManager? realDialogue = null;
    public bool WasBundleLoaded { private set; get; }

    internal T BundleGuard<T>(T? v) => WasBundleLoaded
        ? v!
        : throw new NotSupportedException("cannot access dialogue before the dialogue bundle is loaded.");

    public DialogueManager Dialogue => BundleGuard(realDialogue);
    public AssetManager Assets { get; }
    IDialogueManager IDiscoManager.Dialogue => this.Dialogue;
    IAssetManager IDiscoManager.Assets => this.Assets;
    public readonly List<DiscoSource> linearSources = new();
    public readonly Dictionary<string, int> sourcesByGuid = new();

    public DiscoManager()
    {
        Assets = new(this);

        DiscoSource disco = new(this, "disco", true, Logger.CreateLogSource("disco"));
        sourcesByGuid.Add("disco", 0);
    }

    /// <summary>
    /// The source representing the vanilla game. Always has the guid "disco".
    /// </summary>
    public DiscoSource Disco => linearSources[0];

    public DiscoSource CreateSource(DiscoPlugin plugin)
    {
        if (sourcesByGuid.ContainsKey(plugin.Guid))
            return this[plugin.Guid];

        DiscoSource source = new(this, plugin.Guid, false, plugin.Log);

        sourcesByGuid.Add(plugin.Guid, linearSources.Count);
        linearSources.Add(source);
        return source;
    }

    IDiscoSource IDiscoManager.GetSource(string key) => this[key];
    public DiscoSource GetSource(string key) => this[key];

    public DiscoSource this[string key] => linearSources[sourcesByGuid[key]];

    public void OnDialogueBundleLoad()
    {
        if (!WasBundleLoaded)
        {
            WasBundleLoaded = true;

            realDialogue = new DialogueManager(this);
            Dialogue.pcDatabase = PixelCrushers.DialogueSystem.DialogueManager.MasterDatabase;
            Dialogue.pcDatabase.SyncAll();

            Assets.OnDialogueBundleLoaded();

            foreach (var sc in linearSources) sc.Assets.OnDialogueBundleLoaded();

            // DumpDiscoSources.FullDump();
        }
    }
}
