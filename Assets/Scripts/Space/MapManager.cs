using System.Collections.Generic;
using UnityEngine;
using SpaceLogistics.Core;

namespace SpaceLogistics.Space
{
    /// <summary>
    /// マップの表示モード（ローカル/グローバル）と天体の描画を管理するクラス。
    /// 現在のモードに応じてカメラ設定や天体の位置/スケールを更新する。
    /// </summary>
    /// <summary>
    /// マップの表示モード（ローカル/グローバル）と天体の描画を管理するクラス。
    /// 現在のモードに応じてカメラ設定や天体の位置/スケールを更新する。
    /// </summary>
    public class MapManager : SingletonMonoBehaviour<MapManager>
    {
        // Instance property inherited

        [Header("References")]
        public Camera MainCamera;
        public List<CelestialBody> AllBodies = new List<CelestialBody>();
        
        [Header("Settings")]
        public float GlobalViewLogScale = 1.0f; // テスト用に等倍(1.0)に変更。本来は0.001fなどで広大な宇宙を表現する。
        public CelestialBody ActiveLocalBody; // ローカルビューでの中心天体

        // 1 Unity Unit = 1,000 km = 1,000,000 m
        public const float MapScale = 1e-6f;

        // Awake removed as base handles logic

        private void Start()
        {
            // 状態変更イベントの購読
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            
            // カメラ操作スクリプトの自動追加
            if (MainCamera != null)
            {
                var camCtrl = MainCamera.GetComponent<UI.CameraControls>();
                if (camCtrl == null) MainCamera.gameObject.AddComponent<UI.CameraControls>();
            }

            // リストが空なら自動的に天体を検索する
            if (AllBodies.Count == 0)
            {
                AllBodies.AddRange(FindObjectsByType<CelestialBody>(FindObjectsSortMode.None));
            }
        }

        // ...

        private void Update()
        {
            if (TimeManager.Instance != null)
            {
                RenderVisuals(TimeManager.Instance.UniverseTime);
            }
        }

        private void HandleStateChanged(GameState newState)
        {
            if (newState == GameState.LocalMap)
            {
                // ローカルマップ: 自動フィット
                FitCameraToActiveSystem();
            }
            else if (newState == GameState.GlobalMap)
            {
                if (MainCamera != null) MainCamera.orthographicSize = 50; // グローバル用のデフォルトズーム
                if (MainCamera != null) MainCamera.transform.position = new Vector3(0, 0, -10); // リセット
            }
        }
        
        private void FitCameraToActiveSystem()
        {
            if (MainCamera == null || ActiveLocalBody == null) return;
            
            // 最大半径を探す
            float maxDist = ActiveLocalBody.LocalMapRadius; // デフォルト（例 50）
            
            // 子衛星を探す
            // Note: AllBodiesから検索する（階層構造がTransform通りとは限らないが、今回はTransformベースで探すか、OrbitDataで探す）
            // OrbitDataを持っているものはParentBodyが設定されている
            foreach (var body in AllBodies)
            {
                if (body.ParentBody == ActiveLocalBody)
                {
                    // 軌道半径 (m) * Scale
                    float dist = (float)(body.OrbitData.SemiMajorAxis * MapScale);
                    if (dist > maxDist) maxDist = dist;
                }
            }
            
            // 少し余裕を持たせる
            float targetSize = maxDist * 1.2f;
            MainCamera.orthographicSize = Mathf.Max(targetSize, 5f); // 最小5
            MainCamera.transform.position = new Vector3(0, 0, -10); // 中心リセット
        }

        /// <summary>
        /// 指定した天体を中心としたローカルマップへ切り替える。
        /// </summary>
        /// <param name="body">中心とする天体</param>
        public void SwitchToLocalMap(CelestialBody body)
        {
            ActiveLocalBody = body;
            GameManager.Instance.SetState(GameState.LocalMap);
        }

        /// <summary>
        /// グローバルマップ（星系図）へ切り替える。
        /// </summary>
        public void SwitchToGlobalMap()
        {
            GameManager.Instance.SetState(GameState.GlobalMap);
        }

