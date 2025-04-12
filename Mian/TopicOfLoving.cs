using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ai.behaviours;
using NeoModLoader.api;
using NeoModLoader.services;
using HarmonyLib;
using NeoModLoader.General;

/*

- test orientationless trait (may need edits)

- homosexuals/heterosexuals have a 50% chance of having cultures that are heterophobic/homophobic

- make a sexual reproduction trait that allows two ppl of the same sex to reproduce
(done)

- test insult orientation task to make sure it still works
(needs testing)

- make spouses leave family when they break up (children will be shared if same subspecies. If different subspecies, the child will go to the same subspecies parent)
(needs testing)

- remove reproduction decision for smart units and and only reproduce from casual/preservation sex. 
(needs testing)

- make people unable to fall in love temporarily after breakups/cheating

- error relating to sprites and decisions??

- lovers will defend each other task
- improve breaking up and cheating post-systems. Rn the units just never date again but it should prob be altered so that they can date someone again after a period of time.

(wip)
- add sexual ivf task for units that cant get pregnant but want a baby (can lead to adoption which could be a happiness aspect!)
- add kissing task and other romantic tasks which can improve sexual happiness
*/
namespace Better_Loving.Mian
{
    public class TopicOfLoving : BasicMod<TopicOfLoving>
    {
        public static BasicMod<TopicOfLoving> Mod;
        
        protected override void OnModLoad()
        {
            Mod = this;
            // Initialize your mod.
            // Methods are called in the order: OnLoad -> Awake -> OnEnable -> Start -> Update
            LogService.LogInfo($"[{GetDeclaration().Name}]: Making people more loveable!");
            
            var locale_dir = GetLocaleFilesDirectory(GetDeclaration());
            foreach (var file in Directory.GetFiles(locale_dir))
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
            
            QueerTraits.Init();
            ActorTraits.Init();
            CultureTraits.Init();
            SubspeciesTraits.Init();
            StatusEffects.Init();
            Happiness.Init();
            CommunicationTopics.Init();
            DecisionAndBehaviorTasks.Init();
            GodPowers.Init();
            ClanTraits.Init();
        }
        private void Awake()
        {
            var harmony = new Harmony("netdot.mian.topicofloving");
            harmony.PatchAll();
        }
    }
}