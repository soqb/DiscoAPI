using System.Drawing;

namespace DiscoAPI.Runtime.VirtualTextures;

public interface PageProvider
{
	public delegate PageProvider Factory(int mipCount, Rectangle area);

	Color GetDiffusionPixel(PageLocation page, int x, int y);
	Color GetNormalPixel(PageLocation page, int x, int y);
	Color GetSpecularPixel(PageLocation page, int x, int y);
}

public class BitmapPageProvider : PageProvider
{
	public bool didFail;
	public Bitmap[] diffusionMips;
	public Rectangle area;

	private Bitmap[] GenerateMipmaps(string path, int mipCount)
	{
		Bitmap fullQuality = new(path);
		var mipmaps = new Bitmap[mipCount];
		for (int mip = 0; mip < mipCount; mip++)
		{
			Size size = new(area.Width >> mip, area.Height >> mip);
			if (fullQuality.Size == size) mipmaps[mip] = fullQuality;
			else mipmaps[mip] = new(fullQuality, size);
		}

		return mipmaps;
	}

	private BitmapPageProvider(string path, int mipCount, Rectangle area)
	{
		this.area = area;
		try
		{
			diffusionMips = GenerateMipmaps(path, mipCount);
		}
		catch (System.Exception e)
		{
			didFail = true;
			diffusionMips = new Bitmap[0];
			DiscoRunner.Log.LogWarning($"vt loading failed: {e}");
		}
	}

	public static PageProvider.Factory FromFile(string path)
		=> (mipCount, area) => new BitmapPageProvider(path, mipCount, area);

	private Color GetFallbackDiffusionPixel(PageLocation page, int x, int y)
	{
		if ((page.x + page.y) % 2 == (x / 64 + y / 64) % 2) return Color.Green;
		else return Color.Purple;
	}

	public Color GetDiffusionPixel(PageLocation page, int x, int y)
	{
		if (didFail) return GetFallbackDiffusionPixel(page, x, y);

		Bitmap mipmap = diffusionMips[page.mip];
		int pageLeft = page.x * 128 - (area.X >> page.mip);
		int pageTop = page.y * 128 - (area.Y >> page.mip);
		x = pageLeft + x;
		y = pageTop + y;

		if (x >= 0 && y >= 0 && x < mipmap.Width && y < mipmap.Height) return mipmap.GetPixel(x, y);
		return GetFallbackDiffusionPixel(page, x, y);
	}
	public Color GetNormalPixel(PageLocation page, int x, int y) => Color.FromArgb(0, 0, 0, 255);
	public Color GetSpecularPixel(PageLocation page, int x, int y) => Color.White;
}