        /// <summary>
        /// すべての天体の描画更新を行う。
        /// </summary>
        private void RenderVisuals(double time)
        {
            GameState state = GameManager.Instance.CurrentState;

            foreach (var body in AllBodies)
            {
                // 一旦すべて非表示にするのが安全、あるいは各メソッドで制御
                bool isVisible = false;
                body.SetSOIVisibility(false); // SOIはデフォルトで非表示

                if (state == GameState.LocalMap)
                {
                    isVisible = UpdateLocalMap(body, time);
                }
                else if (state == GameState.GlobalMap)
                {
                    isVisible = UpdateGlobalMap(body, time);
                }

                if (body.BodyRenderer != null) body.BodyRenderer.enabled = isVisible;
            }
            
            // カメラ追従は不要（中心固定のため、カメラも(0,0,-10)固定でOKだが、ズーム操作は残す）
            if (state == GameState.LocalMap && MainCamera != null)
            {
                // 常に原点を見る
                Vector3 targetPos = Vector3.zero; 
                targetPos.z = -10;
                MainCamera.transform.position = targetPos;
            }
        }

        private bool UpdateLocalMap(CelestialBody body, double time)
        {
            // 表示対象:
            // 1. ActiveLocalBody そのもの (中心)
            // 2. ActiveLocalBody の子 (衛星)
            
            if (body == ActiveLocalBody)
            {
                body.transform.position = Vector3.zero; // 中心不動
                body.transform.localScale = Vector3.one;
                if (body.BodyRenderer != null)
                {
                    body.BodyRenderer.transform.localScale = Vector3.one * body.VisualScaleLocal;
                }
                body.SetSOIVisibility(true); // 中心天体はSOIを表示
                return true;
            }
            else if (body.ParentBody == ActiveLocalBody)
            {
                // 親がActiveなので、GetLocalPositionで軌道位置を取得
                // OrbitParametersはメートル単位で計算されるため、MapScaleを掛けてUnity単位に変換
                Vector3 pos = body.GetLocalPosition(time);
                body.transform.position = pos * MapScale;
                
                body.transform.localScale = Vector3.one;
                if (body.BodyRenderer != null)
                {
                    body.BodyRenderer.transform.localScale = Vector3.one * body.VisualScaleLocal;
                }
                
                body.SetSOIVisibility(true); // 衛星もSOIを表示
                
                if (Time.frameCount % 200 == 0)
                {
                    Debug.Log($"[MapManager] {body.BodyName}: Time={time:F1}, PosMeters={pos}, n={body.OrbitData.MeanMotion:E2}, ScaledPos={body.transform.position}");
                }
                
                return true;
            }
            
            // それ以外は非表示
            return false;
        }

        private bool UpdateGlobalMap(CelestialBody body, double time)
        {
            // 表示対象:
            // 1. 親がいない天体 (太陽) ? 
            // 2. 主要な惑星 (Earth, Mars) -> これらはどこレベル？
            // 抽象マップの設計によるが、今回は「親がいない or 親が太陽」を表示とするか、
            // 単純に「AbstractGlobalPositionが設定されているもの」を表示とする。
            
            // 天体が持つAbstractGlobalPositionを使う
            // ただし、月などの衛星をグローバルマップでどうするか？通常は表示しないか、惑星に追従するアイコンにする。
            // 今回はシンプルに「主要惑星のみ表示」とする。
            // 判定基準: 親がSun、または親がnull
            
            bool isMajor = (body.ParentBody == null || body.ParentBody.BodyName == "Sun"); // 簡易判定
            
            if (isMajor)
            {
                body.transform.position = body.GetGlobalPosition(time);
                body.transform.localScale = Vector3.one;
                if (body.BodyRenderer != null)
                {
                    body.BodyRenderer.transform.localScale = Vector3.one * body.VisualScaleGlobal;
                }
                return true;
            }
            
            return false;
        }
    }
}
