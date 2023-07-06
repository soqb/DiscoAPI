using System.Collections.Generic;
using DiscoAPI.Dialogue;

namespace DiscoAPI;

public class DiscoManager
{
    public bool initialized;
    public DialogueManager dialogue;
    public readonly List<DiscoSource> linearSources = new();
    public readonly Dictionary<string, int> sourcesByGuid = new();
    /// <summary>
    /// The source representing the vanilla game. Always has the guid "disco".
    /// </summary>
    public DiscoSource Disco => linearSources[0];

    public DiscoManager()
    {
        this.dialogue = new DialogueManager(this);
        MakeSource("disco");
    }

    public DiscoSource MakeSource(string guid)
    {
        if (sourcesByGuid.ContainsKey(guid))
            return GetSource(guid);

        var source = new DiscoSource(this, guid);
        if (linearSources.Count > 0)
        {
            var lastSource = linearSources[linearSources.Count - 1];
            source.dialogue.actorOffset = lastSource.dialogue.actorOffset + lastSource.dialogue.actorCount;
            source.dialogue.conversationOffset = lastSource.dialogue.conversationOffset + lastSource.dialogue.conversationCount;
            source.dialogue.variableOffset = lastSource.dialogue.variableOffset + lastSource.dialogue.variableCount;
        }

        sourcesByGuid.Add(guid, linearSources.Count);
        linearSources.Add(source);
        return source;
    }

    public DiscoSource GetSource(string key) => linearSources[sourcesByGuid[key]];

    public void EnsureInitialized()
    {
        if (!initialized)
        {
            dialogue.InitializeDisco(Disco);
            initialized = true;
        }
    }
}