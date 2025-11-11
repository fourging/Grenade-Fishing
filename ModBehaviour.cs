using System;
using UnityEngine;
using GrenadeFishing.Utils;
using ItemStatsSystem;
using HarmonyLib;

namespace GrenadeFishing
{
    /// <summary>
    /// 炸鱼测试模组 - 自动检测手雷爆炸落点并打印是否为允许炸鱼（水体）的位置
    /// </summary>
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        // 日志模块
        private static readonly GrenadeFishing.Utils.Logger L = GrenadeFishing.Utils.Log.GetLogger();
        // 在类初始化时，由你定义的局部布尔变量控制该文件日志：
        private static bool LocalLogs = true; // 你可以在别处修改这个变量
        static ModBehaviour()
        {
            L.SetEnabled(LocalLogs); // 一次设置即可
        }
        
        private GrenadeExplosionTracker? _tracker;
        private WaterRegionHelper? _waterHelper;
        private bool _subscribed;
  private static bool _harmonyPatched;
  private static Harmony? _harmonyInstance;
  private FishLootService? _fishLoot;
  private bool _settingsInitialized;
  private KeyCode _pickupKey = KeyCode.RightShift;
  private float _pickupRadius = 15f;
  private GameObject? _runtimeHost;
  private bool _isEnabled;

		/// <summary>
		/// 模组启用时调用 - 初始化所有模组功能
		/// </summary>
		void OnEnable()
		{
			if (_isEnabled)
			{
				L.Warn("[炸鱼测试] 模组已启用，跳过重复初始化。");
				return;
			}

			try
			{
				L.Info("[炸鱼测试] Mod已启用。自动检测手雷爆炸并打印是否为水体炸鱼点。");

				// 初始化 Harmony 补丁（全局爆炸入口 Hook）
				if (!_harmonyPatched)
				{
					try
					{
						_harmonyInstance = new Harmony("com.duckovmods.grenadefishing");
						_harmonyInstance.PatchAll();
						_harmonyPatched = true;
						L.Info("[炸鱼测试] Harmony 补丁已应用（ExplosionManager.CreateExplosion -> NotifyExplosion）。");
					}
					catch (Exception ex)
					{
						L.Warn($"[炸鱼测试] Harmony 补丁应用失败: {ex.Message}", ex);
						_harmonyInstance = null;
					}
				}

				// 确保场景中存在水体判定与爆炸跟踪组件
				EnsureHelpers();

				// 订阅爆炸事件
				if (!_subscribed)
				{
					GrenadeExplosionTracker.OnAnyExplosion += HandleAnyExplosion;
					GrenadeExplosionTracker.OnWaterExplosion += HandleWaterExplosion;
					_subscribed = true;
				}

				// 初始化 ModSetting UI（若可用）
				TryInitSettingsUI();

				_isEnabled = true;
				L.Info("[炸鱼测试] 模组初始化完成。");
			}
			catch (Exception ex)
			{
				L.Error($"[炸鱼测试] OnEnable 初始化失败: {ex.Message}\n{ex.StackTrace}", ex);
				// 即使初始化失败，也尝试清理已创建的资源
				OnDisable();
			}
		}

