using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Runtime.Assets;
using DiscoAPI.Runtime.Components;
using DiscoAPI.Runtime.Utils;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json.Linq;
using Sunshine;
using UnityEngine;
using Voidforge;
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
		IAssetRoute<Sprite> IAssetRouter.Sprites => new LooseSpriteRoute(Location.Get("assets", "images"));
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
		assets.Register(new EnumArena<SM.EffectType, CharacterEffect>(ModifierUtils.RecoverEffect, ModifierUtils.EffectIsReal), true);
		assets.Register(new PCProxyArena<Task, PC.Conversation>(convos.Raw, (conv) => conv.FieldExists("display_condition_main")), false);
		assets.Register(new GenericArena<Area>(), false);
		assets.Register(new GenericArena<CharacterArchetype>(), false);
		assets.Register(new EnumArena<Reputation, Common.Assets.Reputation>(ReputationUtils.RecoverRep, ReputationUtils.RepIsReal), true);
		assets.Register(new GenericArena<Thought>(), false);

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

		// CustomVirtualTextureManager.RegisterOverrides("e8f9498d308bbbac312e6be93ac820bd", new VirtualTextureOverrides()
		// {
		// 	substitutions = { new PageSubstitution(
		// 		new(1024, 1024, 2048, 2048),
		// 		BitmapPageProvider.FromFile(Location.Get("assets/textures/hello_revachol_page2.png")!)
		// 	) }
		// });

		// AdHocTextureConfig textureConfig = new(
		// 	new(4096, 4096),
		// 	BitmapPageProvider.FromFile(Location.Get("assets/textures/adhoc_bg.png")!)
		// );

		// var collection = CustomVirtualTextureManager.InternNewCollection("the-big-collection");
		// collection.VirtualTextures.Add(ModVirtualTexture.CreateAdHoc(textureConfig));
	}


	public static void OnDialogueBundleLoad()
	{
		AssembleSunshineData();
		ReputationUtils.RegisterModReputations();
		
		// we have to introduce a dummy actor with a simple portrait for the case where no skill is used in the portrait grid.
		source.Add(new Actor("dummy-none-skill", DUMMY_NONE_SKILL) { portraitName = "portrait_none.png" });
	}

	public static void AssembleSunshineData()
	{
		var dataHolder = new GameObject("DiscoAPI Data");

		var thoughtHolder = new GameObject("Thoughts");
		thoughtHolder.transform.parent = dataHolder.transform;
		var modThoughts = DiscoRunner.manager.Assets.GetArena<Thought>();
		var modProjects = new ThoughtListItem[modThoughts.Count];
		var baseProjectList = SingletonComponent<ThoughtCabinetProjectList>.Singleton;
		for (var i = 0; i < modThoughts.Count; i++)
		{
			var thought = modThoughts[i];
			if (thought == null) continue;
			var thtObject = new GameObject(thought.id);
			thtObject.transform.parent = thoughtHolder.transform;

			var project = thought.AttachComponent(thtObject);
			modProjects[i] = new ThoughtListItem()
			{
				name = thought.id,
				project = project
			};
		}

		Il2CppReferenceArray<ThoughtListItem> newList;
		if (DiscoRunner.globalConfig.disableDiscoThoughts)
		{
			newList = modProjects;
		}
		else
		{
			var baseProjectCount = baseProjectList.projects.Count;
			newList = baseProjectList.projects.Resize(baseProjectCount + modProjects.Length);
			for (int i = 0; i < modProjects.Length; i++)
			{
				newList[i + baseProjectCount] = modProjects[i];
			}
		}

		baseProjectList.projects = newList;
		baseProjectList.RefreshCache();
		SingletonComponent<ThoughtManager>.Singleton.ReinitializeThoughtsList();

		Object.DontDestroyOnLoad(dataHolder);
	}
}
