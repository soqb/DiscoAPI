using Newtonsoft.Json;
using DiscoAPI.Common.Assets;

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

public class Variable : Asset, IAssetRef<Variable>
{
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

    [JsonIgnore]
    public new AssetLocation<Variable> Location => new(source, id);
    Variable? IAssetRef<Variable>.Resolve(IDiscoManager mgr) => (Variable?)((IAssetRef)this).Resolve(mgr);

    public override string ToString() => $"{source}.{id}";
}
