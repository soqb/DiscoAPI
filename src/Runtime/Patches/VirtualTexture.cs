using HarmonyLib;
using AmplifyTexture;
using DiscoAPI.Runtime.VirtualTextures;

namespace DiscoAPI.Runtime.Patches;

public class VirtualTexturePatches
{
	[HarmonyPatch(typeof(PageSequencer), nameof(PageSequencer.ComputeIndex))]
	[HarmonyPostfix]
	private static void OnComputeIndex(int mip, int x, int y, int __result)
	{
		DiscoRunner.Log.LogInfo($"this is page {mip}#({x}, {y}) at index {__result}");
	}

	[HarmonyPatch(typeof(PageFile2), nameof(PageFile2.ReadPage))]
	[HarmonyPrefix]
	private static bool PreReadPage2(
		PageFile2 __instance,
		int index,
		PageRequest pageReq,
		ref (System.Drawing.Rectangle, PageLocation, PageProvider?) __state
	)
	{
		if (!CustomVirtualTextureManager.TryLookup(__instance.m_asset, out var customizer))
		{
			__state = default;
			return true;
		}

		PageLocation page = customizer!.InvertPageId(index);
		if (!customizer.TrySubstitute(page, out var pages, out var overlap)) return true;

		DiscoRunner.Log.LogInfo($"i think this is page {page.mip}#({page.x}, {page.y}) at index {index}");
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
		(System.Drawing.Rectangle overlap, PageLocation page, PageProvider? pages) = __state;
		DiscoRunner.Log.LogInfo($"but now i think this is page {page.mip}#({page.x}, {page.y}) at index {index}");
		if (pages == null) return;

		void FillBy(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<byte> array, GetPixel getPixel)
		{
			for (int y = overlap.Y; y < overlap.Bottom; y++) for (int x = overlap.X; x < overlap.Right; x++)
				{
					var pix = getPixel(page, x, y);
					array[0 + 4 * (y * 136 + x)] = pix.R;
					array[1 + 4 * (y * 136 + x)] = pix.G;
					array[2 + 4 * (y * 136 + x)] = pix.B;
					array[3 + 4 * (y * 136 + x)] = pix.A;
				}
		}

		FillBy(pageReq.diff, (page, x, y) => pages.GetDiffusionPixel(page, x, y));
		FillBy(pageReq.norm, (page, x, y) => pages.GetNormalPixel(page, x, y));
		FillBy(pageReq.spec, (page, x, y) => pages.GetSpecularPixel(page, x, y));

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
