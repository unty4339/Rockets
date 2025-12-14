using UnityEngine;
using SpaceLogistics.Space;
using System;
using SpaceLogistics.Core;
using SpaceLogistics.Rocketry;

namespace SpaceLogistics.Missions
{
    public static class MissionBuilder
    {

        /// <summary>
        /// 惑星(Primary)の周回軌道から、その衛星(Moon)の周回軌道への飛行計画を作成する。
        /// </summary>
        /// <param name="planet">出発する主星 (例: 地球)</param>
        /// <param name="moon">目的地の衛星 (例: 月)</param>
        /// <param name="requestTime">ミッション開始希望時刻</param>
        public static FlightPlan CreatePlanetToMoonPlan(CelestialBody planet, CelestialBody moon, double requestTime)
        {
            FlightPlan plan = new FlightPlan();

            // --- 0. 定数とパラメータの準備 ---
            double G = PhysicsConstants.GameGravitationalConstant;
            double mu_primary = G * planet.Mass.Kilograms;
            double mu_moon = G * moon.Mass.Kilograms;

            // 各種半径 (m)
            double r_primary_surface = planet.Radius.ToMeters();
            double r_parking_primary = r_primary_surface + 200000.0; // 出発: 高度200km

            double r_moon_dist = moon.OrbitData.SemiMajorAxis; 
            double r_moon_soi = moon.SOIRadius.ToMeters();
            
            double r_moon_surface = moon.Radius.ToMeters();
            double r_parking_moon = r_moon_surface + 500000.0; // 目標: 月高度50km

            // --- 1. 最適な遷移軌道の探索 (Apogee Interaction) ---
            // アポジー会合仮定: 「遷移軌道の遠地点 = 月のSOI境界への進入点」となるような距離 ra を探す
            double optimal_ra = OrbitalMath.FindOptimalApogeeRadiusForMoonTransfer(
                r_parking_moon, r_parking_primary, r_moon_dist, r_moon_soi, mu_primary, mu_moon
            );

            // 探索失敗時のフォールバック
            if (double.IsNaN(optimal_ra)) 
            {
                Debug.LogWarning("Optimization failed. Fallback to simple distance.");
                optimal_ra = r_moon_dist; 
            }

            // --- 2. 遷移軌道(楕円)のパラメータ確定 ---
            // 地球中心の楕円軌道
            double a_trans = (r_parking_primary + optimal_ra) / 2.0;
            double e_trans = (optimal_ra - r_parking_primary) / (optimal_ra + r_parking_primary);

            // --- 3. 時間計算: SOI到達時間 ---
            // アポジー会合モデルでは、遠地点到達 = SOI突入 となるため、
            // 到達時間は「遷移軌道の周期の半分」となる。
            double transferPeriod = 2.0 * Math.PI * Math.Sqrt(Math.Pow(a_trans, 3) / mu_primary);
            double timeToSOI = transferPeriod / 2.0;

            double t_launch = requestTime;
            double t_soi_entry = t_launch + timeToSOI;

            // --- 4. 角度合わせ (Phasing) ---
            // t_soi_entry 時点での月の位置を計算
            Vector3 moonPosAtSOI = moon.OrbitData.CalculatePosition(t_soi_entry);
            double moonAngleAtSOI = Math.Atan2(moonPosAtSOI.y, moonPosAtSOI.x);

            // 余弦定理より、会合時の角度差 phi を計算
            // optimal_ra は探索で求められた「地球-宇宙船」の距離
            double cosPhi = (r_moon_dist * r_moon_dist + optimal_ra * optimal_ra - r_moon_soi * r_moon_soi) 
                            / (2 * r_moon_dist * optimal_ra);
            double phi = Math.Acos(Math.Clamp(cosPhi, -1.0, 1.0));

            // アポジーの角度を決定 (月の位置から phi ずらす)
            // ※外側への遷移で、月が後ろから追いつく形なら phi を引く
            // double targetApogeeAngle = moonAngleAtSOI - phi;
            double targetApogeeAngle = moonAngleAtSOI + phi;
            
            // 近地点(打ち上げ点)はアポジーの180度反対側
            double omega_rad = targetApogeeAngle - Math.PI; 
            double omega_deg = omega_rad * Mathf.Rad2Deg;

            // --- 5. 双曲線軌道 (Phase 3) のパラメータ計算 ---
            // SOI境界での相対速度ベクトルから、月圏内の軌道を決定する

            // A. アポジーでの宇宙船の速度 (地球基準) - 接線方向のみ
            // v^2 = mu * (2/r - 1/a)
            double v_ship_mag = Math.Sqrt(mu_primary * (2.0 / optimal_ra - 1.0 / a_trans));
            // 速度ベクトル (位置ベクトルに対して垂直)
            // 位置角度: targetApogeeAngle, 速度角度: targetApogeeAngle + 90deg
            Vector2 v_ship_vec = new Vector2(
                (float)(v_ship_mag * Math.Cos(targetApogeeAngle + Math.PI / 2.0)),
                (float)(v_ship_mag * Math.Sin(targetApogeeAngle + Math.PI / 2.0))
            );

            // B. 月の速度 (地球基準)
            // 厳密には月の軌道要素から計算すべきだが、ここでは円軌道近似で速度ベクトルを出す
            // 月の位置角度: moonAngleAtSOI, 速度角度: moonAngleAtSOI + 90deg
            double v_moon_mag = Math.Sqrt(mu_primary / r_moon_dist);
            Vector2 v_moon_vec = new Vector2(
                (float)(v_moon_mag * Math.Cos(moonAngleAtSOI + Math.PI / 2.0)),
                (float)(v_moon_mag * Math.Sin(moonAngleAtSOI + Math.PI / 2.0))
            );

            // C. 双曲線過剰速度 (v_inf) = 相対速度
            double v_inf_sq = (v_ship_vec - v_moon_vec).sqrMagnitude;

            // D. 双曲線軌道の形状決定
            // ターゲット近点距離 rp は r_parking_moon (50km高度)
            // 近点速度 vp^2 = v_inf^2 + 2*mu_moon / rp
            // 比エネルギー E = v_inf^2 / 2
            // 軌道長半径 a_hyp = -mu / 2E = -mu / v_inf^2 (双曲線なので負になる)
            double a_hyp_abs = mu_moon / v_inf_sq; // 計算用に絶対値を使用
            
            // 離心率 e = 1 + rp * v_inf^2 / mu
            double e_hyp = 1.0 + (r_parking_moon * v_inf_sq) / mu_moon;

            // E. SOI通過時間の計算
            // 半径 r_moon_soi から 近点 rp までの落下時間
            double timeInSOI = OrbitalMath.CalculateHyperbolicTimeToPeriapsis(
                a_hyp_abs, e_hyp, mu_moon, r_moon_soi
            );
            
            double t_periapsis = t_soi_entry + timeInSOI;

            // F. 双曲線の角度設定
            // 1. 相対速度ベクトルの角度 (入射方向)
            Vector2 v_rel = v_ship_vec - v_moon_vec;
            double v_inf_angle = Math.Atan2(v_rel.y, v_rel.x);

            // 2. ベータ角 (漸近線と近点軸の角度)
            // e_hyp は前のステップで計算済み (e_hyp = 1 + rp * v_inf^2 / mu)
            double beta = Math.Acos(1.0 / e_hyp); 

            // 3. 回転方向の判定 (外積を使用)
            // 宇宙船の位置ベクトル (SOI進入時 = アポジー位置)
            Vector2 pos_ship_vec = new Vector2(
                (float)(optimal_ra * Math.Cos(targetApogeeAngle)),
                (float)(optimal_ra * Math.Sin(targetApogeeAngle))
            );
            
            // 月の位置ベクトル (SOI進入時)
            // moonPosAtSOI は Vector3 なので Vector2 にキャスト
            Vector2 pos_moon_vec = new Vector2(moonPosAtSOI.x, moonPosAtSOI.y);

            // 相対位置ベクトル r_rel
            Vector2 r_rel = pos_ship_vec - pos_moon_vec;

            // 2D外積 (z成分) = rx * vy - ry * vx
            double crossProduct = r_rel.x * v_rel.y - r_rel.y * v_rel.x;

            // 4. 近点引数 omega の決定
            double omega_moon_rad;
            
            // 外積の符号で回転方向を分岐
            if (crossProduct >= 0)
            {
                // 反時計回り (CCW): 左に曲がる
                omega_moon_rad = v_inf_angle + beta;
            }
            else
            {
                // 時計回り (CW): 右に曲がる
                omega_moon_rad = v_inf_angle - beta;
            }

            double omega_moon_deg = omega_moon_rad * Mathf.Rad2Deg;


            // ==========================================
            //      セグメント(Phase)の生成と登録
            // ==========================================

            // Phase 1: Parking Orbit (LEO待機)
            OrbitParameters parkParams = new OrbitParameters
            {
                SemiMajorAxis = r_parking_primary,
                Eccentricity = 0,
                Inclination = 0,
                ArgumentOfPeriapsis = omega_deg, // 打ち上げ角に合わせて待機
                MeanMotion = Math.Sqrt(mu_primary / Math.Pow(r_parking_primary, 3))
            };
            var seg1 = new TrajectorySegment(new KeplerOrbit(planet, parkParams, t_launch - 600, t_launch));
            seg1.phaseName = $"Parking at {planet.BodyName}";
            seg1.type = TrajectoryType.OrbitPropagation;
            plan.AddSegment(seg1);

            // Phase 2: Transfer (Hohmann -> SOI Entry)
            OrbitParameters transParams = new OrbitParameters
            {
                SemiMajorAxis = a_trans,
                Eccentricity = e_trans,
                Inclination = 0,
                ArgumentOfPeriapsis = omega_deg,
                MeanAnomalyAtEpoch = 0, // 近点(M=0)から開始
                MeanMotion = Math.Sqrt(mu_primary / Math.Pow(a_trans, 3))
            };
            // t_launch から t_soi_entry まで
            var seg2 = new TrajectorySegment(new KeplerOrbit(planet, transParams, t_launch, t_soi_entry, t_launch));
            seg2.phaseName = $"Transfer to {moon.BodyName}";
            seg2.type = TrajectoryType.HohmannTransfer;
            seg2.exitCondition = ExitCondition.EnterTargetSOI;
            seg2.nextReferenceBody = moon;
            plan.AddSegment(seg2);

            // Phase 3: Approach (Hyperbolic Drop)
            OrbitParameters approachParams = new OrbitParameters
            {
                SemiMajorAxis = a_hyp_abs, // KeplerOrbitの実装がa>0を想定している場合
                Eccentricity = e_hyp,
                Inclination = 0,
                ArgumentOfPeriapsis = omega_moon_deg,
                // 双曲線のMean Anomaly計算:
                // 近点(t_periapsis)でM=0となるようにEpochを設定するか、現在のMを逆算してセットする
                // ここではEpoch = t_periapsis, M_at_Epoch = 0 とするのが最も簡単
                MeanAnomalyAtEpoch = 0,
                MeanMotion = Math.Sqrt(mu_moon / Math.Pow(a_hyp_abs, 3))
            };
            
            // t_soi_entry から t_periapsis まで
            // 注意: KeplerOrbitクラスがEpoch(=t_periapsis)より前の時刻(t_soi_entry)を正しく計算できる前提
            var keplerHyp = new KeplerOrbit(moon, approachParams, t_soi_entry, t_periapsis, t_periapsis);
            // 双曲線のパラメータを正しく反映させるため、KeplerOrbit内部での時刻扱い(t - Epoch)に注意が必要
            // もしKeplerOrbitがEpochプロパティを持たない場合、OrbitParametersのMeanAnomalyAtEpochの定義時刻を合わせる必要がある
            
            var seg3 = new TrajectorySegment(keplerHyp);
            seg3.phaseName = "Moon Approach";
            seg3.type = TrajectoryType.OrbitPropagation;
            seg3.exitCondition = ExitCondition.PeriapsisReached;
            plan.AddSegment(seg3);

            // Phase 4: Capture (Circular Orbit)
            OrbitParameters captureParams = new OrbitParameters
            {
                SemiMajorAxis = r_parking_moon,
                Eccentricity = 0,
                Inclination = 0,
                ArgumentOfPeriapsis = 0,
                MeanMotion = Math.Sqrt(mu_moon / Math.Pow(r_parking_moon, 3))
            };
            var seg4 = new TrajectorySegment(new KeplerOrbit(moon, captureParams, t_periapsis, t_periapsis + 86400));
            seg4.phaseName = $"Orbiting {moon.BodyName}";
            seg4.type = TrajectoryType.Circularize;
            plan.AddSegment(seg4);

            return plan;
        }
    }
}