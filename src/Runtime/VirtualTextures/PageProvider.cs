using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;

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
	public Bitmap[] diffusionMips;

	private BitmapPageProvider(string path, VirtualTexture asset, Rectangle area) : base(asset, area)
	{
		Bitmap fullQuality = new(path);
		diffusionMips = new Bitmap[asset.m_mipCount];
		for (int mip = 0; mip < asset.m_mipCount; mip++)
		{
			Size size = new(area.Width >> mip, area.Height >> mip);
			if (fullQuality.Size == size) diffusionMips[mip] = fullQuality;
			else diffusionMips[mip] = new(fullQuality, size);
		}
	}

	public static PageProvider.Factory FromFile(string path)
		=> (asset, area) => new BitmapPageProvider(path, asset, area);

	public override Color GetDiffusionPixel(PageLocation page, int x, int y)
	{
		var mipmap = diffusionMips[page.mip];
		int pageLeft = page.x * 136 - (area.X >> page.mip);
		int pageTop = page.y * 136 - (area.Y >> page.mip);
		x = pageLeft + x;
		y = pageTop + y;
		if (x < mipmap.Width && y < mipmap.Height) return mipmap.GetPixel(x, y);
		else return Color.Purple;
	}
	public override Color GetNormalPixel(PageLocation page, int x, int y) => Color.White;
	public override Color GetSpecularPixel(PageLocation page, int x, int y) => Color.White;
}
