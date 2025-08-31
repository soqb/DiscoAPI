using System;
using System.Runtime.CompilerServices;
using UnityEngine.ResourceManagement.AsyncOperations;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using Il2CppEnumerator = Il2CppSystem.Collections.IEnumerator;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;

namespace DiscoAPI.Runtime;

public class Il2CppAsyncOperationHandleAwaiter<T> : INotifyCompletion
{
	private AsyncOperationHandle<T> inner;

	public Il2CppAsyncOperationHandleAwaiter(AsyncOperationHandle<T> inner)
	{
		this.inner = inner;
	}

	public void OnCompleted(Action cont)
	{
		// this is like the only surefire way of making a generic continuation action..
		inner.m_InternalOp.Task.ContinueWith((Action<Il2CppSystem.Threading.Tasks.Task>)((_) => cont()));
	}

	public bool IsCompleted => inner.m_InternalOp.IsDone;
	public T? GetResult() => inner.m_InternalOp.Result;
}

public struct Il2CppAsyncOperationHandleAwaiter : INotifyCompletion
{
	private AsyncOperationHandle inner;

	public Il2CppAsyncOperationHandleAwaiter(AsyncOperationHandle inner)
	{
		this.inner = inner;
	}

	public void OnCompleted(Action cont)
	{
		// this is like the only surefire way of making a generic continuation action..
		inner.m_InternalOp.Task.ContinueWith((Action<Il2CppSystem.Threading.Tasks.Task>)((_) => cont()));
	}

	public bool IsCompleted => inner.m_InternalOp.IsDone;
	public void GetResult() { }
}

public struct Il2CppAsyncOperationAwaiter : INotifyCompletion
{
	private AsyncOperation inner;

	public Il2CppAsyncOperationAwaiter(AsyncOperation inner)
	{
		this.inner = inner;
	}

	public void OnCompleted(Action cont)
	{
		inner.add_completed((Action<AsyncOperation>)((_) => cont()));
	}

	public bool IsCompleted => inner.isDone;
	public void GetResult() { }
}

public struct Il2CppCoroutineAwaiter : INotifyCompletion
{
	public bool IsCompleted { get; private set; }
	private Queue<Action> completion = new();

	public Il2CppCoroutineAwaiter() { }

	public static Il2CppEnumerator Wrap(Il2CppEnumerator cor, out Il2CppCoroutineAwaiter awaiter)
	{
		Il2CppCoroutineAwaiter me = new();
		IEnumerator Chain()
		{
			while (cor.MoveNext()) yield return cor.Current;
			me.Complete();
		}

		awaiter = me;
		return Chain().WrapToIl2Cpp();
	}

	private void Complete()
	{
		IsCompleted = true;
		while (completion.Count > 0) completion.Dequeue()();
	}

	public void OnCompleted(Action cont) => completion.Enqueue(cont);

	public void GetResult() { }
}

// these currently wrap .NET tasks, but we might want to use something else. (maybe Unity Jobs?)
[AsyncMethodBuilder(typeof(DiscoTaskBuilder))]
public partial struct DiscoTask
{
	private Task inner;


	public DiscoTask(Task task)
	{
		inner = task;
	}

	public TaskAwaiter GetAwaiter() => inner?.GetAwaiter() ?? new TaskAwaiter();

	public void ContinueWith(Action<DiscoTask> value) => inner?.ContinueWith((task) => value(new(task)));

	public static DiscoTask MainThread(Action inner)
	{
		TaskCompletionSource tcs = new();
		MainThreadExecutor.Queue(() =>
		{
			inner();
			tcs.SetResult();
		});

		return new(tcs.Task);
	}

	public static DiscoTask<T> MainThread<T>(Func<T> inner)
	{
		TaskCompletionSource<T> tcs = new();
		MainThreadExecutor.Queue(() => tcs.SetResult(inner()));

		return new(tcs.Task);
	}

	public static DiscoTask Ready() => new(new Task(() => { }));
	public static DiscoTask<T> Ready<T>(T t) => new(new Task<T>(() => t));
}

[AsyncMethodBuilder(typeof(DiscoTaskBuilder<>))]
public struct DiscoTask<T>
{
	private Task<T> inner;

	public DiscoTask(Task<T> task)
	{
		inner = task;
	}

	public static implicit operator DiscoTask(DiscoTask<T> task) => new(task.inner);

	public TaskAwaiter<T> GetAwaiter() => inner?.GetAwaiter() ?? new TaskAwaiter<T>();
	public T Result => inner == null ? default(T)! : inner.Result;

	public DiscoTask ContinueWith(Action<DiscoTask<T>> value)
	{
		if (inner != null) return new(inner.ContinueWith(task => value(new(task))));
		else
		{
			value(DiscoTask.Ready<T>(default!));
			return DiscoTask.Ready();
		}
	}

	public DiscoTask<T> ContinueWith(Func<DiscoTask<T>, T> value)
	{
		if (inner != null) return new(inner.ContinueWith(task => value(new(task))));
		else return DiscoTask.Ready(value(DiscoTask.Ready<T>(default!)));
	}
}

public struct DiscoTaskBuilder
{
	private AsyncTaskMethodBuilder inner;

	public static DiscoTaskBuilder Create() => new() { inner = AsyncTaskMethodBuilder.Create() };
	public void Start<M>(ref M sm) where M : IAsyncStateMachine => inner.Start(ref sm);
	public void SetStateMachine(IAsyncStateMachine sm) => inner.SetStateMachine(sm);
	public void SetResult() => inner.SetResult();
	public void SetException(Exception exception) => inner.SetException(exception);
	public DiscoTask Task => new(inner.Task);

