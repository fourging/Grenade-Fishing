using System;
using HarmonyLib;
using UnityEngine;

// 全局爆炸创建入口 Hook：ExplosionManager.CreateExplosion
[HarmonyPatch(typeof(ExplosionManager))] //鸭科夫中爆炸管理器类名
public static class ExplosionManagerPatch // 我的类名
{
	[HarmonyPostfix] // 后置钩子
	[HarmonyPatch("CreateExplosion")] // 鸭科夫中爆炸管理器类中的CreateExplosion方法
	public static void Postfix(Vector3 center) // 我的方法名
	{
		try
		{
			GrenadeFishing.Utils.GrenadeExplosionTracker.NotifyExplosion(center);
		}
		catch (Exception ex)
		{
			Debug.LogWarning($"[GrenadeFishing] ExplosionManagerPatch.Postfix error: {ex.Message}");
		}
	}
}


