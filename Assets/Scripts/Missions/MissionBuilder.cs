using UnityEngine;
using SpaceLogistics.Space;
using System;
using SpaceLogistics.Core;

namespace SpaceLogistics.Missions
{
    public static class MissionBuilder
    {
        /// <summary>
        /// 親子関係にある天体間（例：地球 -> 月）の飛行計画を作成する。
        /// </summary>
        public static FlightPlan CreateEarthToMoonPlan(CelestialBody origin, CelestialBody destination, double requestTime)
        {
            FlightPlan plan = new FlightPlan();

            // 1. Parking Orbit (Low Earth Orbit)
            double r_origin = origin.Radius.ToMeters(); 
            double r_park = r_origin + 200000.0; // +200km altitude
            
            double r1_meters = r_park;
            double mapScale = MapManager.MapScale;
            double r2_meters = destination.OrbitData.SemiMajorAxis / mapScale; // destination is Child of origin (or sibling logic adapted)

            // 2. Transfer Calculation (Provisional)
            // Duration計算のため、Orientation 0 で仮計算
            var tempTransfer = TrajectoryCalculator.CalculateHohmannTransfer(origin, r1_meters, r2_meters, requestTime);
            double transferDuration = tempTransfer.duration;

            double t_launch = requestTime; // Immediate launch
            double t_arrival = t_launch + transferDuration;

            // 3. Match Orientation
            // 到着時刻 t_arrival における月(Child)の位置(角度)を計算
            Vector3 moonPosAtArrival = destination.OrbitData.CalculatePosition(t_arrival);
            float angle_dest_rad = Mathf.Atan2(moonPosAtArrival.y, moonPosAtArrival.x);
            double angle_dest_deg = angle_dest_rad * Mathf.Rad2Deg;

            // ホーマン遷移(外側へ)では、Apogeeで到着する。
            // つまり、到着点のTrueAnomalyは180度(PI)。
            // 軌道の回転角(ArgOfPeriapsis, w) + 180 = 到着角(angle_dest)
            // w = angle_dest - 180
            double required_w = angle_dest_deg - 180.0;

            // 4. Create Actual Transfer Orbit
            var transferResult = TrajectoryCalculator.CalculateHohmannTransfer(
                origin, 
                r1_meters, 
                r2_meters, 
                t_launch, 
                required_w
            );

            // Phase 1: Parking Orbit (Short Wait)
            OrbitParameters parkParams = new OrbitParameters
            {
                SemiMajorAxis = r_park,
                Eccentricity = 0,
                Inclination = 0,
                ArgumentOfPeriapsis = 0,
                LongitudeOfAscendingNode = 0,
                MeanAnomalyAtEpoch = 0, 
                MeanMotion = Math.Sqrt((PhysicsConstants.GameGravitationalConstant * origin.Mass.Kilograms) / Math.Pow(r_park, 3))
            };
            // 視覚的な溜めとして10秒
            KeplerOrbit parkingOrbit = new KeplerOrbit(origin, parkParams, t_launch - 10, t_launch);
            plan.AddSegment(new TrajectorySegment(parkingOrbit));

            // Phase 2: Transfer Orbit
            plan.AddSegment(new TrajectorySegment(transferResult.orbit));
            
            // Phase 3: Capture at Moon (Low Lunar Orbit)
            // 到着後の処理
            double r_moon = destination.Radius.ToMeters();
            double r_park_moon = r_moon + 50000.0; 
            
            OrbitParameters moonParkParams = new OrbitParameters
            {
                SemiMajorAxis = r_park_moon,
                Eccentricity = 0,
                Inclination = 0,
                ArgumentOfPeriapsis = 0,
                LongitudeOfAscendingNode = 0,
                MeanAnomalyAtEpoch = 0,
                MeanMotion = Math.Sqrt((PhysicsConstants.GameGravitationalConstant * destination.Mass.Kilograms) / Math.Pow(r_park_moon, 3))
            };
            
            KeplerOrbit moonOrbit = new KeplerOrbit(destination, moonParkParams, t_arrival, t_arrival + 86400 * 7);
            plan.AddSegment(new TrajectorySegment(moonOrbit));

            return plan;
        }
    }
}
