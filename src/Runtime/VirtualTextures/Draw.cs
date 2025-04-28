using System;
using System.Drawing;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

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

public interface IPageProvider
{
	public delegate IPageProvider Factory(int mipCount, Rectangle area);

	PageBuffers Provide(PageLocation page);
}

public class BitmapPageProvider : IPageProvider
{
	public bool didFail;
	public Bitmap[] diffusionMips;
	public Rectangle area;

	private Bitmap[] GenerateMipmaps(Bitmap fullQuality, int mipCount)
	{
		var mipmaps = new Bitmap[mipCount];
		for (int mip = 0; mip < mipCount; mip++)
		{
			Size size = new(area.Width >> mip, area.Height >> mip);
			if (fullQuality.Size == size) mipmaps[mip] = fullQuality;
			else mipmaps[mip] = new(fullQuality, size);
		}

		return mipmaps;
	}

	private BitmapPageProvider(Bitmap fullQuality, int mipCount, Rectangle area)
	{
		this.area = area;
		try
		{
			diffusionMips = GenerateMipmaps(fullQuality, mipCount);
		}
		catch (System.Exception e)
		{
			didFail = true;
			diffusionMips = new Bitmap[0];
			DiscoRunner.Log.LogWarning($"vt loading failed: {e}");
		}
	}

	public static IPageProvider.Factory FromFile(string path) => (mipCount, area) =>
	{
		using (Bitmap bmp = new(path)) return new BitmapPageProvider(bmp, mipCount, area);
	};

	private record BitmapDiffusionPixelBuffer(BitmapPageProvider parent, PageLocation page) : IPixelBuffer
	{
		public Color this[int x, int y]
		{
			get
			{
				if (parent.didFail) return IPixelBuffer.Fallback(page, x, y);

				Bitmap mipmap = parent.diffusionMips[page.mip];
				int pageLeft = page.x * 128 - (parent.area.X >> page.mip);
				int pageTop = page.y * 128 - (parent.area.Y >> page.mip);
				int mmx = pageLeft + x;
				int mmy = pageTop + y;

				try
				{
					int xb = Math.Max(0, Math.Min(mmx, mipmap.Width - 1));
					int yb = Math.Max(0, Math.Min(mmy, mipmap.Height - 1));
					return mipmap.GetPixel(xb, yb);
				}
				catch (Exception ex)
				{
					DiscoRunner.Log.LogWarning($"failed to decode pixel {page.mip}#({x}, {y}): {ex.GetType().ToString()}");
					return IPixelBuffer.Fallback(page, x, y);
				}
			}
		}
	}

	public PageBuffers Provide(PageLocation page) => new()
	{
		diffusion = new BitmapDiffusionPixelBuffer(this, page),
		normal = new FillColor(Color.FromArgb(0, 0, 0, 255)),
		specular = new FillColor(Color.White),
	};
}

public readonly record struct Il2CppRGBABuffer(int width, Il2CppArrayBase<byte> array) : IMutablePixelBuffer
{
	public Color this[int x, int y]
	{
		get => Color.FromArgb(
			array[0 + 4 * (y * width + x)],
			array[1 + 4 * (y * width + x)],
			array[2 + 4 * (y * width + x)],
			array[3 + 4 * (y * width + x)]
		);
		set
		{

			array[0 + 4 * (y * width + x)] = value.R;
			array[1 + 4 * (y * width + x)] = value.G;
			array[2 + 4 * (y * width + x)] = value.B;
			array[3 + 4 * (y * width + x)] = value.A;
		}
	}
}

public readonly record struct FillColor(Color color) : IPixelBuffer
{
	public Color this[int x, int y] { get => color; }
}

public record VirtualTextureBlitter(
	IMutablePixelBuffer diff,
	IMutablePixelBuffer norm,
	IMutablePixelBuffer spec
)
{
	private void Blit(IPixelBuffer source, IMutablePixelBuffer dest, Rectangle area, Point offset)
	{
		for (int y = area.Top; y < area.Bottom; y++)
			for (int x = area.Left; x < area.Right; x++)
				dest[x + offset.X, y + offset.Y] = source[x, y];
	}

	private Color DebugColor(int x, int y)
	{
		int d = Math.Min(255, (x + y) * 16 / 17);
		return System.Drawing.Color.FromArgb(d, 255 - d, d);
	}

	public const int DebugBorderWidth = 6;

	private void DrawDebugBorders()
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

	}

	public void Draw(Rectangle overlap, PageBuffers? buffers)
	{
		if (buffers != null)
		{
			var buffers2 = (PageBuffers)buffers;
			Blit(buffers2.diffusion, diff, overlap, new(4, 4));
			Blit(buffers2.normal, norm, overlap, new(4, 4));
			Blit(buffers2.specular, spec, overlap, new(4, 4));
		}

		if (DiscoAPISettings.DrawVirtualTextureBorders) DrawDebugBorders();
	}
}
