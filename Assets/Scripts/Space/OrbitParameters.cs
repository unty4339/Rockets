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
            // 簡易計算: XZ平面（または2DならXY平面）上の円/楕円軌道とする
            // 現状はMVPのため完全な円、または単純な楕円を想定
            
            double currentMeanAnomaly = MeanAnomalyAtEpoch + MeanMotion * time;
            
            // 円軌道 (e=0) の場合:
            double x = SemiMajorAxis * Math.Cos(currentMeanAnomaly);
            double y = SemiMajorAxis * Math.Sin(currentMeanAnomaly);
            
            return new UnityEngine.Vector3((float)x, (float)y, 0);
        }
    }
}
