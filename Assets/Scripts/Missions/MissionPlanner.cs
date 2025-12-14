using System;
using UnityEngine;
using SpaceLogistics.Space;
using SpaceLogistics.Core;
using SpaceLogistics.Rocketry;

namespace SpaceLogistics.Missions
{
    /// <summary>
    /// ミッション計画を計算する中核クラス。
    /// ノード間の移動計画を計算し、MissionLegを生成する。
    /// </summary>
    public static class MissionPlanner
    {
        /// <summary>
        /// 2つのノード間の移動計画を計算し、MissionLegを返す
        /// </summary>
        /// <param name="from">出発ノード</param>
        /// <param name="to">到着ノード</param>
        /// <param name="departureTime">出発時刻</param>
        /// <returns>計算されたMissionLeg、または移動不可能な場合はnull</returns>
        public static MissionLeg CalculateLeg(MissionNode from, MissionNode to, double departureTime)
        {
            if (from == null || to == null)
            {
                Debug.LogError("MissionPlanner: from or to node is null");
                return null;
            }

            // 1. 移動タイプの判定
            // ノードのType（Surface/Orbit）の組み合わせによって、
            // 単純な移動（1-2セグメント）か、複合的な移動（多段セグメント）かを判断する。

            if (from.TargetBody == to.TargetBody)
            {
                // 同一天体圏内の移動
                if (from.Type == LocationType.Surface && to.Type == LocationType.Orbit)
                    return CalculateAscent(from, to, departureTime); // 打ち上げ

                if (from.Type == LocationType.Orbit && to.Type == LocationType.Surface)
                    return CalculateLanding(from, to, departureTime); // 着陸

                if (from.Type == LocationType.Orbit && to.Type == LocationType.Orbit)
                    return CalculateRendezvous(from, to, departureTime); // 軌道変更/ランデブー

                if (from.Type == LocationType.Surface && to.Type == LocationType.Surface)
                {
                    // 同一表面上の移動（簡易実装: 一旦打ち上げ→着陸として扱う）
                    // 将来的には表面間ホバリングなどを実装可能
                    Debug.LogWarning("MissionPlanner: Surface-to-Surface transfer not fully implemented, using ascent+descent");
                    // 中間軌道ノードを作って処理することも可能だが、今回は簡易実装
                    return CalculateAscentAndLanding(from, to, departureTime);
                }
            }
            else
            {
                // 惑星間移動
                return CalculateInterplanetaryTransfer(from, to, departureTime);
            }

            Debug.LogWarning($"MissionPlanner: Unsupported transfer type combination: {from.Type} -> {to.Type}");
            return null; // 移動不可能
        }

        // --- 各モードの計算メソッド ---

        /// <summary>
        /// 打ち上げ (Surface -> Orbit)
        /// </summary>
        private static MissionLeg CalculateAscent(MissionNode from, MissionNode to, double departureTime)
        {
            MissionLeg leg = new MissionLeg
            {
                FromNode = from,
                ToNode = to
            };

            CelestialBody body = from.TargetBody;
            if (body == null)
            {
                Debug.LogError("MissionPlanner.CalculateAscent: TargetBody is null");
                return null;
            }

            double G = PhysicsConstants.GameGravitationalConstant;
            double mu = G * body.Mass.Kilograms;
            double r_surface = body.Radius.ToMeters();

            // 目標軌道高度（OrbitノードのParkingOrbitから取得、デフォルト200km）
            double targetAltitude = 200000.0; // meters
            if (to.ParkingOrbit != null)
            {
                targetAltitude = to.ParkingOrbit.SemiMajorAxis - r_surface;
            }

            double r_parking = r_surface + targetAltitude;

            // ΔV計算
            // 円軌道速度 v_orbital = sqrt(mu / r)
            double v_orbital = Math.Sqrt(mu / r_parking);
            
            // 打ち上げΔV = 軌道速度 + 重力損失 + 空気抵抗損失 + 予備
            // 簡易計算: 軌道速度 * 1.15 (15%の余裕) + 重力損失
            double gravityLoss = Math.Sqrt(2.0 * (G * body.Mass.Kilograms) / r_surface) * 0.5; // 簡易近似
            double dragLoss = 100.0; // 簡易値 (大気がない場合は0)
            if (!body.HasAtmosphere) dragLoss = 0.0;

            leg.RequiredDeltaV = v_orbital * 1.15 + gravityLoss + dragLoss;
            leg.MinTWR = 1.2; // 打ち上げには TWR > 1.2 が必要

            // 打ち上げ時間（簡易: 500秒）
            leg.TravelTime = 500.0;

            // セグメント生成
            // Segment 1: Launch (打ち上げ)
            OrbitParameters launchParams = new OrbitParameters
            {
                SemiMajorAxis = r_parking,
                Eccentricity = 0,
                Inclination = 0,
                ArgumentOfPeriapsis = 0,
                MeanMotion = Math.Sqrt(mu / Math.Pow(r_parking, 3))
            };

            var launchOrbit = new KeplerOrbit(body, launchParams, departureTime, departureTime + leg.TravelTime);
            var launchSegment = new TrajectorySegment(launchOrbit);
            launchSegment.phaseName = $"Launch to {body.BodyName} orbit";
            launchSegment.type = TrajectoryType.Launch;

            leg.Segments.Add(launchSegment);

            return leg;
        }

