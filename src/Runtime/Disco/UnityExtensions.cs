using DiscoAPI.Common.Assets;

namespace DiscoAPI.Runtime;

public static class UnityExtensions
{
    public static Sunshine.Metric.Difficulty Sunshine(this Difficulty diff) => (Sunshine.Metric.Difficulty)diff;

    public static int ResolveId(this IAssetRef ass, DiscoManager? mgr = null)
    {
        var l = ass.Location;
        DiscoSource source = (mgr ?? DiscoRunner.manager).GetSource(l.source);
        return source.Assets.ResolveId(l.type, l.id);
    }
}
