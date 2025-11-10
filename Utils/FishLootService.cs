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

		// 价值分档（单位：货币）
		private const int Tier1Max = 1000;   // 0 ~ 1000
		private const int Tier2Max = 3000;   // 1000 ~ 3000
		private const int Tier3Max = 6000;   // 3000 ~ 6000
		// >6000 为 Tier4

		// 各档出现概率
		private const float ProbTier1 = 0.55f; // 0-1000：55%
		private const float ProbTier2 = 0.30f; // 1000-3000：30%
		private const float ProbTier3 = 0.10f; // 3000-6000：10%
		private const float ProbTier4 = 0.05f; // >6000：5%

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

		// 鱼数据表（来自 Docs/现有鱼.txt）
		private static readonly List<FishEntry> _allFish = new List<FishEntry>
		{
			new FishEntry{ displayName = "大头金鱼", typeId = 1123, value = 492 },
			new FishEntry{ displayName = "棕沙丁鱼", typeId = 1106, value = 497 },
			new FishEntry{ displayName = "蓝鲭鱼", typeId = 1100, value = 506 },
			new FishEntry{ displayName = "红金鱼", typeId = 1119, value = 515 },
			new FishEntry{ displayName = "粉金鱼", typeId = 1114, value = 525 },
			new FishEntry{ displayName = "红鳍鲷鱼", typeId = 1117, value = 785 },
			new FishEntry{ displayName = "蓝吊鱼", typeId = 1103, value = 805 },
			new FishEntry{ displayName = "紫雀鲷鱼", typeId = 1115, value = 814 },
			new FishEntry{ displayName = "绿刺豚", typeId = 1126, value = 970 },
			new FishEntry{ displayName = "红九间鱼", typeId = 1118, value = 983 },
			new FishEntry{ displayName = "蓝雀鲷鱼", typeId = 1098, value = 996 },
			new FishEntry{ displayName = "白扁鱼", typeId = 1124, value = 1006 },
			new FishEntry{ displayName = "绿鲷鱼", typeId = 1097, value = 1066 },
			new FishEntry{ displayName = "白燕鱼", typeId = 1122, value = 1108 },
			new FishEntry{ displayName = "棕梭鱼", typeId = 1105, value = 1128 },
			new FishEntry{ displayName = "绿背鳙鱼", typeId = 1109, value = 1578 },
			new FishEntry{ displayName = "绿胖头鱼", typeId = 1108, value = 1640 },
			new FishEntry{ displayName = "青南乳鱼", typeId = 1099, value = 1715 },
			new FishEntry{ displayName = "大眼红鱼", typeId = 1120, value = 1835 },
			new FishEntry{ displayName = "绿黄鲀", typeId = 1110, value = 2057 },
			new FishEntry{ displayName = "棕白石鲈", typeId = 1104, value = 2063 },
			new FishEntry{ displayName = "橙金鳞鱼", typeId = 1113, value = 2979 },
			new FishEntry{ displayName = "蓝枪鱼", typeId = 1101, value = 3762 },
			new FishEntry{ displayName = "粉鳍火焰鱼", typeId = 1116, value = 3798 },
			new FishEntry{ displayName = "黄绿鲷", typeId = 1125, value = 3807 },
			new FishEntry{ displayName = "蓝旗鱼", typeId = 1102, value = 4107 },
			new FishEntry{ displayName = "橙青鳍鱼", typeId = 1112, value = 4113 },
			new FishEntry{ displayName = "蓝猫鲨鱼", typeId = 1111, value = 6084 },
			new FishEntry{ displayName = "红斑鱼", typeId = 1121, value = 6534 },
			new FishEntry{ displayName = "黄金鱼", typeId = 1107, value = 12300 },
		};

		private List<FishEntry> _tier1 = new List<FishEntry>(); // <= 1000
		private List<FishEntry> _tier2 = new List<FishEntry>(); // (1000, 3000]
		private List<FishEntry> _tier3 = new List<FishEntry>(); // (3000, 6000]
		private List<FishEntry> _tier4 = new List<FishEntry>(); // > 6000

		void Awake()
		{
			BuildFishTiers();
		}

		private void BuildFishTiers()
		{
			_tier1 = _allFish.Where(f => f.value <= Tier1Max).ToList();
			_tier2 = _allFish.Where(f => f.value > Tier1Max && f.value <= Tier2Max).ToList();
			_tier3 = _allFish.Where(f => f.value > Tier2Max && f.value <= Tier3Max).ToList();
			_tier4 = _allFish.Where(f => f.value > Tier3Max).ToList();
		}

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
				StartCoroutine(SpawnDropsExplosionSplashCoroutine(pending, explosionWorldPosition));
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
			int count = GetFishCountByLuck(luck);

			var result = new List<PendingFishData>(count);
			for (int i = 0; i < count; i++)
			{
				var picked = PickOneFishByValueProbability();
				if (picked == null) continue;
				result.Add(new PendingFishData
				{
					displayName = picked.displayName,
					typeId = picked.typeId,
					value = picked.value,
					icon = TryGetIconFromCache(picked.typeId)
				});
			}
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

			Vector3 playerPos = SafeGetPlayerPosition(player);
			Log.DebugMsg($"开始生成真实掉落：总计={pending.Count}，玩家位置={playerPos}");

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
		private IEnumerator SpawnDropsExplosionSplashCoroutine(List<PendingFishData> pending, Vector3 explosionPos)
		{
			if (pending == null || pending.Count == 0)
			{
				yield break;
			}

			// 起飞点：爆炸位置对应水面 +1m（若无法找到水面，则使用爆炸点 +1m）
			Vector3 spawnBase = GetSplashStartFromWaterSurface(explosionPos, 1.0f);
			Log.DebugMsg($"开始爆炸飞溅掉落：总计={pending.Count}，爆炸点={explosionPos}, 基准={spawnBase}");

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
					// 生成水平+向上方向
					Vector2 planar = UnityEngine.Random.insideUnitCircle.normalized;
					float up = UnityEngine.Random.Range(explosionUpwardBiasMin, explosionUpwardBiasMax);
					Vector3 dir = new Vector3(planar.x, up, planar.y).normalized;

					// 控制力度（不要飞太远）
					float force = UnityEngine.Random.Range(explosionForceRange.x, explosionForceRange.y);
					Vector3 dropDir = dir * force;

					itemInstance.Drop(spawnBase, true, dropDir, explosionRandomAngle);
					Log.Info($"[飞溅] 已生成掉落：{data.displayName}({data.typeId}) @ {spawnBase}, Dir={dir}, Force={force:F2}");
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

		private FishEntry? PickOneFishByValueProbability()
		{
			float r = UnityEngine.Random.value;
			List<FishEntry> source;

			if (r < ProbTier1)
			{
				source = _tier1;
			}
			else if (r < ProbTier1 + ProbTier2)
			{
				source = _tier2;
			}
			else if (r < ProbTier1 + ProbTier2 + ProbTier3)
			{
				source = _tier3;
			}
			else
			{
				source = _tier4;
			}

			if (source == null || source.Count == 0)
			{
				// 若某档为空，回退至全库
				source = _allFish;
			}

			if (source.Count == 0) return null;
			int idx = UnityEngine.Random.Range(0, source.Count);
			return source[idx];
		}

		private Vector3 GetRandomNearPlayerPosition(Vector3 playerPos)
		{
			// 随机水平位移（移除地面检测，直接以玩家高度为基准），使用小范围散落
			Vector2 delta = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(nearPlayerMinOffset, nearPlayerMaxOffset);
			Vector3 spawn = new Vector3(playerPos.x + delta.x, playerPos.y + spawnHeightOffset, playerPos.z + delta.y);
			return spawn;
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