		/// <summary>
		/// 模组禁用时调用 - 清理所有模组资源
		/// </summary>
		void OnDisable()
		{
			if (!_isEnabled)
			{
				return; // 未启用，无需清理
			}

			try
			{
				L.Info("[炸鱼测试] 开始禁用模组，清理资源...");

				// 1. 取消事件订阅（优先处理，防止事件触发时访问已销毁的对象）
				if (_subscribed)
				{
					try
					{
						GrenadeExplosionTracker.OnAnyExplosion -= HandleAnyExplosion;
						GrenadeExplosionTracker.OnWaterExplosion -= HandleWaterExplosion;
						_subscribed = false;
						L.Info("[炸鱼测试] 已取消事件订阅。");
					}
					catch (Exception ex)
					{
						L.Warn($"[炸鱼测试] 取消事件订阅时出错: {ex.Message}", ex);
					}
				}

				// 2. 清理 ModSetting UI
				if (_settingsInitialized)
				{
					try
					{
						if (ModSettingAPI.IsInit)
						{
							ModSettingAPI.RemoveMod();
							L.Info("[炸鱼测试] 已移除 ModSetting UI。");
						}
						_settingsInitialized = false;
					}
					catch (Exception ex)
					{
						L.Warn($"[炸鱼测试] 清理 ModSetting UI 时出错: {ex.Message}", ex);
					}
				}

				// 3. 清理 Harmony 补丁
				if (_harmonyPatched && _harmonyInstance != null)
				{
					try
					{
						_harmonyInstance.UnpatchAll("com.duckovmods.grenadefishing");
						_harmonyInstance = null;
						_harmonyPatched = false;
						L.Info("[炸鱼测试] 已移除 Harmony 补丁。");
					}
					catch (Exception ex)
					{
						L.Warn($"[炸鱼测试] 移除 Harmony 补丁时出错: {ex.Message}", ex);
					}
				}

				// 4. 清理组件引用（不销毁，因为可能被其他模组使用）
				_tracker = null;
				_waterHelper = null;
				_fishLoot = null;

				// 5. 销毁运行时 GameObject（如果由本模组创建）
				if (_runtimeHost != null)
				{
					try
					{
						Destroy(_runtimeHost);
						_runtimeHost = null;
						L.Info("[炸鱼测试] 已销毁运行时 GameObject。");
					}
					catch (Exception ex)
					{
						L.Warn($"[炸鱼测试] 销毁运行时 GameObject 时出错: {ex.Message}", ex);
					}
				}

				_isEnabled = false;
				L.Info("[炸鱼测试] 模组已禁用，资源清理完成。");
			}
			catch (Exception ex)
			{
				L.Error($"[炸鱼测试] OnDisable 清理过程中出错: {ex.Message}\n{ex.StackTrace}", ex);
			}
		}

        void Start()
        {
            // Start 方法保留用于向后兼容，但主要逻辑已移至 OnEnable
            // 如果 OnEnable 未被调用，则在此处初始化
            if (!_isEnabled)
            {
                OnEnable();
            }
        }

        void Update()
        {
            // 仅在模组启用时处理输入
			if (!_isEnabled) return;

            // 无需按键，自动检测
			if (_pickupKey != KeyCode.None && Input.GetKeyDown(_pickupKey))
			{
				ExecuteFishPickup();
			}
        }

        private void EnsureHelpers()
        {
            // 查找已存在的 Helper
            _waterHelper = FindObjectOfType<WaterRegionHelper>();
            _tracker = FindObjectOfType<GrenadeExplosionTracker>();

            // 若不存在则创建一个持久对象承载它们
            if (_waterHelper == null || _tracker == null)
            {
                if (_runtimeHost == null)
                {
                    _runtimeHost = new GameObject("GrenadeFishingRuntime");
                    DontDestroyOnLoad(_runtimeHost);
                }

                if (_waterHelper == null)
                {
                    _waterHelper = _runtimeHost.AddComponent<WaterRegionHelper>();
                }

                if (_tracker == null)
                {
                    _tracker = _runtimeHost.AddComponent<GrenadeExplosionTracker>();
                }
				if (_fishLoot == null)
				{
					_fishLoot = _runtimeHost.AddComponent<FishLootService>();
				}
            }
			else
			{
				// 若已存在，则确保有掉落服务
				_fishLoot = FindObjectOfType<FishLootService>();
				if (_fishLoot == null)
				{
					if (_runtimeHost == null)
					{
						_runtimeHost = new GameObject("GrenadeFishingRuntime");
						DontDestroyOnLoad(_runtimeHost);
					}
					_fishLoot = _runtimeHost.AddComponent<FishLootService>();
				}
			}

            // 启用自动扫描手雷并立即进行一次扫描
            if (_tracker != null)
            {
				// 改为事件驱动为主：默认关闭轮询，必要时再开启为兜底
                _tracker.explodeUnityEventMemberName = string.IsNullOrEmpty(_tracker.explodeUnityEventMemberName) ? "onExplodeEvent" : _tracker.explodeUnityEventMemberName;
				// 启动时直接尝试订阅已存在的 Grenade（直连 UnityEvent，无反射）
				_tracker.SubscribeExistingGrenadesDirect(8);
            }

            // 调试参数与日志增强
            if (_waterHelper != null)
            {
				// 关闭高频调试日志以降低运行时开销（需要定位误判时再临时开启）
				_waterHelper.diagnosticLogging = false;
                _waterHelper.nearCheckRadius = 0.4f;             // 收紧近邻半径，减少公路近水误判
                _waterHelper.verticalHalfExtent = 1.2f;          // 垂直半高（可按需要微调）
                _waterHelper.sphereCastRadius = Mathf.Max(0.4f, _waterHelper.sphereCastRadius);
                _waterHelper.raycastDepth = Mathf.Max(1.5f, _waterHelper.raycastDepth);

                L.Info($"[炸鱼测试] 水体检测配置：nearR={_waterHelper.nearCheckRadius:F2}, halfH={_waterHelper.verticalHalfExtent:F2}, sphereR={_waterHelper.sphereCastRadius:F2}, rayDepth={_waterHelper.raycastDepth:F2}, colliders={_waterHelper.GetCachedWaterColliders().Count}, bounds={_waterHelper.GetCachedWaterBounds().Count}");
            }

			// 初始化 ModSetting UI（若可用）
			TryInitSettingsUI();
        }

