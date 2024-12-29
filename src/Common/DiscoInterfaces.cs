using System.Collections.Generic;
using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;

namespace DiscoAPI.Common;

public interface IDiscoSource
{
    public string Guid { get; }
    public string? DisplayName { get; }
    public string? Description { get; }
    public List<string>? Authors { get; }
    public IDiscoManager Manager { get; }
    public IAssetSource Assets { get; }

    public void InsertLink(Link link);
}

public interface IDiscoManager
{
    public IAssetManager Assets { get; }
    public IDialogueManager Dialogue { get; }
    public IDiscoSource GetSource(string key);
}
