using System;
using System.Collections.Generic;
using ai;
using ai.behaviours;
using NeoModLoader.services;
using Topic_of_Love.Mian.CustomAssets.AI.CustomBehaviors.sex;
using Topic_of_Love.Mian.CustomAssets.Custom;

#if TOPICOFIDENTITY
using Topic_of_Identity;
#endif

namespace Topic_of_Love.Mian
{
    public class TolUtil
    {
        public static void ShowWhisperTipWithTime(string pText, float time=6f)
        {
            string pText1 = LocalizedTextManager.getText(pText);
            if (Config.whisper_A != null)
                pText1 = pText1.Replace("$kingdom_A$", Config.whisper_A.name);
            if (Config.whisper_B != null)
                pText1 = pText1.Replace("$kingdom_B$", Config.whisper_B.name);
            WorldTip.showNow(pText1, false, "top", time);
        }
        
        // Returns the parent that has a population limit not REACHED yet
        public static Actor EnsurePopulationFromParent(List<Actor> parents)
        {
            var canMake = new List<Actor>();

            foreach (var parent in parents)
            {
                if (!parent.subspecies.hasReachedPopulationLimit())
                    canMake.Add(parent);
            }

            if (canMake.Count <= 0) return null;

            return canMake.GetRandom();
        }
        public static bool CouldReproduce(Actor pActor, Actor pTarget)
        {
            return pActor.subspecies.isPartnerSuitableForReproduction(pActor, pTarget);
        }

        // this method may be a bit confusing but it's to determine if actors can get pregnant based on their genitalia and if they have eggs
        public static bool IsAbleToBecomePregnant(Actor pActor)
        {
            if (IsTOIInstalled())
            {
                // TOI compatibility
            }
            
            if (NeedSameSexTypeForReproduction(pActor) || CanDoAnySexType(pActor))
                return true;
            if (NeedDifferentSexTypeForReproduction(pActor))
                return Preferences.HasVulva(pActor);
            return false;
        }

        public static bool IsIntimacyHappinessEnough(Actor actor, float happiness)
        {
            actor.data.get("intimacy_happiness", out float compare);
            return compare >= happiness;
        }
        
        public static void ChangeIntimacyHappinessBy(Actor actor, float happiness)
        {
            actor.data.get("intimacy_happiness", out float init);
            actor.data.set("intimacy_happiness", Math.Max(-100, Math.Min(happiness + init, 100)));
        }

        private static void OpinionOnSex(Actor actor1, Actor actor2)
        {
            if (actor1.hasSubspeciesTrait("amygdala"))
            {
                actor1.data.get("sex_reason", out var sexReason, "");
                
                // bug spotted? some actors were lovers but one of them disliked the sex for some reason
                if ((Preferences.PreferenceMatches(actor1, actor2, true) || (actor1.lover == actor2 && Randy.randomChance(0.5f)))
                    && (Randy.randomChance(sexReason.Equals("reproduction") ? 0.5f : 1f) || actor1.lover == actor2))
                {
                    var normal = 0.3f;
                    if (actor1.lover == actor2)
                        normal += 0.5f;

                    actor1.a.data.get("intimacy_happiness", out float happiness);
                    if (happiness < 0)
                    {
                        normal += Math.Abs((happiness / 100) / 2);
                    }

                    if (!Preferences.PreferenceMatches(actor1, actor2, true))
                        normal -= 0.2f;
                    
                    var type = Randy.randomChance(Math.Min(1, normal)) ? "enjoyed_sex" : "okay_sex"; 
                    actor1.addStatusEffect(type);
                }
                else
                    actor1.addStatusEffect("disliked_sex");   
            }
        }

        public static bool IsFaithful(Actor pActor)
        {
            return pActor.hasCultureTrait("committed") || pActor.hasTrait("faithful");
        }

