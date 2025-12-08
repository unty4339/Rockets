using UnityEngine;
using SpaceLogistics.Rocketry;
using SpaceLogistics.Core;
using SpaceLogistics.Space;

namespace SpaceLogistics.Missions
{
    public enum RocketState
    {
        Ready,           // 発射準備完了
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
        public RocketState State = RocketState.Ready;
        public double LaunchTime;
        public double ArrivalTime;
        public bool IsGlobalMission;

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

        public void Launch(double time)
        {
            LaunchTime = time;
            State = RocketState.Local_Ascending;
            
            // ミッション完了時間を適当に設定（デバッグ用）
            // 実際はルートのデータを使う
            if (AssignedRoute != null)
            {
                // テスト: DeltaVに応じて時間を変えるか、RouteにDurationを持たせる
                // Routeのルート天体を比較してミッションタイプを判定
                var originRoot = AssignedRoute.Origin.GetComponentInParent<CelestialBody>().GetSystemRoot();
                var destRoot = AssignedRoute.Destination.GetComponentInParent<CelestialBody>().GetSystemRoot();
                
                IsGlobalMission = (originRoot != destRoot);
                
                double duration = IsGlobalMission ? 20.0 : 10.0; // グローバルは長め
                ArrivalTime = time + duration;
            }
        }

        public void UpdatePosition(double time)
        {
            if (IsGlobalMission)
            {
                UpdatePositionInterPlanetary(time);
            }
            else
            {
                UpdatePositionIntraPlanetary(time);
            }
        }

        // ==========================================
        // 惑星間移動 (Global Mission)
        // ==========================================
        private void UpdatePositionInterPlanetary(double time)
        {
            bool isGlobalMap = GameManager.Instance.CurrentState == GameState.GlobalMap;
            var activeBody = MapManager.Instance.ActiveLocalBody;
            var originBody = AssignedRoute.Origin.GetComponentInParent<CelestialBody>();
            var originRoot = originBody.GetSystemRoot();
            var destBody = AssignedRoute.Destination.GetComponentInParent<CelestialBody>();
            var destRoot = destBody.GetSystemRoot();

            // フェーズ計算 (Ascent: 30%, Transit: 40%, Descent: 30% とする)
            // 実際はもっと時間に基づくべきだが簡略化
            double totalDuration = ArrivalTime - LaunchTime;
            double elapsed = time - LaunchTime;
            double progress = elapsed / totalDuration; // 0.0 - 1.0
            
            if (progress >= 1.0)
            {
                State = RocketState.Local_Orbiting;
                // 目的地に固定
                if (isGlobalMap)
                {
                    transform.position = destBody.GetGlobalPosition(time) * MapManager.Instance.GlobalViewLogScale;
                    SetVisuals(true);
                }
                else
                {
                    if (activeBody == destRoot)
                    {
                        // 目的地のローカルマップ内
                        transform.position = destBody.GetLocalPosition(time) + Vector3.up * 2.0f; // 簡易オービット
                        SetVisuals(true);
                    }
                    else
                    {
                        SetVisuals(false);
                    }
                }
                return;
            }

            // --- Phase判定 ---
            
            // Ascent (0% - 20%): 出発ローカルマップ端へ
            if (progress < 0.2)
            {
                State = RocketState.Local_Ascending;
                double phaseProg = progress / 0.2;
                
                if (isGlobalMap)
                {
                    // GlobalMapでは出発惑星のアイコン上
                     transform.position = originRoot.GetGlobalPosition(time) * MapManager.Instance.GlobalViewLogScale;
                     SetVisuals(true);
                }
                else if (activeBody == originRoot)
                {
                    // LocalMap: 中心から端（radius）へ移動
                    // OriginBodyの位置からスタートし、Systemsの端へ向かって飛ぶ
                    // 簡易的に (1,1,0) 方向へ飛ばす
                    Vector3 startPos = originBody.GetLocalPosition(time);
                    Vector3 exitDir = (startPos + new Vector3(1, 1, 0)).normalized; 
                    float exitDist = originRoot.LocalMapRadius;
                    
                    transform.position = Vector3.Lerp(startPos, exitDir * exitDist, (float)phaseProg);
                    SetVisuals(true);
                }
                else
                {
                    SetVisuals(false);
                }
            }
            // Transit (20% - 80%): 惑星間移動
            else if (progress < 0.8)
            {
                State = RocketState.Global_Transit;
                double phaseProg = (progress - 0.2) / 0.6;
                
                if (isGlobalMap)
                {
                    Vector3 startG = originRoot.GetGlobalPosition(time);
                    Vector3 endG = destRoot.GetGlobalPosition(time);
                    transform.position = Vector3.Lerp(startG, endG, (float)phaseProg) * MapManager.Instance.GlobalViewLogScale;
                    SetVisuals(true);
                }
                else
                {
                    // ローカルマップでは見えない (宇宙空間)
                    SetVisuals(false);
                }
            }
            // Descent (80% - 100%): 到着ローカルマップ端から目的地へ
            else
            {
                State = RocketState.Global_Transit; // 着陸まではTransit扱いで
                double phaseProg = (progress - 0.8) / 0.2;

                if (isGlobalMap)
                {
                     transform.position = destRoot.GetGlobalPosition(time) * MapManager.Instance.GlobalViewLogScale;
                     SetVisuals(true);
                }
                else if (activeBody == destRoot)
                {
                    // LocalMap: 端から目的地へ
                    Vector3 targetPos = destBody.GetLocalPosition(time) + Vector3.up * 2.0f; // 目標周回軌道
                    // 入ってくる方向（適当）
                    Vector3 entryDir = (targetPos + new Vector3(-1, 1, 0)).normalized;
                    float entryDist = destRoot.LocalMapRadius;
                    
                    transform.position = Vector3.Lerp(entryDir * entryDist, targetPos, (float)phaseProg);
                    SetVisuals(true);
                }
                else
                {
                    SetVisuals(false);
                }
            }
        }

