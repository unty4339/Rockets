using System;
using UnityEngine;

namespace SpaceLogistics.Rocketry
{
    public static class OrbitalMath
    {
        /// <summary>
        /// ケプラー方程式 M = E - e sin E を解いて、平均近点角 M から 離心近点角 E を求める。
        /// ニュートン・ラフソン法を使用。
        /// </summary>
        public static double SolveKepler(double meanAnomaly, double eccentricity, int maxIter = 100, double epsilon = 1e-6)
        {
            // M を 0 ~ 2PI に正規化
            meanAnomaly = meanAnomaly % (2.0 * Math.PI);
            if (meanAnomaly < 0) meanAnomaly += 2.0 * Math.PI;

            // 初期推定 (eが小さい前提で M に近い)
            double E = meanAnomaly;
            if (eccentricity > 0.8) E = Math.PI; // 高離心率用

            for (int i = 0; i < maxIter; i++)
            {
                double nextE = E - (E - eccentricity * Math.Sin(E) - meanAnomaly) / (1.0 - eccentricity * Math.Cos(E));
                if (Math.Abs(nextE - E) < epsilon)
                {
                    return nextE;
                }
                E = nextE;
            }
            return E;
        }

        /// <summary>
        /// 離心近点角 E と 離心率 e から 真近点角 nu を計算する。
        /// </summary>
        public static double EccentricToTrueAnomaly(double E, double e)
        {
            // tan(nu/2) = sqrt((1+e)/(1-e)) * tan(E/2)
            double sqrtTerm = Math.Sqrt((1 + e) / (1 - e));
            double tanNu2 = sqrtTerm * Math.Tan(E / 2.0);
            return 2.0 * Math.Atan(tanNu2);
        }

        /// <summary>
        /// アポジー会合仮定に基づき、主星(Primary)からの遠地点距離(ra)を与えたときの、
        /// ターゲット天体(Moon)周回の近点距離(rp)を計算する。
        /// </summary>
        /// <param name="r_apogee_transfer">遷移軌道の遠地点距離 (探索変数)</param>
        /// <param name="r_park_primary">主星周回の駐機軌道半径 (近地点距離)</param>
        /// <param name="r_target_dist">主星からターゲット天体までの距離</param>
        /// <param name="r_target_soi">ターゲット天体のSOI半径</param>
        /// <param name="mu_primary">主星の重力定数 (GM)</param>
        /// <param name="mu_target">ターゲット天体の重力定数 (GM)</param>
        /// <returns>ターゲット天体中心からの近点距離</returns>
        public static double CalculateTargetPeriapsisFromTransferApogee(
            double r_apogee_transfer,
            double r_park_primary,
            double r_target_dist,
            double r_target_soi,
            double mu_primary,
            double mu_target
        )
        {
            // 1. 幾何学的配置の計算 (余弦定理)
            // 三角形: 主星(Origin) - ターゲット(Moon) - 宇宙船(Ship)
            // 辺の長さ: r_target_dist, r_apogee_transfer, r_target_soi
            
            // cos(phi) = (r_target^2 + r_apogee^2 - r_soi^2) / (2 * r_target * r_apogee)
            double numerator = r_target_dist * r_target_dist + r_apogee_transfer * r_apogee_transfer - r_target_soi * r_target_soi;
            double denominator = 2 * r_target_dist * r_apogee_transfer;
            double cosPhi = numerator / denominator;
            
            // 三角形が成立しない（届いていない or 遠すぎる）場合はNaNを返す
            if (cosPhi < -1.0 || cosPhi > 1.0) return double.NaN;

            double phi = Math.Acos(cosPhi); // 主星から見た、ターゲットと宇宙船の角度差

            // 2. 速度ベクトルの計算 (主星中心慣性系)
            
            // A. ターゲット天体の速度 (円軌道近似)
            double v_target_mag = Math.Sqrt(mu_primary / r_target_dist);
            Vector2 v_target = new Vector2(0, (float)v_target_mag);

            // B. 宇宙船の速度 (楕円軌道の遠地点)
            // 活力の式: v = sqrt(mu * (2/r - 1/a))
            double a_transfer = (r_park_primary + r_apogee_transfer) / 2.0;
            double v_ship_mag = Math.Sqrt(mu_primary * (2.0 / r_apogee_transfer - 1.0 / a_transfer));

            // 宇宙船の位置: ターゲットから角度phiずれている
            // Pos = (ra * cos(phi), ra * sin(phi))
            // Vel = (-v * sin(phi), v * cos(phi))  <- 位置ベクトルに垂直(遠地点のため)
            Vector2 v_ship = new Vector2(
                (float)(-v_ship_mag * Math.Sin(phi)),
                (float)(v_ship_mag * Math.Cos(phi))
            );

            // 3. 相対速度 (ターゲットから見た速度)
            Vector2 v_rel = v_ship - v_target;
            double v_inf_sq = v_rel.sqrMagnitude; // v_infinity^2

            // 4. ターゲット周回の近点距離(rp)の計算
            
            // ターゲットから見た位置ベクトル (大きさは r_soi)
            // Pos_target = (r_target, 0)
            // Pos_ship   = (ra * cos(phi), ra * sin(phi))
            Vector2 pos_rel = new Vector2(
                (float)(r_apogee_transfer * Math.Cos(phi) - r_target_dist),
                (float)(r_apogee_transfer * Math.Sin(phi))
            );

            // 比角運動量 h = r x v (2D外積)
            double h = pos_rel.x * v_rel.y - pos_rel.y * v_rel.x;
            double h_sq = h * h;

            // 比エネルギー E = v^2/2 - mu/r
            double specificEnergy = v_inf_sq / 2.0 - mu_target / r_target_soi;

            // 離心率 e = sqrt(1 + 2Eh^2/mu^2)
            double term = (2.0 * specificEnergy * h_sq) / (mu_target * mu_target);
            double e_hyp = Math.Sqrt(1.0 + term);

            // 近点距離: rp = (h^2/mu) / (1+e)
            double rp = (h_sq / mu_target) / (1.0 + e_hyp);

            return rp;
        }

