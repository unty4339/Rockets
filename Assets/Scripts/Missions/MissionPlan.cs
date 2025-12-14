using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpaceLogistics.Missions
{
    /// <summary>
    /// ミッション全体を管理するルートオブジェクト。
    /// ノードとレッグのリスト、および集計情報を含む。
    /// </summary>
    [System.Serializable]
    public class MissionPlan
    {
        public string PlanName;
        public string Description;

        // ノードのリスト（時系列順）
        public List<MissionNode> Nodes = new List<MissionNode>();

        // ノード間をつなぐ移動情報のリスト
        // Legs[i] は Nodes[i] から Nodes[i+1] への移動を表す
        public List<MissionLeg> Legs = new List<MissionLeg>();

        /// <summary>
        /// 総合所要時間（全レッグの移動時間と全ノードの滞在時間の合計）
        /// </summary>
        public double TotalEstimatedDuration
        {
            get
            {
                double total = 0.0;

                // 各ノードの滞在時間を加算
                foreach (var node in Nodes)
                {
                    total += node.StayDuration;
                }

                // 各レッグの移動時間を加算
                foreach (var leg in Legs)
                {
                    total += leg.TravelTime;
                }

                return total;
            }
        }

        /// <summary>
        /// 必要な総ΔV（全レッグのΔVの合計）
        /// </summary>
        public double TotalRequiredDeltaV
        {
            get
            {
                return Legs.Sum(leg => leg.RequiredDeltaV);
            }
        }

        /// <summary>
        /// バリデーション：経路が繋がっているか、燃料計算済みか等をチェック
        /// </summary>
        public bool IsValid()
        {
            // ノードが存在しない場合、無効
            if (Nodes.Count == 0)
            {
                Debug.LogWarning("MissionPlan: No nodes defined");
                return false;
            }

            // レッグの数は (ノード数 - 1) である必要がある
            if (Legs.Count != Nodes.Count - 1 && Nodes.Count > 1)
            {
                Debug.LogWarning($"MissionPlan: Leg count ({Legs.Count}) does not match node count ({Nodes.Count - 1})");
                return false;
            }

            // 各レッグが正しくノードを参照しているか確認
            for (int i = 0; i < Legs.Count && i < Nodes.Count - 1; i++)
            {
                var leg = Legs[i];
                var fromNode = Nodes[i];
                var toNode = Nodes[i + 1];

                // nullチェック
                if (leg.FromNode == null || leg.ToNode == null)
                {
                    Debug.LogWarning($"MissionPlan: Leg[{i}] has null FromNode or ToNode");
                    return false;
                }

                // 参照の整合性チェック
                if (leg.FromNode != fromNode || leg.ToNode != toNode)
                {
                    Debug.LogWarning($"MissionPlan: Leg[{i}] does not properly connect Nodes[{i}] to Nodes[{i + 1}]");
                    return false;
                }
            }

            // 時間の整合性を確認（各ノードの到着時間と出発時間が正しい順序か）
            for (int i = 0; i < Nodes.Count - 1; i++)
            {
                var currentNode = Nodes[i];
                var nextNode = Nodes[i + 1];

                if (i < Legs.Count)
                {
                    var leg = Legs[i];

                    // 現在ノードの出発時間 + レッグの移動時間 = 次のノードの到着時間
                    double expectedArrival = currentNode.DepartureTime + leg.TravelTime;

                    if (Math.Abs(nextNode.ArrivalTime - expectedArrival) > 1.0) // 1秒の誤差を許容
                    {
                        Debug.LogWarning($"MissionPlan: Time mismatch at Node[{i + 1}]. Expected arrival: {expectedArrival}, Actual: {nextNode.ArrivalTime}");
                        // 警告のみで、無効にはしない（柔軟性のため）
                    }
                }
            }

            return true;
        }
    }
}

