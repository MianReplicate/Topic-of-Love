
using System.Collections.Generic;

namespace Better_Loving
{
    public class ClanTraits
    {
        public static void Init()
        {
            Add(new ClanTrait
            {
                id = "clanbound_isolation",
                group_id = "special",
                needs_to_be_explored = true,
                rarity = Rarity.R2_Epic,
                can_be_in_book = true,
                can_be_removed = true,
                can_be_given = true
            }, List.Of("human", "demon", "evil_mage", "civ_wolf"));
        }

        private static void Add(ClanTrait trait, List<string> actorAssets = null, List<string> biomeAssets = null)
        {
            trait.path_icon = "ui/Icons/clan_traits/" + trait.id;
            AssetManager.clan_traits.add(trait);
            if (actorAssets != null)
                foreach (var asset in actorAssets)
                {
                    var actorAsset = AssetManager.actor_library.get(asset);
                    if (actorAsset != null)
                        actorAsset.addClanTrait(trait.id);
                }

            if (biomeAssets != null)
                foreach (var asset in biomeAssets)
                {
                    var biomeAsset = AssetManager.biome_library.get(asset);
                    if (biomeAsset != null)
                        biomeAsset.addClanTrait(trait.id);
                }
        }
    }
}