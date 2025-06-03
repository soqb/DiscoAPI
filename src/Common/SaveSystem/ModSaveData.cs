using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DiscoAPI.Common.SaveSystem;

public class ModSaveData
{
    public readonly int version;
    private Dictionary<string, JToken> entries;

    public int RetrieveInt(string key)
    {
        if (!entries.TryGetValue(key, out var value)) return default;
        return value.Type == JTokenType.Integer ? value.Value<int>() : default;
    }

    public string? RetrieveString(string key)
    {
        if (!entries.TryGetValue(key, out var value)) return default;
        return value.Type == JTokenType.String ? value.Value<string>() : default;
    }

    public T[]? RetrieveArray<T>(string key)
    {
        if (!entries.TryGetValue(key, out var value)) return default;
        return value.Type == JTokenType.Array ? value.Value<T[]>() : default;
    }

    public T? RetrieveObject<T>(string key)
    {
        if (!entries.TryGetValue(key, out var value)) return default;
        return value.Type == JTokenType.Object ? value.Value<T>() : default;
    }

    public bool RetrieveBool(string key)
    {
        if (!entries.TryGetValue(key, out var value)) return default;
        return value.Type == JTokenType.Boolean ? value.Value<bool>() : default;
    }

    public JToken? RetrieveJson(string key)
    {
        return entries.GetValueOrDefault(key);
    }
}