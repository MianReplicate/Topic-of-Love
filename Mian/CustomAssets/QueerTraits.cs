using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

namespace Better_Loving
{
    public enum Preference
    {
        All,
        SameSex,
        DifferentSex,
        SameOrDifferentSex,
        Neither,
        Inapplicable
    }

    public class QueerTrait : ActorTrait
    {
        public Preference preference;
    }
    
    // this will be improved upon in the future cuz it's not perfect representation
    public class QueerTraits
    {
        private static List<QueerTrait> _sexualityTraits = new List<QueerTrait>();
        private static List<QueerTrait> _romanticTraits = new List<QueerTrait>();
        private static List<QueerTrait> _allTraits = new List<QueerTrait>();
        public static void Init()
        {
            // maybe add actions to break up when romantic traits change
            AddQueerTrait("heterosexual", Preference.DifferentSex, true);
            AddQueerTrait("homosexual", Preference.SameSex, true);
            AddQueerTrait("bisexual", Preference.SameOrDifferentSex, true);
            AddQueerTrait("asexual", Preference.Neither, true);
            AddQueerTrait("abrosexual", Preference.Inapplicable, true);
            
            AddQueerTrait("heteroromantic", Preference.DifferentSex, false);
            AddQueerTrait("homoromantic", Preference.SameSex, false);
            AddQueerTrait("biromantic", Preference.SameOrDifferentSex, false);
            AddQueerTrait("aromantic", Preference.Neither, false);
            AddQueerTrait("abroromantic", Preference.Inapplicable, false);
            
            Finish();
        }

        public static void Finish()
        {
            foreach(var trait in _sexualityTraits)
            {
                if (trait.preference == Preference.Inapplicable) continue;
                
                trait.opposite_traits = new HashSet<ActorTrait>();
                foreach (var traitToAdd in _sexualityTraits)
                {
                    if (trait == traitToAdd || traitToAdd.preference == Preference.Inapplicable) continue;
                    trait.opposite_traits.Add(traitToAdd);
                }

                // trait.traits_to_remove_ids = trait.opposite_list.ToArray();
            }
            
            foreach(var trait in _romanticTraits)
            {
                if (trait.preference == Preference.Inapplicable) continue;
                
                trait.opposite_traits = new HashSet<ActorTrait>();
                foreach (var traitToAdd in _romanticTraits)
                {
                    if (trait == traitToAdd || traitToAdd.preference == Preference.Inapplicable) continue;
                    trait.opposite_traits.Add(traitToAdd);
                }
                
                // trait.traits_to_remove_ids = trait.opposite_list.ToArray();
            }
            _allTraits = _sexualityTraits.ConvertAll(trait => trait);
            _allTraits.AddRange(_romanticTraits);
        }

        public static void CleanQueerTraits(Actor pActor, bool sexual, bool clearInapplicable=false)
        {
            var list = (sexual ? _sexualityTraits : _romanticTraits).Where(trait => !trait.preference.Equals(Preference.Inapplicable) || clearInapplicable).ToList();
            foreach (var trait in list)
            {
                pActor.removeTrait(trait);
            }
        }

        public static bool HasQueerTraits(Actor pActor)
        {
            foreach (var trait in _allTraits)
            {
                if (pActor.hasTrait(trait))
                {
                    return true;
                }
            }

            return false;
        }

        public static List<QueerTrait> GetQueerTraits(Actor pActor, bool excludeInapplicable=false)
        {
            List<QueerTrait> list = new List<QueerTrait>();
            foreach (var trait in _sexualityTraits)
            {
                if (pActor.hasTrait(trait) && (!trait.preference.Equals(Preference.Inapplicable) || !excludeInapplicable))
                {
                    list.Add(trait);
                }
            }
            foreach (var trait in _romanticTraits)
            {
                if (pActor.hasTrait(trait) && (!trait.preference.Equals(Preference.Inapplicable) || !excludeInapplicable))
                {
                    list.Add(trait);
                }
            }

            return list;
        }

        public static void GiveQueerTraits(Actor pActor, bool equalChances, bool clearInapplicable = false)
        {
            if (!pActor.hasSubspecies()) return;
            var currentTraits = GetQueerTraits(pActor);
            foreach (var trait in currentTraits)
            {
                if (!clearInapplicable && trait.preference.Equals(Preference.Inapplicable)) continue;
                pActor.removeTrait(trait);
            }
            
            var queerTraits = RandomizeQueerTraits(pActor, equalChances, currentTraits);
            for(int i = 0; i < queerTraits.Count; i++)
            {
                if (queerTraits[i] != null)
                {
                    pActor.addTrait(queerTraits[i]);
                }
            }
        }

