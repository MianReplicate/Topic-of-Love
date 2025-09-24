using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HarmonyLib;
using JetBrains.Annotations;
using NCMS.Extensions;
using NeoModLoader.General;
using NeoModLoader.services;
using UnityEngine;
using UnityEngine.UI;

namespace Topic_of_Love.Mian.CustomAssets.Custom
{
    public enum LoveType
    {
        Both,
        Sexual,
        Romantic
    }
    public class LikeGroup
    {
        public readonly string ID;
        public readonly string HexCode;
        public readonly Func<LikeAsset, GameObject> GetDynamicIcon; // for dynamic assets
        public readonly Func<LikeAsset, Sprite> GetDynamicSprite;
        public readonly Func<Actor, string[]> ActorType;

        public LikeGroup(string id, string hexCode, Func<Actor, string[]> actorType, Func<LikeAsset, GameObject> getDynamicIcon, Func<LikeAsset, Sprite> getDynamicSprite)
        {
            ID = id.ToLower();
            HexCode = hexCode;
            GetDynamicIcon = getDynamicIcon;
            GetDynamicSprite = getDynamicSprite;
            ActorType = actorType;
        }

        public string Title => LM.Get("like_group_" + ID);
        public override bool Equals(object obj)
        {
            return obj is LikeGroup compare && compare.ID.Equals(ID);
        }

        public override int GetHashCode() => ID.GetHashCode();
    }
    public class LikeAsset
    {
        public readonly string ID;
        public readonly LikeGroup LikeGroup;
        public readonly bool IsDynamic;

        public LoveType ApplicableLoveType;
        
        public LikeAsset(string id, LikeGroup likeGroup, LoveType applicable, bool isDynamic = false)
        {
            ID = id;
            LikeGroup = likeGroup;
            ApplicableLoveType = applicable;
            IsDynamic = isDynamic;
        }
        
        public override bool Equals(object obj)
        {
            return obj is LikeAsset compare && compare.ID.Equals(ID);
        }

        public override int GetHashCode() => ID.GetHashCode();
    }

    public class Like
    {
        public readonly LikeAsset LikeAsset;
        public readonly LoveType LoveType;

        private string SpecificLoveString => LoveType.Equals(LoveType.Sexual) ? "sexual" : "romantic";
        public string ID => LikeAsset.ID;

        public string IDWithLoveType => LikeAsset.ID + "_" + SpecificLoveString;
        public string Title => LM.Get("like_"+LikeAsset.ID+"_"+SpecificLoveString);
        public string Description => LM.Get("like_"+LikeAsset.ID+"_"+SpecificLoveString+"_info");
        public string Description2 => LM.Get("like_"+LikeAsset.ID+"_"+SpecificLoveString+"_info_2");

        public Sprite GetSprite()
        {
            return LikeAsset.LikeGroup.GetDynamicSprite != null ? LikeAsset.LikeGroup.GetDynamicSprite(LikeAsset) : SpriteTextureLoader.getSprite("ui/Icons/likes/" + ID);
        }

        public GameObject GetIcon()
        {
            GameObject mainHolder;

            if (LikeAsset.LikeGroup.GetDynamicIcon != null)
            {
                mainHolder = LikeAsset.LikeGroup.GetDynamicIcon(LikeAsset);
            }
            else
            {
                var sprite = GetSprite();

                mainHolder = new GameObject();
                mainHolder.transform.localScale = new Vector3(0, 0);
                mainHolder.AddOrGetComponent<RectTransform>();
                mainHolder.AddOrGetComponent<Image>().sprite = sprite;
            }
            mainHolder.name = "LikeIconHolder";   
            
            var sprite2 = SpriteTextureLoader.getSprite("ui/Icons/likes/" + (LoveType == LoveType.Sexual ? "sexual" : "romantic"));
            
            var secondaryHolder = new GameObject();
            secondaryHolder.transform.SetParent(mainHolder.transform);
            secondaryHolder.AddOrGetComponent<RectTransform>();
            secondaryHolder.AddComponent<Image>();
            if (LikeAsset.ApplicableLoveType == LoveType.Both)
                secondaryHolder.GetComponent<Image>().sprite = sprite2;
            else
                secondaryHolder.GetComponent<Image>().enabled = false;
            secondaryHolder.transform.localPosition = new Vector3(25, -30);
            secondaryHolder.transform.localScale =  new Vector3(1f, 1f);
            secondaryHolder.name = "LoveTypeHolder";
            
            return mainHolder;
        }

        public Like(LikeAsset asset, LoveType type)
        {
            if (type == LoveType.Both)
                throw new Exception("Likes cannot have a LoveType of both!");
            LikeAsset = asset;
            LoveType = type;
        }
        
