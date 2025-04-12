using System;
using System.Collections.Generic;
using Better_Loving.Mian;
using NeoModLoader.services;

namespace Better_Loving
{
    public class Util
    {
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

        public static bool IsSmart(Actor actor)
        {
            return actor.hasSubspeciesTrait("prefrontal_cortex")
                   && actor.hasSubspeciesTrait("advanced_hippocampus")
                   && actor.hasSubspeciesTrait("amygdala")
                   && actor.hasSubspeciesTrait("wernicke_area");
        }

        public static void AddActorTrait(ActorTrait trait, List<string> actorAssets)
        {
            for (int index = 0; index < trait.rate_birth; ++index)
                AssetManager.traits.pot_traits_birth.Add(trait);
            for (int index = 0; index < trait.rate_acquire_grow_up; ++index)
                AssetManager.traits.pot_traits_growup.Add(trait);
            if (trait.combat)
                AssetManager.traits.pot_traits_combat.Add(trait);

            AssetManager.traits.add(trait);

            if (actorAssets != null)
                foreach (string asset in actorAssets)
                {
                    ActorAsset actorAsset = AssetManager.actor_library.get(asset);
                    if (actorAsset != null)
                        actorAsset.addTrait(trait.id);
                }
        }

        public static bool HasHadSexRecently(Actor actor)
        {
            return actor.hasStatus("enjoyed_sex") || actor.hasStatus("okay_sex") || actor.hasStatus("disliked_sex");
        }

    public static bool IsDyingOut(Actor pActor)
        {
            if (!pActor.hasSubspecies()) return false;
            int limit = (int)pActor.subspecies.base_stats_meta["limit_population"];
            return pActor.subspecies.countCurrentFamilies() <= 3
                   || (limit != 0 ? pActor.subspecies.countUnits() <= limit / 3 : pActor.subspecies.countUnits() <= 50);
        }

        public static bool CanReproduce(Actor pActor, Actor pTarget)
        {
            return pActor.subspecies.isPartnerSuitableForReproduction(pActor, pTarget);
        }

        public static bool IsSexualHappinessEnough(Actor actor, float happiness)
        {
            actor.data.get("sexual_happiness", out float compare);
            return compare >= happiness;
        }
        
        public static void ChangeSexualHappinessBy(Actor actor, float happiness)
        {
            actor.data.get("sexual_happiness", out float init);
            actor.data.set("sexual_happiness", Math.Max(-100, Math.Min(happiness + init, 100)));
        }

