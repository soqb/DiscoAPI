using System;
using System.Drawing;

namespace DiscoAPI.Runtime.VirtualTextures;

public interface IPixelBuffer
{
	public static Color Fallback(PageLocation page, int x, int y)
	{
		if ((page.x + page.y) % 2 == (x / 64 + y / 64) % 2) return Color.Green;
		else return Color.Purple;
	}

	Color this[int x, int y] { get; }
}

public interface IMutablePixelBuffer : IPixelBuffer
{
	new Color this[int x, int y] { get; set; }
	Color IPixelBuffer.this[int x, int y] { get => this[x, y]; }
}

public struct PageBuffers
{
	public IPixelBuffer diffusion;
	public IPixelBuffer normal;
	public IPixelBuffer specular;
}

public interface IPageProvider : IDisposable
{
	ICompressedPageReader ProvideCompressed(PageLocation page);

	bool CanProvideUncompressed { get; }

	PageBuffers ProvideUncompressed(PageLocation page)
		=> throw new InvalidOperationException($"{GetType()} does not support uncompressed reads");
}

public interface ICompressedPageReader
{
	void ReadTo(CompressedPageBuffers target);

	private class EmptyCompressedPageReader : ICompressedPageReader
	{
		public void ReadTo(CompressedPageBuffers target) { }
	}

	public static ICompressedPageReader Empty => new EmptyCompressedPageReader();
}

// public class BitmapPageProvider : IPageProvider
// {
// 	public bool didFail;
// 	public Bitmap[] diffusionMips;
// 	public Rectangle area;

// 	private Bitmap[] GenerateMipmaps(Bitmap fullQuality, int mipCount)
// 	{
// 		var mipmaps = new Bitmap[mipCount];
// 		for (int mip = 0; mip < mipCount; mip++)
// 		{
// 			Size size = new(area.Width >> mip, area.Height >> mip);
// 			if (fullQuality.Size == size) mipmaps[mip] = fullQuality;
// 			else mipmaps[mip] = new(fullQuality, size);
// 		}

// 		return mipmaps;
// 	}

// 	private BitmapPageProvider(Bitmap fullQuality, int mipCount, Rectangle area)
// 	{
// 		this.area = area;
// 		try
// 		{
// 			diffusionMips = GenerateMipmaps(fullQuality, mipCount);
// 		}
// 		catch (System.Exception e)
// 		{
// 			didFail = true;
// 			diffusionMips = new Bitmap[0];
// 			DiscoRunner.Log.LogWarning($"vt loading failed: {e}");
// 		}
// 	}

// 	private record BitmapDiffusionPixelBuffer(BitmapPageProvider parent, PageLocation page) : IPixelBuffer
// 	{
// 		public Color this[int x, int y]
// 		{
// 			get
// 			{
// 				if (parent.didFail) return IPixelBuffer.Fallback(page, x, y);

// 				Bitmap mipmap = parent.diffusionMips[page.mip];
// 				int pageLeft = page.x * 128 - (parent.area.X >> page.mip);
// 				int pageTop = page.y * 128 - (parent.area.Y >> page.mip);
// 				int mmx = pageLeft + x;
// 				int mmy = pageTop + y;

// 				try
// 				{
// 					int xb = Math.Max(0, Math.Min(mmx, mipmap.Width - 1));
// 					int yb = Math.Max(0, Math.Min(mmy, mipmap.Height - 1));
// 					return mipmap.GetPixel(xb, yb);
// 				}
// 				catch (Exception ex)
// 				{
// 					DiscoRunner.Log.LogWarning($"failed to decode pixel {page.mip}#({x}, {y}): {ex.GetType().ToString()}");
// 					return IPixelBuffer.Fallback(page, x, y);
// 				}
// 			}
// 		}
// 	}

// 	public PageBuffers Provide(PageLocation page) => new()
// 	{
// 		diffusion = new BitmapDiffusionPixelBuffer(this, page),
// 		normal = new FillColor(Color.FromArgb(0, 0, 0, 255)),
// 		specular = new FillColor(Color.White),
// 	};
// }

[Flags]
public enum ChannelFilter
{
	R = 1 << 0,
	G = 1 << 1,
	B = 1 << 2,
	A = 1 << 3
}

public readonly record struct FilteredBuffer(IPixelBuffer inner, ChannelFilter filter) : IPixelBuffer
{
	public Color this[int x, int y]
	{
		get
		{
			Color c = inner[x, y];
			byte a = 255, r = 0, g = 0, b = 0;
			if ((filter & ChannelFilter.A) != 0) a = c.A;
			if ((filter & ChannelFilter.R) != 0) r = c.R;
			if ((filter & ChannelFilter.G) != 0) g = c.G;
			if ((filter & ChannelFilter.B) != 0) b = c.B;
			return Color.FromArgb(a, r, g, b);
		}
	}
}

