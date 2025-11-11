using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GrenadeFishing.Utils
{
    /// <summary>
    /// 鱼类抽样与生成器（含“爆炸奇迹”机制）
    /// 兼容原先调用：GenerateByLuck(float luck01)
    /// 新增可选参数：grenadeTier OR grenadeCost, totalValueCap, miracleChance, miracleIgnoresCap
    /// 价格将被映射为 costFactor (0..1) 用于微调高价值鱼概率和奇迹概率
    /// </summary>
    public static class FishGenerator
    {
        // 日志模块
        private static readonly GrenadeFishing.Utils.Logger L = GrenadeFishing.Utils.Log.GetLogger();
        // 在类初始化时，由你定义的局部布尔变量控制该文件日志：
        private static bool LocalLogs = true; // 你可以在别处修改这个变量
        static FishGenerator()
        {
            L.SetEnabled(LocalLogs); // 一次设置即可
        }
        
        public class FishDefinition
        {
            public string displayName = string.Empty;
            public int typeId;
            public int value;
        }

        public enum GrenadeTier { D = 0, C = 1, B = 2, A = 3, S = 4 }

        #region 配置（可按需微调）
        private const int Tier1Max = 1000;
        private const int Tier2Max = 3000;
        private const int Tier3Max = 6000;

        // 基础分档概率（低档偏高以保证常见产出，玩家更爽）
        private static readonly float[] BaseTierProbs = new float[] { 0.68f, 0.22f, 0.07f, 0.03f };

        // 原始档位乘子（作为基线），最终会根据 price/costFactor 做动态放缩
        private static readonly Dictionary<GrenadeTier, float[]> TierMultipliers = new Dictionary<GrenadeTier, float[]>
        {
            { GrenadeTier.D, new float[]{ 1.00f, 1.00f, 0.30f, 0.10f } },
            { GrenadeTier.C, new float[]{ 1.00f, 1.05f, 0.60f, 0.20f } },
            { GrenadeTier.B, new float[]{ 0.98f, 1.00f, 1.05f, 1.00f } },
            { GrenadeTier.A, new float[]{ 0.95f, 1.00f, 1.15f, 1.20f } },
            { GrenadeTier.S, new float[]{ 0.90f, 0.98f, 1.20f, 1.45f } },
        };

        // 基础产出成功率（按档位），但我们会基于 costFactor 轻微放大
        private static readonly Dictionary<GrenadeTier, float> PSuccessByTier = new Dictionary<GrenadeTier, float>
        {
            { GrenadeTier.D, 0.02f },
            { GrenadeTier.C, 0.06f },
            { GrenadeTier.B, 0.16f },
            { GrenadeTier.A, 0.28f },
            { GrenadeTier.S, 0.48f }
        };

        private const float DecayBase = 0.8f; // 衰减基数；越小越抑制连爆
        private const float DudChanceByD = 0.10f;
        private const float DudChanceGeneric = 0.01f;

        // 默认奇迹概率（默认 1%）：可在调用时覆盖。
        // 如果使用 cost-driven overload，会根据 costFactor 额外提升（最高约 +2%）
        private const float DefaultMiracleChance = 0.01f;

        // 用于 price -> costFactor 归一化（注意：这里按你提供的手雷价格表选择了 min/max）
        // minCost = 最便宜管状炸药 70； maxCost = 当前表里最贵的电击手雷 390
        private const int PriceMin = 70;
        private const int PriceMax = 390;

        // 对 tier4 （>6000价值）额外加权基数（最终会乘以 costFactor）
        private const float Tier4BaseBoost = 0.012f; // 常规 boost（可微调）
        private const float Tier4CostFactorMultiplier = 0.035f; // 当 costFactor==1 时，额外加成 ~3.5%

        // 保底机制配置键
        private const string KeyHighTierGrenadeCount = "GF_HighTierGrenadeCount"; // 高价/中高价手雷使用计数
        private const string KeyLowTierGrenadeCount = "GF_LowTierGrenadeCount"; // 低价爆炸物使用计数
        #endregion

        /// <summary>
        /// 所有可用鱼类的完整列表
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

        #region 延迟构建分层
        private static bool tiersBuilt = false;
        private static List<FishDefinition> tier1 = new List<FishDefinition>();
        private static List<FishDefinition> tier2 = new List<FishDefinition>();
        private static List<FishDefinition> tier3 = new List<FishDefinition>();
        private static List<FishDefinition> tier4 = new List<FishDefinition>();

        private static void EnsureTiers()
        {
            if (tiersBuilt) return;
            tier1 = allFish.Where(f => f.value <= Tier1Max).ToList();
            tier2 = allFish.Where(f => f.value > Tier1Max && f.value <= Tier2Max).ToList();
            tier3 = allFish.Where(f => f.value > Tier2Max && f.value <= Tier3Max).ToList();
            tier4 = allFish.Where(f => f.value > Tier3Max).ToList();
            tiersBuilt = true;
        }
        #endregion

        #region 兼容旧接口（保留）
        public static List<FishDefinition> GenerateByLuck(float luck01)
        {
            return GenerateByLuck(luck01, GrenadeTier.B, totalValueCap: 0, miracleChance: DefaultMiracleChance, miracleIgnoresCap: false);
        }
        #endregion

        #region 新增：按手雷价格调用的重载（推荐）
        /// <summary>
        /// 新增：直接传入手雷价格（cost）。内部会决定档位并用 costFactor 做额外微调。
        /// 包含保底机制：高价手雷和低价爆炸物的保底触发。
        /// </summary>
        public static List<FishDefinition> GenerateByLuck(float luck01, int grenadeCost, int totalValueCap = 0, float miracleChance = DefaultMiracleChance, bool miracleIgnoresCap = false)
        {
            // 添加调试日志
            L.DebugMsg($"[炸鱼保底调试] GenerateByLuck被调用，手雷价格={grenadeCost}");
            
            // 检查保底机制（在随机生成之前）
            var guaranteedFish = CheckGuaranteedFish(grenadeCost);
            if (guaranteedFish != null)
            {
                // 触发保底：返回保底鱼，跳过随机生成
                L.Info($"[炸鱼保底] 触发保底机制！手雷价格={grenadeCost}，保底鱼={guaranteedFish.displayName}(价值={guaranteedFish.value})");
                return new List<FishDefinition> { guaranteedFish };
            }

            L.DebugMsg($"[炸鱼保底调试] 未触发保底，继续正常生成流程");
            GrenadeTier tier = ChooseTierByCost(grenadeCost);
            float costFactor = ComputeCostFactorFromPrice(grenadeCost); // 0..1
            return GenerateByLuck_Internal(luck01, tier, costFactor, totalValueCap, miracleChance, miracleIgnoresCap);
        }
        #endregion

        #region 原有：按档位调用（向后兼容）
        public static List<FishDefinition> GenerateByLuck(float luck01, GrenadeTier grenadeTier, int totalValueCap = 0, float miracleChance = DefaultMiracleChance, bool miracleIgnoresCap = false)
        {
            // 保持原来行为（成本因子为 0）
            return GenerateByLuck_Internal(luck01, grenadeTier, 0f, totalValueCap, miracleChance, miracleIgnoresCap);
        }
        #endregion

        /// <summary>
        /// 内部实现：增加了 costFactor 参数
        /// </summary>
        private static List<FishDefinition> GenerateByLuck_Internal(float luck01, GrenadeTier grenadeTier, float costFactor, int totalValueCap = 0, float miracleChance = DefaultMiracleChance, bool miracleIgnoresCap = false)
        {
            EnsureTiers();

            int attempts = GetFishCountByLuck(luck01);
            var results = new List<FishDefinition>(attempts);

            float pSuccessBase = PSuccessByTier.ContainsKey(grenadeTier) ? PSuccessByTier[grenadeTier] : 0.12f;
            // 基于价格，适度增加成功率（最高约 +15%）
            float pSuccess = pSuccessBase * (1f + 0.15f * costFactor);

            float dudChance = grenadeTier == GrenadeTier.D ? DudChanceByD : DudChanceGeneric;
            if (UnityEngine.Random.value < dudChance)
            {
                // 哑雷 -> 无产出
                return results;
            }

            // 如果调用方没有显式传入 miracleChance（仍为默认值），则按 costFactor 动态提高奇迹率（最多 +0.02）
            if (Math.Abs(miracleChance - DefaultMiracleChance) < 1e-6f)
            {
                miracleChance = DefaultMiracleChance + (0.02f * costFactor); // 默认 1% -> 1%~3% 随价格增加
            }

            // 计算按炸弹档位与价格修正后的分档概率（归一化）
            float[] adjustedTierProbs = ComputeAdjustedTierProbs(grenadeTier, costFactor);

            int attemptsLeft = attempts;

            // ------------- 奇迹机制 -------------
            if (miracleChance > 0f && UnityEngine.Random.value < miracleChance)
            {
                // 奇迹触发：尝试直接生成一条顶级鱼（tier4 / index 3，对应 value > 6000）
                FishDefinition miracleFish = ChooseFishFromTier(3);
                if (miracleFish != null)
                {
                    if (totalValueCap <= 0 || miracleIgnoresCap || miracleFish.value <= totalValueCap)
                    {
                        results.Add(miracleFish);
                        attemptsLeft = Math.Max(0, attemptsLeft - 1);
                    }
                    else
                    {
                        var alt = TryFindAlternativeUnderCap(miracleFish, 3, totalValueCap);
                        if (alt != null)
                        {
                            results.Add(alt);
                            attemptsLeft = Math.Max(0, attemptsLeft - 1);
                        }
                        // 否则放弃奇迹（保持 attemptsLeft）
                    }
                }
            }
            // ------------------------------------

            int producedCount = results.Count; // 用于计算衰减 (第一条 producedCount=0)
            int usedValue = results.Sum(f => f.value);

            for (int i = 0; i < attemptsLeft; i++)
            {
                // 每次尝试是否产出
                if (UnityEngine.Random.value > pSuccess) continue;

                // 衰减基于已生成的数量 producedCount（而不是尝试次数），保证第 n 条生成时高档概率被削弱
                float decay = Mathf.Pow(DecayBase, producedCount);

                float[] probsForThis = new float[4];
                for (int t = 0; t < 4; t++)
                {
                    float baseP = adjustedTierProbs[t];
                    if (t == 0)
                    {
                        // 低档补偿，避免所有概率压低到 0
                        probsForThis[t] = baseP + (1 - decay) * 0.08f;
                    }
                    else
                    {
                        probsForThis[t] = baseP * decay;
                    }
                }
                NormalizeInPlace(probsForThis);

                int chosenTierIndex = RollIndexByProb(probsForThis);
                FishDefinition chosen = ChooseFishFromTier(chosenTierIndex);
                if (chosen == null) continue;

                // 检查总价值上限
                if (totalValueCap > 0 && usedValue + chosen.value > totalValueCap)
                {
                    FishDefinition alt = TryFindAlternativeUnderCap(chosen, chosenTierIndex, totalValueCap - usedValue);
                    if (alt != null)
                    {
                        chosen = alt;
                    }
                    else
                    {
                        // 无可放置替代则跳过此产出
                        continue;
                    }
                }

                results.Add(chosen);
                usedValue += chosen.value;
                producedCount++;
            }

            return results;
        }

        #region 内部工具
        private static int GetFishCountByLuck(float luck01)
        {
            if (luck01 < 0.25f) return UnityEngine.Random.Range(2, 4);  // [2,3]
            if (luck01 < 0.50f) return UnityEngine.Random.Range(3, 5);  // [3,4]
            if (luck01 < 0.75f) return UnityEngine.Random.Range(4, 6);  // [4,5]
            return 5;
        }

        /// <summary>
        /// 计算按档位与价格修正后的分档概率
        /// - costFactor: 0..1（来自价格）
        /// - 对 tier4 (index 3) 额外给与小幅加权，随 costFactor 增强
        /// </summary>
        private static float[] ComputeAdjustedTierProbs(GrenadeTier tier, float costFactor)
        {
            float[] mult = TierMultipliers.ContainsKey(tier) ? TierMultipliers[tier] : TierMultipliers[GrenadeTier.B];
            float[] outp = new float[4];
            for (int i = 0; i < 4; i++) outp[i] = BaseTierProbs[i] * mult[i];

            // 对 tier4 (index 3) 加入基于价格的附加权重（目的是小幅提高 >6000 的出现概率）
            float tier4Boost = Tier4BaseBoost + Tier4CostFactorMultiplier * costFactor;
            outp[3] += tier4Boost;

            // 为了避免高档太偏，按 costFactor 缓慢削弱低档权重（但不能太狠）
            float lowTierReduction = 0.06f * costFactor; // costFactor==1 时减少 0.06 给低档
            outp[0] = Mathf.Max(0f, outp[0] - lowTierReduction);

            NormalizeInPlace(outp);
            return outp;
        }

        private static void NormalizeInPlace(float[] arr)
        {
            float s = 0f;
            for (int i = 0; i < arr.Length; i++) s += arr[i];
            if (s <= 0f)
            {
                float v = 1f / arr.Length;
                for (int i = 0; i < arr.Length; i++) arr[i] = v;
                return;
            }
            for (int i = 0; i < arr.Length; i++) arr[i] /= s;
        }

        private static int RollIndexByProb(float[] probs)
        {
            float r = UnityEngine.Random.value;
            float acc = 0f;
            for (int i = 0; i < probs.Length; i++)
            {
                acc += probs[i];
                if (r <= acc) return i;
            }
            return probs.Length - 1;
        }

        private static FishDefinition ChooseFishFromTier(int tierIndex)
        {
            List<FishDefinition> source = GetTierListByIndex(tierIndex);
            if (source == null || source.Count == 0) return null;

            // 采样权重：原来使用 sqrt(value)。为小幅提高大于6000 的出现率，
            // 我做了两点修改：1) 使用 value^0.45 让大鱼略更吸引（但不是线性翻天）；
            // 2) 若 fish.value > 6000 则额外乘以一个微增因子（1.18），进一步提升黄金鱼等的权重。
            double totalW = 0.0;
            var weights = new double[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                double baseW = Math.Pow(Math.Max(1, source[i].value), 0.45); // 0.45 指数是经验性微调
                if (source[i].value > 6000)
                {
                    baseW *= 1.18; // 对于 >6000 的鱼再给一点倾斜奖励（可调）
                }
                weights[i] = baseW;
                totalW += weights[i];
            }

            double pick = UnityEngine.Random.value * totalW;
            double c = 0.0;
            for (int i = 0; i < source.Count; i++)
            {
                c += weights[i];
                if (pick <= c) return source[i];
            }
            return source.Last();
        }

        private static List<FishDefinition> GetTierListByIndex(int idx)
        {
            switch (idx)
            {
                case 0: return tier1;
                case 1: return tier2;
                case 2: return tier3;
                case 3: return tier4;
                default: return null;
            }
        }

        private static FishDefinition TryFindAlternativeUnderCap(FishDefinition original, int chosenTierIndex, int remainingCap)
        {
            if (remainingCap <= 0) return null;

            // 同档中选最接近 remainingCap 的鱼
            var same = GetTierListByIndex(chosenTierIndex)?.Where(f => f.value <= remainingCap).OrderByDescending(f => f.value).ToList();
            if (same != null && same.Count > 0) return same.First();

            // 向下档查找
            for (int down = chosenTierIndex - 1; down >= 0; down--)
            {
                var list = GetTierListByIndex(down)?.Where(f => f.value <= remainingCap).OrderByDescending(f => f.value).ToList();
                if (list != null && list.Count > 0) return list.First();
            }
            return null;
        }
        #endregion

        #region 保底机制

        // 内存缓存，避免存储模块临时异常导致值被重置
        private static readonly Dictionary<string, int> counterCache = new Dictionary<string, int>();

        /// <summary>
        /// 检查是否触发保底机制，如果触发则返回保底鱼，否则返回 null
        /// 说明：
        ///  - ChooseFishFromTier(3) 中的 3 是索引 3（0..3 共 4 档），即第 4 档（value > 6000）
        ///  - 使用 ChooseTierByCost 判断是“高价/低价”分支，避免直接用魔数 50
        /// </summary>
        private static FishDefinition CheckGuaranteedFish(int grenadeCost)
        {
            GrenadeTier tier = ChooseTierByCost(grenadeCost);
            bool isHighTierGrenade = tier >= GrenadeTier.B; // B/A/S 视为较高投入

            if (isHighTierGrenade)
            {
                L.DebugMsg($"[炸鱼保底调试] 手雷被归为中高价档（{tier}），检查高价手雷保底");
                int count = GetOrInitCounter(KeyHighTierGrenadeCount);
                L.DebugMsg($"[炸鱼保底调试] 高价手雷当前使用次数={count}");
                count++;
                SaveCounter(KeyHighTierGrenadeCount, count);
                L.DebugMsg($"[炸鱼保底调试] 保存后高价手雷使用次数={count}");

                // 100 的倍数：掉落一条 index 3（第4档，>6000）的鱼（优先）
                if (count % 100 == 0)
                {
                    L.DebugMsg($"[炸鱼保底调试] 检查100的倍数保底，count={count}");
                    var fish = ChooseFishFromTier(3); // index 3 = tier4 (>6000)
                    if (fish != null)
                    {
                        L.Info($"[炸鱼保底] 高价手雷保底触发！使用次数={count}（100的倍数），保底鱼={fish.displayName}(价值={fish.value})");
                        return fish;
                    }
                }

                // 50 的倍数（但非 100 的倍数）：掉落一条价格在 3000 到 6000 之间的鱼
                if (count % 50 == 0 && count % 100 != 0)
                {
                    L.DebugMsg($"[炸鱼保底调试] 检查50的倍数保底，count={count}");
                    var fish = ChooseFishFromValueRange(3000, 6000);
                    if (fish != null)
                    {
                        L.Info($"[炸鱼保底] 高价手雷保底触发！使用次数={count}（50的倍数），保底鱼={fish.displayName}(价值={fish.value})");
                        return fish;
                    }
                }
            }
            else
            {
                L.DebugMsg($"[炸鱼保底调试] 手雷被归为低价档（{tier}），检查低价爆炸物保底");
                int count = GetOrInitCounter(KeyLowTierGrenadeCount);
                L.DebugMsg($"[炸鱼保底调试] 低价爆炸物当前使用次数={count}");
                count++;
                SaveCounter(KeyLowTierGrenadeCount, count);
                L.DebugMsg($"[炸鱼保底调试] 保存后低价爆炸物使用次数={count}");

                // >114 次：直接给一条全表中最贵的鱼（保底剧烈放大）
                if (count > 114)
                {
                    L.DebugMsg($"[炸鱼保底调试] 检查114次保底，次数count={count}");
                    var fish = allFish.OrderByDescending(f => f.value).FirstOrDefault();
                    if (fish != null)
                    {
                        L.Info($"[炸鱼保底] 低价爆炸物保底触发！使用次数={count}（>114次），保底鱼={fish.displayName}(价值={fish.value})");
                        return fish;
                    }
                }

                // 51 的倍数：给一条 index 3（第4档，>6000）的鱼
                if (count % 51 == 0)
                {
                    L.DebugMsg($"[炸鱼保底调试] 检查51的倍数保底，count={count}");
                    var fish = ChooseFishFromTier(3); // index 3 = tier4 (>6000)
                    if (fish != null)
                    {
                        L.Info($"[炸鱼保底] 低价爆炸物保底触发！使用次数={count}（51的倍数），保底鱼={fish.displayName}(价值={fish.value})");
                        return fish;
                    }
                }

                // 20 的倍数：给一条 1000 块以下的鱼
                if (count % 20 == 0)
                {
                    L.DebugMsg($"[炸鱼保底调试] 检查20的倍数保底，count={count}");
                    var fish = ChooseFishFromValueRange(0, 1000);
                    if (fish != null)
                    {
                        L.Info($"[炸鱼保底] 低价爆炸物保底触发！使用次数={count}（20的倍数），保底鱼={fish.displayName}(价值={fish.value})");
                        return fish;
                    }
                }
            }

            L.DebugMsg($"[炸鱼保底调试] 未触发任何保底，返回null");
            return null;
        }

        /// <summary>
        /// 从指定价值范围内选择一条鱼
        /// </summary>
        private static FishDefinition ChooseFishFromValueRange(int minValue, int maxValue)
        {
            var candidates = allFish.Where(f => f.value >= minValue && f.value <= maxValue).ToList();
            if (candidates.Count == 0) return null;

            // 使用价值加权采样（价值越高权重越大）
            double totalW = 0.0;
            var weights = new double[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                double baseW = Math.Pow(Math.Max(1, candidates[i].value), 0.45);
                weights[i] = baseW;
                totalW += weights[i];
            }

            double pick = UnityEngine.Random.value * totalW;
            double c = 0.0;
            for (int i = 0; i < candidates.Count; i++)
            {
                c += weights[i];
                if (pick <= c) return candidates[i];
            }
            return candidates.Last();
        }

        /// <summary>
        /// 获取或初始化计数器
        /// 安全策略：
        ///  - 优先使用 GuaranteedFishStorage 获取值，成功后更新内存缓存并返回
        ///  - 若存储读取失败（抛异常），则尝试返回内存缓存中的值（若存在）
        ///  - 仅在两者都不可用时返回 0，但不会尝试把 0 写回远端存储以避免覆盖已有数据
        /// </summary>
        private static int GetOrInitCounter(string key)
        {
            L.DebugMsg($"[炸鱼保底调试] GetOrInitCounter被调用，key={key}");
            
            try
            {
                int value = GuaranteedFishStorage.GetCounter(key, 0);
                // 更新内存缓存
                lock (counterCache)
                {
                    counterCache[key] = value;
                }
                L.DebugMsg($"[炸鱼保底调试] 使用JSON存储读取计数器成功，key={key}, value={value}");
                return value;
            }
            catch (Exception ex)
            {
                L.Warn($"[炸鱼保底] JSON存储读取失败（key={key}）：{ex.Message}", ex);
                lock (counterCache)
                {
                    if (counterCache.TryGetValue(key, out int cached))
                    {
                        L.DebugMsg($"[炸鱼保底调试] 使用内存缓存计数器值，key={key}, value={cached}");
                        return cached;
                    }
                }
                L.Warn($"[炸鱼保底] 读取失败且无缓存，返回默认0（不会写回远端以免覆盖）。key={key}");
                return 0;
            }
        }

        /// <summary>
        /// 保存计数器
        /// 安全策略：
        ///  - 优先尝试写入 GuaranteedFishStorage
        ///  - 若写入失败，仍在内存缓存中更新值以免当前会话丢失计数
        /// </summary>
        private static void SaveCounter(string key, int value)
        {
            L.DebugMsg($"[炸鱼保底调试] SaveCounter被调用，key={key}, value={value}");
            // 先更新内存缓存（保证会话内不丢失）
            lock (counterCache)
            {
                counterCache[key] = value;
            }

            try
            {
                GuaranteedFishStorage.SetCounter(key, value);
                L.DebugMsg($"[炸鱼保底调试] 使用JSON存储保存计数器成功，key={key}, value={value}");
            }
            catch (Exception ex)
            {
                L.Warn($"[炸鱼保底] JSON存储保存失败（key={key}, value={value}）：{ex.Message}", ex);
                L.Warn($"[炸鱼保底] 已在内存缓存中更新计数，待下次存储可再同步。key={key}, cachedValue={value}");
            }
        }
        #endregion

        #region 辅助：cost -> tier 与 costFactor 映射
        /// <summary>
        /// 基于 PriceMin/PriceMax 的动态分档（避免硬编码魔数并保证 S 档在实际价格表中可达）
        /// 规则（可调整）：
        /// - cost <= PriceMin : D
        /// - cost 在 PriceMin..PriceMax 中，根据区间百分位落到 C/B/A/S（按 25%/25%/40%/10% 划分）
        /// 目的是让 PriceMax 对应到 S（如果价格达到或超过 PriceMax）
        /// </summary>
        public static GrenadeTier ChooseTierByCost(int cost)
        {
            if (cost <= PriceMin) return GrenadeTier.D;

            if (PriceMax <= PriceMin) return GrenadeTier.B; // 退化保护

            float p = Mathf.InverseLerp(PriceMin, PriceMax, cost); // 0..1
            if (p <= 0.25f) return GrenadeTier.C;
            if (p <= 0.50f) return GrenadeTier.B;
            if (p <= 0.90f) return GrenadeTier.A; // 40% 段
            return GrenadeTier.S; // top 10%（或 cost > PriceMax）
        }

        /// <summary>
        /// 将实际价格归一化为 costFactor（0..1），基于 PriceMin..PriceMax
        /// 用于驱动高档概率及奇迹概率等
        /// </summary>
        private static float ComputeCostFactorFromPrice(int cost)
        {
            if (PriceMax <= PriceMin) return 0f;
            float cf = Mathf.InverseLerp(PriceMin, PriceMax, cost); // 0..1
            // 稍微做个非线性压缩，让中高价的提升更显著（平方根）
            cf = Mathf.Sqrt(cf);
            cf = Mathf.Clamp01(cf);
            return cf;
        }
        #endregion
    }
}
