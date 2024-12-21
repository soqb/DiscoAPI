using System;
using System.Collections.Generic;

namespace DiscoAPI.Common.Dialogue;

public interface DialogueNodeVisitor
{
    public void Empty(EmptyDialogueNode ck);
    public void Passive(PassiveCheck ck);
    public void Active(ActiveCheck ck);
    public void Cost(CostCheck ck);
}

public interface DialogueNode
{
    public void Visit(DialogueNodeVisitor vtr);
}

public class EmptyDialogueNode : DialogueNode
{
    public void Visit(DialogueNodeVisitor vtr) => vtr.Empty(this);
}

[Serializable]
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

    public void Visit(DialogueNodeVisitor vtr) => vtr.Passive(this);
}

[Serializable]
public class ActiveCheck : DialogueNode
{
    [Serializable]
    public enum Kind
    {
        White,
        Red,
        AlwaysFail,
        AlwaysSucceed,
    }
    [Serializable]
    public class Modifier
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

    public void Visit(DialogueNodeVisitor vtr) => vtr.Active(this);
}

[Serializable]
public class CostCheck : DialogueNode
{
    /// <summary>
    /// Cost in centims (1/100 of a Reál).
    /// </summary>
    public int cost;
    public bool hideIfNotPayable = false;
    public CostCheck(int price)
    {
        this.cost = price;
    }

    public void Visit(DialogueNodeVisitor vtr) => vtr.Cost(this);
}
