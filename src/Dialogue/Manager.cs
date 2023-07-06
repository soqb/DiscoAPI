using System;
using System.Collections.Generic;
using Sunshine.Metric;
using PC = PixelCrushers.DialogueSystem;

namespace DiscoAPI.Dialogue;

public class DialogueManager
{
    public DiscoManager parent;
    public PC.DialogueDatabase pcDatabase = null!; // this property is set in InitializeDisco.
    public Dictionary<SkillType, string> articySkillIDs = new();
    public Dictionary<Difficulty, int> reverseArticyDifficultyMap = new();
    public Dictionary<string, PC.Asset> fakeArticyIDToAssetCache = new();

    private SkillType NameToSkillType(string name)
    {
        return name switch
        {
            "Authority" => SkillType.AUTHORITY,
            "Composure" => SkillType.COMPOSURE,
            "Conceptualization" => SkillType.CONCEPTUALIZATION,
            "Half Light" => SkillType.HALF_LIGHT,
            "Drama" => SkillType.DRAMA,
            "Electrochemistry" => SkillType.ELECTROCHEMISTRY,
            "Empathy" => SkillType.EMPATHY,
            "Endurance" => SkillType.ENDURANCE,
            "Esprit de Corps" => SkillType.ESPRIT_DE_CORPS,
            "Hand/Eye Coordination" => SkillType.HE_COORDINATION,
            "Inland Empire" => SkillType.INLAND_EMPIRE,
            "Interfacing" => SkillType.INTERFACING,
            "Logic" => SkillType.LOGIC,
            "Pain Threshold" => SkillType.PAIN_THRESHOLD,
            "Perception" => SkillType.PERCEPTION,
            "Perception (Hearing)" => SkillType.HEARING,
            "Perception (Sight)" => SkillType.SIGHT,
            "Perception (Smell)" => SkillType.SMELL,
            "Perception (Taste)" => SkillType.TASTE,
            "Physical Instrument" => SkillType.PHYSICAL_INSTRUMENT,
            "Reaction Speed" => SkillType.REACTION,
            "Rhetoric" => SkillType.RHETORIC,
            "Savoir Faire" => SkillType.SAVOIR_FAIRE,
            "Shivers" => SkillType.SHIVERS,
            "Suggestion" => SkillType.SUGGESTION,
            "Encyclopedia" => SkillType.ENCYCLOPEDIA,
            "Visual Calculus" => SkillType.VISUAL_CALCULUS,
            "Volition" => SkillType.VOLITION,
            _ => SkillType.NONE,
        };
    }

    public int SkillToActorID(SkillType skill)
    {
        var actorName = ArticyBridge.ARTICY_ID_TO_SKILL_NAME[articySkillIDs[skill]];
        return pcDatabase.GetActor(actorName).id;
    }

    public DialogueManager(DiscoManager parent)
    {
        this.parent = parent;
        // TODO: memoize
        // for some reason, ARTICY_ID_TO_SKILL_TYPE just doesn't contain the correct data.
        foreach (var entry in ArticyBridge.ARTICY_ID_TO_SKILL_NAME)
            articySkillIDs.TryAdd(NameToSkillType(entry.value), entry.key);

        for (int i = 0; i < ArticyBridge.ArticyDifficultyIdToDifficulty.Count; i++)
        {
            reverseArticyDifficultyMap.Add(ArticyBridge.ArticyDifficultyIdToDifficulty[i], i);
        }
    }

    public void InitializeDisco(DiscoSource disco)
    {
        pcDatabase = PC.DialogueManager.MasterDatabase;
        pcDatabase.SyncAll();
        // Assets seem to be in ascending order of id, but some ids are skipped,
        // so we start from the last present id so that we can never have two assets with the same id!
        disco.dialogue.actorCount += pcDatabase.actors[pcDatabase.actors.Count - 1].id + 1;
        disco.dialogue.conversationCount += pcDatabase.conversations[pcDatabase.conversations.Count - 1].id + 1;
        disco.dialogue.variableCount += pcDatabase.variables[pcDatabase.variables.Count - 1].id + 1;
    }

    public PC.Actor GetActor(int resolvedID)
        => FindAssetByID<PC.Actor>(pcDatabase.actors, resolvedID);
    public PC.Actor GetActor(ActorRef handle) => GetActor(handle.AsId(this));

    public PC.Conversation GetConversation(ConversationRef handle)
        => FindAssetByID<PC.Conversation>(pcDatabase.conversations, handle.AsId(this));

    public PC.DialogueEntry GetEntry(Link.LineRef line)
    {
        var conv = GetConversation(line.conversation);
        return FindByID<PC.DialogueEntry>(conv.dialogueEntries, line.lineID, (x) => x.id);
    }

    public static T FindAssetByID<T>(Il2CppSystem.Collections.Generic.List<T> assets, int resolvedID) where T : PC.Asset
        => FindByID<T>(assets, resolvedID - 1, (x) => x.id - 1); // assets are 1-indexed

    private static T FindByID<T>(Il2CppSystem.Collections.Generic.List<T> assets, int resolvedID, Func<T, int> getID)
    {
        // This algorithm should hone in on the correct index for the id in as few loops as possible.
        // We need to do this because while ids are strictly ascending,
        // they do not necessarily correspond to the their index in the list.
        T asset;
        int guessedIdx = resolvedID;
        do
        {
            guessedIdx = assets.Count > guessedIdx ? guessedIdx >= 0 ? guessedIdx : 0 : assets.Count - 1;
            asset = assets[guessedIdx];
            // positive if we undershot, negative if we overshot.
            int distance = resolvedID - getID(asset);
            guessedIdx += distance;
        } while (getID(asset) != resolvedID);

        return asset;
    }

    public DialogueSource GetSource(string key) => parent.linearSources[parent.sourcesByGuid[key]].dialogue;
}