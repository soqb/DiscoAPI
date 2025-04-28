using HarmonyLib;
using AmplifyTexture;
using DiscoAPI.Runtime.VirtualTextures;

namespace DiscoAPI.Runtime.Patches;

public class VirtualTexturePatches
{
	[HarmonyPatch(typeof(PageFile2), nameof(PageFile2.ReadPage))]
	[HarmonyPrefix]
	private static bool PreReadPage2(
		PageFile2 __instance,
		int index,
		PageRequest pageReq,
		ref bool __result,
		ref (System.Drawing.Rectangle, PageBuffers?) __state
	)
	{
		if (!ModVirtualTexture.CustomizerKey.TryOf(__instance.m_asset, out var customizer)) return true;

		PageLocation page = VirtualTextureCustomizer.InvertPageId(__instance.m_asset, index);
		if (!customizer.TrySubstitute(page, out var buffers, out var overlap)) return true;
		__state = (overlap, buffers);

		// if overlap is complete, don't bother with original decoding because it will all be replaced.
		__result = overlap.Width == 136 && overlap.Height == 136;
		return !__result;
	}


	[HarmonyPatch(typeof(PageFile2), nameof(PageFile2.ReadPage))]
	[HarmonyPostfix]
	private static void PostReadPage2(
		PageFile2 __instance,
		int index,
		PageRequest pageReq,
		(System.Drawing.Rectangle, PageBuffers?) __state
	)
	{
		(System.Drawing.Rectangle overlap, PageBuffers? buffers) = __state;

		Il2CppRGBABuffer diff = new(136, pageReq.diff);
		Il2CppRGBABuffer norm = new(136, pageReq.norm);
		Il2CppRGBABuffer spec = new(136, pageReq.spec);

		new VirtualTextureBlitter(diff, norm, spec).Draw(overlap, buffers);
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
