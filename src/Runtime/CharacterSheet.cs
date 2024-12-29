using System.Collections.Generic;
using DiscoAPI.Common.Assets;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime;

public class ModWorld
{
	public global::World discoWorld;

	public bool IsRunning => discoWorld.isRunning;
	public CharacterSheet you;

	public ModWorld(World discoWorld)
	{
		this.discoWorld = discoWorld;
		you = new(discoWorld.you);
	}
}

public class CharacterSheet
{
	public static Dictionary<SM.CharacterSheet, CharacterSheet> reverseIndex = new();
	public static CharacterSheet GetForSM(SM.CharacterSheet sheet)
	{
		if (!reverseIndex.ContainsKey(sheet)) reverseIndex.Add(sheet, new(sheet));
		return reverseIndex[sheet];
	}

	public SM.CharacterSheet sm;
	public Dictionary<AssetRef, SM.Skill> skillMap = new();

	public CharacterSheet(SM.CharacterSheet sm)
	{
		this.sm = sm;

		// InstallSkills();
	}

	public void EnsureSkillsInstalled()
	{
		var skills = DiscoRunner.manager.Assets.skills;

		// DiscoAPIPlugin.Instance.Log.LogInfo("installing skills for charsheet");

		for (int i = 0; i <= skills.MaxId; i++)
		{
			var sk = skills[i];
			if (sk == null || skillMap.ContainsKey(sk.Ref)) continue;
			var skill = i <= Skill.VANILLA_MAX ? sm.GetSkill((SM.SkillType)i) : new SM.Skill((SM.SkillType)i, sm);
			skillMap.Add(skills[i].Ref, skill);
		}

		// DiscoAPIPlugin.Instance.Log.LogInfo("> at the end of time:");
		// foreach (var kv in skillMap) DiscoAPIPlugin.Instance.Log.LogInfo($"   * {kv.Key} = {kv.Value?.skillType}");
	}
}
