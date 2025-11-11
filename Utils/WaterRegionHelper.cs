using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrenadeFishing.Utils
{
	/// <summary>
	/// WaterRegionHelper
	/// - 启动时缓存场景中属于 "Water" Layer 的 Collider（如 MeshCollider）及其 world-space Bounds
	/// - 提供 IsPointInWater(Vector3) 方法：先做 bounds 包围盒快速筛选，再用射线/球铸做精确检测（layer mask）
	/// - 提供 RebuildCache() 方法用于在运行时/编辑器重新扫描水体（例如场景动态改变）
	/// - 提供 Editor Gizmos 绘制（仅 Editor）便于调试
	/// </summary>
	[DefaultExecutionOrder(-100)]
	public class WaterRegionHelper : MonoBehaviour
	{
		// 日志模块
		private static readonly GrenadeFishing.Utils.Logger L = GrenadeFishing.Utils.Log.GetLogger();
		// 在类初始化时，由你定义的局部布尔变量控制该文件日志：
		private static bool LocalLogs = true; // 你可以在别处修改这个变量
		static WaterRegionHelper()
		{
			L.SetEnabled(LocalLogs); // 一次设置即可
		}
		
		public static WaterRegionHelper Instance { get; private set; }

		[Header("Detection Settings")]
		[Tooltip("检测时向下/向上 Raycast 的最大深度（米）")]
		public float raycastDepth = 5f;

		[Tooltip("用于 SphereCast 的半径（容错宽度），0 表示不使用 SphereCast")]
		public float sphereCastRadius = 0.5f;

		[Tooltip("是否在判断前先用 bounds 包围盒进行粗略筛选（推荐开启）")]
		public bool useBoundsFilter = true;

		[Header("Proximity Check")]
		[Tooltip("使用胶囊体近邻检测的半径（米），用于判定爆炸点附近是否有水体")]
		public float nearCheckRadius = 0.6f;

		[Tooltip("近邻检测的垂直半高（米），形成上下延伸的检测胶囊")]
		public float verticalHalfExtent = 1.2f;

		[Header("Logging")]
		[Tooltip("输出详细诊断日志（用于定位误判）")]
		public bool diagnosticLogging = false;

		[Header("Debug")]
		[Tooltip("是否绘制调试 Gizmos")]
		public bool drawGizmos = true;
		public Color boundsColor = new Color(0f, 0.6f, 1f, 0.15f);
		public Color boundsWireColor = Color.cyan;
		public Color lastExplosionColor = Color.yellow;

		private int waterLayer = -1;
		private LayerMask waterMask;
		private int solidMaskCache;
		[Tooltip("额外判定为水体的图层名列表（可选），例如：\"WaterVolume\",\"River\"")]
		public string[] extraWaterLayerNames = Array.Empty<string>();

		// 缓存的 world-space bounds（来自 Collider.bounds）
		private readonly List<Collider> waterColliders = new List<Collider>();
		private readonly List<Bounds> waterBounds = new List<Bounds>();

		// 最近一次判断结果与位置（用于可视化）
		private Vector3 lastExplosionPos = Vector3.zero;
		private bool lastExplosionWasWater = false;

		private void Awake()
		{
			if (Instance == null) Instance = this;
			else
			{
				Destroy(this);
				return;
			}

			InitLayerMask();
			RebuildCache();
		}

		private void InitLayerMask()
		{
			waterLayer = LayerMask.NameToLayer("Water");
			if (waterLayer < 0)
			{
				L.Warn("[WaterRegionHelper] Layer named 'Water' not found in project layers.");
				waterMask = 0;
			}
			else
			{
				waterMask = 1 << waterLayer;
			}

			// 叠加额外图层（若存在）
			if (extraWaterLayerNames != null)
			{
				for (int i = 0; i < extraWaterLayerNames.Length; i++)
				{
					var name = extraWaterLayerNames[i];
					if (string.IsNullOrEmpty(name)) continue;
					int l = LayerMask.NameToLayer(name);
					if (l >= 0) waterMask |= (1 << l);
				}
			}

			if (diagnosticLogging)
			{
				L.Info($"[WaterRegionHelper] 初始化图层掩码: mask={waterMask}, base='Water'({waterLayer}), extra=[{string.Join(",", extraWaterLayerNames ?? Array.Empty<string>())}]");
			}

			// 预计算“非水体遮挡”掩码，避免后续每次计算
			solidMaskCache = ~waterMask;
		}

		/// <summary>
		/// 扫描场景中属于 Water layer 的 Collider 并缓存其 world bounds
		/// </summary>
		public void RebuildCache()
		{
			waterColliders.Clear();
			waterBounds.Clear();

			if (waterLayer < 0) return;

			// 获取所有 Collider 并筛选属于 waterLayer 的
			var allColliders = FindObjectsOfType<Collider>();
			for (int i = 0; i < allColliders.Length; i++)
			{
				var c = allColliders[i];
				if (c != null && c.gameObject.layer == waterLayer)
				{
					waterColliders.Add(c);
					waterBounds.Add(c.bounds);
				}
			}

			L.Info($"[WaterRegionHelper] Rebuild cache found {waterColliders.Count} water colliders.");
		}

		/// <summary>
		/// 判断 worldPos 是否位于水体区域上（允许炸鱼）
		/// </summary>
		/// <param name="worldPos">爆炸世界坐标</param>
		/// <returns>true: 视为水体爆炸；false: 非水体或无法判定</returns>
		public bool IsPointInWater(Vector3 worldPos)
		{
			lastExplosionPos = worldPos;
			lastExplosionWasWater = false;

			// 可配置的“干地硬阈值”（当爆炸点高于水面超过该值时，不允许后续近邻/球铸命中推翻为水体）
			const float dryLandHardAllowance = 0.5f;

			if (waterLayer < 0 || waterMask == 0)
			{
				if (diagnosticLogging)
				{
					L.Warn("[WaterRegionHelper] 判定失败：未找到 'Water' 图层或图层掩码为 0。");
				}
				return false;
			}

			// 1) bounds 粗筛（仅用于快速排除，不作为命中依据）
			if (useBoundsFilter && waterBounds.Count > 0)
			{
				bool insideAnyBounds = false;
				for (int i = 0; i < waterBounds.Count; i++)
				{
					if (waterBounds[i].Contains(worldPos))
					{
						insideAnyBounds = true;
						break;
					}
				}
				if (!insideAnyBounds)
				{
					// 非任何水体包围盒内，快速返回 false
					if (diagnosticLogging)
					{
						L.DebugMsg($"[WaterRegionHelper] 爆炸点 {worldPos} 不在任何缓存水体 Bounds 内 -> 非水体。");
					}
					return false;
				}
			}

			// 1.5) 预检：尝试获取垂直方向水面高度（用于后续“干地硬阈值”门禁）
			bool hasSurface = TryGetWaterSurfaceY(worldPos, out float surfaceY, out var surfaceHit);
			bool aboveDryLandHard = hasSurface && (worldPos.y > surfaceY + dryLandHardAllowance);
			if (diagnosticLogging && hasSurface)
			{
				L.DebugMsg($"[WaterRegionHelper] SurfaceY={surfaceY:F2}, delta={(worldPos.y - surfaceY):F2}, hardAllowance={dryLandHardAllowance:F2}, aboveDryLandHard={aboveDryLandHard}");
			}

			// 2) 基于垂直柱的水面判定（优先、强约束）：
			//  - 在爆炸点上方一定高度向下检测首个水面高度 waterY
			//  - 若存在水面，且爆炸点不高于水面太多（<= allowance），
			//    并且爆炸点到水面之间不存在非水体遮挡，则认定为水体
			if (IsWaterByVerticalColumn(worldPos, out var waterHitInfo))
			{
				if (diagnosticLogging)
				{
					L.DebugMsg($"[WaterRegionHelper] VerticalColumn 命中水面: name='{waterHitInfo.collider?.name}' y={waterHitInfo.point.y:F2}");
				}
				lastExplosionWasWater = true;
				return true;
			}
			// 若明显高于水面硬阈值，且垂直柱未命中水面，则提前返回，避免后续多余检测
			if (aboveDryLandHard)
			{
				if (diagnosticLogging)
				{
					L.DebugMsg("[WaterRegionHelper] 高于水面硬阈值且未命中垂直水面 -> 提前返回非水体。");
				}
				return false;
			}

			// 3) 垂直方向首碰判定：以“首个命中是否为水体”为准，可过滤部分近水误判
			{
				RaycastHit firstHit;
				float firstMaxDistance = Mathf.Max(0.01f, raycastDepth + 0.2f);
				Vector3 firstOriginDown = worldPos + Vector3.up * 0.2f;
				Vector3 firstOriginUp = worldPos - Vector3.up * 0.1f;

				if (Physics.Raycast(firstOriginDown, Vector3.down, out firstHit, firstMaxDistance, ~0, QueryTriggerInteraction.Collide))
				{
					bool isWater = IsWaterCollider(firstHit.collider);
					if (diagnosticLogging)
					{
						L.DebugMsg($"[WaterRegionHelper] FirstHit↓ hit={firstHit.collider != null} layer={firstHit.collider?.gameObject.layer} name='{firstHit.collider?.name}' dist={firstHit.distance:F2} -> {(isWater ? "WATER" : "NON-WATER")}");
					}
					if (isWater)
					{
						if (aboveDryLandHard)
						{
							if (diagnosticLogging)
							{
								L.DebugMsg("[WaterRegionHelper] FirstHit↓ 命中水体但高于水面硬阈值，拒绝。");
							}
						}
						else
						{
							lastExplosionWasWater = true;
							return true;
						}
					}
				}

				if (Physics.Raycast(firstOriginUp, Vector3.up, out firstHit, firstMaxDistance, ~0, QueryTriggerInteraction.Collide))
				{
					bool isWater = IsWaterCollider(firstHit.collider);
					if (diagnosticLogging)
					{
						L.DebugMsg($"[WaterRegionHelper] FirstHit↑ hit={firstHit.collider != null} layer={firstHit.collider?.gameObject.layer} name='{firstHit.collider?.name}' dist={firstHit.distance:F2} -> {(isWater ? "WATER" : "NON-WATER")}");
					}
					if (isWater)
					{
						if (aboveDryLandHard)
						{
							if (diagnosticLogging)
							{
								L.DebugMsg("[WaterRegionHelper] FirstHit↑ 命中水体但高于水面硬阈值，拒绝。");
							}
						}
						else
						{
							lastExplosionWasWater = true;
							return true;
						}
					}
				}
			}

			// 4) 近邻胶囊检测：在爆炸点上下一定范围内，快速判定附近是否存在水体（带视线遮挡过滤）
			{
				Vector3 p1 = worldPos + Vector3.up * Mathf.Max(0.05f, verticalHalfExtent);
				Vector3 p2 = worldPos - Vector3.up * Mathf.Max(0.05f, verticalHalfExtent);
				float r = Mathf.Max(0.05f, nearCheckRadius);

				// 使用 OverlapCapsule 收集具体命中的水体 Collider，便于诊断
				var hitsBuffer = _overlapBuffer ?? (_overlapBuffer = new Collider[32]);
				int hitCount = Physics.OverlapCapsuleNonAlloc(p1, p2, r, hitsBuffer, waterMask, QueryTriggerInteraction.Collide);
				bool capsuleHit = hitCount > 0;

				if (diagnosticLogging)
				{
					L.DebugMsg($"[WaterRegionHelper] 胶囊近邻检测 hit={capsuleHit} r={r:F2} halfH={verticalHalfExtent:F2} mask={waterMask} @ {worldPos}");
					if (capsuleHit)
					{
						for (int i = 0; i < hitCount; i++)
						{
							var c = hitsBuffer[i];
							if (c == null) continue;
							Vector3 cp;
							float dist = -1f;
							try
							{
								cp = c.ClosestPoint(worldPos);
								dist = Vector3.Distance(worldPos, cp);
							}
							catch
							{
								// 非凸 MeshCollider 等：采用 bounds 最近点近似
								var b = c.bounds;
								cp = new Vector3(
									Mathf.Clamp(worldPos.x, b.min.x, b.max.x),
									Mathf.Clamp(worldPos.y, b.min.y, b.max.y),
									Mathf.Clamp(worldPos.z, b.min.z, b.max.z)
								);
								dist = Vector3.Distance(worldPos, cp);
								if (diagnosticLogging)
								{
									L.DebugMsg($"[WaterRegionHelper]  - ClosestPoint 不可用，使用 Bounds 近似。");
								}
							}
							L.DebugMsg($"[WaterRegionHelper]  - 命中Collider: '{c.name}' layer={c.gameObject.layer} dist={dist:F2} closest={cp} boundsCenter={c.bounds.center}");

							// 视线遮挡过滤：若从爆炸点到水体最近点之间先被非水体遮挡，则忽略该命中（避免公路近水误判）
							if (!IsOccludedByNonWater(worldPos, cp))
							{
								// 干地硬阈值过滤：若明显高于水面，则忽略近邻命中
								if (aboveDryLandHard)
								{
									if (diagnosticLogging)
									{
										L.DebugMsg("[WaterRegionHelper] 胶囊命中水体但高于水面硬阈值，忽略。");
									}
								}
								else
								{
									lastExplosionWasWater = true;
									return true;
								}
							}
							else if (diagnosticLogging)
							{
								L.DebugMsg($"[WaterRegionHelper]  - 命中但被非水体遮挡，忽略。");
							}
						}
					}
				}

				// 注意：未通过遮挡过滤的命中不算作水体
			}

			// 5) 精确检测：双向（向下 + 向上）检测，优先 SphereCast（若 radius>0），否则 Raycast（并带遮挡过滤）
			RaycastHit castHit;
			// 从稍上方/稍下方开始，避免在表面边界时错过
			Vector3 castOriginDown = worldPos + Vector3.up * 0.2f;
			Vector3 castOriginUp = worldPos - Vector3.up * 0.1f;
			float castMaxDistance = Mathf.Max(0.01f, raycastDepth + 0.2f);

			if (sphereCastRadius > 0f)
			{
				bool downHit = Physics.SphereCast(castOriginDown, sphereCastRadius, Vector3.down, out castHit, castMaxDistance, waterMask, QueryTriggerInteraction.Collide);
				if (diagnosticLogging)
				{
					L.DebugMsg($"[WaterRegionHelper] SphereCast↓ hit={downHit} dist={(downHit ? castHit.distance.ToString("F2") : "-")} r={sphereCastRadius:F2} depth={castMaxDistance:F2}");
					if (downHit)
					{
						L.DebugMsg($"[WaterRegionHelper]  - 命中对象: '{castHit.collider?.name}' layer={castHit.collider?.gameObject.layer} point={castHit.point} normal={castHit.normal}");
					}
				}
				if (downHit && !IsOccludedByNonWater(worldPos, castHit.point))
				{
					if (aboveDryLandHard)
					{
						if (diagnosticLogging)
						{
							L.DebugMsg("[WaterRegionHelper] SphereCast↓ 命中水体但高于水面硬阈值，忽略。");
						}
					}
					else
					{
						lastExplosionWasWater = true;
						return true;
					}
				}

				bool upHit = Physics.SphereCast(castOriginUp, sphereCastRadius, Vector3.up, out castHit, castMaxDistance, waterMask, QueryTriggerInteraction.Collide);
				if (diagnosticLogging)
				{
					L.DebugMsg($"[WaterRegionHelper] SphereCast↑ hit={upHit} dist={(upHit ? castHit.distance.ToString("F2") : "-")} r={sphereCastRadius:F2} depth={castMaxDistance:F2}");
					if (upHit)
					{
						L.DebugMsg($"[WaterRegionHelper]  - 命中对象: '{castHit.collider?.name}' layer={castHit.collider?.gameObject.layer} point={castHit.point} normal={castHit.normal}");
					}
				}
				if (upHit && !IsOccludedByNonWater(worldPos, castHit.point))
				{
					if (aboveDryLandHard)
					{
						if (diagnosticLogging)
						{
							L.DebugMsg("[WaterRegionHelper] SphereCast↑ 命中水体但高于水面硬阈值，忽略。");
						}
					}
					else
					{
						lastExplosionWasWater = true;
						return true;
					}
				}
			}
			else
			{
				bool downHit = Physics.Raycast(castOriginDown, Vector3.down, out castHit, castMaxDistance, waterMask, QueryTriggerInteraction.Collide);
				if (diagnosticLogging)
				{
					L.DebugMsg($"[WaterRegionHelper] Raycast↓ hit={downHit} dist={(downHit ? castHit.distance.ToString("F2") : "-")} depth={castMaxDistance:F2}");
					if (downHit)
					{
						L.DebugMsg($"[WaterRegionHelper]  - 命中对象: '{castHit.collider?.name}' layer={castHit.collider?.gameObject.layer} point={castHit.point} normal={castHit.normal}");
					}
				}
				if (downHit && !IsOccludedByNonWater(worldPos, castHit.point))
				{
					if (aboveDryLandHard)
					{
						if (diagnosticLogging)
						{
							L.DebugMsg("[WaterRegionHelper] Raycast↓ 命中水体但高于水面硬阈值，忽略。");
						}
					}
					else
					{
						lastExplosionWasWater = true;
						return true;
					}
				}

				// 也尝试向上检测（以防爆炸点在浅水下方）
				bool upHit = Physics.Raycast(castOriginUp, Vector3.up, out castHit, castMaxDistance, waterMask, QueryTriggerInteraction.Collide);
				if (diagnosticLogging)
				{
					L.DebugMsg($"[WaterRegionHelper] Raycast↑ hit={upHit} dist={(upHit ? castHit.distance.ToString("F2") : "-")} depth={castMaxDistance:F2}");
					if (upHit)
					{
						L.DebugMsg($"[WaterRegionHelper]  - 命中对象: '{castHit.collider?.name}' layer={castHit.collider?.gameObject.layer} point={castHit.point} normal={castHit.normal}");
					}
				}
				if (upHit && !IsOccludedByNonWater(worldPos, castHit.point))
				{
					if (aboveDryLandHard)
					{
						if (diagnosticLogging)
						{
							L.DebugMsg("[WaterRegionHelper] Raycast↑ 命中水体但高于水面硬阈值，忽略。");
						}
					}
					else
					{
						lastExplosionWasWater = true;
						return true;
					}
				}
			}

			if (diagnosticLogging)
			{
				// 额外：扩展搜索周围若干米内的水体 Collider（仅诊断，不改变结果）
				const float probeRadius = 4f;
				var probeBuffer = _overlapBuffer ?? (_overlapBuffer = new Collider[32]);
				int probeCount = Physics.OverlapSphereNonAlloc(worldPos, probeRadius, probeBuffer, waterMask, QueryTriggerInteraction.Collide);
				L.DebugMsg($"[WaterRegionHelper] 未命中附近水体：pos={worldPos}, mask={waterMask}, boundsCount={waterBounds.Count}, colliders={waterColliders.Count}, probe(r={probeRadius})={probeCount}");
				for (int i = 0; i < probeCount; i++)
				{
					var c = probeBuffer[i];
					if (c == null) continue;
					Vector3 cp;
					float dist;
					try
					{
						cp = c.ClosestPoint(worldPos);
						dist = Vector3.Distance(worldPos, cp);
					}
					catch
					{
						var b = c.bounds;
						cp = new Vector3(
							Mathf.Clamp(worldPos.x, b.min.x, b.max.x),
							Mathf.Clamp(worldPos.y, b.min.y, b.max.y),
							Mathf.Clamp(worldPos.z, b.min.z, b.max.z)
						);
						dist = Vector3.Distance(worldPos, cp);
						if (diagnosticLogging)
						{
							L.DebugMsg($"[WaterRegionHelper]  - ClosestPoint 不可用，使用 Bounds 近似。");
						}
					}
					L.DebugMsg($"[WaterRegionHelper]  - 探测到临近Collider: '{c.name}' layer={c.gameObject.layer} dist={dist:F2} closest={cp} boundsCenter={c.bounds.center}");
				}
			}
			return false;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			// 限制参数范围，避免极端值
			raycastDepth = Mathf.Clamp(raycastDepth, 0.5f, 50f);
			sphereCastRadius = Mathf.Clamp(sphereCastRadius, 0f, 5f);
			nearCheckRadius = Mathf.Clamp(nearCheckRadius, 0.05f, 3f);
			verticalHalfExtent = Mathf.Clamp(verticalHalfExtent, 0.05f, 5f);
		}
#endif

		#region Debug Gizmos
		private void OnDrawGizmosSelected()
		{
			if (!drawGizmos) return;
#if UNITY_EDITOR
			// 绘制缓存的水体 Bounds 概览（简化为顶视圆盘）
			if (waterBounds != null)
			{
				UnityEditor.Handles.color = boundsColor;
				for (int i = 0; i < waterBounds.Count; i++)
				{
					var b = waterBounds[i];
					Vector3 center = b.center;
					float radius = Mathf.Max(b.extents.x, b.extents.z);
					Vector3 circleCenter = new Vector3(center.x, center.y, center.z);
					UnityEditor.Handles.DrawSolidDisc(circleCenter, Vector3.up, radius);
					UnityEditor.Handles.color = boundsWireColor;
					UnityEditor.Handles.DrawWireDisc(circleCenter, Vector3.up, radius);
				}
			}

			// 最近爆炸点
			if (lastExplosionPos != Vector3.zero)
			{
				Gizmos.color = lastExplosionWasWater ? lastExplosionColor : Color.red;
				Gizmos.DrawSphere(lastExplosionPos, 0.25f);
			}
#endif
		}
		#endregion

		// 外部可读信息（只读）
		public IReadOnlyList<Collider> GetCachedWaterColliders() => waterColliders.AsReadOnly();
		public IReadOnlyList<Bounds> GetCachedWaterBounds() => waterBounds.AsReadOnly();

		// 内部缓存
		private Collider[] _overlapBuffer;

		private bool TryGetWaterSurfaceY(Vector3 worldPos, out float waterY, out RaycastHit waterHit)
		{
			waterY = 0f;
			Vector3 top = worldPos + Vector3.up * 12f;
			if (Physics.Raycast(top, Vector3.down, out waterHit, 24f, waterMask, QueryTriggerInteraction.Collide))
			{
				waterY = waterHit.point.y;
				return true;
			}
			return false;
		}

		private bool IsWaterByVerticalColumn(Vector3 worldPos, out RaycastHit waterHit)
		{
			waterHit = default;
			const float verticalScanHeight = 12f;
			const float surfaceAllowance = 0.35f; // 允许在水面上方少量误差

			Vector3 top = worldPos + Vector3.up * verticalScanHeight;
			Vector3 bottom = worldPos - Vector3.up * verticalScanHeight;

			// 自上而下找到水面
			bool hasWater = Physics.Raycast(top, Vector3.down, out waterHit, verticalScanHeight * 2f, waterMask, QueryTriggerInteraction.Collide);
			if (!hasWater)
			{
				if (diagnosticLogging)
				{
					L.DebugMsg("[WaterRegionHelper] VerticalColumn 未找到水面。");
				}
				return false;
			}

			float waterY = waterHit.point.y;

			// 若爆炸点远高于水面，则认为不是水中爆炸
			if (worldPos.y > waterY + surfaceAllowance)
			{
				if (diagnosticLogging)
				{
					L.DebugMsg($"[WaterRegionHelper] VerticalColumn 水面高度={waterY:F2}，爆炸点高于水面 {worldPos.y - waterY:F2}m -> 非水体。");
				}
				return false;
			}

			// 检测爆炸点至水面间是否有非水体遮挡（向上或向下取决于相对高度）
			Vector3 target = new Vector3(worldPos.x, waterY, worldPos.z);
			if (IsOccludedByNonWater(worldPos, target))
			{
				if (diagnosticLogging)
				{
					L.DebugMsg("[WaterRegionHelper] VerticalColumn 爆炸点与水面之间被非水体遮挡 -> 非水体。");
				}
				return false;
			}

			return true;
		}

		private bool IsWaterCollider(Collider c)
		{
			if (c == null) return false;
			int layerBit = 1 << c.gameObject.layer;
			return (waterMask & layerBit) != 0;
		}

		private bool IsOccludedByNonWater(Vector3 from, Vector3 to)
		{
			Vector3 dir = to - from;
			float dist = dir.magnitude;
			if (dist <= 0.001f) return false;
			dir /= dist;

			// 使用预计算的非水体遮挡掩码
			bool blocked = Physics.Raycast(from, dir, dist, solidMaskCache, QueryTriggerInteraction.Collide);
			return blocked;
		}
	}
}

