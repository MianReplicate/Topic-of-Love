using ai.behaviours;
using HarmonyLib;
using Topic_of_Love.Mian.CustomAssets.AI.CustomBehaviors.ivf;
using Topic_of_Love.Mian.CustomAssets.AI.CustomBehaviors.orientation;
using Topic_of_Love.Mian.CustomAssets.AI.CustomBehaviors.other;
using Topic_of_Love.Mian.CustomAssets.AI.CustomBehaviors.romance;
using Topic_of_Love.Mian.CustomAssets.AI.CustomBehaviors.sex;
using Topic_of_Love.Mian.CustomAssets.Custom;

namespace Topic_of_Love.Mian.CustomAssets.AI
{
    public class ActorBehaviorTasks
    {
        // [HarmonyPostfix]
        // [HarmonyPatch(typeof(BehaviourTaskActorLibrary), nameof(BehaviourTaskActorLibrary.init))]
        public static void Init()
        {
            BehaviourTaskActor insultOrientation = new BehaviourTaskActor
            {
                id = "insult_orientation",
                locale_key = "task_insult_orientation",
                path_icon = "ui/Icons/culture_traits/orientationless",
            };
            insultOrientation.addBeh(new BehFindMismatchedOrientation());
            insultOrientation.addBeh(new BehGoToActorTarget(GoToActorTargetType.NearbyTileClosest, pCalibrateTargetPosition: true));
            insultOrientation.addBeh(new BehCheckNearActorTarget());
            insultOrientation.addBeh(new BehInsultOrientation());
            Add(insultOrientation);
            
            InitRomance();
            InitSex();
            InitSexualIvf();
        }

        private static void InitRomance()
        {
            var findToKiss = new BehaviourTaskActor
            {
                id = "find_kiss",
                locale_key = "task_find_partner",
                path_icon = "ui/Icons/status/just_kissed"
            };
            findToKiss.addBeh(new BehFindAPartner(false, true, true, distance: 40f, customValidity: (actor, target) => 
                Preferences.PreferenceMatches(actor, target) || Randy.randomChance(0.5f)));
            findToKiss.addBeh(new BehSetNextTask("try_kiss", pClean: false, pForce: true));
            Add(findToKiss);

            var doKissWithTarget = new BehaviourTaskActor
            {
                id = "try_kiss",
                locale_key = "task_try_kiss",
                path_icon = "ui/Icons/status/just_kissed"
            };
            doKissWithTarget.addBeh(new BehGoToActorTarget(pCalibrateTargetPosition:true));
            doKissWithTarget.addBeh(new BehKissTarget());
            
            Add(doKissWithTarget);

            var findDate = new BehaviourTaskActor
            {
                id = "find_date",
                locale_key = "task_find_partner",
                path_icon = "ui/Icons/status/went_on_date"
            };
            findDate.addBeh(new BehFindAPartner(false, mustMatchPreference:true, distance: 40f, customValidity: (actor, actor1) => actor.canFallInLoveWith(actor1)));
            findDate.addBeh(new BehSetNextTask("try_date", pClean: false, pForce: true));
            Add(findDate);

            var tryDate = new BehaviourTaskActor
            {
                id = "try_date",
                locale_key = "task_try_date",
                path_icon = "ui/Icons/status/went_on_date"
            };
            tryDate.addBeh(new BehGoToActorTarget(GoToActorTargetType.NearbyTileClosest, pCalibrateTargetPosition:true));
            tryDate.addBeh(new BehCheckForDate());
            Add(tryDate);
            
            var actionDate = new BehaviourTaskActor
            {
                id = "action_date",
                locale_key = "task_action_date",
                path_icon = "ui/Icons/status/went_on_date"
            };
            actionDate.addBeh(new BehRandomizeDateTile());
            actionDate.addBeh(new BehGoToTileTarget());
            actionDate.addBeh(new BehWait(0.2f));
            actionDate.addBeh(new BehCheckIfDateHere());
            actionDate.addBeh(new BehDoTalk());
            actionDate.addBeh(new BehFinishTalk());
            actionDate.addBeh(new BehCheckIfDateFinish());
            Add(actionDate);

            var followActionDate = new BehaviourTaskActor
            {
                id = "follow_action_date",
                locale_key = "task_follow_action_date",
                path_icon = "ui/Icons/status/went_on_date"
            };
            followActionDate.addBeh(new BehWait(0.2f));
            followActionDate.addBeh(new BehFollowDate());

            Add(followActionDate);
        }

