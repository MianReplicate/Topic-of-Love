using ai;
using HarmonyLib;

namespace Topic_of_Love.Mian.Patches;

[HarmonyPatch(typeof(ActorTool))]
public class ActorToolPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ActorTool.checkFallInLove))]
    public static bool FallInLovePatch(Actor pActor, Actor pTarget)
    {
        if (pActor.lover == pTarget.lover)
            return false;
        if (!pActor.canFallInLoveWith(pTarget))
            return false;
        if ((TolUtil.WillDoIntimacy(pActor, null, false) 
             || !TolUtil.IsOrientationSystemEnabledFor(pActor)) && (TolUtil.WillDoIntimacy(pTarget, null, false)
            || !TolUtil.IsOrientationSystemEnabledFor(pTarget)))
        {
            if(pActor.hasLover() && Randy.randomBool())
                TolUtil.BreakUp(pActor, false);
            if(pTarget.hasLover() && Randy.randomBool())
                TolUtil.BreakUp(pTarget, false);
            TolUtil.PossibleSocializePostTask(pActor, pTarget);
            return false;
        }

        if (!TolUtil.IsOrientationSystemEnabledFor(pActor) || !TolUtil.IsOrientationSystemEnabledFor(pTarget))
        {
            pActor.becomeLoversWith(pTarget);
        }
        return false;
    }
}