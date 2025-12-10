using UnityEngine;
using SpaceLogistics.Space;
using System;
using SpaceLogistics.Core;

namespace SpaceLogistics.Missions
{
    public static class MissionBuilder
    {
        /// <summary>
        /// 地球から月への飛行計画を作成する。
        /// </summary>
        public static FlightPlan CreateEarthToMoonPlan(CelestialBody origin, CelestialBody destination, double startTime)
        {
            FlightPlan plan = new FlightPlan();

            // 1. Parking Orbit (Low Earth Orbit)
            // 高度200km相当 (Game Scaleに合わせる)
            double r_park = origin.Radius.ToMeters() * 1.2;
            if (r_park < origin.Radius.ToMeters() + 10000) r_park = origin.Radius.ToMeters() + 500000; // Minimum altitude
            
            // MapScale変換は行わず、メートル単位で計算する
            double r1_meters = r_park;

            // 月の軌道半径 (平均)
            // destination.OrbitData.SemiMajorAxis は Unity Unit (MapScale適用済み)
            // 計算のためにメートルに戻す
            double mapScale = MapManager.MapScale;
            double r2_meters = destination.OrbitData.SemiMajorAxis / mapScale;
            
            // 2. Trans-Lunar Injection (TLI) Calculation
            // ホーマン遷移計算 (メートル単位)
            var transferResult = TrajectoryCalculator.CalculateHohmannTransfer(origin, r1_meters, r2_meters, startTime);
            KeplerOrbit transferOrbit = transferResult.orbit;
            double transferDuration = transferResult.duration;

            double t_launch = startTime; // 即時打ち上げと仮定 (Launch Window待ちはPhase 0として追加可能だが簡略化)
            double t_intercept = t_launch + transferDuration;

            // Phase 1: LEO (Parking)
            // TLIまで待機 (ここでは0時間待機とし、即TLIへ)
            // 実際はLaunch Windowに合わせる必要があるが、MVPでは省略。
            
            // Phase 2: Transfer (Earth to Moon SOI)
            // 月のSOIに入るまでの時間を計算すべきだが、まずは全行程の90%くらいまでをTransferとする
            double t_soi_entry = t_launch + transferDuration * 0.9; 
            
            // Transfer Orbitの前半部分を登録
            // EndTimeをSOI Entryに書き換えたコピーを作るべきだが、ITrajectoryはReadonlyっぽいので
            // Segment側で時間を区切るのが正しい設計。でもSegmentはTrajectoryのEndを使う。
            // KeplerOrbitを再生成する。
            KeplerOrbit earthToMoon = new KeplerOrbit(origin, transferOrbit.Parameters, t_launch, t_soi_entry);
            plan.AddSegment(new TrajectorySegment(earthToMoon));

            // Phase 3: Moon SOI Approach (Hyperbolic / Capture)
            // SOI Entry時の位置・速度を計算
            OrbitalState entryState_EarthFrame = earthToMoon.Evaluate(t_soi_entry);
            
            // 月の位置・速度 (Earth Frame)
            // destination.OrbitData はParent(Earth)基準 (Unity Unit)
            // メートルに変換して計算
            Vector3 moonPosUnity = destination.OrbitData.CalculatePosition(t_soi_entry);
            Vector3 moonPosMeters = moonPosUnity / (float)mapScale;
            
            double dt_v = 0.01;
            Vector3 moonPosNextUnity = destination.OrbitData.CalculatePosition(t_soi_entry + dt_v);
            Vector3 moonPosNextMeters = moonPosNextUnity / (float)mapScale;
            
            Vector3 moonVelMeters = (moonPosNextMeters - moonPosMeters) / (float)dt_v;
            
            // EvaluateはUnity Unitを返すので、メートルに戻して物理計算... 
            // いや、KeplerOrbitの修正でEvaluateはUnity Unitを返すようになる。
            // なので entryState_EarthFrame.Position は Unity Unit。
            // 物理計算のために全部メートルにする必要がある。
            // KeplerOrbit.Evaluateが返すのは「表示用」と考えたほうがいいが、
            // ここでは物理計算の続きなので、KeplerOrbit内部の生データ(Meters)が欲しい場合がある。
            // しかしインターフェース上はEvaluateしかない。
            // よって、ここでも mapScale で割ってメートルに戻す。
            
            Vector3 entryPosMeters = entryState_EarthFrame.Position / (float)mapScale;
            Vector3 entryVelMeters = entryState_EarthFrame.Velocity / (float)mapScale;

            Vector3 relPos = entryPosMeters - moonPosMeters;
            Vector3 relVel = entryVelMeters - moonVelMeters;

            // ... (Simple Capture Logic)
            
            // SOI EntryからCaptureまで1時間くらいで遷移
            double t_capture_end = t_intercept + 3600.0; // +1 hour

            // Capture Orbit (Low Lunar Orbit)
            // メートル単位
            double r_lunar_orbit = destination.Radius.ToMeters() * 1.2;
            if (r_lunar_orbit < destination.Radius.ToMeters() + 20000) r_lunar_orbit = destination.Radius.ToMeters() + 20000;

            OrbitParameters lunarOrbitParams = new OrbitParameters
            {
                SemiMajorAxis = r_lunar_orbit, // Meters
                Eccentricity = 0.0,
                Inclination = 0.0,
                ArgumentOfPeriapsis = 0.0,
                MeanMotion = Math.Sqrt((PhysicsConstants.GameGravitationalConstant * destination.Mass.Kilograms) / Math.Pow(r_lunar_orbit, 3)),
                MeanAnomalyAtEpoch = 0.0
            };

            KeplerOrbit lunarOrbit = new KeplerOrbit(destination, lunarOrbitParams, t_soi_entry, t_capture_end + 10000); 
            plan.AddSegment(new TrajectorySegment(lunarOrbit));

            return plan;
        }

        // Helper: Convert Meters to Active Scale?
        // MapManagerへの依存を持たせる。
    }
}
