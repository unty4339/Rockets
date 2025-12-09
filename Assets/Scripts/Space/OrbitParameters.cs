using System;

namespace SpaceLogistics.Space
{
    /// <summary>
    /// 軌道パラメータを保持するクラス。
    /// ケプラー軌道要素に基づいて位置を計算する。
    /// </summary>
    [System.Serializable]
    public class OrbitParameters
    {
        public double SemiMajorAxis; // 軌道長半径 (a)
        public double Eccentricity;  // 離心率 (e)
        public double Inclination;   // 軌道傾斜角 (i) - 2D簡略化では未使用だが拡張用
        public double ArgumentOfPeriapsis; // 近点引数 (w)
        public double MeanAnomalyAtEpoch; // 元期における平均近点角 (M0)
        public double MeanMotion; // 平均運動 (n)

        /// <summary>
        /// 指定された時間における位置を計算する。
        /// 2D用に簡略化されたケプラー軌道計算を行う。
        /// </summary>
        /// <param name="time">宇宙時間</param>
        /// <returns>計算されたローカル位置</returns>
        public UnityEngine.Vector3 CalculatePosition(double time)
        {
            // 1. Mean Anomaly Calculation
            double currentMeanAnomaly = MeanAnomalyAtEpoch + MeanMotion * time;
            
            // 2. Solve Kepler's Equation for Eccentric Anomaly (E)
            // Use Rocketry.OrbitalMath
            double E = Rocketry.OrbitalMath.SolveKepler(currentMeanAnomaly, Eccentricity);
            
            // 3. Calculate True Anomaly (nu)
            double nu = Rocketry.OrbitalMath.EccentricToTrueAnomaly(E, Eccentricity);
            
            // 4. Calculate Radius (r)
            // r = a * (1 - e^2) / (1 + e * cos(nu))
            double r = SemiMajorAxis * (1.0 - Eccentricity * Eccentricity) / (1.0 + Eccentricity * Math.Cos(nu));
            
            // 5. Calculate position in orbital plane (assuming XY plane for 2D)
            // Argument of Periapsis usually rotates the orbit
            double angle = nu + ArgumentOfPeriapsis; 
            
            double x = r * Math.Cos(angle);
            double y = r * Math.Sin(angle);
            
            return new UnityEngine.Vector3((float)x, (float)y, 0);
        }
    }
}