        /// <summary>
        /// 着陸 (Orbit -> Surface)
        /// </summary>
        private static MissionLeg CalculateLanding(MissionNode from, MissionNode to, double departureTime)
        {
            MissionLeg leg = new MissionLeg
            {
                FromNode = from,
                ToNode = to
            };

            CelestialBody body = to.TargetBody;
            if (body == null)
            {
                Debug.LogError("MissionPlanner.CalculateLanding: TargetBody is null");
                return null;
            }

            double G = PhysicsConstants.GameGravitationalConstant;
            double mu = G * body.Mass.Kilograms;
            double r_surface = body.Radius.ToMeters();

            // 出発軌道高度
            double departureAltitude = 200000.0; // meters
            if (from.ParkingOrbit != null)
            {
                departureAltitude = from.ParkingOrbit.SemiMajorAxis - r_surface;
            }

            double r_orbit = r_surface + departureAltitude;

            // ΔV計算
            // 円軌道速度を相殺 + 重力損失
            double v_orbital = Math.Sqrt(mu / r_orbit);
            double gravityLoss = Math.Sqrt(2.0 * mu / r_surface) * 0.3; // 簡易近似

            leg.RequiredDeltaV = v_orbital + gravityLoss;
            leg.MinTWR = 1.1; // 着陸時も TWR > 1.0 が必要

            // 着陸時間（簡易: 300秒）
            leg.TravelTime = 300.0;

            // セグメント生成
            OrbitParameters descentParams = new OrbitParameters
            {
                SemiMajorAxis = r_surface,
                Eccentricity = 0,
                Inclination = 0,
                ArgumentOfPeriapsis = 0,
                MeanMotion = Math.Sqrt(mu / Math.Pow(r_surface, 3))
            };

            var descentOrbit = new KeplerOrbit(body, descentParams, departureTime, departureTime + leg.TravelTime);
            var descentSegment = new TrajectorySegment(descentOrbit);
            descentSegment.phaseName = $"Landing on {body.BodyName}";
            descentSegment.type = TrajectoryType.Landing;

            leg.Segments.Add(descentSegment);

            return leg;
        }

        /// <summary>
        /// 軌道ランデブー (Orbit -> Orbit, 同一天体)
        /// </summary>
        private static MissionLeg CalculateRendezvous(MissionNode from, MissionNode to, double departureTime)
        {
            MissionLeg leg = new MissionLeg
            {
                FromNode = from,
                ToNode = to
            };

            CelestialBody body = from.TargetBody;
            if (body == null)
            {
                Debug.LogError("MissionPlanner.CalculateRendezvous: TargetBody is null");
                return null;
            }

            double G = PhysicsConstants.GameGravitationalConstant;
            double mu = G * body.Mass.Kilograms;
            double r_surface = body.Radius.ToMeters();

            // 軌道高度
            double r_from = r_surface + 200000.0;
            double r_to = r_surface + 200000.0;

            if (from.ParkingOrbit != null)
                r_from = from.ParkingOrbit.SemiMajorAxis;
            if (to.ParkingOrbit != null)
                r_to = to.ParkingOrbit.SemiMajorAxis;

            // Hohmann遷移のΔV
            double a_transfer = (r_from + r_to) / 2.0;
            double v_from = Math.Sqrt(mu / r_from);
            double v_transfer_1 = Math.Sqrt(mu * (2.0 / r_from - 1.0 / a_transfer));
            double v_transfer_2 = Math.Sqrt(mu * (2.0 / r_to - 1.0 / a_transfer));
            double v_to = Math.Sqrt(mu / r_to);

            leg.RequiredDeltaV = Math.Abs(v_transfer_1 - v_from) + Math.Abs(v_to - v_transfer_2);
            leg.MinTWR = 0.1; // 軌道内では低TWRでも可

            // ホーマン遷移時間（軌道周期の半分）
            leg.TravelTime = Math.PI * Math.Sqrt(Math.Pow(a_transfer, 3) / mu);

            // セグメント生成
            OrbitParameters transferParams = new OrbitParameters
            {
                SemiMajorAxis = a_transfer,
                Eccentricity = Math.Abs(r_to - r_from) / (r_to + r_from),
                Inclination = 0,
                ArgumentOfPeriapsis = 0,
                MeanMotion = Math.Sqrt(mu / Math.Pow(a_transfer, 3))
            };

            var transferOrbit = new KeplerOrbit(body, transferParams, departureTime, departureTime + leg.TravelTime);
            var transferSegment = new TrajectorySegment(transferOrbit);
            transferSegment.phaseName = $"Orbit transfer around {body.BodyName}";
            transferSegment.type = TrajectoryType.HohmannTransfer;

            leg.Segments.Add(transferSegment);

            return leg;
        }

