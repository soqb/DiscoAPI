using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DiscoAPI.Common;
using DiscoAPI.Common.Assets;

namespace DiscoAPI.Runtime.Assets;

public interface IArenaTable : IAssetTable
{
	int idOffset { get; }
}

internal abstract class AssetTable<T> : IArenaTable, LocalIdResolver<int> where T : Asset
{
	protected readonly record struct IdInfo(int index, int contention);

	private class TableEnumerator : IEnumerator<Asset>
	{
		public int idx;
		public AssetTable<T> table;

		public TableEnumerator(AssetTable<T> table)
		{
			this.table = table;
			Reset();
		}

		public Asset Current => table[idx]!;
		object IEnumerator.Current => this.Current;

		public bool MoveNext()
		{
			return ++idx < table.count;
		}
		public void Reset() => idx = -1;
		public void Dispose() { }
	}

	protected Dictionary<string, IdInfo> ids = new();
	public IAssetArena<T> arena;
	public DiscoSource parent;
	public int count;

	protected AssetTable(DiscoSource parent, IAssetArena<T> arena)
	{
		this.arena = arena;
		this.parent = parent;
	}

	Type IAssetTable.AssetType => typeof(T);

	public void Add(Asset asset) => Insert((T)asset);

	protected void AddId(string id, int index)
	{
		if (ids.TryGetValue(id, out var info)) ids[id] = new(info.index, info.contention + 1);
		else ids.Add(id, new IdInfo(index, 1));
	}

	public abstract void Insert(T asset);
	public abstract int idOffset { get; }

	public T? this[int resolved] => arena[resolved + idOffset];

	int LocalIdResolver<int>.Resolve(int id) => id;
	int LocalIdResolver<int>.Resolve(string id)
	{
		if (ids.TryGetValue(id, out var info))
		{
			if (info.contention > 1)
				parent.Log.LogWarning($"asset location '{parent.Guid}:{id}' for type '{typeof(T)}' is ambiguous between {info.contention} assets. the runtime choice of asset is arbitrary!");
			return info.index;
		}

		return -1;
	}

	IEnumerator IEnumerable.GetEnumerator() => new TableEnumerator(this);
	IEnumerator<Asset> IEnumerable<Asset>.GetEnumerator() => new TableEnumerator(this);
}

class VanillaTable<T> : AssetTable<T> where T : Asset
{
	public VanillaTable(DiscoSource parent, IAssetArena<T> arena)
		: base(parent, arena)
	{
		count = arena.Count;

		for (int i = 0; i < count; i++) AddId(this[i]!.id, i);
	}

	public override void Insert(T asset) => throw new NotSupportedException("cannot add assets to the `disco` source");
	public override int idOffset => 0;
}

class ModTable<T> : AssetTable<T> where T : Asset
{
	public override int idOffset { get; }

	public override void Insert(T asset)
	{
		asset.source = parent.Guid;

		AddId(asset.id, count);

		arena.Alloc(asset);

		count += 1;
	}

	public ModTable(DiscoSource parent, IAssetArena<T> arena) : base(parent, arena)
	{
		idOffset = arena.Count;
	}
}

public record AssetTypeEnumerator(Type? type) : IEnumerable<Type>, IEnumerator<Type>
{
	private Type? current = type;

	public Type Current => type!;
	object IEnumerator.Current => Current;

	public bool MoveNext()
	{
		if (current != null) current = current.BaseType;
		return current != null;
	}

	public void Dispose() { }
	public void Reset() => current = type;

	public IEnumerator<Type> GetEnumerator() => new AssetTypeEnumerator(current);
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class AssetManager : IAssetManager
{
	public DiscoManager Parent { get; }
	IDiscoManager IAssetManager.Parent => Parent;

	public AssetManager(DiscoManager parent)
	{
		Parent = parent;
	}

	private record ArenaStorage(
		IAssetArena arena,
		Func<DiscoSource, IArenaTable> vanilla,
		Func<DiscoSource, IArenaTable> modded,
		bool hasVanillaAssets
	);

	private Dictionary<Type, ArenaStorage> arenas = new();

	private ArenaStorage GetStorageForType(Type? type)
	{
		foreach (Type ty in new AssetTypeEnumerator(type))
			if (arenas.TryGetValue(ty, out var arena))
				return arena;

		throw new InvalidOperationException($"no asset arena was registered for the type '{type}'");
	}

	public IAssetArena GetArenaForType(Type type) => GetStorageForType(type).arena;
	public IAssetArena<T> GetArena<T>() where T : Asset => (IAssetArena<T>)GetStorageForType(typeof(T)).arena;

	public IArenaTable CreateTableForType(Type type, DiscoSource source)
	{
		ArenaStorage arena = GetStorageForType(type);
		if (source.IsVanilla) return arena.vanilla(source);
		else return arena.modded(source);
	}

	public IEnumerable<IArenaTable> GetDefaultVanillaTables(DiscoSource source) => arenas
		.Where(kv => kv.Value.hasVanillaAssets)
		.Select(kv => kv.Value.vanilla(source));

	public int ResolveId(AssetLocation ass)
	{
		var table = Parent[ass.source]?.GetAssetsForType(ass.type.type);
		if (table == null) return -1;
		return ass.id.ResolveWith(table) + table.idOffset;
	}

	public Asset? Resolve(AssetLocation ass) => (Asset?)GetStorageForType(ass.type.type)?.arena[ResolveId(ass)];

	public void Add(Asset asset) => Parent[asset.source!]?.Add(asset);

	public void Register<T>(IAssetArena<T> arena, bool hasVanillaAssets) where T : Asset
	{
		// NB: We conservatively use `arena.AssetType` instead of `typeof(T)`
		// because the former may be a subtype of the latter.
		DiscoRunner.Log.LogInfo($"registered asset arena '{arena.GetType()}' for '{arena.AssetType}' assets.");
		arenas.Add(
			arena.AssetType,
			new(
				arena,
				(source) => new VanillaTable<T>(source, arena),
				(source) => new ModTable<T>(source, arena),
				hasVanillaAssets
			)
		);
	}
}
