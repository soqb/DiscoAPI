using System.Collections.Generic;
using System.Linq;
using DiscoAPI.Common.Assets;
using Voidforge;
using SM = Sunshine.Metric;
using Il2CppCollection = Il2CppSystem.Collections.Generic;
using DiscoAPI.Runtime.SaveSystem;
using DiscoAPI.Runtime.Utils;
using Newtonsoft.Json.Linq;

namespace DiscoAPI.Runtime.Components;

public interface IRecalculable
{
	void Recalc();
}

public static class CharacterComponents
{
	public static ModEntityRegistry<SM.CharacterSheet> Registry { get; }
		= new(new PersistentEntityMap<SM.CharacterSheet>(s => new(Registry!, s)));

	public static ModEntity<SM.CharacterSheet> Of(SM.CharacterSheet s) => Registry.EntityOf(s);

	// components...

	public static ComponentKey<SkillContainer, SM.CharacterSheet> Skills { get; }
		= Registry.Register<SkillContainer>("discoapi", "skills");
}

public sealed class SkillContainer : ISaveSerializable<SkillContainer, ModEntity<SM.CharacterSheet>>
{
	private Dictionary<AssetLocation, SM.Skill> skillMap = new();

	public SM.Skill? GetRawSkill(IAssetRef<Skill> skill) => skillMap.GetValueOrDefault(skill.Location);
	public SM.Skill MoraleRaw => GetRawSkill(DiscoRunner.globalConfig.MoraleSkill)!;
	public SM.Skill HealthRaw => GetRawSkill(DiscoRunner.globalConfig.HealthSkill)!;

	internal void ReinitializeFromNativeInstance(SM.CharacterSheet sheet)
	{
		var skillArena = SkillUtils.Skills;

		for (int i = 0; i < skillArena.Count; i++)
		{
			var sk = skillArena[i];
			if (sk == null || skillMap.ContainsKey(sk.Location)) continue;

			var type = skillArena.GetRaw(i);
			var skill = i < skillArena.baseCount ? sheet.GetSkill(type) : new SM.Skill(type, sheet);
			skillMap.Add(sk.Location, skill);
		}
	}

	internal void RepopulateNativeInstanceLists(SM.CharacterSheet sheet)
	{
		var skillArena = SkillUtils.Skills;

		int targetCount = skillArena.Count + Skill.VANILLA_SKILL_PORTRAIT_COUNT - Skill.VANILLA_SKILL_COUNT;
		SM.Skill?[] skills = new SM.Skill[targetCount];
		sheet.skills.CopyTo(skills, 0);

		for (int i = 0; i < skillArena.Count - skillArena.baseCount; i++)
		{
			var asset = skillArena[i + skillArena.baseCount]!.Location;
			var skill = GetRawSkill(asset);
			if (skill == null) DiscoRunner.Log.LogError($"skill {asset} on sheet {sheet.name} was null!");
			skills[i + Skill.VANILLA_SKILL_PORTRAIT_COUNT] = skill;
		}

		sheet.skills = skills;
	}

	public JToken Serialize()
	{
		var smSkillsForSerialize = new Il2CppCollection.List<SM.Skill>();
		var skillModifierStateMap = new Il2CppCollection.Dictionary<SM.SkillType, Il2CppCollection.List<CharacterSheetPersister.ModifierState>>();
		foreach (var modSkill in SkillUtils.Skills)
		{
			var smSkill = GetRawSkill(modSkill.Location);
			if (smSkill == null || SkillUtils.SkillIsVanilla(smSkill.skillType)) continue;

			smSkill = smSkill.MemberwiseClone().Cast<SM.Skill>(); // cloning to not modify the real skills if the game resumes
			skillModifierStateMap.Add(smSkill.skillType, new Il2CppCollection.List<CharacterSheetPersister.ModifierState>());

			if (smSkill.modifiers != null)
			{
				foreach (var mod in smSkill.modifiers)
				{
					if (mod == null) continue;
					var modState = CharacterSheetPersister.ConvertModifierToModifierState(mod);
					skillModifierStateMap[smSkill.skillType].Add(modState);
				}
			}
			
			smSkill.ClearModifiersForPersistence();
			smSkill.bonusOnlyValues = null;
			smSkillsForSerialize.Add(smSkill);
		}

		// builtin serializer used here bc it obeys Modifiable serializer attribs
		Sunshine.JsonUtil.serializer.Config.SerializeEnumsAsInteger = true;
		var serializerResult = new JObject {
			{ "modifierStateMap", Sunshine.JsonUtil.Serialize(skillModifierStateMap) },
			{ "sunshineSkills", Sunshine.JsonUtil.Serialize(smSkillsForSerialize) },
		};
		Sunshine.JsonUtil.serializer.Config.SerializeEnumsAsInteger = false;
		
		return serializerResult;
	}


