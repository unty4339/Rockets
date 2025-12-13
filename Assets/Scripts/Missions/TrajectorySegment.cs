using UnityEngine;
using System; // Serializable用

namespace SpaceLogistics.Space
{
    // --- 必要な列挙型の定義 ---

    public enum TrajectoryType
    {
        OrbitPropagation,   // 慣性飛行 (待機、巡航)
        Launch,             // 打ち上げ
        HohmannTransfer,    // ホーマン遷移
        Circularize,        // 円軌道化
        Landing,            // 着陸
        Aerobraking         // 大気減速 (将来用)
    }

    public enum ExitCondition
    {
        TimeElapsed,        // 指定時間の経過 (デフォルト)
        EnterTargetSOI,     // ターゲット天体のSOIに到達
        ReachAltitude,      // 指定高度に到達
        ApoapsisReached,    // 遠点に到達
        PeriapsisReached    // 近点に到達
    }

    /// <summary>
    /// ミッションの1工程を表すクラス。
    /// 物理的な軌道(Trajectory)と、計画上のメタデータ(Type, Condition)を持つ。
    /// </summary>
    [System.Serializable]
    public class TrajectorySegment
    {
        [Header("Planning Data")]
        public string phaseName;                // フェーズ名 (例: "TLI Burn")
        public TrajectoryType type;             // 軌道の種類
        public ExitCondition exitCondition;     // 終了条件
        public CelestialBody nextReferenceBody; // SOI遷移時の次ターゲット

        [Header("Physical Trajectory")]
        public ITrajectory Trajectory;
        public double StartTime;
        public double EndTime;

        // デフォルトコンストラクタ (初期化子用)
        public TrajectorySegment() { }

        // 既存のコンストラクタ (軌道のみ指定)
        public TrajectorySegment(ITrajectory trajectory)
        {
            Trajectory = trajectory;
            if (trajectory != null)
            {
                StartTime = trajectory.StartTime;
                EndTime = trajectory.EndTime;
            }
            type = TrajectoryType.OrbitPropagation; // デフォルト
            exitCondition = ExitCondition.TimeElapsed;
        }

        // 時間指定コンストラクタ
        public TrajectorySegment(ITrajectory trajectory, double start, double end)
        {
            Trajectory = trajectory;
            StartTime = start;
            EndTime = end;
            type = TrajectoryType.OrbitPropagation;
            exitCondition = ExitCondition.TimeElapsed;
        }

        public OrbitalState Evaluate(double time)
        {
            if (Trajectory == null) return default;
            return Trajectory.Evaluate(time);
        }
    }
}