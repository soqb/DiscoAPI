using System;
using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Runtime.Assets;
using Il2CppInterop.Runtime;
using PC = PixelCrushers.DialogueSystem;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime.Utils;

public static class SkillUtils
{
	public static Actor? ActorForSkill(SM.SkillType st) => Lookup(st)?.actor.Resolve(DiscoRunner.manager);

	public static string? GetActorSkillName(SM.SkillType type)
	{
		if (type == SM.SkillType.NONE) return InherentProvider.DUMMY_NONE_SKILL;
		if ((int)type <= Skill.VANILLA_MAX) return SM.Skill.actorSkillNames[(int)type];
		else return ActorForSkill(type)?.displayName;
	}

	public static string? GetSkillOrAbilityName(SM.Modifiable modifiable)
	{
		if (modifiable == null)
		{
			DiscoRunner.Log.LogWarning("unexpected null modifiable");
			return null;
		}
		// excellent example of why il2cpp is a bit weird:
		if (modifiable.GetIl2CppType() == Il2CppType.Of<SM.Skill>())
			return GetActorSkillName(modifiable.Cast<SM.Skill>().skillType);
		else if (modifiable.GetIl2CppType() == Il2CppType.Of<SM.Ability>())
			return SM.Ability.GetActorAbilityName(modifiable.Cast<SM.Ability>().abilityType);
		else
			return null;
	}

	public static Skill RecoverSkill(SM.SkillType type)
	{
		string name = SM.Skill.GetActorSkillName(type);
		return new Skill(
			FormatUtils.Slugify(type.ToString()),
			name,
			new AssetLocation<Actor>(FormatUtils.Slugify(name)),
			AbilityFromSunshine(SM.Skill.GetAbility(type))
		);
	}

	public static AbilityType AbilityFromSunshine(SM.AbilityType ability) => ability switch
	{
		SM.AbilityType.INT => AbilityType.Int,
		SM.AbilityType.PSY => AbilityType.Psy,
		SM.AbilityType.FYS => AbilityType.Fys,
		SM.AbilityType.MOT => AbilityType.Mot,
		_ => throw new NotSupportedException("expected a valid ability type"),
	};

	public static SM.AbilityType AbilityToSunshine(AbilityType ability) => ability switch
	{
		AbilityType.Int => SM.AbilityType.INT,
		AbilityType.Psy => SM.AbilityType.PSY,
		AbilityType.Fys => SM.AbilityType.FYS,
		AbilityType.Mot => SM.AbilityType.MOT,
		_ => throw new NotSupportedException("expected a valid ability type"),
	};

	public static bool SkillIsReal(SM.SkillType skill) => skill switch
	{
		SM.SkillType.NONE
		or SM.SkillType.ALT => false,
		_ => true
	};

	public static bool SkillIsVanilla(SM.SkillType skillType) => (int)skillType <= Skill.VANILLA_MAX;

	public static bool IsExcludedFromPortraits(SM.SkillType type) => type switch
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

	public static EnumArena<SM.SkillType, Skill> Skills => (EnumArena<SM.SkillType, Skill>)DiscoRunner.manager.Assets.GetArena<Skill>();

	public static Skill? Lookup(SM.SkillType st) => Skills[Skills.ReverseId(st)];

	public static PC.Actor SkillToPCActor(IAssetRef<Skill> skill)
	{
		return skill.Resolve(DiscoRunner.manager)!.actor.ResolveCrushed(DiscoRunner.manager)!;
	}

	public static void OnDialogueBundleLoad()
	{
		for (int i = SkillUtils.Skills.baseCount; i < SkillUtils.Skills.Count; i++)
		{
			Skill skill = SkillUtils.Skills[i]!;
			SM.SkillType rawSkill = SkillUtils.Skills.GetRaw(i);
			string id = skill.actor.ResolveCrushed()!.LookupValue(ArticyBridge.ARTICY_ID_FIELD);

			ArticyBridge.ARTICY_ID_TO_SKILL_TYPE.Add(id, rawSkill);
			ArticyBridge.ARTICY_ID_TO_SKILL_NAME.Add(id, skill.displayName);
		}

		var newOrbMap = new SM.SkillType[Skill.VANILLA_SKILL_ORB_COUNT + SkillUtils.Skills.Count];
		for (int i = Skill.VANILLA_SKILL_ORB_COUNT; i < newOrbMap.Length; i++)
		{
			newOrbMap[i - 1] = (SM.SkillType)i;
		}

		Array.ConstrainedCopy(ArticyBridge.articyOrbSkillToSunshineOrbSkill, 0,
			newOrbMap, 0, ArticyBridge.articyOrbSkillToSunshineOrbSkill.Count);
		ArticyBridge.articyOrbSkillToSunshineOrbSkill = newOrbMap;
	}
}