public readonly record struct FillColor(Color color) : IPixelBuffer
{
	public Color this[int x, int y] => color;
}

public readonly record struct LinearGradient(Color topLeft, Color bottomRight) : IPixelBuffer
{
	public Color this[int x, int y]
	{
		get
		{
			float t = MathF.Min(1f, ((float)(x + y)) / 272f);
			float r = topLeft.R * (1 - t) + bottomRight.R * t;
			float g = topLeft.G * (1 - t) + bottomRight.G * t;
			float b = topLeft.B * (1 - t) + bottomRight.B * t;
			float a = topLeft.A * (1 - t) + bottomRight.A * t;
			return Color.FromArgb((int)a, (int)r, (int)g, (int)b);
		}
	}
}

// NB: doesn't implement IMutablePixelBuffer because
//     we're stuck on poor old C# 10.
public readonly ref struct ByteSpanPixelBuffer(Span<byte> mem, int width)
{
	public readonly int width = width;
	public readonly Span<byte> span = mem;

	public Color this[int x, int y]
	{
		get => Color.FromArgb(
			span[3 + 4 * (y * width + x)],
			span[0 + 4 * (y * width + x)],
			span[1 + 4 * (y * width + x)],
			span[2 + 4 * (y * width + x)]
		);
		set
		{
			span[0 + 4 * (y * width + x)] = value.R;
			span[1 + 4 * (y * width + x)] = value.G;
			span[2 + 4 * (y * width + x)] = value.B;
			span[3 + 4 * (y * width + x)] = value.A;
		}
	}
}

public interface IBcnDecompressor
{
	public void BC1ToRGBX(Span<byte> input, Span<byte> output);
	public void BC5ToPairedRGBA(Span<byte> input, Span<byte> output, byte ch0, byte ch1);
}

public readonly ref struct VirtualTextureBlitter(ByteSpanPixelBuffer diff, ByteSpanPixelBuffer norm, ByteSpanPixelBuffer spec)
{
	public readonly ByteSpanPixelBuffer diff = diff;
	public readonly ByteSpanPixelBuffer norm = norm;
	public readonly ByteSpanPixelBuffer spec = spec;

	public static void Blit(IPixelBuffer source, ByteSpanPixelBuffer dest, Rectangle area, Point offset)
	{
		for (int y = area.Top; y < area.Bottom; y++)
			for (int x = area.Left; x < area.Right; x++)
				dest[x + offset.X, y + offset.Y] = source[x, y];
	}

	public static Color DebugColor(int x, int y)
	{
		int d = Math.Min(255, (x + y) * 16 / 17);
		return Color.FromArgb(d, 255 - d, d);
	}

	public const int DebugBorderWidth = 6;

	public void DrawDebugOverlay(PageLocation page)
	{
		for (int y = 0; y < DebugBorderWidth; y++)
			for (int x = 0; x < 136; x++) diff[x, y] = DebugColor(x, y);

		for (int y = 136 - DebugBorderWidth; y < 136; y++)
			for (int x = 0; x < 136; x++) diff[x, y] = DebugColor(x, y);

		for (int y = DebugBorderWidth; y < 136 - DebugBorderWidth; y++)
		{
			for (int x = 0; x < DebugBorderWidth; x++) diff[x, y] = DebugColor(x, y);
			for (int x = 136 - DebugBorderWidth; x < 136; x++) diff[x, y] = DebugColor(x, y);
		}

		Bitfont.DrawGlyph(diff, 16, 16, Bitfont.glyphMColonLigature);
		Bitfont.DrawGlyph(diff, 16, 16 + 1 * 7 * Bitfont.WIDTH, Bitfont.glyphXColonLigature);
		Bitfont.DrawGlyph(diff, 16, 16 + 2 * 7 * Bitfont.WIDTH, Bitfont.glyphYColonLigature);
		Bitfont.DrawInt(diff, 16 + Bitfont.WIDTH * 7, 16, page.mip);
		Bitfont.DrawInt(diff, 16 + Bitfont.WIDTH * 7, 16 + 1 * 7 * Bitfont.WIDTH, page.x);
		Bitfont.DrawInt(diff, 16 + Bitfont.WIDTH * 7, 16 + 2 * 7 * Bitfont.WIDTH, page.y);
	}

	public void Draw(Rectangle overlap, PageBuffers buffers)
	{
		Blit(buffers.diffusion, diff, overlap, new(4, 4));
		Blit(buffers.normal, norm, overlap, new(4, 4));
		Blit(buffers.specular, spec, overlap, new(4, 4));
	}

	public void DecompressFromBuffers(IBcnDecompressor decompressor, CompressedPageBuffers src)
	{
		Span<byte> neutral = src.neutral();
		Span<byte> normals = src.normals();
		Span<byte> heightmap = src.heightmap();
		Span<byte> shadow = src.shadow();
		Span<byte> shadow2 = src.shadow2();

		for (int i = 0; i < norm.span.Length / 4; i++)
		{
			norm.span[4 * i + 0] = normals[2 * i + 0];
			norm.span[4 * i + 2] = normals[2 * i + 1];
		}

		decompressor.BC1ToRGBX(neutral, diff.span);
		decompressor.BC5ToPairedRGBA(heightmap, norm.span, 1, 3);
		decompressor.BC5ToPairedRGBA(shadow, spec.span, 0, 1);
		decompressor.BC5ToPairedRGBA(shadow2, spec.span, 2, 3);
	}

	public static void FillWith(IMutablePixelBuffer buffer, Color color)
	{
		for (int y = 0; y < 136; y++)
			for (int x = 0; x < 136; x++)
				buffer[x, y] = color;
	}
}

