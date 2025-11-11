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
            
            // 可以将价格信息存储在手雷实例中，供后续使用
            // 例如通过组件或字典
        }
    }
}
