using System;
using ai;
using ai.behaviours;
using Topic_of_Love.Mian.CustomAssets.Custom;
using Topic_of_Love.Mian.CustomAssets.Traits;

namespace Topic_of_Love.Mian.CustomAssets.AI.CustomBehaviors.romance;

public class BehCheckIfDateFinish : BehaviourActionActor
{
    public override BehResult execute(Actor pActor)
    {
        TolUtil.Debug("Checking for date before finalizing: "+ pActor.getName());
        if (pActor.beh_actor_target == null)
        {
            TolUtil.Debug(pActor.getName()+": Cancelled from finalizing because actor was null");
            return BehResult.Stop;
        }
        var target = pActor.beh_actor_target.a;

        pActor.data.get("date_happiness", out var happiness, 0f);
        if (Preferences.BothPreferencesMatch(pActor, target, false))
        {
            happiness += Randy.randomFloat(15, 20f);
        }
        else
        {
            happiness += Randy.randomFloat(8, 14f);
        }
        pActor.data.set("date_happiness", happiness);

        if (happiness <= 100f)
        {
            if (happiness <= 15f || Randy.randomChance(1f - happiness / 100))
            {
                var wait = Randy.randomFloat(3f, 6f);
                pActor.makeWait(wait);
                target.makeWait(wait);
                EffectsLibrary.spawnAt("fx_hearts", pActor.current_position, pActor.current_scale.y);
                EffectsLibrary.spawnAt("fx_hearts", pActor.beh_actor_target.current_position, pActor.beh_actor_target.current_scale.y);
                pActor.beh_actor_target.a.beh_actor_target = pActor;
                pActor.beh_actor_target.a.setTask("follow_action_date", false, pForceAction: true);
                return BehResult.RestartTask;
            }
        }

        if (happiness >= (pActor.getBestFriend() == target ? 10f : 25f))
        {
            if(pActor.hasLover() && Randy.randomBool())
                TolUtil.BreakUp(pActor, false);
            if(target.hasLover() && Randy.randomBool())
                TolUtil.BreakUp(target, false);
            TolUtil.PotentiallyCheatedWith(pActor, target);
            TolUtil.PotentiallyCheatedWith(target, pActor);
            pActor.becomeLoversWith(target);
        }

        target.cancelAllBeh();

        pActor.addStatusEffect("went_on_date");
        target.addStatusEffect("went_on_date");
        
        TolUtil.ChangeIntimacyHappinessBy(pActor, happiness);
        TolUtil.ChangeIntimacyHappinessBy(target, happiness);
        
        pActor.data.removeFloat("date_happiness");
        
        TolUtil.Debug("The date for "+pActor.getName()+" and "+target.getName() + " has finalized! Total happiness: "+happiness);
        return BehResult.Continue;
    }
}