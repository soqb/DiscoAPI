using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AmplifyTexture;

namespace DiscoAPI.Runtime.VirtualTextures;

public class VirtualTextureConfig
{

}

public static class CustomVirtualTextureManager
{
	private record struct VTEntry(VirtualTextureOverrides settings, VirtualTextureCustomizer? texture);

	private static Dictionary<string, VTEntry> map = new();

	public static string Register(VirtualTextureConfig config)
	{
		VirtualTexture asset = new()
		{
			m_virtualSize = VirtualSize._2K_x_2K,
			m_mipFilter = MipFilter.Nearest,
			m_layoutPreset = LayoutPreset.Unity_Standard,
			m_signature = new byte[16],
			m_version = new VersionInfo(2, 2, 4),
			m_assetIndex = 42,
		};

		asset.UpdateProperties();
		asset.Initialize();

		map.Add(asset.m_hashName, new());

		return asset.m_hashName;
	}

	public static void RegisterOverrides(string hashName, VirtualTextureOverrides o)
	{
		map.Add(hashName, new(o, null));
	}

	public static bool TryLookup(VirtualTexture asset, [NotNullWhen(true)] out VirtualTextureCustomizer? texture)
	{
		if (map.TryGetValue(asset.m_hashName, out var entry))
		{
			if (entry.texture == null)
			{
				VirtualTextureCustomizer vt = new(asset, entry.settings);
				map[asset.m_hashName] = new(entry.settings, vt);
				texture = vt;
			}
			else
			{
				texture = entry.texture;
			}
			return true;
		}
		texture = null;
		return false;
	}
}

