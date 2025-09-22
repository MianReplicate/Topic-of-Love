using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NCMS.Extensions;
using Topic_of_Love.Mian.CustomAssets.Custom;
using UnityEngine.Pool;

namespace Topic_of_Love.Mian;

public static class Extensions
{
    public static bool CanDoAnySexType(this Actor pActor)
    {
        return pActor.hasSubspeciesTrait("reproduction_hermaphroditic");
    }
        
    public static bool NeedDifferentSexTypeForReproduction(this Actor pActor)
    {
        return pActor.hasSubspeciesTrait("reproduction_sexual");
    }

    // this is to typically catch types like boats
    public static bool CapableOfLove(this Actor pActor)
    {
        return pActor.hasSubspecies() && !pActor.asset.is_boat;
    }

    public static bool HasNoOne(this Actor pActor)
    {
        return !pActor.hasBestFriend() && !pActor.hasFamily() && !pActor.hasLover() && !pActor.getParents().Any();
    }

    public static bool AffectedByIntimacy(this Actor pActor)
    {
        return !pActor.hasTrait("intimacy_averse") && !pActor.hasTrait("psychopath");
    }
    public static bool IsActorUndateable(this Actor pActor, Actor toCheck)
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
    public static void AddOrRemoveUndateableActor(this Actor pActor, Actor undateable)
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
    public static bool IsOrientationSystemEnabled(this Actor pActor)
    {
        return !pActor.hasCultureTrait("orientationless");
    }

    public static bool IsDyingOut(this Actor pActor)
    {
        if (!pActor.hasSubspecies() || pActor.hasReachedOffspringLimit()
                                    || (pActor.hasCity() && pActor.city.getUnitsTotal() >= pActor.city.getPopulationMaximum())) return false;
        var limit = (int)pActor.subspecies.base_stats_meta["limit_population"];
        return pActor.subspecies.countCurrentFamilies() <= 10 
               || (pActor.hasCity() && (pActor.city.getAge() < 100 || (float) pActor.city.getUnitsTotal() / pActor.city.getPopulationMaximum() < 0.7))
               || (limit != 0 ? pActor.subspecies.countUnits() <= limit / 3 : pActor.subspecies.countUnits() <= 100);
    }
        
    public static bool WantsBaby(this Actor pActor, bool reproductionPurposesIncluded=true)
    {
        if (!BabyHelper.canMakeBabies(pActor))
            return false;

        reproductionPurposesIncluded = !pActor.isSapient() ? true : reproductionPurposesIncluded;
        if (reproductionPurposesIncluded)
        {
            if (!IsDyingOut(pActor))
            {
                TolUtil.Debug(pActor.getName() + " wants a baby because they are non-intelligent species or are dying out");
                return true;
            }   
        }

        if (pActor.hasCity() && pActor.city.getUnitsTotal() >= pActor.city.getPopulationMaximum())
            return false;
            
        if (pActor.getHappiness() >= 50 || pActor.getIntimacy() < 10)
        {
            TolUtil.Debug(pActor.getName() + " wants a baby because they are happy enough");
            return true;
        }
            
        return false;
    }
    public static bool CanStopBeingLovers(this Actor actor)
    {
        actor.data.get("force_lover", out var isForced, false);
        return !isForced;
    }

    // can they fall in love in some way?
    public static bool CanFallInLove(this Actor actor)
    {
        actor.data.get("just_lost_lover", out var justLostLover, false);
        actor.data.get("force_lover", out var isForced, false);
        return !justLostLover && !actor.hasStatus("broke_up") && (!isForced || !actor.hasLover()) && CapableOfLove(actor);
    }

    public static void RemoveLovers(this Actor actor)
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
    public static void BreakUp(this Actor actor, bool actorIsSad=true)
    {
        if (!actor.hasLover())
            return;
            
        TolUtil.Debug(actor.getName() + " broke up with "+ actor.lover.getName());
            
        AddOrRemoveUndateableActor(actor, actor.lover);
        AddOrRemoveUndateableActor(actor.lover, actor);

        if(actor.hasKingdom())
            actor.kingdom.increaseBrokenUp();
        if(actor.hasCity())
            actor.city.increaseBrokenUp();
        World.world.increaseBrokenUp();
            
        actor.lover.addStatusEffect("broke_up");
        if(actorIsSad)
            actor.addStatusEffect("broke_up");
            
        RemoveLovers(actor);
    }
    public static void PotentiallyCheatedWith(this Actor actor, Actor actor2)
    {
        if (actor.hasLover() && actor.lover != actor2 && CanStopBeingLovers(actor))
        {
            var cheatedActor = actor.lover;
            // will they know :O
            // if (cheatedActor.isLying() || !cheatedActor.isOnSameIsland(actor))
            //     return;
                
            cheatedActor.addStatusEffect("cheated_on");
                
            if(actor.hasKingdom())
                actor.kingdom.increaseCheated();
            if(actor.hasCity())
                actor.city.increaseCheated();
            World.world.increaseCheated();
        }
    }

