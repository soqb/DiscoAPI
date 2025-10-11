using DiscoAPI.Runtime.Utils;
using HarmonyLib;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime.Patches;

public static class ThoughtPatches
{
    [HarmonyPatch(typeof(SM.CharacterThoughts), nameof(SM.CharacterThoughts.RegisterThought))]
    [HarmonyPrefix]
    public static bool OnRegisterThought(SM.ThoughtCabinetProject project, SM.CharacterThoughts __instance)
    {
        if (!__instance.cookingEffects.ContainsKey(project))
        {
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
                    var modEffect = ModifierUtils.Lookup(effect.effect);
                    //if (modEffect?.applyEffect != null) modEffect.applyEffect();
                }
            }
            SM.CharacterThoughts.FireTHCOnChangeEvent();
        }

        return false;
    }
}