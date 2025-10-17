using System;
using DiscoAPI.Runtime.Dialogue;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DiscoAPI.Runtime.Utils;

public static class AssetUtils
{
    public const string EXTRA_TEXTURE_PREFIX = "\0EXTRA\0";

    public async static DiscoTask<Sprite> LoadPortrait(string textureName,
        Il2CppSystem.Action<AsyncOperationHandle<Sprite>>? del)
    {
        string hint = textureName;
        DiscoTask<Sprite?> task;
        try
        {
            if (PixelsToDisco.TryDecodeTextureName(textureName, out string source, out string path))
            {
                task = DiscoRunner.GetSource(source)!.Router.Portraits.Get(path);
                hint = $"{source}:{path}";
                if (await task is Sprite ss)
                {
                    if (del != null) task.AsAddressableOperation().add_Completed(del!);
                    return ss;
                }
            }
            else if (await ActorsPortraitsBundleManager.LoadPortraitSpriteAsync(textureName, del) is Sprite ss)
            {
                return ss;
            }
        }
        catch (Exception ex)
        {
            DiscoRunner.Log.LogError($"failed to load portrait \"{hint}\"");
            DiscoRunner.Log.LogError(ex);
        }

        // if everything went wrong, load fallback portrait:
        const string FALLBACK = "missing_texture_lol.png";
        task = InherentProvider.source.Router.Portraits.Get(FALLBACK)!;
        if (await task is Sprite s)
        {
            if (del != null) task.AsAddressableOperation().add_Completed(del!);
            return s;
        }

        // at this point there's nothing else we can do..
        throw new Exception("failed to load even fallback portrait");
    }
}

internal static class ReputationUtils
{
    public const int VANILLA_REP_DIALOGUE_COUNT = 15;

    public static bool RepIsReal(Reputation rep) => true;

    public static Common.Assets.Reputation RecoverRep(Reputation smRep)
    {
        bool repHasLevel = ReputationAlterant.reputationSystemIndividualLevels.TryGetValue(smRep, out int repLevel);
        return new Common.Assets.Reputation(
            smRep.ToString(), null, 
            repHasLevel ? repLevel : null, 
            ReputationAlterant.orbDialogues[(int)smRep]);
    }
    
    public static void RegisterModReputations()
    {
        var repArena = DiscoRunner.manager.Assets.GetArena<Common.Assets.Reputation>();
        int repArenaLength = repArena.Count;
        var confrontDialogues =
            ReputationAlterant.orbDialogues.Resize(ReputationAlterant.orbDialogues.Length + repArenaLength);

        for (int i = VANILLA_REP_DIALOGUE_COUNT; i < repArenaLength; i++)
        {
            var modRep = repArena[i];
            confrontDialogues[i] = modRep?.confrontationOrbName ?? "";
            ReputationAlterant.reputationSystemIndividualLevels.TryAdd((Reputation)modRep.ResolveId(),
                modRep.confrontationTriggerThreshold ?? 999);
        }

        ReputationAlterant.orbDialogues = confrontDialogues;
    }
}