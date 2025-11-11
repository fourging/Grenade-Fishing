using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using GrenadeFishing.Utils;

namespace GrenadeFishing.Utils
{
	/// <summary>
	/// GrenadeExplosionTracker
	/// - 可选：在场景中查找名称包含 Grenade 的组件并订阅其零参数 UnityEvent（onExplodeEvent）
	/// - 当爆炸发生时，调用 WaterRegionHelper.IsPointInWater 并触发事件（供炸鱼逻辑订阅）
	/// - 也提供静态 API NotifyExplosion(Vector3) 供外部主动上报爆炸
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

		[Tooltip("用于匹配手雷组件类型名的关键字（大小写敏感）")]
		public string grenadeComponentNameHint = "Grenade";

		[Tooltip("尝试订阅的 UnityEvent 字段或属性名（要求为无参 UnityEvent）")]
		public string explodeUnityEventMemberName = "onExplodeEvent";

		[Header("Diagnostics")]
		[Tooltip("输出订阅/扫描等诊断信息")]
		public bool diagnosticLogging = false;
		[Tooltip("在爆炸时打印附近碰撞体（高开销，仅调试时开启）")]
		public bool logNearbyCollidersOnExplode = false;

		private float _scanTimer;

		// 跟踪：已订阅的对象 -> (UnityEvent, UnityAction) 便于解除订阅
		private readonly Dictionary<UnityEngine.Object, (UnityEvent evt, UnityAction action)> _subscriptions =
			new Dictionary<UnityEngine.Object, (UnityEvent, UnityAction)>();

		// 类型解析与成员访问缓存，避免重复反射
		private static readonly Dictionary<string, Type> _typeNameCache = new Dictionary<string, Type>();
		private static readonly Dictionary<Type, Func<Component, UnityEvent>> _eventAccessorCache =
			new Dictionary<Type, Func<Component, UnityEvent>>();

		private static Collider[] _nearbyDebugBuffer; // 仅在启用调试时使用

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



		/// <summary>
		/// 直接订阅场景中所有 Grenade 的 onExplodeEvent（无需反射）。
		/// </summary>
		public int SubscribeExistingGrenadesDirect(int maxCount = 2147483647)
		{
			int added = 0;
			try
			{
				var grenades = GameObject.FindObjectsOfType<Grenade>();
				for (int i = 0; i < grenades.Length; i++)
				{
					if (added >= maxCount) break;
					var g = grenades[i];
					if (g == null) continue;
					if (_subscriptions.ContainsKey(g)) continue;
					var evt = g.onExplodeEvent;
					if (evt == null) continue;
					UnityAction action = () =>
					{
						if (logNearbyCollidersOnExplode)
						{
							DebugListNearbyCollidersNonAlloc(g, g.transform.position);
						}
						OnGrenadeExploded(g.transform.position);
					};
					evt.AddListener(action);
					_subscriptions[g] = (evt, action);
					added++;
				}
			}
			catch (Exception ex)
			{
				L.Warn($"[GrenadeExplosionTracker] Direct subscribe failed: {ex.Message}", ex);
			}
			if (diagnosticLogging)
			{
				L.Info($"[GrenadeExplosionTracker] Direct subscribed grenades: +{added}, totalSubs={_subscriptions.Count}");
			}
			return added;
		}

		/// <summary>
		/// 请求一次性延迟直接订阅（适合在“开始使用爆炸物”后一小段时间执行，避免轮询）。
		/// </summary>
		public void RequestOneShotDirectSubscribe(float delaySeconds = 0.05f, int maxCount = 16)
		{
			StartCoroutine(OneShotDirectSubscribe(delaySeconds, maxCount));
		}

		private IEnumerator OneShotDirectSubscribe(float delaySeconds, int maxCount)
		{
			if (delaySeconds > 0f)
			{
				yield return new WaitForSeconds(delaySeconds);
			}
			SubscribeExistingGrenadesDirect(maxCount);
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

		private static UnityEvent GetCachedNoArgUnityEvent(Component instance, Type type, string memberName)
		{
			if (!_eventAccessorCache.TryGetValue(type, out var accessor))
			{
				// 构建访问器：优先字段，其次属性
				UnityEvent Accessor(Component c)
				{
					var t = c.GetType();
					var field = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (field != null && typeof(UnityEvent).IsAssignableFrom(field.FieldType))
					{
						return (UnityEvent)field.GetValue(c);
					}
					var prop = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (prop != null && typeof(UnityEvent).IsAssignableFrom(prop.PropertyType))
					{
						return (UnityEvent)prop.GetValue(c, null);
					}
					return null;
				}
				accessor = Accessor;
				_eventAccessorCache[type] = accessor;
			}
			return accessor(instance);
		}

		private static Type ResolveTypeByName(string nameHint)
		{
			if (string.IsNullOrEmpty(nameHint)) return null;
			if (_typeNameCache.TryGetValue(nameHint, out var cached)) return cached;
			Type found = null;
			var asms = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < asms.Length && found == null; i++)
			{
				try
				{
					var types = asms[i].GetTypes();
					for (int j = 0; j < types.Length; j++)
					{
						var t = types[j];
						// 优先精确匹配，其次包含匹配
						if (t.Name == nameHint || t.FullName == nameHint || t.Name.Contains(nameHint))
						{
							found = t;
							break;
						}
					}
				}
				catch
				{
					// 某些动态程序集可能抛出异常，忽略
				}
			}
			_typeNameCache[nameHint] = found;
			return found;
		}

		private void OnGrenadeExploded(Vector3 pos)
		{
			NotifyExplosion(pos);
		}

		private static void DebugListNearbyCollidersNonAlloc(Component source, Vector3 pos)
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

				if (_nearbyDebugBuffer == null || _nearbyDebugBuffer.Length < 64)
				{
					_nearbyDebugBuffer = new Collider[64];
				}
				int count = Physics.OverlapSphereNonAlloc(center, radius, _nearbyDebugBuffer, ~0, QueryTriggerInteraction.Collide);
				for (int i = 0; i < count; i++)
				{
					var c = _nearbyDebugBuffer[i];
					if (c == null) continue;
					L.DebugMsg($"手雷爆炸于碰撞箱：{c.gameObject.name} / layer: {LayerMask.LayerToName(c.gameObject.layer)}");
				}
			}
			catch
			{
				// 忽略调试输出异常
			}
		}

		private static void DefaultSpawnFishAt(Vector3 pos)
		{
			L.Info($"[GrenadeExplosionTracker] Explosion at {pos} is on water -> spawn fish (placeholder).");
			// 在此接入钓鱼掉落表或生成鱼实体
		}
	}
}

