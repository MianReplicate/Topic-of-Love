using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using ai.behaviours;
using HarmonyLib;
using NeoModLoader.services;

namespace Better_Loving
{
    public class DecisionAndBehaviorTasks
    {
        private static List<DecisionAsset> _decisionAssets = new List<DecisionAsset>();
        public static void Init()
        {
            BehaviourTaskActor insultOrientation = new BehaviourTaskActor
            {
                id = "insult_orientation",
                locale_key = "task_insult_orientation",
                path_icon = "ui/Icons/culture_traits/orientationless",
            };
            insultOrientation.addBeh(new BehFindMismatchedOrientation());
            insultOrientation.addBeh(new BehGoToActorTarget(GoToActorTargetType.NearbyTileClosest, pCalibrateTargetPosition: true));
            insultOrientation.addBeh(new BehCheckNearActorTarget());
            insultOrientation.addBeh(new BehInsultOrientation());
            AddBehavior(insultOrientation);

            AddDecision(new DecisionAsset
            {
                id = "insult_orientation_try",
                task_id = "insult_orientation",
                priority = NeuroLayer.Layer_2_Moderate,
                path_icon = "ui/Icons/culture_traits/orientationless",
                cooldown = 30,
                action_check_launch = actor => (actor.hasCultureTrait("homophobic") || actor.hasCultureTrait("heterophobic")) && Util.IsOrientationSystemEnabledFor(actor),
                weight = 0.5f,
                list_civ = true,
                only_safe = true
            });
            AssetManager.subspecies_traits.get("wernicke_area").addDecision("insult_orientation_try");
            
            // should not always cause a pregnancy!
            AddDecision(new DecisionAsset
            {
                id = "invite_for_sex",
                priority = NeuroLayer.Layer_2_Moderate,
                path_icon = "ui/Icons/status/enjoyed_sex",
                cooldown = 30,
                action_check_launch = actor => Util.IsSmart(actor)
                                               && QueerTraits.GetQueerTraits(actor).Count >= 2 
                                               && !QueerTraits.GetPreferenceFromActor(actor, true).Equals(Preference.Neither)
                                               && !Util.IsSexualHappinessEnough(actor, 100f)
                                               && !Util.HasHadSexRecently(actor)
                                                && Util.IsOrientationSystemEnabledFor(actor),
                list_civ = true,
                weight_calculate_custom = actor => Util.IsSexualHappinessEnough(actor, 75f) ? 0.25f: 
                    Util.IsSexualHappinessEnough(actor, 50f) ? 0.5f : Util.IsSexualHappinessEnough(actor, 0) ? .75f : 
                    Util.IsSexualHappinessEnough(actor, -50) ? 1f : Util.IsSexualHappinessEnough(actor, -100f) ? 1.5f : 1.25f,
                only_adult = true,
                only_safe = true,
                cooldown_on_launch_failure = true
            });

            // will force all units to make babies regardless of orientation if they have preservation
            AddDecision(new DecisionAsset
            {
                id = "reproduce_preservation",
                priority = NeuroLayer.Layer_3_High,
                path_icon = "ui/Icons/status/disliked_sex",
                cooldown = 20,
                action_check_launch = actor =>
                {
                    actor.subspecies.countReproductionNeuron();
                    return Util.IsDyingOut(actor)
                           && QueerTraits.GetQueerTraits(actor).Count >= 2
                           && Util.CanMakeBabies(actor)
                           && !Util.HasHadSexRecently(actor)
                           && actor.hasSubspeciesTrait("preservation")
                           && Util.IsOrientationSystemEnabledFor(actor);
                },
                weight_calculate_custom = actor => Util.CanMakeBabies(actor) ? 2f : 0.1f,
                only_adult = true,
                only_safe = true,
                cooldown_on_launch_failure = true
            });
            AssetManager.subspecies_traits.get("reproduction_sexual").addDecision("reproduce_preservation");
            AssetManager.subspecies_traits.get("reproduction_same_sex").addDecision("reproduce_preservation");
            AssetManager.subspecies_traits.get("reproduction_hermaphroditic").addDecision("reproduce_preservation");

            var reproduceForPreservation = new BehaviourTaskActor
            {
                id = "reproduce_preservation",
                locale_key = "task_reproduce_preservation",
                path_icon = "ui/Icons/status/disliked_sex",
            };
            reproduceForPreservation.addBeh(new BehFindReproduceableSex());
            reproduceForPreservation.addBeh(new BehGetPossibleTileForSex());

            AddBehavior(reproduceForPreservation);

            var doKissWithLover = new BehaviourTaskActor
            {
                id = "kiss_lover",
                locale_key = "task_kiss_lover",
                path_icon = "ui/Icons/status/enjoyed_sex"
            };
            doKissWithLover.addBeh(new BehGetLoverForKiss());
            
            AddBehavior(doKissWithLover);

            var inviteForSex = new BehaviourTaskActor
            {
                id = "invite_for_sex",
                locale_key = "task_invite_for_sex",
                path_icon = "ui/Icons/status/enjoyed_sex",
            };
            inviteForSex.addBeh(new BehFindMatchingPreference());
            inviteForSex.addBeh(new BehGetPossibleTileForSex());
            AddBehavior(inviteForSex);

            var haveSexGo = new BehaviourTaskActor // our alternative of the sexual_reproduction_civ_go because it only works on lovers
            {
                id = "have_sex_go",
                locale_key = "task_have_sex_go",
                path_icon = "ui/Icons/status/enjoyed_sex",
            };
            haveSexGo.addBeh(new BehGoToTileTarget());
            for (int index = 0; index < 6; ++index)
            {
                haveSexGo.addBeh(new BehRandomWait(2f, 3f));
                haveSexGo.addBeh(new BehCheckForSexTarget());
                haveSexGo.addBeh(new BehRandomWait(1f, 2f));
            }
            AddBehavior(haveSexGo);
            
            Finish();
        }

