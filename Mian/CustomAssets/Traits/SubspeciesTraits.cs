﻿using System.Collections.Generic;
using System.Linq;

namespace Topic_of_Love.Mian.CustomAssets.Traits
{
    public class SubspeciesTraits : BaseTraits<SubspeciesTrait, SubspeciesTraitLibrary>
    {
        public void Init()
        {
            Init("subspecies");
            
            // preservation is a mustttttttt for preventing population collapse
            Add(new SubspeciesTrait
            {
                id="preservation",
                group_id = "mind",
                rarity = Rarity.R0_Normal,
                spawn_random_trait_allowed = false,
                in_mutation_pot_add = true,
                in_mutation_pot_remove = true,
                remove_for_zombies = true
            }, AssetManager.actor_library.list.Select(asset => asset.id));

            SubspeciesTrait reproductionSameSex = new SubspeciesTrait
            {
                id = "reproduction_same_sex",
                group_id = "reproductive_methods",
                rarity = Rarity.R1_Rare,
                priority = 100,
                spawn_random_trait_allowed = false,
                in_mutation_pot_add = true,
                remove_for_zombies = true
            };
            reproductionSameSex.base_stats = new BaseStats();
            reproductionSameSex.base_stats["birth_rate"] = 3f;
            reproductionSameSex.addDecision("sexual_reproduction_try");
            reproductionSameSex.addDecision("find_lover");
            
            reproductionSameSex.base_stats_meta = new BaseStats();
            reproductionSameSex.base_stats_meta.addTag("needs_mate");
            
            reproductionSameSex.addOpposites(new[]{"reproduction_sexual"});
            reproductionSameSex.addOpposites(AssetManager.subspecies_traits.get("reproduction_sexual").opposite_list);
            foreach (var opposite in AssetManager.subspecies_traits.get("reproduction_sexual").opposite_list)
            {
                AssetManager.subspecies_traits.get(opposite).opposite_traits.Add(reproductionSameSex);
            }
            
            AssetManager.subspecies_traits.get("reproduction_sexual").opposite_traits.Add(reproductionSameSex);
            
            Add(reproductionSameSex, new[]{"skeleton"});
            AssetManager.actor_library.get("skeleton").addSubspeciesTrait("reproduction_strategy_viviparity");
            
            Finish();
        }

        protected override void Finish()
        {
            foreach (SubspeciesTrait pObject in _assets)
            {
                if (pObject.in_mutation_pot_add)
                    AssetManager.subspecies_traits._pot_mutation_traits_add.AddTimes(pObject.rarity.GetRate(), pObject);
                if (pObject.in_mutation_pot_remove)
                    AssetManager.subspecies_traits._pot_mutation_traits_remove.AddTimes(pObject.rarity.GetRate(), pObject);
                if (pObject.phenotype_egg && pObject.after_hatch_from_egg_action != null)
                    pObject.has_after_hatch_from_egg_action = true;
            }
            base.Finish();
        }
    }
}