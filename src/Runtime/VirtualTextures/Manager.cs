using System;
using System.Collections.Generic;
using AmplifyTexture;
using UnityEngine;
using CompressionType = AmplifyTexture.CompressionType;

namespace DiscoAPI.Runtime.VirtualTextures;

public enum VirtualTextureState
{
	Vanilla,
	Overriden,
	AdHoc,
}

public static class CustomVirtualTextureManager
{
	private static Dictionary<string, VirtualTextureOverrides> overriden = new();
	public static Dictionary<string, VirtualTextureCollection> collections = new();

	private static LayoutSettings layoutSettings = new()
	{
		propDiffuse = "_C",
		propNormal = "_N",
		propOcclusion = "_O",
		propDisplacement = "_H",
		propSpecular = "_S",
		rgbCompType = CompressionType.Lossy,
		alphaCompType = CompressionType.Lossy,
		normalCompType = CompressionType.Lossy,
		occlusionCompType = CompressionType.Lossy,
		displacementCompType = CompressionType.Lossless,
		specularCompType = CompressionType.Lossless,
		rgbQuality = 88,
		alphaQuality = 85,
		normalQuality = 90,
		occlusionQuality = 85,
		displacementQuality = 85,
		specularQuality = 85,
		useAlpha = true,
		useNormal = true,
		useOcclusion = true,
		useDisplacement = true,
		useSpecular = true,
		defaultDiffuseValue = new(1f, 1f, 1f, 1f),
		defaultNormalValue = new(0.5f, 0.5f, 0.5f, 0.5f),
		defaultOcclusionValue = new(1f, 1f, 1f, 1f),
		defaultDisplacementValue = new(0.5f, 0.5f, 0.5f, 1f),
		defaultSpecularValue = new(1f, 1f, 1f, 1f),
	};

	public static VirtualTextureCollection InternNewCollection(string name)
	{
		VirtualTextureCollection collection = ScriptableObject.CreateInstance<VirtualTextureCollection>();
		collection.UniqueName = name;
		collection.name = name;
		collection.VirtualTextures = new();
		collection.m_pageTablePacker = new();
		collection.hideFlags |= HideFlags.DontUnloadUnusedAsset;
		collections.Add(name, collection);

		return collection;

	}

	internal static void InitializeAdHoc(VirtualTexture asset, AdHocTextureConfig config)
	{
		asset.m_virtualSize = VirtualSize._2K_x_2K;
		asset.m_mipFilter = MipFilter.Nearest;
		asset.m_layoutPreset = LayoutPreset.Unity_Standard;
		asset.m_assetIndex = 42;
		asset.m_layoutSettings = layoutSettings;
		asset.m_pageFile = new(asset);
		asset.UpdateProperties();
		asset.Initialize();
		asset.name = $"adhoc/{asset.m_hashName}";
		asset.hideFlags |= HideFlags.DontUnloadUnusedAsset;
	}

	public static void RegisterOverrides(string hashName, VirtualTextureOverrides overrides)
	{
		overriden.Add(hashName, overrides);
	}

	public static VirtualTextureState TryLoadTexture(VirtualTexture asset)
	{
		var texture = VirtualTextureComponents.Of(asset);
		if (!overriden.TryGetValue(asset.m_hashName, out var overrides))
		{
			return texture.Contains(VirtualTextureComponents.Customizer)
				? VirtualTextureState.AdHoc
				: VirtualTextureState.Vanilla;
		}

		if (texture.Contains(VirtualTextureComponents.Customizer)) throw new InvalidOperationException("double virtual texture load");

		texture.Add(VirtualTextureComponents.Customizer, new OverridesVirtualTextureCustomizer(asset, overrides));
		return VirtualTextureState.Overriden;
	}

	public static void TryUnloadTexture(VirtualTexture asset)
	{
		// nothing to be done...
	}

	public static void RebuildTextures()
	{
		if (AmplifyTextureManager.m_instance?.m_currentVirtualTextures == null) return;
		foreach (var tx in AmplifyTextureManager.m_instance.m_currentVirtualTextures) tx.RequestRebuild();
	}
}