        public static bool WillDoIntimacy(Actor pActor, string sexReason=null, bool withLover=true, bool isInit=false)
        {
            pActor.data.get("intimacy_happiness", out float d);
            if (isInit)
                Debug(pActor.getName() + " is requesting to do intimacy. Sexual happiness: "+d + ". With lover: "+withLover);
            else
                Debug(pActor.getName() + " is being requested to do intimacy. Sexual happiness: "+d + ". With lover: "+withLover);
            if(sexReason != null)
                Debug("\n"+sexReason);

            if (sexReason == null && !pActor.isAdult())
                return false;
            
            if (!isInit)
            {
                if(pActor.hasTask() && !(pActor.ai.task.cancellable_by_reproduction ||
                                         pActor.ai.task.cancellable_by_socialize))
                {
                    Debug("Unable to do intimacy from this actor due to an uncancellable task");
                    return false;
                }
            }
            
            var allowedToHaveIntimacy = withLover || (sexReason != null ? CanHaveSexWithoutRepercussionsWithSomeoneElse(pActor, sexReason) : CanHaveRomanceWithoutRepercussionsWithSomeoneElse(pActor));
            var reduceChances = 0f;
            pActor.data.get("intimacy_happiness", out float intimacyHappiness);
            
            if (intimacyHappiness < 0)
            {
                var toReduce = intimacyHappiness / 500;
                reduceChances += toReduce;
            }

            if (pActor.hasTrait("sex_indifferent") && sexReason != null)
                reduceChances = 0f;
            
            reduceChances = Math.Max(-0.2f, reduceChances);

            if(!allowedToHaveIntimacy
               && Randy.randomChance(Math.Max(0, (pActor.hasTrait("unfaithful") ? 0.6f : 1f) + reduceChances)))
            {
                Debug("Not allowed to do intimacy because of lover and not low enough happiness");
                return false;
            }

            if (!allowedToHaveIntimacy && IsFaithful(pActor))
            {
                Debug("Not allowed to do intimacy because of lover and is faithful");
                return false;
            }
            
            reduceChances = 0.1f;
            if (intimacyHappiness > 0)
            {
                reduceChances += intimacyHappiness / 100f;
            }
            
            // person may choose to do sex even if really happy
            var doIntimacy = Randy.randomChance(Math.Max(0.05f, 1f - reduceChances));
            if (!doIntimacy && (sexReason == null || !sexReason.Equals("reproduction")))
            {
                Debug("Will not do intimacy since they are deemed to be happy enough");
                return false;
            }   

            if(!allowedToHaveIntimacy)
                Debug(pActor.getName() + " is cheating!");
            return true;
        }

        public static void ActorsInteractedIntimately(Actor actor1, Actor actor2)
        {
            actor1.data.set("last_had_intimate_interaction_with", actor2.getID());
            actor2.data.set("last_had_intimate_interaction_with", actor1.getID());
        }
        
        // handles the actors' happiness and mood towards their sex depending on their preferences
        // handle cheating here too
        public static void JustHadSex(Actor actor1, Actor actor2)
        {
            TolUtil.Debug(actor1.getName() + " had sex with "+actor2.getName()+". They are lovers: "+(actor1.lover==actor2));
            ActorsInteractedIntimately(actor1, actor2);
            
            actor1.addAfterglowStatus();
            actor2.addAfterglowStatus();   
            
            if (Randy.randomChance(actor1.lover == actor2 ? 1f : Preferences.BothPreferencesMatch(actor1, actor2, true) ? 0.25f : 0f))
            {
                actor1.addStatusEffect("just_kissed");
                actor2.addStatusEffect("just_kissed");
            }
            
            OpinionOnSex(actor1, actor2);
            OpinionOnSex(actor2, actor1);

            if (actor1.lover != actor2)
            {
                actor1.data.get("sex_reason", out var sexReason, "reproduction");
                TolUtil.Debug("Sex Reason: "+sexReason);
                if (!CanHaveSexWithoutRepercussionsWithSomeoneElse(actor1, sexReason))
                {
                    PotentiallyCheatedWith(actor1, actor2);
                }

                if (!CanHaveSexWithoutRepercussionsWithSomeoneElse(actor2, sexReason))
                {
                    PotentiallyCheatedWith(actor2, actor1);
                }   
            }

            if (actor1.hasLover() && actor1.lover != actor2)
            {
                ChangeIntimacyHappinessBy(actor1.lover, -25f);
            }

            if (actor2.hasLover() && actor2.lover != actor1)
            {
                ChangeIntimacyHappinessBy(actor2.lover, -25f);
            }
        }
        public static void NewPreferences(Actor actor)
        {
            if (actor != null)
            {
                var oldPreferences = Preferences.GetActorPreferences(actor);
                oldPreferences.AddRange(Preferences.GetActorPreferences(actor, true));
                actor.removeTraits(oldPreferences);
                
                var preferences =  Preferences.GetRandomPreferences(actor);
                foreach (var trait in preferences)
                {
                    actor.addTrait(trait);
                }
                Orientations.LabelOrientations(actor);   
            }
        }
        public static bool CanHaveSexWithoutRepercussionsWithSomeoneElse(Actor actor, string sexReason)
        {
            return !actor.hasLover()
                   || (actor.hasLover() && ((!Preferences.PreferenceMatches(actor, actor.lover, true)
                                                              && actor.lover.hasCultureTrait("sexual_expectations"))
                                                              || (actor.hasSubspeciesTrait("preservation") && IsDyingOut(actor) 
                                                                  && sexReason.Equals("reproduction")
                                                                  && (!BabyHelper.canMakeBabies(actor.lover) || !CouldReproduce(actor, actor.lover)))));
        }
        
