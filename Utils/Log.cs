using System;
using UnityEngine;

namespace GrenadeFishing.Utils
{
    /// <summary>
    /// 模组日志模块（Log）
    /// 功能：提供统一的日志输出接口，支持分级日志（信息、调试、警告、错误），并可通过开关控制日志是否输出
    /// </summary>
    /// <remarks>
    /// 核心特性：
    /// - 支持全局日志开关（Enabled属性），发布时可关闭调试信息
    /// - 自动添加模组标识前缀[RadialMenu]，便于日志筛选
    /// - 区分日志级别，适配Unity的不同日志输出方式（Log、LogWarning、LogError）
    /// 
    /// 使用场景：
    /// - 开发阶段：保持Enabled=true，输出详细日志用于调试
    /// - 发布阶段：设置Enabled=false，关闭所有日志输出以优化性能
    /// 
    /// 注意事项：
    /// - 错误日志（Error）建议在捕获异常时使用，可附加Exception对象输出堆栈信息
    /// - 调试日志（DebugMsg）应仅用于开发调试，正式发布前建议清理或依赖开关关闭
    /// </remarks>
    public static class Log
    {
        /// <summary>
        /// 是否启用日志输出（全局开关）
        /// </summary>
        public static bool Enabled = true;

        /// <summary>
        /// 模组日志统一标识前缀，所有日志都会包含此前缀
        /// </summary>
        private const string Prefix = "[手雷炸鱼]";

        /// <summary>
        /// 输出普通信息日志
        /// </summary>
        /// <param name="message">日志内容</param>
        public static void Info(string message)
        {
            if (!Enabled) return;
            Debug.Log($"{Prefix} {message}");
        }

        /// <summary>
        /// 输出调试信息日志（仅用于开发阶段）
        /// </summary>
        /// <param name="message">调试内容</param>
        public static void DebugMsg(string message)
        {
            if (!Enabled) return;
            UnityEngine.Debug.Log($"{Prefix} [DEBUG] {message}");
        }

        /// <summary>
        /// 输出警告信息日志
        /// </summary>
        /// <param name="message">警告内容</param>
        public static void Warn(string message)
        {
            if (!Enabled) return;
            UnityEngine.Debug.LogWarning($"{Prefix} [WARN] {message}");
        }

        /// <summary>
        /// 输出错误信息日志，可附加异常详情
        /// </summary>
        /// <param name="message">错误描述</param>
        /// <param name="ex">可选，关联的异常对象，将输出异常信息和堆栈跟踪</param>
        public static void Error(string message, Exception? ex = null)
        {
            if (!Enabled) return;
            string finalMsg = ex == null ? message : $"{message}\n{ex}";
            UnityEngine.Debug.LogError($"{Prefix} [ERROR] {finalMsg}");
        }

        public static void Warn(string message, Exception? ex = null)
        {
            if (!Enabled) return;
            UnityEngine.Debug.LogWarning($"{Prefix} [WARN] {message}" + (ex != null ? $"\n{ex}" : ""));
        }

    }
}