        private void HandleAnyExplosion(Vector3 worldPos, bool wasWater)
        {
            L.Info($"[炸鱼测试] 手雷爆炸位置: X={worldPos.x:F2}, Y={worldPos.y:F2}, Z={worldPos.z:F2} | 允许炸鱼: {(wasWater ? "是" : "否")}");
        }

        private void HandleWaterExplosion(Vector3 worldPos)
        {
            L.Info($"[炸鱼测试] 检测到水体爆炸点（可出鱼）: X={worldPos.x:F2}, Y={worldPos.y:F2}, Z={worldPos.z:F2}");
			// 步骤1 + 步骤3：生成待命鱼并在玩家附近生成真实掉落物（跳过第二步动画）
			if (_fishLoot != null)
			{
				var player = CharacterMainControl.Main;
				_fishLoot.HandleWaterExplosion(worldPos, player);
			}
        }

		protected override void OnAfterSetup()
		{
			// Mod 安装完成后尝试初始化设置 UI（兼容 ModSetting 先后加载顺序）
			TryInitSettingsUI();
		}

		private void TryInitSettingsUI()
		{
			if (_settingsInitialized) return;
			try
			{
				if (!ModSettingAPI.Init(info)) return;
				if (_fishLoot == null)
				{
					_fishLoot = FindObjectOfType<FishLootService>();
					if (_fishLoot == null)
					{
						if (_runtimeHost == null)
						{
							_runtimeHost = new GameObject("GrenadeFishingRuntime");
							DontDestroyOnLoad(_runtimeHost);
						}
						_fishLoot = _runtimeHost.AddComponent<FishLootService>();
					}
				}

				// 定义键
				const string kToggleSplash = "GF_EnableSplash";
				const string kUpMin = "GF_UpBiasMin";
				const string kUpMax = "GF_UpBiasMax";
				const string kForceMin = "GF_ForceMin";
				const string kForceMax = "GF_ForceMax";
				const string kAngle = "GF_RandomAngle";
				const string kGroup = "GF_SplashGroup";
				const string kPickupKey = "GF_PickupKey";
				const string kPickupRadius = "GF_PickupRadius";
				const string kPickupGroup = "GF_PickupGroup";

				// 添加 UI 控件
				ModSettingAPI.AddToggle(
					kToggleSplash,
					"开启爆炸飞溅效果",
					_fishLoot.enableExplosionSplash,
					val => { if (_fishLoot != null) _fishLoot.enableExplosionSplash = val; }
				);

				// 手动拾取相关设置
				ModSettingAPI.AddKeybinding(
					kPickupKey,
					"手动拾取鱼类按键",
					_pickupKey,
					KeyCode.RightShift,
					val => { _pickupKey = val; }
				);

				ModSettingAPI.AddSlider(
					kPickupRadius,
					"手动拾取范围半径",
					_pickupRadius,
					new Vector2(9f, 18.0f),
					val => { _pickupRadius = Mathf.Clamp(val, 0.1f, 3.0f); },
					2
				);

				ModSettingAPI.AddSlider(
					kUpMin,
					"飞溅向上分量最小值",
					_fishLoot.explosionUpwardBiasMin,
					new Vector2(0.1f, 0.95f),
					val =>
					{
						if (_fishLoot == null) return;
						_fishLoot.explosionUpwardBiasMin = Mathf.Clamp(val, 0.0f, _fishLoot.explosionUpwardBiasMax);
					},
					2
				);

				ModSettingAPI.AddSlider(
					kUpMax,
					"飞溅向上分量最大值",
					_fishLoot.explosionUpwardBiasMax,
					new Vector2(0.15f, 1.0f),
					val =>
					{
						if (_fishLoot == null) return;
						_fishLoot.explosionUpwardBiasMax = Mathf.Clamp(val, _fishLoot.explosionUpwardBiasMin, 1.0f);
					},
					2
				);

				ModSettingAPI.AddSlider(
					kForceMin,
					"飞溅力度最小值",
					_fishLoot.explosionForceRange.x,
					new Vector2(2f, 30f),
					val =>
					{
						if (_fishLoot == null) return;
						_fishLoot.explosionForceRange.x = Mathf.Min(val, _fishLoot.explosionForceRange.y - 0.1f);
					},
					1
				);

				ModSettingAPI.AddSlider(
					kForceMax,
					"飞溅力度最大值",
					_fishLoot.explosionForceRange.y,
					new Vector2(3f, 35f),
					val =>
					{
						if (_fishLoot == null) return;
						_fishLoot.explosionForceRange.y = Mathf.Max(val, _fishLoot.explosionForceRange.x + 0.1f);
					},
					1
				);

				ModSettingAPI.AddSlider(
					kAngle,
					"随机旋转角度",
					_fishLoot.explosionRandomAngle,
					new Vector2(0f, 360f),
					val => { if (_fishLoot != null) _fishLoot.explosionRandomAngle = val; },
					0
				);

				// 分组显示
				ModSettingAPI.AddGroup(
					kGroup,
					"爆炸飞溅参数",
					new System.Collections.Generic.List<string> { kToggleSplash, kUpMin, kUpMax, kForceMin, kForceMax, kAngle },
					0.7f,
					false,
					true
				);

				ModSettingAPI.AddGroup(
					kPickupGroup,
					"拾取设置",
					new System.Collections.Generic.List<string> { kPickupKey, kPickupRadius },
					0.6f,
					false,
					false
				);

				_settingsInitialized = true;
				L.Info("[GrenadeFishing] 设置面板初始化完成。");
			}
			catch (Exception ex)
			{
				L.Warn($"[GrenadeFishing] 初始化设置 UI 失败：{ex.Message}", ex);
			}
		}

