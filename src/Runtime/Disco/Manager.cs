using System.Collections.Generic;
using DiscoAPI.Runtime.Dialogue;
using DiscoAPI.Common;
using DiscoAPI.Common.Dialogue;

namespace DiscoAPI.Runtime;

public class DiscoManager : IDiscoManager
{
    public bool initialized;
    public DialogueManager Dialogue { get; init; }
    IDialogueManager IDiscoManager.Dialogue => this.Dialogue;
    public readonly List<DiscoSource> linearSources = new();
    public readonly Dictionary<string, int> sourcesByGuid = new();
    /// <summary>
    /// The source representing the vanilla game. Always has the guid "disco".
    /// </summary>
    public DiscoSource Disco => linearSources[0];

    public DiscoManager()
    {
        Dialogue = new DialogueManager(this);
    }

    public DiscoSource CreateSource(string guid)
    {
        EnsureInitialized();

        if (sourcesByGuid.ContainsKey(guid))
            return this[guid];

        DiscoSource source = new(this, guid, false);

        sourcesByGuid.Add(guid, linearSources.Count);
        linearSources.Add(source);
        return source;
    }

    IDiscoSource IDiscoManager.GetSource(string key) => this[key];
    public DiscoSource GetSource(string key) => this[key];

    public DiscoSource this[string key] => linearSources[sourcesByGuid[key]];

    public void EnsureInitialized()
    {
        if (!initialized)
        {
            Dialogue.EnsureInitialized();
            DiscoSource disco = new(this, "disco", true);
            sourcesByGuid.Add("disco", 0);
            linearSources.Add(disco);
            initialized = true;

            // DumpDiscoSources.FullDump();
        }
    }
}
