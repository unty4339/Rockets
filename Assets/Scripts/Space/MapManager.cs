using System.Collections.Generic;
using UnityEngine;
using SpaceLogistics.Core;

namespace SpaceLogistics.Space
{
    /// <summary>
    /// マップの表示モード（ローカル/グローバル）と天体の描画を管理するクラス。
    /// 現在のモードに応じてカメラ設定や天体の位置/スケールを更新する。
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }

        [Header("References")]
        public Camera MainCamera;
        public List<CelestialBody> AllBodies = new List<CelestialBody>();
        
        [Header("Settings")]
        public float GlobalViewLogScale = 0.001f; // グローバル表示時の縮尺係数
        public CelestialBody ActiveLocalBody; // ローカルビューでの中心天体

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // 状態変更イベントの購読
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += HandleStateChanged;
                
            // リストが空なら自動的に天体を検索する
            if (AllBodies.Count == 0)
            {
                AllBodies.AddRange(FindObjectsByType<CelestialBody>(FindObjectsSortMode.None));
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void Update()
        {
            if (TimeManager.Instance == null) return;
            
            RenderVisuals(TimeManager.Instance.UniverseTime);
        }

        /// <summary>
        /// ゲーム状態の変更を処理する。
        /// カメラのズーム設定などをモードに合わせてリセットする。
        /// </summary>
        /// <param name="newState">新しいゲーム状態</param>
        private void HandleStateChanged(GameState newState)
        {
            if (newState == GameState.LocalMap)
            {
                if (MainCamera != null) MainCamera.orthographicSize = 10; // ローカル用のデフォルトズーム
            }
            else if (newState == GameState.GlobalMap)
            {
                if (MainCamera != null) MainCamera.orthographicSize = 50; // グローバル用のデフォルトズーム
            }
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
        /// <param name="time">現在の宇宙時間</param>
        private void RenderVisuals(double time)
        {
            GameState state = GameManager.Instance.CurrentState;

            foreach (var body in AllBodies)
            {
                if (state == GameState.LocalMap)
                {
                    UpdateLocalMap(body, time);
                }
                else if (state == GameState.GlobalMap)
                {
                    UpdateGlobalMap(body, time);
                }
            }
            
            // ローカルマップ時はActiveLocalBodyを中心にカメラを追従させる
            if (state == GameState.LocalMap && ActiveLocalBody != null)
            {
                if (MainCamera != null)
                {
                    Vector3 targetPos = ActiveLocalBody.transform.position;
                    targetPos.z = -10;
                    MainCamera.transform.position = targetPos;
                }
            }
        }

        /// <summary>
        /// ローカルマップモードでの天体更新。
        /// 物理的な位置関係に基づいて配置する。
        /// </summary>
        private void UpdateLocalMap(CelestialBody body, double time)
        {
            Vector3 localPos = body.GetLocalPosition(time);
            
            // 親がいる場合は親の位置を加算してワールド座標を決定する
            Vector3 parentPos = Vector3.zero;
            if (body.ParentBody != null)
            {
                parentPos = body.ParentBody.transform.position;
            }
            
            body.transform.position = parentPos + localPos;
            body.transform.localScale = Vector3.one * body.VisualScaleLocal;
            
            // レンダラーを有効化
            if (body.BodyRenderer != null) body.BodyRenderer.enabled = true;
        }

        /// <summary>
        /// グローバルマップモードでの天体更新。
        /// 縮小スケールを適用し、全体が見渡せるように配置する。
        /// </summary>
        private void UpdateGlobalMap(CelestialBody body, double time)
        {
            Vector3 globalPos = body.GetGlobalPosition(time);
            
            // グローバルスケール係数を適用
            body.transform.position = globalPos * GlobalViewLogScale;
            body.transform.localScale = Vector3.one * body.VisualScaleGlobal;
            
            if (body.BodyRenderer != null) body.BodyRenderer.enabled = true;
        }
    }
}
