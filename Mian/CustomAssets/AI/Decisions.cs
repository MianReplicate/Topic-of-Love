using System.Collections.Generic;
using Topic_of_Love.Mian.CustomAssets.Traits;
using HarmonyLib;
using Topic_of_Love.Mian.CustomAssets.Custom;

namespace Topic_of_Love.Mian.CustomAssets.AI;

public class Decisions
{
    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(DecisionsLibrary), nameof(DecisionsLibrary.init))]
    public static void Init()
    {
        Add(new DecisionAsset
        {
            id = "insult_orientation_try",
            task_id = "insult_orientation",
            priority = NeuroLayer.Layer_2_Moderate,
            path_icon = "ui/Icons/culture_traits/orientationless",
            cooldown = 30,
            action_check_launch = actor => (actor.hasCultureTrait("homophobic") || actor.hasCultureTrait("heterophobic")) && TolUtil.IsOrientationSystemEnabledFor(actor),
            weight = 0.5f,
            list_civ = true,
            only_safe = true
        });
        AssetManager.subspecies_traits.get("wernicke_area").addDecision("insult_orientation_try");

        Add(new DecisionAsset
        {
            id = "find_kiss",
            priority = NeuroLayer.Layer_2_Moderate,
            path_icon = "ui/Icons/status/just_kissed",
            cooldown = 15,
            action_check_launch = actor => TolUtil.CapableOfLove(actor)
                                           && (actor.hasLover() || actor.hasBestFriend())
                                           && !TolUtil.IsIntimacyHappinessEnough(actor, 100f)
                                           && TolUtil.IsOrientationSystemEnabledFor(actor)
                                           && !actor.hasStatus("just_kissed"),
            weight_calculate_custom = actor => TolUtil.IsIntimacyHappinessEnough(actor, 75f) ? 0.5f: 
                TolUtil.IsIntimacyHappinessEnough(actor, 50f) ? 0.6f : TolUtil.IsIntimacyHappinessEnough(actor, 0) ? .8f : 
                TolUtil.IsIntimacyHappinessEnough(actor, -50) ? 1f : TolUtil.IsIntimacyHappinessEnough(actor, -100f) ? 1.5f : 1.25f,
            only_safe = true,
            cooldown_on_launch_failure = true
        });
        
        Add(new DecisionAsset
        {
            id = "find_date",
            priority = NeuroLayer.Layer_2_Moderate,
            path_icon = "ui/Icons/status/went_on_date",
            cooldown = 30,
            action_check_launch = actor => TolUtil.CapableOfLove(actor)
                                           && !TolUtil.IsIntimacyHappinessEnough(actor, 100f)
                                           && TolUtil.IsOrientationSystemEnabledFor(actor)
                                           && !actor.hasStatus("went_on_date"),
            weight_calculate_custom = actor => !actor.hasLover() ? 1.5f : TolUtil.IsIntimacyHappinessEnough(actor, 75f) ? 0.5f: 
                TolUtil.IsIntimacyHappinessEnough(actor, 50f) ? 0.6f : TolUtil.IsIntimacyHappinessEnough(actor, 0) ? .8f : 
                TolUtil.IsIntimacyHappinessEnough(actor, -50) ? 1f : TolUtil.IsIntimacyHappinessEnough(actor, -100f) ? 1.5f : 1.25f,
            only_safe = true,
            cooldown_on_launch_failure = true
        });
        
        Add(new DecisionAsset
        {
            id = "reproduce_preservation",
            priority = NeuroLayer.Layer_3_High,
            path_icon = "ui/Icons/status/disliked_sex",
            cooldown = 20,
            action_check_launch = actor =>
            {
                actor.subspecies.countReproductionNeuron();
                return TolUtil.IsDyingOut(actor)
                       && BabyHelper.canMakeBabies(actor)
                       && actor.hasSubspeciesTrait("preservation")
                       && TolUtil.IsOrientationSystemEnabledFor(actor);
            },
            weight_calculate_custom = actor => BabyHelper.canMakeBabies(actor) ? 2f : 0.1f,
            only_adult = true,
            only_safe = true,
            cooldown_on_launch_failure = true
        });
        // will force all units to make babies regardless of orientation if they have preservation
        AssetManager.subspecies_traits.get("reproduction_sexual").addDecision("reproduce_preservation");
        AssetManager.subspecies_traits.get("reproduction_same_sex").addDecision("reproduce_preservation");
        AssetManager.subspecies_traits.get("reproduction_hermaphroditic").addDecision("reproduce_preservation");
        
        Add(new DecisionAsset
        {
            id = "invite_for_sex",
            priority = NeuroLayer.Layer_2_Moderate,
            path_icon = "ui/Icons/status/enjoyed_sex",
            cooldown = 15,
            action_check_launch = actor => TolUtil.CapableOfLove(actor)
                                           && !Preferences.Dislikes(actor, true)
                                           && !TolUtil.IsIntimacyHappinessEnough(actor, 100f)
                                           && TolUtil.IsOrientationSystemEnabledFor(actor),
            weight_calculate_custom = actor => TolUtil.IsIntimacyHappinessEnough(actor, 75f) ? 0.25f: 
                TolUtil.IsIntimacyHappinessEnough(actor, 50f) ? 0.5f : TolUtil.IsIntimacyHappinessEnough(actor, 0) ? .75f : 
                TolUtil.IsIntimacyHappinessEnough(actor, -50) ? 1f : TolUtil.IsIntimacyHappinessEnough(actor, -100f) ? 1.5f : 1.25f,
            only_adult = true,
            only_safe = true,
            cooldown_on_launch_failure = true
        });

        Add(new DecisionAsset
        {
            id = "find_sexual_ivf",
            priority = NeuroLayer.Layer_2_Moderate,
            path_icon = "ui/Icons/status/adopted_baby",
            cooldown = 10,
            action_check_launch = actor =>
            {
                if (!actor.isSapient() || !TolUtil.WantsBaby(actor, false))
                    return false;
                    
                var bestFriend = actor.getBestFriend();

                if (actor.hasLover())
                {
                    if (!TolUtil.WantsBaby(actor.lover, false))
                        return false;

                    if (TolUtil.CouldReproduce(actor, actor.lover) &&
                        !Preferences.BothActorsPreferenceMatch(actor, actor.lover, true))
                    {
                        return true;
                    }

                    if (TolUtil.CouldReproduce(actor, actor.lover) &&
                        Preferences.BothActorsPreferenceMatch(actor, actor.lover, true))
                        return false;
                }

                bool success = bestFriend != null && TolUtil.CouldReproduce(actor, bestFriend) &&
                               !bestFriend.hasStatus("pregnant");
                return success;
            },
            list_civ = true,
            weight = 1.5f,
            only_safe = true,
            only_adult = true,
            cooldown_on_launch_failure = true
        });
    }

    
    private static void Add(DecisionAsset asset)
    {
        AssetManager.decisions_library.add(asset);
    }
}