        private static void Finish()
        {
            // using (ListPool<DecisionAsset> list1 = new ListPool<DecisionAsset>(AssetManager.decisions_library.list_only_civ))
            // {
            //     using (ListPool<DecisionAsset> list2 = new ListPool<DecisionAsset>(AssetManager.decisions_library.list_only_children))
            //     {
            //         using (ListPool<DecisionAsset> list3 = new ListPool<DecisionAsset>(AssetManager.decisions_library.list_only_city))
            //         {
            //             using (ListPool<DecisionAsset> list4 = new ListPool<DecisionAsset>(AssetManager.decisions_library.list_only_animal))
            //             {
            //                 using (ListPool<DecisionAsset> list5 = new ListPool<DecisionAsset>(AssetManager.decisions_library.list_others))
            //                 {
            //                     int num = 0;
            //                     for(int i = 0; i < _decisionAssets.Count; i++)
            //                     {
            //                         DecisionAsset decisionAsset = _decisionAssets[i];
            //                         decisionAsset.decision_index = num++;
            //                         decisionAsset.priority_int_cached = (int) decisionAsset.priority;
            //                         decisionAsset.has_weight_custom = decisionAsset.weight_calculate_custom != null;
            //                         if (!decisionAsset.unique)
            //                         {
            //                             if (decisionAsset.list_baby)
            //                                 list2.Add(decisionAsset);
            //                             else if (decisionAsset.list_animal)
            //                                 list4.Add(decisionAsset);
            //                             else if (decisionAsset.list_civ)
            //                                 list1.Add(decisionAsset);
            //                             else
            //                                 list5.Add(decisionAsset);
            //                         }
            //                     }
            //                     this.list_only_civ = list1.ToArray<DecisionAsset>();
            //                     this.list_only_children = list2.ToArray<DecisionAsset>();
            //                     this.list_only_city = list3.ToArray<DecisionAsset>();
            //                     this.list_only_animal = list4.ToArray<DecisionAsset>();
            //                     this.list_others = list5.ToArray<DecisionAsset>();
            //                     base.linkAssets();
            //                 }
            //             }
            //         }
            //     }
            // }
            
            for(int i = 0; i < _decisionAssets.Count; i++)
            {
                var decisionAsset = _decisionAssets[i];
                decisionAsset.priority_int_cached = (int) decisionAsset.priority;
                decisionAsset.has_weight_custom = decisionAsset.weight_calculate_custom != null;
                if (!decisionAsset.unique)
                {
                    if (decisionAsset.list_baby)
                        AssetManager.decisions_library.list_only_children = AssetManager.decisions_library.list_only_children.AddToArray(decisionAsset);
                    else if (decisionAsset.list_animal)
                        AssetManager.decisions_library.list_only_animal = AssetManager.decisions_library.list_only_animal.AddToArray(decisionAsset);
                    else if (decisionAsset.list_civ)
                        AssetManager.decisions_library.list_only_civ = AssetManager.decisions_library.list_only_civ.AddToArray(decisionAsset);
                    else
                        AssetManager.decisions_library.list_only_city = AssetManager.decisions_library.list_only_city.AddToArray(decisionAsset);
                }
            }
        }

