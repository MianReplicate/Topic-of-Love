using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Topic_of_Love.Mian.CustomAssets.Traits;

public class BaseTraits<T, TR> 
    where T : BaseTrait<T> 
    where TR : BaseTraitLibrary<T>
{
    private TR _library;
    protected List<T> _assets = new List<T>();
    private string _id;
    private bool _isPartOfMethod;
    
    protected void Init(string id, bool isPartOfMethod=true)
    {
        _id = id.ToLower();
        _isPartOfMethod = isPartOfMethod;
        _library = (TR) AssetManager._instance._list.Find(library => typeof(TR) == library.GetType());
    }

    protected virtual void Finish()
    {
        foreach (T pObject in _assets)
        {
            if (pObject.spawn_random_trait_allowed)
                _library._pot_allowed_to_be_given_randomly.Add(pObject);
            
            if (pObject.opposite_list != null)
            {
                pObject.opposite_traits = new HashSet<T>();
                foreach (var oppositeId in pObject.opposite_list)
                {
                    var oppositeTrait = _assets.Find(trait => trait.id == oppositeId);
                    if(oppositeTrait == null)
                        oppositeTrait = _library.list.Find(trait => trait.id == oppositeId);
                    pObject.opposite_traits.Add(oppositeTrait);
                }
            }
        }   
    }
    
    protected T Add(T trait, IEnumerable<string> actorAssets = null, IEnumerable<string> biomeAssets = null)
    {
        string methodCall;
        if (_isPartOfMethod)
        {
            var addToMethod = _id.Substring(0, 1).ToUpper() + _id.Substring(1);
            methodCall = "add" + addToMethod + "Trait";
        }
        else
        {
            methodCall = "addTrait";
        }

        if (actorAssets != null)
        {
            var method = AccessTools.Method(typeof(ActorAsset), methodCall, new[] {typeof(string)});
            
            trait.default_for_actor_assets = new List<ActorAsset>();
            
            foreach (var asset in actorAssets)
            {
                var actorAsset = AssetManager.actor_library.get(asset);
                if (actorAsset != null)
                {
                    method.Invoke(actorAsset, new object[] { trait.id });
                    trait.default_for_actor_assets.Add(actorAsset);
                }
            }
        }

        if (biomeAssets != null)
        {
            var method = AccessTools.Method(typeof(BiomeAsset), methodCall, new[] { typeof(string) });
            foreach (var asset in biomeAssets)
            {
                var biomeAsset = AssetManager.biome_library.get(asset);
                if(biomeAsset != null)
                    method.Invoke(biomeAsset, new object[]{trait.id});
            }

        }

        if (trait.spawn_random_trait_allowed)
            _library._pot_allowed_to_be_given_randomly.AddTimes(trait.spawn_random_rate, trait);

        trait.path_icon = "ui/Icons/"+_id+"_traits/" + trait.id;
        _assets.Add(trait);
        return _library.add(trait);
    }
}