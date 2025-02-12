using System;
using System.Collections.Generic;
using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Runtime.Dialogue;
using System.Collections;
using System.Linq;
using PC = PixelCrushers.DialogueSystem;

namespace DiscoAPI.Runtime.Assets;

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

	bool HasVanillaAssets { get; }
	Type AssetType { get; }

	void Alloc(Asset asset);
	Asset? this[int id] { get; }
	int Count { get; }

	IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
}

public interface IAssetArena<T> : IAssetArena, IEnumerable<T> where T : Asset
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
		object IEnumerator.Current => Current;

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
	void IAssetArena.Alloc(Asset asset) => Alloc((T)asset);
	Asset? IAssetArena.this[int id] => this[id];

	IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);
}


public class PCArena<T, U> : IAssetArena<U> where U : Asset where T : PC.Asset, new()
{
	// FIXME: the treatment of "raw" assets here is pretty sloppy. consider a dedicated collection.
	private class RawEnumerator : IEnumerator<T>
	{
		public int idx;
		public PCArena<T, U> arena;

		public RawEnumerator(PCArena<T, U> table)
		{
			this.arena = table;
			Reset();
		}

		public T Current => arena.GetRaw(idx)!;
		object IEnumerator.Current => Current;

		public bool MoveNext()
		{
			return ++idx < arena.Count;
		}
		public void Reset() => idx = -1;
		public void Dispose() { }
	}

	private readonly record struct RawEnumerable(PCArena<T, U> arena) : IEnumerable<T>
	{
		public IEnumerator<T> GetEnumerator() => new RawEnumerator(arena);
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	public IEnumerable<T> Raw => new RawEnumerable(this);

	private bool WasBundleLoaded => DiscoRunner.manager.WasBundleLoaded;
	private DialogueManager Dialogue => DiscoRunner.manager.Dialogue;
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

	private void Init()
	{
		// ensure the value is created before we dequeue.
		var l = pcList.Value;

		// PC ids are never zero.
		baseMaxOffset = (l.Count > 0 ? l[l.Count - 1].id : 0) - l.Count + 1;
		foreach (var asset in assetsToProcessWhenReady) Alloc(asset);
	}

	private Queue<U> assetsToProcessWhenReady = new();

	public PixelsToDisco uncrusher => Dialogue.Parent.Dialogue.uncrusher;
	public DiscoToPixels crusher => Dialogue.Parent.Dialogue.crusher;

	private int baseMaxOffset = 0;

	public PCArena(Func<DialogueManager, Il2CppSystem.Collections.Generic.List<T>> listSupplicant)
	{
		this.pcList = new(() => listSupplicant(Dialogue));

		DiscoRunner.preDialogueLoad.Add(Init);
	}

	public bool HasVanillaAssets => true;

	public void Alloc(U asset)
	{
		if (!WasBundleLoaded)
		{
			assetsToProcessWhenReady.Enqueue(asset);
			return;
		}

		Intern(asset, (src, id) => crusher.Crush(src, asset, id));
	}

	public void Intern(Asset origin, Func<DiscoSource, int, PC.Asset> crush)
	{
		var src = Dialogue.Parent[origin.source!]!;
		int plannedId = baseMaxOffset + Count;
		PC.Asset pcAsset = crush(src, plannedId);
		pcAsset.id = plannedId;

		bool isConversation = typeof(U) == typeof(Conversation);

		string articyId = DiscoToPixels.BuildArticyId(origin);
		pcAsset.fields.Add(new PC.Field(ArticyBridge.ARTICY_ID_FIELD, articyId, PC.FieldType.Text));
		Dialogue.fakeArticyIDToAssetCache.Add(articyId, pcAsset);

		if (isConversation)
		{
			Dialogue.pcDatabase.AddConversation((PC.Conversation)pcAsset);
		}
		else
		{
			list.Add((T)pcAsset);
		}
	}

	public T? GetRaw(int id) => id >= 0 && id < Count ? list[id] : null;

	public U? this[int id] => id >= 0 && id < Count ? (U)uncrusher.Uncrush(list[id]) : null;

	public int Count => list.Count;

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

	public bool HasVanillaAssets => true;

	public void Alloc(U asset)
	{
		int idx = entries.Count - baseCount + baseMax + 1;
		entries.Add(new(Enum.Parse<T>(idx.ToString()), asset));
	}
}

// FIXME: all a bit hacky tbh.
public class PCProxyArena<T, T2, U> : IAssetArena<T> where T : Asset where U : Asset where T2 : PC.Asset, new()
{
	private bool WasBundleLoaded => DiscoRunner.manager.WasBundleLoaded;
	private PCArena<T2, U> inner;
	private Func<T2, bool> isValid;

	// Forced to use a list because indices are sparsely scattered.
	private List<int> rawIndices = new();
	private Queue<T> allocWhenReady = new();

	public PCProxyArena(PCArena<T2, U> inner, Func<T2, bool> isValid)
	{
		this.inner = inner;
		this.isValid = isValid;

		DiscoRunner.preDialogueLoad.Add(Init);
	}

	public void Init()
	{
		rawIndices = inner.Raw.Where(isValid).Select((_, idx) => idx).ToList();
		foreach (T ass in allocWhenReady) Alloc(ass);
	}

	public T? this[int id] => null;
	// one day soon:
	// id < rawIndices.Count && id >= 0 ? inner.list[rawIndices[id]] : null;

	public bool HasVanillaAssets => throw new NotImplementedException();

	public int Count => rawIndices.Count;

	public void Alloc(T asset)
	{
		if (!WasBundleLoaded)
		{
			allocWhenReady.Enqueue(asset);
			return;
		}

		rawIndices.Add(inner.list.Count);
		inner.Intern(asset, (src, id) => inner.crusher.Crush(src, asset, id));
	}
}
