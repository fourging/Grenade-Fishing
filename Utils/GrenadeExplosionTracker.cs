using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using GrenadeFishing.Utils;

namespace GrenadeFishing.Utils
{
	/// <summary>
	/// GrenadeExplosionTracker
	/// - 通过Harmony补丁捕获手雷爆炸事件
	/// - 当爆炸发生时，调用 WaterRegionHelper.IsPointInWater 并触发事件（供炸鱼逻辑订阅）
	/// - 提供静态 API NotifyExplosion(Vector3) 供外部主动上报爆炸
	/// </summary>
	public class GrenadeExplosionTracker : MonoBehaviour
	{
		// 日志模块
		private static readonly GrenadeFishing.Utils.Logger L = GrenadeFishing.Utils.Log.GetLogger();
		// 在类初始化时，由你定义的局部布尔变量控制该文件日志：
		private static bool LocalLogs = true; // 你可以在别处修改这个变量
		static GrenadeExplosionTracker()
		{
			L.SetEnabled(LocalLogs); // 一次设置即可
		}
		
		/// <summary>
		/// 当有爆炸且发生在水体时触发（参数: 世界坐标）
		/// </summary>
		public static event Action<Vector3> OnWaterExplosion;

		/// <summary>
		/// 任意爆炸（不论是否水体）都会触发 (pos, wasWater)
		/// </summary>
		public static event Action<Vector3, bool> OnAnyExplosion;

		/// <summary>
		/// 炸鱼生成钩子。若未设置，则使用默认占位行为。
		/// </summary>
		public static Action<Vector3> SpawnFishAt;


		[Header("Diagnostics")]
		[Tooltip("输出诊断信息")]
		public bool diagnosticLogging = false;

		private void Awake()
		{
			if (SpawnFishAt == null)
			{
				SpawnFishAt = DefaultSpawnFishAt;
			}
		}


		/// <summary>
		/// 手动上报一次爆炸位置
		/// </summary>
		public static void NotifyExplosion(Vector3 worldPos)
		{
			bool wasWater = false;
			if (WaterRegionHelper.Instance != null)
			{
				wasWater = WaterRegionHelper.Instance.IsPointInWater(worldPos);
			}
			else
			{
				// 兜底：如果 Helper 未初始化，尽量用 Water Layer 射线快速判断
				int layer = LayerMask.NameToLayer("Water");
				if (layer >= 0)
				{
					int mask = 1 << layer;
					if (Physics.Raycast(worldPos + Vector3.up * 0.2f, Vector3.down, out var hit, 5f, mask, QueryTriggerInteraction.Collide))
					{
						wasWater = true;
					}
				}
			}

			OnAnyExplosion?.Invoke(worldPos, wasWater);
			if (wasWater)
			{
				OnWaterExplosion?.Invoke(worldPos);
				SpawnFishAt?.Invoke(worldPos);
			}
		}

		private static void DefaultSpawnFishAt(Vector3 pos)
		{
			L.Info($"[GrenadeExplosionTracker] Explosion at {pos} is on water -> spawn fish (placeholder).");
			// 在此接入钓鱼掉落表或生成鱼实体
		}
	}
}

