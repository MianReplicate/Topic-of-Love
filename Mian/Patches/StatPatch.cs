using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using NeoModLoader.General;
using NeoModLoader.services;
using Topic_of_Love.Mian.CustomAssets;
using Topic_of_Love.Mian.CustomAssets.Custom;
using Topic_of_Love.Mian.CustomAssets.Traits;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Topic_of_Love.Mian.Patches;

// Big credits to xing_yao on Discord for this!
[HarmonyPatch(typeof(UnitWindow))]
public class StatPatch
{
    private class Stat<T>
    {
        public readonly string Name;
        public readonly Func<Actor, bool> Valid;
        public readonly string IconPath;
        public readonly Func<Actor, T> Value;
        public readonly bool IsFloat = typeof(T) == typeof(float);
        
        public Stat(string name, Func<Actor, bool> valid, Func<Actor, T> value, string iconPath=null)
        {
            Name=name;
            Valid = valid;
            IconPath=iconPath;
            Value=value;
        }
    }
    private static readonly Stat<int>[] Icons = {
        new ("intimacy_happiness", TolUtil.CapableOfLove, actor => (int) actor.stats["intimacy_happiness"], "ui/Icons/god_powers/force_lover"),
    };
    private static readonly Stat<Dictionary<string, string>>[] Stats = {
        new ("sexual_orientation", TolUtil.CapableOfLove, actor =>
        {
            var orientationType = Orientation.GetOrientation(actor, true);
            if (orientationType != null)
            {

                Dictionary<string, string> dict = new();
                dict.Add("value", LM.Get(orientationType.SexualPathLocale));
                dict.Add("hex_code", orientationType.HexCode);
                dict.Add("icon", orientationType.SexualPathIcon);

                return dict;
            }
            return null;
        }),
        new ("romantic_orientation", TolUtil.CapableOfLove, actor =>
        {
            var orientationType = Orientation.GetOrientation(actor, false);
            if (orientationType != null)
            {
                Dictionary<string, string> dict = new();
                dict.Add("value", LM.Get(orientationType.RomanticPathLocale));
                dict.Add("hex_code", orientationType.HexCode);
                dict.Add("icon", orientationType.RomanticPathIcon);

                return dict;            
            }
            return null;
        })
    };

    static StatsIcon CreateNewIcon(Transform parent, Transform iconTemplate, string id, Sprite sprite)
    {
        var baseIcon = GameObject.Instantiate(iconTemplate, parent);
        var icon = baseIcon.GetComponent<StatsIcon>();
        var iconText = baseIcon.GetComponent<TipButton>();
        iconText.textOnClick = LM.Get("statistic_"+id);
        iconText.textOnClickDescription = "statistic_"+id+"_description";
        icon.name = id;
        icon.getIcon().sprite = sprite;
                        
        return icon;
    }
    
