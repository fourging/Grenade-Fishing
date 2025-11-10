using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace GrenadeFishing
{
	/// <summary>
	/// GrenadeExplosionTracker
	/// - 可选：在场景中查找名称包含 Grenade 的组件并订阅其零参数 UnityEvent（onExplodeEvent）
	/// - 当爆炸发生时，调用 WaterRegionHelper.IsPointInWater 并触发事件（供炸鱼逻辑订阅）
	/// - 也提供静态 API NotifyExplosion(Vector3) 供外部主动上报爆炸
	/// </summary>
	public class GrenadeExplosionTracker : MonoBehaviour
	{
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

		[Header("Auto Scan (Optional)")]
		[Tooltip("是否启用自动扫描并尝试订阅 Grenade 的 onExplodeEvent (UnityEvent 无参)")]
		public bool enableAutoScanGrenades = false;

		[Tooltip("自动扫描频率（秒）")]
		public float scanInterval = 2f;

		[Tooltip("用于匹配手雷组件类型名的关键字（大小写敏感）")]
		public string grenadeComponentNameHint = "Grenade";

		[Tooltip("尝试订阅的 UnityEvent 字段或属性名（要求为无参 UnityEvent）")]
		public string explodeUnityEventMemberName = "onExplodeEvent";

		private float _scanTimer;

		// 跟踪：已订阅的对象 -> (UnityEvent, UnityAction) 便于解除订阅
		private readonly Dictionary<UnityEngine.Object, (UnityEvent evt, UnityAction action)> _subscriptions =
			new Dictionary<UnityEngine.Object, (UnityEvent, UnityAction)>();

		private void Awake()
		{
			if (SpawnFishAt == null)
			{
				SpawnFishAt = DefaultSpawnFishAt;
			}
		}

		private void Start()
		{
			_scanTimer = 0f;
			if (enableAutoScanGrenades)
			{
				ScanAndSubscribe();
			}
		}

		private void Update()
		{
			if (!enableAutoScanGrenades) return;

			_scanTimer -= Time.deltaTime;
			if (_scanTimer <= 0f)
			{
				_scanTimer = Mathf.Max(0.2f, scanInterval);
				ScanAndSubscribe();
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

		/// <summary>
		/// 扫描场景并自动订阅疑似手雷组件上的无参 UnityEvent。
		/// 说明：仅支持无参 UnityEvent（UnityEvent），不支持 UnityEvent&lt;T&gt; 泛型。
		/// </summary>
		public void ScanAndSubscribe()
		{
			try
			{
				var behaviours = FindObjectsOfType<MonoBehaviour>();
				for (int i = 0; i < behaviours.Length; i++)
				{
					var mb = behaviours[i];
					if (mb == null) continue;
					var t = mb.GetType();
					if (!t.Name.Contains(grenadeComponentNameHint)) continue;
					if (_subscriptions.ContainsKey(mb)) continue;

					if (TrySubscribeToNoArgUnityEvent(mb, explodeUnityEventMemberName))
					{
						// 订阅成功
					}
				}

				// 清理失效对象
				var dead = new List<UnityEngine.Object>();
				foreach (var kv in _subscriptions)
				{
					if (kv.Key == null) dead.Add(kv.Key);
				}
				for (int i = 0; i < dead.Count; i++)
				{
					_subscriptions.Remove(dead[i]);
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[GrenadeExplosionTracker] Scan failed: {ex.Message}");
			}
		}

		/// <summary>
		/// 手动订阅：传入一个组件实例，尝试在其上找到无参 UnityEvent 并订阅
		/// </summary>
		/// <param name="grenadeLike">疑似手雷组件</param>
		/// <param name="unityEventMemberName">UnityEvent 字段或属性名</param>
		/// <returns>成功与否</returns>
		public bool SubscribeTo(Component grenadeLike, string unityEventMemberName = null)
		{
			if (grenadeLike == null) return false;
			return TrySubscribeToNoArgUnityEvent(grenadeLike, unityEventMemberName ?? explodeUnityEventMemberName);
		}

		/// <summary>
		/// 手动取消订阅（如果之前是通过本类订阅成功的）
		/// </summary>
		public void UnsubscribeFrom(Component grenadeLike)
		{
			if (grenadeLike == null) return;
			if (_subscriptions.TryGetValue(grenadeLike, out var sub))
			{
				try
				{
					sub.evt.RemoveListener(sub.action);
				}
				catch { /* 忽略移除错误 */ }
				_subscriptions.Remove(grenadeLike);
			}
		}

		private bool TrySubscribeToNoArgUnityEvent(Component target, string memberName)
		{
			try
			{
				var t = target.GetType();
				// 先字段，后属性
				UnityEvent foundEvent = null;

				var field = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (field != null && typeof(UnityEvent).IsAssignableFrom(field.FieldType))
				{
					foundEvent = (UnityEvent)field.GetValue(target);
				}
				if (foundEvent == null)
				{
					var prop = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (prop != null && typeof(UnityEvent).IsAssignableFrom(prop.PropertyType))
					{
						foundEvent = (UnityEvent)prop.GetValue(target, null);
					}
				}

				if (foundEvent == null)
				{
					// 未找到无参 UnityEvent，提示用户改用 NotifyExplosion 或调整配置
					return false;
				}

				UnityAction action = () =>
				{
					// 调试：列出手雷自身包围盒半径范围内的碰撞体（帮助定位误判）
					DebugListNearbyColliders(target, target.transform.position);
					OnGrenadeExploded(target.transform.position);
				};
				foundEvent.AddListener(action);
				_subscriptions[target] = (foundEvent, action);
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[GrenadeExplosionTracker] Subscribe failed on {target}: {ex.Message}");
				return false;
			}
		}

		private void OnGrenadeExploded(Vector3 pos)
		{
			NotifyExplosion(pos);
		}

		private static void DebugListNearbyColliders(Component source, Vector3 pos)
		{
			try
			{
				Collider grenadeCollider = source != null ? source.gameObject.GetComponent<Collider>() : null;
				Vector3 center = pos;
				float radius = 1.0f;

				if (grenadeCollider != null)
				{
					var b = grenadeCollider.bounds;
					center = b.center;
					radius = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z)) + 0.2f;
				}

				var list = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Collide);
				for (int i = 0; i < list.Length; i++)
				{
					var c = list[i];
					if (c == null) continue;
					Debug.Log($"手雷爆炸于碰撞箱：{c.gameObject.name} / layer: {LayerMask.LayerToName(c.gameObject.layer)}");
				}
			}
			catch
			{
				// 忽略调试输出异常
			}
		}

		private static void DefaultSpawnFishAt(Vector3 pos)
		{
			Debug.Log($"[GrenadeExplosionTracker] Explosion at {pos} is on water -> spawn fish (placeholder).");
			// 在此接入钓鱼掉落表或生成鱼实体
		}
	}
}