        // ==========================================
        // 惑星内移動 (Local Mission) - e.g. Earth -> Moon
        // ==========================================
        private void UpdatePositionIntraPlanetary(double time)
        {
            var activeBody = MapManager.Instance.ActiveLocalBody;
            var originBody = AssignedRoute.Origin.GetComponentInParent<CelestialBody>();
            var root = originBody.GetSystemRoot(); // LocalなのでOriginもDestも同じRoot
            var destBody = AssignedRoute.Destination.GetComponentInParent<CelestialBody>();
            
            // GlobalMapでは、この系の位置にずっといる
            if (GameManager.Instance.CurrentState == GameState.GlobalMap)
            {
                transform.position = root.GetGlobalPosition(time) * MapManager.Instance.GlobalViewLogScale;
                SetVisuals(true);
                return;
            }
            
            // LocalMap: Rootが表示されている場合のみ描画
            if (activeBody != root)
            {
                SetVisuals(false);
                return;
            }

            State = RocketState.Local_Ascending; // 便宜上

            double totalDuration = ArrivalTime - LaunchTime;
            double elapsed = time - LaunchTime;
            double progress = elapsed / totalDuration;

            if (progress >= 1.0)
            {
                State = RocketState.Local_Orbiting;
                transform.position = destBody.GetLocalPosition(time) + Vector3.up * 1.0f;
                SetVisuals(true);
                return;
            }
            
            // ホーマン遷移風の曲線移動
            Vector3 p1 = originBody.GetLocalPosition(time);
            Vector3 p2 = destBody.GetLocalPosition(time);
            
            Vector3 currentPos;
            
            if (p1.magnitude < 0.1f) // Center -> Satellite
            {
                // 直線的、かつスパイラルっぽく
                Vector3 targetDir = p2.normalized;
                // 進行度に応じて角度をずらす (渦巻き)
                float angleOffset = (1.0f - (float)progress) * 90.0f * Mathf.Deg2Rad; 
                float x = Mathf.Cos(angleOffset) * targetDir.x - Mathf.Sin(angleOffset) * targetDir.y;
                float y = Mathf.Sin(angleOffset) * targetDir.x + Mathf.Cos(angleOffset) * targetDir.y;
                Vector3 spiralDir = new Vector3(x, y, 0);
                
                currentPos = spiralDir * (p2.magnitude * (float)progress);
            }
            else if (p2.magnitude < 0.1f) // Satellite -> Center
            {
                // 逆渦巻き
                 currentPos = Vector3.Lerp(p1, p2, (float)progress);
            }
            else // Sat -> Sat
            {
                currentPos = Vector3.Slerp(p1, p2, (float)progress);
                float r1 = p1.magnitude;
                float r2 = p2.magnitude;
                float currentR = Mathf.Lerp(r1, r2, (float)progress);
                currentPos = currentPos.normalized * currentR;
            }
            
            transform.position = currentPos;
            SetVisuals(true);
        }

        private void SetVisuals(bool active)
        {
            if (Icon != null) Icon.enabled = active;
            if (Trail != null) Trail.enabled = active;
        }
    }
}
