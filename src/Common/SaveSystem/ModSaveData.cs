using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DiscoAPI.Common.SaveSystem;

public class ModSaveData
{
    public int version { get; private set; }
    private Dictionary<string, JToken> entries;

    public bool KeyExists(string key) => entries.ContainsKey(key);

    public int GetInt(string key)
    {
        if (!entries.TryGetValue(key, out var value)) return default;
        return value.Type == JTokenType.Integer ? value.Value<int>() : default;
    }

    public string? GetString(string key)
    {
        if (!entries.TryGetValue(key, out var value)) return default;
        return value.Type == JTokenType.String ? value.Value<string>() : default;
    }

    public IList<T>? GetCollection<T>(string key)
    {
        if (!entries.TryGetValue(key, out var value)) return default;
        return value.Type == JTokenType.Array ? value.Value<List<T>>() : default;
    }

    public T? GetObject<T>(string key)
    {
        if (!entries.TryGetValue(key, out var value)) return default;
        return value.Type == JTokenType.Object ? value.Value<T>() : default;
    }

    public bool GetBool(string key)
    {
        if (!entries.TryGetValue(key, out var value)) return default;
        return value.Type == JTokenType.Boolean ? value.Value<bool>() : default;
    }

    public JToken? GetJson(string key)
    {
        return entries.GetValueOrDefault(key);
    }

    public void SetDataVersion(int newVersion)
    {
        version = newVersion;
    }

    public void SetInt(string key, int value)
    {
        entries[key] = value;
    }

    public void SetString(string key, string value)
    {
        entries[key] = value;
    }

    public void SetCollection<T>(string key, IList<T> value)
    {
        entries[key] = JToken.FromObject(value);
    }

    public void SetObject<T>(string key, T obj)
    {
        entries[key] = JToken.FromObject(obj!);
    }

    public void SetBool(string key, bool value)
    {
        entries[key] = value;
    }

    public void SetJson(string key, JToken value)
    {
        entries[key] = value;
    }
}