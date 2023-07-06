

using System.Collections.Generic;
using PC = PixelCrushers.DialogueSystem;
using Sunshine.Metric;

namespace DiscoAPI.Dialogue;

public interface DialogueNode
{
    public void VisitEntry(PC.DialogueEntry entry, DialogueSource source, ConversationRef parentConv);
}

public class PassiveCheck : DialogueNode
{
    public SkillType skill;
    public Difficulty difficulty;
    public bool speakOnFailure = false;
    public PassiveCheck(SkillType skill, Difficulty difficulty)
    {
        this.skill = skill;
        this.difficulty = difficulty;
    }

    private string DifficultyString(DialogueSource source)
        => source.Manager.reverseArticyDifficultyMap[difficulty].ToString();
    public void VisitEntry(PC.DialogueEntry entry, DialogueSource source, ConversationRef parentConv)
    {
        entry.fields.Add(new PC.Field("DifficultyPass", DifficultyString(source), PC.FieldType.Number));
        entry.fields.Add(new PC.Field("DifficultyPass", DifficultyString(source), PC.FieldType.Number));
        if (speakOnFailure) entry.fields.Add(new PC.Field("Antipassive", "True", PC.FieldType.Boolean));
        entry.ActorID = source.Manager.SkillToActorID(skill);
    }
}

public class ActiveCheck : DialogueNode
{
    public enum Kind
    {
        White,
        Red,
        AlwaysFail,
        AlwaysSucceed,
    }
    public record Modifier
    {
        public Modifier(string id, int delta, string tooltip)
        {
            this.id = id;
            this.delta = delta;
            this.tooltip = tooltip;
        }

        /// <summary>
        /// The value of the modifier.
        /// <para>
        /// Note that a negative delta results in a positive bonus to the roll and vice versa.
        /// </para>
        /// </summary>
        public int delta;
        public string tooltip;
        public string id;
    }

    public string id;
    public Kind kind;
    public List<Modifier> modifiers = new();
    public SkillType skill;
    public Difficulty difficulty;
    public ActiveCheck(string id, Kind kind, SkillType skill, Difficulty difficulty)
    {
        this.skill = skill;
        this.difficulty = difficulty;
        this.id = id;
        this.kind = kind;
    }

    private string DifficultyString(DialogueSource source)
        => source.Manager.reverseArticyDifficultyMap[difficulty].ToString();
    public void VisitEntry(PC.DialogueEntry entry, DialogueSource source, ConversationRef parentConv)
    {
        entry.ConversantID = source.Manager.pcDatabase.GetActor(Skill.GetActorSkillName(skill)).id;
        entry.fields.Add(new PC.Field("FlagName", $"{source.Guid}.checks.{id}", PC.FieldType.Text));
        source.Add(new Variable(source.variableCount, $"checks.{id}", Variable.Type.Boolean, "False"));

        entry.fields.Add(new PC.Field("SkillType", source.Manager.articySkillIDs[skill], PC.FieldType.Text));
        // These two fields are required for all checks but do nothing AFAICT.
        entry.fields.Add(new PC.Field("Articy Id", "0x0000000000000000", PC.FieldType.Text));
        entry.fields.Add(new PC.Field("check_target", "0x0000000000000000", PC.FieldType.Text));

        if (modifiers.Count > 10)
            DiscoAPIPlugin.Instance.Log.LogWarning($"more than 10 modifiers found for a check in {parentConv} @ {entry.id}; some will be ignored");

        for (int i = 1; i <= 10; i++)
        {
            string value = "", variable = "", tooltip = "";
            if (modifiers.Count > i)
            {
                var modifier = modifiers[i];

                var varName = $"modifier.{parentConv.id}.{entry.id}.{modifier.id}";
                source.Add(new Variable(source.variableCount, varName, Variable.Type.Boolean));
                variable = $"{source.Guid}.{varName}";

                value = modifier.delta.ToString();
                tooltip = modifier.tooltip;
            }
            entry.fields.Add(new PC.Field($"modifier{i}", value, PC.FieldType.Number));
            // TODO: variable is not the name of the variable but a LUA conditions string!
            entry.fields.Add(new PC.Field($"variable{i}", variable, PC.FieldType.Text));
            entry.fields.Add(new PC.Field($"tooltip{i}", tooltip, PC.FieldType.Text));
        }

        if (kind == Kind.White)
        {
            entry.fields.Add(new PC.Field("Forced", "False", PC.FieldType.Boolean));
            entry.fields.Add(new PC.Field("DifficultyWhite", DifficultyString(source), PC.FieldType.Number));

            // var wc = new WhiteCheck();
            // wc.Actor = source.manager.GetActor(pcEntry.ActorID);
            // wc.

            // FailedWhiteChecks.AddToCaches(wc);
        }
        else if (kind == Kind.Red)
        {
            entry.fields.Add(new PC.Field("DifficultyRed", DifficultyString(source), PC.FieldType.Number));
        }
        else
        {
            entry.fields.Add(new PC.Field("DifficultyAtmo", DifficultyString(source), PC.FieldType.Number));
            entry.fields.Add(new PC.Field("AlwaysSucceed", (kind == Kind.AlwaysSucceed).ToString(), PC.FieldType.Number));
        }
    }
}

public class Cost : DialogueNode
{
    /// <summary>
    /// Cost in centims (1/100 of a Reál).
    /// </summary>
    public int cost;
    public bool hideIfNotPayable = false;
    public Cost(int price)
    {
        this.cost = price;
    }

    public void VisitEntry(PC.DialogueEntry entry, DialogueSource source, ConversationRef parentConv)
    {
        entry.fields.Add(new PC.Field("ClickCost", cost.ToString(), PC.FieldType.Number));
        entry.fields.Add(new PC.Field("HiddenNotEnough", hideIfNotPayable.ToString(), PC.FieldType.Boolean));
        // i don't think this field actually does anything.
        entry.fields.Add(new PC.Field("CostOnce", "True", PC.FieldType.Boolean));
    }
}