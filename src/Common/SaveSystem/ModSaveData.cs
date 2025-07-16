using System.Collections.Generic;
using BepInEx.Logging;
using DiscoAPI.Runtime.SaveSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscoAPI.Common.SaveSystem;

public class ModSaveData
{
    private static ManualLogSource Log => ModSaveSystem.Log;

    public int version { get; private set; }
    public string modGuid { get; private set; }
    [JsonProperty] private Dictionary<string, JToken> entries;

    public ModSaveData(string modGuid)
    {
        this.modGuid = modGuid;
        this.entries = new();
    }

    public bool KeyExists(string key) => entries.ContainsKey(key);

    public int GetInt(string key)
    {
        if (!entries.TryGetValue(key, out var value))
        {
            WarnIfKeyNotExists(key);
            return default;
        }
        return value.Type == JTokenType.Integer ? value.Value<int>() : default;
    }

    public string? GetString(string key)
    {
        if (!entries.TryGetValue(key, out var value))
        {
            WarnIfKeyNotExists(key);
            return default;
        }
        return value.Type == JTokenType.String ? value.Value<string>() : default;
    }

    public IList<T>? GetCollection<T>(string key)
    {
        if (!entries.TryGetValue(key, out var value))
        {
            WarnIfKeyNotExists(key);
            return default;
        }
        return value.Type == JTokenType.Array ? value.Value<List<T>>() : default;
    }

    public T? GetObject<T>(string key)
    {
        if (!entries.TryGetValue(key, out var value))
        {
            WarnIfKeyNotExists(key);
            return default;
        }
        return value.Type == JTokenType.Object ? value.Value<T>() : default;
    }

    public bool GetBool(string key)
    {
        if (!entries.TryGetValue(key, out var value))
        {
            WarnIfKeyNotExists(key);
            return default;
        }
        return value.Type == JTokenType.Boolean ? value.Value<bool>() : default;
    }

    public JToken? GetJson(string key)
    {
        if (!entries.TryGetValue(key, out var value))
        {
            WarnIfKeyNotExists(key);
            return default;
        }
        ;
        return value;
    }

    public void SetDataVersion(int newVersion)
    {
        version = newVersion;
    }

    public void SetInt(string key, int value)
    {
        WarnIfKeyExists(key);
        entries[key] = value;
    }

    public void SetString(string key, string value)
    {
        WarnIfKeyExists(key);
        entries[key] = value;
    }

    public void SetCollection<T>(string key, IList<T> value)
    {
        WarnIfKeyExists(key);
        entries[key] = JToken.FromObject(value);
    }

    public void SetObject<T>(string key, T obj)
    {
        WarnIfKeyExists(key);
        entries[key] = JToken.FromObject(obj!);
    }

    public void SetBool(string key, bool value)
    {
        WarnIfKeyExists(key);
        entries[key] = value;
    }

    public void SetJson(string key, JToken value)
    {
        WarnIfKeyExists(key);
        entries[key] = value;
    }

    private void WarnIfKeyExists(string key)
    {
        if (KeyExists(key))
        {
            Log.LogWarning($"({modGuid}): Key \"{key}\" already has a value, overwriting anyway.");
        }
    }

    private void WarnIfKeyNotExists(string key)
    {
        if (!KeyExists(key))
        {
            Log.LogWarning($"({modGuid}): Key \"{key}\" does not exist, returning default value.");
        }
    }
}
