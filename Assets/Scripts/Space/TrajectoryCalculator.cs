using UnityEngine;
using SpaceLogistics.Core;
using System;

namespace SpaceLogistics.Space
{
    public static class TrajectoryCalculator
    {
        /// <summary>
        /// ホーマン遷移軌道を計算する。
        /// </summary>
        /// <param name="originBody">出発天体（または現在の軌道の中心天体）</param>
        /// <param name="r1">出発軌道半径</param>
        /// <param name="r2">目標軌道半径</param>
        /// <param name="startTime">出発時刻</param>
        /// <returns>遷移軌道 (KeplerOrbit) と所要時間</returns>
        public static (KeplerOrbit orbit, double duration) CalculateHohmannTransfer(
            CelestialBody centerBody, 
            double r1, 
            double r2, 
            double startTime)
        {
            // μ = GM
            double mu = PhysicsConstants.GameGravitationalConstant * centerBody.Mass.Kilograms;

            // 遷移軌道の長半径 a_transfer = (r1 + r2) / 2
            double a_transfer = (r1 + r2) / 2.0;

            // 遷移時間 T = pi * sqrt(a^3 / mu)
            double duration = Math.PI * Math.Sqrt(Math.Pow(a_transfer, 3) / mu);

            // 離心率 e = (r2 - r1) / (r2 + r1) ... 絶対値
            // r_p (近点) が小さい方
            double e = Math.Abs(r2 - r1) / (r1 + r2);

            // 近点引数と平均近点角の設定
            // r1 < r2 (外へ): 出発点は近点 (True Anomaly = 0)
            // r1 > r2 (内へ): 出発点は遠点 (True Anomaly = PI) -> Mean Anomaly = PI
            
            double M0 = (r1 < r2) ? 0.0 : Math.PI;
            // 近点引数は、出発地点の角度に合わせる必要があるが、
            // ここでは簡易的に「出発地点を近点(0度)」とする座標系で作る。
            // 実際は出発地点の位相に合わせて w を回転させる必要がある。
            // いったん w = 0 として返す。呼び出し元で回転させるか、
            // 引数に「出発角度」を含めるのが良い。
            
            // 今回は引数に出発角度がないので、呼び出し元で調整することを想定し、
            // 標準的な形状(X軸+方向が出発)で返す。
            
            OrbitParameters paramsData = new OrbitParameters
            {
                SemiMajorAxis = a_transfer,
                Eccentricity = e,
                Inclination = 0,
                ArgumentOfPeriapsis = 0, // 仮
                MeanAnomalyAtEpoch = M0,
                // Mean Motion n = sqrt(mu / a^3)
                MeanMotion = Math.Sqrt(mu / Math.Pow(a_transfer, 3))
            };

            KeplerOrbit orbit = new KeplerOrbit(centerBody, paramsData, startTime, startTime + duration);
            return (orbit, duration);
        }

        // 将来的には会合調整(Intercept)などもここに追加
    }
}
