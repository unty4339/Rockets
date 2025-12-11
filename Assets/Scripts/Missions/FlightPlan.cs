using System.Collections.Generic;
using UnityEngine;
using SpaceLogistics.Space;

namespace SpaceLogistics.Missions
{
    [System.Serializable]
    public class FlightPlan
    {
        public List<TrajectorySegment> Segments = new List<TrajectorySegment>();

        public void AddSegment(TrajectorySegment segment)
        {
            Segments.Add(segment);
            // 時間順にソートするロジックを入れても良いが、構築順に追加されると仮定
        }

        /// <summary>
        /// 指定時刻におけるロケットの状態と、その時点での基準天体を返す。
        /// </summary>
        public (OrbitalState state, CelestialBody currentRef) Evaluate(double time)
        {
            // セグメントがない場合
            if (Segments.Count == 0) return (default(OrbitalState), null);

            // 該当するセグメントを探す
            TrajectorySegment activeSegment = null;
            
            // 範囲外の場合の挙動:
            // 開始前 -> 最初のセグメントの開始状態
            // 終了後 -> 最後のセグメントの終了状態
            if (time < Segments[0].StartTime)
            {
                activeSegment = Segments[0];
                time = activeSegment.StartTime; // Clamp time
            }
            else if (time > Segments[Segments.Count - 1].EndTime)
            {
                activeSegment = Segments[Segments.Count - 1];
                time = activeSegment.EndTime; // Clamp time
            }
            else
            {
                // 範囲内検索
                foreach (var seg in Segments)
                {
                    if (time >= seg.StartTime && time <= seg.EndTime)
                    {
                        activeSegment = seg;
                        break;
                    }
                }

                // 隙間（ギャップ）等の理由で見つからない場合、直近の過去セグメントを使うなど
                if (activeSegment == null)
                {
                    // 簡易的に最後のセグメントを返すか、エラーとするか
                    // とりあえず時間を超えているとみなして最後のセグメントを返す
                     activeSegment = Segments[Segments.Count - 1];
                }
            }

            return (activeSegment.Evaluate(time), activeSegment.Trajectory.ReferenceBody);
        }
    }
}
