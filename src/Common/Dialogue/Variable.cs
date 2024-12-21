using Newtonsoft.Json;

namespace DiscoAPI.Common.Dialogue;

[JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
public enum FieldType
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

public class Variable : Asset
{
    [JsonIgnore]
    public override AssetType Type => AssetType.Variable;
    [JsonIgnore]
    public override AssetRef Ref => new VariableRef(this);

    public FieldType type;
    [JsonProperty("value")]
    public string initialValue;

    public Variable(string id, FieldType type, string initialValue) : base(id)
    {
        this.type = type;
        this.initialValue = initialValue;
    }

    public Variable(string id, bool initialValue)
        : this(id, FieldType.Boolean, initialValue.ToString()) { }

    public Variable(string id, int initialValue)
        : this(id, FieldType.Number, initialValue.ToString()) { }

    public Variable(string id, FieldType type)
        : this(id, type, "") { }

    public override string ToString() => $"{sourceGuid}.{id}";
}
