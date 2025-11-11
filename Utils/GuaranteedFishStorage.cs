using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace GrenadeFishing.Utils
{
    /// <summary>
    /// 保底计数器JSON存储模块
    /// 使用手动JSON构建和解析，避免JsonUtility的问题
    /// </summary>
    public static class GuaranteedFishStorage
    {
        // 日志模块
        private static readonly GrenadeFishing.Utils.Logger L = GrenadeFishing.Utils.Log.GetLogger();
        // 在类初始化时，由你定义的局部布尔变量控制该文件日志：
        private static bool LocalLogs = true; // 你可以在别处修改这个变量
        
        private static readonly string StorageFileName = "grenade_fishing_guaranteed.json";
        private static readonly string StorageFilePath;
        private static readonly Dictionary<string, int> Counters = new Dictionary<string, int>();
        private static bool _loaded = false;

        static GuaranteedFishStorage()
        {
            L.SetEnabled(LocalLogs); // 一次设置即可
            
            // 获取DLL所在路径
            string dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string dllDir = Path.GetDirectoryName(dllPath);
            StorageFilePath = Path.Combine(dllDir, StorageFileName);
            
            L.Info($"[保底存储] 初始化存储路径: {StorageFilePath}");
            
            // 启动时加载数据
            LoadCounters();
        }

        /// <summary>
        /// 获取计数器值
        /// </summary>
        /// <param name="key">计数器键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>计数器值</returns>
        public static int GetCounter(string key, int defaultValue = 0)
        {
            if (!_loaded)
            {
                LoadCounters();
            }

            if (Counters.TryGetValue(key, out int value))
            {
                L.DebugMsg($"[保底存储] 读取计数器成功: {key} = {value}");
                return value;
            }

            L.DebugMsg($"[保底存储] 计数器不存在，返回默认值: {key} = {defaultValue}");
            return defaultValue;
        }

        /// <summary>
        /// 设置计数器值
        /// </summary>
        /// <param name="key">计数器键</param>
        /// <param name="value">计数器值</param>
        public static void SetCounter(string key, int value)
        {
            Counters[key] = value;
            L.DebugMsg($"[保底存储] 设置计数器: {key} = {value}");
            SaveCounters();
        }

        /// <summary>
        /// 增加计数器值
        /// </summary>
        /// <param name="key">计数器键</param>
        /// <param name="increment">增加量</param>
        /// <returns>增加后的值</returns>
        public static int IncrementCounter(string key, int increment = 1)
        {
            int newValue = GetCounter(key, 0) + increment;
            SetCounter(key, newValue);
            return newValue;
        }

        /// <summary>
        /// 手动构建JSON字符串
        /// </summary>
        /// <returns>JSON字符串</returns>
        private static string BuildJson()
        {
            var sb = new StringBuilder();
            sb.Append("{");
            
            bool first = true;
            foreach (var kvp in Counters)
            {
                if (!first)
                {
                    sb.Append(",");
                }
                first = false;
                
                sb.Append("\"");
                sb.Append(EscapeJsonString(kvp.Key));
                sb.Append("\":");
                sb.Append(kvp.Value);
            }
            
            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>
        /// 简化的JSON解析器
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>解析的键值对</returns>
        private static Dictionary<string, int> ParseJson(string json)
        {
            var result = new Dictionary<string, int>();
            
            if (string.IsNullOrWhiteSpace(json))
                return result;

            try
            {
                // 移除空白字符
                json = json.Trim();
                
                // 检查是否是对象格式
                if (!json.StartsWith("{") || !json.EndsWith("}"))
                    return result;

                // 移除大括号
                json = json.Substring(1, json.Length - 2).Trim();
                
                if (string.IsNullOrEmpty(json))
                    return result;

                // 简单解析键值对
                var pairs = SplitJsonPairs(json);
                
                foreach (var pair in pairs)
                {
                    var keyValue = SplitKeyValue(pair);
                    if (keyValue.Length == 2)
                    {
                        string key = UnescapeJsonString(keyValue[0].Trim());
                        if (int.TryParse(keyValue[1].Trim(), out int value))
                        {
                            result[key] = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                L.Error($"[保底存储] JSON解析失败: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// 分割JSON键值对
        /// </summary>
        private static List<string> SplitJsonPairs(string json)
        {
            var pairs = new List<string>();
            var sb = new StringBuilder();
            bool inString = false;
            int braceLevel = 0;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                {
                    inString = !inString;
                }

                if (!inString)
                {
                    if (c == '{')
                    {
                        braceLevel++;
                    }
                    else if (c == '}')
                    {
                        braceLevel--;
                    }
                    else if (c == ',' && braceLevel == 0)
                    {
                        pairs.Add(sb.ToString());
                        sb.Clear();
                        continue;
                    }
                }

                sb.Append(c);
            }

            if (sb.Length > 0)
            {
                pairs.Add(sb.ToString());
            }

            return pairs;
        }

        /// <summary>
        /// 分割键值
        /// </summary>
        private static string[] SplitKeyValue(string pair)
        {
            int colonIndex = -1;
            bool inString = false;

            for (int i = 0; i < pair.Length; i++)
            {
                char c = pair[i];

                if (c == '"' && (i == 0 || pair[i - 1] != '\\'))
                {
                    inString = !inString;
                }

                if (!inString && c == ':')
                {
                    colonIndex = i;
                    break;
                }
            }

            if (colonIndex == -1)
                return new string[0];

            string key = pair.Substring(0, colonIndex);
            string value = pair.Substring(colonIndex + 1);

            return new string[] { key, value };
        }

        /// <summary>
        /// 转义JSON字符串
        /// </summary>
        private static string EscapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            var sb = new StringBuilder();
            foreach (char c in str)
            {
                switch (c)
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < 0x20)
                        {
                            sb.AppendFormat("\\u{0:x4}", (int)c);
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 反转义JSON字符串
        /// </summary>
        private static string UnescapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            // 移除引号
            if (str.StartsWith("\"") && str.EndsWith("\""))
            {
                str = str.Substring(1, str.Length - 2);
            }

            var sb = new StringBuilder();
            bool escape = false;

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (escape)
                {
                    switch (c)
                    {
                        case '"':
                            sb.Append('"');
                            break;
                        case '\\':
                            sb.Append('\\');
                            break;
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'u':
                            if (i + 4 < str.Length)
                            {
                                string hex = str.Substring(i + 1, 4);
                                if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int code))
                                {
                                    sb.Append((char)code);
                                    i += 4;
                                }
                                else
                                {
                                    sb.Append(c);
                                }
                            }
                            else
                            {
                                sb.Append(c);
                            }
                            break;
                        default:
                            sb.Append(c);
                            break;
                    }
                    escape = false;
                }
                else if (c == '\\')
                {
                    escape = true;
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 保存计数器到文件
        /// </summary>
        private static void SaveCounters()
        {
            try
            {
                string json = BuildJson();
                File.WriteAllText(StorageFilePath, json, Encoding.UTF8);
                L.Info($"[保底存储] 保存计数器成功: {StorageFilePath}");
            }
            catch (Exception ex)
            {
                L.Error($"[保底存储] 保存计数器失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从文件加载计数器
        /// </summary>
        private static void LoadCounters()
        {
            try
            {
                if (File.Exists(StorageFilePath))
                {
                    string json = File.ReadAllText(StorageFilePath, Encoding.UTF8);
                    var loadedCounters = ParseJson(json);
                    
                    Counters.Clear();
                    foreach (var kvp in loadedCounters)
                    {
                        Counters[kvp.Key] = kvp.Value;
                    }
                    
                    _loaded = true;
                    L.Info($"[保底存储] 加载计数器成功，共{Counters.Count}个计数器");
                }
                else
                {
                    L.Info($"[保底存储] 存储文件不存在，使用空计数器");
                    _loaded = true;
                }
            }
            catch (Exception ex)
            {
                L.Error($"[保底存储] 加载计数器失败: {ex.Message}", ex);
                _loaded = true; // 防止重复尝试
            }
        }

        /// <summary>
        /// 获取所有计数器的副本（用于调试）
        /// </summary>
        public static Dictionary<string, int> GetAllCounters()
        {
            return new Dictionary<string, int>(Counters);
        }

        /// <summary>
        /// 重置所有计数器（用于调试）
        /// </summary>
        public static void ResetAllCounters()
        {
            Counters.Clear();
            SaveCounters();
            L.Info("[保底存储] 已重置所有计数器");
        }
    }
}