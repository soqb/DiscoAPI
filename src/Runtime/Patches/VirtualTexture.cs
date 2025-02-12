using HarmonyLib;
using AmplifyTexture;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace DiscoAPI.Runtime.Patches;

public class VirtualTexturePatches
{
	[HarmonyPatch(typeof(PageDecoder), nameof(PageDecoder.DecodePage2))]
	[HarmonyPostfix]
	private static void OnDecodePage2(PageRequest pageReq, byte[] input)
	{
		DiscoRunner.Log.LogInfo($"page: {pageReq.page.x}x{pageReq.page.y}");
		DiscoRunner.Log.LogInfo($"page data is {input.Length} long");
		DiscoRunner.Log.LogInfo($"page diffuse is {pageReq.diff?.Length} long or {pageReq.diff0?.Length} long");

		int width = 136, height = 136;
		byte[] data = pageReq.diff;

		var page = pageReq.page;

		string vtDir = Path.Join(BepInEx.Paths.BepInExRootPath, "discoDumps", "vt", $"{page.asset}-m{page.mip}");
		Directory.CreateDirectory(vtDir);
		string path = Path.Join(vtDir, $"{page.x}x{page.y}.bmp");

		unsafe
		{
			fixed (byte* ptr = data)
			{
				using (Bitmap image = new Bitmap(width, height, width * 4, PixelFormat.Format32bppRgb, new IntPtr(ptr)))
				{
					image.Save(path);
				}
			}
		}
	}

	[HarmonyPatch(typeof(AmplifyTextureCamera), "InternalInitialize")]
	[HarmonyPrefix]
	private static void OnCameraInternalInitialize(AmplifyTextureCamera __instance, EditorRuntimeProperties editorProps)
	{
		if (editorProps != null) editorProps.m_cacheCompression = false;
		else __instance.m_cacheCompression = false;

		DiscoRunner.Log.LogInfo("disabled cache compression !!");
	}
}
