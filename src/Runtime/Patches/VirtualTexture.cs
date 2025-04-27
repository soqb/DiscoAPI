using HarmonyLib;
using AmplifyTexture;
using DiscoAPI.Runtime.VirtualTextures;
using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace DiscoAPI.Runtime.Patches;

public class VirtualTexturePatches
{
	// [HarmonyPatch(typeof(PageSequencer), nameof(PageSequencer.ComputeIndex))]
	// [HarmonyPostfix]
	// private static void OnComputeIndex(int mip, int x, int y, int __result)
	// {
	// DiscoRunner.Log.LogDebug($"this is page {mip}#({x}, {y}) at index {__result}");
	// }

	[HarmonyPatch(typeof(PageFile2), nameof(PageFile2.ReadPage))]
	[HarmonyPrefix]
	private static bool PreReadPage2(
		PageFile2 __instance,
		int index,
		PageRequest pageReq,
		ref (System.Drawing.Rectangle, PageLocation, PageProvider?) __state
	)
	{
		if (!ModVirtualTexture.CustomizerKey.TryOf(__instance.m_asset, out var customizer))
		{
			return true;
		}

		PageLocation page = VirtualTextureCustomizer.InvertPageId(__instance.m_asset, index);
		if (!customizer.TrySubstitute(page, out var pages, out var overlap)) return true;

		__state = (overlap, page, pages);

		// if overlap is complete, don't bother with original decoding because it will all be replaced.
		return overlap.Width != 136 || overlap.Height != 136;
	}

	private delegate System.Drawing.Color GetPixel(PageLocation page, int x, int y);

	[HarmonyPatch(typeof(PageFile2), nameof(PageFile2.ReadPage))]
	[HarmonyPostfix]
	private static void PostReadPage2(
		PageFile2 __instance,
		int index,
		PageRequest pageReq,
		ref bool __result,
		(System.Drawing.Rectangle, PageLocation, PageProvider?) __state
	)
	{
		const int DebugBorderWidth = 4;
		(System.Drawing.Rectangle overlap, PageLocation page, PageProvider? pages) = __state;

		void SetPixel(Il2CppArrayBase<byte> array, int x, int y, System.Drawing.Color color)
		{
			array[0 + 4 * (y * 136 + x)] = color.R;
			array[1 + 4 * (y * 136 + x)] = color.G;
			array[2 + 4 * (y * 136 + x)] = color.B;
			array[3 + 4 * (y * 136 + x)] = color.A;
		}

		void FillOverlap()
		{
			DiscoRunner.Log.LogDebug($"overlap at {page.mip}#({page.x}, {page.y})");
			void FillWith(Il2CppArrayBase<byte> array, GetPixel getPixel)
			{
				for (int y = overlap.Y; y < overlap.Bottom; y++)
					for (int x = overlap.X; x < overlap.Right; x++)
						SetPixel(array, x + 4, y + 4, getPixel(page, x, y));
			}

			FillWith(pageReq.diff, pages.GetDiffusionPixel);
			FillWith(pageReq.norm, pages.GetNormalPixel);
			FillWith(pageReq.spec, pages.GetSpecularPixel);
		}

		void DebugFill()
		{
			System.Drawing.Color DebugColor(int x, int y)
			{
				int d = Math.Min(255, (x + y) * 16 / 17);
				return System.Drawing.Color.FromArgb(d, 255 - d, d);
			}

			for (int y = 0; y < DebugBorderWidth; y++)
				for (int x = 0; x < 136; x++) SetPixel(pageReq.diff, x, y, DebugColor(x, y));

			for (int y = 136 - DebugBorderWidth; y < 136; y++)
				for (int x = 0; x < 136; x++) SetPixel(pageReq.diff, x, y, DebugColor(x, y));

			for (int y = DebugBorderWidth; y < 136 - DebugBorderWidth; y++)
			{
				for (int x = 0; x < DebugBorderWidth; x++) SetPixel(pageReq.diff, x, y, DebugColor(x, y));
				for (int x = 136 - DebugBorderWidth; x < 136; x++) SetPixel(pageReq.diff, x, y, DebugColor(x, y));
			}

		}

		if (pages != null) FillOverlap();
		if (DiscoAPISettings.DrawVirtualTextureBorders) DebugFill();

		__result = true;
	}

	[HarmonyPatch(typeof(PageFile2), nameof(PageFile2.OpenRead))]
	[HarmonyPrefix]
	private static bool OnOpenRead2(PageFile2 __instance, ref bool __result)
	{
		if (CustomVirtualTextureManager.TryLoadTexture(__instance.m_asset) == VirtualTextureState.AdHoc)
		{
			// if we're an ad-hoc texture, we don't need any of the standard malarkey.
			__instance.m_stream = Il2CppSystem.IO.Stream.Null;
			__result = true;
			return false;
		}

		return true;
	}

	[HarmonyPatch(typeof(PageFile2), nameof(PageFile2.Close))]
	[HarmonyPrefix]
	private static void OnClose2(PageFile2 __instance)
	{
		CustomVirtualTextureManager.TryUnloadTexture(__instance.m_asset);
	}

	[HarmonyPatch(typeof(AmplifyTextureManager), nameof(AmplifyTextureManager.InitializeCollections))]
	[HarmonyPrefix]
	private static void OnInitializeTextureCollections(AmplifyTextureManager __instance)
	{
		foreach (var collection in CustomVirtualTextureManager.collections.Values)
		{
			__instance.VirtualTextureCollections.Add(collection);
		}
	}

	// NB: we would like this to be temporary but re-enabling VT cache compression
	// will require further digging into the VTC2 format, which i really don't want to do.
	[HarmonyPatch(typeof(AmplifyTextureCamera), "InternalInitialize")]
	[HarmonyPrefix]
	private static void OnCameraInternalInitialize(AmplifyTextureCamera __instance, EditorRuntimeProperties editorProps)
	{
		if (editorProps != null) editorProps.m_cacheCompression = false;
		else __instance.m_cacheCompression = false;

		DiscoRunner.Log.LogInfo("disabled virtual texture cache compression. performance will be impacted.");
	}
}