        public static bool CanHaveRomanceWithoutRepercussionsWithSomeoneElse(Actor actor)
        {
            return !actor.hasLover()
                   || (actor.hasLover() && !Preferences.SAndRPreferencesMatch(actor, actor.lover)
                                             && actor.lover.hasCultureTrait("sexual_expectations"));
        }


        public static void PotentiallyCheatedWith(Actor actor, Actor actor2)
        {
            if (actor.hasLover() && actor.lover != actor2 && CanStopBeingLovers(actor))
            {
                var cheatedActor = actor.lover;
                // will they know :O
                // if (cheatedActor.isLying() || !cheatedActor.isOnSameIsland(actor))
                    // return;
                
                // HandleFamilyRemoval(actor);

                cheatedActor.addStatusEffect("cheated_on");
            }
        }

        public static bool CannotDate(Actor actor, Actor actor2)
        {
            return IsActorUndateable(actor, actor2) || IsActorUndateable(actor2, actor);
        }

        public static void BreakUp(Actor actor, bool actorIsSad=true)
        {
            if (!actor.hasLover())
                return;
            
            Debug(actor.getName() + " broke up with "+ actor.lover.getName());
            
            // HandleFamilyRemoval(actor);
            
            // DateableManager.Manager.AddOrRemoveUndateable(actor, actor.lover);
            // DateableManager.Manager.AddOrRemoveUndateable(actor.lover, actor);
            
            AddOrRemoveUndateableActor(actor, actor.lover);
            AddOrRemoveUndateableActor(actor.lover, actor);
            
            actor.lover.changeHappiness("breakup");
            if(actorIsSad)
                actor.changeHappiness("breakup");
            
            RemoveLovers(actor);
        }

        public static bool CanStopBeingLovers(Actor actor)
        {
            actor.data.get("force_lover", out var isForced, false);
            return !isForced;
        }

        public static bool CanFallInLove(Actor actor)
        {
            actor.data.get("just_lost_lover", out var justLostLover, false);
            actor.data.get("force_lover", out var isForced, false);
            return !justLostLover && !isForced;
        }

        public static void RemoveLovers(Actor actor)
        {
            var lover = actor.lover;
            if (lover == null)
                return;
            lover.setLover(null);
            actor.setLover(null);
            actor.data.set("just_lost_lover", true);
            lover.data.set("just_lost_lover", true);
            lover.data.set("force_lover", false);
            actor.data.set("force_lover", false);
        }
        
        public static bool IsOrientationSystemEnabledFor(Actor pActor)
        {
            return !pActor.hasCultureTrait("orientationless");
        }

        // public static bool CanMakeBabies(Actor pActor)
        // {
        //     
        //     return pActor.canBreed() &&
        //            pActor.isAdult() && !pActor.hasReachedOffspringLimit() &&
        //            !pActor.subspecies.hasReachedPopulationLimit() && (!pActor.hasCity() || !pActor.city.hasReachedWorldLawLimit()
        //     && ((pActor.subspecies.isReproductionSexual() || pActor.subspecies.hasTraitReproductionSexualHermaphroditic() 
        //                                                   || pActor.hasSubspeciesTrait("reproduction_same_sex"))
        //     && pActor.current_children_count == 0 || pActor.city.hasFreeHouseSlots()));
        // }

        public static bool IsDyingOut(Actor pActor)
        {
            if (!pActor.hasSubspecies()) return false;
            var limit = (int)pActor.subspecies.base_stats_meta["limit_population"];
            return pActor.subspecies.countCurrentFamilies() <= 10
                   || (limit != 0 ? pActor.subspecies.countUnits() <= limit / 3 : pActor.subspecies.countUnits() <= 50);
        }
        
