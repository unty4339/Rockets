using System.Collections.Generic;
using UnityEngine;

namespace SpaceLogistics.Rocketry
{
    /// <summary>
    /// ロケットの1ステージ（段）を表すクラス。
    /// 複数のパーツリストを保持する。
    /// </summary>
    [System.Serializable]
    public class RocketStage
    {
        public int StageIndex;
        public List<RocketPart> Parts = new List<RocketPart>();

        /// <summary>
        /// ステージの満タン時の総質量を取得する。
        /// </summary>
        public float GetStageMassFull()
        {
            float m = 0;
            foreach (var p in Parts)
            {
                m += p.MassDry;
                if (p.Type == PartType.FuelTank) m += p.FuelCapacity;
            }
            return m;
        }

        /// <summary>
        /// ステージの乾燥重量（燃料なし）を取得する。
        /// </summary>
        public float GetStageMassDry()
        {
            float m = 0;
            foreach (var p in Parts)
            {
                m += p.MassDry;
            }
            return m;
        }

        /// <summary>
        /// ステージの合計推力を取得する。
        /// </summary>
        public float GetStageThrust()
        {
            float t = 0;
            foreach (var p in Parts)
            {
                if (p.Type == PartType.Engine) t += p.Thrust;
            }
            return t;
        }

        /// <summary>
        /// ステージの平均比推力を取得する。
        /// </summary>
        public float GetAverageIsp()
        {
            // 推力による加重平均で算出する
            // 式: TotalThrust / Sum(Thrust_i / Isp_i)
            float numerator = 0;
            float denominator = 0;

            foreach (var p in Parts)
            {
                if (p.Type == PartType.Engine && p.Thrust > 0)
                {
                    numerator += p.Thrust;
                    denominator += (p.Thrust / p.Isp);
                }
            }

            if (denominator == 0) return 0;
            return numerator / denominator;
        }
    }

    /// <summary>
    /// ロケット全体の設計図クラス。
    /// 複数のステージを積み重ねて構成される。
    /// </summary>
    [System.Serializable]
    public class RocketBlueprint
    {
        public string DesignName = "Untitled Rocket";
        public List<RocketStage> Stages = new List<RocketStage>();

        /// <summary>
        /// ロケット全体の統計情報（Delta V, TWRなど）を計算する。
        /// 単純な直列ステージング（Serial Staging）を前提とする。
        /// </summary>
        public RocketStats CalculateTotalStats()
        {
            RocketStats stats = new RocketStats();
            
            float currentMass = 0;
            // 総質量を計算
            foreach (var stage in Stages)
            {
                currentMass += stage.GetStageMassFull();
            }
            stats.TotalMass = currentMass;

            // 各ステージを下から順に計算して積算していく
            // Stages[0] が最下段（最初に点火）とする
            
            float totalDeltaV = 0;
            float currentStackMass = stats.TotalMass;
            
            foreach (var stage in Stages)
            {
                float stageThrust = stage.GetStageThrust();
                float stageIsp = stage.GetAverageIsp();
                float stageFuelMass = stage.GetStageMassFull() - stage.GetStageMassDry();
                
                // エンジンと燃料がある場合のみ計算
                if (stageThrust > 0 && stageFuelMass > 0)
                {
                    float massInitial = currentStackMass;
                    float massFinal = currentStackMass - stageFuelMass; // 燃料を使い切ると仮定
                    
                    // ツィオルコフスキーの公式: dV = Isp * g0 * ln(m0 / m1)
                    float stageDv = stageIsp * 9.81f * Mathf.Log(massInitial / massFinal);
                    totalDeltaV += stageDv;
                }
                
                // 次のステージ計算のために、このステージ全体の質量を減算する
                // （燃焼終了後にステージごと切り離されると仮定）
                currentStackMass -= stage.GetStageMassFull(); 
            }

            stats.DeltaV = totalDeltaV;

            // 離陸時のTWR計算
            if (Stages.Count > 0)
            {
                float thrust0 = Stages[0].GetStageThrust();
                if (stats.TotalMass > 0)
                    stats.TWR_Surface = thrust0 / (stats.TotalMass * 9.81f);
            }

            return stats;
        }
    }
}