        /// <summary>
        /// 惑星間遷移 (異なる天体間)
        /// 既存の MissionBuilder.CreatePlanetToMoonPlan のロジックを使用
        /// </summary>
        private static MissionLeg CalculateInterplanetaryTransfer(MissionNode from, MissionNode to, double departureTime)
        {
            MissionLeg leg = new MissionLeg
            {
                FromNode = from,
                ToNode = to
            };

            CelestialBody fromBody = from.TargetBody;
            CelestialBody toBody = to.TargetBody;

            if (fromBody == null || toBody == null)
            {
                Debug.LogError("MissionPlanner.CalculateInterplanetaryTransfer: TargetBody is null");
                return null;
            }

            // 既存のMissionBuilder.CreatePlanetToMoonPlanのロジックを活用
            // 親子関係がある場合（例: 地球->月）は既存ロジックを使用
            if (toBody.ParentBody == fromBody || fromBody.ParentBody == toBody)
            {
                // 親子関係: MissionBuilderのロジックを使用
                FlightPlan plan = MissionBuilder.CreatePlanetToMoonPlan(fromBody, toBody, departureTime);

                // FlightPlanからMissionLegに変換
                leg.Segments = plan.Segments;
                leg.TravelTime = 0.0;
                leg.RequiredDeltaV = 0.0;

                // セグメントからΔVと時間を集計
                foreach (var seg in leg.Segments)
                {
                    leg.TravelTime = Math.Max(leg.TravelTime, seg.EndTime - seg.StartTime);
                    // ΔVは各セグメントから計算可能だが、簡易的に合計時間のみ
                }

                // 簡易ΔV計算（実際には各セグメントから計算すべき）
                double G = PhysicsConstants.GameGravitationalConstant;
                double mu_from = G * fromBody.Mass.Kilograms;
                double r_from = fromBody.Radius.ToMeters() + 200000.0;
                double v_from = Math.Sqrt(mu_from / r_from);
                
                double mu_to = G * toBody.Mass.Kilograms;
                double r_to = toBody.Radius.ToMeters() + 500000.0;
                double v_to = Math.Sqrt(mu_to / r_to);

                // 遷移軌道の簡易計算
                double r_moon_dist = toBody.OrbitData.SemiMajorAxis;
                double a_transfer = (r_from + r_moon_dist) / 2.0;
                double v_transfer_1 = Math.Sqrt(mu_from * (2.0 / r_from - 1.0 / a_transfer));
                double v_transfer_2 = Math.Sqrt(mu_from * (2.0 / r_moon_dist - 1.0 / a_transfer));

                leg.RequiredDeltaV = Math.Abs(v_transfer_1 - v_from) + Math.Abs(v_to - v_transfer_2);
                leg.MinTWR = 1.2;

                // 移動時間を再計算
                if (leg.Segments.Count > 0)
                {
                    double firstStart = leg.Segments[0].StartTime;
                    double lastEnd = leg.Segments[leg.Segments.Count - 1].EndTime;
                    leg.TravelTime = lastEnd - firstStart;
                }

                return leg;
            }
            else
            {
                // より複雑な惑星間移動（未実装）
                Debug.LogWarning($"MissionPlanner: Complex interplanetary transfer from {fromBody.BodyName} to {toBody.BodyName} not fully implemented");
                return null;
            }
        }

        /// <summary>
        /// 表面間移動（打ち上げ→着陸）
        /// </summary>
        private static MissionLeg CalculateAscentAndLanding(MissionNode from, MissionNode to, double departureTime)
        {
            // 簡易実装: 一旦打ち上げ→軌道待機→着陸として扱う
            // 将来的には直接遷移も実装可能

            MissionLeg leg = new MissionLeg
            {
                FromNode = from,
                ToNode = to
            };

            CelestialBody body = from.TargetBody;
            if (body == null) return null;

            // 打ち上げ部分
            var ascentLeg = CalculateAscent(from, new MissionNode { Type = LocationType.Orbit, TargetBody = body }, departureTime);
            double ascentTime = ascentLeg.TravelTime;
            double ascentDeltaV = ascentLeg.RequiredDeltaV;

            // 着陸部分
            var landingNode = new MissionNode { Type = LocationType.Orbit, TargetBody = body };
            var landingLeg = CalculateLanding(landingNode, to, departureTime + ascentTime);
            double landingTime = landingLeg.TravelTime;
            double landingDeltaV = landingLeg.RequiredDeltaV;

            leg.TravelTime = ascentTime + landingTime;
            leg.RequiredDeltaV = ascentDeltaV + landingDeltaV;
            leg.MinTWR = Math.Max(ascentLeg.MinTWR, landingLeg.MinTWR);

            // セグメントを統合
            leg.Segments.AddRange(ascentLeg.Segments);
            leg.Segments.AddRange(landingLeg.Segments);

            return leg;
        }
    }
}

