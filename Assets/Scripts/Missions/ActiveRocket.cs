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
            // 現在のモードを取得
            bool isGlobalMap = GameManager.Instance.CurrentState == GameState.GlobalMap;

            // 1. Global Map Mode
            if (isGlobalMap)
            {
                UpdateGlobalPosition(time);
                return;
            }
            
            // 2. Local Map Mode
            // ロケットがいる場所（AssignedRoute.Origin か Destination, あるいは Transit中）と、
            // 表示中の ActiveLocalBody が一致するかどうかで表示を切り替える。
            
            var activeBody = MapManager.Instance.ActiveLocalBody;
            
            if (State == RocketState.Local_Ascending)
            {
                // 出発地にいる
                var origin = AssignedRoute.Origin.GetComponentInParent<CelestialBody>();
                if (origin == activeBody)
                {
                    // 中心天体から離れていく演出
                    // 中心天体は(0,0)にいるので、単純にUp方向へ
                    transform.position = Vector3.up * (2.0f + (float)(time - LaunchTime) * 0.5f);
                    SetVisuals(true);
                }
                else
                {
                    SetVisuals(false);
                }
            }
            else if (State == RocketState.Local_Orbiting)
            {
                 // 目的地にいる
                 var dest = AssignedRoute.Destination.GetComponentInParent<CelestialBody>();
                 if (dest == activeBody)
                 {
                     // 中心天体(0,0)の周りを回る演出（固定位置）
                     transform.position = new Vector3(2, 2, 0);
                     SetVisuals(true);
                 }
                 else
                 {
                     SetVisuals(false);
                 }
            }
            else if (State == RocketState.Global_Transit)
            {
                // ローカルマップでは、トランジット中のロケットは見えない（あるいは出発/到着付近なら見えるかもだが、基本非表示）
                SetVisuals(false);
            }
            
            // 状態遷移ロジック
            if (State == RocketState.Local_Ascending && time - LaunchTime > 5.0f)
            {
                State = RocketState.Global_Transit;
            }
            if (State == RocketState.Global_Transit && time >= ArrivalTime)
            {
                State = RocketState.Local_Orbiting;
            }
        }

        private void UpdateGlobalPosition(double time)
        {
            // グローバルマップでの表示
            
            if (State == RocketState.Local_Ascending)
            {
                // 出発地の座標
                var origin = AssignedRoute.Origin.GetComponentInParent<CelestialBody>();
                transform.position = origin.GetGlobalPosition(time); 
                SetVisuals(true);
            }
            else if (State == RocketState.Global_Transit)
            {
                double totalDuration = ArrivalTime - LaunchTime;
                double elapsed = time - LaunchTime;
                float progress = (float)(elapsed / totalDuration); // 0.0 - 1.0

                var origin = AssignedRoute.Origin.GetComponentInParent<CelestialBody>();
                var dest = AssignedRoute.Destination.GetComponentInParent<CelestialBody>();

                Vector3 startPos = origin.GetGlobalPosition(time);
                Vector3 endPos = dest.GetGlobalPosition(time);
                
                // アイコン間を直線移動
                transform.position = Vector3.Lerp(startPos, endPos, progress);
                SetVisuals(true);
            }
            else if (State == RocketState.Local_Orbiting)
            {
                // 到着地の座標
                var dest = AssignedRoute.Destination.GetComponentInParent<CelestialBody>();
                transform.position = dest.GetGlobalPosition(time);
                SetVisuals(true);
            }
        }

        private void SetVisuals(bool active)
        {
            if (Icon != null) Icon.enabled = active;
            if (Trail != null) Trail.enabled = active;
        }
    }
}
