using System;
using System.Collections.Generic;
using DiscoAPI.Common.Assets;

namespace DiscoAPI.Runtime.Dialogue;

public class DialogueMapping
{
    private DialogueManager manager;
    private Dictionary<AssetLocation, string> articySkillIds = new();
    public Dictionary<Difficulty, int> reverseArticyDifficultyMap = new();

    private IAssetRef<Skill>? NameToSkillRef(string name)
    {
        return name switch
        {
            "Authority" => Skills.Authority,
            "Composure" => Skills.Composure,
            "Conceptualization" => Skills.Conceptualization,
            "Half Light" => Skills.HalfLight,
            "Drama" => Skills.Drama,
            "Electrochemistry" => Skills.Electrochemistry,
            "Empathy" => Skills.Empathy,
            "Endurance" => Skills.Endurance,
            "Esprit de Corps" => Skills.EspritDeCorps,
            "Hand/Eye Coordination" => Skills.HandEyeCoordination,
            "Inland Empire" => Skills.InlandEmpire,
            "Interfacing" => Skills.Interfacing,
            "Logic" => Skills.Logic,
            "Pain Threshold" => Skills.PainThreshold,
            "Perception" => Skills.Perception,
            "Perception (Hearing)" => Skills.Hearing,
            "Perception (Sight)" => Skills.Sight,
            "Perception (Smell)" => Skills.Smell,
            "Perception (Taste)" => Skills.Taste,
            "Physical Instrument" => Skills.PhysicalInstrument,
            "Reaction Speed" => Skills.ReactionSpeed,
            "Rhetoric" => Skills.Rhetoric,
            "Savoir Faire" => Skills.SavoirFaire,
            "Shivers" => Skills.Shivers,
            "Suggestion" => Skills.Suggestion,
            "Encyclopedia" => Skills.Encyclopedia,
            "Visual Calculus" => Skills.VisualCalculus,
            "Volition" => Skills.Volition,
            _ => null,
        };
    }
    public DialogueMapping(DialogueManager manager)
    {
        this.manager = manager;
        // for some reason, ARTICY_ID_TO_SKILL_TYPE just doesn't contain the correct data.
        foreach (var entry in ArticyBridge.ARTICY_ID_TO_SKILL_NAME)
        {
            var skill = NameToSkillRef(entry.value);
            if (skill != null) articySkillIds.TryAdd(skill.Location, entry.key);
        }

        for (int i = 0; i < ArticyBridge.ArticyDifficultyIdToDifficulty.Count; i++)
        {
            var value = (Difficulty)(int)ArticyBridge.ArticyDifficultyIdToDifficulty[i];
            if (Enum.IsDefined<Difficulty>(value))
                reverseArticyDifficultyMap.Add(value, i);
        }
    }

    public int DifficultyToArticy(Difficulty difficulty) => reverseArticyDifficultyMap[difficulty];

    public int SkillToActorId(IAssetRef<Skill> skill)
    {
        var actorName = ArticyBridge.ARTICY_ID_TO_SKILL_NAME[SkillToArticyId(skill)];
        return manager.pcDatabase.GetActor(actorName).id;
    }

    public string SkillToArticyId(IAssetRef<Skill> skill) => articySkillIds[skill.Location];
}
