using System;
using System.Drawing;

namespace DiscoAPI.Runtime.VirtualTextures;

public abstract class PageProvider
{
	public delegate PageProvider Factory(VirtualTexture asset, Rectangle area);

	protected VirtualTexture asset;
	protected Rectangle area;

	public abstract Color GetDiffusionPixel(PageLocation page, int x, int y);
	public abstract Color GetNormalPixel(PageLocation page, int x, int y);
	public abstract Color GetSpecularPixel(PageLocation page, int x, int y);

	protected PageProvider(VirtualTexture asset, Rectangle area)
	{
		this.asset = asset;
		this.area = area;
	}
}

public class BitmapPageProvider : PageProvider
{
	public bool didFail;
	public Bitmap[] diffusionMips;

	private Bitmap[] GenerateMipmaps(string path)
	{
		Bitmap fullQuality = new(path);
		var mipmaps = new Bitmap[asset.m_mipCount];
		for (int mip = 0; mip < asset.m_mipCount; mip++)
		{
			Size size = new(area.Width >> mip, area.Height >> mip);
			if (fullQuality.Size == size) mipmaps[mip] = fullQuality;
			else mipmaps[mip] = new(fullQuality, size);
		}

		return mipmaps;
	}

	private BitmapPageProvider(string path, VirtualTexture asset, Rectangle area) : base(asset, area)
	{
		try
		{
			diffusionMips = GenerateMipmaps(path);
		}
		catch (System.Exception e)
		{
			didFail = true;
			diffusionMips = new Bitmap[0];
			DiscoRunner.Log.LogWarning($"vt loading failed: {e}");
		}
	}

	public static PageProvider.Factory FromFile(string path)
		=> (asset, area) => new BitmapPageProvider(path, asset, area);

	public override Color GetDiffusionPixel(PageLocation page, int x, int y)
	{
		// DiscoRunner.Log.LogInfo($"* ({x}, {y}) and this is page {page.mip}#({page.x}, {page.y})");
		// if (didFail)
		// {
		if (x < 34 || x >= 102 || y < 34 || y >= 102) return Color.Red;
		// if ((page.x + page.y) % 2 == 0) return Color.FromArgb(255, Math.Min(255, page.x * 4), 0, Math.Min(255, page.y * 4));
		// else return Color.Purple;
		// }

		var mipmap = diffusionMips[page.mip];
		int pageLeft = page.x * 136 - (area.X >> page.mip);
		int pageTop = page.y * 136 - (area.Y >> page.mip);
		x = pageLeft + x;
		y = pageTop + y;
		if (x < mipmap.Width && y < mipmap.Height) return mipmap.GetPixel(x, y);
		else return Color.Purple;
	}
	public override Color GetNormalPixel(PageLocation page, int x, int y) => Color.FromArgb(0, 0, 0, 255);
	public override Color GetSpecularPixel(PageLocation page, int x, int y) => Color.White;
}
