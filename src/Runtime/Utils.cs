using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static UnityEngine.ResourceManagement.ResourceManager;

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

