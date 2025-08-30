using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using DiscoAPI.Runtime.Components;

namespace DiscoAPI.Runtime.VirtualTextures;

public struct VirtualTextureOverrides
{
    public List<PageSubstitution> substitutions = new();

    public VirtualTextureOverrides() { }
}

public struct AdHocTextureConfig(IPageProvider provider)
{
    public IPageProvider getPages = provider;
    public uint widthInPages;
    public uint mipCount;
}

public record struct PageLocation(int mip, int x, int y);
public record struct PageSubstitution(Rectangle area, IPageProvider pages);

public abstract class VirtualTextureCustomizer : IDisposable
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

    public abstract void Dispose();
    public abstract bool TrySubstitute(PageLocation page, [NotNullWhen(true)] out IPageProvider? provider, out Rectangle overlap);

    public bool TrySubstituteUncompressed(
        PageLocation page,
        [NotNullWhen(true)] out PageBuffers? buffers,
        out Rectangle overlap
    )
    {
        if (!TrySubstitute(page, out var provider, out overlap) || !provider.CanProvideUncompressed)
        {
            buffers = null;
            return false;
        }

        buffers = provider.ProvideUncompressed(page);
        return true;
    }

    public virtual bool TrySubstituteCompressed(
        PageLocation page,
        [NotNullWhen(true)] out ICompressedPageReader? reader,
        out Rectangle overlap
    )
    {
        if (!TrySubstitute(page, out var provider, out overlap))
        {
            reader = null;
            return false;
        }

        reader = provider.ProvideCompressed(page);
        return true;
    }
}

public class AdHocVirtualTextureCustomizer : VirtualTextureCustomizer
{
    private IPageProvider pages;

    public AdHocVirtualTextureCustomizer(VirtualTexture asset, AdHocTextureConfig config) : base(asset)
    {
        pages = config.getPages;
    }

    public override void Dispose() => pages.Dispose();

    public override bool TrySubstitute(PageLocation page, [NotNullWhen(true)] out IPageProvider? provider, out Rectangle overlap)
    {
        provider = pages;
        overlap = new(0, 0, 136, 136);
        return true;
    }
}

public class OverridesVirtualTextureCustomizer : VirtualTextureCustomizer
{
    private PageSubstitution[] substs;

    public OverridesVirtualTextureCustomizer(VirtualTexture asset, VirtualTextureOverrides settings) : base(asset)
    {
        substs = settings.substitutions.ToArray();
    }

    public override void Dispose()
    {
        foreach (var sub in substs)
        {
            sub.pages.Dispose();
        }
    }

    public override bool TrySubstitute(PageLocation page, [NotNullWhen(true)] out IPageProvider? provider, out Rectangle overlap)
    {
        foreach (var sub in substs)
        {
            Rectangle subArea = new(
                (sub.area.X >> page.mip) - page.x * 128 + 4,
                (sub.area.Y >> page.mip) - page.y * 128 + 4,
                sub.area.Width >> page.mip,
                sub.area.Height >> page.mip
            );
            Rectangle pageArea = new(
                0,
                0,
                136,
                136
            );

            overlap = Rectangle.Intersect(subArea, pageArea);
            if (overlap.Width != 0 && overlap.Height != 0)
            {
                provider = sub.pages;
                return true;
            }
        }

        provider = null;
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

    public static int WidthInPages(this ModEntity<VirtualTexture> vt) => (int)(vt.EntityBase.m_virtualSize) / 128;
}
