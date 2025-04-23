namespace DiscoAPI.Runtime.Components;

public class ModWorld : ModEntity<ModWorld, global::World>
{
	public static ModEntityRegistry<ModWorld, global::World> Registry { get; }
		= new(new CWTEntityMap<ModWorld, global::World>(s => new(s)));
	public static ModWorld Of(global::World w) => Registry.EntityOf(w);

	protected override IComponentStore Components { get; } = new DictComponentStore();

	public bool IsRunning => EntityBase.isRunning;
	public ModCharacterSheet You => ModCharacterSheet.Of(EntityBase.you);

	private ModWorld(World disco) : base(Registry, disco) { }
}
