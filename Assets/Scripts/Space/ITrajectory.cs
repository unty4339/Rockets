using UnityEngine;

namespace SpaceLogistics.Space
{
    /// <summary>
    /// 軌道、遷移経路、着陸パスなど、あらゆる軌跡を表すインターフェース。
    /// </summary>
    public interface ITrajectory
    {
        CelestialBody ReferenceBody { get; } // 座標の基準となる天体
        double StartTime { get; }            // 開始時刻
        double EndTime { get; }              // 終了時刻

        /// <summary>
        /// 指定時刻の状態（位置・速度）を取得する。
        /// </summary>
        OrbitalState Evaluate(double time);
        
        /// <summary>
        /// 描画用に軌道パスの点群を取得する（オプション）。
        /// </summary>
        Vector3[] GetPathPoints(int resolution);
    }
}
