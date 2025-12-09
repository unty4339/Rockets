using System;
using UnityEngine;
using SpaceLogistics.Space;

namespace SpaceLogistics.Missions
{
    [System.Serializable]
    public class MissionData
    {
        public string OriginBodyName;
        public string DestinationBodyName;
        public double StartTime;
        public double EndTime;
        public bool IsGlobalMission;

        // Constructor
        public MissionData(string origin, string dest, double start, double end, bool isGlobal)
        {
            OriginBodyName = origin;
            DestinationBodyName = dest;
            StartTime = start;
            EndTime = end;
            IsGlobalMission = isGlobal;
        }

        // 進行度 (0.0 - 100.0) を取得
        public double GetProgress(double currentTime)
        {
            if (currentTime < StartTime) return 0.0;
            if (currentTime >= EndTime) return 100.0;

            double duration = EndTime - StartTime;
            if (duration <= 0) return 100.0;

            return ((currentTime - StartTime) / duration) * 100.0;
        }

        // 進行度から時刻を逆算
        public double GetTimeFromProgress(double progress)
        {
            double duration = EndTime - StartTime;
            return StartTime + duration * (progress / 100.0);
        }

        // 座標計算
        public Vector3 CalculatePosition(double progress)
        {
            // 進行度に対応する時刻
            double time = GetTimeFromProgress(progress);
            
            // 天体の参照解決 (MapManager経由)
            var originBody = FindBody(OriginBodyName);
            var destBody = FindBody(DestinationBodyName);

            if (originBody == null || destBody == null) return Vector3.zero;

            // マップスケール適用済みのUnity座標を返す
            if (IsGlobalMission)
            {
                return CalculateGlobalPosition(time, progress, originBody, destBody);
            }
            else
            {
                return CalculateLocalPosition(time, progress, originBody, destBody);
            }
        }

        private CelestialBody FindBody(string name)
        {
            // MapManagerのリストから検索 (キャッシュ推奨だがMVPでは直検索)
            if (MapManager.Instance == null) return null;
            return MapManager.Instance.AllBodies.Find(b => b.BodyName == name);
        }

        private Vector3 CalculateGlobalPosition(double time, double progress, CelestialBody origin, CelestialBody dest)
        {
            // グローバルマップ: 現在のMapManager.GlobalViewLogScaleなどを使って計算
            // ActiveRocketのロジックを移植
            
            var originRoot = origin.GetSystemRoot();
            var destRoot = dest.GetSystemRoot();
            
            // Phase Logic (Simplification)
            // 0-20: Ascent (Origin Root)
            // 20-80: Transit (Root to Root)
            // 80-100: Descent (Dest Root)
            
            double p = progress / 100.0;
            float scale = MapManager.Instance.GlobalViewLogScale;
            
            if (p < 0.2)
            {
                // Root上で静止、あるいは少し浮く
                return originRoot.GetGlobalPosition(time) * scale;
            }
            else if (p < 0.8)
            {
                double transitP = (p - 0.2) / 0.6;
                Vector3 start = originRoot.GetGlobalPosition(time);
                Vector3 end = destRoot.GetGlobalPosition(time);
                return Vector3.Lerp(start, end, (float)transitP) * scale;
            }
            else
            {
                return destRoot.GetGlobalPosition(time) * scale;
            }
        }

        private Vector3 CalculateLocalPosition(double time, double progress, CelestialBody origin, CelestialBody dest)
        {
            double p = progress / 100.0;
            
            // 到着済み
            if (p >= 1.0)
            {
                // 到着天体の上空
                double radiusKm = dest.Radius.ToKilometers();
                float dist = (float)(radiusKm / 1000.0 * 1.5 + 1.0); // 簡易高度
                Vector3 localPos = dest.GetLocalPosition(time);
                return (localPos + Vector3.up * dist) * MapManager.MapScale;
            }

            // Hohmann Transfer Logic
            // ActiveRocketからロジックを流用（MapScale適用済みで返す）
            
            Vector3 originPos = origin.GetLocalPosition(time); // Meters (Unscaled) from OrbitParameters
            Vector3 destPos = dest.GetLocalPosition(time);     // Meters
            
            // 注: ここで使う time は StartTime/EndTime ではなく、現在の time (progressに対応するtime)
            // 厳密なHohmann軌道計算では、
            // DepartureTimeにおけるOrigin位置 と ArrivalTimeにおけるDest位置 を結ぶ楕円を描く
            // そして、その楕円上の位置を 現在のtime で求める
            
            // しかし、ActiveRocketの簡易実装では「現在のOrigin位置」と「現在のDest位置」を使っていた (Moving Target問題)
            // 正しくは:
            // r1 = origin.GetLocalPosition(StartTime).magnitude
            // r2 = dest.GetLocalPosition(EndTime).magnitude
            // これで軌道長半径 a, 離心率 e が決まる（定数）。
            // そして progress (MeanAnomaly) に応じて r, nu を計算。
            // 最後に nu 分だけ回転させるが、基準となる角度は？
            
            // 簡易実装 (Scaling Target):
            // 現在のOrigin/Destの距離を使って補間する（今の実装に近い）
            // これだとターゲットが動くと軌道も歪むが、ゲーム的には命中しやすい
            
            // ここではActiveRocketの「改良版」として、StartTime/EndTimeそれぞれの位置を使う「正しい」計算に近づけるか、
            // 既存の「動的補間」を維持するか。
            // ユーザー要件は「進行度から座標が求められる」なので、deterministicであればよい。
            // GetTimeFromProgress(progress) で一意に時間が決まるので、どちらでもdeterministicになる。
            // 既存ロジックを踏襲する（動的ターゲット）。
            
            // Meters
            double r1 = originPos.magnitude;
            double r2 = destPos.magnitude;
            if (r1 < 0.1) r1 = 0.5;
            if (r2 < 0.1) r2 = 0.5;
            
            double majorAxis = r1 + r2;
            double a = majorAxis / 2.0;
            double e = Math.Abs(r2 - r1) / majorAxis;
            
            double meanAnomaly = p * Math.PI;
            
            // Solve Kepler
            // Use Rocketry.OrbitalMath
            double E = SpaceLogistics.Rocketry.OrbitalMath.SolveKepler(meanAnomaly, e);
            double nu = SpaceLogistics.Rocketry.OrbitalMath.EccentricToTrueAnomaly(E, e);
            
            double r = a * (1 - e * e) / (1 + e * Math.Cos(nu));
            
            // Angle interpolation
            float startAngle = Mathf.Atan2(originPos.y, originPos.x); // Rad
            float endAngle = Mathf.Atan2(destPos.y, destPos.x);     // Rad
            
            // DeltaAngle correctly handles wrapping usually, but Mathf.LerpAngle uses Degrees.
            float startDeg = startAngle * Mathf.Rad2Deg;
            float endDeg = endAngle * Mathf.Rad2Deg;
            float currentDeg = Mathf.LerpAngle(startDeg, endDeg, (float)p);
            float currentRad = currentDeg * Mathf.Deg2Rad;
            
            float x = (float)(r * Math.Cos(currentRad));
            float y = (float)(r * Math.Sin(currentRad));
            
            Vector3 orbitPosMeters = new Vector3(x, y, 0);
            
            // Apply MapScale
            return orbitPosMeters * MapManager.MapScale;
        }
    }
}
