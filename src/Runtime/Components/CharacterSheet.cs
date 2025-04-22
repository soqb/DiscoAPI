using System.Collections.Generic;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Assets;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime.Components;

public interface IRecalculable
{
	void Recalc();
}

public sealed class SkillContainer
{
	private Dictionary<AssetLocation, SM.Skill> skillMap = new();

	public SM.Skill? GetRawSkill(IAssetRef<Skill> skill) => skillMap.GetValueOrDefault(skill.Location);
	public SM.Skill MoraleRaw => GetRawSkill(DiscoRunner.globalConfig.MoraleSkill)!;
	public SM.Skill HealthRaw => GetRawSkill(DiscoRunner.globalConfig.HealthSkill)!;

	internal void ReinitializeFromNativeInstance(SM.CharacterSheet sheet)
	{
		var skills = (EnumArena<SM.SkillType, Skill>)DiscoRunner.manager.Assets.GetArena<Skill>();

		for (int i = 0; i < skills.Count; i++)
		{
			var sk = skills[i];
			if (sk == null || skillMap.ContainsKey(sk.Location)) continue;

			var type = skills.GetRaw(i);
			var skill = i < skills.baseCount ? sheet.GetSkill(type) : new SM.Skill(type, sheet);
			skillMap.Add(sk.Location, skill);
		}
	}

	internal void RepopulateNativeInstanceLists(SM.CharacterSheet sheet)
	{
		DiscoRunner.Log.LogInfo($"repoping!!! @ {DebugUtils.StackTrace()}");
		int targetCount = SkillUtils.Skills.Count + Skill.VANILLA_SKILL_PORTRAIT_COUNT - Skill.VANILLA_SKILL_COUNT;
		SM.Skill?[] skills = new SM.Skill[targetCount];
		sheet.skills.CopyTo(skills, 0);

		for (int i = 0; i < SkillUtils.Skills.Count - SkillUtils.Skills.baseCount; i++)
		{
			var asset = SkillUtils.Skills[i + SkillUtils.Skills.baseCount]!.Location;
			var skill = GetRawSkill(asset);
			if (skill == null) DiscoRunner.Log.LogError($"skill {asset} on sheet {sheet.name} was null!");
			skills[i + Skill.VANILLA_SKILL_PORTRAIT_COUNT] = skill;
		}

		sheet.skills = skills;
	}
}

public class ModCharacterSheet : ModEntity<ModCharacterSheet, SM.CharacterSheet>, IRecalculable
{
	public static ModEntityRegistry<ModCharacterSheet, SM.CharacterSheet> Registry { get; } = new(s => new(s));
	public static ModCharacterSheet Of(SM.CharacterSheet s) => Registry.EntityOf(s);

	// NB: The base method is hooked to recalculate all components.
	public void Recalc() => EntityBase.Recalc();

	public ModCharacterSheet(SM.CharacterSheet disco) : base(Registry, disco) { }
}

public static class CharacterComponents
{
	public static ComponentKey<SkillContainer, ModCharacterSheet, SM.CharacterSheet> Skills { get; }
		= ModCharacterSheet.Registry.Register<SkillContainer>(_ => new());
}
