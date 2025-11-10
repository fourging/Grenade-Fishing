using System;
using System.Collections.Generic;
using System.Linq;

namespace GrenadeFishing.Utils
{
	/// <summary>
	/// 鱼类抽样与生成器（独立于掉落/图标），便于后续模块化替换。
	/// 主要功能：
	/// - 根据幸运值决定本次生成数量区间
	/// - 按价值分档与概率抽样鱼种
	/// - 提供基于价值的分层随机选择机制
	/// </summary>
	public static class FishGenerator
	{
		/// <summary>
		/// 鱼类定义类，包含鱼的基本信息
		/// </summary>
		public class FishDefinition
		{
			/// <summary>
			/// 鱼的显示名称
			/// </summary>
			public string displayName = string.Empty;
			
			/// <summary>
			/// 鱼的类型ID，用于在游戏中识别具体的鱼种
			/// </summary>
			public int typeId;
			
			/// <summary>
			/// 鱼的价值，用于分层和概率计算
			/// </summary>
			public int value;
		}

		#region 价值分档阈值常量
		/// <summary>
		/// 第一档价值上限 (0 ~ 1000)
		/// </summary>
		private const int Tier1Max = 1000;
		
		/// <summary>
		/// 第二档价值上限 (1000 ~ 3000)
		/// </summary>
		private const int Tier2Max = 3000;
		
		/// <summary>
		/// 第三档价值上限 (3000 ~ 6000)
		/// </summary>
		private const int Tier3Max = 6000;
		#endregion

		#region 各档出现概率常量
		/// <summary>
		/// 第一档鱼出现概率 (55%)
		/// </summary>
		private const float ProbTier1 = 0.55f;
		
		/// <summary>
		/// 第二档鱼出现概率 (30%)
		/// </summary>
		private const float ProbTier2 = 0.30f;
		
		/// <summary>
		/// 第三档鱼出现概率 (10%)
		/// </summary>
		private const float ProbTier3 = 0.10f;
		
		/// <summary>
		/// 第四档鱼出现概率 (5%)
		/// </summary>
		private const float ProbTier4 = 0.05f;
		#endregion

		/// <summary>
		/// 所有可用鱼类的完整列表，包含各种价值层次的鱼
		/// </summary>
		private static readonly List<FishDefinition> allFish = new List<FishDefinition>
		{
			new FishDefinition{ displayName = "大头金鱼", typeId = 1123, value = 492 },
			new FishDefinition{ displayName = "棕沙丁鱼", typeId = 1106, value = 497 },
			new FishDefinition{ displayName = "蓝鲭鱼", typeId = 1100, value = 506 },
			new FishDefinition{ displayName = "红金鱼", typeId = 1119, value = 515 },
			new FishDefinition{ displayName = "粉金鱼", typeId = 1114, value = 525 },
			new FishDefinition{ displayName = "红鳍鲷鱼", typeId = 1117, value = 785 },
			new FishDefinition{ displayName = "蓝吊鱼", typeId = 1103, value = 805 },
			new FishDefinition{ displayName = "紫雀鲷鱼", typeId = 1115, value = 814 },
			new FishDefinition{ displayName = "绿刺豚", typeId = 1126, value = 970 },
			new FishDefinition{ displayName = "红九间鱼", typeId = 1118, value = 983 },
			new FishDefinition{ displayName = "蓝雀鲷鱼", typeId = 1098, value = 996 },
			new FishDefinition{ displayName = "白扁鱼", typeId = 1124, value = 1006 },
			new FishDefinition{ displayName = "绿鲷鱼", typeId = 1097, value = 1066 },
			new FishDefinition{ displayName = "白燕鱼", typeId = 1122, value = 1108 },
			new FishDefinition{ displayName = "棕梭鱼", typeId = 1105, value = 1128 },
			new FishDefinition{ displayName = "绿背鳙鱼", typeId = 1109, value = 1578 },
			new FishDefinition{ displayName = "绿胖头鱼", typeId = 1108, value = 1640 },
			new FishDefinition{ displayName = "青南乳鱼", typeId = 1099, value = 1715 },
			new FishDefinition{ displayName = "大眼红鱼", typeId = 1120, value = 1835 },
			new FishDefinition{ displayName = "绿黄鲀", typeId = 1110, value = 2057 },
			new FishDefinition{ displayName = "棕白石鲈", typeId = 1104, value = 2063 },
			new FishDefinition{ displayName = "橙金鳞鱼", typeId = 1113, value = 2979 },
			new FishDefinition{ displayName = "蓝枪鱼", typeId = 1101, value = 3762 },
			new FishDefinition{ displayName = "粉鳍火焰鱼", typeId = 1116, value = 3798 },
			new FishDefinition{ displayName = "黄绿鲷", typeId = 1125, value = 3807 },
			new FishDefinition{ displayName = "蓝旗鱼", typeId = 1102, value = 4107 },
			new FishDefinition{ displayName = "橙青鳍鱼", typeId = 1112, value = 4113 },
			new FishDefinition{ displayName = "蓝猫鲨鱼", typeId = 1111, value = 6084 },
			new FishDefinition{ displayName = "红斑鱼", typeId = 1121, value = 6534 },
			new FishDefinition{ displayName = "黄金鱼", typeId = 1107, value = 12300 },
		};

