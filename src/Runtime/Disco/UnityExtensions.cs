using DiscoAPI.Common.Dialogue;

namespace DiscoAPI.Runtime;

public static class UnityExtensions
{
    public static Sunshine.Metric.SkillType Sunshine(this SkillType skill) => (Sunshine.Metric.SkillType)skill;
    public static Sunshine.Metric.Difficulty Sunshine(this Difficulty diff) => (Sunshine.Metric.Difficulty)diff;

    public static int CrushedId(this AssetRef ass, DiscoManager? mgr = null)
    {
        DiscoSource source = (mgr ?? DiscoRunner.manager).GetSource(ass.sourceGuid);
        int answer = source.Dialogue.CrushedId(ass.Type, ass.id);
        return answer;
    }
}
