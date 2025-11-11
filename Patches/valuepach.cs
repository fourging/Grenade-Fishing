using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(Skill_Grenade))]
public static class SkillGrenadePatch
{
    [HarmonyPostfix]
    [HarmonyPatch("OnRelease")]
    public static void Postfix(Skill_Grenade __instance, Grenade ___grenadePfb)
    {
        if (__instance.fromItem != null)
        {
            // 直接访问fromItem.Value
            int itemValue = __instance.fromItem.Value;
            Debug.Log($"手雷价格: {itemValue}");
            
			// 记录最近一次手雷价格，供掉落逻辑读取
			GrenadeFishing.Utils.GrenadePriceTracker.SetLastGrenadeValue(itemValue);
        }
    }
}
