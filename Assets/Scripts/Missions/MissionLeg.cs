using System.Collections.Generic;
using UnityEngine;
using SpaceLogistics.Space;

namespace SpaceLogistics.Missions
{
    /// <summary>
    /// ミッションの移動区間を表すクラス。
    /// ノード間の移動情報と計算結果を含む。
    /// </summary>
    [System.Serializable]
    public class MissionLeg
    {
        public MissionNode FromNode;
        public MissionNode ToNode;

        // 計算結果
        public double RequiredDeltaV; // この区間で消費するΔV
        public double MinTWR;         // 必要な最小推力重量比 (離陸時は >1.0 など)
        public double TravelTime;     // 移動にかかる時間

        // 軌道データ詳細
        // 1つのLegは、物理的な移動工程である複数の「Segment」のリストで構成される。
        // 
        // 【例：地球表面から月表面への直接移動 (Surface to Surface) の場合】
        // ユーザーがNodeを細かく設定せず、始点「地球」・終点「月」とした場合、
        // この1つのLegの中に以下のようなSegment列が生成される：
        //   Segments[0]: Launch (打ち上げ・上昇)
        //   Segments[1]: Parking Orbit (地球低軌道待機)
        //   Segments[2]: TLI Burn (月遷移軌道投入)
        //   Segments[3]: Coast (月への慣性飛行)
        //   Segments[4]: LOI Burn (月周回軌道投入)
        //   Segments[5]: Descent (降下・着陸)
        public List<TrajectorySegment> Segments = new List<TrajectorySegment>();
    }
}

