
using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Runtime.Assets;
using PC = PixelCrushers.DialogueSystem;

namespace DiscoAPI.Runtime;

public static class UnityExtensions
{
    public static Sunshine.Metric.Difficulty Sunshine(this Difficulty diff) => (Sunshine.Metric.Difficulty)diff;

    public static T? Resolve<T>(this IAssetRef<T> asset) where T : Asset => asset.Resolve(DiscoRunner.manager);
    public static int ResolveId(this IAssetRef asset) => DiscoRunner.manager.Assets.ResolveId(asset.Location);

    public static PC.Actor? ResolveCrushed(this IAssetRef<Actor> asset, DiscoManager? mgr = null)
        => Crushed<PC.Actor, Actor>(asset, mgr);
    public static PC.Conversation? ResolveCrushed(this IAssetRef<Conversation> asset, DiscoManager? mgr = null)
        => Crushed<PC.Conversation, Conversation>(asset, mgr);
    public static PC.Variable? ResolveCrushed(this IAssetRef<Variable> asset, DiscoManager? mgr = null)
        => Crushed<PC.Variable, Variable>(asset, mgr);

    private static PCArena<T, U> GetArena<T, U>(DiscoManager mgr) where T : PC.Asset, new() where U : Asset
    {
        return (PCArena<T, U>)mgr.Assets.GetArena<U>();
    }

    private static T? Crushed<T, U>(IAssetRef<U> ass, DiscoManager? mgr) where T : PC.Asset, new() where U : Asset
    {
        mgr = mgr ?? DiscoRunner.manager;
        int id = mgr.Assets.ResolveId(ass.Location);
        var ana = GetArena<T, U>(mgr);
        return ana.GetRaw(id);
    }
}
