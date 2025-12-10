using UnityEngine;
using SpaceLogistics.Rocketry;
using SpaceLogistics.Core;
using SpaceLogistics.Space;

namespace SpaceLogistics.Missions
{
    public enum RocketState
    {
        Ready,           // 発射準備完了
        InFlight,        // 飛行中 (FlightPlanに従う)
        Landed,          // 着陸済み
        Docked           // ドッキング済み
    }

    /// <summary>
    /// 実際に宇宙空間を運行中のロケットを制御するクラス。
    /// FlightPlanに基づいて位置を更新する。
    /// </summary>
    public class ActiveRocket : MonoBehaviour
    {
        [Header("Data")]
        public RocketBlueprint Blueprint;
        public Route AssignedRoute;
        
        [Header("Flight Plan")]
        public FlightPlan CurrentFlightPlan;
        
        [Header("State")]
        public RocketState State = RocketState.Ready;
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
            if (State == RocketState.InFlight)
            {
                UpdatePosition(TimeManager.Instance.UniverseTime);
            }
        }

        public void Launch(double time)
        {
            LaunchTime = time;
            State = RocketState.InFlight;
            
            if (AssignedRoute != null)
            {
                var originBody = AssignedRoute.Origin.GetComponentInParent<CelestialBody>();
                var destBody = AssignedRoute.Destination.GetComponentInParent<CelestialBody>();
                
                // MissionBuilderを使ってFlightPlanを生成
                // 現在は Earth -> Moon 前提のロジックだが、将来的にはMissionBuilder内で分岐
                if (destBody.ParentBody == originBody || originBody.ParentBody == destBody || destBody.ParentBody == originBody.ParentBody)
                {
                    // 同一系内、あるいは親子関係 (Moon Mission含む)
                    CurrentFlightPlan = MissionBuilder.CreateEarthToMoonPlan(originBody, destBody, time);
                    
                    // 到着予想時刻の取得（最後のセグメントの終了時間）
                    if (CurrentFlightPlan.Segments.Count > 0)
                    {
                        ArrivalTime = CurrentFlightPlan.Segments[CurrentFlightPlan.Segments.Count - 1].EndTime;
                    }
                }
                else
                {
                    // 複雑な惑星間移動などは未実装のため、空のプランなどにするか、簡易版を作る
                    Debug.LogWarning("Interplanetary Mission Builder requires expansion.");
                }
            }
        }

        public void UpdatePosition(double time)
        {
            if (CurrentFlightPlan == null) return;

            // 1. FlightPlanから現在の状態を取得
            var result = CurrentFlightPlan.Evaluate(time);
            OrbitalState state = result.state;
            CelestialBody referenceBody = result.currentRef;
            
            if (referenceBody == null) return;

            // 2. 座標系の解決と表示位置の決定
            Vector3 finalPosition = Vector3.zero;
            bool showVisuals = true;

            if (GameManager.Instance.CurrentState == GameState.GlobalMap)
            {
                // Global Map: 
                // 基準天体のGlobal Position + 相対位置 (スケール調整)
                // ローカルの移動距離(state.Position)は、GlobalScaleに合わせて縮小する必要があるか？
                // SystemRootのGlobalPosition + LocalPosition * GlobalScale
                
                var systemRoot = referenceBody.GetSystemRoot();
                Vector3 rootGlobalPos = systemRoot.GetGlobalPosition(time);
                
                // ReferenceBodyがSystemRootでない場合（例：月）、
                // ReferenceBody自体のLocalPositionも加算する必要がある。
                // GlobalMapでは「恒星間」が見えるレベルなので、System内の細かい動きは見えなくて良いかもしれないが、
                // 一応計算する。
                
                Vector3 refBodyOffset = Vector3.zero;
                if (referenceBody != systemRoot)
                {
                    refBodyOffset = referenceBody.GetLocalPosition(time);
                }

                // 全てをGlobalScaleで合成
                // GlobalViewLogScale は MapManager で管理されている
                float gScale = MapManager.Instance.GlobalViewLogScale;
                
                finalPosition = (rootGlobalPos + refBodyOffset + state.Position) * gScale; 
                
                // Global Mapでは、アイコンを少し大きくしたりする処理が必要かも
            }
            else // Local Map
            {
                var activeCamBody = MapManager.Instance.ActiveLocalBody;

                if (activeCamBody == referenceBody)
                {
                    // ケース1: カメラ基準天体 = ロケット基準天体 (例: 地球を見ていて、ロケットも地球周回)
                    finalPosition = state.Position;
                }
                else if (referenceBody.ParentBody == activeCamBody)
                {
                    // ケース2: ロケットは衛星(月)基準だが、カメラは親(地球)を見ている
                    // ロケット位置 = 月の位置(Active基準) + ロケット相対位置
                    finalPosition = referenceBody.GetLocalPosition(time) + state.Position;
                }
                else if (activeCamBody.ParentBody == referenceBody)
                {
                    // ケース3: ロケットは親(地球)基準だが、カメラは衛星(月)を見ている
                    // ロケット位置(Active基準) = ロケット位置(Ref基準) - カメラ中心位置(Ref基準)
                    finalPosition = state.Position - activeCamBody.GetLocalPosition(time);
                }
                else
                {
                    // ケース4: 全く関係ない場所にいる (例: 火星を見ているがロケットは地球)
                    // 表示しない
                    showVisuals = false;
                }
            }

            // 3. 適用
            if (showVisuals)
            {
                transform.position = finalPosition;
                SetVisuals(true);

                // 向きの更新 (速度ベクトルの方向へ)
                if (state.Velocity.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(state.Velocity.y, state.Velocity.x) * Mathf.Rad2Deg;
                    // ロケットの絵が右向きなら angle、上向きなら angle - 90
                    // Spriteの向きによる。一旦そのまま。
                    transform.rotation = Quaternion.Euler(0, 0, angle - 90); 
                }
            }
            else
            {
                SetVisuals(false);
            }
        }

        private void SetVisuals(bool active)
        {
            if (Icon != null) Icon.enabled = active;
            if (Trail != null) Trail.enabled = active;
        }
    }
}
