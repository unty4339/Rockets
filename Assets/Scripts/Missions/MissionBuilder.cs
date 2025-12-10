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
            // ActiveRocket.cs の既存ロジックでは root.LocalMapRadius 等を使っているが、
            // ここでは少し物理っぽく、地表から少し浮いた位置とする。
            // 簡易的に Radius * 1.2
            double r_park = origin.Radius.ToMeters() * 1.2;
            if (r_park < origin.Radius.ToMeters() + 10000) r_park = origin.Radius.ToMeters() + 500000; // Minimum altitude
            
            // MapScale変換 (Meters -> Unity Units)
            // MapManager.MapScale を使う必要があるが、ここではCelestialBodyから逆算も可。
            // CelestialBody.VisualScaleLocalなどがあるが、OrbitParametersはUnity Unitで計算していると仮定?
            // OrbitParameters.csを見ると、CalculatePositionはそのまま返している。
            // ActiveRocket.Launchでは r1 = originBody.GetLocalPosition(time).magnitude とある。
            // これは親天体中心の距離。
            
            // Parking Orbitは親天体(Earth)中心の円軌道。
            // ここでは簡単のため、MapManagerを通さず、Unity Unitでの半径を決める。
            // 地球のUnity上半径を取得する術が必要。RadiusはPhysicsTypes.Distance。
            // CelestialBody.VisualSOIRadius の計算式を参考にすると、
            // (r_soi * MapManager.MapScale).
            // つまり r_park_unity = r_park_meters * MapManager.MapScale
            
            // MapManager.MapScaleへのアクセスが必要。シングルトン前提。
            double mapScale = MapManager.MapScale; 
            double r1_unity = r_park * mapScale;
            // 安全策: 0.5 unit確保 (ActiveRocketのロジック踏襲)
            if (r1_unity < 0.5) r1_unity = 0.5;

            // 月の軌道半径 (平均)
            // destination.OrbitData.SemiMajorAxis はUnity Unitのはず。
            double r2_unity = destination.OrbitData.SemiMajorAxis;
            
            // 2. Trans-Lunar Injection (TLI) Calculation
            // ホーマン遷移計算
            var transferResult = TrajectoryCalculator.CalculateHohmannTransfer(origin, r1_unity, r2_unity, startTime);
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
            // 月もOrbitDataを持つので計算可能
            // destination.OrbitData はParent(Earth)基準
            // ここは簡易的にOrbitParametersを使って計算
            Vector3 moonPos = destination.OrbitData.CalculatePosition(t_soi_entry);
            // 月の速度は... KeplerOrbit化してEvaluateしないと出ない。
            // 簡易的に円軌道速度推定: v = sqrt(mu/r) * tangent, または差分計算
            double dt_v = 0.01;
            Vector3 moonPosNext = destination.OrbitData.CalculatePosition(t_soi_entry + dt_v);
            Vector3 moonVel = (moonPosNext - moonPos) / (float)dt_v;

            Vector3 relPos = entryState_EarthFrame.Position - moonPos;
            Vector3 relVel = entryState_EarthFrame.Velocity - moonVel;

            // 月基準の軌道を作成 (Hyperbolic Approach)
            // 位置・速度ベクターから軌道要素変換 (State Vectors to Orbital Elements)
            // これはまだOrbitMathに実装していない...！
            // なので、MVPとしては「それっぽい双曲線」を捏造するか、
            // 追加で StateVectorsToElements を実装するか。
            
            // MVPアプローチ:
            // Transferの残りを、月座標系に変換して表示する「擬似パッチドコニックス」
            // しかし軌道要素として保持したい。
            // ここは重要なので、簡易的な要素変換を入れるか、
            // あるいは「月周回の円軌道にブレンド」するActiveRocketのロジックをFlightPlanで再現するか。
            
            // ここでは「着陸軌道への直接遷移」として、月を中心とした円軌道に接続する（簡易化）
            // SOI EntryからCaptureまで1時間くらいで遷移
            double t_capture_end = t_intercept + 3600.0; // +1 hour

            // Capture Orbit (Low Lunar Orbit)
            double r_lunar_orbit = (destination.Radius.ToMeters() * 1.2) * mapScale;
            if (r_lunar_orbit < 0.2) r_lunar_orbit = 0.2;

            OrbitParameters lunarOrbitParams = new OrbitParameters
            {
                SemiMajorAxis = r_lunar_orbit,
                Eccentricity = 0.0, // 円軌道
                Inclination = 0.0,
                ArgumentOfPeriapsis = 0.0,
                MeanMotion = Math.Sqrt((PhysicsConstants.GameGravitationalConstant * destination.Mass.Kilograms) / Math.Pow(r_lunar_orbit, 3)),
                MeanAnomalyAtEpoch = 0.0 // 位相は適当
            };

            KeplerOrbit lunarOrbit = new KeplerOrbit(destination, lunarOrbitParams, t_soi_entry, t_capture_end + 10000); // ずっと周回
            plan.AddSegment(new TrajectorySegment(lunarOrbit));

            return plan;
        }

        // Helper: Convert Meters to Active Scale?
        // MapManagerへの依存を持たせる。
    }
}
