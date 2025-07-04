using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DiscoAPI.Common.SaveSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using File = System.IO.File;

namespace DiscoAPI.Runtime.SaveSystem;

public interface ISaveSerializable
{
    JToken Serialize();
}

public interface ISaveSerializable<T, in C> : ISaveSerializable where T : ISaveSerializable<T, C>
{
    bool TryDeserialize(JToken token, C context);
}
public interface ISaveSerializable<T> : ISaveSerializable<T, object?> where T : ISaveSerializable<T>
{
    bool TryDeserialize(JToken token);
    bool ISaveSerializable<T, object?>.TryDeserialize(JToken token, object? context)
    {
        return TryDeserialize(token);
    }
}

public enum SaveLoadState
{
    Saving,
    Loading,
    Neither,
}

public class ModSaveSystem
{
    public JsonSerializerSettings serializerSettings = new ()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Converters = []
    };
    
    private Location _saveDataLocation;
    public readonly object saveLock = new();
    public Dictionary<string, ModSaveData> modSaveDatas = new();
    private SaveLoadState state = SaveLoadState.Neither;

    public static MethodInfo? FindDeserializer(Type target, Type? context)
    {
        Type iface = typeof(ISaveSerializable<,>).MakeGenericType(target, context ?? typeof(object));
        if (!target.IsAssignableTo(iface)) return null;

        return iface?.GetMethod("TryDeserialize", BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
    }

    public static bool TryDeserializeUntyped(Type type, JToken token, object? context, object result)
    {
        if (FindDeserializer(type, context?.GetType()) is MethodInfo method)
        {
            return (bool)method.Invoke(result, new object?[] { token, context })!;
        }
        else
        {
            return false;
        }
    }

    public ModSaveData GetModData(string modGuid)
    {
        if (state == SaveLoadState.Neither)
            throw new InvalidOperationException(
                "ModSaveSystem: Retrieval of mod data called outside of save/load hook!");


        if (state != SaveLoadState.Saving && modSaveDatas.TryGetValue(modGuid, out var data)) return data;

        // if saving, return a reference to blank data since we want to completely overwrite.
        var newData = new ModSaveData(modGuid);
        modSaveDatas[modGuid] = newData;
        return newData;
    }

    public string GetPath(string filename) => System.IO.Path.Join(BepInEx.Paths.GameRootPath, "SaveGames", filename + ".json");

    public void TriggerSaveEvent(string discoFilename)
    {
        bool taken = false;
        try
        {
            Monitor.Enter(saveLock, ref taken);
            state = SaveLoadState.Saving;
            DiscoRunner.saveGame.Invoke(this);
            Task.Run(async () =>
            {
                try
                {
                    await WriteSaveData(GetPath(discoFilename));
                    state = SaveLoadState.Neither;
                }
                finally
                {
                    if (taken) Monitor.Exit(saveLock);
                    taken = false;

                }
                GC.Collect();
            });
        }
        finally
        {
            if (taken) Monitor.Exit(saveLock);
            taken = false;
        }
    }

    public void TriggerLoadEvent(string discoFilename)
    {
        lock (saveLock)
        {

            state = SaveLoadState.Loading;
            modSaveDatas = LoadSaveData(GetPath(discoFilename));
            DiscoRunner.loadSavedGame.Invoke(this);
            state = SaveLoadState.Neither;
        }
        GC.Collect();
    }

    private Dictionary<string, ModSaveData> LoadSaveData(string path)
    {
        if (File.Exists(path))
        {
            var saveText = File.ReadAllText(path!);
            var converted = JsonConvert.DeserializeObject<Dictionary<string, ModSaveData>>(saveText);
            if (converted == null)
            {
                DiscoRunner.Log.LogError($"ModSaveSystem : Failed to deserialize valid data from {path}");
            }
            else
            {
                return converted;
            }
        }

        return new();
    }

    private async Task WriteSaveData(string path)
    {
        DiscoRunner.Log.LogInfo("Writing save data to disk...");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonConvert.SerializeObject(modSaveDatas, Formatting.Indented, serializerSettings);
        await File.WriteAllTextAsync(path, json);
    }
}
