using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DiscoAPI.Runtime.VirtualTextures;

public class DCAVTFile : IDisposable
{
	// the ascii string "dacvt..."
	private static byte[] Magic = new byte[8] { 0x64, 0x63, 0x61, 0x76, 0x74, 0x2e, 0x2e, 0x2e };
	private uint widthInPages;
	private uint mipCount;
	private DCAVTFlags flags;
	private PageTable pages = new();
	internal BinaryReader reader;

	public AdHocTextureConfig AdHocConfig()
	{
		return new() { mipCount = mipCount, widthInPages = widthInPages };
	}

	public DCAVTFile(BinaryReader reader)
	{
		this.reader = reader;
		if (!TryParse()) throw new FormatException("failed to parse DCAVT file");
	}

	public void Dispose() => reader.Dispose();

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
		pages = PageTable.Parse(reader, widthInPages);
		return true;
	}

	public uint PageIndex(PageLocation page)
	{
		int w = (int)widthInPages;
		int factor = 1 << (2 * page.mip);
		int mipOffset = (4 * ((factor - 1) * w * w))
			/ (3 * factor);
		int total = mipOffset + page.x + page.y * (w >> page.mip);
		return (uint)total;
	}

	internal bool TryGetPage(PageLocation location, out PageDesc desc)
	{
		return pages.pages.TryGetValue(PageIndex(location), out desc);
	}
}

[Flags]
public enum DCAVTFlags : uint { }

internal class PageTable
{
	internal Dictionary<uint, PageDesc> pages = new();

	public static PageTable Parse(BinaryReader reader, uint widthInPages)
	{
		PageTable table = new();
		uint pageCount = widthInPages * widthInPages;

		for (uint i = 0; i < pageCount; i++)
			table.pages.Add(i, PageDesc.Parse(reader));

		return table;
	}
}

internal struct PageDesc
{
	public long addr;
	public short lengthNeutral;
	public short lengthNormals;
	public short lengthHeightmap;
	public short lengthShadow;

	public static PageDesc Parse(BinaryReader reader)
	{
		return new()
		{
			addr = reader.ReadInt64(),
			lengthNeutral = reader.ReadInt16(),
			lengthNormals = reader.ReadInt16(),
			lengthHeightmap = reader.ReadInt16(),
			lengthShadow = reader.ReadInt16(),
		};
	}
}

public record DCAVTPageReader(DCAVTFile file, PageLocation page) : ICompressedPageReader
{
	public void ReadTo(CompressedPageBuffers buffers)
	{
		if (!file.TryGetPage(page, out PageDesc d)) return;
		ReadToSpan(buffers.neutral(), d.addr, d.lengthNeutral);
		ReadToSpan(buffers.normals(), d.addr + d.lengthNeutral, d.lengthNormals);
		ReadToSpan(buffers.heightmap(), d.addr + d.lengthNeutral + d.lengthNormals, d.lengthHeightmap);
		ReadToSpan(buffers.shadow(), d.addr + d.lengthNeutral + d.lengthNormals + d.lengthHeightmap, d.lengthShadow);
	}

	private void ReadToSpan(Span<byte> buf, long addr, int size)
	{
		file.reader.BaseStream.Position = addr;
		if (size != file.reader.Read(buf.Slice(0, size)))
		{
			DiscoRunner.Log.LogInfo("not read enough :(((");
			buf.Slice(0, size).Fill(0);
		}
	}
}

public record DCAVTPageProvider(DCAVTFile file) : IPageProvider
{
	public bool CanProvideUncompressed => false;

	public void Dispose()
	{
		throw new NotImplementedException();
	}

	public ICompressedPageReader ProvideCompressed(PageLocation page) => new DCAVTPageReader(file, page);
}
