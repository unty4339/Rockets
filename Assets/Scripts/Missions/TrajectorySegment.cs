using UnityEngine;

namespace SpaceLogistics.Space
{
    [System.Serializable]
    public class TrajectorySegment
    {
        public ITrajectory Trajectory;
        public double StartTime;
        public double EndTime;

        public TrajectorySegment(ITrajectory trajectory)
        {
            Trajectory = trajectory;
            // ITrajectoryから時間を取得
            StartTime = trajectory.StartTime;
            EndTime = trajectory.EndTime;
        }

        public TrajectorySegment(ITrajectory trajectory, double start, double end)
        {
            Trajectory = trajectory;
            StartTime = start;
            EndTime = end;
        }

        public OrbitalState Evaluate(double time)
        {
            return Trajectory.Evaluate(time);
        }
    }
}
