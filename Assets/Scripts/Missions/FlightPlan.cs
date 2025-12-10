using System.Collections.Generic;
using SpaceLogistics.Space;
using UnityEngine;

namespace SpaceLogistics.Missions
{
    [System.Serializable]
    public class FlightPlan
    {
        public List<TrajectorySegment> Segments = new List<TrajectorySegment>();
        
        /// <summary>
        /// 指定時刻の状態と、その時点の基準天体を取得する。
        /// </summary>
        public (OrbitalState state, CelestialBody currentRef) Evaluate(double time)
        {
            if (Segments.Count == 0)
            {
                // 空の場合はデフォルト値（またはエラー扱い）
                return (new OrbitalState(), null);
            }

            // 1. time が含まれる Segment を検索
            // 単純なリニアサーチ（セグメント数は少ない想定）
            TrajectorySegment activeSegment = null;
            
            // 範囲外チェック: 開始前なら最初のセグメントの開始時
            if (time < Segments[0].StartTime)
            {
                activeSegment = Segments[0];
                time = activeSegment.StartTime; // クランプ
            }
            // 終了後なら最後のセグメントの終了時
            else if (time > Segments[Segments.Count - 1].EndTime)
            {
                activeSegment = Segments[Segments.Count - 1];
                time = activeSegment.EndTime; // クランプ
            }
            else
            {
                foreach (var seg in Segments)
                {
                    if (time >= seg.StartTime && time <= seg.EndTime)
                    {
                        activeSegment = seg;
                        break;
                    }
                }
            }
            
            // ギャップなどで見つからない場合のフォールバック（直近の過去を探すなど）
            if (activeSegment == null)
            {
                 // 暫定：最後のセグメントを返す
                 activeSegment = Segments[Segments.Count - 1];
            }

            // 2. Evaluate
            OrbitalState state = activeSegment.Trajectory.Evaluate(time);
            
            // 3. Return
            return (state, activeSegment.Trajectory.ReferenceBody);
        }

        public void AddSegment(TrajectorySegment segment)
        {
            Segments.Add(segment);
            // 必要ならソートなどをここで行う
        }
    }
}
