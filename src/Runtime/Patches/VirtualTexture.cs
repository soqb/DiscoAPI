using HarmonyLib;
using AmplifyTexture;
using DiscoAPI.Runtime.VirtualTextures;
using Rectangle = System.Drawing.Rectangle;
using System;

namespace DiscoAPI.Runtime.Patches;

public class VirtualTexturePatches
{
	private record NativeDecompressor(IntPtr nativeHandle) : IBcnDecompressor
	{
		public void BC1ToRGBX(Span<byte> input, Span<byte> output)
		{
			if (input.Length == 0) return;
			NativeRuntime.DecodeBC1ToRGBX(nativeHandle, ref input[0], ref output[0]);
		}

		public void BC5ToPairedRGBA(Span<byte> input, Span<byte> output, byte ch0, byte ch1)
		{
			if (input.Length == 0) return;
			NativeRuntime.DecodeBC5ToRGBA(nativeHandle, ref input[0], ref output[0], ch0, ch1);
		}
	}

	[HarmonyPatch(typeof(PageFile2), nameof(PageFile2.ReadPage))]
	[HarmonyPrefix]
	private static bool PreReadPage2(
		PageFile2 __instance,
		PageDecoder decoder,
		int index,
		PageRequest pageReq,
		// use a state variable to track data between `Pre` and `Post` patches:
		ref (PageLocation page, Rectangle, PageBuffers?, ICompressedPageReader?) __state
	)
	{
		PageLocation page = VirtualTextureCustomizer.InvertPageId(__instance.m_asset, index);
		__state.Item1 = page;

		if (!VirtualTextureComponents.Customizer.TryOf(__instance.m_asset, out var czar)) return true;

		Rectangle overlap;
		PageBuffers? buffers = null;
		ICompressedPageReader? reader = null;

		// allow uncompressed buffers iff context not compressed,
		// otherwise, data must be compressed (we can decompress later if necessary):
		if (decoder.m_compressed || !czar.TrySubstituteUncompressed(page, out buffers, out overlap))
		{
			// force compression.
			czar.TrySubstituteCompressed(page, out reader, out overlap);
		}

		__state = (page, overlap, buffers, reader);

		// if overlap is complete, don't bother with original decoding because it will all be replaced:
		bool overlapNotFull = overlap.Width != 136 || overlap.Height != 136;
		return overlapNotFull;
	}


	[HarmonyPatch(typeof(PageFile2), nameof(PageFile2.ReadPage))]
	[HarmonyPostfix]
	private static void PostReadPage2(
		PageFile2 __instance,
		PageDecoder decoder,
		int index,
		PageRequest pageReq,
		ref bool __result,
		(PageLocation, Rectangle, PageBuffers?, ICompressedPageReader?) __state
	)
	{
		// NB: if buffers notnull, reader null.
		(PageLocation page, Rectangle overlap, PageBuffers? nullableBuffers, ICompressedPageReader? reader) = __state;

		__result = true;

		// if no overlap, do nothing.
		if (overlap.Width == 0 && overlap.Height == 0) return;

		void ReadToCompressed(CompressedPageBuffers compressedBuffers)
		{
			compressedBuffers.MaybeDisable(
				DiscoAPISettings.DisableVTDiffusionMaps,
				DiscoAPISettings.DisableVTNormalMaps,
				DiscoAPISettings.DisableVTSpecularMaps
			);
			reader!.ReadTo(compressedBuffers);
		}

		if (decoder.m_compressed)
		{
			if (reader != null) {
				// (1) AT uses these buffers when compressed..
				ReadToCompressed(new CompressedPageBuffers()
				{
					neutral = pageReq.diff0.AsSpan,
					normals = pageReq.norm0.AsSpan,
					heightmap = pageReq.norm1.AsSpan,
					shadow = pageReq.spec0.AsSpan,
					shadow2 = pageReq.spec1.AsSpan,
				});
			}
		}
		else
		{
			ByteSpanPixelBuffer diff = new(pageReq.diff.AsSpan(), 136);
			ByteSpanPixelBuffer norm = new(pageReq.norm.AsSpan(), 136);
			ByteSpanPixelBuffer spec = new(pageReq.spec.AsSpan(), 136);
			VirtualTextureBlitter blitter = new(diff, norm, spec);

			if (nullableBuffers is PageBuffers buffers)
			{
				blitter.Draw(overlap, buffers);
				if (DiscoAPISettings.DisableVTDiffusionMaps) diff.span.Fill(0);
				if (DiscoAPISettings.DisableVTNormalMaps) norm.span.Fill(0);
				if (DiscoAPISettings.DisableVTSpecularMaps) spec.span.Fill(0);
			}
			else if (reader != null)
			{
				// (2) and these when not compressed..
				CompressedPageBuffers compressedBuffers = new CompressedPageBuffers()
				{
					neutral = decoder.m_tempDiff0.AsSpan,
					normals = decoder.m_tempNorm0.AsSpan,
					heightmap = decoder.m_tempNorm1.AsSpan,
					shadow = decoder.m_tempSpec0.AsSpan,
					shadow2 = decoder.m_tempSpec1.AsSpan,
				};
				ReadToCompressed(compressedBuffers);

				// if context not compressed, but page is, then decompress:
				NativeDecompressor decomp = new(decoder.m_decoderHandle);
				blitter.DecompressFromBuffers(decomp, compressedBuffers);
			}

			// fancy debug effects only supported when compression disabled:
			if (DiscoAPISettings.DrawVTOverlay) blitter.DrawDebugOverlay(page);
		}

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
	private static bool OnInitializeTextureCollections(AmplifyTextureManager __instance)
	{
		foreach (var col in CustomVirtualTextureManager.collections.Values)
		{
			__instance.VirtualTextureCollections.Add(col);
		}

		foreach (var col in __instance.VirtualTextureCollections)
		{
			if (__instance.m_collections.TryAdd(col.UniqueName, col)) col.Initialize();
		}

		return false;
	}

	[HarmonyPatch(typeof(AmplifyTextureCamera), "InternalInitialize")]
	[HarmonyPrefix]
	private static void OnCameraInternalInitialize(AmplifyTextureCamera __instance, EditorRuntimeProperties? editorProps)
	{
		if (editorProps != null) editorProps.m_cacheCompression = !DiscoAPISettings.ForceDisableVTCacheCompression;
		__instance.m_cacheCompression = !DiscoAPISettings.ForceDisableVTCacheCompression;
	}
}
