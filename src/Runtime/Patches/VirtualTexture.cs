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

		public void BC4ToPairedRGBA(Span<byte> input, Span<byte> output, byte ch0, byte ch1)
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
		ref bool __result,
		// use a state variable to track data between `Pre` and `Post` patches:
		ref (Rectangle, PageBuffers?, ICompressedPageReader) __state
	)
	{
		if (!VirtualTextureComponents.Customizer.TryOf(__instance.m_asset, out var czar)) return true;

		PageLocation page = VirtualTextureCustomizer.InvertPageId(__instance.m_asset, index);

		// allow uncompressed buffers iff context not compressed,
		// otherwise, data must be compressed (we can decompress later if necessary):
		if (!decoder.m_compressed && czar.TrySubstituteUncompressed(page, out var buffers, out var overlap2))
		{
			__state = (overlap2, buffers, ICompressedPageReader.Empty);
		}
		else
		{
			if (!czar.TrySubstituteCompressed(page, out var reader, out var overlap3))
			{
				// if no page (whether compressed or not) just report success since nothing to display anyway:
				return true;
			}
			__state = (overlap3, null, reader);
		}

		__result = true;

		// if overlap is complete, don't bother with original decoding because it will all be replaced:
		Rectangle overlap = __state.Item1;
		return overlap.Width != 136 || overlap.Height != 136;
	}


	[HarmonyPatch(typeof(PageFile2), nameof(PageFile2.ReadPage))]
	[HarmonyPostfix]
	private static void PostReadPage2(
		PageFile2 __instance,
		PageDecoder decoder,
		int index,
		PageRequest pageReq,
		(Rectangle, PageBuffers?, ICompressedPageReader?) __state
	)
	{
		(Rectangle overlap, PageBuffers? nullableBuffers, ICompressedPageReader? reader) = __state;

		if (overlap.IsEmpty) return;

		if (decoder.m_compressed)
		{
			CompressedPageBuffers compressedBuffers = new CompressedPageBuffers()
			{
				neutral = pageReq.diff0.AsSpan,
				normals = pageReq.norm0.AsSpan,
				heightmap = pageReq.norm1.AsSpan,
				shadow = pageReq.spec0.AsSpan,
				shadow2 = pageReq.spec1.AsSpan,
			};
			// if context compressed with nonempty overlap, then buffers definitely compressed.
			reader!.ReadTo(compressedBuffers);
		}
		else
		{
			ByteSpanPixelBuffer diff = new(pageReq.diff.AsSpan(), 136);
			ByteSpanPixelBuffer norm = new(pageReq.norm.AsSpan(), 136);
			ByteSpanPixelBuffer spec = new(pageReq.spec.AsSpan(), 136);
			VirtualTextureBlitter blitter = new(diff, norm, spec);

			if (nullableBuffers is PageBuffers buffers) blitter.Draw(overlap, buffers);
			else if (reader != null)
			{
				// if context not compressed, but page is, then decompress:
				CompressedPageBuffers compressedBuffers = new CompressedPageBuffers()
				{
					neutral = decoder.m_tempDiff0.AsSpan,
					normals = decoder.m_tempNorm0.AsSpan,
					heightmap = decoder.m_tempNorm1.AsSpan,
					shadow = decoder.m_tempSpec0.AsSpan,
					shadow2 = decoder.m_tempSpec1.AsSpan,
				};
				reader.ReadTo(compressedBuffers);

				NativeDecompressor decomp = new(decoder.m_decoderHandle);
				blitter.DecompressFromBuffers(decomp, compressedBuffers);
			}

			// fancy debug effects only supported when compression disabled:
			if (DiscoAPISettings.DisableVTDiffusionMaps) diff.mem.Fill(0);
			if (DiscoAPISettings.DisableVTNormalMaps) norm.mem.Fill(0);
			if (DiscoAPISettings.DisableVTSpecularMaps) spec.mem.Fill(0);
			if (DiscoAPISettings.DrawVTBorders) blitter.DrawDebugBorders();
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
	private static void OnInitializeTextureCollections(AmplifyTextureManager __instance)
	{
		foreach (var collection in CustomVirtualTextureManager.collections.Values)
		{
			__instance.VirtualTextureCollections.Add(collection);
		}
	}

	[HarmonyPatch(typeof(AmplifyTextureCamera), "InternalInitialize")]
	[HarmonyPrefix]
	private static void OnCameraInternalInitialize(AmplifyTextureCamera __instance, EditorRuntimeProperties editorProps)
	{
		if (!DiscoAPISettings.ForceDisableVTCacheCompression) return;

		if (editorProps != null) editorProps.m_cacheCompression = false;
		else __instance.m_cacheCompression = false;

		DiscoRunner.Log.LogInfo("disabled virtual texture cache compression. performance will be impacted.");
	}
}
