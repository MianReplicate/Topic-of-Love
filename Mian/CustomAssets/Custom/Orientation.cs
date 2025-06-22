using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Topic_of_Love.Mian.CustomAssets.Custom;

public class Orientation
{
    public static readonly List<Orientation> Orientations = new();
    public readonly string OrientationType;
    public readonly string SexualPathLocale;
    public readonly string RomanticPathLocale;
    public readonly string DescriptionLocale;
    public readonly bool IsHomo;
    public readonly string HexCode;
    public readonly bool IsHetero;
    public readonly string SexualPathIcon;
    public readonly string RomanticPathIcon;
    private Sprite _sexualSprite;
    private Sprite _romanticSprite;
    public readonly Func<Actor, bool, bool> Criteria;

    private Orientation(string orientationType, string sexualPathLocale, string romanticPathLocale, string descriptionLocale,
        string sexualPathIcon, string romanticPathIcon, bool isHomo, bool isHetero, string hexCode,
        Func<Actor, bool, bool> criteriaCheck)
    {
        OrientationType = orientationType;
        SexualPathLocale = sexualPathLocale;
        RomanticPathLocale = romanticPathLocale;
        DescriptionLocale=descriptionLocale;
        Criteria = criteriaCheck;
        IsHomo = isHomo;
        HexCode = hexCode;
        IsHetero = isHetero;
        SexualPathIcon = sexualPathIcon;
        RomanticPathIcon = romanticPathIcon;
    }

    public Sprite GetSprite(bool sexual)
    {
        if (sexual)
        {
            if (_sexualSprite == null)
                _sexualSprite = SpriteTextureLoader.getSprite("ui/Icons/" + SexualPathIcon);
                // _sexualSprite = Resources.Load<Sprite>("ui/Icons/" + SexualPathIcon);
            return _sexualSprite;
        }

        if (_romanticSprite == null)
            _romanticSprite = SpriteTextureLoader.getSprite("ui/Icons/" + RomanticPathIcon);
            // _romanticSprite = Resources.Load<Sprite>("ui/Icons/" + RomanticPathIcon);
        return _romanticSprite;
    }

    public static Orientation Create(string orientation, string romanticVariant, bool isHomo, bool isHetero,
        string hexCode, Func<Actor, bool, bool> fitsCriteria)
    {
        var pathLocale = "orientations_" + orientation;
        var romanticPathLocale = "orientations_" + orientation + "_romantic";
        var descriptionLocale = pathLocale + "_description";
        var sexualPathIcon = "orientations/" + orientation;
        var romanticPathIcon = "orientations/" + romanticVariant;
        var orientationType = new Orientation(orientation, pathLocale, romanticPathLocale, descriptionLocale, sexualPathIcon,
            romanticPathIcon, isHomo, isHetero, hexCode, fitsCriteria);
        Orientations.Add(orientationType);

        LM.AddToCore(pathLocale, char.ToUpper(orientation.First()) + orientation.Substring(1));
        LM.AddToCore(romanticPathLocale, char.ToUpper(romanticVariant.First()) + romanticVariant.Substring(1));
        LM.AddToCore("count_" + pathLocale, char.ToUpper(orientation.First()) + orientation.Substring(1) + "s");
        LM.AddToCore("count_" + romanticPathLocale, char.ToUpper(romanticVariant.First()) + romanticVariant.Substring(1) + "s");
        return orientationType;
    }

    public static Orientation GetOrientation(string orientation)
    {
        return Orientations.Find(orientationType => orientationType.OrientationType.Equals(orientation));
    }

    public static Orientation GetOrientation(Actor actor, bool sexual)
    {
        var text = sexual ? "sexual_orientation" : "romantic_orientation";
        actor.data.get(text, out var orientation, "");
        return GetOrientation(orientation);
    }

    public static bool IsAHomo(Actor actor)
    {
        return GetOrientation(actor, false).IsHomo || GetOrientation(actor, true).IsHomo;
    }

    public static bool IsAHetero(Actor actor)
    {
        return GetOrientation(actor, false).IsHetero || GetOrientation(actor, true).IsHetero;
    }
}

