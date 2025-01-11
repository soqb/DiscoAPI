using System;
using System.Collections.Generic;

namespace DiscoAPI.Common.Assets;

public interface IAssetManager
{
	IDiscoManager Parent { get; }

	Asset? Resolve(AssetLocation asset);

	void Add(Asset asset);
}

public interface IAssetTable : IEnumerable<Asset>, LocalIdResolver<int>
{
	Type AssetType { get; }
	void Add(Asset asset);
}
