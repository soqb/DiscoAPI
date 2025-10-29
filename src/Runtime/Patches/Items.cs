using System.Linq;
using DiscoAPI.Common.Assets;
using HarmonyLib;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime.Patches;

internal static class ItemPatches
{
    [HarmonyPatch(typeof(SM.InventoryItem), nameof(SM.InventoryItem.displayName), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool DisplayName_get(SM.InventoryItem __instance, ref string __result)
    {
        var modProject = DiscoRunner.manager.Assets.GetArena<Item>().FirstOrDefault(t => t.id == __instance.name);
        if (modProject == null) return true;
        
        __result = modProject.displayName;
        return false;
    }
    
    [HarmonyPatch(typeof(SM.InventoryItem), nameof(SM.InventoryItem.description), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool DescriptionName_get(SM.InventoryItem __instance, ref string __result)
    {
        var modProject = DiscoRunner.manager.Assets.GetArena<Item>().FirstOrDefault(t => t.id == __instance.name);
        if (modProject == null) return true;
        
        __result = modProject.description;
        return false;
    }
}