using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Runtime.Assets;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static UnityEngine.ResourceManagement.ResourceManager;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime;

public static class MainThreadExecutor
{
	private static Queue<Action> queue = new();

	public static void Queue(Action cb) => queue.Enqueue(cb);

	internal static void DequeueOnMainThread()
	{
		while (queue.Count > 0)
		{
			queue.Dequeue().Invoke();
		}
	}
}

public static class FormatUtils
{
	public static string Slugify(string id)
	{
		id = id.ToLowerInvariant().Normalize();
		id = Regex.Replace(id, @"\s|_", "-", RegexOptions.Compiled);
		id = Regex.Replace(id, @"[^a-z0-9-\u00C0-\u024F\u1E00-\u1EFF.]", "", RegexOptions.Compiled);
		id = Regex.Replace(id, @"-{2,}", "-", RegexOptions.Compiled);
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
}


public static class AssetUtils
{
	public delegate void Complete<T>(T? result, string? error);


	public static AsyncOperationHandle<T?> SpoofHandle<T>(Action<Complete<T>> execute) where T : Il2CppObjectBase
	{
		return SpoofHandle<T>(execute, new AsyncOperationHandle());
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

		return op.Handle;
	}
}

