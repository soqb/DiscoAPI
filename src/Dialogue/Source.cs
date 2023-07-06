
using System.Collections.Generic;
using Sunshine.Metric;
using UnityEngine;
using static statistics.DialogueSanityChecks;
using PC = PixelCrushers.DialogueSystem;

namespace DiscoAPI.Dialogue;

public class DialogueSource
{
    public DiscoSource parent;
    public int actorOffset;
    public int conversationOffset;
    public int variableOffset;
    public int actorCount;
    public int variableCount;
    internal int conversationCount;
    public Dictionary<Link.LineRef, int> linksAdded = new();
    public DialogueManager Manager { get => parent.manager.dialogue; }
    public string Guid { get => parent.guid; }

    public DialogueSource(DiscoSource parent)
    {
        this.parent = parent;
    }

    private PC.Link MapLink(Link link)
    {
        var pcLink = new PC.Link();
        pcLink.isConnector = true;
        if (link.from != null)
        {
            pcLink.originConversationID = link.from.conversation.AsId(Manager);
            pcLink.originDialogueID = link.from.lineID;
        }
        pcLink.destinationConversationID = link.to.conversation.AsId(Manager);
        pcLink.destinationDialogueID = link.to.lineID;

        return pcLink;
    }
    private PC.DialogueEntry MapLine(Line line, ConversationRef parentConv)
    {
        var pcEntry = new PC.DialogueEntry();
        pcEntry.conversationID = parentConv.AsId(Manager);
        pcEntry.id = line.internalID;
        pcEntry.fields = new();
        pcEntry.conditionsString = line.condition ?? "";
        pcEntry.userScript = line.script ?? "";
        pcEntry.ActorID = line.speaker?.AsId(Manager) ?? 0;
        pcEntry.DialogueText = line.text;
        pcEntry.Title = line.title ?? line.text ?? $"{Guid}.{parentConv.id}.{line.internalID}";
        if (line.sequence != null) pcEntry.Sequence = line.sequence;
        if (line.sequence != null) pcEntry.ResponseMenuSequence = line.menuSequence;
        foreach (var link in line.links)
            pcEntry.outgoingLinks.Add(MapLink(link));


        if (line.node != null)
        {
            pcEntry.fields.Add(new PC.Field("AlwaysPlayVoice", "False", PC.FieldType.Boolean));
            pcEntry.fields.Add(new PC.Field("PlayVoiceInPsychologicalMode", "False", PC.FieldType.Boolean));
            line.node.VisitEntry(pcEntry, this, parentConv);
        }

        return pcEntry;
    }
    private void VisitAsset(PC.Asset pcAsset, Asset asset, string assetKind)
    {
        asset.sourceGuid = Guid;
        var name = $"{Guid}.${assetKind}.{asset.id}";
        pcAsset.fields.Add(new PC.Field(ArticyBridge.ARTICY_ID_FIELD, name, PC.FieldType.Text));
        Manager.fakeArticyIDToAssetCache.Add(name, pcAsset);
    }
    private PC.Conversation MapAsset(Conversation conv)
    {
        var pcConv = new PC.Conversation();
        pcConv.id = conv.internalID + conversationOffset;
        pcConv.fields = new();
        pcConv.Name = conv.id;
        pcConv.Title = conv.id;
        pcConv.dialogueEntries = new();

        var handle = new ConversationRef(this, conv.internalID);
        foreach (var line in conv.lines)
        {
            pcConv.dialogueEntries.Add(MapLine(line, handle));
        }


        return pcConv;
    }
    public void Add(Conversation asset)
    {
        var pc = MapAsset(asset);
        VisitAsset(pc, asset, "conversation");
        Manager.pcDatabase.AddConversation(pc);
        conversationCount += 1;
    }

    private PC.Actor MapAsset(Actor actor)
    {
        var pcActor = new PC.Actor();
        pcActor.id = actor.internalID + actorOffset;
        pcActor.fields = new();
        pcActor.Name = actor.displayName;

        return pcActor;
    }
    public void Add(Actor asset)
    {
        var pc = MapAsset(asset);
        VisitAsset(pc, asset, "actor");
        Manager.pcDatabase.actors.Add(pc);
        actorCount += 1;
    }
    private PC.Variable MapAsset(Variable var)
    {
        var pcVar = new PC.Variable();
        pcVar.id = var.internalID + variableOffset;
        pcVar.fields = new();
        pcVar.Name = $"{Guid}.{var.id}";
        pcVar.InitialValue = var.initialValue;
        pcVar.Description = ""; // TODO
        pcVar.Type = (PC.FieldType)var.type;

        return pcVar;
    }
    public void Add(Variable asset)
    {
        var pc = MapAsset(asset);
        VisitAsset(pc, asset, "variable");
        Manager.pcDatabase.variables.Add(pc);
        variableCount += 1;
    }

    public void InsertLink(Link link)
    {
        if (link.from != null)
        {
            Manager.GetEntry(link.from).outgoingLinks.Add(MapLink(link));
            linksAdded.TryGetValue(link.from, out int count);
            linksAdded[link.from] = count + 1;
        }
        else
        {
            DiscoAPIPlugin.Instance.Log.LogWarning("could not insert link because its source was null");
        }
    }
}