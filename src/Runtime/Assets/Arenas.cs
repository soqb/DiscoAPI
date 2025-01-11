using System;
using System.Collections.Generic;
using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Runtime.Dialogue;
using PC = PixelCrushers.DialogueSystem;

namespace DiscoAPI.Runtime.Assets;

public interface IAssetArena
{
	bool HasVanillaAssets { get; }
	Type AssetType { get; }

	void Alloc(Asset asset);
	Asset? this[int id] { get; }
	int Count { get; }
}

public interface IAssetArena<T> : IAssetArena where T : Asset
{
	void Alloc(T asset);
	new T? this[int id] { get; }

	Type IAssetArena.AssetType => typeof(T);
	void IAssetArena.Alloc(Asset asset) => Alloc((T)asset);
	Asset? IAssetArena.this[int id] => this[id];
}


public class PCArena<T, U> : IAssetArena<U> where U : Asset where T : PC.Asset, new()
{
	private bool WasBundleLoaded => DiscoRunner.manager.WasBundleLoaded;
	private DialogueManager Dialogue => DiscoRunner.manager.Dialogue;
	private Lazy<Il2CppSystem.Collections.Generic.List<T>> pcList;
	private Il2CppSystem.Collections.Generic.List<T> list
	{
		get
		{
			if (!WasBundleLoaded) return new();

			if (!pcList.IsValueCreated) DequeueAssets();
			return pcList.Value;
		}
	}

	private void DequeueAssets()
	{
		// ensure the value is created before we dequeue.
		var l = pcList.Value;

		// PC ids are never zero.
		baseMaxOffset = l.Count > 0 ? l[l.Count - 1].id : 0;
		baseMaxOffset -= l.Count;
		foreach (var asset in assetsToProcessWhenReady) Alloc(asset);
	}

	private Queue<U> assetsToProcessWhenReady = new();

	private PixelsToDisco uncrusher => Dialogue.Parent.Dialogue.uncrusher;
	private DiscoToPixels crusher => Dialogue.Parent.Dialogue.crusher;

	private int baseMaxOffset = 0;

	public PCArena(Func<DialogueManager, Il2CppSystem.Collections.Generic.List<T>> listSupplicant)
	{
		this.pcList = new(() => listSupplicant(Dialogue));
	}

	public bool HasVanillaAssets => true;

	public void Alloc(U asset)
	{
		if (!WasBundleLoaded)
		{
			assetsToProcessWhenReady.Enqueue(asset);
			return;
		}

		var src = Dialogue.Parent[asset.source!]!;
		int plannedId = baseMaxOffset + Count;
		var pcAsset = crusher.Crush(src, asset, plannedId);
		pcAsset.id = plannedId;

		string assetKind;
		bool isConversation;
		{
			Type t = typeof(U);
			assetKind = t == typeof(Actor) ? "actors"
				: t == typeof(Conversation) ? "conversations"
				: t == typeof(Variable) ? "variables"
				: throw new NotSupportedException();
			isConversation = t == typeof(Conversation);
		}

		string fakeArticyID = $"{asset.source}.${assetKind}.{asset.id}";
		pcAsset.fields.Add(new PC.Field(ArticyBridge.ARTICY_ID_FIELD, fakeArticyID, PC.FieldType.Text));
		Dialogue.fakeArticyIDToAssetCache.Add(fakeArticyID, pcAsset);

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
	public int MaxId => list.Count > 0 ? pcList.Value[Count - 1].id : -1;

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