        public override bool Equals(object obj)
        {
            return obj is Like compare && compare.LikeAsset.Equals(LikeAsset) && compare.LoveType.Equals(LoveType);
        }

        public override int GetHashCode() =>
            (LikeAsset, LoveType).GetHashCode();
    }
    public static class LikesManager
    {
        private static readonly Dictionary<(LikeAsset, LoveType), Like> CachedLikes = new();
        private static readonly Dictionary<string, LikeGroup> LikeGroups = new();
        private static readonly List<LikeAsset> AllLikeAssets = new();
        public static readonly Dictionary<LikeGroup, List<LikeAsset>> LikeTypes = new();
        private static readonly Dictionary<string, List<string>> MatchingSets = new();
        private static readonly Dictionary<LoveType, List<Like>> CachedLikeLists = new();
        private static readonly Dictionary<(string, LoveType), List<Like>> CachedLikeListsWithGroup = new();

        // private static readonly Func<LikeAsset, Sprite> GetSubspeciesSprite = asset =>
        // {
        //     var subspecies = MapBox.instance.subspecies.get(long.Parse(asset.ID));
        //     return subspecies.getSpriteIcon();
        // };
        //
        // private static readonly Func<LikeAsset, GameObject> GetSubspeciesIcon = asset =>
        // {
        //     var subspeciesBanner = GameObject.Find("Canvas Container Main/Canvas - Windows/windows/unit/Background/Scroll View/Viewport/Content/content_meta/Meta Elements/PrefabBannerSubspecies");
        //     var actualObject = GameObject.Instantiate(subspeciesBanner.transform.GetChild(0));
        //     GameObject.Destroy(actualObject.GetComponent<RotateOnHover>());
        //     
        //     var subspecies = MapBox.instance.subspecies.get(long.Parse(asset.ID));
        //     
        //     actualObject.GetChild(0).GetComponent<Image>().sprite = subspecies.getSpriteBackground();
        //     actualObject.GetChild(5).GetComponent<Image>().color = subspecies.getColor().getColorMain2();
        //     actualObject.GetChild(6).GetComponent<Image>().color = subspecies.getColor().getColor32Main();
        //     
        //     ActorAsset tAsset = subspecies.getActorAsset();
        //     SubspeciesTrait tMutationAsset = null;
        //     ActorTextureSubAsset tTextureAsset;
        //     if (subspecies.has_mutation_reskin)
        //     {
        //         tMutationAsset = subspecies.mutation_skin_asset;
        //         tTextureAsset = tMutationAsset.texture_asset;
        //     }
        //     else
        //     {
        //         tTextureAsset = tAsset.texture_asset;
        //     }
        //     AnimationContainerUnit tContainerAdult = DynamicActorSpriteCreatorUI.getContainerForUI(tAsset, true, tTextureAsset, tMutationAsset, false, null, null);
        //     ColorAsset tKingdomColor = AssetManager.kingdoms.get(tAsset.kingdom_id_wild).default_kingdom_color;
        //     int tPhenotypeIndex = subspecies.getMainPhenotypeIndexForBanner();
        //     int tPhenotypeShade = 0;
        //     Sprite tSprite = DynamicActorSpriteCreatorUI.getUnitSpriteForUI(tAsset, tContainerAdult.walking.frames[0], tContainerAdult, true, ActorSex.Male, tPhenotypeIndex, tPhenotypeShade, tKingdomColor, null, false);
        //     
        //     actualObject.GetChild(4).GetComponent<Image>().sprite = tSprite;
        //     actualObject.transform.localScale = new Vector3(0.09f, 0.09f);
        //
        //     // GameObject.Destroy(actualObject.GetChild(2).GetComponent<Image>());
        //     GameObject.DestroyImmediate(actualObject.GetChild(2).gameObject);
        //     
        //     return actualObject.gameObject;
        // };
        
        public static void Init()
        {
            var identities = List.Of("female", "male");

            if (TolUtil.IsTOIInstalled())
                identities.Add("nonbinary");
            
            AddLikeType("identity", "#B57EDC", identities, LoveType.Both, actor => new []{GetIdentity(actor)});
            // AddLikeType("subspecies", "#34e965", new List<string>(), LoveType.Both, actor =>
            // {
            //     if (!actor.hasSubspecies())
            //         return Array.Empty<string>();
            //     
            //     return new[] { actor.subspecies.id.ToString() };
            // }, GetSubspeciesIcon, GetSubspeciesSprite);
            
            if(TolUtil.IsTOIInstalled())
                AddLikeType("expression", "#C900FF", List.Of("feminine", "masculine"), LoveType.Both, actor => new []{GetExpression(actor)});
            
            if(TolUtil.IsTOIInstalled())
                AddLikeType("genital", "#B77E7E", List.Of("phallus", "vulva"), LoveType.Sexual, actor => actor.GetExternalGenitalia());

            AddMatchingSets();
            
            Finish();
        }

