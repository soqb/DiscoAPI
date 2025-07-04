using System.Collections.Generic;
using System.Linq;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Patches;
using DiscoAPI.Runtime.SaveSystem.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Voidforge;
using JsonUtil = Sunshine.JsonUtil;
using SM = Sunshine.Metric;
using Il2CppCollection = Il2CppSystem.Collections.Generic;

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

	public SkillContainer()
	{
		DiscoRunner.saveGame.Add(SaveSkillSunshineData);
		DiscoRunner.loadSavedGame.Add(LoadSkillSunshineData);
	}

	internal void ReinitializeFromNativeInstance(SM.CharacterSheet sheet)
	{
		var skillArena = SkillUtils.Skills;

		for (int i = 0; i < skillArena.Count; i++)
		{
			var sk = skillArena[i];
			if (sk == null) continue;

			var type = skillArena.GetRaw(i);
			var skill = sheet.GetSkill(type);
			skillMap[sk.Location] = skill ?? new SM.Skill(type, sheet);
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

	private void SaveSkillSunshineData()
	{
		var smSkillsForSerialize = new Il2CppCollection.List<SM.Skill>();
		var skillModifierStateMap = new Il2CppCollection.Dictionary<SM.SkillType, Il2CppCollection.List<CharacterSheetPersister.ModifierState>>();
		
		foreach (var modSkill in SkillUtils.Skills)
		{
			var smSkill = GetRawSkill(modSkill.Location);
			if (smSkill == null || SkillUtils.SkillIsVanilla(smSkill.skillType)) continue;
			
			DiscoRunner.Log.LogInfo("Saving " + modSkill.displayName);
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

			// todo: verify modifiers are not being cleared before the on-exit autosave (once double hook invocation is fixed)
			smSkill.ClearModifiersForPersistence();
			smSkill.bonusOnlyValues = null;
			smSkillsForSerialize.Add(smSkill);
		}
		// JsonSerializerSettings charSheetSerializer = new ()
		// {
		// 	ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
		// 	Converters = [new SunshineSkillConverter()]
		// };
		
		var saveData = DiscoRunner.saveSystem.GetModData("disco");
		JsonUtil.serializer.Config.SerializeEnumsAsInteger = true;
		var modStateJson = JsonUtil.Serialize(skillModifierStateMap);
		JsonUtil.serializer.Config.SerializeEnumsAsInteger = false;
		saveData.SetString("modifierStateMap", modStateJson);
		//var skillJson = JsonConvert.SerializeObject(smSkillsForSerialize, charSheetSerializer);
		var skillJson = JsonUtil.Serialize(smSkillsForSerialize);
		saveData.SetString("sunshineSkills", skillJson);
	}

	private void LoadSkillSunshineData()
	{
		var saveData = DiscoRunner.saveSystem.GetModData("disco");
		string? modifierStatesJson = saveData.GetString("modifierStateMap");
		string? serializedSkillsJson = saveData.GetString("sunshineSkills");
		if (modifierStatesJson == null || serializedSkillsJson == null)
		{
			DiscoRunner.Log.LogWarning("CharacterSheet: failed to fetch mod skill data for this savegame. aborting!");
			return;
		}

		// JsonSerializerSettings serializerSettings = new ()
		// {
		// 	ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
		// 	Converters = [new SunshineSkillConverter()]
		// };
		var modStates =
			JsonUtil
				.Deserialize<
					Il2CppCollection.Dictionary<SM.SkillType,
						Il2CppCollection.List<CharacterSheetPersister.ModifierState>>>(modifierStatesJson);
		//var smSkills = JsonConvert.DeserializeObject<List<SM.Skill>>(serializedSkillsJson, serializerSettings);
		var smSkills = JsonUtil.Deserialize<Il2CppCollection.List<SM.Skill>>(serializedSkillsJson);
		if (modStates == null || smSkills == null)
		{
			DiscoRunner.Log.LogError("CharacterSheet: failed to deserialize mod skill data for this savegame. aborting!");
			return;
		}

		var characterSheet = SingletonComponent<World>.Singleton.you;

		foreach (var smSkill in smSkills)
		{
			smSkill.SetCharacterSheetFromPersistence(characterSheet);
			smSkill.modifiers = new Il2CppCollection.List<SM.Modifier>();
			foreach (var modState in modStates[smSkill.skillType])
			{
				var builtMod = BuildModifierFromState(characterSheet, modState);
				if (builtMod != null) smSkill.modifiers.Add(builtMod);
			}
			
			// repopulate into mod skills
			var modSkill = SkillUtils.Lookup(smSkill.skillType);
			if (modSkill != null)
			{
				skillMap[modSkill.Location] = smSkill;
			}
			//CharacterPatches.PrintModifiable(smSkill);
		}
		
		RepopulateNativeInstanceLists(characterSheet);
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

public class ModCharacterSheet : ModEntity<ModCharacterSheet, SM.CharacterSheet>, IRecalculable
{
	public static ModEntityRegistry<ModCharacterSheet, SM.CharacterSheet> Registry { get; }
		= new(new PersistentEntityMap<ModCharacterSheet, SM.CharacterSheet>(s => new(s)));

	protected override IComponentStore Components { get; } = new DictComponentStore();

	public static ModCharacterSheet Of(SM.CharacterSheet s) => Registry.EntityOf(s);

	// NB: The base method is hooked to recalculate all components.
	public void Recalc() => EntityBase.Recalc();

	private ModCharacterSheet(SM.CharacterSheet disco) : base(Registry, disco) { }
}

public static class CharacterComponents
{
	public static ComponentKey<SkillContainer, ModCharacterSheet, SM.CharacterSheet> Skills { get; }
		= ModCharacterSheet.Registry.Register<SkillContainer>("discoapi", "skills");
}
