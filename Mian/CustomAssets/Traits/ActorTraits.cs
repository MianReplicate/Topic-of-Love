using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Topic_of_Love.Mian.CustomAssets.Traits
{
    public class ActorTraits
    {
        public static ActorTrait Add(ActorTrait trait)
        {
            trait.path_icon = "ui/Icons/actor_traits/" + trait.id;
            AssetManager.traits.add(trait);
            return trait;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActorTraitLibrary), nameof(ActorTraitLibrary.init))]
        public static void Init()
        {
            Debug.Log("ADDING TRAITS");
            Add(new ActorTrait
            {
                id = "unfluid",
                group_id = "mind",
                rate_birth = 8,
                type = TraitType.Other,
                unlocked_with_achievement = false,
                rarity = Rarity.R1_Rare,
                needs_to_be_explored = true
            });
            Add(new ActorTrait
            {
                id = "intimacy_averse",
                group_id = "mind",
                rate_birth = 2,
                rate_acquire_grow_up = 4,
                type = TraitType.Other,
                unlocked_with_achievement = false,
                rarity = Rarity.R1_Rare,
                needs_to_be_explored = true
            });
            
            Add(new ActorTrait
            {
                id = "faithful",
                group_id = "mind",
                rate_birth = 2,
                rate_acquire_grow_up = 4,
                type = TraitType.Positive,
                unlocked_with_achievement = false,
                rarity = Rarity.R1_Rare,
                needs_to_be_explored = true,
            }).addOpposite("unfaithful");
            Add(new ActorTrait
            {
                id = "unfaithful",
                group_id = "mind",
                rate_birth = 2,
                rate_acquire_grow_up = 4,
                type = TraitType.Negative,
                unlocked_with_achievement = false,
                rarity = Rarity.R1_Rare,
                needs_to_be_explored = true,
            }).addOpposite("faithful");
        }
    }
}