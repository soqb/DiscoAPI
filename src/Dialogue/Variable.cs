namespace DiscoAPI.Dialogue;

public class Variable : Asset
{
    public enum Type
    {
        Text = 0,
        Number = 1,
        Boolean = 2,
        Files = 3,
        Localization = 4,
        Actor = 5,
        Item = 6,
        Location = 7
    }

    public Type type;
    public string initialValue;

    public Variable(int internalID, string id, Type type, string initialValue) : base(internalID, id)
    {
        this.type = type;
        this.initialValue = initialValue;
    }

    public Variable(int internalID, string id, bool initialValue)
        : this(internalID, id, Type.Boolean, initialValue.ToString()) { }

    public Variable(int internalID, string id, int initialValue)
        : this(internalID, id, Type.Number, initialValue.ToString()) { }

    public Variable(int internalID, string id, Type type)
        : this(internalID, id, type, "") { }

    public override string ToString() => $"{sourceGuid}.{id}";
}