public class Orientations
{
    public static void Init()
    {
        Orientation.Create("lesbian", "lesbiromantic", true, false, "#FF9A56", (actor, isSexual) =>
        {
            if (Preferences.Dislikes(actor, isSexual))
                return false;

            if (!Preferences.IdentifiesAsMan(actor) && actor.isSapient())
            {
                var preferredIdentities = Preferences.GetActorPreferencesFromType(actor, "identity", isSexual);

                if (preferredIdentities.Count == 1
                    && (Preferences.HasPreference(actor, "female", isSexual) ||
                        Preferences.HasPreference(actor, "xenogender", isSexual)))
                    return true;

                if (preferredIdentities.Count == 2 &&
                    Preferences.HasPreference(actor, "female", isSexual) &&
                    Preferences.HasPreference(actor, "xenogender", isSexual))
                    return true;
            }

            return false;
        });
        Orientation.Create("gay", "gayromantic", true, false, "#26CEAA", (actor, isSexual) =>
        {
            if (Preferences.Dislikes(actor, isSexual))
                return false;

            if (!Preferences.IdentifiesAsWoman(actor) && actor.isSapient())
            {
                var preferredIdentities = Preferences.GetActorPreferencesFromType(actor, "identity", isSexual);

                if (preferredIdentities.Count == 1
                    && (Preferences.HasPreference(actor, "male", isSexual) ||
                        Preferences.HasPreference(actor, "xenogender", isSexual)))
                    return true;

                if (preferredIdentities.Count == 2 && Preferences.HasPreference(actor, "male", isSexual) &&
                    Preferences.HasPreference(actor, "xenogender", isSexual))
                    return true;
            }

            return false;
        });
        Orientation.Create("straight", "straightromantic", false, true, "#FFFFFF", (actor, isSexual) =>
        {
            if (Preferences.Dislikes(actor, isSexual))
                return false;

            if (actor.isSapient())
            {
                var preferredIdentities = Preferences.GetActorPreferencesFromType(actor, "identity", isSexual);
                if (preferredIdentities.Count == 1)
                {
                    if (((PreferenceTrait)preferredIdentities.First()).WithoutOrientationID !=
                        Preferences.GetIdentity(actor))
                        return true;
                }
            }

            return false;
        });
        Orientation.Create("heterosexual", "heteroromantic", false, true, "#FFFFFF", (actor, isSexual) =>
        {
            if (Preferences.Dislikes(actor, isSexual))
                return false;

            if (!actor.isSapient())
            {
                var preferredIdentities = Preferences.GetActorPreferencesFromType(actor, "identity", isSexual);
                if (preferredIdentities.Count == 1)
                {
                    if (((PreferenceTrait)preferredIdentities.First()).WithoutOrientationID !=
                        Preferences.GetIdentity(actor))
                        return true;
                }
            }

            return false;
        });
        Orientation.Create("homosexual", "homoromantic", true, false, "#BB07DF", (actor, isSexual) =>
        {
            if (Preferences.Dislikes(actor, isSexual))
                return false;
            
            if (!actor.isSapient())
            {
                var preferredIdentities = Preferences.GetActorPreferencesFromType(actor, "identity", isSexual);

                if (actor.isSexFemale())
                {
                    if (preferredIdentities.Count == 1
                        && Preferences.HasPreference(actor, "female", isSexual))
                        return true;
                }
                else
                {
                    if (preferredIdentities.Count == 1
                        && Preferences.HasPreference(actor, "male", isSexual))
                        return true;
                }
            }

            return false;
        });
        Orientation.Create("bisexual", "biromantic", true, true, "#9B4F96", (actor, isSexual) =>
        {
            if (Preferences.Dislikes(actor, isSexual))
                return false;

            if (Preferences.GetActorPreferencesFromType(actor, "identity", isSexual).Count >= 2)
                return true;
            return false;
        });
        Orientation.Create("pansexual", "panromantic", true, true, "#FFD800", (actor, isSexual) =>
        {
            if (Preferences.Dislikes(actor, isSexual))
                return false;

            if (Preferences.GetActorPreferencesFromType(actor, "identity", isSexual).Count == 0)
                return true;
            return false;
        });

        Orientation.Create("asexual", "aromantic", false, false, "#FFFFFF", Preferences.Dislikes);
    }

    // homosexuals count as gay, lesbian, etc (and same goes for heterosexual)
    public static bool HasOrientation(Actor actor, string id, bool sexual)
    {
        var orientation = GetOrientationFromActor(actor, sexual);
        if (id.Equals("homosexual"))
            return orientation.IsHomo;
        if(id.Equals("heterosexual"))
            return orientation.IsHetero;
        
        return orientation.OrientationType.Equals(id);
    }
    
    public static Orientation GetOrientationFromActor(Actor actor, bool sexual = false)
    {
        var orientations =
            Orientation.Orientations.Where(orientationType => orientationType.Criteria(actor, sexual)).ToList();

        return orientations.GetRandom(); // at the very least should be pansexual
    }

    public static void RollOrientationLabel(Actor actor)
    {
        var sexualOrientation = GetOrientationFromActor(actor, true);
        var romanticOrientation = GetOrientationFromActor(actor);
        actor.data.set("romantic_orientation", romanticOrientation.OrientationType);
        actor.data.set("sexual_orientation", sexualOrientation.OrientationType);
    }

    public static void CreateOrientationBasedOnPrefChange(Actor actor, PreferenceTrait newTrait)
    {
        var orientation = GetOrientationFromActor(actor, newTrait.IsSexual);
        if (newTrait.IsSexual)
            actor.data.set("sexual_orientation", orientation.OrientationType);
        else
            actor.data.set("romantic_orientation", orientation.OrientationType);
    }
}