using ai.behaviours;
using HarmonyLib;
using Topic_of_Love.Mian.CustomAssets.Custom;

namespace Topic_of_Love.Mian.Patches;

[HarmonyPatch]
public class BehSexualReproductionPatches
{
    private static bool CanReproduceTogether(Actor pActor, Actor pTarget)
    {
        return ((pActor.isSapient() && pTarget.isSapient()) || pTarget.isSameSpecies(pActor)) && pTarget.canBreed() && pTarget.canBreed() && !BabyHelper.isMetaLimitsReached(pTarget) && pActor.CanReproduce(pTarget) && (!pTarget.hasTask() || (pTarget.hasTask() && pTarget.ai.task.cancellable_by_reproduction));
    }
    private static Actor GetClosestPossibleMatchingActor(Actor pActor, bool sexualMatch=false)
    {
        Actor toReturn = null;
        
        foreach (var pTarget in Finder.getUnitsFromChunk(pActor.current_tile, 2))
        {
            if (pTarget != pActor && (!sexualMatch || pActor.AreCompatible(pTarget, true)) && CanReproduceTogether(pActor, pTarget) && pTarget.CanHaveIntimacyWithoutRepercussions(SexType.Reproduction))
            {
                toReturn = pTarget;
                break;
            }
        }

        return toReturn;
        
    }

    private static Actor GetActorForReproduction(Actor pActor)
    {
        Actor actorToDoSex = null;

        if (pActor.IsDyingOut())
        {
            if (pActor.hasLover() && CanReproduceTogether(pActor, pActor.lover))
            {
                actorToDoSex = pActor.lover;
            }
            else if (pActor.CanHaveIntimacyWithoutRepercussions(SexType.Reproduction))
            {
                actorToDoSex = GetClosestPossibleMatchingActor(pActor);
            }
        }
        else
        {
            if (pActor.hasLover() && pActor.AreCompatible(pActor.lover, true) && CanReproduceTogether(pActor, pActor.lover))
            {
                actorToDoSex = pActor.lover;
            }
            else if (!pActor.hasLover())
            {
                actorToDoSex = GetClosestPossibleMatchingActor(pActor, true);
            }
        }

        return actorToDoSex;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BehCheckSexualReproductionCiv), nameof(BehCheckSexualReproductionCiv.execute))]
    static bool CivPrefix(BehCheckSexualReproductionCiv __instance, Actor pActor, ref BehResult __result)
    {
        var actorToDoSex = GetActorForReproduction(pActor);
        if (pActor.isKingdomCiv() && (!pActor.hasHouse() || actorToDoSex == null))
        {
            __result = BehResult.Stop;
            return false;
        }
        
        actorToDoSex.setTask("have_sex_go", pCleanJob: true);
        actorToDoSex.timer_action = 0.0f;
        __result = __instance.forceTask(pActor, "have_sex_go");
        
        pActor.beh_actor_target = actorToDoSex;
        actorToDoSex.beh_actor_target = pActor;
        
        TolUtil.Debug($"{pActor.getName()} is reproducing with {actorToDoSex.getName()}: Lovers, {pActor.lover == actorToDoSex}");
        
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BehCheckSexualReproductionOutside), nameof(BehCheckSexualReproductionOutside.execute))]
    static bool OutsidePrefix(BehCheckSexualReproductionOutside __instance, Actor pActor, ref BehResult __result)
    {
        if (!pActor.canBreed())
        {
            __result = BehResult.Stop;
            return false;
        }

        var actorToDoSex = GetActorForReproduction(pActor);

        if (actorToDoSex == null)
        {
            __result = BehResult.Stop;
            return false;
        }

        TolUtil.Debug($"{pActor.getName()} is reproducing with {actorToDoSex.getName()}: Lovers, {pActor.lover == actorToDoSex}");

        if (__instance.tryStartBreeding(pActor, actorToDoSex)){
            __result = BehResult.RepeatStep;
            return false;
        }

        pActor.addAfterglowStatus();
        __result = BehResult.Stop;
        return false;
    }
}