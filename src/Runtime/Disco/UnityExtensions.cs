using DiscoAPI.Common.Assets;

namespace DiscoAPI.Runtime;

public static class UnityExtensions
{
    public static Sunshine.Metric.Difficulty Sunshine(this Difficulty diff) => (Sunshine.Metric.Difficulty)diff;

    public static int ResolveId(this AssetRef ass, DiscoManager? mgr = null)
    {
        DiscoSource source = (mgr ?? DiscoRunner.manager).GetSource(ass.sourceGuid);
        int answer = source.Assets.ResolveId(ass.type, ass.id);
        return answer;
    }
}
