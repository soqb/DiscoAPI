using System.Collections.Generic;
using BepInEx.Logging;
using DiscoAPI.Common;
using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Runtime.Assets;

namespace DiscoAPI.Runtime;

public class DiscoSource : IDiscoSource
{
    public ManualLogSource Log { get; }
    public string Guid { get; }
    public string? DisplayName { get; }
    public string? Description { get; }
    public List<string>? Authors { get; }
    public AssetSource Assets { get; }

    public DiscoManager Manager { get; }
    IDiscoManager IDiscoSource.Manager => Manager;
    IAssetSource IDiscoSource.Assets => Assets;

    private Dictionary<LineRef, int> linksAdded = new();

    public DiscoSource(DiscoManager manager, string guid, bool isDisco, ManualLogSource log)
    {
        Guid = guid;
        Manager = manager;
        Assets = isDisco ? AssetSource.CreateDisco(this) : AssetSource.Create(this);

        Log = log;
    }

    public void InsertLink(Link link)
    {
        if (link.from != null)
        {
            Log.LogInfo($"inserting link from {link.from} to {link.to}");
            var from = Manager.Dialogue.pcDatabase.GetDialogueEntry(link.from.conversation.ResolveId(Manager), link.from.lineID);
            from.outgoingLinks.Add(Manager.Assets.crusher.Crush(this, link));
            linksAdded.TryGetValue(link.from, out int count);
            linksAdded[link.from] = count + 1;

            if (Manager.Dialogue.pcDatabase.GetDialogueEntry(link.to.conversation.ResolveId(Manager), link.to.lineID) == null)
                Log.LogWarning($"link inserted to a non-existant destination ({link.to})");
        }
        else
        {
            Log.LogWarning("could not insert link because its source was null");
        }
    }
}