        public static void RemoveDynamicLikeAsset(long id)
        {
            var data = MapBox.instance.map_stats.custom_data;
            data.get("custom_like_"+id, out string likeID);
            if (likeID == null)
                return;
            
            TolUtil.Debug("Removed dynamic like asset: " + likeID);
            
            var likeAsset = GetAssetFromID(id.ToString());
            AllLikeAssets.Remove(likeAsset);
            
            data.removeString("custom_like_"+id);
            data.removeString(id + "_love_type");
            
            CachedLikeLists.Clear();
            CachedLikeListsWithGroup.Clear();
        }
        public static LikeAsset AddDynamicLikeAsset(long id, string likeName, string groupId, LoveType loveType, bool save = true)
        {
            var like = AllLikeAssets.Find(asset => asset.ID.Equals(id.ToString()));
            if (like != null)
                return like;
            
            TolUtil.Debug("Created dynamic like asset: " + likeName + ", " + groupId + ", " + loveType);
            
            var likeGroup = GetLikeGroup(groupId);
            like = new LikeAsset
            (
                id.ToString(),
                likeGroup,
                loveType,
                true
            );
            
            AllLikeAssets.Add(like);
            
            AddLocalesForLikeAsset(like, likeName);
            
            if (save)
            {
                var data = MapBox.instance.map_stats.custom_data;
                data.set("custom_like_" + id, likeName);
                data.set(id.ToString(), groupId);
                data.set(id + "_love_type", loveType.ToString());
            }
            
            LM.ApplyLocale(false);
            // quiet update
            foreach (LocalizedText text in LocalizedTextManager.instance.texts)
                text.updateText();
            CachedLikeLists.Clear();
            CachedLikeListsWithGroup.Clear();
            
            return like;
        }

        // called when the world loads
        private static void LoadDynamicLikeAssets()
        {
            TolUtil.LogInfo("Loading dynamic like assets...");

            AllLikeAssets.RemoveAll(asset => asset.IsDynamic);
            
            var data = MapBox.instance.map_stats.custom_data;
            if (data.custom_data_string == null)
                return;
            var search = "custom_like_";
            foreach (var key in data.custom_data_string.Keys)
            {
                if (key.StartsWith(search))
                {
                    var likeID = key.Substring(key.IndexOf(search, StringComparison.OrdinalIgnoreCase) + search.Length);
                    data.get(search+likeID, out string likeName);
                    data.get(likeID, out string groupId);
                    data.get(likeID + "_love_type", out string loveType);
                    LoveType.TryParse(loveType, out LoveType _loveType);

                    AddDynamicLikeAsset(long.Parse(likeID), likeName, groupId, _loveType, false);
                }
            }
        }

        // typically used for dynamic assets
        public static void RenameLikeAssetLocale(LikeAsset asset, string name)
        {
            LM.AddToCurrentLocale("like_" + asset.ID + "_romantic", name + " (R)");
            LM.AddToCurrentLocale("like_" + asset.ID + "_sexual", name + " (S)");
            LM.AddToCurrentLocale("like_" + asset.ID + "_romantic_info", "Romantically likes " + name);
            LM.AddToCurrentLocale("like_" + asset.ID + "_sexual_info", "Sexually likes " + name);

            LM.ApplyLocale(false);
            // quiet update
            foreach (LocalizedText text in LocalizedTextManager.instance.texts)
                text.updateText();
        }
        
        private static void AddLocalesForLikeAsset(LikeAsset asset, [CanBeNull] string forcedName=null)
        {
            var likeID = asset.ID;
            var titleName = forcedName != null ? forcedName : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(likeID.Replace('_',' '));
            
            if (!LM.Has("like_" + likeID + "_romantic"))
                LM.AddToCurrentLocale("like_" + likeID + "_romantic", titleName + " (R)");
            if (!LM.Has("like_" + likeID + "_romantic_info"))
                LM.AddToCurrentLocale("like_" + likeID + "_romantic_info", "Romantically likes " + titleName);
            if (!LM.Has("like_" + likeID + "_romantic_info_2"))
                LM.AddToCurrentLocale("like_" + likeID + "_romantic_info_2", "");        
            
            if (!LM.Has("like_" + likeID + "_sexual"))
                LM.AddToCurrentLocale("like_" + likeID + "_sexual", titleName + " (S)");
            if (!LM.Has("like_" + likeID + "_sexual_info"))
                LM.AddToCurrentLocale("like_" + likeID + "_sexual_info", "Sexually likes " + titleName);
            if (!LM.Has("like_" + likeID + "_sexual_info_2"))
                LM.AddToCurrentLocale("like_" + likeID + "_sexual_info_2", "");
        }
        
