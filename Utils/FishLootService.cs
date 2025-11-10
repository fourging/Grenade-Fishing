using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ItemStatsSystem;

namespace GrenadeFishing.Utils
{
	/// <summary>
	/// 炸鱼掉落与待命鱼生成服务：
	/// - 步骤1：根据幸运值生成待命鱼数据（含异步图标预取，不在爆炸帧阻塞）
	/// - 步骤3：在玩家附近生成真实掉落物（带地面射线检测）
	/// 第二步动画暂不实现，仅保留图标数据以便后续使用。
	/// </summary>
	public class FishLootService : MonoBehaviour
	{
		[Header("爆炸飞溅开关")]
		[Tooltip("为 true 时：在爆炸点向四周（水平面以上）抛射生成鱼；为 false 时：在玩家附近小范围散落。")]
		public bool enableExplosionSplash = true;

		[Header("飞溅到岸辅助参数")]
		[Tooltip("起飞高度 = 玩家Y + 该偏移（仅飞溅模式使用）")]
		public float splashStartHeightOffsetFromPlayerY = 1.5f;
		[Tooltip("朝向玩家的权重（0=完全随机方向，1=完全朝向玩家）")]
		[Range(0f, 1f)]
		public float splashHomingWeight = 0.6f;
		[Tooltip("按玩家距离附加力度（每米增加的力度倍数）")]
		public float splashDistanceForcePerMeter = 0.6f;

		[Serializable]
		public class FishEntry
		{
			public string displayName = string.Empty;
			public int typeId;
			public int value;
		}

		[Serializable]
		public class PendingFishData
		{
			public string displayName = string.Empty;
			public int typeId;
			public int value;
			public Sprite? icon; // 可为空（异步加载后填充）
		}

		// 掉落位置相关
		[Header("掉落位置与物理")]
		public float minSpawnRadius = 2.0f;
		public float maxSpawnRadius = 4.0f;
		public float groundRaycastHeight = 5.0f;
		public float groundRaycastDownDistance = 15.0f;
		public float spawnHeightOffset = 0.20f; // 生成点略高于地面
		public LayerMask groundMask = ~0; // 默认对所有碰撞体进行检测

		[Header("玩家附近散落（关闭飞溅时使用）")]
		public float nearPlayerMinOffset = 0.6f;
		public float nearPlayerMaxOffset = 1.2f;

		[Header("爆炸飞溅参数（开启飞溅时使用）")]
		[Tooltip("水平+向上方向的最小上仰分量，避免向下抛射")]
		public float explosionUpwardBiasMin = 0.25f;
		[Tooltip("水平+向上方向的最大上仰分量")]
		public float explosionUpwardBiasMax = 0.85f;
		[Tooltip("抛射力度范围，数值越大飞得越远")]
		public Vector2 explosionForceRange = new Vector2(7.5f, 12.0f);
		[Tooltip("随机旋转角度范围")]
		public float explosionRandomAngle = 180f;

		// 图标缓存：typeId -> Sprite
		private readonly Dictionary<int, Sprite> _iconCache = new Dictionary<int, Sprite>();
		private readonly HashSet<int> _iconLoading = new HashSet<int>();

		void Awake() { }

		/// <summary>
		/// 供外部调用：在水体爆炸时执行步骤1（生成待命鱼并预取图标）与步骤3（玩家附近生成真实掉落物）。
		/// </summary>
		public void HandleWaterExplosion(Vector3 explosionWorldPosition, CharacterMainControl player)
		{
			var pending = GeneratePendingFish(player);
			// 异步预取图标（不阻塞当前帧）
			StartCoroutine(PrefetchIconsCoroutine(pending));
			// 生成真实掉落物（根据开关选择模式）
			if (enableExplosionSplash)
			{
				Log.Info($"开启爆炸飞溅：使用爆炸点抛射 {pending.Count} 条鱼");
				StartCoroutine(SpawnDropsExplosionSplashCoroutine(pending, explosionWorldPosition, player));
			}
			else
			{
				Log.Info($"关闭爆炸飞溅：在玩家附近散落 {pending.Count} 条鱼");
				StartCoroutine(SpawnDropsAroundPlayerCoroutine(pending, player));
			}
		}

