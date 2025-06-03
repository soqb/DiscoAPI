using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace DiscoAPI.Runtime;

internal class DelegateConsumer<D> where D : Delegate
{
	public DiscoHook<D> hook;
	public IEnumerator<D> delegates;

	public DelegateConsumer(DiscoHook<D> hook)
	{
		this.hook = hook;
		this.delegates = hook.actions.GetEnumerator();
	}

	// it seems absurd..
	// there's literally no non-asm way to "wrap" the execution of arbitrary delegates. it is simply not possible.
	// thus, we go whole-hog and il generate as much as possible to keep performance in check.
	private static DynamicMethod GenerateInvocationMethod()
	{
		MethodInfo dlg = typeof(D).GetMethod("Invoke")!;
		if (dlg.ReturnType != typeof(void))
			throw new ArgumentException($"cannot create a hook from a delegate with a return type (it returns {dlg.ReturnType}).");

		Type[] parameters = dlg.GetParameters().Select(param => param.GetType()).ToArray();
		DynamicMethod method = new(
			"InvokeDelegates",
			null,
			new Type[] { typeof(DelegateConsumer<D>) }.Concat(parameters).ToArray(),
			typeof(DelegateConsumer<D>),
			true
		);

		ILGenerator il = method.GetILGenerator();
		il.DeclareLocal(typeof(D));
		il.DeclareLocal(typeof(Exception));

		// move the enumerator over:
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldfld, typeof(DelegateConsumer<D>).GetField(nameof(delegates))!);
		il.Emit(OpCodes.Callvirt, SymbolExtensions.GetMethodInfo((IEnumerator<D> i) => i.MoveNext()));

		// check for completion & maybe return:
		Label skipReturn = il.DefineLabel();
		il.Emit(OpCodes.Brtrue_S, skipReturn);
		il.Emit(OpCodes.Ret);
		il.MarkLabel(skipReturn);

		// get the current delegate:
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldfld, typeof(DelegateConsumer<D>).GetField(nameof(delegates))!);
		il.Emit(OpCodes.Callvirt, typeof(IEnumerator<D>).GetProperty(nameof(IEnumerator<D>.Current))!.GetGetMethod()!);
		il.Emit(OpCodes.Stloc_0);

		// try call the current delegate:
		Label trial = il.BeginExceptionBlock();
		il.Emit(OpCodes.Ldloc_0);
		for (int i = 0; i < parameters.Length; i++) il.Emit(OpCodes.Ldarg, i + 1);
		il.Emit(OpCodes.Callvirt, dlg);
		il.Emit(OpCodes.Leave, trial);

		// catch exception in the current delegate:
		il.BeginCatchBlock(typeof(Exception));
		il.Emit(OpCodes.Stloc_1);
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldfld, typeof(DelegateConsumer<D>).GetField(nameof(hook))!);
		il.Emit(OpCodes.Ldloc_1);
		il.Emit(OpCodes.Call, SymbolExtensions.GetMethodInfo((DiscoHook<D> i, Exception e) => i.Report(e)));
		il.EndExceptionBlock();


		// call recursive because that seemed easier:
		il.Emit(OpCodes.Ldarg_0);
		for (int i = 0; i < parameters.Length; i++) il.Emit(OpCodes.Ldarg, i + 1);
		il.Emit(OpCodes.Call, method);
		il.Emit(OpCodes.Ret);

		return method;
	}

	private static DynamicMethod method = GenerateInvocationMethod();

	public D Invocation => (D)method.CreateDelegate(typeof(D), this);
}

public class DiscoHook<D> where D : Delegate
{
	internal readonly List<D> actions = new();

	public string id;
	public D Invoke => new DelegateConsumer<D>(this).Invocation;

	public DiscoHook(string id)
	{
		this.id = id;
	}

	public void Add(D action) => actions.Add(action);
	public void Remove(D action) => actions.Remove(action);
	internal void Report(Exception e)
	{
		DiscoRunner.Log.LogError($"error in hook '{id}':");
		DiscoRunner.Log.LogError(e);
	}
}

public class DiscoHook : DiscoHook<Action>
{
	public DiscoHook(string id) : base(id) { }
}

public static class DiscoHooks
{
	public static event Action OnUpdate
	{
		add => DiscoRunner.update.Add(value);
		remove => DiscoRunner.update.Remove(value);
	}
	public static event Action OnLoad
	{
		add => DiscoRunner.load.Add(value);
		remove => DiscoRunner.load.Remove(value);
	}
	public static event Action OnSceneLoad
	{
		add => DiscoRunner.sceneLoad.Add(value);
		remove => DiscoRunner.sceneLoad.Remove(value);
	}
	public static event Action OnDialogueLoad
	{
		add => DiscoRunner.dialogueLoad.Add(value);
		remove => DiscoRunner.dialogueLoad.Remove(value);
	}
}
