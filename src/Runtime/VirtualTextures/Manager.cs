using System;
using System.Collections.Generic;
using System.IO;
using AmplifyTexture;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Components;
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
	public const int VANILLA_TEXTURE_COUNT = 41;
	private static int AdHocsAdded = 0;
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
		asset.m_virtualSize = (VirtualSize)(config.widthInPages * 128);
		asset.m_mipCount = (int)config.mipCount;
		asset.m_mipFilter = MipFilter.Nearest;
		asset.m_layoutPreset = LayoutPreset.Unity_Standard;
		asset.m_assetIndex = VANILLA_TEXTURE_COUNT + ++AdHocsAdded;
		asset.m_layoutSettings = layoutSettings;

		// NB: both of these properties are empty and return false from `IsOpen`
		//     but AT only checks PageFile2 for notnull before starting a read,
		//     so we can hook there for adhoc.
		asset.m_pageFile = new(asset);
		asset.m_pageFile2 = new(asset);

		asset.UpdateProperties();
		asset.Initialize();
		asset.name = $"adhoc/{asset.m_hashName}";
		asset.hideFlags |= HideFlags.DontUnloadUnusedAsset;

		asset.m_packer.Pack(asset.m_collection);
	}

	public static void RegisterOverrides(string hashName, VirtualTextureOverrides overrides)
	{
		overriden.Add(hashName, overrides);
	}

	public static VirtualTextureState TryLoadTexture(VirtualTexture asset)
	{
		var texture = VirtualTextureComponents.Of(asset);
		bool loaded = texture.Contains(VirtualTextureComponents.Customizer);
		if (!overriden.TryGetValue(asset.m_hashName, out var overrides))
		{
			return loaded ? VirtualTextureState.AdHoc : VirtualTextureState.Vanilla;
		}

		if (loaded) throw new InvalidOperationException("double virtual texture load");

		texture.Add(VirtualTextureComponents.Customizer, new OverridesVirtualTextureCustomizer(asset, overrides));
		return VirtualTextureState.Overriden;
	}

	public static void TryUnloadTexture(VirtualTexture asset)
	{
		if (!VirtualTextureComponents.Customizer.TryOf(asset, out var czar)) return;

		czar.Dispose();
	}

	public static ModEntity<VirtualTexture> VtFromArea(Area area)
	{
		string name = area.scenePath;
		if (!collections.TryGetValue(name, out var col))
		{
			col = InternNewCollection(name);
			string path = DiscoRunner.GetSource(area.source!)!.Location.Get(area.vtPath!)!;
			var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			var dcavt = new VirtualTextures.DCAVTFile(file);
			var vt = VirtualTextures.VirtualTextureComponents.CreateAdHoc(dcavt.AdHocConfig());
			col.VirtualTextures.Add(vt.EntityBase);
			AmplifyTextureManager.m_instance.InitializeCollections();
			return vt;
		}

		return VirtualTextureComponents.Of(col.VirtualTextures[0]);
	}

	public static void Reset()
	{
		foreach (var cam in AmplifyTextureManager.m_runtimeList)
		{
			if (!cam.IsInitialized) continue;
			cam.InternalReset();
		}
		AmplifyTextureManager.Instance.ResetGlobalShaderParams();
	}

	public static void UpdateCacheCompression(bool compress)
	{
		DiscoRunner.Log.LogWarning($"cc = {compress}");
		foreach (var cam in AmplifyTextureManager.m_runtimeList)
		{
			cam.UpdateCacheCompression(compress);
		}

		if (!compress)
			DiscoRunner.Log.LogWarning("disabled virtual texture cache compression. performance will be impacted.");
	}
}
