using System;
using System.Collections.Generic;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Dialogue;
using System.Collections;
using System.Linq;
using PC = PixelCrushers.DialogueSystem;
using Il2CppInterop.Runtime;

namespace DiscoAPI.Runtime.Assets;

/// <summary>
/// Stores and allocates assets.
/// <para>
/// By "allocation", we refer to the process of interning and perhaps marshalling data
/// used in communication with the base game.
/// If an asset needs any kind of translation or postprocessing to interface with the base game,
/// chances are, it will happen here.
/// </para>
/// <para>
/// Every asset type has exactly one arena.
/// </para>
/// </summary>
public interface IAssetArena : IEnumerable
{
	private class Enumerator : IEnumerator
	{
		public int idx;
		public IAssetArena arena;

		public Enumerator(IAssetArena table)
		{
			this.arena = table;
			Reset();
		}

		public object Current => arena[idx]!;
		object IEnumerator.Current => this.Current;

		public bool MoveNext()
		{
			return ++idx < arena.Count;
		}
		public void Reset() => idx = -1;
		public void Dispose() { }
	}

	Type AssetType { get; }

	void Alloc(object asset);
	object? this[int id] { get; }
	int Count { get; }

	IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
}

/// <summary>
/// Stores and allocates assets. <see cref="IAssetArena"/>
/// </summary>
public interface IAssetArena<T> : IAssetArena, IEnumerable<T>
{
	private class Enumerator : IEnumerator<T>
	{
		public int idx;
		public IAssetArena<T> arena;

		public Enumerator(IAssetArena<T> table)
		{
			this.arena = table;
			Reset();
		}

		public T Current => arena[idx]!;
		object IEnumerator.Current => Current!;

		public bool MoveNext()
		{
			return ++idx < arena.Count;
		}
		public void Reset() => idx = -1;
		public void Dispose() { }
	}

	void Alloc(T asset);
	new T? this[int id] { get; }

	Type IAssetArena.AssetType => typeof(T);
	void IAssetArena.Alloc(object asset) => Alloc((T)asset);
	object? IAssetArena.this[int id] => this[id];

	IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);
}

public interface IRawArena<T>
{
	IAssetArena<T> Raw { get; }
}

public class GenericArena<T> : IAssetArena<T>
{
	public readonly List<T> Items = new();
	public int Count => Items.Count;
	public void Alloc(T asset)
	{
		Items.Add(asset);
	}

	public T? this[int id] => Items[id];
}

public class PCRawArena<T> : IAssetArena<T> where T : PC.Asset, new()
{
	private DialogueManager Dialogue => DiscoRunner.manager.Dialogue;
	private bool WasBundleLoaded => DiscoRunner.manager.WasBundleLoaded;
	private Lazy<Il2CppSystem.Collections.Generic.List<T>> pcList;
	public Il2CppSystem.Collections.Generic.List<T> list
	{
		get
		{
			if (!WasBundleLoaded) return new();

			if (!pcList.IsValueCreated) Init();
			return pcList.Value;
		}
	}

	public PCRawArena(Func<DialogueManager, Il2CppSystem.Collections.Generic.List<T>> listSupplicant)
	{
		this.pcList = new(() => listSupplicant(Dialogue));
	}

	public void Init()
	{
		// ensure the value is created before we dequeue.
		var l = pcList.Value;

		// PC ids are never zero.
		baseMaxOffset = (l.Count > 0 ? l[l.Count - 1].id : 0) - l.Count + 1;
	}

	public T? this[int id] => id >= 0 && id < Count ? list[id] : null;

	private int baseMaxOffset = 0;
	public int Count => list.Count;

	public int PlannedNextId => baseMaxOffset + Count;

	public void Alloc(T pcAsset)
	{
		if (!WasBundleLoaded) throw new Exception("raw arena cannot queue assets");

		if (typeof(T) == typeof(PC.Conversation))
		{
			Dialogue.pcDatabase.AddConversation((PC.Conversation)(PC.Asset)pcAsset);
		}
		else
		{
			list.Add((T)pcAsset);
		}
	}

	public T Preprocess(Asset origin, Func<DiscoSource, int, T> crush)
	{
		var src = Dialogue.Parent[origin.source!]!;
		int plannedId = baseMaxOffset + Count;
		T pcAsset = crush(src, plannedId);
		pcAsset.id = plannedId;

		string articyId = DiscoToPixels.BuildArticyId(origin);
		pcAsset.fields.Add(new PC.Field(ArticyBridge.ARTICY_ID_FIELD, articyId, PC.FieldType.Text));
		Dialogue.fakeArticyIDToAssetCache.Add(articyId, pcAsset);
		return pcAsset;
	}
}

public class PCArena<T, U> : IAssetArena<U>, IRawArena<T> where U : Asset where T : PC.Asset, new()
{
	public PCRawArena<T> Raw { get; }
	IAssetArena<T> IRawArena<T>.Raw => Raw;

	public PCArena(Func<DialogueManager, Il2CppSystem.Collections.Generic.List<T>> listSupplicant)
	{
		Raw = new PCRawArena<T>(listSupplicant);

		DiscoRunner.preDialogueLoad.Add(Init);
	}