    public static bool CanHaveIntimacyWithoutRepercussions(this Actor actor, SexType sexType)
    {
        if (sexType == SexType.None)
        {
            return !actor.hasLover()
                   || (actor.hasLover() && !LikesManager.LikeMatches(actor, actor.lover)
                                        && actor.lover.hasCultureTrait("sexual_expectations"));
        }
        else
        {
            return !actor.hasLover()
                   || (actor.hasLover() && ((!LikesManager.LikeMatches(actor, actor.lover, true)
                                             && actor.lover.hasCultureTrait("sexual_expectations"))
                                            || (actor.hasCultureTrait("sex_for_reproduction") && sexType == SexType.Reproduction)));
        }
    }

    public static void NewLikes(this Actor actor)
    {
        if (actor != null && CapableOfLove(actor))
        {
            var oldPreferences = actor.GetActorLikes();
            foreach (var preference in oldPreferences)
            {
                actor.data.removeBool(preference.IDWithLoveType);
            }
                
            var preferences =  LikesManager.GetRandomLikes(actor);
            foreach (var preference in preferences)
            {
                actor.data.set(preference.IDWithLoveType, true);
            }
            Orientations.RollOrientationLabel(actor);
        }
    }
    public static bool WillDoIntimacy(this Actor pActor, Actor pTarget, SexType sexReason=SexType.None, bool isInit=false)
        {
            var withLover = pActor.hasLover() && pActor.lover == pTarget;
            
            pActor.data.get("intimacy_happiness", out float d);
            if (isInit)
                if(pActor.isSapient()) TolUtil.Debug(pActor.getName() + " is requesting to do intimacy. Sexual happiness: "+d + ". With lover: "+withLover);
            else
            if(pActor.isSapient()) TolUtil.Debug(pActor.getName() + " is being requested to do intimacy. Sexual happiness: "+d + ". With lover: "+withLover);
            TolUtil.Debug("\n"+sexReason);

            if (!sexReason.Equals(SexType.Reproduction) && (pActor.hasCultureTrait("sex_for_reproduction_only") ||
                                                            pTarget.hasCultureTrait("sex_for_reproduction_only")))
                return false;
            if (!sexReason.Equals(SexType.None) && !pActor.isAdult())
                return false;
            if (!sexReason.Equals(SexType.Reproduction) && !LikesManager.LikeMatches(pActor, pTarget, "identity",
                    !sexReason.Equals(SexType.None)))
                return false;
            
            if (!isInit)
            {
                if(pActor.hasTask() && !(pActor.ai.task.cancellable_by_reproduction ||
                                         pActor.ai.task.cancellable_by_socialize))
                {
                    if(pActor.isSapient()) TolUtil.Debug("Unable to do intimacy from this actor due to an uncancellable task");
                    return false;
                }
            }
            
            var allowedToHaveIntimacy = withLover || pActor.CanHaveIntimacyWithoutRepercussions(sexReason);
            var reduceChances = 0f;
            pActor.data.get("intimacy_happiness", out float intimacyHappiness);
            
            if (intimacyHappiness < 0)
            {
                var toReduce = intimacyHappiness / 300;
                reduceChances += toReduce;
            }

            if (!AffectedByIntimacy(pActor))
                reduceChances = 0f;
            
            reduceChances = Math.Max(-0.2f, reduceChances);

            if(!allowedToHaveIntimacy
               && Randy.randomChance(Math.Max(0, (pActor.hasTrait("unfaithful") ? 0.6f : .99f) + reduceChances)))
            {
                if(pActor.isSapient()) TolUtil.Debug("Not allowed to do intimacy because of lover and not low enough happiness");
                return false;
            }

            if (!allowedToHaveIntimacy && IsFaithful(pActor))
            {
                if(pActor.isSapient()) TolUtil.Debug("Not allowed to do intimacy because of lover and is faithful");
                return false;
            }
            
            reduceChances = 0.1f;
            if (intimacyHappiness > 0)
            {
                reduceChances += intimacyHappiness / 100f;
            }
            
            // person may choose to do sex even if really happy
            var doIntimacy = Randy.randomChance(Math.Max(0.05f, 1f - reduceChances));
            if (!doIntimacy && !sexReason.Equals(SexType.Reproduction))
            {
                if(pActor.isSapient()) TolUtil.Debug("Will not do intimacy since they are deemed to be happy enough");
                return false;
            }   

            if(!allowedToHaveIntimacy)
                if(pActor.isSapient()) TolUtil.Debug(pActor.getName() + " is cheating!");
            return true;
        }
    public static bool IsFaithful(this Actor pActor)
    {
        return pActor.hasCultureTrait("committed") || pActor.hasTrait("faithful");
    }

