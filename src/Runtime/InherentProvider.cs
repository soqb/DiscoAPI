using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Runtime.Assets;
using DiscoAPI.Runtime.Components;
using DiscoAPI.Runtime.VirtualTextures;
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
	public const string MISSING_PORTRAIT_PATH = "assets/textures/portrait_missing_placeholder.png";
	public static Location Location { get; } = Location.GetFromAssembly(typeof(InherentProvider).Assembly, "discoapi");

	public static void Provide()
	{
		source = DiscoRunner.SourceFromPlugin(DiscoAPIPlugin.Instance, new() { router = new InherentAssetRouter() });
		var assets = source.Manager.Assets;

		var convos = new PCArena<PC.Conversation, Conversation>(mgr => mgr.pcDatabase.conversations);

		assets.Register(new PCArena<PC.Actor, Actor>(mgr => mgr.pcDatabase.actors), true);
		assets.Register(convos, true);
		assets.Register(new PCArena<PC.Variable, Variable>(mgr => mgr.pcDatabase.variables), true);
		assets.Register(new EnumArena<SM.SkillType, Skill>(SkillUtils.RecoverSkill, SkillUtils.SkillIsReal), true);
		assets.Register(new PCProxyArena<Task, PC.Conversation>(convos.Raw, (conv) => conv.FieldExists("display_condition_main")), false);
		assets.Register(new GenericArena<Area>(), false);
		
		DiscoHooks.OnDialogueLoad += OnDialogueBundleLoad;
		DiscoHooks.OnSaveGame += save =>
		{
			var mod = save.GetModData(source.Guid);
			mod.SetObject("you", DiscoRunner.world!.You().Serialize());
			mod.SetObject("world", DiscoRunner.world!.Serialize());
		};
		DiscoHooks.OnLoadSavedGame += save =>
		{
			var mod = save.GetModData(source.Guid);
			CharacterComponents.Of(global::World.singleton.you).TryDeserialize(mod.GetObject<JToken>("you"));
			WorldComponents.Of(global::World.singleton).TryDeserialize(mod.GetObject<JToken>("world"));
		};

		// string path = Location.Get("assets/textures/hello_revachol_page2.png")!;
		// CustomVirtualTextureManager.RegisterOverrides("e8f9498d308bbbac312e6be93ac820bd", new VirtualTextureOverrides()
		// {
		// 	substitutions = { new PageSubstitution(new(1088, 1088, 2176, 2176), BitmapPageProvider.FromFile(path)) }
		// });

		AdHocTextureConfig config = new(
			BitmapPageProvider.FromFile(Location.Get("assets/textures/adhoc_bg.png")!),
			new(4352, 4352)
		);
		CustomVirtualTextureManager.RegisterAdHoc(config);
	}


	public static void OnDialogueBundleLoad()
	{
		// we have to introduce a dummy actor with a simple portrait for the case where no skill is used in the portrait grid.
		source.Add(new Actor("dummy-none-skill", DUMMY_NONE_SKILL) { portraitName = "assets/images/portrait_none.png" });
	}
}
