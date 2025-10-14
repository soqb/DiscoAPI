using System.Linq;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Utils;
using HarmonyLib;
using Sunshine;
using TMPro;
using UnityEngine;
using Voidforge;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime.Patches;

public static class ThoughtPatches
{
    [HarmonyPatch(typeof(SM.CharacterThoughts), nameof(SM.CharacterThoughts.RegisterThought))]
    [HarmonyPrefix]
    private static bool OnRegisterThought(SM.ThoughtCabinetProject project, SM.CharacterThoughts __instance)
    {
        if (__instance.cookingEffects.ContainsKey(project)) return false;
        
        var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id == project.name);
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
        
        var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id == project.name);
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
        
        var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id == project.name);
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
        var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id == project.name);
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
    
    [HarmonyPatch(nameof(SpriteManager), nameof(SpriteManager.GetSprite))]
    [HarmonyPrefix]
    public static bool OnGetSprite(string itemName, ref Sprite __result)
    {
     if (!itemName.StartsWith("thought_icons/")) return true;
    
     bool isMiniIcon = itemName.StartsWith("thought_icons/icon/");
     string projectName = isMiniIcon ? itemName[19..^5] : itemName[14..];
     DiscoRunner.Log.LogInfo($"looking for THC image {projectName} @ size {(isMiniIcon ? "small" : "big")} ");
     var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id == projectName);
     if (modProject == null) return true;
     
     var task = DiscoRunner.GetSource(modProject.source!).Router.Portraits.Get(isMiniIcon ? modProject.iconImageLocation : modProject.bigImageLocation);
     var result = task.Wait();
     __result = result;
     return false;
    }

    [HarmonyPatch(typeof(ThoughtCabinetTooltip), nameof(ThoughtCabinetTooltip.SetTab), typeof(bool))]
    [HarmonyPostfix]
    public static void OnTHCDetailsRefresh(bool showProblem, ThoughtCabinetTooltip __instance)
    {
	    __instance.description.text =
		    showProblem ? __instance.thought.description : __instance.thought.completionDescription;
    }

    // // might be able to combine all four of these into one patch?
    // [HarmonyPatch(typeof(Sunshine.ThoughtSlot), nameof(Sunshine.ThoughtSlot.FindAndSetThoughtImage))]
    // [HarmonyPrefix]
    // private static bool OnFindSetThoughtImage(Sunshine.ThoughtSlot __instance)
    // {
    //     var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id == __instance.Project.name);
    //     if (modProject == null) return true;
    //
    //     var task = AssetUtils.LoadPortrait(DiscoToPixels.EncodeTextureName(modProject.source!, modProject.iconImageLocation), null);
    //     task.ContinueWith(handle =>
    //     {
    //         __instance._thoughtProjectImage.sprite = handle.Result;
    //         __instance.RefreshImage();
    //         __instance._thoughtProjectImage.enabled = false;
    //         __instance._thoughtProjectImage.enabled = true;
    //     });
    //     return false;
    // }
    //
    // [HarmonyPatch(typeof(PageSystemThoughtSlot), nameof(PageSystemThoughtSlot.FindAndSetThoughtImage))]
    // [HarmonyPrefix]
    // private static bool OnFindSetThoughtImage(PageSystemThoughtSlot __instance)
    // {
    //     var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id == __instance.Project.name);
    //     if (modProject == null) return true;
    //
    //     var task = AssetUtils.LoadPortrait(DiscoToPixels.EncodeTextureName(modProject.source!, modProject.iconImageLocation), null);
    //     task.ContinueWith(handle =>
    //     {
    //         __instance._thoughtProjectImage.sprite = handle.Result;
    //         __instance.RefreshImage();
    //         __instance._thoughtProjectImage.enabled = false;
    //         __instance._thoughtProjectImage.enabled = true;
    //     });
    //     return false;
    // }
    //
    // [HarmonyPatch(typeof(PageSystemThoughtOceanSlot), nameof(PageSystemThoughtOceanSlot.FindAndSetThoughtImage))]
    // [HarmonyPrefix]
    // private static bool OnFindSetThoughtImage(PageSystemThoughtOceanSlot __instance)
    // {
    //     var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id == __instance.Project.name);
    //     if (modProject == null) return true;
    //
    //     var task = AssetUtils.LoadPortrait(DiscoToPixels.EncodeTextureName(modProject.source!, modProject.iconImageLocation), null);
    //     task.ContinueWith(handle =>
    //     {
    //         __instance._thoughtProjectImage.sprite = handle.Result;
    //         __instance.RefreshImage();
    //         __instance._thoughtProjectImage.enabled = false;
    //         __instance._thoughtProjectImage.enabled = true;
    //     });
    //     return false;
    // }
    //
    // [HarmonyPatch(typeof(THCDetailsPage), nameof(THCDetailsPage.RefreshUI))]
    // [HarmonyPostfix]
    // private static void OnRefresh(THCDetailsPage __instance)
    // {
	   //  var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id == __instance.thoughtProject.name);
	   //  if (modProject == null) return;
    //
	   //  var task = AssetUtils.LoadPortrait(DiscoToPixels.EncodeTextureName(modProject.source!, modProject.bigImageLocation), null);
	   //  task.ContinueWith(handle =>
	   //  {
		  //   __instance.graphic.sprite = handle.Result;
	   //  });
    // }
    //
    // [HarmonyPatch(typeof(THCDetailsPage), nameof(THCDetailsPage.SetAndShowGraphicIfProjectNotNull))]
    // [HarmonyPrefix]
    // private static bool OnSetAndShowGraphicIfProjectNotNull(SM.ThoughtCabinetProject thoughtProject, Image graphic)
    // {
	   //  var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id == thoughtProject.name);
	   //  if (modProject == null) return true;
    //
	   //  var task = AssetUtils.LoadPortrait(DiscoToPixels.EncodeTextureName(modProject.source!, modProject.bigImageLocation), null);
	   //  task.ContinueWith(handle =>
	   //  {
		  //   graphic.sprite = handle.Result;
		  //   graphic.gameObject.SetActive(true);
	   //  });
	   //  return false;
    // }
    //
    // [HarmonyPatch(typeof(THCSplashscreenPage), nameof(THCSplashscreenPage.SetThoughtProject))]
    // [HarmonyPostfix]
    // private static void OnSetThoughtProject(SM.ThoughtCabinetProject project, THCSplashscreenPage __instance)
    // {
	   //  var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id == project.name);
	   //  if (modProject == null) return;
    //
	   //  var task = AssetUtils.LoadPortrait(DiscoToPixels.EncodeTextureName(modProject.source!, modProject.bigImageLocation), null);
	   //  task.ContinueWith(handle =>
	   //  {
		  //   __instance.thoughtImage.sprite = handle.Result;
	   //  });
    // }
    //
    // [HarmonyPatch(typeof(ThoughtSplashScreenView), nameof(ThoughtSplashScreenView.SetThoughtProject))]
    // [HarmonyPrefix]
    // private static bool OnSetThoughtProject(SM.ThoughtCabinetProject project, ThoughtSplashScreenView __instance)
    // {
	   //  var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id == project.name);
	   //  if (modProject == null) return true;
    //
	   //  var task = AssetUtils.LoadPortrait(DiscoToPixels.EncodeTextureName(modProject.source!, modProject.bigImageLocation), null);
	   //  task.ContinueWith(handle =>
	   //  {
		  //   __instance.image.sprite = handle.Result;
	   //  });
	   //  return false;
    // }

    [HarmonyPatch(typeof(SM.ThoughtCabinetProject), nameof(SM.ThoughtCabinetProject.displayName), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool DisplayName_get(SM.ThoughtCabinetProject __instance, ref string __result)
    {
        // is there a better way to resolve this? other than a cache
        var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id == __instance.name);
        if (modProject == null) return true;
        
        __result = modProject.displayName;
        DiscoRunner.Log.LogInfo(__result);
        return false;
    }
    
    [HarmonyPatch(typeof(SM.ThoughtCabinetProject), nameof(SM.ThoughtCabinetProject.formattedDisplayNameUpper), MethodType.Getter)]
    [HarmonyPatch(typeof(SM.ThoughtCabinetProject), nameof(SM.ThoughtCabinetProject.displayNameToUpper), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool FormattedDisplayName_get(SM.ThoughtCabinetProject __instance, ref string __result)
    {
        var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id == __instance.name);
        if (modProject == null) return true;
        
        __result = modProject.displayName.ToUpper();
        return false;
    }
    
    [HarmonyPatch(typeof(SM.ThoughtCabinetProject), nameof(SM.ThoughtCabinetProject.description), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool Description_get(SM.ThoughtCabinetProject __instance, ref string __result)
    {
        var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id == __instance.name);
        if (modProject == null) return true;

        __result = modProject.descripton;
        return false;
    }
    
    [HarmonyPatch(typeof(SM.ThoughtCabinetProject), nameof(SM.ThoughtCabinetProject.completionDescription), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool CompletionDescription_get(SM.ThoughtCabinetProject __instance, ref string __result)
    {
        var modProject = DiscoRunner.manager.Assets.GetArena<Thought>().FirstOrDefault(t => t.id == __instance.name);
        if (modProject == null) return true;

        __result = modProject.completionDescription;
        return false;
    }

    [HarmonyPatch(typeof(ThoughtOnList), nameof(ThoughtOnList.Refresh))]
    [HarmonyPrefix] // fixme(?): base method has some NRE & doesn't seem to match our Mono source
    private static bool OnRefreshListItem(ThoughtOnList __instance)
    {
	    __instance._text.text = __instance.Project.displayName.ToUpper();
			if (__instance.Project.state == SM.ThoughtState.UNKNOWN || __instance.Project.state == SM.ThoughtState.FORGOTTEN)
			{
				__instance._text.color = ((__instance.Project.state == SM.ThoughtState.UNKNOWN) ? SingletonComponent<ThoughtManager>.Singleton.ColorTextUnknown : SingletonComponent<ThoughtManager>.Singleton.ColorTextForgotten);
				__instance._textLocalize.SecondaryTerm = SingletonComponent<ThoughtManager>.Singleton.FontNormalTerm;
				if (__instance.Project.state == SM.ThoughtState.FORGOTTEN && __instance._text.text.Length > 0)
				{
					__instance._text.text = TextUtils.StrikethroughText(__instance._text.text);
				}
				__instance._backgroundImage.gameObject.SetActive(value: false);
				__instance._leftBlock.gameObject.SetActive(value: false);
				__instance._icon.gameObject.SetActive(value: false);
				__instance._researchProgressText.gameObject.SetActive(value: false);
				__instance._fillingImage.gameObject.SetActive(value: false);
				return false;
			}
			if (__instance.Project.state == SM.ThoughtState.KNOWN)
			{
				__instance._backgroundImage.gameObject.SetActive(value: false);
				float num = 1f - __instance.Project.ResearchProgress;
				if ((double)num > 0.01)
				{
					__instance._leftBlock.gameObject.SetActive(value: true);
					__instance._leftBlock.color = SingletonComponent<ThoughtManager>.Singleton.ColorLeftBlockDeselected;
					TextMeshProUGUI researchProgressText = __instance._researchProgressText;
					string text2 = (__instance._researchProgressText.text = $"{(int)(num * 100f):0.}%");
					researchProgressText.text = text2;
					__instance._researchProgressText.gameObject.SetActive(value: true);
					__instance._researchProgressText.color = SingletonComponent<ThoughtManager>.Singleton.ColorLeftIconDeselected;
				}
				else
				{
					__instance._leftBlock.gameObject.SetActive(value: false);
					__instance._leftBlock.color = SingletonComponent<ThoughtManager>.Singleton.ColorLeftBlockDeselected;
					__instance._researchProgressText.gameObject.SetActive(value: false);
				}
				__instance._icon.gameObject.SetActive(value: false);
				__instance._fillingImage.gameObject.SetActive(value: false);
				if (__instance._isSelected)
				{
					__instance._backgroundImage.gameObject.SetActive(value: true);
					__instance.ChangeImageColor(__instance._backgroundImage, SingletonComponent<ThoughtManager>.Singleton.ColorBackgroundSelected);
					__instance.ChangeTextColor(__instance._text, SingletonComponent<ThoughtManager>.Singleton.ColorTextSelected);
				}
				else
				{
					__instance.ChangeTextColor(__instance._text, SingletonComponent<ThoughtManager>.Singleton.ColorTextKnown);
				}
			}
			else if (__instance.Project.state == SM.ThoughtState.COOKING || __instance.Project.state == SM.ThoughtState.DISCOVERED)
			{
				__instance._backgroundImage.gameObject.SetActive(value: true);
				__instance._leftBlock.gameObject.SetActive(value: true);
				__instance._icon.gameObject.SetActive(value: false);
				__instance._researchProgressText.gameObject.SetActive(value: true);
				__instance._fillingImage.gameObject.SetActive(value: true);
				if (__instance._isSelected)
				{
					__instance.ChangeTextColor(__instance._text, SingletonComponent<ThoughtManager>.Singleton.ColorTextSelected);
					__instance.ChangeImageColor(__instance._backgroundImage, SingletonComponent<ThoughtManager>.Singleton.ColorBackgroundSelected);
					__instance._fillingImage.gameObject.SetActive(value: false);
				}
				else
				{
					__instance.ChangeTextColor(__instance._text, SingletonComponent<ThoughtManager>.Singleton.ColorTextCooking);
					__instance.ChangeImageColor(__instance._backgroundImage, SingletonComponent<ThoughtManager>.Singleton.ColorBackgroundCooking);
					__instance._researchProgressText.color = SingletonComponent<ThoughtManager>.Singleton.ColorLeftIconDeselected;
					__instance._leftBlock.color = SingletonComponent<ThoughtManager>.Singleton.ColorLeftBlockDeselected;
				}
				__instance._fillingImage.color = SingletonComponent<ThoughtManager>.Singleton.ColorResearchFillingImage;
				float num2 = 1f - __instance.Project.ResearchProgress;
				__instance._fillingImage.fillAmount = num2;
				TextMeshProUGUI researchProgressText2 = __instance._researchProgressText;
				object text3;
				if (__instance.Project.state != SM.ThoughtState.COOKING)
				{
					text3 = "";
				}
				else
				{
					string text2 = (__instance._researchProgressText.text = $"{(int)(num2 * 100f):0.}%");
					text3 = text2;
				}
				researchProgressText2.text = (string)text3;
				if (__instance.Project.state == SM.ThoughtState.DISCOVERED)
				{
					__instance._icon.gameObject.SetActive(value: true);
					__instance._icon.sprite = SingletonComponent<ThoughtManager>.Singleton.LockedIcon;
					if (__instance._isSelected)
					{
						__instance.ChangeImageColor(__instance._backgroundImage, SingletonComponent<ThoughtManager>.Singleton.ColorBackgroundSelected);
						__instance._icon.color = SingletonComponent<ThoughtManager>.Singleton.ColorLeftIconDeselected;
						__instance._backgroundImage.gameObject.SetActive(value: true);
					}
					else
					{
						__instance._icon.color = SingletonComponent<ThoughtManager>.Singleton.ColorLeftIconDeselected;
					}
				}
			}
			else if (__instance.Project.state == SM.ThoughtState.FIXED)
			{
				__instance._backgroundImage.gameObject.SetActive(value: true);
				__instance._leftBlock.gameObject.SetActive(value: true);
				__instance._researchProgressText.gameObject.SetActive(value: false);
				__instance._fillingImage.gameObject.SetActive(value: false);
				__instance._icon.gameObject.SetActive(value: true);
				__instance._icon.sprite = SingletonComponent<ThoughtManager>.Singleton.LockedIcon;
				__instance._icon.color = SingletonComponent<ThoughtManager>.Singleton.ColorLeftIconDeselected;
				if (__instance._isSelected)
				{
					__instance.ChangeTextColor(__instance._text, SingletonComponent<ThoughtManager>.Singleton.ColorTextSelected);
					__instance.ChangeImageColor(__instance._backgroundImage, SingletonComponent<ThoughtManager>.Singleton.ColorBackgroundSelected);
				}
				else
				{
					__instance.ChangeTextColor(__instance._text, SingletonComponent<ThoughtManager>.Singleton.ColorTextKnown);
					__instance.ChangeImageColor(__instance._backgroundImage, SingletonComponent<ThoughtManager>.Singleton.ColorBackgroundFixed);
					__instance._leftBlock.color = SingletonComponent<ThoughtManager>.Singleton.ColorLeftBlockDeselected;
				}
			}
			if (__instance._isUpdated)
			{
				if (__instance.Project.state != SM.ThoughtState.DISCOVERED)
				{
					__instance._icon.gameObject.SetActive(value: true);
					__instance._icon.sprite = SingletonComponent<ThoughtManager>.Singleton.UpdatedIcon;
					__instance._icon.color = SingletonComponent<ThoughtManager>.Singleton.ColorLeftIconDeselected;
				}
				__instance._textLocalize.SecondaryTerm = SingletonComponent<ThoughtManager>.Singleton.FontBoldTerm;
			}
			else
			{
				__instance._textLocalize.SecondaryTerm = SingletonComponent<ThoughtManager>.Singleton.FontNormalTerm;
			}

			return false;
    }
}