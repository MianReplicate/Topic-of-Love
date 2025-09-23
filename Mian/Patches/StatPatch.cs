using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using NCMS.Extensions;
using NeoModLoader.General;
using NeoModLoader.services;
using Topic_of_Love.Mian.CustomAssets;
using Topic_of_Love.Mian.CustomAssets.Custom;
using Topic_of_Love.Mian.CustomAssets.Traits;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Topic_of_Love.Mian.Patches;

// Big credits to xing_yao on Discord for this!
// TODO : lowkey let's remake this class to be less fucking radically insane. More automating would be nice :O
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
        new ("intimacy_happiness", Extensions.CapableOfLove, actor => (int) actor.stats["intimacy_happiness"], "ui/Icons/god_powers/force_lover"),
    };
    private static readonly Stat<Dictionary<string, string>>[] Stats = {
        new ("sexual_orientation", Extensions.CapableOfLove, actor =>
        {
            var orientationType = _Orientation.GetOrientation(actor, true);
            if (orientationType != null)
            {

                Dictionary<string, string> dict = new();
                dict.Add("value", LM.Get(orientationType.SexualPathLocale));
                dict.Add("hex_code", orientationType.HexCode);
                dict.Add("icon", orientationType.GetPathIcon(true, false));

                return dict;
            }
            return null;
        }),
        new ("romantic_orientation", Extensions.CapableOfLove, actor =>
        {
            var orientationType = _Orientation.GetOrientation(actor, false);
            if (orientationType != null)
            {
                Dictionary<string, string> dict = new();
                dict.Add("value", LM.Get(orientationType.RomanticPathLocale));
                dict.Add("hex_code", orientationType.HexCode);
                dict.Add("icon", orientationType.GetPathIcon(false, false));

                return dict;            
            }
            return null;
        })
    };

    static StatsIcon CreateNewIcon(Transform parent, StatsIconContainer __instance, string id, Sprite sprite)
    {
        var iconTemplate = __instance._stats_icons.Values.First().transform;

        var baseIcon = GameObject.Instantiate(iconTemplate, parent);
        var icon = baseIcon.GetComponent<StatsIcon>();
        var iconText = baseIcon.GetComponent<TipButton>();
        iconText.textOnClick = LM.Get("statistics_"+id);
        iconText.textOnClickDescription = "statistics_"+id+"_description";
        icon.name = id;
        icon.getIcon().sprite = sprite;
                        
        return icon;
    }

    static Transform CreateNewGroup(StatsIconContainer __instance)
    {
        var iconGroupTemplate = __instance._stats_icons.Values.First().transform.parent;
        var newGroup = GameObject.Instantiate(iconGroupTemplate, iconGroupTemplate.parent);
        for (var i = newGroup.transform.childCount - 1; i >= 0; i--)
        {
            var child = newGroup.transform.GetChild(i);
            GameObject.DestroyImmediate(child.gameObject);
        }
        return GameObject.Instantiate(iconGroupTemplate, iconGroupTemplate.parent);
    }
    
    static void ShowCustomIcons<TMetaObject, TData>(StatsIconContainer __instance, TMetaObject pMetaObject) where TMetaObject : MetaObject<TData> where TData : MetaObjectData
    {
            if (!__instance._stats_icons.ContainsKey("lonely")) // this is how we will check if the ui was made for this menu yet
            {
                // for (int _i = 0; _i <= 1; _i++)
                // {
                //     bool isSexual = _i == 0;
                //     var iconGroup = CreateNewGroup(__instance);
                //
                //     iconGroup.name = "orientation_icons_" + (isSexual ? "sexual" : "romantic");
                //     iconGroup.transform.localScale = new Vector3(1, 1, 1);     
                //     
                //     foreach (var orientation in Orientation.Orientations.Values)
                //     {
                //         var icon = CreateNewIcon(
                //             iconGroup,
                //             __instance,
                //             isSexual ? orientation.OrientationType : orientation.OrientationType + "_romantic",
                //             Resources.Load<Sprite>(orientation.GetPathIcon(true, true)));
                //
                //         __instance._stats_icons.Add(icon.name, icon);
                //     }   
                // }

                var mainGroup = CreateNewGroup(__instance);
                var lonelyIcon = CreateNewIcon(
                    mainGroup,
                    __instance,
                    "lonely",
                    Resources.Load<Sprite>("ui/Icons/status/okay_sex"));
                __instance._stats_icons.Add(lonelyIcon.name, lonelyIcon);
                
                var brokenIcon = CreateNewIcon(
                    mainGroup,
                    __instance,
                    "broke_up",
                    Resources.Load<Sprite>("ui/Icons/status/broke_up"));
                __instance._stats_icons.Add(brokenIcon.name, brokenIcon);
                
                var cheatedIcon = CreateNewIcon(
                    mainGroup,
                    __instance,
                    "cheated_on",
                    Resources.Load<Sprite>("ui/Icons/status/cheated_on"));
                __instance._stats_icons.Add(cheatedIcon.name, cheatedIcon);
                
                var adoptedIcon = CreateNewIcon(
                    mainGroup,
                    __instance,
                    "adopted_baby",
                    Resources.Load<Sprite>("ui/Icons/status/adopted_baby"));
                __instance._stats_icons.Add(adoptedIcon.name, adoptedIcon);
            }
            
            // Orientation.RegisteredOrientations.Values.ForEach(orientation =>
            // {
                // var orientationType = orientation.OrientationType;
                // __instance.setIconValue(orientationType, pMetaObject.countOrientation(orientationType, true));
                // __instance.setIconValue(orientationType+"_romantic", pMetaObject.countOrientation(orientationType, false));
            // });
            
            __instance.setIconValue("lonely", pMetaObject.countLonely());
            __instance.setIconValue("broke_up", pMetaObject.countBrokenUp());
            __instance.setIconValue("cheated_on", pMetaObject.countCheated());
            __instance.setIconValue("adopted_baby", pMetaObject.countAdoptedBaby());
    }

    private static readonly string[] ValidIconsList =
    {
        "content_more_icons",
        "content_text_row_stats",
        "content_stats"
    }; 
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SubspeciesStatsElement), nameof(SubspeciesStatsElement.showContent))]
    static void ShowSubspeciesCustomStats(SubspeciesStatsElement __instance)
    {
        if(__instance._stats_icons != null && ValidIconsList.Contains(__instance._stats_icons.transform.name))
            ShowCustomIcons<Subspecies, SubspeciesData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ReligionStatsElement), nameof(ReligionStatsElement.showContent))]
    static void ShowReligionCustomStats(ReligionStatsElement __instance)
    {
        if(__instance._stats_icons != null && ValidIconsList.Contains(__instance._stats_icons.transform.name))
            ShowCustomIcons<Religion, ReligionData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LanguageStatsElement), nameof(LanguageStatsElement.showContent))]
    static void ShowLanguageCustomStats(LanguageStatsElement __instance)
    {
        if(__instance._stats_icons != null && ValidIconsList.Contains(__instance._stats_icons.transform.name))
            ShowCustomIcons<Language, LanguageData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(FamilyStatsElement), nameof(FamilyStatsElement.showContent))]
    static void ShowFamilyCustomStats(FamilyStatsElement __instance)
    {
        if(__instance._stats_icons != null && ValidIconsList.Contains(__instance._stats_icons.transform.name))
            ShowCustomIcons<Family, FamilyData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CultureStatsElement), nameof(CultureStatsElement.showContent))]
    static void ShowCultureCustomStats(CultureStatsElement __instance)
    {
        if(__instance._stats_icons != null && ValidIconsList.Contains(__instance._stats_icons.transform.name))
            ShowCustomIcons<Culture, CultureData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ClanStatsElement), nameof(ClanStatsElement.showContent))]
    static void ShowClanCustomStats(ClanStatsElement __instance)
    {
        if(__instance._stats_icons != null && ValidIconsList.Contains(__instance._stats_icons.transform.name))
            ShowCustomIcons<Clan, ClanData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AllianceStatsElement), nameof(AllianceStatsElement.showContent))]
    static void ShowAllianceCustomStats(AllianceStatsElement __instance)
    {
        if(__instance._stats_icons != null && ValidIconsList.Contains(__instance._stats_icons.transform.name))
            ShowCustomIcons<Alliance, AllianceData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(KingdomStatsElement), nameof(KingdomStatsElement.showContent))]
    static void ShowKingdomCustomStats(KingdomStatsElement __instance)
    {
        if(__instance._stats_icons != null && ValidIconsList.Contains(__instance._stats_icons.transform.name))
            ShowCustomIcons<Kingdom, KingdomData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CityStatsElement), nameof(CityStatsElement.showContent))]
    static void ShowCityCustomStats(CityStatsElement __instance)
    {
        if(__instance._stats_icons != null && ValidIconsList.Contains(__instance._stats_icons.transform.name))
            ShowCustomIcons<City, CityData>(__instance._stats_icons, __instance.meta_object);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ArmyStatsElement), nameof(ArmyStatsElement.showContent))]
    static void ShowArmyCustomStats(ArmyStatsElement __instance)
    {
        if(__instance._stats_icons != null && ValidIconsList.Contains(__instance._stats_icons.transform.name))
            ShowCustomIcons<Army, ArmyData>(__instance._stats_icons, __instance.meta_object);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(KingdomWindow), nameof(KingdomWindow.showStatsRows))]
    static void ShowKingdomRows(KingdomWindow __instance)
    {
        __instance.showSplitPopulationByOrientation(__instance.meta_object.units);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CityWindow), nameof(CityWindow.showStatsRows))]
    static void ShowCityRows(CityWindow __instance)
    {
        __instance.showSplitPopulationByOrientation(__instance.meta_object.units);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CultureWindow), nameof(CultureWindow.showStatsRows))]
    static void ShowCultureRows(CultureWindow __instance)
    {
        __instance.showSplitPopulationByOrientation(__instance.meta_object.units);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AllianceWindow), nameof(AllianceWindow.showStatsRows))]
    static void ShowAllianceRows(AllianceWindow __instance)
    {
        __instance.showSplitPopulationByOrientation(__instance.meta_object.kingdoms_list.SelectMany(kingdom => kingdom.units).ToList());
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SubspeciesWindow), nameof(SubspeciesWindow.showStatsRows))]
    static void ShowSubspeciesRows(SubspeciesWindow __instance)
    {
        __instance.showSplitPopulationByOrientation(__instance.meta_object.units);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ClanWindow), nameof(ClanWindow.showStatsRows))]
    static void ShowClanRows(ClanWindow __instance)
    {
        __instance.showSplitPopulationByOrientation(__instance.meta_object.units);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ReligionWindow), nameof(ReligionWindow.showStatsRows))]
    static void ShowReligionWindow(ReligionWindow __instance)
    {
        __instance.showSplitPopulationByOrientation(__instance.meta_object.units);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ArmyWindow), nameof(ArmyWindow.showStatsRows))]
    static void ShowArmyRows(ArmyWindow __instance)
    {
        __instance.showSplitPopulationByOrientation(__instance.meta_object.units);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(FamilyWindow), nameof(FamilyWindow.showStatsRows))]
    static void ShowFamilyRows(FamilyWindow __instance)
    {
        __instance.showSplitPopulationByOrientation(__instance.meta_object.units);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LanguageWindow), nameof(LanguageWindow.showStatsRows))]
    static void ShowLanguageRows(LanguageWindow __instance)
    {
        __instance.showSplitPopulationByOrientation(__instance.meta_object.units);
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
                        __instance.setIconValue(stat.Name, stat.Value(__instance.actor), pFloat : stat.IsFloat);
                    }   
                }
            }
        }
    }

    private static WindowMetaTab _preferenceTabEntry;
    private static Image _imageRegenComponent;
    
    private static bool _initializedUnitWindow;
    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnitWindow.OnEnable))]
    static void WindowCreatureInfo(UnitWindow __instance)
    {
        if (__instance.actor == null || !__instance.actor.isAlive())
            return;
        
        if (!_initializedUnitWindow)
        {
            _initializedUnitWindow = true;
            InitializeLikesMind(__instance);
            InitializeIcons(__instance);
        }
        
        _preferenceTabEntry.toggleActive(true);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnitWindow.clear))]
    static void Clear()
    {
        if(_preferenceTabEntry != null)
            _preferenceTabEntry.toggleActive(false);
    }
    
    private static void InitializeLikesMind(UnitWindow window)
    {
        var mindPreferenceEntry = Object.Instantiate(
            ResourcesFinder.FindResource<GameObject>("Mind"), 
            window._button_equipment_editor.transform.parent.transform);

        mindPreferenceEntry.transform.SetSiblingIndex(7);
        mindPreferenceEntry.SetActive(true);
        mindPreferenceEntry.name = "MindPreferences";

        var statElements = window.gameObject.transform.Find("Background/Scroll View/Viewport/Content/content_stats");
        var container = statElements.gameObject.GetComponent<StatsRowsContainer>();
        
        var homosexualSprite = Resources.Load<Sprite>("ui/Icons/orientations/homosexual");
        mindPreferenceEntry.transform.GetChild(0).GetComponent<Image>().sprite = homosexualSprite;

        var tip = mindPreferenceEntry.GetComponent<TipButton>();
        tip.textOnClick = "tab_mind_likes";
        tip.textOnClickDescription = "tab_mind_likes_description";
        
        var mind = ResourcesFinder.FindResource<GameObject>("content_mind");
        var mindPreferences = Object.Instantiate(mind, mind.transform.parent); // the whole menu shabang
        
        var neuronsOverview = mind.GetComponent<NeuronsOverview>();
        NeuronsOverview.instance = neuronsOverview;
        
        GameObject.Destroy(mindPreferences.GetComponent<NeuronsOverview>());
        var likesOverview = mindPreferences.AddOrGetComponent<LikesOverview>();

        var mindMain = mindPreferences.transform.GetChild(1).GetChild(1);
        likesOverview._mind_main = mindMain.gameObject;
        likesOverview._parent_axons = mindMain.GetChild(0).GetComponent<RectTransform>();
        likesOverview._parent_nerve_impulses = mindMain.GetChild(1).GetComponent<RectTransform>();
        likesOverview._parent_neurons = mindMain.GetChild(2).GetComponent<RectTransform>();
        
        var neuronObject = GameObject.Instantiate(NeuronsOverview.instance._prefab_neuron.gameObject);
        neuronObject.name = "element_neuron_like";
        GameObject.Destroy(neuronObject.GetComponent<NeuronElement>());
        // neuronObject.AddComponent<LikeNeuronElement>().image = neuronObject.GetComponent<Image>();
        neuronObject.AddComponent<LikeNeuronElement>();
        GameObject.Destroy(neuronObject.GetComponent<Image>());
        
        likesOverview._prefab_neuron = neuronObject.GetComponent<LikeNeuronElement>();
        
        var nerveObject = GameObject.Instantiate(NeuronsOverview.instance._prefab_nerve_impulse.gameObject);
        nerveObject.name = "element_nerve_impulse_like";
        GameObject.Destroy(nerveObject.GetComponent<NerveImpulseElement>());
        nerveObject.AddComponent<LikeNerveImpulseElement>().image = nerveObject.GetComponent<Image>();
        likesOverview._prefab_nerve_impulse = nerveObject.GetComponent<LikeNerveImpulseElement>();
        
        var axonObject = GameObject.Instantiate(NeuronsOverview.instance._prefab_axon.gameObject);
        axonObject.name = "element_axon_like";
        GameObject.Destroy(axonObject.GetComponent<AxonElement>());
        axonObject.AddComponent<LikeAxonElement>().image = axonObject.GetComponent<Image>();
        likesOverview._prefab_axon = axonObject.GetComponent<LikeAxonElement>();
        
        likesOverview.mainWindow = window;

        mindPreferences.name = "content_mind_preferences";
        
        var localized = mindPreferences.transform.GetChild(0).GetChild(0).GetComponent<LocalizedText>();
        localized.setKeyAndUpdate("tab_mind_likes");

        mindPreferences.transform.GetChild(0).GetChild(1).GetComponent<Image>().sprite = homosexualSprite;
        mindPreferences.transform.GetChild(0).GetChild(2).GetComponent<Image>().sprite = homosexualSprite;

        _preferenceTabEntry = mindPreferenceEntry.GetComponent<WindowMetaTab>();
        _preferenceTabEntry.tab_elements.Remove(mind.transform);
        _preferenceTabEntry.tab_elements.Add(mindPreferences.transform);
        mindPreferences.transform.SetSiblingIndex(1);

        _preferenceTabEntry.tab_action.AddListener(_ =>
        {
            statElements.gameObject.SetActive(true);
            
            container.StopAllCoroutines();
            UpdateOrientationStats(window);
        });
        
        var reforge = ResourcesFinder.FindResource<GameObject>("content_reforge");
        // var cursedItem = ResourcesFinder.FindResource<GameObject>("Item Cursed");
        var orientationButtons = GameObject.Instantiate(reforge, mind.transform.parent);
        orientationButtons.name = "content_orientations";
        var regenerate = orientationButtons.transform.GetChild(0);
        regenerate.name = "regenerate_orientation";
        localized = regenerate.GetChild(1).GetComponent<LocalizedText>();
        localized.setKeyAndUpdate("mind_likes_regenerate_orientation");

        tip = regenerate.GetComponent<TipButton>();
        tip.textOnClick = "mind_likes_regenerate_orientation";
        tip.textOnClickDescription = "mind_likes_regenerate_orientation_description";
        tip.text_description_2 = "mind_likes_regenerate_orientation_description_2";

        _imageRegenComponent = regenerate.GetChild(0).GetComponent<Image>();
        
        var button = regenerate.GetComponent<Button>();
        regenerate.localPosition = Vector3.zero;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() =>
        {
            Orientations.RollOrientationLabel(window.actor);
            UpdateOrientationStats(window);
        });
        
        GameObject.Destroy(orientationButtons.transform.GetChild(1).gameObject);
        // var lockButton = GameObject.Instantiate(cursedItem, orientationButtons.transform);
        
        _preferenceTabEntry.tab_elements.Add(orientationButtons.transform);
        
        window.tabs._tabs.Add(_preferenceTabEntry);
    }

    public static void UpdateOrientationStats(UnitWindow window)
    {
        var container = window._stats_rows_container;
        container.OnDisable();
            
        var sexual = Stats[0].Value(window.actor);
        var romantic = Stats[1].Value(window.actor);
        sexual.TryGetValue("hex_code", out var hexCode);
        sexual.TryGetValue("icon", out var icon);
        window.showStatRow("sexual_orientation", sexual["value"], hexCode, pColorText: true, pIconPath: icon);

        _imageRegenComponent.sprite = SpriteTextureLoader.getSprite("ui/Icons/" + icon);
        
        romantic.TryGetValue("hex_code", out hexCode);
        romantic.TryGetValue("icon", out icon);
        window.showStatRow("romantic_orientation", romantic["value"], hexCode, pColorText: true, pIconPath: icon);
            
        container.StartCoroutine(container.showRows());
    }
    
    private static void InitializeIcons(UnitWindow window)
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