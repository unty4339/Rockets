using System;

namespace SpaceLogistics.Space
{
    /// <summary>
    /// 軌道パラメータを保持するクラス。
    /// ケプラー軌道要素に基づいて位置を計算する。
    /// </summary>
    [System.Serializable]
    public class OrbitParameters : UnityEngine.ISerializationCallbackReceiver
    {
        public double SemiMajorAxis; // 軌道長半径 (a)
        public double Eccentricity;  // 離心率 (e)
        public double Inclination;   // 軌道傾斜角 (i)
        public double ArgumentOfPeriapsis; // 近点引数 (w)
        public double LongitudeOfAscendingNode; // 昇交点赤経 (Omega)
        public double MeanAnomalyAtEpoch; // 元期における平均近点角 (M0)
        public double MeanMotion; // 平均運動 (n)

        public double Period => 2.0 * Math.PI / MeanMotion; // 周期 (P)

        // Serialization Backing Fields
        [UnityEngine.SerializeField, UnityEngine.HideInInspector] private string _semiMajorAxisS;
        [UnityEngine.SerializeField, UnityEngine.HideInInspector] private string _eccentricityS;
        [UnityEngine.SerializeField, UnityEngine.HideInInspector] private string _inclinationS;
        [UnityEngine.SerializeField, UnityEngine.HideInInspector] private string _argPeriapsisS;
        [UnityEngine.SerializeField, UnityEngine.HideInInspector] private string _lanS; // LON
        [UnityEngine.SerializeField, UnityEngine.HideInInspector] private string _meanAnomalyS;
        [UnityEngine.SerializeField, UnityEngine.HideInInspector] private string _meanMotionS;

        public void OnBeforeSerialize()
        {
            _semiMajorAxisS = SemiMajorAxis.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
            _eccentricityS = Eccentricity.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
            _inclinationS = Inclination.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
            _argPeriapsisS = ArgumentOfPeriapsis.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
            _lanS = LongitudeOfAscendingNode.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
            _meanAnomalyS = MeanAnomalyAtEpoch.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
            _meanMotionS = MeanMotion.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
        }

        public void OnAfterDeserialize()
        {
            double.TryParse(_semiMajorAxisS, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out SemiMajorAxis);
            double.TryParse(_eccentricityS, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out Eccentricity);
            double.TryParse(_inclinationS, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out Inclination);
            double.TryParse(_argPeriapsisS, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out ArgumentOfPeriapsis);
            double.TryParse(_lanS, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out LongitudeOfAscendingNode);
            double.TryParse(_meanAnomalyS, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out MeanAnomalyAtEpoch);
            double.TryParse(_meanMotionS, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out MeanMotion);
        }

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
            
            // Debug.Log($"Orbit: t={time:F2}, n={MeanMotion:E2}, r={r:E2}, ang={angle:F2}");
            
            return new UnityEngine.Vector3((float)x, (float)y, 0);
        }
    }
}
