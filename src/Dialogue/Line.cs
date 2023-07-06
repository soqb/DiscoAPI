using System.Collections.Generic;
using Sunshine.Metric;

namespace DiscoAPI.Dialogue;

/// <summary>
/// A line of dialogue.
/// </summary>
public class Line
{
    public int internalID;
    /// <summary>
    /// Extra information relevant to the line for example an <c>ActiveCheck</c>.
    /// </summary>
    public DialogueNode? node;
    /// <summary>
    /// The actor speaking the line.
    /// </summary>
    public ActorRef? speaker;
    /// <summary>
    /// The contents of this dialogue line; what is spoken.
    /// </summary>
    public string? text;
    /// <summary>
    /// A title given to this dialogue line.
    /// </summary>
    public string? title;
    /// <summary>
    /// Lua conditions for this dialogue line to be usable.
    /// </summary>
    public string? condition;
    /// <summary>
    /// Lua script to run when this line is spoken.
    /// </summary>
    public string? script;
    /// <summary>
    /// Cutscene sequence to play while the line is being spoken.
    /// </summary>
    public string? sequence;
    /// <summary>
    /// Cutscene sequence to play while the response menu is showing.
    /// </summary>
    public string? menuSequence;
    /// <summary>
    /// Outgoing links to other lines; the options shown in the menu after the line is spoken.
    /// </summary>
    public List<Link> links = new();

    public Line(int id, string? text)
    {
        this.internalID = id;
        title = text;
        this.text = text;
    }

    public static int IndexOffset(PixelCrushers.DialogueSystem.Conversation conv)
    {
        return conv.dialogueEntries.Count;
    }
}