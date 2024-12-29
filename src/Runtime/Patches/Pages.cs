using HarmonyLib;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using SM = Sunshine.Metric;
using Voidforge;

namespace DiscoAPI.Runtime.Patches;

public static class PagesPatches
{

	[HarmonyPatch(typeof(ActorsPortraitsBundleManager), nameof(ActorsPortraitsBundleManager.LoadPortraitSpriteAsync))]
	[HarmonyPrefix]
	private static bool OnLoadPortraitSpriteAsync(ref AsyncOperationHandle<Sprite> __result, string textureName, Il2CppSystem.Action<AsyncOperationHandle<Sprite>> del)
	{

		if (textureName.StartsWith("EXTRA#"))
		{
			textureName = textureName.Substring(6);
			// pretty ham-fisted but works.
			byte[] bytes = System.IO.File.ReadAllBytes("BepInEx/plugins/DCA/" + textureName);
			Texture2D tex = new(0, 0, TextureFormat.RGB24, false);
			ImageConversion.LoadImage(tex, bytes, false);
			Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new(0.5f, 0.5f));
			__result = Addressables.ResourceManager.CreateCompletedOperation<Sprite>(sprite, null);
			__result.add_Completed(del);
			return false;
		}

		return true;
	}

	// [HarmonyPatch(typeof(SkillPortraitPanel), nameof(SkillPortraitPanel.LoadSkillPortraitAsync))]
	// [HarmonyPrefix]
	// private static void OnLoadSkillPortraitAsync(SkillPortraitPanel __instance)
	// {
	// 	// DiscoAPIPlugin.Instance.Log.LogInfo($"nb that there are {CharacterSheet.reverseIndex.Count} sheets.");

	// 	// PixelCrushers.DialogueSystem.Actor actor = CharacterSheetTooltip.ActorFromModifiable(__instance.currentSkill);
	// 	// DiscoAPIPlugin.Instance.Log.LogInfo($"{__instance.currentSkill.skillType} is about to load '{actor?.Name ?? "noone"}'");
	// }

	private static bool IsExcluded(SM.SkillType type) => type switch
	{
		SM.SkillType.NONE
		or SM.SkillType.CONVALESCENCE
		or SM.SkillType.HEARING
		or SM.SkillType.SIGHT
		or SM.SkillType.SMELL
		or SM.SkillType.TASTE
		or SM.SkillType.ALT => true,
		_ => false,
	};


	[HarmonyPatch(typeof(Charsheet.SkillPortrayConfigurator), nameof(Charsheet.SkillPortrayConfigurator.UpdateSkillPortraitPanelsContent))]
	[HarmonyPrefix]
	private static bool OnSkillConfiguratorUpdateSkillPortraitPanelsContent(Charsheet.SkillPortrayConfigurator __instance)
	{
		DiscoAPIPlugin.Instance.Log.LogDebug($"updating panels content. note there are {__instance.skillList.Length} skills.");

		var skills = (SM.SkillType[])System.Enum.GetValues(typeof(SM.SkillType));
		for (int i = 0; i < skills.Length; i++)
		{
			SM.SkillType skillType = skills[i];

			// DiscoAPIPlugin.Instance.Log.LogInfo($" > ({i}) treating {skillType}");
			if (IsExcluded(skillType)) continue;
			SkillPortraitPanel? skillPortraitPanel = null;

			foreach (var sk in __instance.skillList)
			{
				if (sk.name == skillType.ToString())
				{
					// DiscoAPIPlugin.Instance.Log.LogInfo($"   * comparing with {sk.name}...");
					skillPortraitPanel = sk;
					break;
				}
			}

			if (skillPortraitPanel != null)
			{
				skillType = skillType switch
				{
					SM.SkillType.LOGIC => (SM.SkillType)31,
					SM.SkillType.ENCYCLOPEDIA => (SM.SkillType)32,
					SM.SkillType.RHETORIC => (SM.SkillType)33,
					SM.SkillType.DRAMA => (SM.SkillType)34,
					SM.SkillType.VOLITION => (SM.SkillType)35,
					SM.SkillType.INLAND_EMPIRE => (SM.SkillType)36,
					SM.SkillType.EMPATHY => (SM.SkillType)37,
					SM.SkillType.AUTHORITY => (SM.SkillType)38,
					SM.SkillType.ENDURANCE => (SM.SkillType)39,
					SM.SkillType.PAIN_THRESHOLD => (SM.SkillType)40,
					SM.SkillType.PHYSICAL_INSTRUMENT => (SM.SkillType)41,
					SM.SkillType.ELECTROCHEMISTRY => (SM.SkillType)42,
					SM.SkillType.HE_COORDINATION => (SM.SkillType)43,
					SM.SkillType.PERCEPTION => (SM.SkillType)44,
					SM.SkillType.REACTION => (SM.SkillType)45,
					SM.SkillType.SAVOIR_FAIRE => (SM.SkillType)46,
					_ => skillType,
				};
				SM.Skill skill = new SM.Skill(skillType, null);
				DiscoAPIPlugin.Instance.Log.LogInfo($" > setting skill to {skill.skillType}...");
				skillPortraitPanel.SetSkill(skill);
			}
		}

		if (SingletonComponent<CharsheetView>.Singleton != null)
		{
			SingletonComponent<CharsheetView>.Singleton.UpdateSkillPortraitPanels(__instance.skillList);
		}

		return false;
	}
}
