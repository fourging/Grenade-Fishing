using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

// 通过 Harmony 拦截游戏原版 Grenade.Explode，在爆炸发生时打印手雷价格
[HarmonyPatch(typeof(Grenade))]
public static class GrenadeExplodePatch
{
	// 日志模块
	private static readonly GrenadeFishing.Utils.Logger L = GrenadeFishing.Utils.Log.GetLogger();
	// 在类初始化时，由你定义的局部布尔变量控制该文件日志：
	private static bool LocalLogs = true; // 你可以在别处修改这个变量
	static GrenadeExplodePatch()
	{
		L.SetEnabled(LocalLogs); // 一次设置即可
	}
	
	[HarmonyPostfix]
	[HarmonyPatch("Explode")]
	public static void Postfix(Grenade __instance)
	{
		try
		{
			GrenadeFishing.Utils.GrenadeExplosionTracker.NotifyExplosion(__instance.transform.position);
		}
		catch (Exception ex)
		{
			L.Warn($"[手雷炸鱼] harmony补丁出错: {ex.Message}", ex);
		}
	}
}


