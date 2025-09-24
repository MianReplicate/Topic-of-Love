using System.Collections;
using System.Collections.Generic;
using HarmonyLib;

namespace Topic_of_Love.Mian.Patches;

public class BabyMakerPatch
{
    [HarmonyPatch(typeof(BabyMaker), nameof(BabyMaker.makeBabyFromPregnancy))]
    class MakeBabyFromPregnancyPatch
    {
        static bool Prefix(Actor pActor)
        {
            var mother = pActor;
            mother.data.get("otherParent", out long otherParentID);
            var otherParent = MapBox.instance.units.get(otherParentID);
            mother.data.removeLong("otherParent");

            mother.birthEvent();
            BabyHelper.countMakeChild(mother, otherParent);
            BabyMaker.makeBaby(mother, otherParent);
            var pVal = 0.5f;
            var stat = (int) mother.stats["birth_rate"];
            for (var index = 0; index < stat && Randy.randomChance(pVal); ++index)
            {
                BabyMaker.makeBaby(mother, otherParent);
                pVal *= 0.85f;
            }

            return false;
        }
    }
        // We just randomize the parent chosen for the subspecies here
    [HarmonyPatch(typeof(BabyMaker), nameof(BabyMaker.makeBaby))]
    class MakeBabyPatch
    {
        static void Postfix(Actor __result)
        {
            ActorManagerPatch.NewUnit(__result); // generate preferences
        }
        static bool Prefix(
            Actor pregnantActor,
            Actor dickActor,
            ActorSex pForcedSexType,
            bool pCloneTraits,
            int pMutationRate,
            WorldTile pTile,
            bool pAddToFamily,
            bool pJoinFamily,
            ref Actor __result
            )
        {
            List<Actor> birthers = new List<Actor>{pregnantActor};
            if(dickActor != null)
                birthers.Add(dickActor);
            
            City pCity = pregnantActor.city ?? dickActor?.city;
            
            if (pCity != null)
                --pCity.status.housing_free;

            Actor dominantParent = TolUtil.EnsurePopulationFromParent(birthers);
            if (dominantParent == null) 
                // there seems to be a bug in the game that allows reproduction strategies that aren't sexual to PRODUCE beyond the harmony traits cap. probably because they dont check for populations there lol
                dominantParent = pregnantActor;
            
            Actor nonDominantParent = dominantParent != pregnantActor ? pregnantActor : dickActor;
            ActorAsset asset = dominantParent.asset;
            
            ActorData pData = new ActorData();
            pData.created_time = World.world.getCurWorldTime();
            pData.id = World.world.map_stats.getNextId("unit");
            pData.asset_id = asset.id;
            int generation = dominantParent.data.generation;
            if (nonDominantParent != null && nonDominantParent.data.generation > generation)
                generation = nonDominantParent.data.generation;
            pData.generation = generation + 1;
            using (ListPool<WorldTile> list = new ListPool<WorldTile>(4))
            {
                foreach (WorldTile worldTile in dominantParent.current_tile.neighboursAll)
                {
                    if (worldTile != dominantParent.current_tile &&
                        (nonDominantParent == null || worldTile != nonDominantParent.current_tile) &&
                        worldTile.Type.ground)
                        list.Add(worldTile);
                }

                WorldTile pTile1 = pTile == null
                    ? ((ICollection)list).Count != 0 ? list.GetRandom<WorldTile>() : dominantParent.current_tile
                    : pTile;
                Actor actorFromData = World.world.units.createBabyActorFromData(pData, pTile1, pCity);

                pregnantActor.data.get("familyParentA", out var familyParentAid, 0L);
                // pParent1.data.get("familyParentB", out var familyParentBid, 0L);
                var familyParentA = World.world.units.get(familyParentAid);
                var familyParentB = familyParentA?.lover;

                if (familyParentA != null)
                {
                    // sexual ivf adoption :O
                    if(familyParentA.hasKingdom())
                        familyParentA.kingdom.increaseAdoptedBaby();
                    if(familyParentA.hasCity())
                        familyParentA.city.increaseAdoptedBaby();
                    World.world.increaseAdoptedBaby();

                    familyParentA.changeHappiness("adopted_baby");
                }
                else
                {
                    familyParentA = dominantParent;
                }
                
                if (familyParentB != null){
                    if(familyParentB.hasKingdom() && familyParentB.kingdom != familyParentA.kingdom)
                        familyParentB.kingdom.increaseAdoptedBaby();
                    if(familyParentB.hasCity() && familyParentB.city != familyParentA.city)
                        familyParentB.city.increaseAdoptedBaby();
                    
                    familyParentB.changeHappiness("adopted_baby");
                }else if (nonDominantParent != null)
                {
                    familyParentB = nonDominantParent;
                }
                
                actorFromData.setParent1(dominantParent);
                if(nonDominantParent != null)
                    actorFromData.setParent2(nonDominantParent);

                if (pAddToFamily)
                {
                    if (!familyParentA.hasFamily())
                        World.world.families.newFamily(familyParentA, familyParentA.current_tile, 
                            familyParentB != null && !familyParentB.hasFamily() ? familyParentB : null);
                    else if (familyParentB != null && !familyParentB.hasFamily())
                    {
                        World.world.families.newFamily(familyParentB, familyParentA.current_tile, null);
                    }
                } else if (pJoinFamily) {
                    Family pObject = 
                        familyParentA.hasFamily() ? 
                        familyParentA.family : World.world.families.newFamily(familyParentA, familyParentA.current_tile, familyParentB);
                    if (pObject != null)
                        actorFromData.setFamily(pObject);
                }
                BabyHelper.applyParentsMeta(familyParentA, familyParentB, actorFromData);
                // the game seems to have some sort of code that chooses a baby's subspecies based on generation? not really sure how it works tbh
                actorFromData.setSubspecies(dominantParent.subspecies);

                if (pCloneTraits || dominantParent.hasSubspeciesTrait("genetic_mirror"))
                {
                    BabyHelper.traitsClone(actorFromData, dominantParent);
                }
                else
                {
                    if(familyParentA == dominantParent)
                        familyParentA = null;
                    if(familyParentB == nonDominantParent)
                        familyParentB = null;
                    
                    foreach (ActorTrait trait in (IEnumerable<ActorTrait>)actorFromData.subspecies.getActorBirthTraits()
                                 .getTraits())
                        actorFromData.addTrait(trait);
                    BabyHelperPatch.TraitsInherit(actorFromData, dominantParent, nonDominantParent, familyParentA, familyParentB);
                }

                actorFromData.checkTraitMutationOnBirth();
                actorFromData.setNutrition(SimGlobals.m.nutrition_start_level_baby);
                if (pForcedSexType != ActorSex.None)
                {
                    actorFromData.data.sex = pForcedSexType;
                }
                else
                {
                    ActorSex actorSex = ActorSex.None;

                    // confirm it was sexual
                    if (pregnantActor.hasSubspeciesTrait("reproduction_same_sex") && dickActor != null)
                    {
                        actorSex = pregnantActor.data.sex;
                    } else if (dickActor != null && dickActor.hasSubspeciesTrait("reproduction_same_sex"))
                    {
                        actorSex = dickActor.data.sex;
                    }
                    else
                    {
                        if (Randy.randomBool())
                            actorSex = !dominantParent.hasCity()
                                ? (dominantParent.subspecies.cached_females <= dominantParent.subspecies.cached_males
                                    ? ActorSex.Female
                                    : ActorSex.Male)
                                : (dominantParent.city.status.females <= dominantParent.city.status.males
                                    ? ActorSex.Female
                                    : ActorSex.Male);   
                    }
                    
                    if (actorSex != ActorSex.None)
                        actorFromData.data.sex = actorSex;
                    else
                        actorFromData.generateSex();
                }
                actorFromData.checkShouldBeEgg();
                actorFromData.makeStunned(10f);
                actorFromData.applyRandomForce();
                BabyHelper.countBirth(actorFromData);
                actorFromData.setStatsDirty();
                actorFromData.event_full_stats = true;
                __result = actorFromData;
                
                // LogService.LogInfo($"[{__result.getName()}]: Baby is of {__result.subspecies.name} and comes from {__result.asset.getTranslatedName()}");
                
                pregnantActor.data.removeLong("familyParentA");
                pregnantActor.data.removeLong("familyParentB");
                return false;
            }
        }
    }
}