using System;
using Topic_of_Love.Mian;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Topic_of_Love.UI.Mian;

public class CustomStatWindows
{
    private static bool Initalized;
    
    private static Type[] allMetasButUnit = {typeof(KingdomWindow), typeof(AllianceWindow), typeof(ArmyWindow), typeof(AllianceWindow), typeof(FamilyWindow), typeof(LanguageWindow), typeof(SubspeciesWindow), typeof(CultureWindow), typeof(CityWindow), typeof(ClanWindow), typeof(WarWindow), typeof(ReligionWindow)};
    public static void Init()
    {
        if (Initalized) return;
        Initalized = true;
        
        var template =
            GameObject.Find(
                "Canvas Container Main/Canvas - Windows/windows/kingdom/Background/Scroll View/Viewport/Content/content_metas");
        
        foreach (var type in allMetasButUnit)
        {
            StatsWindow instance = (StatsWindow) Object.FindFirstObjectByType(type, FindObjectsInactive.Include);
            var info = instance.transform.FindRecursive("Info").GetComponent<WindowMetaTab>();
            var mainContentWindow = instance.transform.FindRecursive("Content");
                
            var orientationsObj = Object.Instantiate(template, mainContentWindow.transform);
            var contentOrientations = orientationsObj.transform;
            contentOrientations.name = "content_orientations";
            contentOrientations.transform.GetChild(0).GetComponent<LocalizedText>().setKeyAndUpdate("orientations");
            // window.GetChild(0).GetComponent<Text>().text = "Orientations";
            var container = contentOrientations.GetChild(1);
            Object.Destroy(container.GetComponent<StatsMetaRowsContainer>());
            // for (int i = container.transform.childCount - 1; i >= 0; i--)
            // {
            //     Object.Destroy(container.GetChild(i));
            // }
            contentOrientations.SetSiblingIndex(50);
            info.tab_elements.Add(contentOrientations);
            // if(info._state)
            //     contentOrientations.gameObject.SetActive(true); 
            contentOrientations.GetChild(1).AddComponent<CustomStatsRows>();
        }
    }
}