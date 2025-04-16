using HarmonyLib;
using AmplifyTexture;
using DiscoAPI.Runtime.VirtualTextures;

namespace DiscoAPI.Runtime.Patches;

public class VirtualTexturePatches
{
	// [HarmonyPatch(typeof(PageDecoder), nameof(PageDecoder.DecodePage2))]
	// [HarmonyPostfix]
	// private static void OnDecodePage2(PageRequest pageReq, byte[] input)
	// {
	// 	DiscoRunner.Log.LogInfo($"page: {pageReq.page.x}x{pageReq.page.y}");
	// 	DiscoRunner.Log.LogInfo($"page data is {input.Length} long");
	// 	DiscoRunner.Log.LogInfo($"page diffuse is {pageReq.diff?.Length} long or {pageReq.diff0?.Length} long");

	// 	int width = 136, height = 136;
	// 	byte[] data = pageReq.diff;

	// 	var page = pageReq.page;

	// 	string vtDir = Path.Join(BepInEx.Paths.BepInExRootPath, "discoDumps", "vt", $"{page.asset}-m{page.mip}");
	// 	Directory.CreateDirectory(vtDir);
	// 	string path = Path.Join(vtDir, $"{page.x}x{page.y}.bmp");

	// 	unsafe
	// 	{
	// 		fixed (byte* ptr = data)
	// 		{
	// 			using (Bitmap image = new Bitmap(width, height, width * 4, PixelFormat.Format32bppRgb, new IntPtr(ptr)))
	// 			{
	// 				image.Save(path);
	// 			}
	// 		}
	// 	}
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
		if (!CustomVirtualTextureManager.TryLookup(__instance.m_asset, out var texture)) return true;

		PageLocation page = texture.InvertPageId(index);
		if (!texture.TrySubstitute(page, out var pages, out var overlap)) return true;

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
		if (pages == null) return;

		DiscoRunner.Log.LogInfo($"blitting overlap ({overlap.X}, {overlap.Y}) to ({overlap.Right}, {overlap.Bottom}) which is {overlap.Width * overlap.Height} pixels");

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
		// if (CustomVirtualTextureManager.TryLookup(__instance.m_asset, out var texture))
		// {
		// 	__instance.m_stream = Il2CppSystem.IO.Stream.Null;
		// 	__result = true;
		// 	return false;
		// }
		return true;
	}

	// NB: we would like this to be temporary but re-enabling VT cache compression
	// will require further digging into the VTC2 format, which i really don't want to do.
	[HarmonyPatch(typeof(AmplifyTextureCamera), "InternalInitialize")]
	[HarmonyPrefix]
	private static void OnCameraInternalInitialize(AmplifyTextureCamera __instance, EditorRuntimeProperties editorProps)
	{
		if (editorProps != null) editorProps.m_cacheCompression = false;
		else __instance.m_cacheCompression = false;

		DiscoRunner.Log.LogInfo("disabled cache compression !!");
	}
}
