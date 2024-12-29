namespace DiscoAPI.Common.Assets;

using System;
using Newtonsoft.Json;
using SM = Sunshine.Metric;

public enum AbilityType
{
	Int = 1,
	Psy = 2,
	Fys = 3,
	Mot = 4,
}

public class Skill : Asset
{
	public override AssetType Type => AssetType.Skill;

	[JsonProperty("name")]
	public string displayName;
	public AssetRef actor;
	public AbilityType ability;

	public Skill(string id, string displayName, AssetRef actor, AbilityType ability) : base(id)
	{
		this.displayName = displayName;
		this.actor = actor;
		this.ability = ability;
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

	public static bool IsReal(SM.SkillType skill) => skill switch
	{
		SM.SkillType.NONE
		or SM.SkillType.ALT => false,
		_ => true
	};

	public const int VANILLA_SKILL_COUNT = 29;
	public const int VANILLA_SKILL_PORTRAIT_COUNT = 24;
	public const int VANILLA_MAX = 30;
}

public class Skills
{
	public static AssetRef Authority = new(AssetType.Skill, "disco", SM.SkillType.AUTHORITY.ToString());
	public static AssetRef Composure = new(AssetType.Skill, "disco", SM.SkillType.COMPOSURE.ToString());
	public static AssetRef Conceptualization = new(AssetType.Skill, "disco", SM.SkillType.CONCEPTUALIZATION.ToString());
	public static AssetRef HalfLight = new(AssetType.Skill, "disco", SM.SkillType.HALF_LIGHT.ToString());
	public static AssetRef Drama = new(AssetType.Skill, "disco", SM.SkillType.DRAMA.ToString());
	public static AssetRef Electrochemistry = new(AssetType.Skill, "disco", SM.SkillType.ELECTROCHEMISTRY.ToString());
	public static AssetRef Empathy = new(AssetType.Skill, "disco", SM.SkillType.EMPATHY.ToString());
	public static AssetRef Endurance = new(AssetType.Skill, "disco", SM.SkillType.ENDURANCE.ToString());
	public static AssetRef EspritDeCorps = new(AssetType.Skill, "disco", SM.SkillType.ESPRIT_DE_CORPS.ToString());
	public static AssetRef HandEyeCoordination = new(AssetType.Skill, "disco", SM.SkillType.HE_COORDINATION.ToString());
	public static AssetRef InlandEmpire = new(AssetType.Skill, "disco", SM.SkillType.INLAND_EMPIRE.ToString());
	public static AssetRef Interfacing = new(AssetType.Skill, "disco", SM.SkillType.INTERFACING.ToString());
	public static AssetRef Logic = new(AssetType.Skill, "disco", SM.SkillType.LOGIC.ToString());
	public static AssetRef PainThreshold = new(AssetType.Skill, "disco", SM.SkillType.PAIN_THRESHOLD.ToString());
	public static AssetRef Perception = new(AssetType.Skill, "disco", SM.SkillType.PERCEPTION.ToString());
	public static AssetRef Hearing = new(AssetType.Skill, "disco", SM.SkillType.HEARING.ToString());
	public static AssetRef Sight = new(AssetType.Skill, "disco", SM.SkillType.SIGHT.ToString());
	public static AssetRef Smell = new(AssetType.Skill, "disco", SM.SkillType.SMELL.ToString());
	public static AssetRef Taste = new(AssetType.Skill, "disco", SM.SkillType.TASTE.ToString());
	public static AssetRef PhysicalInstrument = new(AssetType.Skill, "disco", SM.SkillType.PHYSICAL_INSTRUMENT.ToString());
	public static AssetRef ReactionSpeed = new(AssetType.Skill, "disco", SM.SkillType.REACTION.ToString());
	public static AssetRef Rhetoric = new(AssetType.Skill, "disco", SM.SkillType.RHETORIC.ToString());
	public static AssetRef SavoirFaire = new(AssetType.Skill, "disco", SM.SkillType.SAVOIR_FAIRE.ToString());
	public static AssetRef Shivers = new(AssetType.Skill, "disco", SM.SkillType.SHIVERS.ToString());
	public static AssetRef Suggestion = new(AssetType.Skill, "disco", SM.SkillType.SUGGESTION.ToString());
	public static AssetRef Encyclopedia = new(AssetType.Skill, "disco", SM.SkillType.ENCYCLOPEDIA.ToString());
	public static AssetRef VisualCalculus = new(AssetType.Skill, "disco", SM.SkillType.VISUAL_CALCULUS.ToString());
	public static AssetRef Volition = new(AssetType.Skill, "disco", SM.SkillType.VOLITION.ToString());
}