        private static void AddBehavior(BehaviourTaskActor behaviour)
        {
            AssetManager.tasks_actor.add(behaviour);
        }

        private static void AddDecision(DecisionAsset asset)
        {
            AssetManager.decisions_library.add(asset);
            asset.decision_index = AssetManager.decisions_library.list.Count-1;
            _decisionAssets.Add(asset);
        }
    }

    public class BehGetLoverForKiss : BehaviourActionActor
    {
        public override BehResult execute(Actor pObject)
        {
            if (!pObject.hasLover() || !pObject.isOnSameIsland(pObject.lover) || pObject.lover.isLying())
                return BehResult.Stop;
            
            pObject.beh_actor_target = pObject.lover;
            return BehResult.Continue;
        }
    }
    
    public class BehGetPossibleTileForSex : BehaviourActionActor
    {
        public bool isPlacePrivateForBreeding(Actor actor, WorldTile tile)
        {
            int num1 = Toolbox.countUnitsInChunk(tile);
            if (!actor.hasCity())
                return actor.asset.animal_breeding_close_units_limit > num1;
            int num2 = actor.city.getPopulationMaximum() * 2 + 10;
            return actor.city.countUnits() < num2;
        }
        public override BehResult execute(Actor pActor)
        {
            if (pActor.beh_actor_target == null)
            {
                Util.Debug(pActor.getName()+": Cancelled because actor was null");

                return BehResult.Stop;
            }

            var homeBuilding = GetHomeBuilding(pActor, pActor.beh_actor_target.a);

            pActor.beh_tile_target = homeBuilding != null ? homeBuilding.current_tile : pActor.beh_actor_target.current_tile;
            if (!isPlacePrivateForBreeding(pActor, pActor.beh_tile_target))
            {
                Util.Debug("Cancelled because of lack of privacy");
                return BehResult.Stop;
            }
            
            var sexActor = pActor.beh_actor_target.a;
            
            sexActor.clearBeh();
            sexActor.beh_actor_target = pActor;
            sexActor.beh_tile_target = pActor.beh_tile_target;
            if (homeBuilding != null)
            {
                sexActor.beh_building_target = homeBuilding;
                pActor.beh_building_target = homeBuilding;
            }
            sexActor.setTask("have_sex_go", pCleanJob: true, pClean: false, pForceAction: true);
            sexActor.timer_action = 0.0f;
            return forceTask(pActor, "have_sex_go", pClean: false, pForceAction: true);
        }

        private static Building GetHomeBuilding(Actor pActor1, Actor pActor2)
        {
            if (pActor1.hasHouse())
                return pActor1.getHomeBuilding();
            return pActor2.hasHouse() ? pActor2.getHomeBuilding() : null;
        }
    }
    public class BehCheckForSexTarget : BehaviourActionActor
    {
        public override BehResult execute(Actor pActor)
        {
            if (pActor.beh_actor_target == null)
            {
                Util.Debug(pActor.getName()+": Cancelled from checking for sex target because actor was null");
                return BehResult.Stop;
            }

            Actor sexActor = pActor.beh_actor_target.a;
            if (sexActor.isTask("have_sex_go") && sexActor.ai.action_index > 3 && sexActor.beh_building_target == null)
            {
                return forceTask(pActor, "sexual_reproduction_outside", false, true);
            }  
            
            if (sexActor.isTask("have_sex_go") && sexActor.beh_building_target == pActor.beh_building_target && sexActor.ai.action_index > 3)
            {
                // this is not working properly for some reason
                pActor.stayInBuilding(pActor.beh_building_target);
                sexActor.stayInBuilding(sexActor.beh_building_target);
                sexActor.setTask("sexual_reproduction_civ_wait", false, pForceAction: true);
                
                return forceTask(pActor, "sexual_reproduction_civ_action", false, true);
            }
            return !sexActor.isTask("have_sex_go") ? BehResult.Stop : BehResult.Continue;
        }
    }
    public class BehFindReproduceableSex : BehaviourActionActor
    {
        public override BehResult execute(Actor pActor)
        {
            var lover = pActor.lover;
            Actor closestActor = null;
            var withLover = false;
            Util.Debug(pActor.getName() + " is requesting reproduction");

            if (lover != null)
            {
                if (lover.isSameIslandAs(pActor)
                    && Util.WillDoSex(lover, "reproduction") 
                    && Util.CanReproduce(pActor, lover)
                    && Util.CanMakeBabies(lover))
                {
                    closestActor = lover;
                    withLover = true;
                }

                if (Util.CanReproduce(pActor, lover) && !withLover)
                    return BehResult.Stop;
            }

            if (!Util.WillDoSex(pActor, "reproduction", withLover, isInit: true))
            {
                Util.Debug("They decided that they will not do it.");
                return BehResult.Stop;
            }
            
            // try to do it with lover first
            if (closestActor == null)
            {
                closestActor = GetClosestPossibleMatchingActor(pActor);
                if (closestActor == null)
                    return BehResult.Stop;   
            }
            Util.Debug(pActor.getName()+ " is going to reproduce with: "+closestActor.getName()+". They are lovers: "+(pActor.lover==closestActor));
            pActor.beh_actor_target = closestActor;
            
            pActor.data.set("sex_reason", "reproduction");
            closestActor.data.set("sex_reason", "reproduction");

            Util.GiveIncestCommitmentTrait(pActor, closestActor);

            Util.Debug($"\nAble to make a baby for task?\n{pActor.getName()}: "+(Util.CanMakeBabies(pActor)+$"\n${closestActor.getName()}: "+(Util.CanMakeBabies(closestActor))));
            
            return BehResult.Continue;
        }
        
