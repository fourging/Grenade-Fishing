using System;
using System.Collections.Generic;
using UnityEngine;
using SodaCraft.Localizations;

namespace GrenadeFishing.Utils
{
    /// <summary>
    /// 本地化辅助类，提供多语言支持功能
    /// </summary>
    public static class LocalizationHelper
    {
        // 存储所有语言的翻译数据
        private static Dictionary<SystemLanguage, Dictionary<string, string>> _localizationData;
        
        // 语言变更事件
        public static event Action<SystemLanguage> OnLanguageChanged;
        
        // 模组前缀，避免与游戏原有键冲突
        private const string MOD_PREFIX = "GrenadeFishing_";
        
        /// <summary>
        /// 初始化本地化系统
        /// </summary>
        public static void Initialize()
        {
            var logger = Log.GetLogger();
            logger.Info("[LocalizationHelper] 初始化本地化系统...");
            
            try
            {
                // 初始化翻译数据
                LoadTranslations();
                
                // 应用当前语言的翻译
                ApplyTranslations(LocalizationManager.CurrentLanguage);
                
                // 监听语言切换事件
                LocalizationManager.OnSetLanguage += OnLanguageChangedHandler;
                
                logger.Info($"[LocalizationHelper] 本地化系统初始化完成，当前语言: {LocalizationManager.CurrentLanguage}");
            }
            catch (Exception ex)
            {
                logger.Error($"[LocalizationHelper] 初始化本地化系统失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 加载所有语言的翻译数据
        /// </summary>
        private static void LoadTranslations()
        {
            _localizationData = new Dictionary<SystemLanguage, Dictionary<string, string>>();
            
            // 简体中文
            _localizationData[SystemLanguage.ChineseSimplified] = new Dictionary<string, string>
            {
                { "Setting_EnableSplash", "开启爆炸飞溅效果" },
                { "Setting_PickupKey", "手动拾取鱼类按键（掉在河里捡不到的鱼）" },
                { "Setting_PickupRadius", "手动拾取范围半径（只能捡鱼别想着白嫖邪教房）" }
            };
            
            // 繁体中文
            _localizationData[SystemLanguage.ChineseTraditional] = new Dictionary<string, string>
            {
                { "Setting_EnableSplash", "開啟爆炸飛濺效果" },
                { "Setting_PickupKey", "手動拾取魚類按鍵（掉在河裡撿不到的魚）" },
                { "Setting_PickupRadius", "手動拾取範圍半徑（只能撿魚別想著白嫖邪教房）" }
            };
            
            // 英语
            _localizationData[SystemLanguage.English] = new Dictionary<string, string>
            {
                { "Setting_EnableSplash", "Enable Explosion Splash Effect" },
                { "Setting_PickupKey", "Manual Fish Pickup Key (for fish that fall in the river and can't be picked up)" },
                { "Setting_PickupRadius", "Manual Pickup Radius (only for fish pickup, don't try to exploit cult rooms)" }
            };
            
            // 日语
            _localizationData[SystemLanguage.Japanese] = new Dictionary<string, string>
            {
                { "Setting_EnableSplash", "爆発スプラッシュ効果を有効にする" },
                { "Setting_PickupKey", "手動魚拾取キー（川に落ちて拾えない魚用）" },
                { "Setting_PickupRadius", "手動拾取範囲半径（魚のみ拾取可能、カルト部屋の悪用は不可）" }
            };
            
            // 韩语
            _localizationData[SystemLanguage.Korean] = new Dictionary<string, string>
            {
                { "Setting_EnableSplash", "폭발 튕김 효과 활성화" },
                { "Setting_PickupKey", "수동 물고기 줍기 키（강에 떨어져서 줍을 수 없는 물고기용）" },
                { "Setting_PickupRadius", "수동 줍기 반경（물고기만 줍기 가능, 컬트 방 악용 불가）" }
            };
            
            // 俄语
            _localizationData[SystemLanguage.Russian] = new Dictionary<string, string>
            {
                { "Setting_EnableSplash", "Включить эффект всплеска от взрыва" },
                { "Setting_PickupKey", "Клавиша ручного подбора рыбы (для рыбы, которая упала в реку и не может быть подобрана)" },
                { "Setting_PickupRadius", "Радиус ручного подбора (только для рыбы, не пытайтесь злоупотреблять комнатами культа)" }
            };
            
            // 法语
            _localizationData[SystemLanguage.French] = new Dictionary<string, string>
            {
                { "Setting_EnableSplash", "Activer l'effet d'éclaboussure d'explosion" },
                { "Setting_PickupKey", "Touche de ramassage manuel des poissons (pour les poissons qui tombent dans la rivière et ne peuvent pas être ramassés)" },
                { "Setting_PickupRadius", "Rayon de ramassage manuel (uniquement pour les poissons, n'essayez pas d'exploiter les salles de culte)" }
            };
        }
        
        /// <summary>
        /// 应用指定语言的翻译
        /// </summary>
        /// <param name="language">目标语言</param>
        private static void ApplyTranslations(SystemLanguage language)
        {
            var logger = Log.GetLogger();
            
            // 如果当前语言没有翻译，回退到英语
            if (!_localizationData.ContainsKey(language))
            {
                language = SystemLanguage.English;
                logger.Info($"[LocalizationHelper] 语言 {language} 没有翻译，回退到英语");
            }
            
            var translations = _localizationData[language];
            int appliedCount = 0;
            
            foreach (var kvp in translations)
            {
                string fullKey = GetFullKey(kvp.Key);
                LocalizationManager.SetOverrideText(fullKey, kvp.Value);
                appliedCount++;
            }
            
            logger.Info($"[LocalizationHelper] 应用了 {appliedCount} 个翻译，语言: {language}");
        }
        
        /// <summary>
        /// 语言切换事件处理器
        /// </summary>
        /// <param name="newLanguage">新语言</param>
        private static void OnLanguageChangedHandler(SystemLanguage newLanguage)
        {
            var logger = Log.GetLogger();
            logger.Info($"[LocalizationHelper] 语言切换到: {newLanguage}");
            
            // 清除旧的文本覆盖 - 由于API不支持，我们跳过这一步
            // LocalizationManager.ClearOverrideTexts();
            
            // 应用新语言的翻译
            ApplyTranslations(newLanguage);
            
            // 触发语言变更事件
            OnLanguageChanged?.Invoke(newLanguage);
        }
        
        /// <summary>
        /// 获取本地化文本
        /// </summary>
        /// <param name="key">本地化键</param>
        /// <returns>本地化文本</returns>
        public static string Get(string key)
        {
            string fullKey = GetFullKey(key);
            return LocalizationManager.GetPlainText(fullKey);
        }
        
        /// <summary>
        /// 获取格式化的本地化文本
        /// </summary>
        /// <param name="key">本地化键</param>
        /// <param name="args">格式化参数</param>
        /// <returns>格式化的本地化文本</returns>
        public static string GetFormatted(string key, params object[] args)
        {
            string text = Get(key);
            try
            {
                return string.Format(text, args);
            }
            catch
            {
                return text;
            }
        }
        
        /// <summary>
        /// 获取完整的本地化键（包含模组前缀）
        /// </summary>
        /// <param name="key">本地化键</param>
        /// <returns>完整的本地化键</returns>
        private static string GetFullKey(string key)
        {
            return MOD_PREFIX + key;
        }
        
        /// <summary>
        /// 清理本地化系统
        /// </summary>
        public static void Cleanup()
        {
            var logger = Log.GetLogger();
            logger.Info("[LocalizationHelper] 清理本地化系统...");
            
            try
            {
                // 取消语言切换事件监听
                LocalizationManager.OnSetLanguage -= OnLanguageChangedHandler;
                
                // 清除文本覆盖 - 由于API不支持，我们跳过这一步
                // LocalizationManager.ClearOverrideTexts();
                
                // 清理事件订阅
                OnLanguageChanged = null;
                
                logger.Info("[LocalizationHelper] 本地化系统清理完成");
            }
            catch (Exception ex)
            {
                logger.Error($"[LocalizationHelper] 清理本地化系统失败: {ex.Message}", ex);
            }
        }
    }
}