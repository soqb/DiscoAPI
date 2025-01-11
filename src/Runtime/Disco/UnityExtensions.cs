
namespace DiscoAPI.Runtime;

public static class UnityExtensions
{
    public static Sunshine.Metric.Difficulty Sunshine(this Difficulty diff) => (Sunshine.Metric.Difficulty)diff;
}