    public static bool ReproducesSexually(this Actor pActor)
    {
        return pActor.NeedDifferentSexTypeForReproduction() ||
               pActor.CanDoAnySexType();
    }
    public static bool CanReproduce(this Actor pActor, Actor pTarget)
    {
        return pActor.subspecies.isPartnerSuitableForReproduction(pActor, pTarget);
    }
    public static bool IsAbleToBecomePregnant(this Actor pActor)
    {
        return pActor.HasUterus();
    }

    public static void RemoveAllCachedLikes(this Actor pActor)
    {
        MapBox.instance.units.dict.ForEach((pair =>
        {
            var target = pair.Value;
            LikesManager.SetMatchingIDForActors(pActor, target, false, -1);
            LikesManager.SetMatchingIDForActors(pActor, target, true, -1);
        }));
    }

    public static float getIntimacy(this Actor actor)
    {
        actor.data.get("intimacy_happiness", out float intimacy);
        return intimacy;
    }

    public static bool isIntimacyHappinessEnough(this Actor actor, float happiness)
    {
        actor.data.get("intimacy_happiness", out float compare);
        return compare >= happiness;
    }
        
    public static void changeIntimacyHappinessBy(this Actor actor, float happiness)
    {
        actor.data.get("intimacy_happiness", out float init);
        actor.data.set("intimacy_happiness", Math.Max(-100, Math.Min(happiness + init, 100)));
    }

    public static void increaseCheated<TData>(this CoreSystemObject<TData> instance) where TData : BaseSystemData
    {
        instance.data.get("cheated_on_amount", out int amount);
        instance.data.set("cheated_on_amount", amount + 1);
    }
    
    public static void increaseBrokenUp<TData>(this CoreSystemObject<TData> instance) where TData : BaseSystemData
    {
        instance.data.get("broken_up_amount", out int amount);
        instance.data.set("broken_up_amount", amount + 1);
    }
    
    public static void increaseAdoptedBaby<TData>(this CoreSystemObject<TData> instance) where TData : BaseSystemData
    {
        instance.data.get("broken_up_amount", out int amount);
        instance.data.set("broken_up_amount", amount + 1);
    }
    
    public static void increaseCheated(this MapBox instance)
    {
        instance.map_stats.custom_data.get("cheated_on_amount", out int amount);
        instance.map_stats.custom_data.set("cheated_on_amount", amount + 1);
    }
    
    public static void increaseBrokenUp(this MapBox instance)
    {
        instance.map_stats.custom_data.get("broken_up_amount", out int amount);
        instance.map_stats.custom_data.set("broken_up_amount", amount + 1);
    }
    
    public static void increaseAdoptedBaby(this MapBox instance)
    {
        instance.map_stats.custom_data.get("broken_up_amount", out int amount);
        instance.map_stats.custom_data.set("broken_up_amount", amount + 1);
    }

    public static int countCheated<TData>(this CoreSystemObject<TData> instance) where TData : BaseSystemData
    {
        instance.data.get("cheated_on_amount", out int amount);
        return amount;
    }
    
    public static int countBrokenUp<TData>(this CoreSystemObject<TData> instance) where TData : BaseSystemData
    {
        instance.data.get("broken_up_amount", out int amount);
        return amount;
    }
    
