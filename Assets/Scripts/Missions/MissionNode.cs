using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SpaceLogistics.Space;
using SpaceLogistics.Structures;

namespace SpaceLogistics.Missions
{
    public enum LocationType
    {
        Surface,        // 未開発の地上
        Orbit,          // 周回軌道
        SurfaceBase,    // 地上基地 (施設あり)
        OrbitalStation  // 軌道ステーション (施設あり)
    }

    /// <summary>
    /// ミッションの経由地を表すクラス。
    /// 「場所」と「行動」の定義を含む。
    /// </summary>
    [System.Serializable]
    public class MissionNode
    {
        public string NodeName;
        public LocationType Type;

        // 場所の参照 (どちらか一方が設定される)
        public CelestialBody TargetBody; // 天体参照 (Surface/Orbit用)
        public Base TargetBase;          // 基地参照 (Base/Station用)

        // 軌道詳細 (Orbitの場合のみ使用)
        public OrbitParameters ParkingOrbit;

        // 地上詳細 (Surfaceの場合のみ使用)
        public Vector2 SurfaceCoordinates; // 緯度経度

        // このノードでの滞在タスクリスト
        public List<MissionTask> Tasks = new List<MissionTask>();

        // タイムスケジュール
        public double ArrivalTime;   // 到着時刻
        public double DepartureTime; // 出発時刻 (Arrival + StayDuration)

        /// <summary>
        /// 滞在時間 (全タスクの所要時間の合計)
        /// </summary>
        public double StayDuration => Tasks.Sum(t => t.Duration);
    }
}

