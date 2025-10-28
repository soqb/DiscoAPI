using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

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
		
		public static Il2CppStringArray Resize(this Il2CppStringArray original, int newSize) 
		{
			var newArr = new string[newSize];
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
}

/// <summary>
/// Helper attribute to tell Il2CppInterop to inject this class. You'll also need to
/// call <see cref="InjectClasses()"/> in your plugin's Load callback.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class InjectManagedClassAttribute : Attribute
{
	public static void InjectClasses()
	{
		foreach (Type type in Assembly.GetCallingAssembly().GetTypes()) {
			if (type.GetCustomAttributes(typeof(InjectManagedClassAttribute), true).Length > 0) {
				Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp(type);
			}
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
