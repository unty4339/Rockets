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
            var root = originBody.GetSystemRoot();
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

            State = RocketState.Local_Ascending;

            double totalDuration = ArrivalTime - LaunchTime;
            double elapsed = time - LaunchTime;
            double progress = elapsed / totalDuration;
            
            if (progress >= 1.0)
            {
                State = RocketState.Local_Orbiting;
                // 到着時の周回軌道 (簡易)
                // 半径をToKilometers等で計算すべきだが、ここでは簡易的に
                // 1000km = 1 unit
                transform.position = destBody.GetLocalPosition(time) + Vector3.up * (float)(destBody.Radius.ToKilometers() / 1000.0 * 1.5 + 1.0); 
                SetVisuals(true);
                return;
            }

            // --- パッチドコニックス近似 (Hohmann Transfer like) ---

            // 1. 各天体の絶対軌道半径を取得 (LocalPositionのMagnitude)
            Vector3 originPosStart = originBody.GetLocalPosition(LaunchTime);
            Vector3 destPosEnd = destBody.GetLocalPosition(ArrivalTime);

            double r1 = originPosStart.magnitude;
            double r2 = destPosEnd.magnitude;

            // 中心から近すぎる場合 (中心天体からの発射など) は最小半径を確保
            if (r1 < 0.1) r1 = 0.5; // 仮のParking Orbit Radius
            if (r2 < 0.1) r2 = 0.5;

            // 2. 遷移軌道のパラメータ計算 (楕円)
            double majorAxis = r1 + r2;
            double a = majorAxis / 2.0; // 半長軸
            double e = System.Math.Abs(r2 - r1) / majorAxis; // 離心率

            // 3. 進行度から現在の半径 r を計算
            // Progress 0.0 -> 近点 (or 遠点), 1.0 -> 遠点 (or 近点)
            // 真近点角 nu : 0 -> PI
            double nu = progress * System.Math.PI;
            
            double r;
            if (r1 < r2) // 外向き
            {
                r = a * (1 - e * e) / (1 + e * System.Math.Cos(nu));
            }
            else // 内向き
            {
                // 内向きの場合、r1(始点)が遠点
                r = a * (1 - e * e) / (1 - e * System.Math.Cos(nu)); 
            }

            // 4. 角度の補間
            float startAngle = Mathf.Atan2(originPosStart.y, originPosStart.x) * Mathf.Rad2Deg;
            float endAngle = Mathf.Atan2(destPosEnd.y, destPosEnd.x) * Mathf.Rad2Deg;
            
            float currentAngleDeg = Mathf.LerpAngle(startAngle, endAngle, (float)progress);
            float currentAngleRad = currentAngleDeg * Mathf.Deg2Rad;

            // 5. 親天体中心の座標 (Hohmann Path)
            float x = (float)(r * System.Math.Cos(currentAngleRad));
            float y = (float)(r * System.Math.Sin(currentAngleRad));
            Vector3 transferPos = new Vector3(x, y, 0);

            // 6. SOI (重力圏) ブレンド
            // 目的地の現在の位置
            Vector3 currentDestPos = destBody.GetLocalPosition(time);
            float distToDest = Vector3.Distance(transferPos, currentDestPos);
            
            // VisualSOIRadiusを使用
            float visualSOI = destBody.VisualSOIRadius;

            if (distToDest < visualSOI)
            {
                // SOI内部: 目的地中心の座標系へ遷移
                Vector3 approachDir = (transferPos - currentDestPos).normalized;
                // 最終的に近づく距離 (周回半径) = SOIの半分くらいまで寄る
                float orbitRad = visualSOI * 0.5f;
                Vector3 finalOrbitPos = currentDestPos + approachDir * orbitRad;
                
                // ブレンド率
                float blend = 1.0f - (distToDest / visualSOI);
                blend = Mathf.Clamp01(blend);
                blend = blend * blend * (3f - 2f * blend); // SmoothStep

                transform.position = Vector3.Lerp(transferPos, finalOrbitPos, blend);
            }
            else
            {
                transform.position = transferPos;
            }
            
            SetVisuals(true);
        }

        private void SetVisuals(bool active)
        {
            if (Icon != null) Icon.enabled = active;
            if (Trail != null) Trail.enabled = active;
        }
    }
}