        // if all preferences of a preference type is added, remove them all (no reason for preferences if all are included?)
        private static void AddLikeType(string groupType, string hexColor, List<string> preferences, LoveType prefType, Func<Actor, string[]> actorType, Func<LikeAsset, GameObject> getIcon=null, Func<LikeAsset, Sprite> getSprite=null)
        {
            if (LikeTypes.Any(type => type.Key.ID.Equals(groupType)))
            {
                LogService.LogError(groupType + " is already an added like type!");
                return;
            }
            
            var withSpaces = groupType.Replace("_", " ");
            var endCombination = withSpaces.EndsWith("y") ? "ies" : withSpaces.EndsWith("s") ? "" : "s";
            var nameWithPlural = withSpaces.TrimEnd('y') + endCombination;

            var stringBuilder = new StringBuilder(nameWithPlural);
            stringBuilder[0] = char.ToUpper(stringBuilder[0]);
            for(var i = 0; i < stringBuilder.Length - 1; i++)
            {
                if (stringBuilder[i].Equals(' '))
                {
                    stringBuilder[i + 1] = char.ToUpper(stringBuilder[i + 1]);
                }
            }

            nameWithPlural = stringBuilder.ToString();
            
            if(!LM.Has("like_group_"+groupType))
                LM.AddToCurrentLocale("like_group_"+groupType, nameWithPlural);

            var likeAssets = new List<LikeAsset>();
            var likeGroup = new LikeGroup(groupType, hexColor, actorType, getIcon, getSprite);
            
            foreach (var likeName in preferences)
            {
                var like = new LikeAsset
                (
                    likeName,
                    likeGroup,
                    prefType
                );

                likeAssets.Add(like);
                
                AddLocalesForLikeAsset(like);
            }
            AllLikeAssets.AddRange(likeAssets);
            LikeTypes.Add(likeGroup, likeAssets);
            LikeGroups.Add(groupType, likeGroup);
        }
        private static void AddMatchingSets()
        {
            MatchingSets.Add("female", List.Of("feminine", "vulva"));
            MatchingSets.Add("male", List.Of("masculine", "phallus"));
        }
        private static void Finish()
        {
            LM.ApplyLocale("en");
            
            MapBox.on_world_loaded += LoadDynamicLikeAssets;
            MapBox.on_world_loaded += () =>
            {
                TolUtil.LogInfo("Fixing up units...");
                foreach (var actor in MapBox.instance.units.getSimpleList())
                {
                    actor.data.get("tol_edited", out bool edited);
                    if(!edited)
                        actor.NewLikes();
                }
            };
        }

        public static string GetHexCodeForLoveType(LoveType loveType)
        {
            return loveType switch
            {
                LoveType.Sexual => "#e934a2",
                LoveType.Romantic => "#34dee9",
                LoveType.Both => "#e934e6",
                _ => "#ffffff"
            };
        }

        public static LoveType GetRandomLoveType()
        {
            var array = Enum.GetValues(typeof(LoveType));
            return (LoveType) array.GetValue(Randy.randomInt(0, array.Length));
        }

        private static LikeGroup GetLikeGroup(string id)
        {
            LikeGroups.TryGetValue(id, out var group);
            if (group == null)
                throw new Exception(id + " is an invalid like group!");
            return group;
        }

        public static Actor GetPriorityActor(Actor pActor, Actor pTarget)
        {
            var id0 = pActor.getID();
            var id1 = pTarget.getID();
            return id0 > id1 ? pActor : pTarget;
        }
        public static string GetIDForActors(Actor pActor, Actor pTarget, bool sexual)
        {
            var priority = GetPriorityActor(pActor, pTarget);
            var other = priority == pActor ? pTarget : pActor;
            return priority.getID().ToString() + other.getID().ToString() + sexual.ToString();
        }

        public static int GetMatchingIDForActors(Actor pActor, Actor pTarget, bool sexual)
        {
            var id = GetIDForActors(pActor, pTarget, sexual);
            var priority = GetPriorityActor(pActor, pTarget);
            priority.data.get(id, out var match, -1);
            return match;
        }

        public static void SetMatchingIDForActors(Actor pActor, Actor pTarget, bool sexual, int matching)
        {
            var id = GetIDForActors(pActor, pTarget, sexual);
            var priority = GetPriorityActor(pActor, pTarget);
            priority.data.set(id, matching);
        }
        
