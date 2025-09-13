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