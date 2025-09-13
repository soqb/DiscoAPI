using SM = Sunshine.Metric;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Assets;

namespace DiscoAPI.Runtime.Components;

public static class WorldComponents
{
	public static ModEntityRegistry<World> Registry { get; }
		= new(new PersistentEntityMap<World>(s => new(Registry!, s)));

	public static ModEntity<World> Of(World s) => Registry.EntityOf(s);

	public static ModEntity<SM.CharacterSheet> You(this ModEntity<World> s)
	{
		return CharacterComponents.Of(s.EntityBase.you);
	}

	public static IAssetArena<Area> Areas => DiscoRunner.manager.Assets.GetArena<Area>();

}
