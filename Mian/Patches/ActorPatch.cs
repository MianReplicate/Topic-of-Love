using System;
using HarmonyLib;
using NeoModLoader.General;
using NeoModLoader.services;
using Topic_of_Love.Mian.CustomAssets.Custom;
using UnityEngine.Rendering;

namespace Topic_of_Love.Mian.Patches;

[HarmonyPatch(typeof(Actor))]
public class ActorPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Actor.removeTrait), typeof(ActorTrait))]
    static void ClearTraitPatch(ActorTrait pTrait, Actor __instance)
    {
        if (pTrait is PreferenceTrait preferenceTrait)
        {
            Orientations.CreateOrientationBasedOnPrefChange(__instance, preferenceTrait);
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Actor.addTrait), typeof(ActorTrait), typeof(bool))]
    static void AddTraitPatch(ActorTrait pTrait, Actor __instance)
    {
        // removes preference traits if they are all added for some reason 
        foreach (var type in Preferences.PreferenceTypes.Keys)
        {
            // Romantic
            
            var list = Preferences.GetActorPreferencesFromType(__instance, type);
            var toCompare = Preferences.GetPreferencesFromType(type);

            if (list.Count == toCompare.Count)
                __instance.removeTraits(list);
            
            // Sexual
            
            list = Preferences.GetActorPreferencesFromType(__instance, type, true);
            toCompare = Preferences.GetPreferencesFromType(type, true);

            if (list.Count == toCompare.Count)
                __instance.removeTraits(list);
        }

        if (pTrait is PreferenceTrait preferenceTrait)
        {
            Orientations.CreateOrientationBasedOnPrefChange(__instance, preferenceTrait);
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Actor.buildCityAndStartCivilization))]
    static void BuildCityAndStartCivPatch(Actor __instance)
    {
        if (__instance.hasLover() && (!__instance.lover.hasKingdom() || __instance.lover.kingdom.wild))
        {
            __instance.lover.setForcedKingdom(__instance.kingdom);
            __instance.lover.setCity(__instance.city);
            __instance.lover.setCulture(__instance.culture);
            __instance.kingdom.data.original_actor_asset = __instance.getActorAsset().id;
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Actor.getHit))]
    static void OnHitPatch(Actor __instance)
    {
        if (__instance.hasLover())
        {
            var lover = __instance.lover;
            if ((!lover.has_attack_target || (lover.has_attack_target && lover.attackedBy != lover.attack_target)) 
                && __instance.attackedBy != null && !lover.isLying()  && !lover.shouldIgnoreTarget(__instance.attackedBy)
                && lover.distanceToObjectTarget(__instance.attackedBy) < 40)
            {
                TolUtil.Debug(lover.getName() + "'s lover was attacked! They are going to defend them.");
                lover.startFightingWith(__instance.attackedBy);
            }
        }
    }
    
    [HarmonyPatch(typeof(Actor), nameof(Actor.create))]
    class ActorCreatePatch
    {
        static void Postfix(Actor __instance)
        {
            __instance.asset.addDecision("find_lover");
            __instance.data.set("intimacy_happiness", 10f);
        }
    }

    [HarmonyPatch(typeof(Actor), nameof(Actor.updateAge))]
    class CalcAgeStatesPatch
    {
        static void Postfix(Actor __instance)
        {
            // maybe we can reintroduce fluid preferences in the future?
            
            if (__instance.isAdult()) // fluid sexuality
            {
                if (!__instance.hasTrait("unfluid"))
                {
                    // preference for gender
                    if (Randy.randomChance(0.005f))
                    {
                        if (Randy.randomBool())
                        {
                            __instance.addTrait(Preferences.RandomPreferenceFromType("identity", Randy.randomBool()));
                        }
                        else
                        {
                            var preferences = Preferences.GetActorPreferencesFromType(__instance, "identity", Randy.randomBool());
                            if (preferences.Count > 0)
                            {
                                __instance.removeTrait(preferences.GetRandom());
                            }
                        }
                    }

                    if (TolUtil.IsTOIInstalled())
                    {
                        // preference for masculinity/femininity
                        if (Randy.randomChance(0.01f))
                        {
                            if (Randy.randomBool())
                            {
                                __instance.addTrait(Preferences.RandomPreferenceFromType("expression", Randy.randomBool()));
                            }
                            else
                            {
                                var preferences = Preferences.GetActorPreferencesFromType(__instance, "expression", Randy.randomBool());
                                if (preferences.Count > 0)
                                {
                                    __instance.removeTrait(preferences.GetRandom());
                                }
                            }
                        }
                
                        // preference for genitals
                        if (Randy.randomChance(0.005f))
                        {
                            if (Randy.randomBool())
                            {
                                __instance.addTrait(Preferences.RandomPreferenceFromType("genital", true));
                            }
                            else
                            {
                                var preferences = Preferences.GetActorPreferencesFromType(__instance, "genital", true);
                                if (preferences.Count > 0)
                                {
                                    __instance.removeTrait(preferences.GetRandom());
                                }
                            }
                        }   
                    }
                }
                
                // maybe rework so that aromantic/asexual ppl still experience intimacy happiness in some way?
                if(!Preferences.Dislikes(__instance, true) && TolUtil.IsOrientationSystemEnabledFor(__instance))
                    TolUtil.ChangeIntimacyHappinessBy(__instance.a, -Randy.randomFloat(1f, 7f));
                else
                    __instance.data.set("intimacy_happiness", 100f);
            } else if (!__instance.isAdult() && Randy.randomChance(0.1f) && !Preferences.HasAPreference(__instance))
            {
                TolUtil.NewPreferences(__instance);
                __instance.changeHappiness("true_self");
            }
            
            // List<Actor> undateables = DateableManager.Manager.GetUndateablesFor(__instance);
            // if (undateables != null)
            // {
            //     foreach (var actor in undateables)
            //     {
            //         if (Randy.randomChance(0.2f))
            //         {
            //             Util.Debug(__instance.getName() + " has forgived " + actor.getName());
            //             DateableManager.Manager.AddOrRemoveUndateable(__instance, actor); 
            //         }
            //     }   
            // }
            
            __instance.data.get("amount_undateable", out var length, 0);
            for(var i = 0; i < length; i++)
            {
                __instance.data.get("undateable_" + i, out var id, 0L);
                var actor = World.world.units.get(id);
                if (actor != null)
                {
                    if (Randy.randomChance(0.05f))
                    {
                        TolUtil.Debug(__instance.getName() + " has forgived " + actor.getName());
                        TolUtil.AddOrRemoveUndateableActor(__instance, actor); 
                    }   
                }
            }
            
            if (Randy.randomChance(0.05f))
            {
                __instance.data.set("just_lost_lover", false);
            } 

            if (__instance.hasLover())
            {
                var breakingUpChance = 0.005f;
                if (!Preferences.PreferenceMatches(__instance, __instance.lover, "identity", false))
                    breakingUpChance += 0.2f;
                if (!Preferences.PreferenceMatches(__instance, __instance.lover, "expression", false))
                    breakingUpChance += 0.05f;
                var wantsToBreakUp = Randy.randomChance(breakingUpChance);
                                
                if(TolUtil.CanStopBeingLovers(__instance) &&
                    ( (TolUtil.IsOrientationSystemEnabledFor(__instance) && wantsToBreakUp) 
                     || (!TolUtil.IsOrientationSystemEnabledFor(__instance) && !TolUtil.CouldReproduce(__instance, __instance.lover))))
                {
                    if (!__instance.hasCultureTrait("committed") || !__instance.lover.hasCultureTrait("committed"))
                    {
                        TolUtil.BreakUp(__instance);   
                    }   
                }
            }
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
            var config = TopicOfLove.Mod.GetConfig();
            var allowCrossSpeciesLove = (bool)config["CrossSpecies"]["AllowCrossSpeciesLove"].GetValue();
            var mustBeSmart = (bool)config["CrossSpecies"]["MustBeSmart"].GetValue();
            var mustBeXenophile = (bool)config["CrossSpecies"]["MustBeXenophile"].GetValue();
            
            if (
                // DateableManager.Manager.IsActorUndateable(pTarget, __instance)
                TolUtil.CannotDate(pTarget, __instance)
                ||
                 (!Preferences.PreferenceMatches(__instance, pTarget, false) && TolUtil.IsOrientationSystemEnabledFor(__instance))
                 || (!Preferences.PreferenceMatches(pTarget, __instance, false) && TolUtil.IsOrientationSystemEnabledFor(pTarget))
                
                // allows for custom loving and cheating if orientation system is enabled
                || (!TolUtil.IsOrientationSystemEnabledFor(__instance) && __instance.hasLover())
                || (!TolUtil.IsOrientationSystemEnabledFor(pTarget) && pTarget.hasLover())

                || !WithinOfAge(__instance, pTarget)
                
                || __instance.areFoes(pTarget)
                
                || (!(__instance.isSameSpecies(pTarget) || __instance.isSameSubspecies(pTarget.subspecies))
                                                       && !((__instance.hasXenophiles() || !mustBeXenophile)
                                                             && (__instance.isSapient() && pTarget.isSapient() || !mustBeSmart)
                                                             && !pTarget.hasXenophobic() || !allowCrossSpeciesLove)) // subspecies stuff!
                
                || !TolUtil.CanFallInLove(pTarget)
                || !TolUtil.CanFallInLove(__instance)
                
                // if queer but culture trait says they do not matter
                || ((!TolUtil.IsOrientationSystemEnabledFor(__instance) || !TolUtil.IsOrientationSystemEnabledFor(pTarget))
                    && !TolUtil.CouldReproduce(__instance, pTarget)))
            {
                __result = false;
                return false;
            }

            if (__instance.isRelatedTo(pTarget) && (!__instance.hasCultureTrait("incest") || !pTarget.hasCultureTrait("incest")))
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