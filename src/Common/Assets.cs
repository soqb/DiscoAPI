using System.Collections.Generic;
using DiscoAPI.Common.Dialogue;

namespace DiscoAPI.Common.Assets;

public interface IAssetArena<T> where T : Asset
{
	public void Insert(T asset);
	public T this[int id] { get; }
	public int MaxId { get; }
	public int Count { get; }
}

public interface IAssetManager
{
	public IDiscoManager Parent { get; }

	public int ResolveId(AssetLocation ass);
	public Asset Resolve(AssetLocation ass);

	public IAssetArena<Actor> actors { get; }
	public IAssetArena<Conversation> conversations { get; }
	public IAssetArena<Variable> variables { get; }
	public IAssetArena<Skill> skills { get; }
}

public interface IAssetSource
{
	public IDiscoSource Parent { get; }
	public string Guid => Parent.Guid;
	public IAssetManager Manager => Parent.Manager.Assets;

	public void Add(Asset asset);

	public IAssetTable<Actor> actors { get; }
	public IAssetTable<Conversation> conversations { get; }
	public IAssetTable<Variable> variables { get; }
	public IAssetTable<Skill> skills { get; }
}

public interface IAssetTable<T> : IEnumerable<T>, LocalIdResolver<int> where T : Asset
{
	public void Insert(T asset);
}