		/// <summary>
		/// 步骤1：生成待命鱼缓存（根据幸运值决定数量区间，并按价值分档概率抽样鱼种）。
		/// </summary>
		public List<PendingFishData> GeneratePendingFish(CharacterMainControl player)
		{
			float luck = GetPlayerLuck01(player);
			var selected = FishGenerator.GenerateByLuck(luck);
			var result = selected.Select(sel => new PendingFishData
			{
				displayName = sel.displayName,
				typeId = sel.typeId,
				value = sel.value,
				icon = TryGetIconFromCache(sel.typeId)
			}).ToList();
			Log.DebugMsg($"待命鱼名称列表：{string.Join(", ", result.Select(f => f.displayName))}");
			Debug.Log($"[炸鱼测试] 待命鱼已生成：数量={result.Count}（幸运值={luck:F2}）。");
			return result;
		}

		/// <summary>
		/// 步骤3（模式A - 玩家附近）：在玩家附近随机位置生成真实掉落物（小范围散落）。
		/// </summary>
		private IEnumerator SpawnDropsAroundPlayerCoroutine(List<PendingFishData> pending, CharacterMainControl player)
		{
			if (pending == null || pending.Count == 0 || player == null)
			{
				yield break;
			}

			Transform center = ResolveMainPlayerCenter(player);
			Vector3 playerPos = center.position;
			Log.DebugMsg($"开始生成真实掉落：总计={pending.Count}，中心(玩家)={playerPos}（来源={center.name}）");

			for (int i = 0; i < pending.Count; i++)
			{
				var data = pending[i];
				if (data == null) continue;

				// 创建物品实例（同步，简洁可靠；如需完全异步，可改为 InstantiateAsync + await）
				Item? itemInstance = null;
				try
				{
					Log.DebugMsg($"尝试实例化物品：Name={data.displayName}, TypeID={data.typeId}, Value={data.value}");
					itemInstance = ItemAssetsCollection.InstantiateSync(data.typeId);
				}
				catch (Exception ex)
				{
					Log.Warn($"实例化物品失败（TypeID={data.typeId}, Name={data.displayName}）：{ex.Message}");
				}

				if (itemInstance != null)
				{
					Vector3 spawnPos = GetRandomNearPlayerPosition(playerPos);
					var outward = (spawnPos - playerPos);
					if (outward.sqrMagnitude < 0.01f) outward = Vector3.up;
					var dropDir = outward.normalized;

					Log.DebugMsg($"准备掉落：Name={data.displayName}, TypeID={data.typeId}, SpawnPos={spawnPos}, DropDir={dropDir}, Dist={outward.magnitude:F2}");
					itemInstance.Drop(spawnPos, true, dropDir, 35f);
					Log.Info($"已生成掉落：{data.displayName}({data.typeId}) @ {spawnPos}");
				}
				else
				{
					Log.Warn($"放弃掉落：实例化失败 Name={data.displayName}, TypeID={data.typeId}");
				}

				// 为避免同帧生成全部掉落造成卡顿，适当让渡一帧
				yield return null;
			}
		}

