using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Runtime.Assets;
using DiscoAPI.Runtime.Dialogue;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static UnityEngine.ResourceManagement.ResourceManager;
using PC = PixelCrushers.DialogueSystem;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime;

public static class MainThreadExecutor
{
	private static Queue<Action> queue = new();

	public static void Queue(Action cb) => queue.Enqueue(cb);

	internal static void DequeueOnMainThreadPlease()
	{
		while (queue.Count > 0)
		{
			queue.Dequeue().Invoke();
		}
	}
}

public static class FormatUtils
{
	private static readonly Regex RgHyphenlike = new Regex(@"\s|_", RegexOptions.Compiled);
	private static readonly Regex RgDotlike = new Regex(@"[/\\]+", RegexOptions.Compiled);
	private static readonly Regex RgInvalid = new Regex(@"[^a-z0-9-\u00C0-\u024F\u1E00-\u1EFF.]", RegexOptions.Compiled);
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
			string id = skill.actor.ResolveCrushed()!.LookupValue(ArticyBridge.ARTICY_ID_FIELD);

			ArticyBridge.ARTICY_ID_TO_SKILL_TYPE.Add(id, SkillUtils.Skills.GetRaw(i));
			ArticyBridge.ARTICY_ID_TO_SKILL_NAME.Add(id, skill.displayName);
		}
	}
}


public static class AssetUtils
{
	public const string EXTRA_TEXTURE_PREFIX = "\0EXTRA\0";
	public static AsyncOperationHandle<Sprite?> LoadPortrait(string textureName, Il2CppSystem.Action<AsyncOperationHandle<Sprite?>> del)
	{
		if (PixelsToDisco.TryDecodeTextureName(textureName, out string? source, out string? path))
		{
			var handle = DiscoRunner.GetSource(source)!.Router.Portraits.Get(path);
			handle.add_Completed(del);
			return handle;
		}
		else return ActorsPortraitsBundleManager.LoadPortraitSpriteAsync(textureName, del);

	}

	public delegate void Complete<T>(T? result, string? error);

	public static AsyncOperationHandle<T?> SpoofHandle<T>(Action<Complete<T>> execute) where T : Il2CppObjectBase
	{
		return SpoofHandle<T>(execute, new());
	}

	public static AsyncOperationHandle<T?> SpoofHandle<T>(Action<Complete<T>> execute, AsyncOperationHandle dep) where T : Il2CppObjectBase
	{
		// please don't ask why this is like this.

		CompletedOperation<T?> op = Addressables.ResourceManager.CreateOperation<CompletedOperation<T?>>(
			Il2CppType.Of<CompletedOperation<T?>>(),
			Il2CppType.Of<CompletedOperation<T?>>().GetHashCode(),
			null,
			Addressables.ResourceManager.m_ReleaseOpNonCached
		);

		op.m_RM = Addressables.ResourceManager;
		op.IsRunning = true;
		op.HasExecuted = false;
		op.IncrementReferenceCount();
		op.m_UpdateCallbacks = op.m_RM.m_UpdateCallbacks;

		void Execute()
		{
			execute((res, err) =>
			{
				bool success = string.IsNullOrEmpty(err);
				if (!success) res = null;
				op.Complete(res, success, err, false);
			});
			op.HasExecuted = true;
		}

		if (dep.IsValid() && !dep.IsDone) dep.add_Completed((Action<AsyncOperationHandle>)(_ => Execute()));
		else Execute();

		return new(op.Cast<IAsyncOperation>());
	}
}

