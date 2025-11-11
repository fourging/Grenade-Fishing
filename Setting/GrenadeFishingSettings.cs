using System;
using UnityEngine;
using static ModSettingAPI;
using GrenadeFishing.Utils;

namespace GrenadeFishing.Setting
{
    /// <summary>
    /// 炸鱼模组设置管理类
    /// 负责管理所有模组设置项的保存、加载和UI初始化
    /// </summary>
    public static class GrenadeFishingSettings
    {
        // 设置项键名常量
        private const string KEY_ENABLE_SPLASH = "GF_EnableSplash";
        private const string KEY_PICKUP_KEY = "GF_PickupKey";
        private const string KEY_PICKUP_RADIUS = "GF_PickupRadius";
        
        // 设置项属性
        public static bool EnableExplosionSplash { get; private set; } = true;
        public static KeyCode PickupKey { get; private set; } = KeyCode.RightShift;
        public static float PickupRadius { get; private set; } = 15f;
        
        // 设置变更事件
        public static event Action<bool> OnEnableSplashChanged;
        public static event Action<KeyCode> OnPickupKeyChanged;
        public static event Action<float> OnPickupRadiusChanged;
        
        /// <summary>
        /// 初始化设置，从保存的配置中加载值
        /// </summary>
        public static void Init()
        {
            var logger = GrenadeFishing.Utils.Log.GetLogger();
            logger.Info("[GrenadeFishingSettings] 开始初始化设置...");
            
            try
            {
                bool hasConfig = ModSettingAPI.HasConfig();
                logger.Info($"[GrenadeFishingSettings] ModSettingAPI.HasConfig 结果: {hasConfig}");
                
                if (hasConfig)
                {
                    // 从保存的配置中加载设置
                    EnableExplosionSplash = ModSettingAPI.GetSavedValue(KEY_ENABLE_SPLASH, out bool splash) ? splash : true;
                    PickupKey = ModSettingAPI.GetSavedValue(KEY_PICKUP_KEY, out KeyCode key) ? key : KeyCode.RightShift;
                    PickupRadius = ModSettingAPI.GetSavedValue(KEY_PICKUP_RADIUS, out float radius) ? radius : 15f;
                    logger.Info($"[GrenadeFishingSettings] 从配置加载设置: EnableSplash={EnableExplosionSplash}, PickupKey={PickupKey}, PickupRadius={PickupRadius}");
                }
                else
                {
                    // 设置默认值
                    EnableExplosionSplash = true;
                    PickupKey = KeyCode.RightShift;
                    PickupRadius = 15f;
                    logger.Info("[GrenadeFishingSettings] 使用默认设置值");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"[GrenadeFishingSettings] 初始化设置时出错: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 初始化设置UI
        /// </summary>
        /// <param name="modInfo">模组信息</param>
        public static void InitSettingsUI(Duckov.Modding.ModInfo modInfo)
        {
            var logger = GrenadeFishing.Utils.Log.GetLogger();
            logger.Info("[GrenadeFishingSettings] 开始初始化设置UI...");
            
            try
            {
                logger.Info($"[GrenadeFishingSettings] 模组信息: name={modInfo.name}, displayName={modInfo.displayName}");
                
                // 添加爆炸飞溅效果开关
                bool toggleResult = ModSettingAPI.AddToggle(
                    KEY_ENABLE_SPLASH,
                    LocalizationHelper.Get("Setting_EnableSplash"),
                    EnableExplosionSplash,
                    val => SetEnableExplosionSplash(val)
                );
                logger.Info($"[GrenadeFishingSettings] 添加Toggle结果: {toggleResult}");
                
                // 添加手动拾取按键绑定
                bool keybindingResult = ModSettingAPI.AddKeybinding(
                    KEY_PICKUP_KEY,
                    LocalizationHelper.Get("Setting_PickupKey"),
                    PickupKey,
                    KeyCode.RightShift,
                    val => SetPickupKey(val)
                );
                logger.Info($"[GrenadeFishingSettings] 添加Keybinding结果: {keybindingResult}");
                
                // 添加手动拾取范围滑块
                bool sliderResult = ModSettingAPI.AddSlider(
                    KEY_PICKUP_RADIUS,
                    LocalizationHelper.Get("Setting_PickupRadius"),
                    PickupRadius,
                    new Vector2(9f, 18.0f),
                    val => SetPickupRadius(val),
                    2
                );
                logger.Info($"[GrenadeFishingSettings] 添加Slider结果: {sliderResult}");
                
                logger.Info("[GrenadeFishingSettings] 设置UI初始化完成");
            }
            catch (Exception ex)
            {
                logger.Error($"[GrenadeFishingSettings] 初始化设置UI失败：{ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 设置爆炸飞溅效果开关
        /// </summary>
        public static void SetEnableExplosionSplash(bool value)
        {
            EnableExplosionSplash = value;
            OnEnableSplashChanged?.Invoke(value);
        }
        
        /// <summary>
        /// 设置手动拾取按键
        /// </summary>
        public static void SetPickupKey(KeyCode value)
        {
            PickupKey = value;
            OnPickupKeyChanged?.Invoke(value);
        }
        
        /// <summary>
        /// 设置手动拾取范围
        /// </summary>
        public static void SetPickupRadius(float value)
        {
            PickupRadius = Mathf.Clamp(value, 9f, 18f);
            OnPickupRadiusChanged?.Invoke(PickupRadius);
        }
        
        /// <summary>
        /// 清理所有事件订阅
        /// </summary>
        public static void Clear()
        {
            OnEnableSplashChanged = null;
            OnPickupKeyChanged = null;
            OnPickupRadiusChanged = null;
        }
    }
}