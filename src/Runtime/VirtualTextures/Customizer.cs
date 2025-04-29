using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using DiscoAPI.Runtime.Components;

namespace DiscoAPI.Runtime.VirtualTextures;

public struct VirtualTextureOverrides
{
	public List<PageSubstitution> substitutions = new();

	public VirtualTextureOverrides() { }
}

public struct AdHocTextureConfig
{
	public IPageProvider.Factory getPages;
	public Size size;

	public AdHocTextureConfig(Size size, IPageProvider.Factory providerFactory)
	{
		this.size = size;
		this.getPages = providerFactory;
	}
}

public record struct PageLocation(int mip, int x, int y);
public record struct PageSubstitution(Rectangle area, IPageProvider.Factory pagesFactory);

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

	public static PageLocation InvertPageId(VirtualTexture asset, int index)
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
			}
			;
			int xy = index - pagesSeen;
			return new(mip, xy % pagesPerRow, xy / pagesPerRow);
		}

		throw new Exception("unknown page id !");
	}

	public abstract bool TrySubstitute(PageLocation page, [NotNullWhen(true)] out PageBuffers? pages, out Rectangle overlap);
}

public class AdHocVirtualTextureCustomizer : VirtualTextureCustomizer
{
	private Rectangle area;
	private IPageProvider pages;

	public AdHocVirtualTextureCustomizer(VirtualTexture asset, AdHocTextureConfig config) : base(asset)
	{
		area = new(0, 0, config.size.Width, config.size.Height);
		pages = config.getPages(asset.m_mipCount, area);
	}

	public override bool TrySubstitute(PageLocation page, [NotNullWhen(true)] out PageBuffers? buffers, out Rectangle overlap)
	{
		buffers = pages.Provide(page);
		overlap = new(-4, -4, 136, 136);
		return true;
	}
}

public class OverridesVirtualTextureCustomizer : VirtualTextureCustomizer
{
	private record struct PageSubstitution(Rectangle area, IPageProvider pages);
	private PageSubstitution[] substs;

	public OverridesVirtualTextureCustomizer(VirtualTexture asset, VirtualTextureOverrides settings) : base(asset)
	{
		substs = settings.substitutions.Select(subst => new PageSubstitution(
			subst.area,
			subst.pagesFactory.Invoke(asset.m_mipCount, subst.area)
		)).ToArray();
	}

	public override bool TrySubstitute(PageLocation page, [NotNullWhen(true)] out PageBuffers? buffers, out Rectangle overlap)
	{
		foreach (var sub in substs)
		{
			Rectangle textureArea = new(
				(sub.area.X >> page.mip) - page.x * 128,
				(sub.area.Y >> page.mip) - page.y * 128,
				sub.area.Width >> page.mip,
				sub.area.Height >> page.mip
			);
			Rectangle pageArea = new(
				-4,
				-4,
				136,
				136
			);

			overlap = Rectangle.Intersect(textureArea, pageArea);
			if (overlap.Width != 0 && overlap.Height != 0)
			{
				buffers = sub.pages.Provide(page);
				return true;
			}

		}

		buffers = null;
		overlap = default;
		return false;
	}
}

public static class VirtualTextureComponents
{
	public static ModEntityRegistry<VirtualTexture> Registry { get; }
		= new(new PersistentEntityMap<VirtualTexture>(p => new(Registry!, p)));
	public static ModEntity<VirtualTexture> Of(VirtualTexture s) => Registry.EntityOf(s);

	// components ...
	public static ComponentKey<VirtualTextureCustomizer, VirtualTexture> Customizer { get; }
		= Registry.Register<VirtualTextureCustomizer>("discoapi", "customizer");

	// extension methods ...
	public static ModEntity<VirtualTexture> CreateAdHoc(AdHocTextureConfig config)
	{
		ModEntity<VirtualTexture>? me = null;

		ScriptableObjectHook<VirtualTexture>.CreateInstanceWith(asset =>
		{
			CustomVirtualTextureManager.InitializeAdHoc(asset, config);

			me = Of(asset);
			me.Add(Customizer, new AdHocVirtualTextureCustomizer(asset, config));
		});

		return me!;
	}
}
