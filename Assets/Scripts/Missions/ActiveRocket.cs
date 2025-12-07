using UnityEngine;
using SpaceLogistics.Rocketry;
using SpaceLogistics.Core;
using SpaceLogistics.Space;

namespace SpaceLogistics.Missions
{
    public enum RocketState
    {
        Local_Ascending, // 上昇中（ローカルマップ）
        Global_Transit,  // 惑星間移動中（グローバルマップ）
        Local_Orbiting,  // 周回中（ローカルマップ）
        Landed,          // 着陸済み
        Docked           // ドッキング済み
    }

    /// <summary>
    /// 実際に宇宙空間を運行中のロケットを制御するクラス。
    /// 状態遷移と位置更新、視覚的な表示切り替えを行う。
    /// </summary>
    public class ActiveRocket : MonoBehaviour
    {
        [Header("Data")]
        public RocketBlueprint Blueprint;
        public Route AssignedRoute;
        
        [Header("State")]
        public RocketState State;
        public double LaunchTime;
        public double ArrivalTime;

        [Header("Visuals")]
        public TrailRenderer Trail;
        public SpriteRenderer Icon;

        private void Start()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnTick += OnTimeTick;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnTick -= OnTimeTick;
        }

        private void OnTimeTick(float deltaTime)
        {
            UpdatePosition(TimeManager.Instance.UniverseTime);
        }

        /// <summary>
        /// ロケットを発射し、ミッションを開始する。
        /// </summary>
        /// <param name="time">発射時刻</param>
        public void Launch(double time)
        {
            LaunchTime = time;
            ArrivalTime = time + AssignedRoute.TotalDuration;
            State = RocketState.Local_Ascending; // 上昇開始
        }

        /// <summary>
        /// 現在時刻に基づいてロケットの位置と状態を更新する。
        /// </summary>
        /// <param name="time">現在の宇宙時間</param>
        public void UpdatePosition(double time)
        {
            // 簡易ステートマシン
            
            if (State == RocketState.Local_Ascending)
            {
                // 数秒間上昇演出を行った後、グローバル移動へ移行する
                if (time - LaunchTime > 10.0f) // 10秒間上昇
                {
                    State = RocketState.Global_Transit;
                }
                
                // 出発地がアクティブなローカルマップなら表示する
                if (MapManager.Instance.ActiveLocalBody == AssignedRoute.Origin.GetComponentInParent<CelestialBody>())
                {
                   // 上昇移動
                   transform.position += Vector3.up * (float)(time - LaunchTime) * 0.1f; 
                   SetVisuals(true);
                }
                else
                {
                    SetVisuals(false);
                }
            }
            else if (State == RocketState.Global_Transit)
            {
                if (time >= ArrivalTime)
                {
                    State = RocketState.Local_Orbiting;
                    // 到着イベントがあればここで発火
                    return;
                }

                if (GameManager.Instance.CurrentState == GameState.GlobalMap)
                {
                    // 惑星間を線形補間（Lerp）で移動する
                    double totalDuration = ArrivalTime - LaunchTime;
                    double elapsed = time - LaunchTime;
                    float progress = (float)(elapsed / totalDuration);

                    Vector3 startPos = AssignedRoute.Origin.GetComponentInParent<CelestialBody>().GetGlobalPosition(time);
                    Vector3 endPos = AssignedRoute.Destination.GetComponentInParent<CelestialBody>().GetGlobalPosition(time);
                    
                    // 単純な線形移動
                    Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
                    
                    // グローバルスケールを適用
                    transform.position = currentPos * MapManager.Instance.GlobalViewLogScale;
                    SetVisuals(true);
                }
                else
                {
                    SetVisuals(false);
                }
            }
            else if (State == RocketState.Local_Orbiting)
            {
                // 目的地での周回
                 if (MapManager.Instance.ActiveLocalBody == AssignedRoute.Destination.GetComponentInParent<CelestialBody>())
                {
                   // 簡易的な周回表示
                   transform.position = MapManager.Instance.ActiveLocalBody.transform.position + new Vector3(2, 2, 0); 
                   SetVisuals(true);
                }
                else
                {
                    SetVisuals(false);
                }
            }
        }

        private void SetVisuals(bool active)
        {
            if (Icon != null) Icon.enabled = active;
            if (Trail != null) Trail.enabled = active;
        }
    }
}
