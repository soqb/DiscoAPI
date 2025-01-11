using System.Collections.Generic;
using DiscoAPI.Common.Assets;

namespace DiscoAPI.Common.Dialogue;

public interface IDialogueNodeVisitor
{
    public void Empty(EmptyDialogueNode ck);
    public void Passive(PassiveCheck ck);
    public void Active(ActiveCheck ck);
    public void Cost(CostCheck ck);
}

public interface IDialogueNode
{
    public void Visit(IDialogueNodeVisitor vtr);
}

public class EmptyDialogueNode : IDialogueNode
{
    public void Visit(IDialogueNodeVisitor vtr) => vtr.Empty(this);
}

public class PassiveCheck : IDialogueNode
{
    public IAssetRef<Skill> skill;
    public Difficulty difficulty;
    public bool speakOnFailure = false;
    public PassiveCheck(IAssetRef<Skill> skill, Difficulty difficulty)
    {
        this.skill = skill;
        this.difficulty = difficulty;
    }

    public void Visit(IDialogueNodeVisitor vtr) => vtr.Passive(this);
}

public class ActiveCheck : IDialogueNode
{
    public enum Kind
    {
        White,
        Red,
        AlwaysFail,
        AlwaysSucceed,
    }

    public record Modifier(string id, int delta, string tooltip, string? condition)
    {
        /// <summary>
        /// The value of the modifier.
        /// <para>
        /// Note that a negative delta results in a positive bonus to the roll and vice versa.
        /// </para>
        /// </summary>
        public readonly int delta = delta;
    }

    public string id;
    public Kind kind;
    public List<Modifier> modifiers = new();
    public IAssetRef<Skill> skill;
    public Difficulty difficulty;
    public ActiveCheck(string id, Kind kind, IAssetRef<Skill> skill, Difficulty difficulty)
    {
        this.skill = skill;
        this.difficulty = difficulty;
        this.id = id;
        this.kind = kind;
    }

    public void Visit(IDialogueNodeVisitor vtr) => vtr.Active(this);
}

public class CostCheck : IDialogueNode
{
    /// <summary>
    /// Cost in centims (1/100 of a Reál).
    /// </summary>
    public int cost;
    public bool hideIfNotPayable = false;
    public bool repeatable = false;
    public CostCheck(int price)
    {
        this.cost = price;
    }

    public void Visit(IDialogueNodeVisitor vtr) => vtr.Cost(this);
}