        // checks for one actor based on a type, if you are checking for multiple types or multiple actors, then use the other methods
        public static bool LikeMatches(Actor pActor, Actor pTarget, string type, bool sexual)
        {
            if (pActor == null || pTarget == null) return false;
            // var matches = GetMatchingIDForActors(pActor, pTarget, sexual);
            
            // preferences do not matter
            if (pActor.hasCultureTrait("orientationless"))
                return true;
            // if (pActor.hasCultureTrait("orientationless") || matches == 1)
            //     return true;
            // if (matches == 0)
            //     return false;
            if (GetLikeGroup(type) == null) // the like group is invalid, this may happen when optional dependencies aren't installed
                return true;

            using var list = new ListPool<Like>(GetActorLikes(pActor, type,
                sexual ? LoveType.Sexual : LoveType.Romantic));
            
            var targetTypes = GetActorTypeFromLikeGroup(pTarget, type);
            if (targetTypes.Length == 0)
                return false;
                
            var found = false;
            foreach (var like in list)
            {
                if (targetTypes.Contains(like.ID))
                {
                    found = true;
                    break;
                }
            }
                
            return found;
        }
        
        // checks for one actor, if you are checking for both, use the other methods
        public static bool LikeMatches(Actor pActor, Actor pTarget, bool sexual)
        {
            // var matches = GetMatchingIDForActors(pActor, pTarget, sexual);
            //
            // if (pActor.hasCultureTrait("orientationless") || matches == 1)
            //     return true;
            if (pActor.hasCultureTrait("orientationless"))
                return true;
            // if (matches == 0)
            //     return false;
            
            using var list = new ListPool<Like>(GetActorLikes(pActor, sexual ? LoveType.Sexual : LoveType.Romantic));
            
            var identityTypes = GetActorTypeFromLikeGroup(pTarget, "identity");
            if (identityTypes.Length == 0)
                return false;
            
            var expressionTypes = TolUtil.IsTOIInstalled() ? GetActorTypeFromLikeGroup(pTarget, "expression") : null;
            if (expressionTypes != null && expressionTypes.Length == 0)
                return false;
            
            var genitalTypes = TolUtil.IsTOIInstalled() ? GetActorTypeFromLikeGroup(pTarget, "genital") : null;
            if (genitalTypes != null && genitalTypes.Length == 0)
                return false;
            
            // var subspeciesTypes = GetActorTypeFromLikeGroup(pTarget, "subspecies");
            // if (subspeciesTypes.Length == 0)
            //     return false;

            var foundIdentity = false;
            var foundExpression = false;
            // var foundSubspecies = pActor.hasSubspecies() && pTarget.isSameSubspecies(pActor.subspecies) && !pActor.isSapient();
            var foundGenital = !sexual;

            foreach (var like in list)
            {
                if(identityTypes.Contains(like.ID))
                    foundIdentity = true;
                if(!TolUtil.IsTOIInstalled() || expressionTypes.Contains(like.ID))
                    foundExpression = true;
                // if(subspeciesTypes.Contains(like.ID))
                //     foundSubspecies = true;
                if(!TolUtil.IsTOIInstalled() || expressionTypes.Contains(like.ID))
                    foundGenital = true;

                if (foundExpression && foundIdentity && foundGenital)
                    break;
            }


            if (foundExpression && foundIdentity && foundGenital)
            {
                // SetMatchingIDForActors(pActor, pTarget, sexual, 1);
                return true;
            }

            // SetMatchingIDForActors(pActor, pTarget, sexual, 0);
            return false;
        }

        public static bool LikeMatches(Actor actor1, Actor actor2)
        {
            return LikeMatches(actor1, actor2, false) && LikeMatches(actor1, actor2, true);
        }

        // Performance heavy
        public static bool BothActorsLikesMatch(Actor actor1, Actor actor2, bool sexual)
        {
            return LikeMatches(actor1, actor2, sexual) && LikeMatches(actor2, actor1, sexual);
        }

        public static bool AreCompatible(this Actor actor1, Actor actor2, bool sexual)
        {
            return BothActorsLikesMatch(actor1, actor2, sexual);
        }
        
        // Performance heavy
        public static bool BothActorsLikesMatch(Actor actor1, Actor actor2)
        {
            return BothActorsLikesMatch(actor1, actor2, false)
                   && BothActorsLikesMatch(actor1, actor2, true);
        }