public struct CompressedPageBuffers
{
	public delegate Span<byte> AsSpan();

	// use delegates instead of fields because making this a readonly ref struct causes issues:
	public AsSpan neutral;
	public AsSpan normals;
	public AsSpan heightmap;
	public AsSpan shadow;
	public AsSpan shadow2;

	private static Span<byte> EmptySpan() => Span<byte>.Empty;

	public static CompressedPageBuffers Empty => new CompressedPageBuffers()
	{
		neutral = EmptySpan,
		normals = EmptySpan,
		heightmap = EmptySpan,
		shadow = EmptySpan,
		shadow2 = EmptySpan,
	};

	public void MaybeDisable(bool noDiffusion, bool noNormal, bool noSpecular)
	{
		// NB: while buffers are still compressed, all zeros is still all black.
		if (noDiffusion)
		{
			neutral().Fill(0);
			neutral = EmptySpan;
		}
		if (noNormal)
		{
			normals().Fill(0);
			heightmap().Fill(0);
			normals = EmptySpan;
			heightmap = EmptySpan;
		}
		if (noSpecular)
		{
			shadow().Fill(0);
			shadow2().Fill(0);
			shadow = EmptySpan;
			shadow2 = EmptySpan;
		}
	}
}

// the things i do..
public class Bitfont
{
	public const int WIDTH = 4;

	// digits 0-9.
	// filled state in column-major order but little-endian so from bottom right to top-left:
	public static uint[] glyphDigits = new uint[] {
		0b011101000110001100011000101110,
		0b111110010000100001000011000100,
		0b111110001000100010001000101110,
		0b011101000101100100001000101110,
		0b001000010011111001010010100001,
		0b011111000001110000010000111111,
		0b011101000101111000010000111110,
		0b001000010001000010001000011111,
		0b011101000101110100011000101110,
		0b100001000011110100011000101110,
	};
	public static uint glyphMColonLigature = 0b101010011110101000000000000000;
	public static uint glyphXColonLigature = 0b101010001010101000000000000000;
	public static uint glyphYColonLigature = 0b100010001010101000000000000000;

	public static void DrawGlyph(ByteSpanPixelBuffer buffer, int x, int y, uint glyph)
	{
		for (byte bit = 0; bit < 30; bit++, glyph >>= 1)
		{
			if (glyph % 2 == 0) continue;
			for (int i = 0; i < WIDTH * WIDTH; i++)
			{
				int rx = x + (bit % 5) * WIDTH + i % WIDTH;
				int ry = y + (bit / 5) * WIDTH + i / WIDTH;
				buffer[rx, ry] = VirtualTextureBlitter.DebugColor(rx, ry);
			}
		}
	}

	public static void DrawInt(ByteSpanPixelBuffer buffer, int startX, int startY, int number)
	{
		// what's up, log?
		double uplog = Math.Max(0, Math.Log10(number));
		// num digits minus one
		int m1Digits = (int)Math.Floor(uplog);

		int x = startX + m1Digits * 6 * WIDTH;
		for (int i = 0; i <= m1Digits; i++)
		{
			DrawGlyph(buffer, x, startY, glyphDigits[number % 10]);
			number /= 10;
			x -= 6 * WIDTH;
		}
	}
}