        public static bool WantsBaby(Actor pActor, bool reproductionPurposesIncluded=true)
        {
            if (!BabyHelper.canMakeBabies(pActor))
                return false;
            
            if (reproductionPurposesIncluded)
            {
                if (!pActor.isSapient() || IsDyingOut(pActor))
                {
                    Debug(pActor.getName() + " wants a baby because they are non-intelligent species or are dying out");
                    return true;
                }   
            }
            
            if (pActor.hasHouse() && pActor.getHappiness() >= 75)
            {
                Debug(pActor.getName() + " wants a baby because they have a house and are happy enough");
                return true;
            }
            
            // Debug(pActor.getName() + " does not want a baby.");
            return false;
        }

        public static bool PossibleSocializePostTask(Actor pActor, Actor target, BehaviourActionActor __instance=null)
        {
            if (Randy.randomBool())
            {
                if (pActor.lover != target)
                {
                    if (pActor.canFallInLoveWith(target) 
                        && WillDoIntimacy(pActor, null, false, true)
                        && WillDoIntimacy(target, null, false))
                    {
                        // does date instead
                        if(__instance != null)
                            __instance.forceTask(pActor, "try_date", false);
                        else
                            pActor.setTask("try_date", false);
                        return true;
                    }   
                }
                else if(WillDoIntimacy(pActor, "casual", pActor.lover == target, true) 
                        && WillDoIntimacy(target, "casual", pActor.lover == target, false))
                {
                    pActor.cancelAllBeh();
                    target.cancelAllBeh();
                    pActor.beh_actor_target = target;
                    new BehGetPossibleTileForSex().execute(pActor);
                    return true;
                }   
            }

            return false;
        }

        public static bool IsActorUndateable(Actor pActor, Actor toCheck)
        {
            pActor.data.get("amount_undateable", out var length, 0);
            var id = toCheck.getID();
            for (var i = 0; i < length; i++)
            {
                pActor.data.get("undateable_" + i, out var idFromSave, 0L);
                if (id == idFromSave)
                    return true;
            }
        
            return false;
        }
        public static void AddOrRemoveUndateableActor(Actor pActor, Actor undateable)
        {
            pActor.data.get("amount_undateable", out var length, 0);
            
            var id = undateable.getID();
            var position = -1;
            
            for (var i = 0; i < length; i++)
            {
                pActor.data.get("undateable_" + i, out var idFromSave, 0L);
                if (idFromSave == id)
                {
                    position = i;
                    break;
                }
            }
        
            if (position == -1)
            {
                pActor.data.set("undateable_" + length, id);
                pActor.data.set("amount_undateable", length + 1);
            }
            else
            {
                pActor.data.removeLong("undateable_"+position);
                pActor.data.set("amount_undateable", length - 1);
                
                for (var i = position + 1; i < length; i++)
                {
                    pActor.data.get("undateable_" + i, out var idFromSave, 0L);
                    pActor.data.set("undateable_" + (i - 1), idFromSave);
                    pActor.data.removeLong("undateable_"+i);
                }
            }
        }

        public static void LogInfo(object message)
        {
            TopicOfLove.LogInfo(message.ToString());
        }
        
        public static void Debug(object message)
        {
            var config = TopicOfLove.Mod.GetConfig();
            var slowOnLog = (bool)config["Misc"]["SlowOnLog"].GetValue();
            var debug = (bool)config["Misc"]["Debug"].GetValue();

            if (!debug)
                return;
            if(slowOnLog)
                Config.setWorldSpeed(AssetManager.time_scales.get("slow_mo"));
            LogInfo(message);
        }
        
        public static bool NeedSameSexTypeForReproduction(Actor pActor)
        {
            return pActor.hasSubspeciesTrait("reproduction_same_sex");
        }
        public static bool CanDoAnySexType(Actor pActor)
        {
            return pActor.hasSubspeciesTrait("reproduction_hermaphroditic");
        }
        
        public static bool NeedDifferentSexTypeForReproduction(Actor pActor)
        {
            return pActor.hasSubspeciesTrait("reproduction_sexual");
        }

        public static bool IsTOIInstalled()
        {
            #if TOPICOFIDENTITY
            return true;
            #else
            return false;
            #endif
        }
    }
}