        public static Preference GetSexualPrefBasedOnReproduction(Actor pActor)
        {
            if (!pActor.hasSubspecies()) return Preference.Neither;
            if (pActor.subspecies.hasTraitReproductionSexual())
                return Preference.DifferentSex;
            if (pActor.subspecies.hasTraitReproductionSexualHermaphroditic())
                return Preference.SameOrDifferentSex;
            if (pActor.hasSubspeciesTrait("reproduction_same_sex"))
                return Preference.SameSex;
            return Preference.Neither;
        }

        // randomizes chances based on what actor's reproduction methods
        public static List<QueerTrait> RandomizeQueerTraits(Actor pActor, bool equalChances, List<QueerTrait> exclude)
        {
            if (!pActor.hasSubspecies()) return null;
            // randomize for sexual
            var matchingPreference = equalChances ? Preference.All : GetSexualPrefBasedOnReproduction(pActor);

            List<QueerTrait> randomPool = new List<QueerTrait>();
            foreach (var trait in _sexualityTraits)
            {
                if (exclude.Contains(trait) || trait.preference.Equals(Preference.Inapplicable)) continue;
                
                if (trait.preference.Equals(matchingPreference))
                {
                    for (int i = 0; i < 27; i++)
                    {
                        randomPool.Add(trait);
                    }
                }
                else
                {
                    randomPool.Add(trait);
                }
            }

            var sexualTrait = randomPool[Random.Range(0, randomPool.Count)];
            var romanticTrait = GetOppositeVariant(sexualTrait);
            
            // random chance that romantic trait will not fit sexuality trait
            if (Randy.randomChance(0.03f))
            {
                romanticTrait = _romanticTraits[Random.Range(0, _romanticTraits.Count)];
            }
            
            var queerTraits = List.Of(sexualTrait, romanticTrait);
            
            // randomize non-preference traits
            if (Randy.randomChance(0.05f))
            {
                randomPool = _sexualityTraits.Where(trait => trait.preference.Equals(Preference.Inapplicable)).ToList();
                queerTraits.Add(randomPool[Random.Range(0, randomPool.Count)]);
            }
            
            if (Randy.randomChance(0.05f))
            {
                randomPool = _romanticTraits.Where(trait => trait.preference.Equals(Preference.Inapplicable)).ToList();
                queerTraits.Add(randomPool[Random.Range(0, randomPool.Count)]);
            }

            return queerTraits;
        }
        
        // gets the romantic/sexual version of the trait that matches preferences
        public static QueerTrait GetOppositeVariant(QueerTrait trait)
        {
            return (_sexualityTraits.Contains(trait) ? _romanticTraits : _sexualityTraits).Find(traitInList => traitInList.preference.Equals(trait.preference));
        }
        public static Preference GetPreferenceFromActor(Actor pActor, bool sexual)
        {
            List<QueerTrait> list = sexual ? _sexualityTraits : _romanticTraits;
            foreach (QueerTrait trait in list)
            {
                if(pActor.hasTrait(trait) && !trait.preference.Equals(Preference.Inapplicable))
                    return trait.preference;
            }

            return Preference.Inapplicable; // if they have no preference, then they like neither
        }

        public static bool IsConsideredHomo(Actor pActor, bool sexual)
        {
            var preference = GetPreferenceFromActor(pActor, sexual);
            return preference.Equals(Preference.SameSex) || preference.Equals(Preference.DifferentSex) || preference.Equals(Preference.All);
        }
        
        // Important note that this checks FROM the first actor's point of view, you should also use PreferenceMatches on the other actor to confirm they both like each other!
        public static bool PreferenceMatches(Actor pActor, Actor pTarget, bool sexual)
        {
            if (pActor == null || pTarget == null) return false;
            var preference = GetPreferenceFromActor(pActor, sexual);
            switch (preference)
            {
                case Preference.SameOrDifferentSex:
                    return true;
                case Preference.SameSex:
                    return pActor.data.sex == pTarget.data.sex;
                case Preference.DifferentSex:
                    return pActor.data.sex != pTarget.data.sex;
                default:
                    return false;
            }
        }

        public static bool BothPreferencesMatch(Actor actor1, Actor actor2, bool sexual)
        {
            return PreferenceMatches(actor1, actor2, sexual) && PreferenceMatches(actor2, actor1, sexual);
        }

        internal static void AddQueerTrait(string name, Preference preference, bool sexual)
        {
            var trait = new QueerTrait
            {
                id = name,
                path_icon = "ui/Icons/actor_traits/" + name,
                group_id = "mind",
                rate_birth = 0,
                rate_acquire_grow_up = 0,
                type = TraitType.Other,
                unlocked_with_achievement = false,
                rarity = Rarity.R3_Legendary,
                needs_to_be_explored = true,
                affects_mind = true,
                preference = preference
            };
            if(sexual)
                _sexualityTraits.Add(trait);
            else
            {
                _romanticTraits.Add(trait);
            }

            Util.AddActorTrait(trait, null);
        }
    }
}