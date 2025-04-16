using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;

namespace DiscoAPI.Runtime.VirtualTextures;

public struct VirtualTextureOverrides
{
	public List<PageSubstitution> substitutions = new();

	public VirtualTextureOverrides()
	{
	}
}

public record struct PageLocation(int mip, int x, int y);

public record struct PageSubstitution(Rectangle area, PageProvider.Factory pagesFactory);

public interface IPageBlitter
{
	public bool TryGetPageSubstitute(PageLocation page);
}

public class VirtualTextureCustomizer
{
	public VirtualTexture asset;

	private record struct PageSubstitutionInstance(Rectangle area, PageProvider pages);
	private PageSubstitutionInstance[] substs;

	public VirtualTextureCustomizer(VirtualTexture asset, VirtualTextureOverrides settings)
	{
		this.asset = asset;

		substs = settings.substitutions.Select(subst => new PageSubstitutionInstance(
			subst.area,
			subst.pagesFactory.Invoke(asset, subst.area)
		)).ToArray();
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

	public bool TrySubstitute(PageLocation page, [NotNullWhen(true)] out PageProvider? pages, out Rectangle overlap)
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
