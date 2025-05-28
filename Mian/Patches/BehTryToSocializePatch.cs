using ai;
using ai.behaviours;
using HarmonyLib;

namespace Topic_of_Love.Mian.Patches;

[HarmonyPatch(typeof(BehTryToSocialize))]
public class BehTryToSocializePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(BehTryToSocialize.execute))]
    public static bool SocializePatch(BehTryToSocialize __instance, Actor pActor, ref BehResult __result)
    {
        pActor.resetSocialize();
        Actor randomActorAround = __instance.getRandomActorAround(pActor);
        if (randomActorAround == null)
        {
            __result = BehResult.Stop;
            return false;
        }

        pActor.beh_actor_target = randomActorAround;

        if (TolUtil.IsOrientationSystemEnabledFor(pActor))
        {
            if (TolUtil.PossibleSocializePostTask(pActor, randomActorAround, __instance))
            {
                return false;
            }  
        }
        else
        {
            ActorTool.checkFallInLove(pActor, randomActorAround);
        }
        pActor.resetSocialize();
        randomActorAround.resetSocialize();
        
        __result = pActor.hasTelepathicLink() && randomActorAround.hasTelepathicLink() ? __instance.forceTask(pActor, "socialize_do_talk", false) : __instance.forceTask(pActor, "socialize_go_to_target", false);
        return false;
    }
}