        public static List<Like> GetRandomLikes(Actor actor)
        {
            // var bio = GetBiologicalSex(actor);
            // var preferredSets = new Dictionary<string, List<string>>();
            //
            // if (TolUtil.NeedDifferentSexTypeForReproduction(actor))
            // {
            //     var oppositeSex = bio.Equals("female") ? "male" : "female";
            //     preferredSets.Add(oppositeSex, MatchingSets[oppositeSex]);
            // } else if (TolUtil.NeedSameSexTypeForReproduction(actor))
            // {
            //     preferredSets.Add(bio, MatchingSets[bio]);
            // }

            var likes = new List<Like>();
            
            // if (Randy.randomChance(0.99f) && actor.isSapient())
            // {
            //
            //     var id = actor.subspecies.id.ToString();
            //
            //     if (!AllLikeAssets.Any(asset => asset.ID.Equals(id)))
            //     {
            //         var subspecies = actor.subspecies;
            //         AddDynamicLikeAsset(subspecies.id, subspecies.name, "subspecies", LoveType.Both);
            //     }
            //     
            //     likes.Add(GetLikeFromID(id, LoveType.Sexual));
            //     likes.Add(GetLikeFromID(id, LoveType.Romantic));
            // }

            // if (preferredSets.Count > 0)
            // {
            //     // if actors have preferred likes
            //     var keys = preferredSets.Keys.ToList();
            //     string randomKey;
            //
            //     if (Randy.randomChance(0.95f))
            //     {
            //         randomKey = keys.GetRandom();
            //         
            //         var likeIdentitySexual = GetLikeFromAsset(Randy.randomChance(0.75f) ? 
            //             GetAssetFromID(randomKey) : RandomLikeAssetFromType("identity", new[]{randomKey}), LoveType.Sexual);
            //         var likeIdentityRomantic = GetLikeFromAsset(Randy.randomChance(0.95f) ?
            //             likeIdentitySexual.LikeAsset : RandomLikeAssetFromType("identity", new[]{likeIdentitySexual.LikeAsset.ID}), LoveType.Romantic);
            //
            //         likes.Add(likeIdentitySexual);
            //         likes.Add(likeIdentityRomantic);
            //     }
            //     
            //     // expressions and genitals must be added or else people won't be able to date
            //     if (TolUtil.IsTOIInstalled())
            //     {
            //         randomKey = keys.GetRandom();
            //
            //         var likeExpressionSexual = GetLikeFromAsset(Randy.randomChance(0.75f) ? 
            //             GetAssetFromID(preferredSets[randomKey][0]) : RandomLikeAssetFromType("expression"), LoveType.Sexual);
            //         var likeExpressionRomantic = GetLikeFromAsset(Randy.randomChance(0.95f) ?
            //             likeExpressionSexual.LikeAsset : RandomLikeAssetFromType("expression"), LoveType.Romantic);   
            //         likes.Add(likeExpressionSexual);
            //         likes.Add(likeExpressionRomantic);
            //             
            //         randomKey = keys.GetRandom();
            //         var likeGenital = GetLikeFromAsset(Randy.randomChance(0.95f) ? 
            //             GetAssetFromID(preferredSets[randomKey][1]) : RandomLikeAssetFromType("genital"), LoveType.Sexual);
            //         
            //         likes.Add(likeGenital);
            //     }   
            // }
            
            // if (Randy.randomChance(0.25f) || preferredSets.Count <= 0)
            if (Randy.randomChance(0.99f))
            {
                using var excludeAssets = new ListPool<string>(likes.Count);
                for (var i = 0; i < likes.Count; i++)
                {
                    var likeAsset = likes[i].LikeAsset;
                    if (likeAsset.ApplicableLoveType != LoveType.Both)
                    {
                        excludeAssets.Add(likeAsset.ID);
                    }
                    else
                    {
                        for (var _i = i + 1; _i < likes.Count; _i++)
                        {
                            var likeAsset2 = likes[_i].LikeAsset;
                            if (likeAsset.Equals(likeAsset2))
                            {
                                excludeAssets.Add(likeAsset.ID);
                                break;
                            }
                        }   
                    }
                }

                var current = 0;
                var max = Randy.randomInt(1, (GetAllRegisteredAssets().Count - excludeAssets.Count()) * 2);
                
                while (current < max && excludeAssets.Count() < GetAllRegisteredAssets().Count)
                {
                    current++;

                    var asset = RandomLikeAsset(excludeAssets.ToArray());

                    LoveType? forcedType = null;
                    var otherAssignedLike = likes.Find(like => like.LikeAsset.Equals(asset));
                    if (otherAssignedLike != null)
                    {
                        excludeAssets.Add(otherAssignedLike.LikeAsset.ID);
                        forcedType = otherAssignedLike.LoveType == LoveType.Sexual
                            ? LoveType.Romantic
                            : LoveType.Sexual;
                    } else if (Randy.randomChance(0.95f) && asset.ApplicableLoveType == LoveType.Both)
                    {
                        var chosen = Randy.randomBool() ? LoveType.Sexual : LoveType.Romantic;
                        var otherLike = GetLikeFromAsset(asset, chosen);
                        likes.Add(otherLike);

                        forcedType = chosen == LoveType.Sexual ? LoveType.Romantic : LoveType.Sexual;
                        
                        excludeAssets.Add(otherLike.LikeAsset.ID); // already valid asset
                    }

                    if(asset.ApplicableLoveType != LoveType.Both)
                        excludeAssets.Add(asset.ID);
                    var like = GetLikeFromAsset(asset, forcedType);
                    likes.Add(like);
                }
            }

            return likes;
        }

