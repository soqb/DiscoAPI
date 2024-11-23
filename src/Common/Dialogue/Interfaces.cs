using System.Collections.Generic;

namespace DiscoAPI.Common.Dialogue;

public interface IDialogueSource
{
    public IDiscoSource Parent { get; }
    public string Guid => Parent.Guid;
    public IDialogueManager Manager => Parent.Manager.Dialogue;
    public void Add(Asset asset);
    public void InsertLink(Link link);
    public IEnumerable<Asset> AssetsByType(AssetType type);
}

public interface IDialogueManager
{
    public IDiscoManager Parent { get; }
}
