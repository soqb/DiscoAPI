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
	public static AssetLocation<Skill> Authority = new("authority");
	public static AssetLocation<Skill> Composure = new("composure");
	public static AssetLocation<Skill> Conceptualization = new("conceptualization");
	public static AssetLocation<Skill> HalfLight = new("half-light");
	public static AssetLocation<Skill> Drama = new("drama");
	public static AssetLocation<Skill> Electrochemistry = new("electrochemistry");
	public static AssetLocation<Skill> Empathy = new("empathy");
	public static AssetLocation<Skill> Endurance = new("endurance");
	public static AssetLocation<Skill> EspritDeCorps = new("esprit-de-corps");
	public static AssetLocation<Skill> HandEyeCoordination = new("he-coordination");
	public static AssetLocation<Skill> InlandEmpire = new("inland-empire");
	public static AssetLocation<Skill> Interfacing = new("interfacing");
	public static AssetLocation<Skill> Logic = new("logic");
	public static AssetLocation<Skill> PainThreshold = new("pain-threshold");
	public static AssetLocation<Skill> Perception = new("perception");
	public static AssetLocation<Skill> Hearing = new("hearing");
	public static AssetLocation<Skill> Sight = new("sight");
	public static AssetLocation<Skill> Smell = new("smell");
	public static AssetLocation<Skill> Taste = new("taste");
	public static AssetLocation<Skill> PhysicalInstrument = new("physical-instrument");
	public static AssetLocation<Skill> ReactionSpeed = new("reaction");
	public static AssetLocation<Skill> Rhetoric = new("rhetoric");
	public static AssetLocation<Skill> SavoirFaire = new("savoir-faire");
	public static AssetLocation<Skill> Shivers = new("shivers");
	public static AssetLocation<Skill> Suggestion = new("suggestion");
	public static AssetLocation<Skill> Encyclopedia = new("encyclopedia");
	public static AssetLocation<Skill> VisualCalculus = new("visual-calculus");
	public static AssetLocation<Skill> Volition = new("volition");
}
