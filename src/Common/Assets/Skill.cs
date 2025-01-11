using Newtonsoft.Json;

namespace DiscoAPI.Common.Assets;

public enum AbilityType
{
	Int = 1,
	Psy = 2,
	Fys = 3,
	Mot = 4,
}

public record Skill : Asset, IAssetRef<Skill>
{
	[JsonProperty("name")]
	public readonly string displayName;
	public readonly IAssetRef<Dialogue.Actor> actor;
	public readonly AbilityType ability;

	public Skill(string id, string displayName, IAssetRef<Dialogue.Actor> actor, AbilityType ability) : base(id)
	{
		this.displayName = displayName;
		this.actor = actor;
		this.ability = ability;
	}

	[JsonIgnore]
	public new AssetLocation<Skill> Location => new(source, id);
	Skill? IAssetRef<Skill>.Resolve(IDiscoManager mgr) => (Skill?)((IAssetRef)this).Resolve(mgr);

	public const int VANILLA_SKILL_COUNT = 29;
	public const int VANILLA_SKILL_PORTRAIT_COUNT = 24;
	public const int VANILLA_MAX = 30;
}

public class Skills
{
	public static AssetLocation<Skill> Authority = new("AUTHORITY");
	public static AssetLocation<Skill> Composure = new("COMPOSURE");
	public static AssetLocation<Skill> Conceptualization = new("CONCEPTUALIZATION");
	public static AssetLocation<Skill> HalfLight = new("HALF_LIGHT");
	public static AssetLocation<Skill> Drama = new("DRAMA");
	public static AssetLocation<Skill> Electrochemistry = new("ELECTROCHEMISTRY");
	public static AssetLocation<Skill> Empathy = new("EMPATHY");
	public static AssetLocation<Skill> Endurance = new("ENDURANCE");
	public static AssetLocation<Skill> EspritDeCorps = new("ESPRIT_DE_CORPS");
	public static AssetLocation<Skill> HandEyeCoordination = new("HE_COORDINATION");
	public static AssetLocation<Skill> InlandEmpire = new("INLAND_EMPIRE");
	public static AssetLocation<Skill> Interfacing = new("INTERFACING");
	public static AssetLocation<Skill> Logic = new("LOGIC");
	public static AssetLocation<Skill> PainThreshold = new("PAIN_THRESHOLD");
	public static AssetLocation<Skill> Perception = new("PERCEPTION");
	public static AssetLocation<Skill> Hearing = new("HEARING");
	public static AssetLocation<Skill> Sight = new("SIGHT");
	public static AssetLocation<Skill> Smell = new("SMELL");
	public static AssetLocation<Skill> Taste = new("TASTE");
	public static AssetLocation<Skill> PhysicalInstrument = new("PHYSICAL_INSTRUMENT");
	public static AssetLocation<Skill> ReactionSpeed = new("REACTION");
	public static AssetLocation<Skill> Rhetoric = new("RHETORIC");
	public static AssetLocation<Skill> SavoirFaire = new("SAVOIR_FAIRE");
	public static AssetLocation<Skill> Shivers = new("SHIVERS");
	public static AssetLocation<Skill> Suggestion = new("SUGGESTION");
	public static AssetLocation<Skill> Encyclopedia = new("ENCYCLOPEDIA");
	public static AssetLocation<Skill> VisualCalculus = new("VISUAL_CALCULUS");
	public static AssetLocation<Skill> Volition = new("VOLITION");
}
