using System.Collections.Generic;

namespace Better_Loving
{
    public class SubspeciesTraits
    {
        private static List<SubspeciesTrait> traits = new List<SubspeciesTrait>();
        public static void Init()
        {
            Add(new SubspeciesTrait
            {
                id="preservation",
                group_id = "mind",
                rarity = Rarity.R0_Normal,
                in_mutation_pool_add = true,
                in_mutation_pool_remove = true,
                remove_for_zombies = true
            });

            // preservation is a mustttttttt for preventing population collapse
            foreach (var actorAsset in AssetManager.actor_library.list)
            {
                actorAsset.addSubspeciesTrait("preservation");
            }

            Add(new SubspeciesTrait
            {
                id = "incest",
                group_id = "mind",
                rarity = Rarity.R1_Rare,
                in_mutation_pool_add = true,
                in_mutation_pool_remove = true,
                remove_for_zombies = true
            }, List.Of("human", "dog", "orc", "demon", "civ_dog", "rabbit", "civ_rabbit", "hyena", "civ_hyena", "druid", "rat", "civ_rat"));

            SubspeciesTrait reproductionSameSex = new SubspeciesTrait
            {
                id = "reproduction_same_sex",
                group_id = "reproductive_methods",
                rarity = Rarity.R1_Rare,
                priority = 100,
                in_mutation_pool_add = true,
                remove_for_zombies = true
            };
            reproductionSameSex.base_stats = new BaseStats();
            reproductionSameSex.base_stats["birth_rate"] = 3f;
            reproductionSameSex.addDecision("sexual_reproduction_try");
            reproductionSameSex.addDecision("find_lover");
            
            reproductionSameSex.base_stats_meta = new BaseStats();
            reproductionSameSex.base_stats_meta.addTag("needs_mate");
            
            reproductionSameSex.opposite_traits = new HashSet<SubspeciesTrait>();
            reproductionSameSex.opposite_traits.Add(AssetManager.subspecies_traits.get("reproduction_sexual"));
            reproductionSameSex.opposite_traits.Add(AssetManager.subspecies_traits.get("reproduction_hermaphroditic"));
            
            AssetManager.subspecies_traits.get("reproduction_sexual").opposite_traits.Add(reproductionSameSex);
            AssetManager.subspecies_traits.get("reproduction_hermaphroditic").opposite_traits.Add(reproductionSameSex);

            reproductionSameSex.unlock(); // for now it's unlocked auto cuz i dont know what to put this on
            
            Add(reproductionSameSex);
            
            Finish();
        }

        public static void Finish()
        {
            foreach (SubspeciesTrait pObject in traits)
            {
                if (pObject.in_mutation_pool_add)
                    AssetManager.subspecies_traits._pool_mutation_traits_add.AddTimes(pObject.rarity.GetRate(), pObject);
                if (pObject.in_mutation_pool_remove)
                    AssetManager.subspecies_traits._pool_mutation_traits_remove.AddTimes(pObject.rarity.GetRate(), pObject);
                if (pObject.phenotype_egg && pObject.after_hatch_from_egg_action != null)
                    pObject.has_after_hatch_from_egg_action = true;
            }
        }
        
        private static void Add(SubspeciesTrait trait, List<string> assets = null)
        {
            trait.path_icon = "ui/Icons/subspecies_traits/" + trait.id;
            AssetManager.subspecies_traits.add(trait);
            if (assets != null)
            {
                foreach (string asset in assets)
                {
                    ActorAsset actorAsset = AssetManager.actor_library.get(asset);
                    if(actorAsset != null)
                        actorAsset.addSubspeciesTrait(trait.id);
                }
            }
            traits.Add(trait);
        }
    }
}