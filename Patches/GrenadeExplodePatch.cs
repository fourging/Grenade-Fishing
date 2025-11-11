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
			if (__instance == null) return;

			// 反射读取 private ItemAgent bindedAgent
			var bindedAgentField = typeof(Grenade).GetField("bindedAgent", BindingFlags.Instance | BindingFlags.NonPublic);
			if (bindedAgentField == null) return;

			var agent = bindedAgentField.GetValue(__instance);
			if (agent == null) return;

			// 反射获取 agent.Item
			var itemProp = agent.GetType().GetProperty("Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (itemProp == null) return;
			var item = itemProp.GetValue(agent, null);
			if (item == null) return;

			// 反射获取 item.Value (int)
			var valueProp = item.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (valueProp == null) return;
			object valueObj = valueProp.GetValue(item, null);
			if (valueObj == null) return;

			// 转为 int 并打印
			int itemValue;
			if (valueObj is int iv)
			{
				itemValue = iv;
			}
			else
			{
				// 尝试可转换类型
				itemValue = Convert.ToInt32(valueObj);
			}
			Debug.Log($"手雷价格: {itemValue}");
		}
		catch (Exception ex)
		{
			Debug.LogWarning($"[GrenadeFishing] GrenadeExplodePatch.Postfix error: {ex.Message}");
		}
	}
}


