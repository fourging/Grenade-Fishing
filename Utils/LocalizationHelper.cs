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
                { "Setting_PickupRadius", "手动拾取范围半径（只能捡鱼别想着白嫖邪教房）" },
                
                // FishGenerator 语音文本
                { "Fish_Guaranteed_1", "系统提示：保底啦！你炸上来了一条 {0}！" },
                { "Fish_Guaranteed_2", "不错嘛，运气比技术重要。" },
                { "Fish_Dud_1", "哎呀，这次手雷好像只是来散步的——水里一点动静都没有。" },
                { "Fish_Dud_2", "手雷变成了和平鸽，鱼们正在庆祝。" },
                { "Fish_Miracle_1", "不可思议！直接炸出了{0}！" },
                { "Fish_Miracle_2", "奇迹发生了。" },
                { "Fish_Big_1", "哇！大鱼：{0}！别让它逃跑——或者把它当成晚饭。" },
                { "Fish_Big_2", "感觉到一股钞票的味道。" },
                { "Fish_Guaranteed_High100_1", "保底出货：{0}！你这是富可敌乡。" },
                { "Fish_Guaranteed_High100_2", "别问，问就是战术。" },
                { "Fish_Guaranteed_High50_1", "保底：恭喜，你炸上来一条 {0}！" },
                { "Fish_Guaranteed_High50_2", "别忘了给它拍张照。" },
                { "Fish_Guaranteed_Low114_1", "鱼都是被臭死的！" },
                { "Fish_Guaranteed_Low114_2", "好吧，你值得这份奖励，114514" },
                { "Fish_Guaranteed_Low51_1", "幸运轮盘：你炸上来一条 {0}！（低价保底）" },
                { "Fish_Guaranteed_Low51_2", "鸭子表示：我也惊呆了。" },
                { "Fish_Guaranteed_Low20_1", "保底小奖：你得到了 {0}，别嫌弃它小。" },
                { "Fish_Guaranteed_Low20_2", "至少不是空手而归。" }
            };
            
            // 繁体中文
            _localizationData[SystemLanguage.ChineseTraditional] = new Dictionary<string, string>
            {
                { "Setting_EnableSplash", "開啟爆炸飛濺效果" },
                { "Setting_PickupKey", "手動拾取魚類按鍵（掉在河裡撿不到的魚）" },
                { "Setting_PickupRadius", "手動拾取範圍半徑（只能撿魚別想著白嫖邪教房）" },
                
                // FishGenerator 语音文本

                { "Fish_Guaranteed_1", "系統提示：保底啦！你炸上來了一條 {0}！" },
                { "Fish_Guaranteed_2", "不錯嘛，運氣比技術重要。" },
                { "Fish_Dud_1", "哎呀，這次手雷好像只是來散步的——水裡一點動靜都沒有。" },
                { "Fish_Dud_2", "手雷變成了和平鴿，魚們正在慶祝。" },
                { "Fish_Miracle_1", "不可思議！直接炸出了 {0}！這是運氣還是操作失誤？" },
                { "Fish_Miracle_2", "奇蹟發生了——鴨子驚呆了。" },
                { "Fish_Big_1", "哇！大魚：{0}！別讓它逃跑——或者把它當成晚飯。" },
                { "Fish_Big_2", "感覺到一股鈔票的味道。" },
                { "Fish_Guaranteed_High100_1", "保底出貨：{0}！你這是富可敵鄉。" },
                { "Fish_Guaranteed_High100_2", "別問，問就是戰術。" },
                { "Fish_Guaranteed_High50_1", "保底：恭喜，你炸上來一條 {0}！" },
                { "Fish_Guaranteed_High50_2", "別忘了給它拍張照。" },
                { "Fish_Guaranteed_Low114_1", "魚都是被臭死的！" },
                { "Fish_Guaranteed_Low114_2", "好吧，你值得這份獎勵，114514" },
                { "Fish_Guaranteed_Low51_1", "幸運輪盤：你炸上來一條 {0}！（低價保底）" },
                { "Fish_Guaranteed_Low51_2", "鴨子表示：我也驚呆了。" },
                { "Fish_Guaranteed_Low20_1", "保底小獎：你得到了 {0}，別嫌棄它小。" },
                { "Fish_Guaranteed_Low20_2", "至少不是空手而歸。" }
            };
            
            // 英语
            _localizationData[SystemLanguage.English] = new Dictionary<string, string>
            {
                { "Setting_EnableSplash", "Enable Explosion Splash Effect" },
                { "Setting_PickupKey", "Manual Fish Pickup Key (for fish that fall in the river and can't be picked up)" },
                { "Setting_PickupRadius", "Manual Pickup Radius (only for fish pickup, don't try to exploit cult rooms)" },
                
                // FishGenerator 语音文本
                { "Fish_Guaranteed_1", "System alert: Pity activated! You've fished up a {0}!" },
                { "Fish_Guaranteed_2", "Not bad, luck is more important than skill." },
                { "Fish_Dud_1", "Oops, this grenade seems to be just taking a walk - no movement in the water." },
                { "Fish_Dud_2", "The grenade turned into a peace dove, the fish are celebrating." },
                { "Fish_Miracle_1", "Incredible! You directly fished up a {0}! Is this luck or operator error?" },
                { "Fish_Miracle_2", "A miracle happened - the duck is stunned." },
                { "Fish_Big_1", "Wow! Big fish: {0}! Don't let it escape - or make it dinner." },
                { "Fish_Big_2", "I smell money." },
                { "Fish_Guaranteed_High100_1", "Pity drop: {0}! You're rich enough to rival the town." },
                { "Fish_Guaranteed_High100_2", "Don't ask, it's tactics." },
                { "Fish_Guaranteed_High50_1", "Pity: Congratulations, you've fished up a {0}!" },
                { "Fish_Guaranteed_High50_2", "Don't forget to take a photo." },
                { "Fish_Guaranteed_Low114_1", "The fish were all stinked to death!" },
                { "Fish_Guaranteed_Low114_2", "Well, you deserve this reward, 114514" },
                { "Fish_Guaranteed_Low51_1", "Lucky roulette: You've fished up a {0}! (low-cost pity)" },
                { "Fish_Guaranteed_Low51_2", "Duck says: I'm also stunned." },
                { "Fish_Guaranteed_Low20_1", "Pity small prize: You got a {0}, don't dislike it for being small." },
                { "Fish_Guaranteed_Low20_2", "At least you didn't return empty-handed." }
            };
            
            // 日语
            _localizationData[SystemLanguage.Japanese] = new Dictionary<string, string>
            {
                { "Setting_EnableSplash", "爆発スプラッシュ効果を有効にする" },
                { "Setting_PickupKey", "手動魚拾取キー（川に落ちて拾えない魚用）" },
                { "Setting_PickupRadius", "手動拾取範囲半径（魚のみ拾取可能、カルト部屋の悪用は不可）" },
                
                // FishGenerator 语音文本
                { "Fish_Guaranteed_1", "システム通知：保証発動！{0} を釣り上げました！" },
                { "Fish_Guaranteed_2", "いいね、運は技術より重要だ。" },
                { "Fish_Dud_1", "おっと、この手榴弾はただ散歩してきただけみたい - 水中に全く動きがない。" },
                { "Fish_Dud_2", "手榴弾が平和鳩に変わった、魚たちが祝っている。" },
                { "Fish_Miracle_1", "信じられない！直接 {0} を釣り上げた！これは運か操作ミスか？" },
                { "Fish_Miracle_2", "奇跡が起きた - アヒルが驚いている。" },
                { "Fish_Big_1", "わお！大魚：{0}！逃がすな - または夕食にしろ。" },
                { "Fish_Big_2", "お金の匂いがする。" },
                { "Fish_Guaranteed_High100_1", "保証ドロップ：{0}！君は町に匹敵する金持ちだ。" },
                { "Fish_Guaranteed_High100_2", "聞くな、戦術だ。" },
                { "Fish_Guaranteed_High50_1", "保証：おめでとう、{0} を釣り上げた！" },
                { "Fish_Guaranteed_High50_2", "写真を撮るのを忘れるな。" },
                { "Fish_Guaranteed_Low114_1", "魚は全部臭いで死んだ！" },
                { "Fish_Guaranteed_Low114_2", "まあ、この報酬は君に値する、114514" },
                { "Fish_Guaranteed_Low51_1", "幸運ルーレット：{0} を釣り上げた！（安価保証）" },
                { "Fish_Guaranteed_Low51_2", "アヒル says：私も驚いている。" },
                { "Fish_Guaranteed_Low20_1", "保証小賞：{0} を手に入れた、小さいからといって嫌がるな。" },
                { "Fish_Guaranteed_Low20_2", "少なくとも手ぶらで帰ることはない。" }
            };
            
            // 韩语
            _localizationData[SystemLanguage.Korean] = new Dictionary<string, string>
            {
                { "Setting_EnableSplash", "폭발 튕김 효과 활성화" },
                { "Setting_PickupKey", "수동 물고기 줍기 키（강에 떨어져서 줍을 수 없는 물고기용）" },
                { "Setting_PickupRadius", "수동 줍기 반경（물고기만 줍기 가능, 컬트 방 악용 불가）" },
                
                // FishGenerator 语音文本
                { "Fish_Guaranteed_1", "시스템 알림: 보정 발동! {0}을(를) 잡았습니다!" },
                { "Fish_Guaranteed_2", "괜찮네, 운이 기술보다 중요하네." },
                { "Fish_Dud_1", "어머나, 이 수류탄은 그냥 산책하러 온 것 같아 - 물속에 전혀 움직임이 없어." },
                { "Fish_Dud_2", "수류탄이 평화 비둘기로 변했어, 물고기들이 축하하고 있어." },
                { "Fish_Miracle_1", "믿을 수 없어! 바로 {0}을(를) 잡았어! 이게 운인가 실수인가?" },
                { "Fish_Miracle_2", "기적이 일어났어 - 오리가 놀랐어." },
                { "Fish_Big_1", "와! 큰 물고기: {0}! 도망가게 두지 마 - 아니면 저녁으로 만들어." },
                { "Fish_Big_2", "돈 냄새가 나네." },
                { "Fish_Guaranteed_High100_1", "보정 드롭: {0}! 넌 마을을 능가하는 부자야." },
                { "Fish_Guaranteed_High100_2", "묻지 마, 전술이야." },
                { "Fish_Guaranteed_High50_1", "보정: 축하해, {0}을(를) 잡았어!" },
                { "Fish_Guaranteed_High50_2", "사진 찍는 거 잊지 마." },
                { "Fish_Guaranteed_Low114_1", "물고기는 다 냄새로 죽었다!" },
                { "Fish_Guaranteed_Low114_2", "좋아, 이 보상은 네가 받을 만해, 114514" },
                { "Fish_Guaranteed_Low51_1", "행운 룰렛: {0}을(를) 잡았어!（저가 보정）" },
                { "Fish_Guaranteed_Low51_2", "오리 says: 나도 놀랐어." },
                { "Fish_Guaranteed_Low20_1", "보정 소상: {0}을(를) 얻었어, 작다고 싫어하지 마." },
                { "Fish_Guaranteed_Low20_2", "적어도 빈손으로 돌아가진 않아." }
            };
            
            // 俄语
            _localizationData[SystemLanguage.Russian] = new Dictionary<string, string>
            {
                { "Setting_EnableSplash", "Включить эффект всплеска от взрыва" },
                { "Setting_PickupKey", "Клавиша ручного подбора рыбы (для рыбы, которая упала в реку и не может быть подобрана)" },
                { "Setting_PickupRadius", "Радиус ручного подбора (только для рыбы, не пытайтесь злоупотреблять комнатами культа)" },
                
                // FishGenerator 语音文本
                { "Fish_Guaranteed_1", "Системное оповещение: Сработала гарантия! Вы поймали {0}!" },
                { "Fish_Guaranteed_2", "Неплохо, удача важнее навыков." },
                { "Fish_Dud_1", "Ой, эта граната, кажется, просто прогулялась - никакого движения в воде." },
                { "Fish_Dud_2", "Граната превратилась в голубя мира, рыбы празднуют." },
                { "Fish_Miracle_1", "Невероятно! Вы сразу поймали {0}! Это удача или ошибка оператора?" },
                { "Fish_Miracle_2", "Произошло чудо - утка в шоке." },
                { "Fish_Big_1", "Вау! Большая рыба: {0}! Не дайте ей уйти - или сделайте ужином." },
                { "Fish_Big_2", "Чувствую запах денег." },
                { "Fish_Guaranteed_High100_1", "Гарантированная добыча: {0}! Вы богаты, как целый город." },
                { "Fish_Guaranteed_High100_2", "Не спрашивайте, это тактика." },
                { "Fish_Guaranteed_High50_1", "Гарантия: Поздравляем, вы поймали {0}!" },
                { "Fish_Guaranteed_High50_2", "Не забудьте сделать фото." },
                { "Fish_Guaranteed_Low114_1", "Рыбы все умерли от вони!" },
                { "Fish_Guaranteed_Low114_2", "Ладно, ты заслужил эту награду, 114514" },
                { "Fish_Guaranteed_Low51_1", "Колесо фортуны: Вы поймали {0}! (гарантия низкой цены)" },
                { "Fish_Guaranteed_Low51_2", "Утка говорит: Я тоже в шоке." },
                { "Fish_Guaranteed_Low20_1", "Маленький приз гарантии: Вы получили {0}, не презирайте её за малость." },
                { "Fish_Guaranteed_Low20_2", "По крайней мере, вы не вернулись с пустыми руками." }
            };
            
            // 法语
            _localizationData[SystemLanguage.French] = new Dictionary<string, string>
            {
                { "Setting_EnableSplash", "Activer l'effet d'éclaboussure d'explosion" },
                { "Setting_PickupKey", "Touche de ramassage manuel des poissons (pour les poissons qui tombent dans la rivière et ne peuvent pas être ramassés)" },
                { "Setting_PickupRadius", "Rayon de ramassage manuel (uniquement pour les poissons, n'essayez pas d'exploiter les salles de culte)" },
                
                // FishGenerator 语音文本
                { "Fish_Guaranteed_1", "Alerte système : Pitié activée ! Vous avez pêché un {0} !" },
                { "Fish_Guaranteed_2", "Pas mal, la chance est plus importante que le talent." },
                { "Fish_Dud_1", "Oups, cette grenade semble juste se promener - aucun mouvement dans l'eau." },
                { "Fish_Dud_2", "La grenade s'est transformée en colombe de la paix, les poissons célèbrent." },
                { "Fish_Miracle_1", "Incroyable ! Vous avez directement pêché un {0} ! C'est la chance ou une erreur d'opérateur ?" },
                { "Fish_Miracle_2", "Un miracle s'est produit - le canard est stupéfait." },
                { "Fish_Big_1", "Wow ! Gros poisson : {0} ! Ne le laissez pas s'échapper - ou faites-en le dîner." },
                { "Fish_Big_2", "Je sens l'argent." },
                { "Fish_Guaranteed_High100_1", "Récompense garantie : {0} ! Vous êtes assez riche pour rivaliser avec la ville." },
                { "Fish_Guaranteed_High100_2", "Ne demandez pas, c'est tactique." },
                { "Fish_Guaranteed_High50_1", "Garantie : Félicitations, vous avez pêché un {0} !" },
                { "Fish_Guaranteed_High50_2", "N'oubliez pas de prendre une photo." },
                { "Fish_Guaranteed_Low114_1", "Les poissons sont tous morts par l'odeur !" },
                { "Fish_Guaranteed_Low114_2", "Bon, tu mérites cette récompense, 114514" },
                { "Fish_Guaranteed_Low51_1", "Roulette de la chance : Vous avez pêché un {0} ! (pitié bas prix)" },
                { "Fish_Guaranteed_Low51_2", "Le canard dit : Je suis aussi stupéfait." },
                { "Fish_Guaranteed_Low20_1", "Petit prix garanti : Vous avez obtenu un {0}, ne le méprisez pas pour sa petite taille." },
                { "Fish_Guaranteed_Low20_2", "Au moins vous n'êtes pas revenu les mains vides." }
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