        public static bool HasLike(this Actor actor, string id, LoveType? forcedType)
        {
            return actor.HasLike(GetLikeFromID(id, forcedType));
        }
        
        public static bool HasLike(this Actor actor, Like like)
        {
            actor.data.get(like.IDWithLoveType, out var result, false);
            return result;
        }

        public static bool HasALike(this Actor actor, LoveType loveType=LoveType.Both)
        {
            return GetActorLikes(actor, loveType).Count() > 0;
        }
        public static List<Like> GetActorLikes(this Actor actor, string groupType, LoveType loveType=LoveType.Both)
        {
            var list = GetActorLikes(actor, loveType);

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var like = list[i];
                if (!like.LikeAsset.LikeGroup.ID.Equals(groupType))
                    list.RemoveAt(i);
            }
            
            return list;
        }
        
        public static List<Like> GetActorLikes(this Actor actor, LoveType loveType=LoveType.Both)
        {
            var list = new List<Like>();
            var likes = GetValidLikesFromAssets(loveType);
            for(var i = 0; i < likes.Count; i++)
            {
                var like = likes[i];
                if(actor.HasLike(like))
                    list.Add(like);
            }
            return list;
        }

        public static List<Like> GetValidLikesFromAssets(string groupType, LoveType loveType=LoveType.Both)
        {
            CachedLikeListsWithGroup.TryGetValue((groupType, loveType), out var cachedList);
            if (cachedList != null)
            {
                return cachedList;
            }

            var list = GetValidLikesFromAssets(loveType).Where(like => like.LikeAsset.LikeGroup.ID.Equals(groupType)).ToList();

            CachedLikeListsWithGroup.Add((groupType, loveType), list);
            return list;
        }
        
        public static List<Like> GetValidLikesFromAssets(LoveType loveType=LoveType.Both)
        {
            CachedLikeLists.TryGetValue(loveType, out var cachedList);
            if (cachedList != null)
            {
                return cachedList;
            }

            var list = new List<Like>();
            foreach (var asset in AllLikeAssets)
            {
                if (loveType == LoveType.Both)
                {
                    if (asset.ApplicableLoveType == LoveType.Both)
                    {
                        list.Add(GetLikeFromAsset(asset, LoveType.Romantic));
                        list.Add(GetLikeFromAsset(asset, LoveType.Sexual));
                    }
                    else
                    {
                        list.Add(GetLikeFromAsset(asset, asset.ApplicableLoveType));
                    }
                }
                else if (asset.ApplicableLoveType == loveType || asset.ApplicableLoveType == LoveType.Both)
                {
                    list.Add(GetLikeFromAsset(asset, loveType));
                }
            }

            CachedLikeLists.Add(loveType, list);
            return list;
        }
        
        public static void ToggleLike(this Actor actor, Like like, bool? value=null)
        {
            actor.data.get(like.IDWithLoveType, out bool result);
            var opposite = !result;
            var finalValue = value.HasValue ? value.Value : opposite;
            if(finalValue)
                actor.data.set(like.IDWithLoveType, finalValue);
            else
                actor.data.removeBool(like.IDWithLoveType);
            
            Orientations.CreateOrientationBasedOnLikeChange(actor, like);
            actor.RemoveAllCachedLikes();
        }
        public static List<LikeAsset> GetRegisteredAssetsFromType(string type)
        {
            return LikeTypes[GetLikeGroup(type)];
        }
        public static List<LikeAsset> GetAllRegisteredAssets()
        {
            var list = new List<LikeAsset>();
            LikeTypes.Values.ForEach(list.AddRange);
            return list;
        }
        public static LikeAsset RandomLikeAssetFromType(string type, string[] exclude=null)
        {
            return GetRegisteredAssetsFromType(type).Where(preference => exclude == null || !exclude.Contains(preference.ID)).ToArray().GetRandom();
        }
        
