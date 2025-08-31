using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Runtime.Assets;
using DiscoAPI.Runtime.Dialogue;
using FortressOccident;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Voidforge;
using PC = PixelCrushers.DialogueSystem;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime
{
	public static class LuaConsoleManager
	{
		public static void AttachLuaConsole()
		{
			GameObject obj = new GameObject("luaconsolemgr");
			GameObject.DontDestroyOnLoad(obj);

			var console = obj.AddComponent<PixelCrushers.DialogueSystem.LuaConsole>();
			console.firstKey = KeyCode.LeftControl;
			console.secondKey = KeyCode.Return;
		}
	}

	public static class LobbyLoadExecutor
	{
		private static bool isLoaded;
		private static Queue<Action> queue = new();

		public static event Action OnLobbyLoad
		{
			add
			{
				if (isLoaded) value();
				else queue.Enqueue(value);
			}
			remove => throw new Exception("don't");
		}

		internal static void OnLoadLobbyPlease()
		{
			isLoaded = true;
			while (queue.Count > 0) queue.Dequeue().Invoke();

		}

	}

	public static class MainThreadExecutor
	{
		private static Queue<Action> queue = new();

		public static void Queue(Action cb) => queue.Enqueue(cb);

		internal static void DequeueOnMainThreadPlease()
		{
			while (queue.Count > 0) queue.Dequeue().Invoke();
		}
	}

	public static class FormatUtils
	{
		private static readonly Regex RgHyphenlike = new Regex(@"\s|_", RegexOptions.Compiled);
		private static readonly Regex RgDotlike = new Regex(@"[/\\]+", RegexOptions.Compiled);

		private static readonly Regex RgInvalid =
			new Regex(@"[^a-z0-9-\u00C0-\u024F\u1E00-\u1EFF.]", RegexOptions.Compiled);

		private static readonly Regex RgMultiHyphen = new Regex(@"-{2,}", RegexOptions.Compiled);
		private static readonly Regex RgWierdDot = new Regex(@"-.-", RegexOptions.Compiled);

		public static string Slugify(string id)
		{
			id = id.ToLowerInvariant().Normalize();
			id = RgHyphenlike.Replace(id, "-");
			id = RgDotlike.Replace(id, ".");
			id = RgInvalid.Replace(id, "");
			id = RgMultiHyphen.Replace(id, "-");
			id = RgWierdDot.Replace(id, ".");
			id = id.Trim('-');
			return id;
		}

	}

	public static class SkillUtils
	{
		public static Actor? ActorForSkill(SM.SkillType st) => Lookup(st)?.actor.Resolve(DiscoRunner.manager);

		public static string? GetActorSkillName(SM.SkillType type)
		{
			if (type == SM.SkillType.NONE) return InherentProvider.DUMMY_NONE_SKILL;
			if ((int)type <= Skill.VANILLA_MAX) return SM.Skill.actorSkillNames[(int)type];
			else return ActorForSkill(type)?.displayName;
		}

		public static string? GetSkillOrAbilityName(SM.Modifiable modifiable)
		{
			if (modifiable == null)
			{
				DiscoRunner.Log.LogWarning("unexpected null modifiable");
				return null;
			}
			// excellent example of why il2cpp is a bit weird:
			if (modifiable.GetIl2CppType() == Il2CppType.Of<SM.Skill>())
				return GetActorSkillName(modifiable.Cast<SM.Skill>().skillType);
			else if (modifiable.GetIl2CppType() == Il2CppType.Of<SM.Ability>())
				return SM.Ability.GetActorAbilityName(modifiable.Cast<SM.Ability>().abilityType);
			else
				return null;
		}

		public static Skill RecoverSkill(SM.SkillType type)
		{
			string name = SM.Skill.GetActorSkillName(type);
			return new Skill(
				FormatUtils.Slugify(type.ToString()),
				name,
				new AssetLocation<Actor>(FormatUtils.Slugify(name)),
				AbilityFromSunshine(SM.Skill.GetAbility(type))
			);
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

		public static bool SkillIsReal(SM.SkillType skill) => skill switch
		{
			SM.SkillType.NONE
			or SM.SkillType.ALT => false,
			_ => true
		};

		public static bool SkillIsVanilla(SM.SkillType skillType) => (int)skillType <= Skill.VANILLA_MAX;

		public static bool IsExcludedFromPortraits(SM.SkillType type) => type switch
		{
			SM.SkillType.NONE
			or SM.SkillType.CONVALESCENCE
			or SM.SkillType.HEARING
			or SM.SkillType.SIGHT
			or SM.SkillType.SMELL
			or SM.SkillType.TASTE
			or SM.SkillType.ALT => true,
			_ => false,
		};

		public static EnumArena<SM.SkillType, Skill> Skills => (EnumArena<SM.SkillType, Skill>)DiscoRunner.manager.Assets.GetArena<Skill>();

		public static Skill? Lookup(SM.SkillType st) => Skills[Skills.ReverseId(st)];

		public static PC.Actor SkillToPCActor(IAssetRef<Skill> skill)
		{
			return skill.Resolve(DiscoRunner.manager)!.actor.ResolveCrushed(DiscoRunner.manager)!;
		}

		public static void OnDialogueBundleLoad()
		{
			for (int i = SkillUtils.Skills.baseCount; i < SkillUtils.Skills.Count; i++)
			{
				Skill skill = SkillUtils.Skills[i]!;
				SM.SkillType rawSkill = SkillUtils.Skills.GetRaw(i);
				string id = skill.actor.ResolveCrushed()!.LookupValue(ArticyBridge.ARTICY_ID_FIELD);

				ArticyBridge.ARTICY_ID_TO_SKILL_TYPE.Add(id, rawSkill);
				ArticyBridge.ARTICY_ID_TO_SKILL_NAME.Add(id, skill.displayName);
			}

			var newOrbMap = new SM.SkillType[Skill.VANILLA_SKILL_ORB_COUNT + SkillUtils.Skills.Count];
			for (int i = Skill.VANILLA_SKILL_ORB_COUNT; i < newOrbMap.Length; i++)
			{
				newOrbMap[i - 1] = (SM.SkillType)i;
			}

			Array.ConstrainedCopy(ArticyBridge.articyOrbSkillToSunshineOrbSkill, 0,
				newOrbMap, 0, ArticyBridge.articyOrbSkillToSunshineOrbSkill.Count);
			ArticyBridge.articyOrbSkillToSunshineOrbSkill = newOrbMap;
		}
	}


	public static class AssetUtils
	{
		public const string EXTRA_TEXTURE_PREFIX = "\0EXTRA\0";

		public async static DiscoTask<Sprite> LoadPortrait(string textureName, Il2CppSystem.Action<AsyncOperationHandle<Sprite>>? del)
		{
			string hint = textureName;
			try
			{
				if (PixelsToDisco.TryDecodeTextureName(textureName, out string source, out string path))
				{
					DiscoTask<Sprite?> task = DiscoRunner.GetSource(source)!.Router.Portraits.Get(path);
					hint = $"{source}:{path}";
					if (await task is Sprite s)
					{
						if (del != null) task.AsAddressableOperation().add_Completed(del!);
						return s;
					}
				}
				else if (await ActorsPortraitsBundleManager.LoadPortraitSpriteAsync(textureName, del) is Sprite s)
				{
					return s;
				}
			}
			catch (Exception ex)
			{
				DiscoRunner.Log.LogError($"failed to load portrait \"{hint}\"");
				DiscoRunner.Log.LogError(ex);
			}

			// if everything went wrong, load fallback protrait:
			try
			{
				const string FALLBACK = "assets/images/missing_texture_lol.png";
				DiscoTask<Sprite> task = InherentProvider.source.Router.Portraits.Get(FALLBACK)!;
				await task;
				if (del != null) task.AsAddressableOperation().add_Completed(del!);
				return task.Result;
			}
			catch (Exception ex)
			{
				DiscoRunner.Log.LogError($"failed to even load fallback portrait");
				DiscoRunner.Log.LogError(ex);

				// at this point there's nothing else we can do..
				return null!;
			}
		}
	}

	/// <summary>
	/// A pretty heavy-handed tool to allow property customization of runtime-created <code>ScriptableObject</code>s.
	/// </summary>
	public static class ScriptableObjectHook<T> where T : ScriptableObject
	{
		public static ThreadLocal<Action<T>?> cb = new();

		public static T CreateInstanceWith(Action<T> onEnable)
		{
			cb.Value = onEnable;
			return ScriptableObject.CreateInstance<T>();
		}

		private static void PreOnEnable(T __instance)
		{
			var c = cb.Value;
			if (c == null) return;

			c.Invoke(__instance);
			cb.Value = null;
		}

		static ScriptableObjectHook()
		{
			var enable = typeof(T).GetMethod("OnEnable", 0, new Type[0]);
			if (enable == null)
				throw new InvalidOperationException(
					$"Could not hook into the creation of {typeof(T)} since it does not have an 'OnEnable' method");
			DiscoRunner.Harmony.Patch(enable,
				prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((T t) => PreOnEnable(t))));
		}
	}

	public static class ArchetypeUtils
	{
		public static SunshineCharacterTemplate ToSunshineTemplate(CharacterArchetype archetype)
		{
			var template = ScriptableObject.CreateInstance<SunshineCharacterTemplate>();
			template.Description = archetype.description;
			template.name = archetype.name;
			template.Intellect = archetype.intellect;
			template.Psyche = archetype.psyche;
			template.Fysique = archetype.fysique;
			template.Motorics = archetype.motorics;
			template.signatureSkill = archetype.signatureSkill != null
				? SkillUtils.Skills.GetRaw(archetype.signatureSkill.ResolveId())
				: SM.SkillType.NONE;

			return template;
		}
	}

	public static class Il2CppExtensions
	{
		public static Il2CppReferenceArray<T> Resize<T>(this Il2CppReferenceArray<T> original, int newSize)
			where T : Il2CppObjectBase
		{
			// i am trusting that this does not leak 'original'
			var newArr = new T[newSize];
			if (newSize >= original.Length)
			{
				original.CopyTo(newArr, 0);
			}
			else
			{
				for (int i = 0; i < newArr.Length; i++)
				{
					newArr[i] = original[i];
				}
			}

			return newArr;
		}

		public static Il2CppStructArray<T> Resize<T>(this Il2CppStructArray<T> original, int newSize)
			where T : unmanaged
		{
			var span = new ReadOnlySpan<T>(original);
			var newArr = new Span<T>(new T[newSize]);

			if (newSize >= original.Length)
			{
				span.CopyTo(newArr);
			}
			else
			{
				span = span[..newSize];
				span.CopyTo(newArr);
			}

			return newArr.ToArray();
		}
	}

	public static class AreaUtils
	{
		public static IAssetArena<Area> Areas => DiscoRunner.manager.Assets.GetArena<Area>();
		public static bool IsModdedArea(string scenePath) => Areas.Any(a => a.scenePath == scenePath);
		public static Area? FromScenePath(string scenePath) => Areas.FirstOrDefault(a => a.scenePath == scenePath);
		public static Area? FromSceneName(string sceneName) => Areas.FirstOrDefault(a => a.scenePath.Contains(sceneName));

		public static async DiscoTask<bool> LoadModScene(Area area)
		{
			var src = DiscoRunner.GetSource(area.source!);
			if (src == null) return false;
			var bundle = await src.Router.SceneBundle.Get();

			if (bundle == null)
			{
				DiscoRunner.Log.LogError($"a request to load scene bundle containing {area.scenePath} failed!");
				return false;
			}

			var foundScenePath = bundle.GetAllScenePaths().FirstOrDefault(s => s == area.scenePath);
			if (foundScenePath == null)
			{
				DiscoRunner.Log.LogError($"could not load {area.scenePath} from bundle {bundle.name}!");
				return false;
			}

			await SceneManager.LoadSceneAsync(foundScenePath, LoadSceneMode.Additive);
			return true;
		}

		public static async DiscoTask<NavMeshData?> LoadNavmeshData(Area area)
		{
			var src = DiscoRunner.GetSource(area.source!);
			if (src == null) return null;
			var navmesh = await src.Router.NavMeshes.Get(area.navMeshPath);
			if (navmesh == null)
			{
				DiscoRunner.Log.LogError($"failed to load navmesh data at {area.navMeshPath} for source {area.source}");
				return null;
			}
			return navmesh;
		}


		public static void ChangeArea(string areaId, string destinationId, bool isGameLoad, bool showLoadingScreen,
			bool hideLoadingScreen)
		{
			SingletonScriptable<ApplicationManager>.Singleton.ChangeArea(areaId, destinationId, isGameLoad, showLoadingScreen, hideLoadingScreen);
		}
	}
}

namespace System.Runtime.CompilerServices
{

	internal class IsUnmanagedAttribute : Attribute
	{
		public IsUnmanagedAttribute()
		{
		}
	}
}
