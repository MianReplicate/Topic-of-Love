using ai.behaviours;

namespace Topic_of_Love.Mian.CustomAssets.AI.CustomBehaviors.ivf;
public class BehStartSexualIvf : BehaviourActionActor
    {
        private Actor _target;
        private BehResult Cancel()
        {
            _target.cancelAllBeh();
            return BehResult.Stop;
        }
        public override BehResult execute(Actor pActor)
        {
            TolUtil.Debug("Actually starting sexual ivf for "+pActor.getName());
            if (pActor.beh_actor_target == null || pActor.beh_building_target == null)
            {
                TolUtil.Debug(pActor.getName()+": Cancelled from starting sexual ivf target because actor was null");
                return BehResult.Stop;
            }

            _target = pActor.beh_actor_target.a;

            var aCanBePregnant = pActor.IsAbleToBecomePregnant();
            var bCanBePregnant = _target.IsAbleToBecomePregnant();
            Actor pregnantActor;
            if (aCanBePregnant && bCanBePregnant)
                pregnantActor = Randy.randomBool() ? pActor : _target;
            else
                pregnantActor = aCanBePregnant ? pActor : bCanBePregnant ? _target : null; 

            if (pregnantActor == null)
                return Cancel();
            
            var nonPregnantActor = pregnantActor == pActor ? _target : pActor;

            pregnantActor.data.set("familyParentA", pActor.getID());
            // if (pActor.hasLover())
            // {
            //     pregnantActor.data.set("familyParentB", pActor.lover.getID());
            // }
            
            (new BehCheckForBabiesFromSexualReproduction()).checkFamily(pActor, pActor.lover);

            var reproductionStrategy = pregnantActor.subspecies.getReproductionStrategy();
            switch (reproductionStrategy)
            {
                case ReproductiveStrategy.Egg:
                case ReproductiveStrategy.SpawnUnitImmediate:
                    BabyMaker.makeBabiesViaSexual(pregnantActor, pregnantActor, _target);
                    pregnantActor.subspecies.counterReproduction();
                    break;
                case ReproductiveStrategy.Pregnancy:
                    var maturationTimeSeconds = pregnantActor.getMaturationTimeSeconds();
                    pregnantActor.data.set("otherParent", nonPregnantActor.getID());

                    BabyHelper.babyMakingStart(pregnantActor);
                    pregnantActor.addStatusEffect("pregnant", maturationTimeSeconds);
                    pregnantActor.subspecies.counterReproduction();
                    break;
            }   
            TolUtil.Debug("Sexual ivf successful for "+pActor.getName());

            return BehResult.Continue;
        }
    }
