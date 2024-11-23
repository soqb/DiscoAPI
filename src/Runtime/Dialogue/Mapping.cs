using System;
using System.Collections.Generic;

namespace DiscoAPI.Runtime.Dialogue;

public class DialogueMapping
{
    private DialogueManager manager;
    public Dictionary<SkillType, string> articySkillIDs = new();
    public Dictionary<Difficulty, int> reverseArticyDifficultyMap = new();

    private SkillType? NameToSkillType(string name)
    {
        return name switch
        {
            "Authority" => SkillType.Authority,
            "Composure" => SkillType.Composure,
            "Conceptualization" => SkillType.Conceptualization,
            "Half Light" => SkillType.HalfLight,
            "Drama" => SkillType.Drama,
            "Electrochemistry" => SkillType.Electrochemistry,
            "Empathy" => SkillType.Empathy,
            "Endurance" => SkillType.Endurance,
            "Esprit de Corps" => SkillType.EspritDeCorps,
            "Hand/Eye Coordination" => SkillType.HandEyeCoordination,
            "Inland Empire" => SkillType.InlandEmpire,
            "Interfacing" => SkillType.Interfacing,
            "Logic" => SkillType.Logic,
            "Pain Threshold" => SkillType.PainThreshold,
            "Perception" => SkillType.Perception,
            "Perception (Hearing)" => SkillType.Hearing,
            "Perception (Sight)" => SkillType.Sight,
            "Perception (Smell)" => SkillType.Smell,
            "Perception (Taste)" => SkillType.Taste,
            "Physical Instrument" => SkillType.PhysicalInstrument,
            "Reaction Speed" => SkillType.ReactionSpeed,
            "Rhetoric" => SkillType.Rhetoric,
            "Savoir Faire" => SkillType.SavoirFaire,
            "Shivers" => SkillType.Shivers,
            "Suggestion" => SkillType.Suggestion,
            "Encyclopedia" => SkillType.Encyclopedia,
            "Visual Calculus" => SkillType.VisualCalculus,
            "Volition" => SkillType.Volition,
            _ => null,
        };
    }
    public DialogueMapping(DialogueManager manager)
    {
        this.manager = manager;
        // TODO: memoize
        // for some reason, ARTICY_ID_TO_SKILL_TYPE just doesn't contain the correct data.
        foreach (var entry in ArticyBridge.ARTICY_ID_TO_SKILL_NAME)
        {
            var type = NameToSkillType(entry.value);
            if (type != null) articySkillIDs.TryAdd((SkillType)type, entry.key);
        }

        for (int i = 0; i < ArticyBridge.ArticyDifficultyIdToDifficulty.Count; i++)
        {
            var value = (Difficulty)(int)ArticyBridge.ArticyDifficultyIdToDifficulty[i];
            if (Enum.IsDefined<Difficulty>(value))
                reverseArticyDifficultyMap.Add(value, i);
        }
    }

    public int DifficultyToArticy(Difficulty difficulty) => reverseArticyDifficultyMap[difficulty];

    public int SkillToActorID(SkillType skill)
    {
        var actorName = ArticyBridge.ARTICY_ID_TO_SKILL_NAME[SkillToArticyId(skill)];
        return manager.pcDatabase.GetActor(actorName).id;
    }

    public string SkillToArticyId(SkillType skill) => articySkillIDs[skill];
}
