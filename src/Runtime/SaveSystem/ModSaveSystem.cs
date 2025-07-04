using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DiscoAPI.Common.SaveSystem;
using DiscoAPI.Runtime.SaveSystem.Serialization;
using Newtonsoft.Json;
using File = System.IO.File;
using Path = Il2CppSystem.IO.Path;

namespace DiscoAPI.Runtime.SaveSystem;

public class ModSaveSystem
{
    public Dictionary<string, ModSaveData>? modSaveDatas;
    public JsonSerializerSettings serializerSettings = new ()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Converters = [new SunshineSkillConverter()]
    };
    private Location _saveDataLocation;

    public ModSaveData GetModData(string modGuid)
    {
        if (modSaveDatas == null)
        {
            throw new InvalidOperationException(
                "ModSaveSystem: Retrieval of mod data called outside of save/load hook!");
        }

        if (modSaveDatas.TryGetValue(modGuid, out var data)) return data;
        
        var newData = new ModSaveData(modGuid);
        modSaveDatas.Add(modGuid, newData);
        return newData;
    }

    public void TriggerSaveEvent(string discoFilename)
    {
        modSaveDatas = new();
        _saveDataLocation = DiscoRunner.SourceFromPlugin(DiscoAPIPlugin.Instance).Location.Get($"SaveGames/{discoFilename}.json");
        foreach (var modSources in DiscoRunner.manager.linearSources)
        {
            var newData = new ModSaveData(modSources.Guid);
            modSaveDatas.Add(modSources.Guid, newData);
        }
        DiscoRunner.saveGame.Invoke();
        Task.Run(WriteSaveData);
    }

    public void TriggerLoadEvent(string discoFilename)
    {
        modSaveDatas = new();
        DiscoRunner.Log.LogInfo("Loading mod save data from " + discoFilename);
        _saveDataLocation = DiscoRunner.SourceFromPlugin(DiscoAPIPlugin.Instance).Location.Get($"SaveGames/{discoFilename}.json");
        modSaveDatas = LoadSaveData();
        DiscoRunner.loadSavedGame.Invoke();
        
        modSaveDatas = null;
        GC.Collect();
    }

    private Dictionary<string, ModSaveData> LoadSaveData()
    {
        if (File.Exists(_saveDataLocation))
        {
            var saveText = File.ReadAllText(_saveDataLocation!);
            var converted = JsonConvert.DeserializeObject<Dictionary<string, ModSaveData>>(saveText, serializerSettings);
            if (converted == null)
            {
                DiscoRunner.Log.LogError($"ModSaveSystem : Failed to deserialize valid data from {_saveDataLocation.path}");
            }
            else
            {
                return converted;
            }
        }

        return new();
    }

    private async Task WriteSaveData()
    {
        DiscoRunner.Log.LogInfo("Writing save data to disk...");
        Directory.CreateDirectory(Path.GetDirectoryName(_saveDataLocation));
        var json = JsonConvert.SerializeObject(modSaveDatas, Formatting.Indented, serializerSettings);
        await File.WriteAllTextAsync(_saveDataLocation!, json);
        modSaveDatas = null;
        GC.Collect();
    }
}