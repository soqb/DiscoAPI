using System.Linq;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Utils;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Sunshine;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime.Patches;

internal static class ItemPatches
{
    [HarmonyPatch(typeof(SM.InventoryItem), nameof(SM.InventoryItem.displayName), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool DisplayName_get(SM.InventoryItem __instance, ref string __result)
    {
        var modItem = DiscoRunner.manager.Assets.GetArena<Item>().FirstOrDefault(t => t.id == __instance.name);
        if (modItem == null) return true;
        
        __result = modItem.displayName;
        return false;
    }
    
    [HarmonyPatch(typeof(InventoryTooltip), nameof(InventoryTooltip.PrimeItem))]
    [HarmonyPostfix] 
    // tried transpiling this, did not work. here be the consequences.
    // an error will still be logged about no lockit for the mod item.
    // methodology question: should i reimpl the whole method to avoid the error showing, or only patch the fix for simplicity but leave an irrelevant error?
    private static void OnPrimeItem(SM.InventoryItem item, InventoryTooltip __instance)
    {
        var modItem = DiscoRunner.manager.Assets.GetArena<Item>().FirstOrDefault(t => t.id == item.name);
        if (modItem == null) return;
        __instance.title.text = item.displayName.ToUpper();
    }
    
    [HarmonyPatch(typeof(SM.InventoryItem), nameof(SM.InventoryItem.description), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool DescriptionName_get(SM.InventoryItem __instance, ref string __result)
    {
        var modItem = DiscoRunner.manager.Assets.GetArena<Item>().FirstOrDefault(t => t.id == __instance.name);
        if (modItem == null) return true;
        
        __result = modItem.description;
        return false;
    }

    [HarmonyPatch(typeof(SM.InventoryItem), nameof(SM.InventoryItem.substanceTimeLeft), MethodType.Setter)]
    [HarmonyPostfix]
    private static void SubstanceTimeLeft_set(SM.InventoryItem __instance, int value)
    {
        __instance._substanceTimeLeft = value >= 0 ? value : 0;
    }

    [HarmonyPatch(typeof(HudHeldPanelController), nameof(HudHeldPanelController.UseSubstance))]
    [HarmonyPostfix]
    private static void OnUseSubstance(SM.InventoryItem item)
    {
        var modItem = DiscoRunner.manager.Assets.GetArena<Item>().FirstOrDefault(t => t.id == item.name);
        if (modItem is SubstanceItem substance) item.substanceTimeLeft = substance.effectDuration;
    }

    [HarmonyPatch(typeof(HudHeldButton), nameof(HudHeldButton.UpdateButton))]
    [HarmonyPostfix]
    private static void OnUpdateButton(SM.InventoryItem heldItem, HudHeldButton __instance)
    {
        if (heldItem == null) return;
        var modItem = DiscoRunner.manager.Assets.GetArena<Item>().FirstOrDefault(t => t.id == heldItem.name);
        if (modItem is EquippableItem { heldIconLocation: not null } equipItem)
        {
            var getIconTask = DiscoRunner.manager.GetSource(modItem.source!)!.Router.Sprites.Get(equipItem.heldIconLocation);
            getIconTask.ContinueWith(handle =>
            {
                __instance.heldItemIcon.overrideSprite = handle.Result;
            });
        }
    }

    [HarmonyPatch(typeof(InventoryItemsBundleManager), nameof(InventoryItemsBundleManager.LoadItemSpriteAsync))]
    [HarmonyPrefix]
    private static bool OnLoadItemSpriteAsync(string itemName, Il2CppSystem.Action<AsyncOperationHandle<Sprite>>? del, ref AsyncOperationHandle<Sprite> __result)
    {
        var modItem = DiscoRunner.manager.Assets.GetArena<Item>().FirstOrDefault(t => t.id == itemName);
        if (modItem == null) return true;
    
        bool isBigIcon = itemName.EndsWith("_big");
        
        var loadTask = DiscoRunner.manager.GetSource(modItem.source!)!.Router.Sprites.Get(isBigIcon ? modItem.bigImageLocation : modItem.iconImageLocation);
        __result = loadTask.AsAddressableOperation().Handle;
        if (del != null) loadTask.AsAddressableOperation().add_Completed(del!);
        return false;
    }
    
    [HarmonyPatch(typeof(InventoryItemsBundleManager), nameof(InventoryItemsBundleManager.LoadItemSprite))]
    [HarmonyPrefix]
    private static bool OnLoadItemSprite(string itemName, Il2CppSystem.Action<Sprite>? del, ref AsyncOperationHandle<Sprite> __result)
    {
        var modItem = DiscoRunner.manager.Assets.GetArena<Item>().FirstOrDefault(t => t.id == itemName);
        if (modItem == null) return true;

        bool isBigIcon = itemName.EndsWith("_big");
        
        var loadTask = DiscoRunner.manager.GetSource(modItem.source!)!.Router.Sprites.Get(isBigIcon ? modItem.bigImageLocation : modItem.iconImageLocation);
        __result = loadTask.AsAddressableOperation().Handle;
        if (del != null)
        {
            loadTask.ContinueWith(handle => { del.Invoke(handle.Result!); });
        }
        return false;
    }
    
    [HarmonyPatch(typeof(InventoryItemsBundleManager), nameof(InventoryItemsBundleManager.ItemIconExists))]
    [HarmonyPrefix]
    private static bool OnItemIconExists(string itemName, ref bool __result)
    {
        var modItem = DiscoRunner.manager.Assets.GetArena<Item>().FirstOrDefault(t => t.id == itemName);
        if (modItem != null)
        {
            __result = true;
            return false;
        };
        return true;
    }

    [HarmonyPatch(typeof(Addressables), nameof(Addressables.InstantiateAsync), [typeof(Il2CppSystem.Object), typeof(Transform), typeof(bool), typeof(bool)])]
    [HarmonyPrefix] // this is the only call to InstantiateAsync in DE, somehow
    private static bool OnInstantiateItemAsync(object key, ref AsyncOperationHandle<GameObject> __result)
    {
        if (key is not string itemName) return true;
        
        var modItem = DiscoRunner.manager.Assets.GetArena<Item>().FirstOrDefault(t => t.id == itemName);
        if (modItem is not EquippableItem equippable) return true;
        
        var loadTask = DiscoRunner.manager.GetSource(modItem.source!)!.Router.Prefabs.Get(equippable.itemPrefabLocation);
        __result = loadTask.AsAddressableOperation().Handle;
        return false;
    }

    private static Il2CppSystem.Reflection.MethodInfo SetPortraitMethod = Il2CppType.Of<InventoryTooltip>()
        .GetMethod(nameof(InventoryTooltip.SetPortrait), Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance);
    
    [HarmonyPatch(typeof(InventoryTooltip), nameof(InventoryTooltip.LoadSpriteAsync))]
    [HarmonyPrefix]
    private static bool OnLoadAssetAsync(SM.InventoryItem item, InventoryTooltip __instance)
    {
        if (item == null) return true;
        var modItem = DiscoRunner.manager.Assets.GetArena<Item>().FirstOrDefault(t => t.id == item.name);
        if (modItem == null) return true;

        if (!modItem.id.Equals(__instance.prevItemName))
        {
            __instance.UnloadPortraitAsync();
            __instance.prevItemName = modItem.id;
            __instance.bigItemImage.enabled = false;
            __instance.bigItemImageGlow.enabled = false;
            __instance.bigItemImageGhostFrame.enabled = false;
            var loadTask = DiscoRunner.manager.GetSource(modItem.source!)!.Router.Sprites.Get(modItem.bigImageLocation);
            var del = SetPortraitMethod.CreateDelegate(Il2CppType.Of<Il2CppSystem.Action<AsyncOperationHandle<Sprite?>>>(), __instance);
            var cb = del.Cast<Il2CppSystem.Action<AsyncOperationHandle<Sprite>>>();
            __instance.spriteHandle = loadTask.AsAddressableOperation().Handle;
            __instance.spriteHandle.add_Completed(cb);
            __instance.isAsyncPrepared = true;
        }
        return false;
    }
}