	public void Init()
	{
		Raw.Init();
		foreach (U ass in allocWhenReady) Alloc(ass);
	}

	public PixelsToDisco uncrusher => DiscoRunner.manager.Dialogue.uncrusher;
	public DiscoToPixels crusher => DiscoRunner.manager.Dialogue.crusher;

	private Queue<U> allocWhenReady = new();

	private bool WasBundleLoaded => DiscoRunner.manager.WasBundleLoaded;
	private DialogueManager Dialogue => DiscoRunner.manager.Dialogue;
	public int Count => Raw.Count;

	public void Alloc(U asset)
	{
		if (!WasBundleLoaded)
		{
			allocWhenReady.Enqueue(asset);
			return;
		}

		Raw.Alloc(Raw.Preprocess(asset, (src, id) => (T)crusher.Crush(src, asset, id)));
	}

	public U? this[int id]
	{
		get
		{
			var raw = Raw[id];
			return raw == null ? null : (U)uncrusher.Uncrush(raw);
		}
	}
}

// FIXME: all a bit hacky tbh.
public class PCProxyArena<T, T2> : IAssetArena<T>, IRawArena<T2> where T : Asset where T2 : PC.Asset, new()
{
	private bool WasBundleLoaded => DiscoRunner.manager.WasBundleLoaded;
	private Func<T2, bool> isValid;
	public PCRawArena<T2> Raw { get; }
	IAssetArena<T2> IRawArena<T2>.Raw => Raw;

	// Forced to use a list because indices are sparsely scattered.
	private List<int> rawIndices = new();
	private Queue<T> allocWhenReady = new();

	public PCProxyArena(PCRawArena<T2> inner, Func<T2, bool> isValid)
	{
		Raw = inner;
		this.isValid = isValid;

		DiscoRunner.preDialogueLoad.Add(Init);
	}

	public void Init()
	{
		rawIndices = Raw.Where(isValid).Select((_, idx) => idx).ToList();
		foreach (T ass in allocWhenReady) Alloc(ass);
	}

	public T? this[int id] => null;
	// one day soon:
	// id < rawIndices.Count && id >= 0 ? inner.list[rawIndices[id]] : null;

	public int Count => rawIndices.Count;
	public PixelsToDisco uncrusher => DiscoRunner.manager.Dialogue.uncrusher;
	public DiscoToPixels crusher => DiscoRunner.manager.Dialogue.crusher;

	public void Alloc(T asset)
	{
		if (!WasBundleLoaded)
		{
			allocWhenReady.Enqueue(asset);
			return;
		}

		Raw.Alloc(Raw.Preprocess(asset, (src, id) => (T2)crusher.Crush(src, asset, id)));
	}
}

public static class EnumArena
{
	public static Dictionary<(Il2CppSystem.Type, long), string> GlobalEnumOverrideNames { get; } = new();
	public static Dictionary<(Il2CppSystem.Type, string), long> GlobalEnumOverrideValues { get; } = new();
	public static Dictionary<(Il2CppSystem.Type, string), long> GlobalEnumOverrideValuesLowercase { get; } = new();
}

public class EnumArena<T, U> : IAssetArena<U> where T : struct, Enum where U : Asset
{

	public readonly record struct Entry(T a, U b);

	public readonly int baseCount;
	public readonly int baseMax;
	public List<Entry> entries = new();
	public Recover recover;
	public Filter filter;

	public delegate U Recover(T value);
	public delegate bool Filter(T value);

	public EnumArena(Recover recover, Filter filter)
	{
		this.recover = recover;
		this.filter = filter;

		baseMax = int.MinValue;
		foreach (string name in Enum.GetNames(typeof(T)))
		{
			T old = Enum.Parse<T>(name);

			int n = Convert.ToInt32(old);
			baseMax = Math.Max(n, baseMax);

			if (!filter(old)) continue;
			entries.Add(new(old, DoRecover(old)));
		}

		baseCount = entries.Count;
	}

	private U DoRecover(T t)
	{
		U asset = recover(t);
		asset.source = "disco";
		return asset;
	}

	public int Count => entries.Count;

	public int ReverseId(T value) => entries.FindIndex(e => e.a.Equals(value));
	public T GetRaw(int id) => id >= 0 && id < Count ? entries[id].a : Enum.Parse<T>("-1");
	public U? this[int id] => id >= 0 && id < Count ? entries[id].b : null;

	public void Alloc(U asset)
	{
		int idx = entries.Count - baseCount + baseMax + 1;
		T inst = Enum.Parse<T>(idx.ToString());

		var type = Il2CppType.Of<T>();
		string name = asset.Location.ToString();
		long value = Convert.ToInt64(inst);
		EnumArena.GlobalEnumOverrideNames.Add((type, value), name);
		EnumArena.GlobalEnumOverrideValues.Add((type, name), value);
		EnumArena.GlobalEnumOverrideValuesLowercase.Add((type, name.ToLowerInvariant()), value);
		entries.Add(new(inst, asset));
	}
}
