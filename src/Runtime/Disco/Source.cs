using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using DiscoAPI.Common;
using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Runtime.Assets;
using PC = PixelCrushers.DialogueSystem;

namespace DiscoAPI.Runtime;

public class DiscoSource : IMutableAssets
{
    public class Config
    {
        public Location location = null;
        public ManualLogSource? log = null;
        public IAssetRouter? router = new EmptyAssetRouter();
        public ConfigFile? configFile = null;
    }

    public bool IsVanilla { get; }
    public string Guid { get; }
    public string? DisplayName { get; }
    public string? Description { get; }
    public List<string>? Authors { get; }

    public IAssetRouter Router { get; }

    private static ManualLogSource NowheresvilleLogger = new ManualLogSource("");

    private Config cfg;
    public Location Location => cfg.location;
    public ManualLogSource Log => cfg.log ?? NowheresvilleLogger;
    public ConfigFile? ConfigFile => cfg.configFile;

    public DiscoManager Manager { get; }
    IDiscoManager IDiscoSource.Manager => Manager;

    private Dictionary<Type, IArenaTable> tables;
    private Dictionary<LineRef, int> linksAdded = new();

    internal DiscoSource(DiscoManager manager, string guid, bool isVanilla, Config cfg)
    {
        Guid = guid;
        Manager = manager;

        IsVanilla = isVanilla;

        this.cfg = cfg;
        Router = cfg.router != null ? new MemoizedAssetRouter(cfg.router) : new EmptyAssetRouter();

        if (isVanilla) tables = Enumerable.ToDictionary(manager.Assets.GetDefaultVanillaTables(this), value => value.AssetType);
        else tables = new();
    }

    public void InsertLink(Link link)
    {
        PC.DialogueEntry? GetEntry(LineRef line)
        {
            var arena = (PCArena<PC.Conversation, Conversation>)Manager.Assets.GetArena<Conversation>();

            int id = Manager.Assets.ResolveId(line.conversation.Location);
            return arena.Raw[id]?.dialogueEntries[line.lineId];
        }

        if (link.from != null)
        {
            Log.LogInfo($"inserting link from {link.from} to {link.to}");
            GetEntry(link.from)!.outgoingLinks.Add(Manager.Dialogue.crusher.Crush(this, link));
            linksAdded.TryGetValue(link.from, out int count);
            linksAdded[link.from] = count + 1;

            if (GetEntry(link.to) == null)
                Log.LogWarning($"link inserted to a non-existant destination ({link.to})");
        }
        else
        {
            Log.LogWarning("could not insert link because its source was null");
        }
    }

    public IEnumerable<IAssetTable> GetTables() => tables.Values;

    IAssetTable IDiscoSource.GetAssetsForType(Type type) => GetAssetsForType(type);
    public IArenaTable GetAssetsForType(Type type)
    {
        IArenaTable table;
        foreach (Type ty in new AssetTypeEnumerator(type))
            if (tables.TryGetValue(ty, out table!))
                return table;

        table = Manager.Assets.CreateTableForType(type, this);
        tables.Add(table.AssetType, table);
        return table;
    }

    public void Add(Asset asset) => GetAssetsForType(asset.assetType.type).Add(asset);
}
