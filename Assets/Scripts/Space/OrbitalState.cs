using UnityEngine;

namespace SpaceLogistics.Space
{
    /// <summary>
    /// ある時刻における軌道上の状態（位置・速度）を表す構造体。
    /// </summary>
    [System.Serializable]
    public struct OrbitalState
    {
        public Vector3 Position; // 基準天体からの相対位置 (Meters)
        public Vector3 Velocity; // 基準天体からの相対速度 (Meters / s)
        public double Time;      // この状態の時刻 (Universe Time [s])

        public OrbitalState(Vector3 position, Vector3 velocity, double time)
        {
            Position = position;
            Velocity = velocity;
            Time = time;
        }
    }
}
