using UnityEngine;

namespace SpaceLogistics.Space
{
    [System.Serializable]
    public class TrajectorySegment
    {
        public ITrajectory Trajectory;
        // public Maneuver StartManeuver; // 将来的にマニューバ情報を追加
        
        public double StartTime => Trajectory.StartTime;
        public double EndTime => Trajectory.EndTime;
        public double Duration => EndTime - StartTime;

        public TrajectorySegment(ITrajectory trajectory)
        {
            Trajectory = trajectory;
        }
    }
}
