using System;
using System.Collections.Generic;
using System.Threading;
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
	private delegate VirtualTextureCustomizer? GetCustomizer(VirtualTexture asset);
	private record struct VTEntry(VirtualTextureState state, GetCustomizer factory, VirtualTextureCustomizer? customizer);

	private static ThreadLocal<Action<VirtualTexture>?> vtInitializer = new();
	private static Dictionary<string, VTEntry> textureRegistry = new();
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

	public static void FinishTextureInitialization(VirtualTexture __instance)
	{
		var runInit = vtInitializer.Value;
		if (runInit == null) return;
		vtInitializer.Value = null;

		runInit(__instance);
	}

	private static VirtualTextureCustomizer CreateCustomizer(VirtualTexture asset, AdHocTextureConfig config)
	{
		VirtualTextureCollection collection = ScriptableObject.CreateInstance<VirtualTextureCollection>();
		collection.UniqueName = $"alfresco/{asset.m_hashName}";
		collection.VirtualTextures = new();
		collection.VirtualTextures.Add(asset);
		collection.m_pageTablePacker = new();
		collections.Add(asset.m_hashName, collection);
		collection.hideFlags |= HideFlags.DontUnloadUnusedAsset;

		return new AdHocVirtualTextureCustomizer(asset, config);
	}

	// private static string HashName()
	// {
	// 	byte[] buff = Guid.NewGuid().ToByteArray();
	// 	byte[] hash = MD5.Create().ComputeHash(buff);
	// 	return string.Join("", hash.Select(ch => ch.ToString("x2")));
	// }

	public static string RegisterAdHoc(AdHocTextureConfig config)
	{
		string hashName = "";

		vtInitializer.Value = (asset) =>
		{
			// NB: We do this here because creating the VT in turn calls PageFile2.OpenRead which in turn calls
			//     FinishTextureInitialization. We don't have another route since CreateInstance needs the page file set up.

			asset.m_virtualSize = VirtualSize._2K_x_2K;
			asset.m_mipFilter = MipFilter.Nearest;
			asset.m_layoutPreset = LayoutPreset.Unity_Standard;
			asset.m_signature = new byte[16];
			asset.m_version = new VersionInfo(2, 2, 4);
			asset.m_assetIndex = 42;
			asset.m_layoutSettings = layoutSettings;
			asset.m_pageFile = new(asset);
			asset.UpdateProperties();
			asset.Initialize();
			asset.name = $"adhoc/{asset.m_hashName}";
			asset.hideFlags |= HideFlags.DontUnloadUnusedAsset;

			hashName = asset.HashName;

			textureRegistry.Add(
				hashName,
				new(VirtualTextureState.AdHoc, asset => CreateCustomizer(asset, config), null)
			);
		};

		VirtualTexture asset = ScriptableObject.CreateInstance<VirtualTexture>();

		return hashName;
	}

	public static void RegisterOverrides(string hashName, VirtualTextureOverrides overrides) => textureRegistry.Add(
		hashName,
		new(VirtualTextureState.Overriden, asset => new OverridesVirtualTextureCustomizer(asset, overrides), null)
	);

	public static VirtualTextureState TryLoadTexture(VirtualTexture asset)
	{
		CustomVirtualTextureManager.FinishTextureInitialization(asset);

		if (!textureRegistry.TryGetValue(asset.m_hashName, out var entry)) return VirtualTextureState.Vanilla;
		if (entry.customizer != null) throw new InvalidOperationException("double virtual texture load");

		textureRegistry[asset.HashName] = new(entry.state, entry.factory, entry.factory!(asset));
		return entry.state;
	}

	public static void TryUnloadTexture(VirtualTexture asset)
	{
		if (!textureRegistry.TryGetValue(asset.m_hashName, out var entry)) return;
		if (entry.customizer != null) textureRegistry[asset.HashName] = new(entry.state, entry.factory, null);
	}

	public static bool TryLookup(VirtualTexture asset, out VirtualTextureCustomizer? customizer)
	{
		if (textureRegistry.TryGetValue(asset.m_hashName, out var entry))
		{
			customizer = entry.customizer;
			return true;
		}
		else
		{
			customizer = null;
			return false;
		}
	}
}