        private static void OpinionOnSex(Actor actor1, Actor actor2)
        {
            if (actor1.hasSubspeciesTrait("amygdala"))
            {
                actor1.data.get("sex_reason", out var sexReason, "");
                
                // bug spotted? some actors were lovers but one of them disliked the sex for some reason
                if ((QueerTraits.PreferenceMatches(actor1, actor2, true) || (actor1.lover == actor2 && Randy.randomChance(0.5f)))
                    && (Randy.randomChance(sexReason.Equals("reproduction") ? 0.5f : 1f) || actor1.lover == actor2))
                {
                    var normal = 0.3f;
                    if (actor1.lover == actor2)
                        normal += 0.5f;

                    actor1.a.data.get("sexual_happiness", out float happiness);
                    if (happiness < 0)
                    {
                        normal += Math.Abs((happiness / 100) / 2);
                    }

                    if (!QueerTraits.PreferenceMatches(actor1, actor2, true))
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

        public static bool WillDoSex(Actor pActor, string sexReason, bool withLover=true, bool isInit=false)
        {
            // if (QueerTraits.GetPreferenceFromActor(pActor, true) == Preference.Neither)
            //     return false;
            
            pActor.data.get("sexual_happiness", out float d);
            if (isInit)
            {
                Debug(pActor.getName() + " is requesting to do sex: "+sexReason + ". Sexual happiness: "+d + ". With lover: "+withLover);
            }
            else
            {
                Debug(pActor.getName() + " is being requested to do sex: "+sexReason + ". Sexual happiness: "+d + ". With lover: "+withLover);
            }

            if (!isInit && !HasHadSexRecently(pActor) && (pActor.isLying() || (pActor.hasTask() &&
                                                                       !(pActor.ai.task.cancellable_by_reproduction ||
                                                                         pActor.ai.task.cancellable_by_socialize))))
            {
                Util.Debug("Unable to do sex from this actor due to an uncancellable task");
                return false;
            }
            
            var allowedToHaveSex = withLover || CanHaveSexWithoutRepercussionsWithSomeoneElse(pActor, sexReason);
            var reduceChances = 0f;
            pActor.data.get("sexual_happiness", out float sexualHappiness);
            
            if (sexualHappiness < 0)
            {
                var toReduce = sexualHappiness / 100;
                reduceChances += toReduce;
            }

            if(!allowedToHaveSex
               && Randy.randomChance(Math.Max(0, (pActor.hasTrait("unfaithful") && !IsFaithful(pActor) ? 0.5f : 1f) + reduceChances)))
            {
                Util.Debug("Not allowed to do sex because of lover and not low enough happiness");
                return false;
            }

            if (!allowedToHaveSex && IsFaithful(pActor))
            {
                Util.Debug("Not allowed to do sex because of lover and is faithful");
                return false;
            }
            
            reduceChances = 0.1f;
            if (sexualHappiness > 0)
            {
                reduceChances += sexualHappiness / 100f;
            }

            var doSex = Randy.randomChance(Math.Max(0, 1f - reduceChances));
            if (!doSex && !sexReason.Equals("reproduction"))
            {
                Util.Debug("Will not do sex because they are not sexually unhappy enough");
                return false;
            }

            if(!allowedToHaveSex)
                Util.Debug(pActor.getName() + " is cheating!");
            return true;
        }
        
        // handles the actors' happiness and mood towards their sex depending on their preferences
        // handle cheating here too
        public static void JustHadSex(Actor actor1, Actor actor2)
        {
            Util.Debug(actor1.getName() + " had sex with "+actor2.getName()+". They are lovers: "+(actor1.lover==actor2));
            actor1.data.set("last_had_sex_with", actor2.getID());
            actor2.data.set("last_had_sex_with", actor1.getID());
            
            actor1.addAfterglowStatus();
            actor2.addAfterglowStatus();   
            
            if (Randy.randomChance(actor1.lover == actor2 ? 1f : QueerTraits.BothPreferencesMatch(actor1, actor2, true) ? 0.25f : 0f))
            {
                actor1.changeHappiness("just_kissed");
                actor2.changeHappiness("just_kissed");   
            }
            
            OpinionOnSex(actor1, actor2);
            OpinionOnSex(actor2, actor1);

            if (actor1.lover != actor2)
            {
                actor1.data.get("sex_reason", out var sexReason, "reproduction");
                Util.Debug("Sex Reason: "+sexReason);
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
                ChangeSexualHappinessBy(actor1.lover, -25f);
            }

            if (actor2.hasLover() && actor2.lover != actor1)
            {
                ChangeSexualHappinessBy(actor2.lover, -25f);
            }
        }

        public static bool CanHaveSexWithoutRepercussionsWithSomeoneElse(Actor actor, string sexReason)
        {
            return !actor.hasLover()
                   || (actor.hasLover() && ((!QueerTraits.PreferenceMatches(actor, actor.lover, true)
                                                              && actor.hasCultureTrait("sexual_expectations") && actor.lover.hasCultureTrait("sexual_expectations"))
                                                              || (actor.hasSubspeciesTrait("preservation") && IsDyingOut(actor) 
                                                                  && sexReason.Equals("reproduction")
                                                                  && (!CanMakeBabies(actor.lover) || !CanReproduce(actor, actor.lover)))));
        }

        public static void PotentiallyCheatedWith(Actor actor, Actor actor2)
        {
            if (actor.hasLover() && actor.lover != actor2)
            {
                var cheatedActor = actor.lover;
                if (cheatedActor.isLying() || !cheatedActor.isOnSameIsland(actor))
                    return;
                
                HandleFamilyRemoval(actor);

                cheatedActor.addStatusEffect("cheated_on");
                cheatedActor.setLover(null);
                actor.setLover(null);
            }
        }

        public static bool OnceDated(Actor actor, Actor actor2)
        {
            actor.data.get("cheated_"+actor2.getID(), out bool actor2Cheated);
            actor2.data.get("cheated_"+actor.getID(), out bool actorCheated);
            actor.data.get("broke_up_"+actor2.getID(), out bool actor2BrokeUp);
            actor2.data.get("broke_up_"+actor.getID(), out bool actorBrokeUp);
            
            return actor2Cheated || actorCheated || actor2BrokeUp || actorBrokeUp;
        }

        public static void BreakUp(Actor actor)
        {
            HandleFamilyRemoval(actor);
            
            actor.data.set("broke_up_" +actor.lover.getID(),true);
            actor.lover.data.set("broke_up_" +actor.getID(),true);
            
            actor.lover.setLover(null);
            actor.lover.changeHappiness("breakup");
            actor.setLover(null);
            actor.changeHappiness("breakup");
        }

        public static void HandleFamilyRemoval(Actor actor)
        {
            if (actor.hasFamily() && actor.hasLover())
            {
                var family = actor.family;
                var lover = actor.lover;
                if (family.isMainFounder(actor) && family.isMainFounder(lover))
                {
                    if (family.countUnits() <= 2)
                    {
                        actor.setFamily(null);
                        lover.setFamily(null);
                        return;
                    }
                    
                    var actor1Units = new List<Actor>();
                    var actor2Units = new List<Actor>();
                    
                    foreach (var unit in family.getUnits())
                    {
                        var sameAsActor1 = unit.isSameSubspecies(actor.subspecies);
                        var sameAsActor2 = unit.isSameSubspecies(lover.subspecies);
                        if ((sameAsActor1 && sameAsActor2) || (!sameAsActor1 && !sameAsActor2))
                        {
                            if(Randy.randomChance(0.5f))
                                actor1Units.Add(unit);
                            else
                                actor2Units.Add(unit);
                            continue;
                        }
                        
                        if(sameAsActor1)
                            actor1Units.Add(unit);
                        if(sameAsActor2)
                            actor2Units.Add(unit);
                    }
                    
                    var family1 = BehaviourActionBase<Actor>.world.families.newFamily(actor, actor.current_tile, null);
                    var family2 = BehaviourActionBase<Actor>.world.families.newFamily(lover, lover.current_tile, null);
                    
                    foreach (var unit in actor1Units)
                    {
                        unit.setFamily(family1);
                    }
                    
                    foreach (var unit in actor2Units)
                    {
                        unit.setFamily(family2);
                    }
                }
            }
        }

        public static bool IsOrientationSystemEnabledFor(Actor pActor)
        {
            return !pActor.hasCultureTrait("orientationless");
        }

        public static bool CanMakeBabies(Actor pActor)
        {
            return pActor.canBreed() &&
                   pActor.isAdult() && !pActor.hasReachedOffspringLimit() &&
                   !pActor.subspecies.hasReachedPopulationLimit() && (!pActor.hasCity() || !pActor.city.hasReachedWorldLawLimit()
            && ((pActor.subspecies.isReproductionSexual() || pActor.subspecies.hasTraitReproductionSexualHermaphroditic() 
                                                          || pActor.hasSubspeciesTrait("reproduction_same_sex"))
            && pActor.current_children_count == 0 || pActor.city.hasFreeHouseSlots()));
        }

        public static bool WantsBaby(Actor pActor)
        {
            if (!IsSmart(pActor) || IsDyingOut(pActor))
            {
                Debug(pActor.getName() + " wants a baby because they are non-intelligent species or are dying out");
                return true;
            }

            if (pActor.hasHouse() && pActor.getHappiness() >= 100)
            {
                Debug(pActor.getName() + " wants a baby because they have a house and are happy enough");
                return true;
            }

            Debug(pActor.getName() + " does not want a baby.");
            return false;
        }

        public static void Debug(string message)
        {
            var config = TopicOfLoving.Mod.GetConfig();
            var slowOnLog = (bool)config["Misc"]["SlowOnLog"].GetValue();
            var debug = (bool)config["Misc"]["Debug"].GetValue();

            if (!debug)
                return;
            if(slowOnLog)
                Config.setWorldSpeed(AssetManager.time_scales.get("slow_mo"));
            LogService.LogInfo(message);
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

        public static bool CanCommitIncest(Actor pActor)
        {
            if (!pActor.hasSubspeciesTrait("incest"))
                return false;

            if (IsDyingOut(pActor))
                return true;

            if (pActor.clan.hasTrait(Util.clanboundIsolation))
                return true;

            if (pActor.hasCultureTrait("incest_taboo") && random.Next(1, 60) == 1)
                return true;

            if (pActor.hasCultureTrait("scar_of_incest") && random.Next(1, 2) == 1)
                return true;

            if (random.Next(1, 5) == 1)
                return true;
        
            return false;
        }
        
        private static readonly Random random = new();

        public static ClanTrait clanboundIsolation = AssetManager.clan_traits.get("clanbound_isolation");

        public static void GiveIncestCommitmentTrait(Actor actor1, Actor actor2)
        {
            if (actor1.isRelatedTo(actor2))
            {
                actor1.addTrait("committed_incest");
                actor2.addTrait("committed_incest");
            }
        }
    }
}