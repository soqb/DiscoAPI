using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Runtime.Assets;
using DiscoAPI.Runtime.Components;
using Newtonsoft.Json.Linq;
using UnityEngine;
using PC = PixelCrushers.DialogueSystem;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime;

// <summary>
// All the assets and such which are used by the API itself.
// </summary>
public static class InherentProvider
{
	public static DiscoSource source = null!;

	private class InherentAssetRouter : IAssetRouter
	{
		private Location Location => source.Location;
		IAssetRoute<Sprite> IAssetRouter.Portraits => new LooseSpriteRoute(Location);
	}

	public const string DUMMY_NONE_SKILL = "API DUMMY NONE SKILL";

	public static void Provide()
	{
		Location location = Location.GetFromAssembly(typeof(InherentProvider).Assembly, "discoapi");

		source = DiscoRunner.SourceFromPlugin(DiscoAPIPlugin.Instance, new() { router = new InherentAssetRouter() });
		var assets = source.Manager.Assets;

		var convos = new PCArena<PC.Conversation, Conversation>(mgr => mgr.pcDatabase.conversations);

		assets.Register(new PCArena<PC.Actor, Actor>(mgr => mgr.pcDatabase.actors), true);
		assets.Register(convos, true);
		assets.Register(new PCArena<PC.Variable, Variable>(mgr => mgr.pcDatabase.variables), true);
		assets.Register(new EnumArena<SM.SkillType, Skill>(SkillUtils.RecoverSkill, SkillUtils.SkillIsReal), true);
		assets.Register(new PCProxyArena<Task, PC.Conversation>(convos.Raw, (conv) => conv.FieldExists("display_condition_main")), false);

		DiscoHooks.OnDialogueLoad += OnDialogueBundleLoad;
		DiscoHooks.OnSaveGame += save =>
		{
			var mod = save.GetModData(source.Guid);
			mod.SetObject("you", DiscoRunner.world!.You.Serialize());
			mod.SetObject("world", DiscoRunner.world.Serialize());
		};
		DiscoHooks.OnLoadSavedGame += save =>
		{
			var mod = save.GetModData(source.Guid);
			ModCharacterSheet.Registry.TryDeserialize(mod.GetObject<JToken>("you"), global::World.singleton.you);
			ModWorld.Registry.TryDeserialize(mod.GetObject<JToken>("world"), global::World.singleton);
		};
	}


	public static void OnDialogueBundleLoad()
	{
		// we have to introduce a dummy actor with a simple portrait for the case where no skill is used in the portrait grid.
		source.Add(new Actor("dummy-none-skill", DUMMY_NONE_SKILL) { portraitName = "assets/images/portrait_none.png" });
	}
}
