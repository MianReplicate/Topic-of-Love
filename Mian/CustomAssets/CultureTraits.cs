
using System.Collections.Generic;

namespace Better_Loving
{
    public class CultureTraits
    {
        public static void Init()
        {
            var orientationLess = new CultureTrait
            {
                id = "orientationless",
                group_id = "miscellaneous",
                needs_to_be_explored = true,
                rarity = Rarity.R1_Rare,
                can_be_in_book = true,
                can_be_removed = true,
                can_be_given = true
            };

            var homophobic = new CultureTrait
            {
                id = "homophobic",
                group_id = "worldview",
                needs_to_be_explored = true,
                rarity = Rarity.R1_Rare,
                can_be_in_book = true,
                can_be_removed = true,
                can_be_given = true
            };

            var heterophobic = new CultureTrait
            {
                id = "heterophobic",
                group_id = "worldview",
                needs_to_be_explored = true,
                rarity = Rarity.R1_Rare,
                can_be_in_book = true,
                can_be_removed = true,
                can_be_given = true
            };

            homophobic.opposite_traits = new HashSet<CultureTrait>();
            homophobic.opposite_traits.Add(orientationLess);
            
            heterophobic.opposite_traits = new HashSet<CultureTrait>();
            heterophobic.opposite_traits.Add(orientationLess);
            
            orientationLess.opposite_traits = new HashSet<CultureTrait>();
            orientationLess.opposite_traits.Add(homophobic);
            orientationLess.opposite_traits.Add(heterophobic);
            
            // Add(homophobic, List.Of("orc", "demon"), List.Of("biome_swamp", "biome_infernal", "biome_corrupted"));
            // Add(heterophobic, List.Of("flower_bud", "garl"), List.Of("biome_candy"));
            // Add(orientationLess, List.Of("angle", "snowman"), List.Of("biome_crystal"));
            
            Add(homophobic, List.Of("orc", "demon"));
            Add(heterophobic, List.Of("flower_bud", "garl"));
            Add(orientationLess, List.Of("angle", "snowman"));
            // now influenced by reproduction methods

            CultureTrait incestTaboo = new CultureTrait
            {
                id = "incest_taboo",
                group_id = "miscellaneous",
                needs_to_be_explored = true,
                rarity = Rarity.R1_Rare,
                can_be_in_book = true,
                can_be_removed = true,
                can_be_given = true,
                opposite_traits = new HashSet<CultureTrait>()
            };

            CultureTrait scarOfIncest = new CultureTrait
            {
                id = "scar_of_incest",
                group_id = "miscellaneous",
                rarity = Rarity.R1_Rare,
                can_be_given = true,
                can_be_in_book = false,
                can_be_removed = true,
                needs_to_be_explored = true,
                opposite_traits = new HashSet<CultureTrait>()
            };

            incestTaboo.opposite_traits.Add(scarOfIncest);
            scarOfIncest.opposite_traits.Add(incestTaboo);

            Add(incestTaboo, List.Of("human", "civ_dog", "elf", "white_mage", "druid", "civ_penguin", "civ_capybara"), List.Of("biome_celestial"));

            Add(scarOfIncest, List.Of("orc", "demon", "dog", "rat", "civ_rat", "rabbit", "hyena", "civ_hyena", "evil_mage", "sheep", "cow"), List.Of("biome_infernal", "biome_corrupted"));

            Add(new CultureTrait
            {
                id = "committed",
                group_id = "miscellaneous",
                needs_to_be_explored = true,
                rarity = Rarity.R1_Rare,
                can_be_in_book = true,
                can_be_removed = true,
                can_be_given = true
            }, List.Of("elf", "civ_penguin"), List.Of("biome_celestial", "biome_flower"));
            
            Add(new CultureTrait
            {
                id = "mature_dating",
                group_id = "miscellaneous",
                needs_to_be_explored = true,
                rarity = Rarity.R1_Rare,
                can_be_in_book = true,
                can_be_removed = true,
                can_be_given = true
            }, List.Of("human", "elf", "dwarf"), List.Of("biome_grass", "biome_maple"));

            Add(new CultureTrait
            {
                id = "sexual_expectations",
                group_id = "miscellaneous",
                needs_to_be_explored = true,
                rarity = Rarity.R1_Rare,
                can_be_in_book = true,
                can_be_removed = true,
                can_be_given = true
            }, List.Of("elf"), List.Of("biome_maple"));
        }

        private static void Add(CultureTrait trait, List<string> actorAssets=null, List<string> biomeAssets=null)
        {
            trait.path_icon = "ui/Icons/culture_traits/" + trait.id;
            AssetManager.culture_traits.add(trait);
            if(actorAssets != null)
                foreach (var asset in actorAssets)
                {
                    var actorAsset = AssetManager.actor_library.get(asset);
                    if(actorAsset != null)
                        actorAsset.addCultureTrait(trait.id);
                }
            
            if(biomeAssets != null)
                foreach (var asset in biomeAssets)
                {
                    var biomeAsset = AssetManager.biome_library.get(asset);
                    if(biomeAsset != null)
                        biomeAsset.addCultureTrait(trait.id);
                }
        }
    }
}