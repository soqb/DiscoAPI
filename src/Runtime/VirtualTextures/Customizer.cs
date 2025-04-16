using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;

namespace DiscoAPI.Runtime.VirtualTextures;

public struct VirtualTextureOverrides
{
	public List<PageSubstitution> substitutions = new();

	public VirtualTextureOverrides() { }
}

public struct AdHocTextureConfig
{
	public PageProvider.Factory getPages;
	public Size size;

	public AdHocTextureConfig(PageProvider.Factory providerFactory, Size size)
	{
		this.getPages = providerFactory;
		this.size = size;
	}
}

public record struct PageLocation(int mip, int x, int y);
public record struct PageSubstitution(Rectangle area, PageProvider.Factory pagesFactory);

public interface IPageBlitter
{
	public bool TryGetPageSubstitute(PageLocation page);
}

public abstract class VirtualTextureCustomizer
{
	public VirtualTexture asset;

	protected VirtualTextureCustomizer(VirtualTexture asset)
	{
		this.asset = asset;
	}

	public PageLocation InvertPageId(int index)
	{
		int pagesSeen = 0;
		for (int mip = 0; mip < asset.m_mipCount; mip++)
		{
			int pagesPerRow = asset.m_physicalTableSize >> mip;
			int nextPagesSeen = pagesSeen + pagesPerRow * pagesPerRow;
			if (index >= nextPagesSeen)
			{
				pagesSeen = nextPagesSeen;
				continue;
			};
			int xy = index - pagesSeen;
			return new(mip, xy % pagesPerRow, xy / pagesPerRow);
		}

		throw new Exception("unknown page id !");
	}

	public abstract bool TrySubstitute(PageLocation page, [NotNullWhen(true)] out PageProvider? pages, out Rectangle overlap);
}

public class AdHocVirtualTextureCustomizer : VirtualTextureCustomizer
{
	private Rectangle area;
	private PageProvider pages;

	public AdHocVirtualTextureCustomizer(VirtualTexture asset, AdHocTextureConfig config) : base(asset)
	{
		area = new(0, 0, config.size.Width, config.size.Height);
		pages = config.getPages(asset, area);
	}

	public override bool TrySubstitute(PageLocation page, [NotNullWhen(true)] out PageProvider? pages, out Rectangle overlap)
	{
		pages = this.pages;
		overlap = area;
		return true;
	}
}

public class OverridesVirtualTextureCustomizer : VirtualTextureCustomizer
{
	private record struct PageSubstitutionInstance(Rectangle area, PageProvider pages);
	private PageSubstitutionInstance[] substs;

	public OverridesVirtualTextureCustomizer(VirtualTexture asset, VirtualTextureOverrides settings) : base(asset)
	{
		substs = settings.substitutions.Select(subst => new PageSubstitutionInstance(
			subst.area,
			subst.pagesFactory.Invoke(asset, subst.area)
		)).ToArray();
	}

	public override bool TrySubstitute(PageLocation page, [NotNullWhen(true)] out PageProvider? pages, out Rectangle overlap)
	{
		for (int i = substs.Length - 1; i >= 0; i--)
		{
			var inst = substs[i];
			Rectangle area = new(
				inst.area.X >> page.mip,
				inst.area.Y >> page.mip,
				inst.area.Width >> page.mip,
				inst.area.Height >> page.mip
			);
			if (!area.Contains(page.x * 136, page.y * 136)) continue;

			pages = inst.pages;
			overlap = Rectangle.FromLTRB(
				Math.Max(0, area.X - page.x * 136),
				Math.Max(0, area.Y - page.y * 136),
				Math.Min(136, area.X - page.x * 136 + area.Width),
				Math.Min(136, area.Y - page.y * 136 + area.Height)
			);
			return true;
		}

		pages = null;
		overlap = default;
		return false;
	}
}
