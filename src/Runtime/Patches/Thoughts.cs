using System.Linq;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Dialogue;
using DiscoAPI.Runtime.Utils;
using DiscoPages.Elements.THC;
using HarmonyLib;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime.Patches;

public static class ThoughtPatches
{
    [HarmonyPatch(typeof(SM.CharacterThoughts), nameof(SM.CharacterThoughts.RegisterThought))]
    [HarmonyPrefix]
    private static bool OnRegisterThought(SM.ThoughtCabinetProject project, SM.CharacterThoughts __instance)
    {
        if (__instance.cookingEffects.ContainsKey(project)) return false;
        
        var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.displayName == project.displayName);
        if (modProject == null) return true;
        
        SM.CharacterEffect[] researchEffects = project.researchEffects;
        __instance.cookingEffects.Add(project, researchEffects);
        project.state = SM.ThoughtState.COOKING;
        SM.CharacterEffect[] array = researchEffects;
        for (int i = 0; i < array.Length; i++)
        {
            var effect = array[i];
            if (ModifierUtils.EffectIsVanilla(effect.effect))
            {
                effect.Apply(__instance.characterSheet, SM.ModifierType.THC, project.Cast<IModifierCause>());
            }
            else
            {
                modProject?.researchEffects[i].baseEffect.Resolve()?.applyEffect?.Invoke(modProject.researchEffects[i]);
            }
        }
        SM.CharacterThoughts.FireTHCOnChangeEvent();

        return false;
    }

    [HarmonyPatch(typeof(SM.CharacterThoughts), nameof(SM.CharacterThoughts.UnregisterFixedThought))]
    [HarmonyPrefix]
    private static bool OnUnregisterFixedThought(SM.ThoughtCabinetProject project, SM.CharacterThoughts __instance, ref bool __result)
    {
        if (!__instance.fixedEffects.ContainsKey(project))
        {
            __result = false;
            return false;
        }
        
        var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.displayName == project.displayName);
        if (modProject == null) return true;
        
        SM.CharacterEffect[] array = __instance.fixedEffects[project];
        for (int i = 0; i < array.Length; i++)
        {
            var effect = array[i];
            if (ModifierUtils.EffectIsVanilla(effect.effect))
            {
                array[i].Remove(__instance.characterSheet, SM.ModifierType.THC, project.Cast<IModifierCause>());
            }
            else
            {
                modProject?.completionEffects[i].baseEffect.Resolve()?.removeEffect?.Invoke(modProject.completionEffects[i]);
            }
        }
        __instance.fixedEffects.Remove(project);
        SM.CharacterThoughts.FireTHCOnChangeEvent();

        __result = true;
        return false;
    }
    
    [HarmonyPatch(typeof(SM.CharacterThoughts), nameof(SM.CharacterThoughts.UnregisterCookingThought))]
    [HarmonyPrefix]
    private static bool OnUnregisterCookingThought(SM.ThoughtCabinetProject project, SM.CharacterThoughts __instance, ref bool __result)
    {
        if (!__instance.cookingEffects.ContainsKey(project))
        {
            __result = false;
            return false;
        }
        
        var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.displayName == project.displayName);
        if (modProject == null) return true;
        
        SM.CharacterEffect[] array = __instance.cookingEffects[project];
        for (int i = 0; i < array.Length; i++)
        {
            var effect = array[i];
            if (ModifierUtils.EffectIsVanilla(effect.effect))
            {
                array[i].Remove(__instance.characterSheet, SM.ModifierType.THC, project.Cast<IModifierCause>());
            }
            else
            {
                modProject?.researchEffects[i].baseEffect.Resolve()?.removeEffect?.Invoke(modProject.researchEffects[i]);
            }
        }
        __instance.cookingEffects.Remove(project);
        SM.CharacterThoughts.FireTHCOnChangeEvent();

        __result = true;
        return false;
    }
    
    [HarmonyPatch(typeof(SM.CharacterThoughts), nameof(SM.CharacterThoughts.FixThought))]
    [HarmonyPrefix]
    private static bool OnFixThought(SM.ThoughtCabinetProject project, SM.CharacterThoughts __instance)
    {
        var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.displayName == project.displayName);
        if (modProject == null) return true;
        
        SM.CharacterEffect[] completionEffects = project.completionEffects;
        __instance.fixedEffects.Add(project, completionEffects);
        project.state = SM.ThoughtState.FIXED;
        SM.CharacterEffect[] array = completionEffects;
        for (int i = 0; i < array.Length; i++)
        {
            var effect = array[i];
            if (ModifierUtils.EffectIsVanilla(effect.effect))
            {
                effect.Apply(__instance.characterSheet, SM.ModifierType.THC, project.Cast<IModifierCause>());
            }
            else
            {
                modProject?.completionEffects[i].baseEffect.Resolve()?.applyEffect?.Invoke(modProject.completionEffects[i]);
            }
        }
        SM.CharacterThoughts.FireTHCOnChangeEvent();

        return false;
    }

    [HarmonyPatch(typeof(Sunshine.ThoughtSlot), nameof(Sunshine.ThoughtSlot.FindAndSetThoughtImage))]
    [HarmonyPrefix]
    private static bool OnFindSetThoughtImage(Sunshine.ThoughtSlot __instance)
    {
        var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.displayName == __instance.Project.displayName);
        if (modProject == null) return true;

        var task = AssetUtils.LoadPortrait(DiscoToPixels.EncodeTextureName(modProject.source!, modProject.imageLocation), null);
        task.ContinueWith(handle =>
        {
            __instance._thoughtProjectImage.sprite = handle.Result;
            __instance.RefreshImage();
            __instance._thoughtProjectImage.enabled = false;
            __instance._thoughtProjectImage.enabled = true;
        });
        return false;
    }
    
    [HarmonyPatch(typeof(PageSystemThoughtSlot), nameof(PageSystemThoughtSlot.FindAndSetThoughtImage))]
    [HarmonyPrefix]
    private static bool OnFindSetThoughtImage(PageSystemThoughtSlot __instance)
    {
        var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.displayName == __instance.Project.displayName);
        if (modProject == null) return true;

        var task = AssetUtils.LoadPortrait(DiscoToPixels.EncodeTextureName(modProject.source!, modProject.imageLocation), null);
        task.ContinueWith(handle =>
        {
            __instance._thoughtProjectImage.sprite = handle.Result;
            __instance.RefreshImage();
            __instance._thoughtProjectImage.enabled = false;
            __instance._thoughtProjectImage.enabled = true;
        });
        return false;
    }
    
    [HarmonyPatch(typeof(PageSystemThoughtOceanSlot), nameof(PageSystemThoughtOceanSlot.FindAndSetThoughtImage))]
    [HarmonyPrefix]
    private static bool OnFindSetThoughtImage(PageSystemThoughtOceanSlot __instance)
    {
        var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.displayName == __instance.Project.displayName);
        if (modProject == null) return true;

        var task = AssetUtils.LoadPortrait(DiscoToPixels.EncodeTextureName(modProject.source!, modProject.imageLocation), null);
        task.ContinueWith(handle =>
        {
            __instance._thoughtProjectImage.sprite = handle.Result;
            __instance.RefreshImage();
            __instance._thoughtProjectImage.enabled = false;
            __instance._thoughtProjectImage.enabled = true;
        });
        return false;
    }
}