using System;

namespace GrenadeFishing.Utils
{
	/// <summary>
	/// 记录最近一次投掷手雷时的物品价格，供掉落生成读取。
	/// </summary>
	public static class GrenadePriceTracker
	{
		private static int _lastGrenadeValue = -1;

		public static void SetLastGrenadeValue(int value)
		{
			_lastGrenadeValue = value;
		}

		public static int GetLastGrenadeValueOr(int fallback)
		{
			return _lastGrenadeValue > 0 ? _lastGrenadeValue : fallback;
		}
	}
}


