using System.IO;
using BepInEx;
using BepInEx.Logging;
using db;
using Topic_of_Love.Mian.CustomAssets;
using Topic_of_Love.Mian.CustomAssets.AI;
using Topic_of_Love.Mian.CustomAssets.Traits;
using HarmonyLib;
using Topic_of_Love.Mian.CustomAssets.Custom;
using UnityEngine;

namespace Topic_of_Love.Mian
{
    [BepInPlugin("netdot.mian.topic_of_love", "Topic of Love", "1.0.0")]
    public class TopicOfLove : BaseUnityPlugin
    {
        public new static ManualLogSource Logger;
        public void Reload()
        {
            var localeDir = GetLocaleFilesDirectory(GetDeclaration());
            foreach (var file in Directory.GetFiles(localeDir))
            {
                if (file.EndsWith(".json"))
                {
                    LM.LoadLocale(Path.GetFileNameWithoutExtension(file), file);
                }
                else if (file.EndsWith(".csv"))
                {
                    LM.LoadLocales(file);
                }
            }

            LM.ApplyLocale();
        }
        
        private void Awake()
        {
            Harmony.DEBUG = true;
            
            var harmony = new Harmony("netdot.mian.topicofloving");
            harmony.PatchAll();
            
            // Initialize your mod.
            // Methods are called in the order: OnLoad -> Awake -> OnEnable -> Start -> Update
            TolUtil.LogInfo("Making people more loveable!");
            
            new ActorTraits().Init();
            new CultureTraits().Init();
            new SubspeciesTraits().Init();
            StatusEffects.Init();
            Happiness.Init();
            CommunicationTopics.Init();
            ActorBehaviorTasks.Init();
            Decisions.Init();
            Opinions.Init();
            BaseStatAssets.Init(); // make sure this loads before preferences
            WorldLawAssets.Init();
            StatisticAssets.Init();
            HistoryDataAssets.Init();
            GodPowers.Init();
            TabsAndButtons.Init();
            Orientations.Init();
            Preferences.Init();
            
            Debug.Log("working");
        }
    }
}