    static void ShowCustomIcons<TMetaObject, TData>(StatsIconContainer __instance, TMetaObject pMetaObject) where TMetaObject : MetaObject<TData> where TData : MetaObjectData
    {
            if (!__instance._stats_icons.ContainsKey("lesbian")) // this is how we will check if the ui was made for this menu yet
            {
                var iconTemplate = __instance._stats_icons.Values.First(source => true).transform;
                var iconGroupTemplate = iconTemplate.parent;
                
                for (int _i = 0; _i <= 1; _i++)
                {
                    bool isSexual = _i == 0;
                    var iconGroup = GameObject.Instantiate(iconGroupTemplate, iconGroupTemplate.parent);
                
                    for (var i = iconGroup.transform.childCount - 1; i >= 0; i--)
                    {
                        var child = iconGroup.transform.GetChild(i);
                        GameObject.DestroyImmediate(child.gameObject);
                    }
                
                    iconGroup.name = "orientation_icons_" + (isSexual ? "sexual" : "romantic");
                    iconGroup.transform.localScale = new Vector3(1, 1, 1);     
                    
                    foreach (var orientation in Orientation.Orientations)
                    {
                        var baseIcon = GameObject.Instantiate(iconTemplate, iconGroup);
                        var icon = baseIcon.GetComponent<StatsIcon>();
                        var iconText = baseIcon.GetComponent<TipButton>();
                        iconText.textOnClick = LM.Get("count_"+(isSexual ? orientation.SexualPathLocale : orientation.RomanticPathLocale));
                        iconText.textOnClickDescription = orientation.DescriptionLocale;
                        icon.name = isSexual ? orientation.OrientationType : orientation.OrientationType + "_romantic";
                        icon.getIcon().sprite = Resources.Load<Sprite>("ui/Icons/" + (isSexual ? orientation.SexualPathIcon : orientation.RomanticPathIcon));
                        
                        __instance._stats_icons.Add(icon.name, icon);
                    }   
                }

                var lonelinessGroup = __instance._stats_icons.Values
                .First(source => source.name.Equals("i_single_females")).transform.parent;
                var lonelyIcon = CreateNewIcon(
                    lonelinessGroup,
                    iconTemplate,
                    "lonely",
                    Resources.Load<Sprite>("ui/Icons/status/broke_up"));
                __instance._stats_icons.Add(lonelyIcon.name, lonelyIcon);
            }
            
            Orientation.Orientations.ForEach(orientation =>
            {
                var orientationType = orientation.OrientationType;
                __instance.setIconValue(orientationType, pMetaObject.countOrientation(orientationType, true));
                __instance.setIconValue(orientationType+"_romantic", pMetaObject.countOrientation(orientationType, false));
            });
            
            __instance.setIconValue("lonely", pMetaObject.countLonely());
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SubspeciesStatsElement), nameof(SubspeciesStatsElement.showContent))]
    static void ShowSubspeciesCustomStats(SubspeciesStatsElement __instance)
    {
        if(__instance._stats_icons.transform.name.Equals("content_more_icons"))
            ShowCustomIcons<Subspecies, SubspeciesData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ReligionStatsElement), nameof(ReligionStatsElement.showContent))]
    static void ShowReligionCustomStats(ReligionStatsElement __instance)
    {
        if(__instance._stats_icons.transform.name.Equals("content_more_icons"))
            ShowCustomIcons<Religion, ReligionData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LanguageStatsElement), nameof(LanguageStatsElement.showContent))]
    static void ShowLanguageCustomStats(LanguageStatsElement __instance)
    {
        if(__instance._stats_icons.transform.name.Equals("content_more_icons"))
            ShowCustomIcons<Language, LanguageData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(FamilyStatsElement), nameof(FamilyStatsElement.showContent))]
    static void ShowFamilyCustomStats(FamilyStatsElement __instance)
    {
        if(__instance._stats_icons.transform.name.Equals("content_more_icons"))
            ShowCustomIcons<Family, FamilyData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CultureStatsElement), nameof(CultureStatsElement.showContent))]
    static void ShowCultureCustomStats(CultureStatsElement __instance)
    {
        if(__instance._stats_icons.transform.name.Equals("content_more_icons"))
            ShowCustomIcons<Culture, CultureData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ClanStatsElement), nameof(ClanStatsElement.showContent))]
    static void ShowClanCustomStats(ClanStatsElement __instance)
    {
        if(__instance._stats_icons.transform.name.Equals("content_more_icons"))
            ShowCustomIcons<Clan, ClanData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AllianceStatsElement), nameof(AllianceStatsElement.showContent))]
    static void ShowAllianceCustomStats(AllianceStatsElement __instance)
    {
        if(__instance._stats_icons.transform.name.Equals("content_more_icons"))
            ShowCustomIcons<Alliance, AllianceData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(KingdomStatsElement), nameof(KingdomStatsElement.showContent))]
    static void ShowKingdomCustomStats(KingdomStatsElement __instance)
    {
        if(__instance._stats_icons.transform.name.Equals("content_more_icons"))
            ShowCustomIcons<Kingdom, KingdomData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CityStatsElement), nameof(CityStatsElement.showContent))]
    static void ShowCityCustomStats(CityStatsElement __instance)
    {
        if(__instance._stats_icons.transform.name.Equals("content_more_icons"))
            ShowCustomIcons<City, CityData>(__instance._stats_icons, __instance.meta_object);
    }

    [HarmonyPatch(typeof(UnitStatsElement))]
    public class UnitStatsElementClass
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(UnitStatsElement.showContent))]
        static void ShowContent(UnitStatsElement __instance)
        {
            foreach (var stat in Icons)
            {
                if (stat.Valid(__instance.actor))
                {
                    if (__instance.actor.asset.inspect_stats)
                    {
                        __instance.setIconValue(stat.Name, stat.Value(__instance.actor),"", "", stat.IsFloat);
                    }   
                }
            }
        }
    }
    
    private static bool _initializedUnitIcons;
    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnitWindow.OnEnable))]
    static void WindowCreatureInfo(UnitWindow __instance)
    {
        if (__instance.actor == null || !__instance.actor.isAlive())
            return;
        
        if (!_initializedUnitIcons)
        {
            _initializedUnitIcons = true;
            Initialize(__instance);
        }
    }

    private static void Initialize(UnitWindow window)
        {
            window
                .gameObject.transform.Find("Background/Scroll View")
                .GetComponent<ScrollRect>()
                .enabled = true;
            window
                .gameObject.transform.Find("Background/Scroll View/Viewport")
                .GetComponent<Mask>()
                .enabled = true;
            window
                .gameObject.transform.Find("Background/Scroll View/Viewport")
                .GetComponent<Image>()
                .enabled = true;
    
            var moreIconsTransform = window.gameObject.transform.Find(
                "Background/Scroll View/Viewport/Content/content_more_icons"
            ); // where the icons are
            
            var iconGroupTemplate = moreIconsTransform.GetChild(4);
            var iconGroup = GameObject.Instantiate(iconGroupTemplate, moreIconsTransform);
            
            var iconTemplate = iconGroup.Find("i_kills");
            
            for (var i = iconGroup.transform.childCount - 1; i >= 0; i--)
            {
                var child = iconGroup.transform.GetChild(i);
                if(child.name != "i_kills")
                    GameObject.DestroyImmediate(child.gameObject);
            }
    
            foreach (var iconData in Icons)
            {
                var baseIcon = GameObject.Instantiate(iconTemplate, iconGroup);
                var icon = baseIcon.GetComponent<StatsIcon>();
                var iconText = baseIcon.GetComponent<TipButton>();
                iconText.textOnClick = LM.Get("stats_icon_"+iconData.Name);
                icon.name = iconData.Name;
                icon.getIcon().sprite = Resources.Load<Sprite>(iconData.IconPath);
            }
            
            GameObject.DestroyImmediate(iconTemplate.gameObject);
            
            iconGroup.name = "tol_icons";
            iconGroup.transform.localScale = new Vector3(1, 1, 1);
        }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnitWindow.showStatsRows))]
    static void ShowStatsRowsPatch(UnitWindow __instance)
    {
        foreach (var stat in Stats)
        {
            var value = stat.Value(__instance.actor);
            if (value != null && stat.Valid(__instance.actor))
            {
                value.TryGetValue("hex_code", out var hexCode);
                value.TryGetValue("icon", out var icon);
                __instance.showStatRow(stat.Name, value["value"], hexCode, pColorText: true, pIconPath: icon);
            }
        }
    }
}