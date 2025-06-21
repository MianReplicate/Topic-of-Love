
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Topic_of_Love.Mian.CustomAssets;

// Credits to NML for the code relating to creating tabs :D
public class TabsAndButtons
{
    private static readonly Transform tab_entry_container =
        CanvasMain.instance.canvas_ui.transform.Find("CanvasBottom/BottomElements/BottomElementsMover/TabsButtons");

    private static readonly Transform tab_container = CanvasMain.instance.canvas_ui.transform.Find(
        "CanvasBottom/BottomElements/BottomElementsMover/CanvasScrollView/Scroll View/Viewport/Content/buttons");
    
    private static PowersTab _modTab;
    public static void Init()
    { 
        _modTab = CreateTab(
            "Tab_TOL", 
            "tab_title_tol", 
            "tab_desc_tol", 
            SpriteTextureLoader.getSprite("ui/Icons/tabs/tab_tol"));
        
        AddButton("forceLover");
        AddButton("forceBreakup");
        AddButton("forceSex");
        AddButton("forceKiss");
        AddButton("forceSexualIVF");
        AddButton("forceDate");
    }

    private static void AddButton(string id)
    {
        AddButtonToTab(
            CreateGodPowerButton(id, SpriteTextureLoader.getSprite("ui/Icons/"+AssetManager.powers.get(id).path_icon)),
                _modTab);
    }
    
    private static PowerButton CreateGodPowerButton(string pGodPowerId, Sprite pIcon,
        [CanBeNull] Transform pParent = null, Vector2 pLocalPosition = default)
    {
        PowerButton prefab = FindResource<PowerButton>("inspect");

        bool found_active = prefab.gameObject.activeSelf;
        if (found_active)
        {
            prefab.gameObject.SetActive(false);
        }

        PowerButton obj;
        obj = pParent == null ? Object.Instantiate(prefab) : Object.Instantiate(prefab, pParent);

        if (found_active)
        {
            prefab.gameObject.SetActive(true);
        }


        obj.name = pGodPowerId;
        obj.icon.sprite = pIcon;
        obj.icon.overrideSprite = pIcon;
        obj.open_window_id = null;
        obj.type = PowerButtonType.Active;
        // More settings for it

        var transform = obj.transform;

        transform.localPosition = pLocalPosition;
        transform.localScale = Vector3.one;

        obj.gameObject.SetActive(true);
        return obj;
    }
    
    private static void AddButtonToTab(PowerButton button, PowersTab tab, int? siblingIndex = null)
    {
        Transform transform;
        (transform = button.transform).SetParent(tab.transform);
        transform.localScale = Vector3.one;
        if (siblingIndex.HasValue) transform.SetSiblingIndex(siblingIndex.Value);
        tab._power_buttons.Add(button);
    }
    
    private static T FindResource<T>(string name) where T : Object
    {
        string lower_name = name.ToLower();

        T[] first_search = Resources.FindObjectsOfTypeAll<T>();
        foreach (var obj in first_search)
        {
            if (obj.name.ToLower() == lower_name)
            {
                T result = Object.Instantiate(obj, TopicOfLove.Instance.transform);
                result.name = obj.name;
                return obj;
            }
        }

        return null;
    }
    private static T[] FindResources<T>(string name) where T : Object
    {
        T[] first_search = Resources.FindObjectsOfTypeAll<T>();
        List<T> result = new List<T>(first_search.Length / 16);

        string lower_name = name.ToLower();
        foreach (var obj in first_search)
        {
            if (obj.name.ToLower() == lower_name)
            {
                result.Add(obj);
            }
        }

        return result.ToArray();
    }
    
    private static PowersTab CreateTab(string name, string pTitleKey, string pDescKey, Sprite pIcon,
        string pOptionDescKey = "hotkey_tip_tab_other")
    {
        GameObject tab_entry = Object.Instantiate(FindResources<GameObject>("Button_Other")[0],
            tab_entry_container);

        Object.DestroyImmediate(tab_entry.GetComponent<GraphicRaycaster>());
        Object.DestroyImmediate(tab_entry.GetComponent<Canvas>());
        tab_entry.name = "Button_" + name;
        tab_entry.transform.Find("Icon").GetComponent<Image>().sprite = pIcon;

        PowersTab tab = Object.Instantiate(
            FindResources<GameObject>("units").Select(tgo => tgo.GetComponent<PowersTab>()).First(t => t != null),
            tab_container);

        tab.name = name;

        var asset = new PowerTabAsset
        {
            id = name,
            locale_key = pTitleKey,
            tab_type_main = true,
            get_power_tab = () => tab
        };
        AssetManager.power_tab_library.add(asset);
        tab._asset = asset;

        Button tab_entry_button = tab_entry.GetComponent<Button>();
        tab_entry_button.onClick = new Button.ButtonClickedEvent();
        tab_entry_button.onClick.AddListener(() => tab.showTab(tab_entry_button));
        tab_entry_button.onClick.AddListener(() => tab_entry.GetComponent<ButtonSfx>().playSound());

        TipButton tab_entry_tip = tab_entry.GetComponent<TipButton>();
        tab_entry_tip.textOnClick = pTitleKey;
        tab_entry_tip.textOnClickDescription = pDescKey;
        tab_entry_tip.text_description_2 = pOptionDescKey;
        // Clear tab content
        for (int i = 6; i < tab.transform.childCount; i++)
        {
            Object.Destroy(tab.transform.GetChild(i).gameObject);
        }

        tab._power_buttons.Clear();
        // Add default powerButtons
        foreach (PowerButton power_button in tab.GetComponentsInChildren<PowerButton>())
        {
            if (!(power_button == null) && !(power_button.rect_transform == null))
            {
                tab._power_buttons.Add(power_button);
            }
        }

        foreach (PowerButton power_button in tab._power_buttons)
        {
            power_button.findNeighbours(tab._power_buttons);
        }

        EventTrigger trigger = tab_entry_button.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = tab_entry_button.gameObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.EndDrag;
        // entry.callback.AddListener((data) => { _setToValidPosition(tab_entry_button, tab.name); });
        trigger.triggers.Add(entry);

        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.Drag;
        // entry.callback.AddListener((data) => { _onDragTabEntry(tab_entry_button, tab.name); });
        trigger.triggers.Add(entry);
        
        // _addTabEntry(tab_entry, tab.name);
        // _updateTabLayout();

        tab.gameObject.SetActive(false);
        tab.gameObject.SetActive(true);
        return tab;
    }
}