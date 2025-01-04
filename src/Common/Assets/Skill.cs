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

public class Skill : Asset, IAssetRef<Skill>
{
	[JsonProperty("name")]
	public string displayName;
	public IAssetRef<Dialogue.Actor> actor;
	public AbilityType ability;

	public Skill(string id, string displayName, IAssetRef<Dialogue.Actor> actor, AbilityType ability) : base(id)
	{
		this.displayName = displayName;
		this.actor = actor;
		this.ability = ability;
	}

	[JsonIgnore]
	public new AssetLocation<Skill> Location => new(source, id);
	Skill? IAssetRef<Skill>.Resolve(IDiscoManager mgr) => (Skill?)((IAssetRef)this).Resolve(mgr);

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
	public static AssetLocation<Skill> Authority = new("disco", SM.SkillType.AUTHORITY.ToString());
	public static AssetLocation<Skill> Composure = new("disco", SM.SkillType.COMPOSURE.ToString());
	public static AssetLocation<Skill> Conceptualization = new("disco", SM.SkillType.CONCEPTUALIZATION.ToString());
	public static AssetLocation<Skill> HalfLight = new("disco", SM.SkillType.HALF_LIGHT.ToString());
	public static AssetLocation<Skill> Drama = new("disco", SM.SkillType.DRAMA.ToString());
	public static AssetLocation<Skill> Electrochemistry = new("disco", SM.SkillType.ELECTROCHEMISTRY.ToString());
	public static AssetLocation<Skill> Empathy = new("disco", SM.SkillType.EMPATHY.ToString());
	public static AssetLocation<Skill> Endurance = new("disco", SM.SkillType.ENDURANCE.ToString());
	public static AssetLocation<Skill> EspritDeCorps = new("disco", SM.SkillType.ESPRIT_DE_CORPS.ToString());
	public static AssetLocation<Skill> HandEyeCoordination = new("disco", SM.SkillType.HE_COORDINATION.ToString());
	public static AssetLocation<Skill> InlandEmpire = new("disco", SM.SkillType.INLAND_EMPIRE.ToString());
	public static AssetLocation<Skill> Interfacing = new("disco", SM.SkillType.INTERFACING.ToString());
	public static AssetLocation<Skill> Logic = new("disco", SM.SkillType.LOGIC.ToString());
	public static AssetLocation<Skill> PainThreshold = new("disco", SM.SkillType.PAIN_THRESHOLD.ToString());
	public static AssetLocation<Skill> Perception = new("disco", SM.SkillType.PERCEPTION.ToString());
	public static AssetLocation<Skill> Hearing = new("disco", SM.SkillType.HEARING.ToString());
	public static AssetLocation<Skill> Sight = new("disco", SM.SkillType.SIGHT.ToString());
	public static AssetLocation<Skill> Smell = new("disco", SM.SkillType.SMELL.ToString());
	public static AssetLocation<Skill> Taste = new("disco", SM.SkillType.TASTE.ToString());
	public static AssetLocation<Skill> PhysicalInstrument = new("disco", SM.SkillType.PHYSICAL_INSTRUMENT.ToString());
	public static AssetLocation<Skill> ReactionSpeed = new("disco", SM.SkillType.REACTION.ToString());
	public static AssetLocation<Skill> Rhetoric = new("disco", SM.SkillType.RHETORIC.ToString());
	public static AssetLocation<Skill> SavoirFaire = new("disco", SM.SkillType.SAVOIR_FAIRE.ToString());
	public static AssetLocation<Skill> Shivers = new("disco", SM.SkillType.SHIVERS.ToString());
	public static AssetLocation<Skill> Suggestion = new("disco", SM.SkillType.SUGGESTION.ToString());
	public static AssetLocation<Skill> Encyclopedia = new("disco", SM.SkillType.ENCYCLOPEDIA.ToString());
	public static AssetLocation<Skill> VisualCalculus = new("disco", SM.SkillType.VISUAL_CALCULUS.ToString());
	public static AssetLocation<Skill> Volition = new("disco", SM.SkillType.VOLITION.ToString());
}