    public static int countAdoptedBaby<TData>(this CoreSystemObject<TData> instance) where TData : BaseSystemData
    {
        instance.data.get("adopted_amount", out int amount);
        return amount;
    }
    
    public static int countCheated(this MapBox box)
    {
        box.map_stats.custom_data.get("cheated_on_amount", out int cheatedAmount);
        return cheatedAmount;
    }

    public static int countBrokenUp(this MapBox box)
    {
        box.map_stats.custom_data.get("broken_up_amount", out int brokenUpCount);
        return brokenUpCount;
    }
    
    public static int countAdoptedBaby(this MapBox box)
    {
        box.map_stats.custom_data.get("adopted_amount", out int adoptedCount);
        return adoptedCount;
    }

    public static long countOrientation(this WorldObject world, string id, bool sexual)
    {
        return world.units
            .LongCount(actor => actor.isAlive() && Orientations.HasOrientation(actor, id, sexual));
    }
    public static long countOrientation(this IMetaObject instance, string id, bool sexual)
    {
        return instance.getUnits().LongCount(unit => unit.isAlive() && Orientations.HasOrientation(unit, id, sexual));
    }

    public static long countOrientation(this Alliance instance, string id, bool sexual)
    {
        return instance.kingdoms_list.Sum(kingdom => kingdom.countOrientation(id, sexual));
    }
    public static long countLonely(this IMetaObject instance)
    {
        return instance.getUnits().LongCount(unit => unit.getIntimacy() < 0 && unit.AffectedByIntimacy());
    }
    public static long countLonely(this WorldObject world)
    {
        return world.units.LongCount(unit => unit.getIntimacy() < 0 && unit.AffectedByIntimacy());
    }

    public static void showSplitPopulationByOrientation<TMetaObject, TData>(this WindowMetaGeneric<TMetaObject, TData> instance, ICollection<Actor> pListWithUnits, bool sexual)
    where TMetaObject : CoreSystemObject<TData>
    where TData : BaseSystemData
    {
        Dictionary<_Orientation, int> dictionary = CollectionPool<Dictionary<_Orientation, int>, KeyValuePair<_Orientation, int>>.Get();
        var unitsCount = pListWithUnits.Count;
        foreach (Actor pListWithUnit in pListWithUnits)
        {
            var orientation = Orientations.GetOrientationForActorBasedOnCriteria(pListWithUnit, sexual);
            if (!dictionary.ContainsKey(orientation))
                dictionary.Add(orientation, 0);
            dictionary[orientation]++;
        }
        foreach (KeyValuePair<_Orientation, int> keyValuePair in dictionary.OrderByDescending((kv => kv.Value)))
        {
            var key = keyValuePair.Key;
            int num2 = keyValuePair.Value;
            float pFloat = unitsCount > 0 ? (float) ((double) num2 / unitsCount * 100.0) : 0.0f;
            if (unitsCount == num2)
                pFloat = 100f;
            string pValue = $"[{num2}] {pFloat.ToText()}%";
            instance.showStatRow(
                sexual ? key.SexualPathLocale : key.RomanticPathLocale, 
                pValue, 
                key.HexCode, 
                MetaType.None, 
                -1, 
                true, 
                key.GetPathIcon(sexual, false));
        }
        CollectionPool<Dictionary<_Orientation, int>, KeyValuePair<_Orientation, int>>.Release(dictionary);
    }
    
    public static _Orientation getMainOrientation(this Kingdom kingdom, bool sexual)
    {
        if (kingdom.hasKing())
            return _Orientation.GetOrientation(kingdom.king, sexual);
        return kingdom.units.Count == 0 ? null : _Orientation.GetOrientation(kingdom.units[0], sexual);
    }
    
    public static _Orientation getMainOrientation(this City city, bool sexual)
    {
        if (city.hasLeader())
            return _Orientation.GetOrientation(city.leader, sexual);
        return city.getPopulationPeople() == 0 ? null : _Orientation.GetOrientation(city.units[0], sexual);
    }
    
    public static _Orientation getFounderOrientation(this Kingdom kingdom)
    {
        kingdom.data.get("founder_orientation", out string id);
        return _Orientation.GetOrientation(id);
    }
    
    public static _Orientation getFounderOrientation(this City city)
    {
        city.data.get("founder_orientation", out string id);
        return _Orientation.GetOrientation(id);
    }
}