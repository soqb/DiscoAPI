using System.Linq;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Utils;
using HarmonyLib;
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

    [HarmonyPatch(typeof(Addressables), nameof(Addressables.InstantiateAsync))]
    [HarmonyPrefix] // this is the only call to InstantiateAsync in DE, somehow
    private static bool OnInstantiateItemAsync(object key, ref AsyncOperationHandle<GameObject>? __result)
    {
        if (key is not string itemName) return true;
        
        var modItem = DiscoRunner.manager.Assets.GetArena<Item>().FirstOrDefault(t => t.id == itemName);
        if (modItem is not EquippableItem equippable) return true;
        
        var loadTask = DiscoRunner.manager.GetSource(modItem.source!)!.Router.Prefabs.Get(equippable.itemPrefabLocation);
        __result = loadTask.AsAddressableOperation().Handle;
        return false;
    }
}