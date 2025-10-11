using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Utils;
using HarmonyLib;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime.Patches;

public static class CharacterEffectPatches
{
    // Other patches will ensure the effect is vanilla when this is invoked, but better to be safe
    [HarmonyPatch(typeof(SM.CharacterEffect), nameof(SM.CharacterEffect.Apply))]
    [HarmonyPrefix]
    public static bool OnEffectApply(SM.CharacterSheet ch, SM.ModifierType type, IModifierCause modifierCause, SM.CharacterEffect __instance)
    {
        if (ModifierUtils.EffectIsVanilla(__instance.effect)) return true;
        return false;
    }
    
    [HarmonyPatch(typeof(SM.CharacterEffect), nameof(SM.CharacterEffect.Remove))]
    [HarmonyPrefix]
    public static bool OnEffectRemove(SM.CharacterSheet ch, SM.ModifierType type, IModifierCause modifierCause, SM.CharacterEffect __instance)
    {
        if (ModifierUtils.EffectIsVanilla(__instance.effect)) return true;
        return false;
    }
    
}