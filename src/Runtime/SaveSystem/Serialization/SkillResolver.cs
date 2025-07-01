using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime.SaveSystem.Serialization;

public class IgnoreMemberContractResolver<T> : DefaultContractResolver
{
    private readonly string[] ignoredMembers;

    public IgnoreMemberContractResolver(params string[] members)
    {
        ignoredMembers = members;
    }
    
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        if (type != typeof(T)) return base.CreateProperties(type, memberSerialization);

        List<JsonProperty> properties = [];
        foreach (var member in type.GetMembers())
        {
            var prop = base.CreateProperty(member, memberSerialization);
            bool include = ignoredMembers.Contains(prop.PropertyName);
            prop.ShouldSerialize = _ => include;
            prop.ShouldDeserialize = _ => include;
            properties.Add(prop);
        }

        return properties;
    }
}

public class SunshineSkillConverter : JsonConverter<SM.Skill>
{
    // tried implementing using a ContractResolver and a Converter using reflection --
    // manual field assignment ended up being less error-prone and faster
    public override void WriteJson(JsonWriter writer, SM.Skill? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        
        JObject o = new JObject
        {
            { "skillType", (int)value.skillType },
            { "abilityType", (int)value.abilityType },
            { "dirty", value.dirty },
            { "value", value.value },
            { "valueWithoutPerceptionsSubSkills", value.valueWithoutPerceptionsSubSkills },
            { "damageValue", value.damageValue },
            { "maximumValue" , value.maximumValue },
            { "calculatedAbility", value.calculatedAbility },
            { "rankValue", value.rankValue },
            { "hasAdvancement", value.hasAdvancement },
            { "isSignature", value.isSignature }
        };
        
        o.WriteTo(writer);
    }

    public override SM.Skill? ReadJson(JsonReader reader, Type objectType, SM.Skill? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        return new SM.Skill((SM.SkillType)ReadInt(reader), DiscoRunner.world!.You)
        {
            abilityType = (SM.AbilityType)ReadInt(reader),
            dirty = reader.ReadAsBoolean() ?? false,
            value = ReadInt(reader),
            valueWithoutPerceptionsSubSkills = ReadInt(reader),
            damageValue = ReadInt(reader),
            maximumValue = ReadInt(reader),
            calculatedAbility = ReadInt(reader),
            rankValue = ReadInt(reader),
            hasAdvancement = reader.ReadAsBoolean() ?? false,
            isSignature = reader.ReadAsBoolean() ?? false
        };
    }
    
    private static int ReadInt(JsonReader reader) => reader.ReadAsInt32() ?? 0;
}