		#region 分层鱼类列表
		/// <summary>
		/// 第一档鱼类列表 (价值 0 ~ 1000)
		/// </summary>
		private static List<FishDefinition> tier1 = new List<FishDefinition>();
		
		/// <summary>
		/// 第二档鱼类列表 (价值 1000 ~ 3000)
		/// </summary>
		private static List<FishDefinition> tier2 = new List<FishDefinition>();
		
		/// <summary>
		/// 第三档鱼类列表 (价值 3000 ~ 6000)
		/// </summary>
		private static List<FishDefinition> tier3 = new List<FishDefinition>();
		
		/// <summary>
		/// 第四档鱼类列表 (价值 > 6000)
		/// </summary>
		private static List<FishDefinition> tier4 = new List<FishDefinition>();
		
		/// <summary>
		/// 标记分层列表是否已构建的标志位
		/// </summary>
		private static bool tiersBuilt = false;
		#endregion

		/// <summary>
		/// 确保分层列表已构建，延迟初始化模式
		/// 在首次访问时将所有鱼类按价值分配到不同的层级中
		/// </summary>
		private static void EnsureTiers()
		{
			if (tiersBuilt) return;
			
			// 按价值范围将鱼类分配到不同层级
			tier1 = allFish.Where(f => f.value <= Tier1Max).ToList();
			tier2 = allFish.Where(f => f.value > Tier1Max && f.value <= Tier2Max).ToList();
			tier3 = allFish.Where(f => f.value > Tier2Max && f.value <= Tier3Max).ToList();
			tier4 = allFish.Where(f => f.value > Tier3Max).ToList();
			
			tiersBuilt = true;
		}

		/// <summary>
		/// 根据幸运值生成鱼类列表
		/// </summary>
		/// <param name="luck01">幸运值，范围0.0-1.0，影响生成数量</param>
		/// <returns>生成的鱼类定义列表</returns>
		public static List<FishDefinition> GenerateByLuck(float luck01)
		{
			// 确保分层列表已初始化
			EnsureTiers();
			
			// 根据幸运值确定生成数量
			int count = GetFishCountByLuck(luck01);
			var list = new List<FishDefinition>(count);
			
			// 按数量生成鱼
			for (int i = 0; i < count; i++)
			{
				var one = PickByValueProbability();
				if (one != null) list.Add(one);
			}
			return list;
		}

		/// <summary>
		/// 根据幸运值计算应生成的鱼类数量
		/// 幸运值越高，生成的鱼类数量越多
		/// </summary>
		/// <param name="luck01">幸运值，范围0.0-1.0</param>
		/// <returns>应生成的鱼类数量</returns>
		private static int GetFishCountByLuck(float luck01)
		{
			// 幸运值分四档，每档对应不同的数量范围
			if (luck01 < 0.25f) return UnityEngine.Random.Range(2, 4);  // [2,3] - 低幸运值
			if (luck01 < 0.50f) return UnityEngine.Random.Range(3, 5);  // [3,4] - 中低幸运值
			if (luck01 < 0.75f) return UnityEngine.Random.Range(4, 6);  // [4,5] - 中高幸运值
			return 5;  // 高幸运值，固定生成5条
		}

		/// <summary>
		/// 按价值概率随机选择一条鱼
		/// 使用预定义的概率分布来决定从哪个价值层级中选择
		/// </summary>
		/// <returns>选中的鱼类定义，如果没有可用鱼则返回null</returns>
		private static FishDefinition PickByValueProbability()
		{
			float r = UnityEngine.Random.value;
			List<FishDefinition> source;

			// 根据概率分布选择对应的价值层级
			if (r < ProbTier1) source = tier1;  // 55% 概率选择第一档
			else if (r < ProbTier1 + ProbTier2) source = tier2;  // 30% 概率选择第二档
			else if (r < ProbTier1 + ProbTier2 + ProbTier3) source = tier3;  // 10% 概率选择第三档
			else source = tier4;  // 5% 概率选择第四档

			// 安全检查：如果选中的层级为空，则回退到全部鱼类
			if (source == null || source.Count == 0) source = allFish;
			
			// 如果所有鱼类列表也为空，返回null
			if (source.Count == 0) return null;
			
			// 从选中的层级中随机选择一条鱼
			int idx = UnityEngine.Random.Range(0, source.Count);
			return source[idx];
		}
	}
}

