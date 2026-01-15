using System.Linq;
using DiscoAPI.Runtime.Utils;
using HarmonyLib;

namespace DiscoAPI.Runtime.Patches;

internal static class ReputationPatches
{
    [HarmonyPatch(typeof(ReputationAlterant), nameof(ReputationAlterant.ReputationEffect))]
    [HarmonyPrefix]
    private static bool OnReputationEffect(Reputation rep)
    {
        var modRep = DiscoRunner.manager.Assets.GetArena<Common.Assets.Reputation>()[(int)rep];
        if (modRep == null || (int)rep < ReputationUtils.VANILLA_REP_DIALOGUE_COUNT) return true;
        
        modRep.repIncreaseEffect?.Invoke();
        return false;
    }

    [HarmonyPatch(typeof(ReputationAlterant), nameof(ReputationAlterant.ModifyIfDifferentFromZero))]
    [HarmonyPrefix]
    private static bool OnModifyReputation(string reputationName, int value)
    {
        var modRep = DiscoRunner.manager.Assets.GetArena<Common.Assets.Reputation>().FirstOrDefault(r => r.id == reputationName);
        
        ReputationAlterant.ModifyReputation(reputationName, value);
        if (modRep != null)
        {
            var repEnum = (Reputation)modRep.ResolveId(); 
            ReputationAlterant.ReputationEffect(repEnum);
            ReputationAlterant.ObsessionGiver(repEnum);
        }
        return false;
    }
}