		/// <summary>
		/// 执行一次基于球形范围的鱼类掉落物拾取（TypeID 1097 - 1126）
		/// </summary>
		private void ExecuteFishPickup()
		{
			var character = CharacterMainControl.Main;
			if (character == null) return;

			// 计算检测球
			Vector3 detectionCenter = character.transform.position + Vector3.up * 0.5f + character.CurrentAimDirection * 0.2f;
			float detectionRadius = Mathf.Max(0.05f, _pickupRadius);

			int interactableLayer = LayerMask.NameToLayer("Interactable");
			int layerMask = (interactableLayer >= 0) ? (1 << interactableLayer) : (-1);

			Collider[] detectedColliders = new Collider[8];
			int hitCount = Physics.OverlapSphereNonAlloc(detectionCenter, detectionRadius, detectedColliders, layerMask, QueryTriggerInteraction.Collide);

			// 找最近的可拾取物
			float closestDistance = 999f;
			InteractablePickup? closestPickup = null;

			for (int i = 0; i < hitCount; i++)
			{
				Collider collider = detectedColliders[i];
				if (collider == null) continue;

				var pickup = collider.GetComponent<InteractablePickup>();
				if (pickup != null)
				{
					float distance = Vector3.Distance(character.transform.position, pickup.transform.position);
					if (distance < closestDistance)
					{
						closestDistance = distance;
						closestPickup = pickup;
					}
				}
			}

			// 执行拾取（仅鱼类）
			if (closestPickup != null)
			{
				var itemAgent = closestPickup.ItemAgent;
				if (itemAgent != null)
				{
					var baseAgent = itemAgent as ItemAgent;
					if (baseAgent != null)
					{
						Item item = baseAgent.Item;
						if (item != null)
						{
							int typeId = item.TypeID;
							if (typeId >= 1097 && typeId <= 1126)
							{
								bool pickupSuccess = character.PickupItem(item);
								if (pickupSuccess)
								{
									string itemName = item.DisplayName;
									if (string.IsNullOrEmpty(itemName)) itemName = item.DisplayNameRaw;
									if (string.IsNullOrEmpty(itemName)) itemName = "物品";

									int stackCount = 1;
									try
									{
										int itemCount = item.StackCount;
										if (itemCount > 0) stackCount = itemCount;
									}
									catch { }

									string pickupMessage = (stackCount > 1) ? $"拾取：{itemName}×{stackCount}" : ("拾取：" + itemName);
									character.PopText(pickupMessage, -1f);
								}
							}
						}
					}
				}
			}
		}

        private void OnDestroy()
        {
            // OnDestroy 时确保清理所有资源
            OnDisable();
        }
    }
}