		/// <summary>
		/// 步骤3（模式B - 爆炸飞溅）：在爆炸点向四周（水平面以上）抛射生成真实掉落物。
		/// </summary>
		private IEnumerator SpawnDropsExplosionSplashCoroutine(List<PendingFishData> pending, Vector3 explosionPos, CharacterMainControl player)
		{
			if (pending == null || pending.Count == 0)
			{
				yield break;
			}

			// 起飞点：使用“玩家Y + 偏移”的高度，XZ 取爆炸点；若无玩家则回退为水面+1m 或 爆炸点+1m
			Vector3 spawnBase = GetSplashStartFromPlayerHeight(explosionPos, player, splashStartHeightOffsetFromPlayerY);
			Log.DebugMsg($"开始爆炸飞溅掉落：总计={pending.Count}，爆炸点={explosionPos}, 起飞基准={spawnBase}, 偏移={splashStartHeightOffsetFromPlayerY:F2}, Homing={splashHomingWeight:F2}, DistForce/m={splashDistanceForcePerMeter:F2}");

			for (int i = 0; i < pending.Count; i++)
			{
				var data = pending[i];
				if (data == null) continue;

				Item? itemInstance = null;
				try
				{
					Log.DebugMsg($"[飞溅] 尝试实例化物品：Name={data.displayName}, TypeID={data.typeId}, Value={data.value}");
					itemInstance = ItemAssetsCollection.InstantiateSync(data.typeId);
				}
				catch (Exception ex)
				{
					Log.Warn($"[飞溅] 实例化物品失败（TypeID={data.typeId}, Name={data.displayName}）：{ex.Message}");
				}

				if (itemInstance != null)
				{
					// 基础随机方向（水平 + 向上）
					Vector2 planar = UnityEngine.Random.insideUnitCircle.normalized;
					float up = UnityEngine.Random.Range(explosionUpwardBiasMin, explosionUpwardBiasMax);
					Vector3 randomDir = new Vector3(planar.x, up, planar.y).normalized;

					// 朝向玩家的方向（仅水平朝向，叠加同等上仰）
					Vector3 homingDir = randomDir;
					if (player != null)
					{
						Vector3 playerPos = SafeGetPlayerPosition(player);
						Vector3 toPlayer = playerPos - spawnBase;
						Vector2 toPlayerXZ = new Vector2(toPlayer.x, toPlayer.z);
						if (toPlayerXZ.sqrMagnitude > 0.0001f)
						{
							Vector2 homingPlanar = toPlayerXZ.normalized;
							homingDir = new Vector3(homingPlanar.x, up, homingPlanar.y).normalized;
						}
					}

					// 方向混合：随机 与 朝向玩家
					float w = Mathf.Clamp01(splashHomingWeight);
					Vector3 finalDir = (randomDir * (1f - w) + homingDir * w).normalized;

					// 力度 = 基础随机 + 距离加力（上限受最大力度限制）
					float baseForce = UnityEngine.Random.Range(explosionForceRange.x, explosionForceRange.y);
					float extra = 0f;
					if (player != null)
					{
						float dist = Vector3.Distance(spawnBase, SafeGetPlayerPosition(player));
						extra = Mathf.Max(0f, dist * Mathf.Max(0f, splashDistanceForcePerMeter));
					}
					float force = Mathf.Clamp(baseForce + extra, explosionForceRange.x, explosionForceRange.y);
					Vector3 dropDir = finalDir * force;

					itemInstance.Drop(spawnBase, true, dropDir, explosionRandomAngle);
					Log.Info($"[飞溅] 已生成掉落：{data.displayName}({data.typeId}) @ {spawnBase}, BaseF={baseForce:F2}, ExtraF={extra:F2}, Force={force:F2}, Up={up:F2}, w={w:F2}");
				}
				else
				{
					Log.Warn($"[飞溅] 放弃掉落：实例化失败 Name={data.displayName}, TypeID={data.typeId}");
				}

				yield return null;
			}
		}

		private Vector3 GetSplashStartFromWaterSurface(Vector3 explosionPos, float heightAbove)
		{
			// 基于“Water” Layer 求水面高度
			int waterLayer = LayerMask.NameToLayer("Water");
			if (waterLayer >= 0)
			{
				int mask = 1 << waterLayer;
				Vector3 origin = explosionPos + Vector3.up * 20f;
				if (Physics.Raycast(origin, Vector3.down, out var hit, 60f, mask, QueryTriggerInteraction.Collide))
				{
					float y = hit.point.y + Mathf.Max(0f, heightAbove);
					return new Vector3(explosionPos.x, y, explosionPos.z);
				}
			}
			// 兜底：使用爆炸点 + 指定高度
			return explosionPos + Vector3.up * Mathf.Max(0f, heightAbove);
		}

		private Vector3 GetSplashStartFromPlayerHeight(Vector3 explosionPos, CharacterMainControl player, float heightOffset)
		{
			if (player != null)
			{
				Vector3 playerPos = SafeGetPlayerPosition(player);
				float startY = playerPos.y + Mathf.Max(0f, heightOffset);
				return new Vector3(explosionPos.x, startY, explosionPos.z);
			}
			// 无玩家时回退到水面+1m 或 爆炸点+1m
			return GetSplashStartFromWaterSurface(explosionPos, 1.0f);
		}

