using UnityEngine;
using SpaceLogistics.Core;
using System;

namespace SpaceLogistics.Space
{
    public static class TrajectoryCalculator
    {
        public struct TransferResult
        {
            public KeplerOrbit orbit;
            public double duration;
            public double deltaV1;
            public double deltaV2;
        }

        /// <summary>
        /// 単純な円軌道間のホーマン遷移を計算する。
        /// </summary>
        public static TransferResult CalculateHohmannTransfer(CelestialBody origin, double r1, double r2, double startTime, double argumentOfPeriapsis = 0.0)
        {
            // μ = GM
            double mu = PhysicsConstants.GameGravitationalConstant * origin.Mass.Kilograms;

            // 1. Semi-major axis of transfer orbit
            double a_trans = (r1 + r2) / 2.0;

            // 2. Duration (half period)
            double T_trans = Math.PI * Math.Sqrt(Math.Pow(a_trans, 3) / mu);

            // 3. Velocities
            double v1 = Math.Sqrt(mu / r1); // Circular velocity at r1
            double v2 = Math.Sqrt(mu / r2); // Circular velocity at r2

            double v_p = Math.Sqrt(mu * (2.0 / r1 - 1.0 / a_trans)); // Velocity at periapsis (transfer)
            double v_a = Math.Sqrt(mu * (2.0 / r2 - 1.0 / a_trans)); // Velocity at apoapsis (transfer)

            // Delta V
            double dv1 = v_p - v1; // Burn at r1
            double dv2 = v2 - v_a; // Burn at r2 (to circularize)

            // Create Orbit
            OrbitParameters paramsData = new OrbitParameters
            {
                SemiMajorAxis = a_trans,
                Eccentricity = Math.Abs(r2 - r1) / (r1 + r2),
                Inclination = 0.0,
                ArgumentOfPeriapsis = argumentOfPeriapsis,
                LongitudeOfAscendingNode = 0.0,
                MeanMotion = Math.Sqrt(mu / Math.Pow(a_trans, 3)),
                MeanAnomalyAtEpoch = 0.0 // Default 0
            };
            
            if (r1 > r2)
            {
                // 内側への遷移
                paramsData.ArgumentOfPeriapsis = 180.0; // Apogee start
                paramsData.MeanAnomalyAtEpoch = 180.0; 
            }

            KeplerOrbit orbit = new KeplerOrbit(origin, paramsData, startTime, startTime + T_trans);
            
            return new TransferResult
            {
                orbit = orbit,
                duration = T_trans,
                deltaV1 = dv1,
                deltaV2 = dv2
            };
        }

        /// <summary>
        /// 次の最適な発射ウィンドウ（ランチウィンドウ）を計算する。
        /// </summary>
        /// <param name="originBody">出発天体（例：地球）</param>
        /// <param name="targetBody">目標天体（例：月）</param>
        /// <param name="transferDuration">遷移にかかる時間</param>
        /// <param name="currentTime">現在時刻</param>
        /// <returns>発射すべき時刻 (Universe Time)</returns>
        public static double FindNextLaunchWindow(CelestialBody originBody, CelestialBody targetBody, double transferDuration, double currentTime)
        {
            // 親天体が同じ、または親子関係の遷移を想定
            
            // 目標天体の角速度 (rad/s)
            double n_target = targetBody.OrbitData.MeanMotion;
            
            // 遷移時間中に進む角度
            double angle_travelled = n_target * transferDuration;
            
            // 到着時に、対象天体は「ロケットの正反対(180度, PI)」にいる必要がある（外側への遷移の基本）。
            // 内側への遷移の場合も、会合点としての位相差を計算する必要があるが、
            // 簡略化のため「Hohmann Transferの到着点がちょうど天体の位置になる」条件を使う。
            
            // 目標位相角 (Launch時の Target - Origin の角度差)
            // 外側へ行く場合: 出発時、ターゲットは到着時より angle_travelled だけ手前にいる。
            // 到着時、ロケットは apogee (PI)。ターゲットも PI にいてほしい。
            // よって Launch 時のターゲット角度は PI - angle_travelled。
            
            double target_angle_at_launch = Math.PI - angle_travelled;
            
            // 現在のターゲットの角度 (Mean Anomaly)
            double M0 = targetBody.OrbitData.MeanAnomalyAtEpoch * Mathf.Deg2Rad;
            double n = n_target;
            
            // M(t) = M0 + n * t
            // これが target_angle_at_launch (mod 2PI) になる t を探す。
            
            target_angle_at_launch = NormalizeAngle(target_angle_at_launch);
            double currentM = NormalizeAngle(M0 + n * currentTime);
            
            double deltaM = target_angle_at_launch - currentM;
            if (deltaM < 0) deltaM += 2.0 * Math.PI;
            
            double waitTime = deltaM / n;
            
            return currentTime + waitTime;
        }

        private static double NormalizeAngle(double angle)
        {
            angle = angle % (2.0 * Math.PI);
            if (angle < 0) angle += 2.0 * Math.PI;
            return angle;
        }
    }
}
