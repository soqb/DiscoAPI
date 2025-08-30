using System;
using System.IO;
using System.Linq;

namespace DiscoAPI.Runtime.VirtualTextures;

/// <summary>
/// DCAVT stands for DCA's crazy-awesome virtual textures!
///
/// That's it. That's all the documentation you get.
/// </summary>
public class DCAVTFile : IDisposable
{
	// the ascii string "dacvt..."
	private static byte[] Magic = new byte[8] { 0x64, 0x63, 0x61, 0x76, 0x74, 0x2e, 0x2e, 0x2e };
	private uint widthInPages;
	private uint mipCount;
	private DCAVTFlags flags;
	internal PageDesc[] pages = new PageDesc[0];
	internal BinaryReader reader;
	internal Stream stream;

	public AdHocTextureConfig AdHocConfig()
	{
		return new(new DCAVTPageProvider(this)) { mipCount = mipCount, widthInPages = widthInPages };
	}

	/// <summary>
	/// Create a new instance from a raw stream.
	///
	/// This file stream must be thread safe.
	/// </summary>
	public DCAVTFile(Stream stream)
	{
		this.stream = stream;
		reader = new(stream);
		if (!TryParse()) throw new FormatException("failed to parse DCAVT file");
	}

	public void Dispose()
	{
		reader.Dispose();
		stream?.Dispose();
	}

	private bool TryParse()
	{
		if (!reader.ReadBytes(8).SequenceEqual(Magic))
		{
			return false;
		}

		_ = reader.ReadBytes(8);

		uint bits = reader.ReadUInt32();
		mipCount = bits >> 24;
		flags = (DCAVTFlags)(bits);
		widthInPages = reader.ReadUInt32();
		DiscoRunner.Log.LogInfo($"dcavt: wip is {widthInPages} and flags are {flags} and mips is {mipCount}");

		if (flags != 0) DiscoRunner.Log.LogWarning($"dcavt: expected flags to be 0 but got {flags}");

		uint area = widthInPages * widthInPages;

		pages = new PageDesc[ComputePageCount()];
		for (uint i = 0; i < pages.Length; i++)
			pages[i] = PageDesc.Parse(reader);

		return true;
	}

	public int PageIndex(PageLocation page)
	{
		int w = (int)widthInPages;
		int factor = 1 << (2 * page.mip);
		int mipOffset = (4 * ((factor - 1) * w * w))
			/ (3 * factor);
		int total = mipOffset + page.x + page.y * (w >> page.mip);
		return total;
	}

	private uint ComputePageCount()
	{
		uint count = 0;
		for (int m = 0; m < mipCount; m++)
		{
			uint w = widthInPages >> m;
			count += w * w;
		}

		return count;
	}

	internal bool TryGetPage(PageLocation location, out PageDesc desc)
	{
		int idx = PageIndex(location);
		if (idx < 0 || idx >= pages.Length)
		{
			desc = default;
			return false;
		}

		desc = pages[idx];
		return true;
	}
}

[Flags]
public enum DCAVTFlags : uint { }

internal struct PageDesc
{
	public long addr;
	public int lengthNeutral;
	public int lengthNormals;
	public int lengthHeightmap;
	public int lengthShadow;

	public static PageDesc Parse(BinaryReader reader)
	{
		return new()
		{
			addr = reader.ReadInt64(),
			lengthNeutral = reader.ReadInt32(),
			lengthNormals = reader.ReadInt32(),
			lengthHeightmap = reader.ReadInt32(),
			lengthShadow = reader.ReadInt32(),
		};
	}
}

public record DCAVTPageReader(DCAVTFile file, PageLocation page) : ICompressedPageReader
{
	public void ReadTo(CompressedPageBuffers buffers)
	{
		lock (file.stream)
		{
			if (!file.TryGetPage(page, out PageDesc d)) return;
			ReadToSpan(buffers.neutral(), d.addr, d.lengthNeutral);
			ReadToSpan(buffers.normals(), d.addr + d.lengthNeutral, d.lengthNormals);
			ReadToSpan(buffers.heightmap(), d.addr + d.lengthNeutral + d.lengthNormals, d.lengthHeightmap);
			ReadToSpan(buffers.shadow(), d.addr + d.lengthNeutral + d.lengthNormals + d.lengthHeightmap, d.lengthShadow);
		}
	}

	private void ReadToSpan(Span<byte> buf, long addr, int size)
	{
		file.reader.BaseStream.Position = addr;
		buf.Slice(size).Fill(0);
		buf = buf.Slice(0, size);

		while (buf.Length > 0)
		{
			int n = file.reader.Read(buf);
			if (n == 0)
			{
				// n == 0 means no more data available.
				DiscoRunner.Log.LogError("not read enough :(((");
				buf.Fill(0);
				return;
			}
			buf = buf.Slice(n);
		}
	}
}

public record DCAVTPageProvider(DCAVTFile file) : IPageProvider
{
	public bool CanProvideUncompressed => false;

	public void Dispose() => file.Dispose();

	public ICompressedPageReader ProvideCompressed(PageLocation page) => new DCAVTPageReader(file, page);
}