        private Actor GetClosestPossibleMatchingActor(Actor pActor)
        {
            using (ListPool<Actor> pCollection = new ListPool<Actor>(5))
            {
                foreach (var pTarget in Finder.getUnitsFromChunk(pActor.current_tile, 4))
                {
                    if (pTarget != pActor 
                        && Util.CanMakeBabies(pTarget)
                        && pActor.isSameIslandAs(pTarget) 
                        && Util.CanReproduce(pActor, pTarget) 
                        && pTarget.isAdult()
                        && Util.WillDoSex(pTarget, "reproduction", pTarget.lover == pActor)
                        && (pActor.isSameSubspecies(pTarget.subspecies) 
                            || (Util.IsSmart(pTarget) && Util.IsSmart(pActor) 
                                                      && QueerTraits.PreferenceMatches(pTarget, pActor, true))))
                    {
                        pCollection.Add(pTarget);
                    }
                }

                return Toolbox.getClosestActor(pCollection, pActor.current_tile);
            }
        }
    }
    public class BehFindMatchingPreference : BehaviourActionActor
    {
        public override BehResult execute(Actor pActor)
        {
            var lover = pActor.lover;
            var withLover = false;
            Actor closestActor = null;

            Util.Debug(pActor.getName() + " is requesting casual sex");
            
            if (lover != null)
            {
                if (lover.isSameIslandAs(pActor)
                    && ((QueerTraits.PreferenceMatches(pActor, lover, true) && QueerTraits.PreferenceMatches(lover, pActor, true)) 
                        || Randy.randomChance(0.5f))
                    && Util.WillDoSex(lover, "casual"))
                {
                    closestActor = lover;
                    withLover = true;
                }
            }
            
            if (!Util.WillDoSex(pActor, "casual", withLover, isInit: true))
            {
                Util.Debug("They decided that they will not do it.");
                return BehResult.Stop;
            }
            
            if (closestActor == null)
            {
                closestActor = GetClosestPossibleMatchingActor(pActor);
                if (closestActor == null)
                {
                    Util.Debug("No actor found");
                    return BehResult.Stop;   
                }
            }
            
            Util.Debug(pActor.getName() + " is going to do casual sex with: "+closestActor.getName() + ". They are lovers: "+(pActor.lover==closestActor));
            pActor.beh_actor_target = closestActor;
            
            pActor.data.set("sex_reason", "casual");
            closestActor.data.set("sex_reason", "casual");

            Util.GiveIncestCommitmentTrait(pActor, closestActor);

            return BehResult.Continue;
        }
        
