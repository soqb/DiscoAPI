using System;
using System.Collections.Generic;
using DiscoAPI.Common;
using DiscoAPI.Common.Dialogue;
using PC = PixelCrushers.DialogueSystem;

namespace DiscoAPI.Runtime.Dialogue;

public class DialogueManager : IDialogueManager
{
    public DiscoManager Parent { get; init; }
    IDiscoManager IDialogueManager.Parent => this.Parent;

    public DialogueMapping mapping;
    public PC.DialogueDatabase pcDatabase = null!; // this is properly initialised in `EnsureInitialized`.
    public Dictionary<string, PC.Asset> fakeArticyIDToAssetCache = new();

    public DialogueManager(DiscoManager parent)
    {
        Parent = parent;
        mapping = new(this);
    }
    public void EnsureInitialized()
    {
        pcDatabase = PC.DialogueManager.MasterDatabase;
        pcDatabase.SyncAll();
    }

    public static T FindAssetByID<T>(Il2CppSystem.Collections.Generic.List<T> assets, int searchID) where T : PC.Asset
        => FindByID<T>(assets, searchID - 1, (x) => x.id - 1); // assets are 1-indexed

    public static T FindByID<T>(Il2CppSystem.Collections.Generic.List<T> assets, int searchID, Func<T, int> getID)
        => assets[FindID<T>(assets, searchID, getID)];
    public static int FindID<T>(Il2CppSystem.Collections.Generic.List<T> assets, int searchID, Func<T, int> getID)
    {
        // This algorithm should hone in on the correct index for the id in as few loops as possible.
        // We need to do this because while ids are strictly ascending,
        // they do not necessarily correspond to the their index in the list.
        T asset;
        int guessedIdx = searchID;
        int distance = 0;
        for (int i = 0; i < 5; i++)
        {
            guessedIdx = assets.Count > guessedIdx ? guessedIdx >= 0 ? guessedIdx : 0 : assets.Count - 1;
            asset = assets[guessedIdx];

            distance = searchID - getID(asset);
            if (distance == 0) return guessedIdx;
            // positive if we undershot, negative if we overshot.
            guessedIdx += distance;
        }

        int direction = distance / Math.Abs(distance);
        do
        {
            guessedIdx -= direction;
            asset = assets[guessedIdx];
        } while (getID(asset) != searchID);

        return guessedIdx;
    }
    public static void ForAssetSlice<T>(Il2CppSystem.Collections.Generic.List<T> list, int startID, int sliceLength, Action<T> callback) where T : PC.Asset
    {
        int startIdx = DialogueManager.FindID<T>(list, startID - 1, (x) => x.id - 1);
        for (int i = startIdx; i < startIdx + sliceLength; i++)
        {
            if (i % 100 == 0)
            callback(list[i]);
        }
    }
}
