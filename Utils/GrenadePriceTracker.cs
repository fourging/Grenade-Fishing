using System;
using UnityEngine;

namespace GrenadeFishing.Utils
{
	/// <summary>
	/// 记录最近一次投掷手雷时的物品价格，供掉落生成读取。
	/// </summary>
	public static class GrenadePriceTracker
	{
		// 日志模块
		private static readonly GrenadeFishing.Utils.Logger L = GrenadeFishing.Utils.Log.GetLogger();
		// 在类初始化时，由你定义的局部布尔变量控制该文件日志：
		private static bool LocalLogs = true; // 你可以在别处修改这个变量
		static GrenadePriceTracker()
		{
			L.SetEnabled(LocalLogs); // 一次设置即可
		}
		
		private static int _lastGrenadeValue = -1;

		public static void SetLastGrenadeValue(int value)
		{
			L.DebugMsg($"[炸鱼保底调试] GrenadePriceTracker.SetLastGrenadeValue: 设置手雷价格={value}（之前值={_lastGrenadeValue}）");
			_lastGrenadeValue = value;
		}

		public static int GetLastGrenadeValueOr(int fallback)
		{
			int result = _lastGrenadeValue > 0 ? _lastGrenadeValue : fallback;
			L.DebugMsg($"[炸鱼保底调试] GrenadePriceTracker.GetLastGrenadeValueOr: 返回手雷价格={result}（内部值={_lastGrenadeValue}, fallback={fallback}）");
			return result;
		}
	}
}


