using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using db;
using Topic_of_Love.Mian.CustomAssets;
using Topic_of_Love.Mian.CustomAssets.AI;
using Topic_of_Love.Mian.CustomAssets.Traits;
using HarmonyLib;
using HarmonyLib.Tools;
using Newtonsoft.Json;
using Topic_of_Love.Mian.CustomAssets.Custom;
using UnityEngine;

// TODO: add NML loading compatibility for workshop support & all
namespace Topic_of_Love.Mian
{
    [BepInPlugin("netdot.mian.topic_of_love", "Topic of Love", "1.0.0")]
    public class TopicOfLove : BaseUnityPlugin
    {
        public static TopicOfLove Instance;
        public new static ManualLogSource Logger;

        public static string GetModDirectory()
        {
            return Environment.CurrentDirectory;
        }
        
        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            HarmonyFileLog.Enabled = true;
            
            var harmony = new Harmony("netdot.mian.topicofloving");
            harmony.PatchAll();
            
            // Initialize your mod.
            // Methods are called in the order: OnLoad -> Awake -> OnEnable -> Start -> Update
            TolUtil.LogInfo("Making people more loveable!");
            
            TolConfig.Init(this); // start first
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
            
            LM.LoadLocales(); // run last
            Debug.Log("working");
        }
    }
}