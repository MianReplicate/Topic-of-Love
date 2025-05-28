using ai;
using HarmonyLib;

namespace Topic_of_Love.Mian.Patches;

[HarmonyPatch(typeof(BehFinishTalk))]
public class BehFinishTalkPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(BehFinishTalk.finishTalk))]
    static bool FinishTalkPatch(BehFinishTalk __instance, Actor pActor, Actor pTarget)
    {
        pActor.resetSocialize();
        pTarget.resetSocialize();
        int num1 = Randy.randomChance(0.7f) ? 1 : 0;
        int pValue = num1 == 0 ? -15 : 10;
        pActor.changeHappiness("just_talked", pValue);
        pTarget.changeHappiness("just_talked", pValue);
        pActor.addStatusEffect("recovery_social");
        pTarget.addStatusEffect("recovery_social");
        if (num1 != 0)
            ActorTool.checkBecomingBestFriends(pActor, pTarget);
        __instance.checkMetaSpread(pActor, pTarget);
        if (pActor.hasCulture() && pActor.culture.hasTrait("youth_reverence") && __instance.throwDiceForGift(pActor, pTarget) && pActor.isAdult() && pTarget.getAge() < pActor.getAge())
            __instance.makeGift(pActor, pTarget);
        if (pActor.hasCulture() && pActor.culture.hasTrait("elder_reverence") && __instance.throwDiceForGift(pActor, pTarget) && pActor.isAdult() && pTarget.getAge() > pActor.getAge())
            __instance.makeGift(pActor, pTarget);
        __instance.checkPassLearningAttributes(pActor, pTarget);
        if (num1 != 0 && Randy.randomChance(pActor.hasLover() ? 0.01f : pActor.getBestFriend() == pTarget ? 0.75f : 0.25f))
        {
            ActorTool.checkFallInLove(pActor, pTarget);
        }
        float num2 = Randy.randomFloat(1.1f, 3.3f);
        pActor.timer_action = num2;
        pTarget.timer_action = num2;
        return false;
    }
}