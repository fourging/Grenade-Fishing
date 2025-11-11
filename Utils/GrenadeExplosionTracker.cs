using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

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

		[Tooltip("单次扫描最多新增订阅的数量上限（用于摊平成本）")]
		public int maxSubscribePerScan = 8;

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

		// 扫描过程的临时缓冲，减少 GC 分配
		private static readonly List<Component> _scanCandidates = new List<Component>(128);
		private static readonly List<UnityEngine.Object> _deadSubs = new List<UnityEngine.Object>(32);
		private static Collider[] _nearbyDebugBuffer; // 仅在启用调试时使用

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
			// 已由 Harmony 直接拦截 Grenade.Explode 与全局 ExplosionManager 替代本方法的目的功能
			// 为空实现以避免额外的反射/轮询开销
			if (diagnosticLogging)
			{
				Debug.Log("[GrenadeExplosionTracker] ScanAndSubscribe 已被 Harmony 替代，跳过扫描。");
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
				Debug.LogWarning($"[GrenadeExplosionTracker] Direct subscribe failed: {ex.Message}");
			}
			if (diagnosticLogging)
			{
				Debug.Log($"[GrenadeExplosionTracker] Direct subscribed grenades: +{added}, totalSubs={_subscriptions.Count}");
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
				UnityEvent foundEvent = GetCachedNoArgUnityEvent(target, t, memberName);

				if (foundEvent == null)
				{
					// 未找到无参 UnityEvent，提示用户改用 NotifyExplosion 或调整配置
					return false;
				}

				UnityAction action = () =>
				{
					// 调试：列出手雷自身包围盒半径范围内的碰撞体（帮助定位误判）
					if (logNearbyCollidersOnExplode)
					{
						DebugListNearbyCollidersNonAlloc(target, target.transform.position);
					}
					OnGrenadeExploded(target.transform.position);
				};
				foundEvent.AddListener(action);
				_subscriptions[target] = (foundEvent, action);

				if (diagnosticLogging)
				{
					Debug.Log($"[GrenadeExplosionTracker] Subscribed to {t.FullName}.{memberName} on {target.name}");
				}
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[GrenadeExplosionTracker] Subscribe failed on {target}: {ex.Message}");
				return false;
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

