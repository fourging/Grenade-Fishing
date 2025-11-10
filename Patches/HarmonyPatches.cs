using System;
using HarmonyLib;
using UnityEngine;

// 全局爆炸创建入口 Hook：ExplosionManager.CreateExplosion
[HarmonyPatch(typeof(ExplosionManager))]
public static class ExplosionManagerPatch
{
	[HarmonyPostfix]
	[HarmonyPatch("CreateExplosion")]
	public static void Postfix(Vector3 center)
	{
		try
		{
			GrenadeFishing.GrenadeExplosionTracker.NotifyExplosion(center);
		}
		catch (Exception ex)
		{
			Debug.LogWarning($"[GrenadeFishing] ExplosionManagerPatch.Postfix error: {ex.Message}");
		}
	}
}


