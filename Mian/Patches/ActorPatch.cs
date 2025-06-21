using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using EpPathFinding.cs;
using HarmonyLib;
using Topic_of_Love.Mian.CustomAssets;
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
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Actor.create))]
        static void ActorCreatePatch(Actor __instance)
        {
            __instance.asset.addDecision("find_lover");
            __instance.data.set("intimacy_happiness", 10f);
        }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Actor.updateStats))]
    static void UpdateStatsPatch(Actor __instance)
    {
        __instance.stats["intimacy_happiness"] = __instance.data["intimacy_happiness"];
    }
        
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Actor.updateAge))]
        static void CalcAgeStages(Actor __instance)
        {
            // maybe we can reintroduce fluid preferences in the future?
            
            if (__instance.isAdult()) // fluid sexuality
            {
                if (!__instance.hasTrait("unfluid") && MapBox.instance.world_laws.isEnabled("world_law_fluid_sexuality"))
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
                
                if(!Preferences.Dislikes(__instance, true) && TolUtil.IsOrientationSystemEnabledFor(__instance))
                    TolUtil.ChangeIntimacyHappinessBy(__instance.a, TolUtil.HasNoOne(__instance) ? -Randy.randomFloat(13f, 18f) : -Randy.randomFloat(7f, 12f));
                // else
                    // __instance.data.set("intimacy_happiness", 100f);
            } else if (!__instance.isAdult() && Randy.randomChance(0.1f) && !Preferences.HasAPreference(__instance))
            {
                TolUtil.NewPreferences(__instance);
                __instance.changeHappiness("true_self");
            }
            
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
            
            var intimacy = TolUtil.GetIntimacy(__instance) / 100;

            if (__instance.hasLover())
            {
                var breakingUpChance = 0.005f;

                if (TolUtil.AffectedByIntimacy(__instance))
                {
                    if (intimacy < 0)
                    {
                        breakingUpChance += Math.Abs(intimacy / 8);
                    }
                }
                
                // if (!Preferences.PreferenceMatches(__instance, __instance.lover, "identity", false))
                //     breakingUpChance += 0.2f;
                // if (!Preferences.PreferenceMatches(__instance, __instance.lover, "expression", false))
                //     breakingUpChance += 0.05f;
                var wantsToBreakUp = Randy.randomChance(breakingUpChance);
                                
                if(TolUtil.CanStopBeingLovers(__instance) &&
                    ( (TolUtil.IsOrientationSystemEnabledFor(__instance) && wantsToBreakUp) 
                     || (!TolUtil.IsOrientationSystemEnabledFor(__instance) && !TolUtil.CouldReproduce(__instance, __instance.lover))))
                {
                    if (!__instance.hasCultureTrait("committed") || !__instance.lover.hasCultureTrait("committed"))
                    {
                        TolUtil.BreakUp(__instance, false);   
                    }   
                }
            }

            if (intimacy < 0 && TolUtil.AffectedByIntimacy(__instance))
            {
                var feelsLonely = Randy.randomChance(Math.Abs(intimacy));
                if (feelsLonely && __instance._last_happiness_history.Count(asset => asset.index == Happiness.FeelsLonely.index) < 5)
                    __instance.changeHappiness("feels_lonely", (int) (intimacy * 25));
            }
        }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Actor.becomeLoversWith))]
    static void BecomeLoversWith(Actor pTarget, Actor __instance)
    {
        // if they become new lovers with someone, the others were cheated on
        TolUtil.PotentiallyCheatedWith(__instance, pTarget);
        TolUtil.PotentiallyCheatedWith(pTarget, __instance);
        
        TolUtil.Debug($"{__instance.getName()} fell in love {pTarget.getName()}!");
        
        // falling in love is fucking amazing cherish it mate
        TolUtil.ChangeIntimacyHappinessBy(pTarget, 100);
        TolUtil.ChangeIntimacyHappinessBy(__instance, 100);
    }
        
    // This is where we handle the beef of our code for having cross species and non-same reproduction method ppl fall in love
    // important to note this should check for both actors
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Actor.canFallInLoveWith))]
        static IEnumerable<CodeInstruction> CanFallInLoveWithPatch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var listOfMethodsToRemove = new List<MethodInfo>
            {
                AccessTools.Method(typeof(Actor), nameof(Actor.hasLover)),
                AccessTools.Method(typeof(Actor), nameof(Actor.isAdult)),
                AccessTools.Method(typeof(Actor), nameof(Actor.isBreedingAge)),
                AccessTools.PropertyGetter(typeof(Subspecies), nameof(Subspecies.needs_mate)),
                AccessTools.Method(typeof(Subspecies), nameof(Subspecies.isPartnerSuitableForReproduction), new []{typeof(Actor), typeof(Actor)})
            };
            
            var codeMatcher = new CodeMatcher(instructions, generator);

            var _instructions = codeMatcher.Instructions();
            for (var i = 0; i < _instructions.Count; i++)
            {
                var instruction = _instructions[i];
                var methodInfos = listOfMethodsToRemove.Where(method => instruction.Calls(method)).ToList();
                if (methodInfos.Any())
                {
                    var methodInfo = methodInfos.First();
                    
                    // var start = i - methodInfo.GetParameters().Length;
                    // start -= methodInfo.IsStatic ? 0 : 1;
                    // start -= methodInfo.IsSpecialName ? 1 : 0;
                    var start = 0;
                    
                    for (var _i = i; _i >= 0; _i--)
                    {
                        if (_instructions[_i].opcode == OpCodes.Ret)
                        {
                            start = _i + 1;
                            break;
                        }
                    }

                    var end = i + 3;
                    codeMatcher.RemoveInstructionsInRange(start, end);

                    i = 0; // reset for the sake of it
                }
            }

            codeMatcher = codeMatcher
                .Start()
                .MatchForward(false, new CodeMatch(OpCodes.Call,
                    AccessTools.Method(typeof(Actor), nameof(Actor.isSameSpecies), new[]{typeof(Actor)})))
                .ThrowIfInvalid("Could not find isSameSpecies! Did someone remove this... grrrrrr")
                .Advance(1);
            
            var outtaHere = (Label) codeMatcher.Instruction.operand; // snatch the label so that we can skip ahead to it later on
            
            codeMatcher.Advance(1); // go onto return false
            var returnFalse = generator.DefineLabel();
            codeMatcher.Instruction.WithLabels(returnFalse);
            
            var goOntoMoreChecks = generator.DefineLabel();
            codeMatcher = codeMatcher.InsertAndAdvance(new CodeInstruction(OpCodes.Br, goOntoMoreChecks));
            
            codeMatcher.Advance(2); // go past the return
            
            codeMatcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(goOntoMoreChecks), // load instance actor
                CodeInstruction.Call(typeof(Actor), nameof(Actor.isSapient)), // is actor sapient?
                new CodeInstruction(OpCodes.Brfalse, returnFalse)
            );

            codeMatcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_1),
                CodeInstruction.Call(typeof(Actor), nameof(Actor.isSapient)),
                new CodeInstruction(OpCodes.Brfalse, returnFalse)
            );

            codeMatcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(Actor), nameof(Actor.hasXenophobic)),
                new CodeInstruction(OpCodes.Brtrue, returnFalse)
            );
            
            codeMatcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_1),
                CodeInstruction.Call(typeof(Actor), nameof(Actor.hasXenophobic)),
                new CodeInstruction(OpCodes.Brtrue, returnFalse)
            );
            
            // codeMatcher.MatchEndForward(new CodeMatch(OpCodes.Call,
            //         AccessTools.Method(typeof(Actor),
            //             nameof(Actor
            //                 .isRelatedTo)))) // this is probably bad and we should do something else just in case someone removes this method
            //     .ThrowIfInvalid("Could not find isRelatedTo call??")
            //     .Advance(-3);
                
            // can they date each other
            codeMatcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(outtaHere),
                new CodeInstruction(OpCodes.Ldarg_1),
                CodeInstruction.Call(typeof(TolUtil), nameof(TolUtil.CannotDate)),
                new CodeInstruction(OpCodes.Brtrue, returnFalse)
                );
            
            // do they both have orientation system
            codeMatcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(TolUtil), nameof(TolUtil.IsOrientationSystemEnabledFor)),
                new CodeInstruction(OpCodes.Ldarg_1),
                CodeInstruction.Call(typeof(TolUtil), nameof(TolUtil.IsOrientationSystemEnabledFor)),
                new CodeInstruction(OpCodes.Ceq),
                new CodeInstruction(OpCodes.Brfalse, returnFalse)
            );

            var orientationSystemInvolved = generator.DeclareLocal(typeof(bool));
            var skipOrientationChecks = generator.DefineLabel();
            
            // has lover with orientation system checks
            codeMatcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(TolUtil), nameof(TolUtil.IsOrientationSystemEnabledFor)),
                new CodeInstruction(OpCodes.Stloc, orientationSystemInvolved.LocalIndex),
                new CodeInstruction(OpCodes.Ldloc, orientationSystemInvolved.LocalIndex),
                new CodeInstruction(OpCodes.Brtrue, skipOrientationChecks), // skip the next checks if true
                
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(Actor), nameof(Actor.hasLover)),
                new CodeInstruction(OpCodes.Brtrue, returnFalse),
                
                new CodeInstruction(OpCodes.Ldarg_1),
                CodeInstruction.Call(typeof(Actor), nameof(Actor.hasLover)),
                new CodeInstruction(OpCodes.Brtrue, returnFalse)
            );
            
            var reproductionBranch = generator.DefineLabel();
            var withinAgeBranch = generator.DefineLabel();
            var toFoes = generator.DefineLabel();

            // reproduction and within age
            codeMatcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc, orientationSystemInvolved.LocalIndex).WithLabels(skipOrientationChecks),
                new CodeInstruction(OpCodes.Brfalse, reproductionBranch),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                CodeInstruction.Call(typeof(Preferences), nameof(Preferences.BothActorsPreferenceMatch), new[]{typeof(Actor), typeof(Actor), typeof(bool)}),
                new CodeInstruction(OpCodes.Brfalse, returnFalse),
                
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(withinAgeBranch), // within age branch
                new CodeInstruction(OpCodes.Ldarg_1),
                CodeInstruction.Call(typeof(TolUtil), nameof(TolUtil.WithinOfAge)),
                new CodeInstruction(OpCodes.Brfalse, returnFalse),
                new CodeInstruction(OpCodes.Br, toFoes),
                
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(reproductionBranch),
                new CodeInstruction(OpCodes.Ldarg_1),
                CodeInstruction.Call(typeof(TolUtil), nameof(TolUtil.CouldReproduce)),
                new CodeInstruction(OpCodes.Brfalse, returnFalse),
                new CodeInstruction(OpCodes.Br, withinAgeBranch)
            );

            // if foes return false
            codeMatcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_1).WithLabels(toFoes), // target
                new CodeInstruction(OpCodes.Ldarg_0), // instance
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Actor), nameof(Actor.areFoes))),
                new CodeInstruction(OpCodes.Brtrue, returnFalse));

            // can they both fall in love at all?
            codeMatcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(TolUtil), nameof(TolUtil.CanFallInLove)),
                new CodeInstruction(OpCodes.Brfalse, returnFalse),

                new CodeInstruction(OpCodes.Ldarg_1),
                CodeInstruction.Call(typeof(TolUtil), nameof(TolUtil.CanFallInLove)),
                new CodeInstruction(OpCodes.Brfalse, returnFalse));
            
            return codeMatcher.InstructionEnumeration();
        }
}