	public bool TryDeserialize(JToken token, ModEntity<SM.CharacterSheet> sheet)
	{
		ReinitializeFromNativeInstance(sheet);

		JObject obj = (JObject)token;
		string? modifierStatesJson = obj.GetValue("modifierStateMap")?.Value<string>();
		string? serializedSkillsJson = obj.GetValue("sunshineSkills")?.Value<string>();
		if (modifierStatesJson == null || serializedSkillsJson == null)
		{
			DiscoRunner.Log.LogWarning("failed to load mod skill data for this savegame. aborting!");
			return false;
		}

		var modStates = Sunshine.JsonUtil.Deserialize<Il2CppCollection.Dictionary
				<SM.SkillType, Il2CppCollection.List<CharacterSheetPersister.ModifierState>>>
				(modifierStatesJson);
		var smSkills = Sunshine.JsonUtil.Deserialize<Il2CppCollection.List<SM.Skill>>(serializedSkillsJson);
		if (modStates == null || smSkills == null)
		{
			DiscoRunner.Log.LogError("failed to deserialize mod skill data for this savegame. aborting!");
			return false;
		}
		
		var characterSheet = sheet.EntityBase;

		foreach (var smSkill in smSkills)
		{
			smSkill.SetCharacterSheetFromPersistence(characterSheet);
			smSkill.modifiers = new Il2CppCollection.List<SM.Modifier>();
			foreach (var modState in modStates[smSkill.skillType])
			{
				var builtMod = BuildModifierFromState(characterSheet, modState);
				if (builtMod != null) smSkill.modifiers.Add(builtMod);
			}
			
			var modSkill = SkillUtils.Lookup(smSkill.skillType);
			if (modSkill != null)
			{
				skillMap[modSkill.Location] = smSkill;
			}

			// nb: there is an inexplicable bug where the basegame loader cannot deserialize a signature ability
			if (smSkill.isSignature)
			{
				characterSheet.GetAbility(smSkill.skillType).isSignature = true;
			}
		}
		
		RepopulateNativeInstanceLists(characterSheet);
		return true;
	}

	private static SM.Modifier? BuildModifierFromState(SM.CharacterSheet sheet, CharacterSheetPersister.ModifierState modState)
	{
		IModifierCause? modifierCause = modState.modifierCause?.GetModifierCause(SingletonComponent<World>.Singleton.you);
		SM.Modifier? modifier = null;
		if (modState.type == SM.ModifierType.ITEM)
		{
			SM.InventoryItem byName = SingletonComponent<InventoryItemList>.Singleton.GetByName(modState.modifierCause.ModifierKey);
			SM.CharacterEffect? effect = byName.equipEffects.FirstOrDefault(e => e.skillType == modState.skillType);
			if (effect != null)
			{
				modifier = new SM.Modifier(
					modState.type,
					effect.parameter,
					(Il2CppSystem.Func<string>)effect.EffectName(),
					modifierCause,
					modState.skillType);
			}
		}
		else if (modState.type != SM.ModifierType.THC)
		{
			if (modState.type != SM.ModifierType.ELECTROCHEMISTRY)
				modifier = new SM.Modifier(modState.type, modState.amount, null, modifierCause, modState.skillType);
			else
				modifier = new SM.Modifier(modState.type, modState.amount,
					(Il2CppSystem.Func<string>)modState.explanation, modifierCause, modState.skillType);
		}
		else
		{
			SM.ThoughtCabinetProject byName2 = SingletonComponent<ThoughtCabinetProjectList>.Singleton.GetByName(modState.modifierCause?.ModifierKey);
			SM.CharacterEffect? effect2 = null;
			if (sheet.thoughts.ThoughtCooking(byName2))
			{
				effect2 = byName2.researchEffects.FirstOrDefault(e => e.skillType == modState.skillType);
			}
			else if (sheet.thoughts.ThoughtFixed(byName2))
			{
				effect2 = byName2.completionEffects.FirstOrDefault(e => e.skillType == modState.skillType);
			}
			if (effect2 != null)
			{
				modifier = new SM.Modifier(modState.type, effect2.parameter, (Il2CppSystem.Func<string>)effect2.EffectName(), modifierCause, modState.skillType);
			}
		}

		return modifier;
	}
}
