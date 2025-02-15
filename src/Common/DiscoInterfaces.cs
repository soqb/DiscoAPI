using System;
using System.Collections.Generic;
using DiscoAPI.Common.Assets;

namespace DiscoAPI.Common;

public interface IDiscoSource
{
    bool IsVanilla { get; }
    string Guid { get; }
    string? DisplayName { get; }
    string? Description { get; }
    List<string>? Authors { get; }
    IDiscoManager Manager { get; }

    IAssetTable GetAssetsForType(Type type);
}

public interface IMutableAssets : IDiscoSource
{
    void Add(Asset asset);
}

public interface IDiscoManager
{
    IAssetManager Assets { get; }
    IDiscoSource? GetSource(string key);
}
