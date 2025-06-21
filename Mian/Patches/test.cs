using System;
using System.Collections;
using db;
using HarmonyLib;
using UnityEngine;

namespace Topic_of_Love.Mian.Patches;

public class test
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapBox), nameof(MapBox.Update))]
    public static void grr()
    {
        // Debug.Log("RAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA!");
    }
}