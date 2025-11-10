using System;
using UnityEngine;
using Duckov.Modding;
using Duckov.UI;
using GrenadeFishing.Utils;

namespace GrenadeFishing
{
    /// <summary>
    /// 炸鱼测试模组 - 自动检测手雷爆炸落点并打印是否为允许炸鱼（水体）的位置
    /// </summary>
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private GrenadeExplosionTracker _tracker;
        private WaterRegionHelper _waterHelper;
        private bool _subscribed;

        void Start()
        {
            Debug.Log("[炸鱼测试] Mod已加载。自动检测手雷爆炸并打印是否为水体炸鱼点。");

            // 确保场景中存在水体判定与爆炸跟踪组件
            EnsureHelpers();

            // 订阅爆炸事件
            if (!_subscribed)
            {
                GrenadeExplosionTracker.OnAnyExplosion += HandleAnyExplosion;
                GrenadeExplosionTracker.OnWaterExplosion += HandleWaterExplosion;
                _subscribed = true;
            }
        }

        void Update()
        {
            // 无需按键，自动检测
        }

        private void EnsureHelpers()
        {
            // 查找已存在的 Helper
            _waterHelper = FindObjectOfType<WaterRegionHelper>();
            _tracker = FindObjectOfType<GrenadeExplosionTracker>();

            // 若不存在则创建一个持久对象承载它们
            if (_waterHelper == null || _tracker == null)
            {
                var host = new GameObject("GrenadeFishingRuntime");
                DontDestroyOnLoad(host);

                if (_waterHelper == null)
                {
                    _waterHelper = host.AddComponent<WaterRegionHelper>();
                }

                if (_tracker == null)
                {
                    _tracker = host.AddComponent<GrenadeExplosionTracker>();
                }
            }

            // 启用自动扫描手雷并立即进行一次扫描
            if (_tracker != null)
            {
                _tracker.enableAutoScanGrenades = true;
				// 保持较低频率的周期扫描（避免0.5s高频扫描）
				_tracker.scanInterval = Mathf.Max(2.0f, _tracker.scanInterval);
                _tracker.grenadeComponentNameHint = string.IsNullOrEmpty(_tracker.grenadeComponentNameHint) ? "Grenade" : _tracker.grenadeComponentNameHint;
                _tracker.explodeUnityEventMemberName = string.IsNullOrEmpty(_tracker.explodeUnityEventMemberName) ? "onExplodeEvent" : _tracker.explodeUnityEventMemberName;
                _tracker.ScanAndSubscribe();
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

                Debug.Log($"[炸鱼测试] 水体检测配置：nearR={_waterHelper.nearCheckRadius:F2}, halfH={_waterHelper.verticalHalfExtent:F2}, sphereR={_waterHelper.sphereCastRadius:F2}, rayDepth={_waterHelper.raycastDepth:F2}, colliders={_waterHelper.GetCachedWaterColliders().Count}, bounds={_waterHelper.GetCachedWaterBounds().Count}");
            }
        }

        private void HandleAnyExplosion(Vector3 worldPos, bool wasWater)
        {
            Debug.Log($"[炸鱼测试] 手雷爆炸位置: X={worldPos.x:F2}, Y={worldPos.y:F2}, Z={worldPos.z:F2} | 允许炸鱼: {(wasWater ? "是" : "否")}");
        }

        private void HandleWaterExplosion(Vector3 worldPos)
        {
            Debug.Log($"[炸鱼测试] 检测到水体爆炸点（可出鱼）: X={worldPos.x:F2}, Y={worldPos.y:F2}, Z={worldPos.z:F2}");
        }

        private void OnDestroy()
        {
            if (_subscribed)
            {
                GrenadeExplosionTracker.OnAnyExplosion -= HandleAnyExplosion;
                GrenadeExplosionTracker.OnWaterExplosion -= HandleWaterExplosion;
                _subscribed = false;
            }
        }

        /// <summary>
        /// GUI显示状态信息
        /// </summary>
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label("炸鱼测试模组", GUI.skin.box);
            GUILayout.Space(10);
            GUILayout.Label("自动检测手雷爆炸落点并打印是否为水体");
            GUILayout.EndArea();
        }
    }
}