using Newtonsoft.Json;
using DiscoAPI.Common.Assets;
using System;

namespace DiscoAPI.Common.Dialogue;

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

public class FieldValueJsonConverter : JsonConverter<FieldValue>
{
    public override FieldValue? ReadJson(JsonReader reader, Type objectType, FieldValue? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        FieldValue val;
        if (reader.TokenType == JsonToken.String) val = reader.ReadAsString()!;
        else if (reader.TokenType == JsonToken.Integer) val = reader.ReadAsInt32()!;
        else if (reader.TokenType == JsonToken.Float) val = reader.ReadAsDouble()!;
        else val = new(serializer.Deserialize<IAssetRef<Actor>>(reader)!);

        return val;
    }

    public override void WriteJson(JsonWriter writer, FieldValue? value, JsonSerializer serializer)
    {
        if (value == null) writer.WriteNull();
        else writer.WriteValue(value.value);
    }
}

[JsonConverter(typeof(FieldValueJsonConverter))]
public record FieldValue
{
    public readonly object value;
    public readonly FieldType type;
    private FieldValue(object inner, FieldType type)
    {
        this.value = inner;
        this.type = type;
    }


    public FieldValue(IAssetRef<Actor> value) : this((object)value, FieldType.Actor) { }

    public static implicit operator FieldValue(string value) => new((object)value, FieldType.Text);
    public static implicit operator FieldValue(int value) => new((object)value, FieldType.Number);
    public static implicit operator FieldValue(double value) => new((object)value, FieldType.Number);
    public static implicit operator FieldValue(bool value) => new((object)value, FieldType.Boolean);
    public static implicit operator FieldValue(float value) => (double)value;

    public override string ToString() => value.ToString()!;
}

public record Variable : Asset, IAssetRef<Variable>
{
    [JsonProperty("value")]
    public readonly FieldValue initialValue;

    public FieldType Type => initialValue.type;

    public Variable(string id, FieldValue value) : base(id)
    {
        this.initialValue = value;
    }

    [JsonIgnore]
    public new AssetLocation<Variable> Location => new(source, id);
    Variable? IAssetRef<Variable>.Resolve(IDiscoManager mgr) => (Variable?)((IAssetRef)this).Resolve(mgr);

    public override string ToString() => $"{source}.{id}";
}