        public static LikeAsset RandomLikeAsset(string[] exclude = null)
        {
            var excludeTypes = new List<string>();
            List<LikeAsset> preferences = null;
            while ((preferences == null || preferences.Count == 0) && excludeTypes.Count < LikeTypes.Keys.Count)
            {
                var likeGroup = LikeTypes.Keys.Where(key => !excludeTypes.Contains(key.ID)).ToArray().GetRandom();
                preferences = GetRegisteredAssetsFromType(likeGroup.ID).Where(asset => exclude == null || !exclude.Contains(asset.ID)).ToList();
                if(preferences.Count == 0 && !excludeTypes.Contains(likeGroup.ID))
                    excludeTypes.Add(likeGroup.ID);
            }
                
            return preferences != null && preferences.Count > 0 ? preferences.GetRandom() : null;
        }

        public static Like GetLikeFromID(string id, LoveType? forcedType = null)
        {
            return GetLikeFromAsset(GetAssetFromID(id), forcedType);
        }

        // will return either romantic variant or sexual variant depending on forcedType
        public static Like GetLikeFromAsset(LikeAsset asset, LoveType? forcedType = null)
        {
            if (forcedType.Equals(LoveType.Both))
                throw new Exception("Love type cannot be Both!");
            
            var loveType = asset.ApplicableLoveType.Equals(LoveType.Both)
                ? forcedType.HasValue ? forcedType.Value : Randy.randomBool() ? LoveType.Sexual : LoveType.Romantic
                : asset.ApplicableLoveType;
            if (forcedType.HasValue && !asset.ApplicableLoveType.Equals(LoveType.Both) && !asset.ApplicableLoveType.Equals(forcedType.Value))
                throw new Exception("Tried using type " + forcedType.Value + " when " + asset.ID + "'s applicable love type does not allow for that!");
            CachedLikes.TryGetValue((asset, loveType), out var cached);
            if (cached != null)
                return cached;
            
            var like = new Like(asset, loveType);
            
            CachedLikes.Add((like.LikeAsset, like.LoveType), like);

            return like;
        }
        
        public static LikeAsset GetAssetFromID(string id)
        {
            var asset = AllLikeAssets.Find(asset => asset.ID.Equals(id));
            if (asset == null)
                throw new Exception("No asset found with ID: " + id);
            return asset;
        }

        public static string[] GetActorTypeFromLikeGroup(this Actor actor, string type)
        {
            return LikeGroups[type].ActorType(actor);
    }

        // randomizes if multiple orientations fit the criteria

        // Run this when actors change identity (will patch Topic of Identity code)
        // add in a button that lets users regenerate orientation labels
        public static string GetIdentity(this Actor actor)
        {
            if (TolUtil.IsTOIInstalled())
            {
                actor.data.get("identity", out string identity);
                return identity;
            }

            return actor.isSexFemale() ? "female" : "male";
        }

        public static string GetExpression(this Actor actor)
        {
            if (TolUtil.IsTOIInstalled())
            {
                actor.data.get("expression", out string expression);
                return expression;            
            }

            return actor.isSexFemale() ? "feminine" : "masculine";
        }
        public static string[] GetExternalGenitalia(this Actor actor)
        {
            if (TolUtil.IsTOIInstalled())
            {
                var list = new string[2];
                actor.data.get("vulva", out bool hasVulva);
                actor.data.get("phallus", out bool hasPhallus);
                if (hasVulva)
                    list.AddItem("phallus");
                if (hasPhallus)
                    list.AddItem("phallus");
                return list;
            }

            if (actor.CanDoAnySexType())
                return new[]{"vulva", "phallus"};
            if (actor.NeedDifferentSexTypeForReproduction())
                return actor.isSexFemale() ? new[] { "vulva" } : new[] { "phallus" };
            return new string[]{};
        }
        public static bool IsMasculine(this Actor actor)
        {
            return GetExpression(actor).Equals("masculine");
        }
        public static bool IsFeminine(this Actor actor)
        {
            return GetExpression(actor).Equals("feminine");
        }

        public static bool HasUterus(this Actor actor)
        {
            actor.data.get("uterus", out bool result);
            return TolUtil.IsTOIInstalled() ? result : actor.isSexFemale();
        }
        public static bool HasVulva(this Actor actor)
        {
            return GetExternalGenitalia(actor).Contains("vulva");
        }
        public static bool HasPenis(this Actor actor)
        {
            return GetExternalGenitalia(actor).Contains("phallus");
        }
        
        public static bool IdentifiesAsEnby(this Actor actor)
        {
            return GetIdentity(actor).Equals("nonbinary");
        }
        public static bool IdentifiesAsWoman(this Actor actor)
        {
            return GetIdentity(actor).Equals("female");
        }
        public static bool IdentifiesAsMan(this Actor actor)
        {
            return GetIdentity(actor).Equals("male");
        }

        public static bool HasAnyLikesFor(this Actor actor, string groupType, LoveType loveType=LoveType.Both)
        {
            return actor.GetActorLikes(groupType, loveType).Count() > 0;
        }
    }
}