        private static void InitSex()
        {
            var reproduceForPreservation = new BehaviourTaskActor
            {
                id = "reproduce_preservation",
                locale_key = "task_reproduce_preservation",
                path_icon = "ui/Icons/status/disliked_sex",
            };
            reproduceForPreservation.addBeh(new BehFindAPartner(false, false, false, true, "reproduction"));
            reproduceForPreservation.addBeh(new BehGetPossibleTileForSex());
            Add(reproduceForPreservation);
            
            var inviteForSex = new BehaviourTaskActor
            {
                id = "invite_for_sex",
                locale_key = "task_invite_for_sex",
                path_icon = "ui/Icons/status/enjoyed_sex",
            };
            inviteForSex.addBeh(new BehFindAPartner(false, false, true, false, "casual"));
            inviteForSex.addBeh(new BehGetPossibleTileForSex());
            Add(inviteForSex);

            var haveSexGo = new BehaviourTaskActor // our alternative of the sexual_reproduction_civ_go because it only works on lovers
            {
                id = "have_sex_go",
                locale_key = "task_have_sex_go",
                path_icon = "ui/Icons/status/enjoyed_sex",
            };
            haveSexGo.addBeh(new BehGoToTileTarget());
            for (int index = 0; index < 6; ++index)
            {
                haveSexGo.addBeh(new BehRandomWait(1f, 2f));
                haveSexGo.addBeh(new BehCheckForSexTarget());
                haveSexGo.addBeh(new BehRandomWait(1f, 2f));
            }
            Add(haveSexGo);
        }

        private static void InitSexualIvf()
        {
            var findSexualIvf = new BehaviourTaskActor
            {
                id = "find_sexual_ivf",
                locale_key = "task_find_partner",
                path_icon = "ui/Icons/status/adopted_baby",
            };
            findSexualIvf.addBeh(new BehFindAPartner(false, true, false, true, distance:200,
                customCheck: pActor => pActor.hasHouse() && !pActor.hasStatus("pregnant"),
                customValidity: (pActor, target) => !target.hasStatus("pregnant")));
            findSexualIvf.addBeh(new BehSetNextTask("try_sexual_ivf", false, true));
            Add(findSexualIvf);
            
            var trySexualIvf = new BehaviourTaskActor
            {
                id = "try_sexual_ivf",
                locale_key = "task_try_sexual_ivf",
                path_icon = "ui/Icons/status/adopted_baby"
            };
            trySexualIvf.addBeh(new BehTrySexualIvf());
            Add(trySexualIvf);

            var goSexualIvf = new BehaviourTaskActor
            {
                id = "go_sexual_ivf",
                locale_key = "task_go_sexual_ivf",
                path_icon = "ui/Icons/status/adopted_baby"
            };
            goSexualIvf.addBeh(new BehGoToBuildingTarget());
            for (int index = 0; index < 6; ++index)
            {
                goSexualIvf.addBeh(new BehRandomWait(1f, 2f));
                goSexualIvf.addBeh(new BehCheckForSexualIvf());
                goSexualIvf.addBeh(new BehRandomWait(1f, 2f));
            }
            Add(goSexualIvf);
            
            var goAndWaitSexualIvf = new BehaviourTaskActor
            {
                id = "go_and_wait_sexual_ivf",
                locale_key = "task_go_and_wait_sexual_ivf",
                path_icon = "ui/Icons/status/adopted_baby"
            };
            goAndWaitSexualIvf.addBeh(new BehGoToBuildingTarget());
            for (int index = 0; index < 6; ++index)
            {
                goAndWaitSexualIvf.addBeh(new BehRandomWait(1f, 2f));
            }
            Add(goAndWaitSexualIvf);
            
            var actionSexualIvf = new BehaviourTaskActor
            {
                id = "action_sexual_ivf",
                locale_key = "task_action_sexual_ivf",
                path_icon = "ui/Icons/status/adopted_baby"
            };
            actionSexualIvf.addBeh(new BehStayInBuildingTarget(2f, 3f));
            actionSexualIvf.addBeh(new BehStartSexualIvf());
            actionSexualIvf.addBeh(new BehExitBuilding());
            Add(actionSexualIvf);
           
            var waitSexualIvf = new BehaviourTaskActor
            {
                id = "wait_sexual_ivf",
                locale_key = "task_wait_sexual_ivf",
                path_icon = "ui/Icons/status/adopted_baby"
            };
            waitSexualIvf.addBeh(new BehStayInBuildingTarget(2f, 3f));
            waitSexualIvf.addBeh(new BehExitBuilding());
            Add(waitSexualIvf);
        }
        private static void Add(BehaviourTaskActor behaviour)
        {
            AssetManager.tasks_actor.add(behaviour);
        }
    }
}