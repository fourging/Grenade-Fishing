using System;
using UnityEngine;
using Duckov.UI.DialogueBubbles;
using Cysharp.Threading.Tasks;

namespace GrenadeFishing.Utils
{
    
    // speakup.ShowRandomDialogue(
    //     character.transform,
    //     1f, // 延迟 1 秒
    //     $"准备偷吃 {item.DisplayName}！",
    //     $"装备 {item.DisplayName}！",
    //     $"偷偷尝一口 {item.DisplayName}～"
    // );

    // speakup.ShowDialogue("战斗准备完毕！", delay: 2.5f);

    /// <summary>
    /// speakup 工具类
    /// 提供便捷方法显示 DialogueBubblesManager 弹窗
    /// </summary>
    public static class speakup
    {
        /// <summary>
        /// 显示对话气泡（可选延迟）
        /// </summary>
        /// <param name="message">要显示的文本</param>
        /// <param name="target">目标 Transform，如果为 null 会尝试自动获取玩家 Transform</param>
        /// <param name="yOffset">垂直偏移，可选，默认 -1</param>
        /// <param name="needInteraction">是否需要交互，可选，默认 false</param>
        /// <param name="skippable">是否可跳过，可选，默认 false</param>
        /// <param name="speed">显示速度，可选，默认 -1</param>
        /// <param name="duration">显示时长，可选，默认 2</param>
        /// <param name="delay">延迟显示秒数，可选，默认 0</param>
        public static async void ShowDialogue(
            string message,
            Transform target = null!,
            float yOffset = -1,
            bool needInteraction = false,
            bool skippable = false,
            float speed = -1,
            float duration = 2,
            float delay = 0
        )
        {
            try
            {

                // 延迟执行（如果 delay > 0）
                if (delay > 0)
                    await UniTask.Delay(TimeSpan.FromSeconds(delay));

                // 自动获取玩家 Transform
                if (target == null)
                {
                    var main = CharacterMainControl.Main;
                    if (main == null)
                    {
                        Log.Warn("[speakup] 无法获取 CharacterMainControl.Main，弹窗无法显示");
                        return;
                    }

                    target = main.transform;
                }

                // 调用 DialogueBubblesManager
                DialogueBubblesManager.Show(
                    message,
                    target,
                    yOffset,
                    needInteraction,
                    skippable,
                    speed,
                    duration
                ).Forget();
            }
            catch (Exception ex)
            {
                Log.Error($"[speakup] 显示弹窗失败: {ex}");
            }
        }

        /// <summary>
        /// 随机显示一条对话气泡（可选延迟）
        /// </summary>
        /// <param name="target">目标 Transform</param>
        /// <param name="delay">延迟秒数</param>
        /// <param name="messages">候选文本</param>
        public static void ShowRandomDialogue(Transform target, float delay = 0, params string[] messages)
        {


            if (messages == null || messages.Length == 0)
            {
                Log.Warn("[speakup] ShowRandomDialogue 调用时未提供消息文本");
                return;
            }

            int index = UnityEngine.Random.Range(0, messages.Length);
            string message = messages[index];

            ShowDialogue(message, target, delay: delay);
        }
    }
}