        /// <summary>
        /// 指定した衛星周回の近点高度を実現する、遷移軌道の遠地点半径(ra)を探索する (二分法)
        /// 【修正版】反時計回り(Prograde)になるよう、月の軌道の内側(ra < r_moon)を探索します。
        /// </summary>
        public static double FindOptimalApogeeRadiusForMoonTransfer(
            double targetPeriapsisRadius, // 目標とする衛星の近点半径 (r_target_park)
            double r_park_primary,        // 主星の駐機軌道半径
            double r_target_dist,         // 主星-衛星間の距離
            double r_target_soi,          // 衛星のSOI半径
            double mu_primary,            // 主星のGM
            double mu_target,             // 衛星のGM
            int maxIterations = 30
        )
        {
            // 探索範囲の設定 (Prograde軌道を狙うため、月の内側のみを探索)
            // 下限: SOI境界が届くギリギリ手前
            // 上限: 月の軌道半径そのもの (ここが最もrpが小さくなる=衝突コース)
            double min_ra = r_target_dist - r_target_soi * 0.99; 
            double max_ra = r_target_dist; 

            for (int i = 0; i < maxIterations; i++)
            {
                double mid_ra = (min_ra + max_ra) / 2.0;
                
                double calculated_rp = CalculateTargetPeriapsisFromTransferApogee(
                    mid_ra, r_park_primary, r_target_dist, r_target_soi, mu_primary, mu_target
                );

                if (double.IsNaN(calculated_rp))
                {
                    // 三角形不成立 (通常はraが小さすぎてSOIに届いていない)
                    // 内側を広げる必要があるので、minを上げる
                    min_ra = mid_ra;
                    continue;
                }

                // 二分法の更新ルール (Prograde/内側領域の場合)
                // ra が増える(月に近づく) -> 相対位置ベクトルのずれが減る -> rp が小さくなる
                // つまり「単調減少関数」として扱います。

                if (calculated_rp < targetPeriapsisRadius)
                {
                    // 近すぎる(衝突コース寄り) -> 離れる必要がある
                    // 内側領域なので、raを小さくする(月から遠ざける)
                    max_ra = mid_ra;
                }
                else
                {
                    // 遠すぎる -> 近づける必要がある
                    // 内側領域なので、raを大きくする(月に近づける)
                    min_ra = mid_ra;
                }

                // 収束判定
                if (Math.Abs(calculated_rp - targetPeriapsisRadius) < 100.0)
                {
                    return mid_ra;
                }
            }

            return (min_ra + max_ra) / 2.0;
        }

        /// <summary>
        /// 楕円軌道において、近点(Periapsis)から指定した半径(r)に到達するまでの時間を計算する。
        /// </summary>
        public static double CalculateTimeFromPeriapsisToRadius(double a, double e, double mu, double r_target)
        {
            // 半径rから離心近点角Eを逆算
            // r = a(1 - e * cos E)  =>  cos E = (1 - r/a) / e
            // 注意: eが0に近い場合や、rが近点・遠点範囲外の場合の保護が必要
            double cosE = (1.0 - r_target / a) / e;
            cosE = Math.Clamp(cosE, -1.0, 1.0);

            // 近点から遠点に向かう(0 -> PI)と仮定してEを求める
            double E = Math.Acos(cosE);

            // ケプラー方程式: M = E - e * sin E
            double M = E - e * Math.Sin(E);

            // 平均運動 n = sqrt(mu / a^3)
            double n = Math.Sqrt(mu / Math.Pow(a, 3.0));

            // 時間 t = M / n
            return M / n;
        }

        /// <summary>
        /// 双曲線軌道において、SOI境界(半径r)から近点(Periapsis)までの所要時間を計算する。
        /// </summary>
        /// <param name="a">軌道長半径 (a)</param>
        /// <param name="e">離心率 (e > 1)</param>
        /// <param name="mu">重力定数</param>
        /// <param name="r_boundary">SOI境界半径</param>
        /// <returns>秒単位の時間</returns>
        public static double CalculateHyperbolicTimeToPeriapsis(double a, double e, double mu, double r_boundary)
        {
            // 軌道長半径を絶対値にする
            double a_abs = Math.Abs(a);

            // 双曲線の方程式: r = |a| * (e * cosh(H) - 1)
            // cosh(H) = (r / |a| + 1) / e
            double coshH = (r_boundary / a_abs + 1.0) / e;
            
            // coshHは必ず1以上になるはずだが、数値誤差対策
            if (coshH < 1.0) coshH = 1.0;

            // 双曲線離心近点角 H を求める (arccosh)
            // H = ln(x + sqrt(x^2 - 1))
            double H = Math.Log(coshH + Math.Sqrt(coshH * coshH - 1.0));

            // 双曲線のケプラー方程式: M = e * sinh H - H
            // ここでのMは、近点(M=0)から境界までの平均近点角の差分
            double M = e * Math.Sinh(H) - H;

            // 平均運動 n = sqrt(mu / |a|^3)
            double n = Math.Sqrt(mu / Math.Pow(a_abs, 3.0));

            // 時間 t = M / n
            return M / n;
        }
    }
}
