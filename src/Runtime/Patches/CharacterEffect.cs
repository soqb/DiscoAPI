using System.Linq;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Utils;
using HarmonyLib;
using Voidforge;
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

    [HarmonyPatch(typeof(SM.CharacterEffect), nameof(SM.CharacterEffect.EffectName))]
    [HarmonyPostfix]
    public static void OnGetEffectName(ref string __result, SM.CharacterEffect __instance)
    {
        if (ModifierUtils.EffectIsVanilla(__instance.effect)) return;
        var modEffect = ModifierUtils.Lookup(__instance.effect);

        Skill? modSkill = null;
        if (__instance.skillType != SM.SkillType.ALT || __instance.skillType != SM.SkillType.NONE)
        {
            modSkill = SkillUtils.Lookup(__instance.skillType);
        }

        AbilityType? modAbility = null;
        if (__instance.abilityType != SM.AbilityType.Error)
        {
            modAbility = SkillUtils.AbilityFromSunshine(__instance.abilityType);
        }
        
        __result = modEffect?.effectName?.Invoke(new Modifier(__instance.quipLine, modEffect, __instance.stringParameter,
            __instance.parameter, modSkill, modAbility)) ?? "";
    }
    
    [HarmonyPatch(typeof(CharacterSheetPersister), nameof(CharacterSheetPersister.ApplyEffects))]
    [HarmonyPrefix]
    private static bool OnApplyEffects(SM.CharacterEffect[] effects, SM.ModifierType type, IModifierCause cause)
    {
        var maybeTcp = cause.TryCast<SM.ThoughtCabinetProject>();
        var maybeItem = cause.TryCast<SM.InventoryItem>();
        if (maybeItem == null && maybeTcp == null) return true;
        
        SM.CharacterSheet you = SingletonComponent<World>.Singleton.you;
        
        if (maybeTcp != null)
        {
            var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id== maybeTcp.name);
            if (modProject == null) return true;

            if (you.thoughts.ThoughtCooking(maybeTcp))
            {
                for (var i = 0; i < modProject.researchEffects.Length; i++)
                {
                    var modifier = modProject.researchEffects[i];
                    var discoEffect = maybeTcp.researchEffects[i];
                    if (discoEffect == null) continue;
                    
                    var persists = ModifierUtils.EffectIsVanilla(discoEffect.effect)
                        ? CharacterSheetPersister.ThoughtEffectShouldPersist(discoEffect)
                        : modifier.baseEffect.Resolve()?.isSavePersistent ?? true;
                    if (persists)
                    {
                        modifier.baseEffect.Resolve()?.applyEffect?.Invoke(modifier);
                    }
                }
            } else if (you.thoughts.ThoughtFixed(maybeTcp))
            {
                for (var i = 0; i < modProject.completionEffects.Length; i++)
                {
                    var modifier = modProject.completionEffects[i];
                    var discoEffect = maybeTcp.completionEffects[i];
                    if (discoEffect == null) continue;
                    
                    var persists = ModifierUtils.EffectIsVanilla(discoEffect.effect)
                        ? CharacterSheetPersister.ThoughtEffectShouldPersist(discoEffect)
                        : modifier.baseEffect.Resolve()?.isSavePersistent ?? true;
                    if (persists)
                    {
                        modifier.baseEffect.Resolve()?.applyEffect?.Invoke(modifier);
                    }
                }
            }
        }

        if (maybeItem != null)
        {
            var modItem = DiscoRunner.manager.Assets.GetArena<Item>().FirstOrDefault(t => t.id == maybeItem.name);
            if (modItem is not EquippableItem equippable) return true;

            if (equippable.equipEffects == null) return false;
            for (int i = 0; i < equippable.equipEffects.Length; i++)
            {
                var modifier = equippable.equipEffects[i];
                var discoEffect = maybeItem.equipEffects[i];
                if (discoEffect == null) continue;
                
                var persists = ModifierUtils.EffectIsVanilla(discoEffect.effect)
                    ? CharacterSheetPersister.ThoughtEffectShouldPersist(discoEffect)
                    : modifier.baseEffect.Resolve()?.isSavePersistent ?? true;
                if (persists)
                {
                    modifier.baseEffect.Resolve()?.applyEffect?.Invoke(modifier);
                }
            }
        }

        return false;
    }
    
}