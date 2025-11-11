using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GrenadeFishing.Utils
{
    /// <summary>
    /// 日志级别
    /// </summary>
    internal enum LogLevel
    {
        Info,
        Debug,
        Warn,
        Error
    }

    /// <summary>
    /// 模组日志模块（Log）
    /// - 支持全局开关 Enabled
    /// - 支持“放行”警告/错误（AlwaysShowWarningsAndErrors）
    /// - 支持模块/文件级别开关（通过 Logger 或 SetModuleEnabled 设置）
    /// - 自动以调用源文件名作为模块名，方便按文件控制
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// 是否启用信息/调试类日志（全局开关）。
        /// 注意：警告/错误的输出受 AlwaysShowWarningsAndErrors 控制（默认 true）。
        /// </summary>
        public static bool Enabled = true;

        /// <summary>
        /// 即使 Enabled == false，是否仍然显示 Warn / Error。默认 true（"放行"重要日志）。
        /// 将此置为 false 可完全按照 Enabled 控制所有等级。
        /// </summary>
        public static bool AlwaysShowWarningsAndErrors = true;

        /// <summary>
        /// 模组日志统一标识前缀
        /// </summary>
        private const string Prefix = "[手雷炸鱼]";

        /// <summary>
        /// 按模块名缓存 Logger 实例（模块名通常来自调用文件名）
        /// </summary>
        private static readonly ConcurrentDictionary<string, Logger> _loggers = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 获取/创建一个绑定到当前调用文件的 Logger，便于模块级设置。
        /// 使用方法（每个 cs 文件内）:
        /// private static readonly Logger L = Log.GetLogger();
        /// L.SetEnabled(myLocalBool); // 如果需要由本文件外部变量控制，只需在一次初始化时调用
        /// L.Info("hello");
        /// </summary>
        public static Logger GetLogger([CallerFilePath] string callerFilePath = "")
        {
            var module = GetModuleNameFromPath(callerFilePath);
            return _loggers.GetOrAdd(module, name => new Logger(name));
        }

        /// <summary>
        /// 直接设置某个模块（文件名）是否启用日志（信息/调试）。模块名通常是文件名，如 "MyClass.cs"。
        /// </summary>
        public static void SetModuleEnabled(string moduleName, bool enabled)
        {
            var logger = _loggers.GetOrAdd(moduleName, name => new Logger(name));
            logger.SetEnabled(enabled);
        }

        /// <summary>
        /// 检查模块是否启用（若模块未知则返回 true）
        /// </summary>
        public static bool IsModuleEnabled(string moduleName)
        {
            if (_loggers.TryGetValue(moduleName, out var logger)) return logger.Enabled;
            return true;
        }

        #region 便捷静态方法（自动获得调用源文件作为模块名）

        public static void Info(string message, bool? localModuleOverride = null, [CallerFilePath] string callerFilePath = "")
        {
            InternalLog(LogLevel.Info, message, localModuleOverride, callerFilePath, null);
        }

        public static void DebugMsg(string message, bool? localModuleOverride = null, [CallerFilePath] string callerFilePath = "")
        {
            InternalLog(LogLevel.Debug, message, localModuleOverride, callerFilePath, null);
        }

        public static void Warn(string message, Exception? ex = null, bool? localModuleOverride = null, [CallerFilePath] string callerFilePath = "")
        {
            InternalLog(LogLevel.Warn, message, localModuleOverride, callerFilePath, ex);
        }

        public static void Error(string message, Exception? ex = null, bool? localModuleOverride = null, [CallerFilePath] string callerFilePath = "")
        {
            InternalLog(LogLevel.Error, message, localModuleOverride, callerFilePath, ex);
        }

        #endregion

        /// <summary>
        /// 内部统一判断与输出逻辑
        /// </summary>
        internal static void InternalLog(LogLevel level, string message, bool? localModuleOverride, string callerFilePath, Exception? ex)
        {
            var module = GetModuleNameFromPath(callerFilePath);

            // 决定是否应当输出（根据全局开关/模块开关/等级）
            if (!ShouldLog(level, module, localModuleOverride)) return;

            var tag = $"{Prefix} [{module}]";
            var finalMsg = (ex == null) ? $"{tag} {message}" : $"{tag} {message}\n{ex}";

            switch (level)
            {
                case LogLevel.Info:
                case LogLevel.Debug:
                    UnityEngine.Debug.Log(finalMsg);
                    break;
                case LogLevel.Warn:
                    UnityEngine.Debug.LogWarning(finalMsg);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(finalMsg);
                    break;
            }
        }

        /// <summary>
        /// 决定某条日志是否应该被输出
        /// 逻辑说明：
        /// - Info/Debug：先检查全局 Enabled，再检查模块级 Enabled（或 localModuleOverride）
        /// - Warn/Error：若 AlwaysShowWarningsAndErrors==true 则即便全局 Enabled==false 也会输出；模块级禁用不会屏蔽 Warning/Error（以便错误能被看到）
        /// </summary>
        private static bool ShouldLog(LogLevel level, string module, bool? localModuleOverride)
        {
            // Info/Debug 级别 —— 需要全局与模块都允许
            if (level == LogLevel.Info || level == LogLevel.Debug)
            {
                if (!Enabled) return false; // 全局关掉 info/debug
                // local override 优先
                if (localModuleOverride.HasValue) return localModuleOverride.Value;
                // 否则检查已注册的 logger（若没有注册则视为允许）
                if (_loggers.TryGetValue(module, out var logger)) return logger.Enabled;
                return true;
            }

            // Warn/Error 级别 —— 若全局 Enabled 关且 AlwaysShowWarningsAndErrors==false，则被全局屏蔽
            if (!Enabled && !AlwaysShowWarningsAndErrors) return false;

            // 模块级禁用不应屏蔽 Warn/Error（这是设计选择，保证错误能被看见）
            return true;
        }

        private static string GetModuleNameFromPath(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return "UnknownModule";
                return Path.GetFileName(path) ?? path;
            }
            catch
            {
                return path ?? "UnknownModule";
            }
        }
    }

    /// <summary>
    /// 模块级 Logger，绑定到一个模块名（通常是源文件名），提供实例方法并保存模块级 Enabled。
    /// </summary>
    public sealed class Logger
    {
        internal bool Enabled;
        public string ModuleName { get; }

        internal Logger(string moduleName)
        {
            ModuleName = moduleName;
            Enabled = true;
        }

        /// <summary>
        /// 设置该模块是否启用 Info/Debug 日志（只需在模块初始化时设置一次即可）。
        /// 如果你在该模块中定义了一个布尔变量用于控制日志，可以在模块静态构造或 Start 中调用这个方法绑定它。
        /// </summary>
        public void SetEnabled(bool enabled) => Enabled = enabled;

        public void Info(string message) => Log.InternalLog(LogLevel.Info, message, null, ModuleName, null);
        public void DebugMsg(string message) => Log.InternalLog(LogLevel.Debug, message, null, ModuleName, null);
        public void Warn(string message, Exception? ex = null) => Log.InternalLog(LogLevel.Warn, message, null, ModuleName, ex);
        public void Error(string message, Exception? ex = null) => Log.InternalLog(LogLevel.Error, message, null, ModuleName, ex);
    }
}
