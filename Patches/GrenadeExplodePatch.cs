using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

// 通过 Harmony 拦截游戏原版 Grenade.Explode，在爆炸发生时打印手雷价格
[HarmonyPatch(typeof(Grenade))]
public static class GrenadeExplodePatch
{
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
			Debug.LogWarning($"[手雷炸鱼] harmony补丁出错: {ex.Message}");
		}
	}
}


