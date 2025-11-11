using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(Skill_Grenade))]
public static class SkillGrenadePatch
{
    // 日志模块
    private static readonly GrenadeFishing.Utils.Logger L = GrenadeFishing.Utils.Log.GetLogger();
    // 在类初始化时，由你定义的局部布尔变量控制该文件日志：
    private static bool LocalLogs = true; // 你可以在别处修改这个变量
    static SkillGrenadePatch()
    {
        L.SetEnabled(LocalLogs); // 一次设置即可
    }
    
    [HarmonyPostfix]
    [HarmonyPatch("OnRelease")]
    public static void Postfix(Skill_Grenade __instance, Grenade ___grenadePfb)
    {
        if (__instance.fromItem != null)
        {
            // 直接访问fromItem.Value
            int itemValue = __instance.fromItem.Value;
            L.DebugMsg($"[炸鱼保底调试] Skill_Grenade.OnRelease: 手雷价格={itemValue}");
            
   // 记录最近一次手雷价格，供掉落逻辑读取
   GrenadeFishing.Utils.GrenadePriceTracker.SetLastGrenadeValue(itemValue);
            L.DebugMsg($"[炸鱼保底调试] Skill_Grenade.OnRelease: 已设置手雷价格到GrenadePriceTracker");
        }
        else
        {
            L.DebugMsg($"[炸鱼保底调试] Skill_Grenade.OnRelease: __instance.fromItem为null，无法获取手雷价格");
        }
    }
}