        private Actor GetClosestPossibleMatchingActor(Actor pActor)
        {
            var bestFriendIsValid = pActor.hasBestFriend() 
                                    && pActor.isSameIslandAs(pActor.getBestFriend()) 
                                    && pActor.distanceToActorTile(pActor.getBestFriend()) < 50f; 
            
            using (ListPool<Actor> pCollection = new ListPool<Actor>(4))
            {
                if (bestFriendIsValid && QueerTraits.PreferenceMatches(pActor, pActor.getBestFriend(), true) &&
                    QueerTraits.PreferenceMatches(pActor.getBestFriend(), pActor, true)
                    && Util.WillDoSex(pActor.getBestFriend(), "casual", pActor.lover == pActor.getBestFriend()))
                    return pActor.getBestFriend();
                
                var pRandom = Randy.randomBool();
                var pChunkRadius = Randy.randomInt(2, 3);
                var num = Randy.randomInt(5, 10);
                foreach (Actor pTarget in Finder.getUnitsFromChunk(pActor.current_tile, pChunkRadius, pRandom: pRandom))
                {
                    if (pTarget != pActor 
                        && pActor.isSameIslandAs(pTarget) 
                        && QueerTraits.PreferenceMatches(pActor, pTarget, true) 
                        && QueerTraits.PreferenceMatches(pTarget, pActor, true)
                        && Util.WillDoSex(pTarget, "casual", pTarget.lover == pActor)
                        && pTarget.last_decision_id != "sexual_reproduction_try")
                    {
                        pCollection.Add(pTarget);
                        if (((ICollection) pCollection).Count >= num)
                            break;
                    }
                }
                
                return Toolbox.getClosestActor(pCollection, pActor.current_tile);
            }
        }
    }
    public class BehFindMismatchedOrientation : BehaviourActionActor
    {
        public override BehResult execute(Actor pActor)
        {
            Actor closestActorWithMismatchedOrientation = GetClosestActorMismatchOrientation(pActor);
            if (closestActorWithMismatchedOrientation == null)
                return BehResult.Stop;
            pActor.beh_actor_target = closestActorWithMismatchedOrientation;
            return BehResult.Continue;
        }
        private static Actor GetClosestActorMismatchOrientation(Actor pActor)
        {
            using (ListPool<Actor> pCollection = new ListPool<Actor>(4))
            {
                var unfitPreferences = new List<Preference>();
                if (pActor.hasCultureTrait("homophobic"))
                {
                    unfitPreferences.Add(Preference.All);
                    unfitPreferences.Add(Preference.SameSex);
                    unfitPreferences.Add(Preference.SameOrDifferentSex);
                }
                if (pActor.hasCultureTrait("heterophobic"))
                {
                    unfitPreferences.Add(Preference.DifferentSex);
                }
                
                var pActorTraits = QueerTraits.GetQueerTraits(pActor, true);
                if (pActorTraits.Count < 2) return null;

                if (unfitPreferences.Contains(pActorTraits[0].preference) ||
                    unfitPreferences.Contains(pActorTraits[1].preference)) return null;
                // don't insult someone else
                
                var pRandom = Randy.randomBool();
                var pChunkRadius = Randy.randomInt(1, 2);
                var num = Randy.randomInt(1, 4);
                foreach (Actor pTarget in Finder.getUnitsFromChunk(pActor.current_tile, pChunkRadius, pRandom: pRandom))
                {
                    if (pTarget != pActor && pActor.isSameIslandAs(pTarget))
                    {
                        var queerTraits = QueerTraits.GetQueerTraits(pTarget, true);
                        if (queerTraits.Count < 2) continue;
                        var romanticPreference = queerTraits[1].preference;
                        var sexualPreference = queerTraits[0].preference;

                        if (unfitPreferences.Contains(romanticPreference) || unfitPreferences.Contains(sexualPreference))
                        {
                            pCollection.Add(pTarget);
                        }
                        if (((ICollection) pCollection).Count >= num)
                            break;
                    }
                }
                
                return Toolbox.getClosestActor(pCollection, pActor.current_tile);
            }
        }
    }
    public class BehInsultOrientation : BehaviourActionActor
    {
        public override BehResult execute(Actor pActor)
        {
            var target = pActor.beh_actor_target?.a;
            if (target == null || Toolbox.DistTile(target.current_tile, pActor.current_tile) > 4.0)
                return BehResult.Stop;
            target.changeHappiness("insulted_for_orientation");

            if (Randy.randomChance(0.5f))
            {
                pActor.addAggro(target);
                pActor.startFightingWith(target);
            }
            else if (Randy.randomChance(0.6f))
            {
                target.addStatusEffect("crying");
            } else if (Randy.randomChance(0.8f))
            {
                target.addAggro(pActor);
                target.startFightingWith(pActor);
            }

            return BehResult.Stop;
        }
    }
}