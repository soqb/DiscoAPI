using System;
using System.Collections.Generic;
using DiscoAPI.Common.SaveSystem;
using Newtonsoft.Json;
using File = System.IO.File;

namespace DiscoAPI.Runtime.SaveSystem;

public class ModSaveSystem
{
    private Dictionary<string, ModSaveData>? modSaveDatas;
    private string? _discoFilename;

    public ModSaveSystem()
    {
        DiscoHooks.OnLoadSavedGame += LoadSaveGameModData;
        DiscoHooks.OnSaveGame += SaveGameModData;
    }

    public ModSaveData GetModData(string modGuid)
    {
        if (modSaveDatas == null)
        {
            throw new InvalidOperationException(
                "ModSaveSystem: Retrieval of mod data called outside of save/load hook!");
        }

        return modSaveDatas.TryGetValue(modGuid, out var data) ? data : new ModSaveData();
    }

    private void LoadSaveGameModData(string discoFilename)
    {
        _discoFilename = discoFilename;
    }

    private void SaveGameModData(string discoFilename)
    {
        modSaveDatas = new();
        _discoFilename = discoFilename;
    }

    private void WriteSaveData()
    {
        var json = JsonConvert.SerializeObject(modSaveDatas);
        File.WriteAllTextAsync($"SaveGames/{_discoFilename}.json", json);
        
        modSaveDatas = null;
        DiscoHooks.OnLoadSavedGame -= LoadSaveGameModData;
        DiscoHooks.OnSaveGame -= SaveGameModData;
    }
}