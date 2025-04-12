using System;
using HarmonyLib;

namespace Better_Loving.Mian.Patches;

public class ActorPatch
{
       // gives asexual (reproduction method i mean) units the ability to find love!
    [HarmonyPatch(typeof(Actor), nameof(Actor.create))]
    class ActorCreatePatch
    {
        static void Postfix(Actor __instance)
        {
            __instance.asset.addDecision("find_lover");
            __instance.data.set("sexual_happiness", 10f);
        }
    }

    [HarmonyPatch(typeof(Actor), nameof(Actor.updateAge))]
    class CalcAgeStatesPatch
    {
        static void Postfix(Actor __instance)
        {
            if (__instance.isAdult()) // fluid sexuality
            {
                if (!QueerTraits.HasQueerTraits(__instance)){
                    QueerTraits.GiveQueerTraits(__instance, false, true);
                    __instance.changeHappiness("true_self");
                }
                else
                {
                    bool changed = false;
                    var list = QueerTraits.GetQueerTraits(__instance);
                    list = QueerTraits.RandomizeQueerTraits(__instance, true, list);
                    if (__instance.hasTrait("abroromantic") && Randy.randomChance(0.1f))
                    {
                        QueerTraits.CleanQueerTraits(__instance, false);
                        __instance.addTrait(list[1]);
                        changed = true;
                    }
                    if (__instance.hasTrait("abrosexual") && Randy.randomChance(0.1f))
                    {
                        QueerTraits.CleanQueerTraits(__instance, true);
                        __instance.addTrait(list[0]);
                        changed = true;
                    }
                    if(changed)
                        __instance.changeHappiness("true_self");
                }
                if(QueerTraits.GetPreferenceFromActor(__instance, true) != Preference.Neither && Util.IsOrientationSystemEnabledFor(__instance))
                    Util.ChangeSexualHappinessBy(__instance.a, -Randy.randomFloat(5, 10f));
                else
                    __instance.data.set("sexual_happiness", 100f);
            } else if (!__instance.isAdult() && Randy.randomChance(0.1f)) // random chance younger kid finds their orientations
            {
                QueerTraits.GiveQueerTraits(__instance, false, true);
                __instance.changeHappiness("true_self");
            }
            
            // Randomize breaking up (1% if preferences match. 25% if preferences do not match.) 
            
            // break up is too common rn, let's implement a system in the future to get lovers back together
            // if (Util.IsOrientationSystemEnabledFor(__instance) && __instance.hasLover() &&
            //     Randy.randomChance(
            //         // not needed since normal sex can happen now
            //         // Util.IsDyingOut(__instance) && !Util.CanReproduce(__instance, __instance.lover) ? 0.5f : 
            //         !QueerTraits.PreferenceMatches(__instance, __instance.lover, false) ? 0.25f : 0.01f))
            // {
            //     if (!__instance.hasCultureTrait("committed") || !__instance.lover.hasCultureTrait("committed"))
            //     {
            //         Util.BreakUp(__instance);   
            //     }
            // }
        }
}

    [HarmonyPatch(typeof(Actor), nameof(Actor.becomeLoversWith))]
    class BecomeLoversWithPatch
    {
        static void Postfix(Actor pTarget, Actor __instance)
        {
            __instance.setFamily(null);
            pTarget.setFamily(null);
            
            BehaviourActionBase<Actor>.world.families.newFamily(__instance, __instance.current_tile, pTarget);
        }
    }
    
    // This is where we handle the beef of our code for having cross species and non-same reproduction method ppl fall in love
    [HarmonyPatch(typeof(Actor), nameof(Actor.canFallInLoveWith))]
    class CanFallInLoveWithPatch
    {
        private static bool WithinOfAge(Actor pActor, Actor pTarget)
        { 
            int higherAge = Math.Max(pActor.age, pTarget.age);
            int lowerAge = Math.Min(pActor.age, pTarget.age);
            int minimumAge = higherAge / 2 + 7;
            return lowerAge >= minimumAge || (!pActor.hasCultureTrait("mature_dating") && !pTarget.hasCultureTrait("mature_dating"));
        }
        static bool Prefix(Actor pTarget, ref bool __result, Actor __instance)
        {
            // LogService.LogInfo($"Can {__instance.getName()} fall in love with {pTarget.getName()}?");
            var config = TopicOfLoving.Mod.GetConfig();
            var allowCrossSpeciesLove = (bool)config["CrossSpecies"]["AllowCrossSpeciesLove"].GetValue();
            var mustBeSmart = (bool)config["CrossSpecies"]["MustBeSmart"].GetValue();
            var mustBeXenophile = (bool)config["CrossSpecies"]["MustBeXenophile"].GetValue();

            if (
                Util.OnceDated(pTarget, __instance)
                ||
                 (!QueerTraits.PreferenceMatches(__instance, pTarget, false) && Util.IsOrientationSystemEnabledFor(__instance))
                 || (!QueerTraits.PreferenceMatches(pTarget, __instance, false) && Util.IsOrientationSystemEnabledFor(pTarget))

                || __instance.hasLover()
                || pTarget.hasLover()
                // replaced by the newer sex cheating system (we'll probably do a flirting task in the future for romance?)

                // || (__instance.hasLover() && (!Randy.randomChance(
                //         (pTarget.hasTrait("unfaithful") ? 0.1f : 0f) 
                //         + (QueerTraits.PreferenceMatches(__instance, __instance.lover, false) ? 0.005f: 0.1f))
                //                               || __instance.hasTrait("faithful") || __instance.hasCultureTrait("committed"))) 
                // || (pTarget.hasLover() && (!Randy.randomChance(
                //                                (pTarget.hasTrait("unfaithful") ? 0.1f : 0f) 
                //                                + (QueerTraits.PreferenceMatches(__instance, __instance.lover, false) ? 0.005f: 0.1f))
                //                            || pTarget.hasTrait("faithful") || pTarget.hasCultureTrait("committed")))

                // make this a cultural trait to configure
                || !WithinOfAge(__instance, pTarget)

                || __instance.areFoes(pTarget)

                || (!(__instance.isSameSpecies(pTarget) || __instance.isSameSubspecies(pTarget.subspecies))
                                                       && !((__instance.hasXenophiles() || !mustBeXenophile)
                                                             && (Util.IsSmart(__instance) && Util.IsSmart(pTarget) || !mustBeSmart)
                                                             && !pTarget.hasXenophobic() || !allowCrossSpeciesLove)) // subspecies stuff!

                // if queer but culture trait says they do not matter
                || ((!Util.IsOrientationSystemEnabledFor(__instance) || !Util.IsOrientationSystemEnabledFor(pTarget))
                    && !Util.CanReproduce(__instance, pTarget)))
            {
                __result = false;
                return false;
            }

            if (__instance.isRelatedTo(pTarget) && (!Util.CanCommitIncest(__instance) || !Util.CanCommitIncest(pTarget)))
            {
                __result = false;
                return false;
            }

            if (((__instance.hasClan() && __instance.clan.hasTrait(Util.clanboundIsolation) && !pTarget.hasClan())
                || (pTarget.hasClan() && pTarget.clan.hasTrait(Util.clanboundIsolation) && !__instance.hasClan()))
                && !Util.IsDyingOut(pTarget) && !Util.IsDyingOut(__instance))
            {
                __result = false;
                return false;
            }
            __result = true;

            // LogService.LogInfo($"Success! They in love :D");
            return false;
        }
    }
}