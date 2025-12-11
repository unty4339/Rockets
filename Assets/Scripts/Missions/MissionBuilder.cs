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

            // 物理定数
            // MapManager.MapScale等は表示の話なので、計算は全てPhysicsConstantsとメートルで行う。
            
            // 1. Parking Orbit (Low Earth Orbit)
            // 高度: 地表から一定距離 (例: 200km -> 200,000m)
            // origin.Radius は Unity Unit なのか Meters なのか？
            // CelestialBody の定義を確認すると、恐らく Unit。
            // しかし MapManager.PhysicsScale が導入されたので、Radius * (1e6) くらいか？
            // CelestialBody.Radiusプロパティがどうなっているか怪しいが、ToMeters() メソッドがあるようだ（以前のコード参照）。
            // ToMeters() がない場合は、MapManager.OrbitDataScale (1000km/unit) を使う。
            
            double r_origin = origin.Radius.ToMeters(); 
            double r_park = r_origin + 200000.0; // +200km altitude
            
            // 2. Transfer Calculation (Provisional to get duration)
            // ターゲット軌道半径
            double r_dest = destination.OrbitData.SemiMajorAxis; // Unity Unit (Parent Relative)
            // OrbitData.SemiMajorAxis は恐らく Unity Unit (MapScale適用後) だが、
            // MapManagerのコメント「OrbitParametersはメートル単位で計算される」とある。
            // CelestialBody.OrbitDataの中身がメートルならそのままでよい。
            // 前回のMapManagerのDiffで「OrbitData.SemiMajorAxis * MapScale」としていたので、
            // OrbitData.SemiMajorAxis は「メートル」である可能性が高い。
            // 素のOrbitDataはメートル。
            
            double r1 = r_park;
            double r2 = destination.OrbitData.SemiMajorAxis; // Meters

            // 仮の転移計算（所要時間を知るため）
            var tempTransfer = TrajectoryCalculator.CalculateHohmannTransfer(origin, r1, r2, requestTime);
            double transferDuration = tempTransfer.duration;

            // 3. Launch Window Calculation
            // 最適な発射時刻を計算
            double launchTime = TrajectoryCalculator.FindNextLaunchWindow(origin, destination, transferDuration, requestTime);
            
            // もし launchTime が requestTime より過去なら（周期的なのであり得ないはずだが）、未来へ。
            if (launchTime < requestTime) launchTime += destination.OrbitData.Period;

            // 4. Create Segments

            // Phase 0: Waiting on Parking Orbit
            // requestTime から launchTime まで
            if (launchTime > requestTime)
            {
                // Parking Orbit
                OrbitParameters parkParams = new OrbitParameters
                {
                    SemiMajorAxis = r_park,
                    Eccentricity = 0,
                    Inclination = 0,
                    ArgumentOfPeriapsis = 0,
                    LongitudeOfAscendingNode = 0,
                    MeanAnomalyAtEpoch = 0, // 仮
                    MeanMotion = Math.Sqrt((PhysicsConstants.GameGravitationalConstant * origin.Mass.Kilograms) / Math.Pow(r_park, 3))
                };
                KeplerOrbit parkingOrbit = new KeplerOrbit(origin, parkParams, requestTime, launchTime);
                plan.AddSegment(new TrajectorySegment(parkingOrbit));
            }

            // Phase 1: Transfer Orbit
            // launchTime から arrivalTime まで
            // 実際のホーマン遷移を計算
            var transferResult = TrajectoryCalculator.CalculateHohmannTransfer(origin, r1, r2, launchTime);
            plan.AddSegment(new TrajectorySegment(transferResult.orbit));
            
            double arrivalTime = launchTime + transferResult.duration;

            // Phase 2: Capture / Parking at Destination
            // 到着後の軌道（月の周回軌道）
            // Low Lunar Orbit
            double r_moon = destination.Radius.ToMeters();
            double r_park_moon = r_moon + 50000.0; // +50km
            
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
            
            // ずっと周回する（とりあえず1日分など）
            KeplerOrbit moonOrbit = new KeplerOrbit(destination, moonParkParams, arrivalTime, arrivalTime + 86400 * 7);
            plan.AddSegment(new TrajectorySegment(moonOrbit));

            return plan;
        }
    }
}