		private IEnumerator PrefetchIconsCoroutine(List<PendingFishData> pending)
		{
			if (pending == null || pending.Count == 0) yield break;
			for (int i = 0; i < pending.Count; i++)
			{
				var typeId = pending[i].typeId;
				if (_iconCache.ContainsKey(typeId) || _iconLoading.Contains(typeId)) continue;
				_iconLoading.Add(typeId);

				// 逐个进行（避免一次性加载导致资源尖峰）
				yield return StartCoroutine(LoadIconCoroutine(typeId, sprite =>
				{
					if (sprite != null)
					{
						_iconCache[typeId] = sprite;
					}
				}));

				_iconLoading.Remove(typeId);
				// 将已加载的图标填回待命数据
				if (pending[i].icon == null && _iconCache.TryGetValue(typeId, out var s))
				{
					pending[i].icon = s;
				}
			}
		}

		private IEnumerator LoadIconCoroutine(int typeId, Action<Sprite?> onDone)
		{
			// 为了确保线程安全，这里使用同步实例化，但逐个分帧执行，避免爆炸帧内加载新资源造成卡顿
			yield return null; // 将加载推迟到下一帧，避开爆炸当帧

			Sprite? icon = null;
			Item? syncItem = null;
			try
			{
				syncItem = ItemAssetsCollection.InstantiateSync(typeId);
				icon = syncItem != null ? syncItem.Icon : null;
			}
			catch
			{
				icon = null;
			}
			finally
			{
				if (syncItem != null && syncItem.gameObject != null)
				{
					Destroy(syncItem.gameObject);
				}
			}

			onDone?.Invoke(icon);
		}

		private Sprite? TryGetIconFromCache(int typeId)
		{
			return _iconCache.TryGetValue(typeId, out var s) ? s : null;
		}

		private float GetPlayerLuck01(CharacterMainControl player)
		{
			// 按需求改为 0~1 随机数（目前游戏接口返回恒为1）
			float luck = UnityEngine.Random.value;
			Log.DebugMsg($"使用随机幸运值：{luck:F2}");
			return luck;
		}

		private int GetFishCountByLuck(float luck01)
		{
			// 0.00 ~ 0.25：2-3
			// 0.25 ~ 0.50：3-4
			// 0.50 ~ 0.75：4-5
			// 0.75 ~ 1.00：5
			if (luck01 < 0.25f) return UnityEngine.Random.Range(2, 4);  // [2,3]
			if (luck01 < 0.50f) return UnityEngine.Random.Range(3, 5);  // [3,4]
			if (luck01 < 0.75f) return UnityEngine.Random.Range(4, 6);  // [4,5]
			return 5;
		}

		private Vector3 GetRandomNearPlayerPosition(Vector3 playerPos)
		{
			// 随机水平位移（移除地面检测，直接以玩家高度为基准），使用小范围散落
			Vector2 delta = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(nearPlayerMinOffset, nearPlayerMaxOffset);
			Vector3 spawn = new Vector3(playerPos.x + delta.x, playerPos.y + spawnHeightOffset, playerPos.z + delta.y);
			return spawn;
		}

		private Transform ResolveMainPlayerCenter(CharacterMainControl player)
		{
			// 优先使用带有 "Player" 标签的对象
			try
			{
				var tagged = GameObject.FindGameObjectWithTag("Player");
				if (tagged != null)
				{
					return tagged.transform;
				}
			}
			catch { /* ignore tag absent */ }

			// 回退：使用传入的 player
			if (player != null) return player.transform;

			// 最后兜底：使用场景中第一个 CharacterMainControl
			var any = FindObjectOfType<CharacterMainControl>();
			return any != null ? any.transform : this.transform;
		}
		private static Vector3 SafeGetPlayerPosition(CharacterMainControl player)
		{
			try
			{
				return player != null ? player.transform.position : Vector3.zero;
			}
			catch
			{
				return Vector3.zero;
			}
		}
	}
}