	public void AwaitOnCompleted<A, M>(ref A awaiter, ref M stateMachine)
	where A : INotifyCompletion where M : IAsyncStateMachine
	{
		inner.AwaitOnCompleted(ref awaiter, ref stateMachine);
	}
	public void AwaitUnsafeOnCompleted<A, M>(ref A awaiter, ref M stateMachine)
	where A : ICriticalNotifyCompletion where M : IAsyncStateMachine
	{
		inner.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
	}
}

public struct DiscoTaskBuilder<T>
{
	private AsyncTaskMethodBuilder<T> inner;

	public static DiscoTaskBuilder<T> Create() => new() { inner = AsyncTaskMethodBuilder<T>.Create() };
	public void Start<M>(ref M sm) where M : IAsyncStateMachine => inner.Start(ref sm);
	public void SetStateMachine(IAsyncStateMachine sm) => inner.SetStateMachine(sm);
	public void SetResult(T result) => inner.SetResult(result);
	public void SetException(Exception exception) => inner.SetException(exception);
	public DiscoTask<T> Task => new(inner.Task);

	public void AwaitOnCompleted<A, M>(ref A awaiter, ref M stateMachine)
	where A : INotifyCompletion where M : IAsyncStateMachine
	{
		inner.AwaitOnCompleted(ref awaiter, ref stateMachine);
	}
	public void AwaitUnsafeOnCompleted<A, M>(ref A awaiter, ref M stateMachine)
	where A : ICriticalNotifyCompletion where M : IAsyncStateMachine
	{
		inner.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
	}
}

public static class AsyncUtils
{
	public static void StartAndForgetCoroutine(Il2CppSystem.Collections.IEnumerator cor)
	{
		Voidforge.UpdateManager.singleton.StartCoroutine(cor);
	}

	public static Il2CppCoroutineAwaiter StartCoroutine(Il2CppSystem.Collections.IEnumerator cor)
	{
		Il2CppCoroutineAwaiter.Wrap(cor, out var awaiter);
		StartAndForgetCoroutine(cor);
		return awaiter;
	}

	public static async DiscoTask<T> WrapOp<T, O>(O op, Func<O, T> result) where O : AsyncOperation
	{
		await op;
		return result(op)!;
	}
}


public struct AsyncOperationCompletionSource<T> where T : Il2CppObjectBase
{
	public AsyncOperationBase<T> Operation { get; private set; }
	public AsyncOperationCompletionSource()
	{
		ResourceManager.CompletedOperation<T?> op = Addressables.ResourceManager.CreateOperation<ResourceManager.CompletedOperation<T?>>(
			Il2CppType.Of<ResourceManager.CompletedOperation<T?>>(),
			Il2CppType.Of<ResourceManager.CompletedOperation<T?>>().GetHashCode(),
			null,
			Addressables.ResourceManager.m_ReleaseOpNonCached
		);

		op.m_RM = Addressables.ResourceManager;
		op.IsRunning = true;
		op.HasExecuted = false;
		op.IncrementReferenceCount();
		op.m_UpdateCallbacks = op.m_RM.m_UpdateCallbacks;

		op.HasExecuted = true;

		Operation = op.Cast<AsyncOperationBase<T>>();
	}

	// we could just do `op.Handle` but
	// uhh um uhh.. there was uhh a reason for this.. uhh..
	public AsyncOperationHandle<T> Handle => new(Operation.Cast<IAsyncOperation>());

	public void SetResult(T result)
	{
		Operation.Complete(result, true, (string?)null, false);
	}

	public void SetException(Exception result)
	{
		// FIXME: we should probably preserve the error with a little more fidelity than this:
		Operation.Complete(null!, false, result.ToString(), false);
	}
}

public struct AsyncOperationCompletionSource
{
	private AsyncOperationCompletionSource<Il2CppSystem.Object> inner;
	public IAsyncOperation Operation => new IAsyncOperation(inner.Operation.Pointer);
	public AsyncOperationHandle Handle => inner.Handle;

	public void SetResult() => inner.SetResult(new());
	public void SetException(Exception result) => inner.SetException(result);
}

public static class Il2CppAsyncExtensions
{
	public static Il2CppAsyncOperationHandleAwaiter<T> GetAwaiter<T>(this AsyncOperationHandle<T> handle) => new(handle);
	public static Il2CppCoroutineAwaiter GetAwaiter(this Il2CppSystem.Collections.IEnumerator cor)
	{
		return AsyncUtils.StartCoroutine(cor);
	}

	public static Il2CppAsyncOperationAwaiter GetAwaiter(this AsyncOperation op) => new(op);
}

public static class AsyncExtensions
{
	public static IAsyncOperation AsAddressableOperation(this DiscoTask task)
	{
		AsyncOperationCompletionSource opcs = new();
		task.ContinueWith(task => opcs.SetResult());
		return opcs.Operation;
	}

	public static AsyncOperationBase<T> AsAddressableOperation<T>(this DiscoTask<T> task) where T : Il2CppObjectBase?
	{
#nullable disable
		AsyncOperationCompletionSource<T> opcs = new();
		task.ContinueWith(task => opcs.SetResult(task.Result!));
		return opcs.Operation;
#nullable enable
	}

	public static Il2CppEnumerator ToCoroutine(this DiscoTask task)
	{
		static IEnumerator Wrap(TaskAwaiter task)
		{
			if (!task.IsCompleted) yield return null;
		}

		return Wrap(task.GetAwaiter()).WrapToIl2Cpp();
	}

	public static Il2CppEnumerator ToCoroutine<T>(this DiscoTask<T> task) => ToCoroutine((DiscoTask)task);
}
