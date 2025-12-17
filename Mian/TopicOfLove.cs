using System.IO;
using Topic_of_Love.Mian.CustomAssets;
using Topic_of_Love.Mian.CustomAssets.AI;
using Topic_of_Love.Mian.CustomAssets.Traits;
using NeoModLoader.api;
using HarmonyLib;
using NeoModLoader.General;
using Topic_of_Love.Mian.CustomAssets.Custom;
using Topic_of_Love.Mian.CustomAssets.Custom.meta;
using Topic_of_Love.UI.Mian;

namespace Topic_of_Love.Mian
{
    public class TopicOfLove : BasicMod<TopicOfLove>
    {
        public static BasicMod<TopicOfLove> Mod;
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
        protected override void OnModLoad()
        {
            Mod = this;
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
            GodPowers.Init();
            TabsAndButtons.Init();
            Orientations.Init();
            StatisticAssets.Init(); // uses orientations
            HistoryDataAssets.Init();
            
            // MetaTypes.Init();
            
            
            var config = Mod.GetConfig();
            var statistics = (bool)config["Graphs"]["ModGraphs"].GetValue();
            
            // if(statistics)
                // HistoryMetaDataAssets.Init();
            
            TooltipAssets.Init();
            LikesManager.Init();

            // SmoothLoader.add(new MapLoaderAction(CustomStatWindows.Init), "Creating Topic of Love Interfaces...", pToEnd: true);
            // MapBox.on_world_loaded += CustomStatWindows.Init;
        }
        private void Awake()
        {
            Harmony.DEBUG = true;
            
            var harmony = new Harmony("netdot.mian.topicoflove");
            harmony.PatchAll();
        }
    }
}