using UnityEngine;
using SpaceLogistics.Core;
using System;

namespace SpaceLogistics.Space
{
    /// <summary>
    /// 惑星や衛星などの天体を表すクラス。
    /// 軌道データや物理パラメータ、表示用のスケーリングを管理する。
    /// </summary>
    public class CelestialBody : MonoBehaviour
    {
        [Header("Body Stats")]
        public string BodyName;
        public Mass Mass; // PhysicsTypes.Mass
        public Distance Radius; // PhysicsTypes.Distance
        public Distance SOIRadius; // PhysicsTypes.Distance
        public bool HasAtmosphere;

        [Header("Orbit Config")]
        public Distance LowOrbitAltitude; // PhysicsTypes.Distance

        [Header("Map Positions")]
        public Vector3 AbstractGlobalPosition; // グローバルマップでの定位置 (アイコン表示用)
        public float LocalMapRadius = 30.0f; // ローカルマップの表示限界半径 (Visual Scale)

        [Header("Orbit")]
        public CelestialBody ParentBody;
        public OrbitParameters OrbitData;
        public Color OrbitColor = Color.white;

        [Header("Visuals")]
        public SpriteRenderer BodyRenderer;

        
        // SOIの見た目上の半径 (Local Map)
        public float VisualSOIRadius
        {
            get
            {
                // 理論値計算をやめ、設定値をそのままUnityスケールに変換して返す
                return SOIRadius.ToUnityUnits();
            }
        }

        private LineRenderer _soiLineRenderer;

        // Inspector用フィールド
        [SerializeField] private double _massKg;
        [SerializeField] private double _radiusKm;
        [SerializeField] private double _soiRadiusKm;
        [SerializeField] private double _lowOrbitAltitudeKm;

        private void Awake()
        {
            Mass = new Mass(_massKg);
            Radius = Distance.FromKilometers(_radiusKm);
            SOIRadius = Distance.FromKilometers(_soiRadiusKm);
            LowOrbitAltitude = Distance.FromKilometers(_lowOrbitAltitudeKm);
        }

        private void OnValidate()
        {
            // Inspectorでの変更を反映 (Edit Mode用)
            Mass = new Mass(_massKg);
            Radius = Distance.FromKilometers(_radiusKm);
            SOIRadius = Distance.FromKilometers(_soiRadiusKm);
            LowOrbitAltitude = Distance.FromKilometers(_lowOrbitAltitudeKm);
        }

        private void Start()
        {
            if (BodyRenderer == null)
                BodyRenderer = GetComponentInChildren<SpriteRenderer>();



            // SOI表示用LineRendererのセットアップ
            _soiLineRenderer = GetComponent<LineRenderer>();
            if (_soiLineRenderer == null)
            {
                _soiLineRenderer = gameObject.AddComponent<LineRenderer>();
            }
            SetupSOIVisual();
        }
        


        /// <summary>
        /// この天体が属する惑星系のルート天体（ローカルマップの中心）を取得する。
        /// 例: 地球->地球, 月->地球, 火星->火星, フォボス->火星
        /// </summary>
        public CelestialBody GetSystemRoot()
        {
            // 親がいない、または親がSun（恒星）なら自分がルート
            if (ParentBody == null) return this;
            if (ParentBody.BodyName == "Sun") return this;
            
            // それ以外は親のルートを再帰的に返す
            return ParentBody.GetSystemRoot();
        }

        private void SetupSOIVisual()
        {
            _soiLineRenderer.useWorldSpace = false;
            _soiLineRenderer.loop = true;
            _soiLineRenderer.positionCount = 50;
            _soiLineRenderer.startWidth = 0.05f;
            _soiLineRenderer.endWidth = 0.05f;
            _soiLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _soiLineRenderer.startColor = new Color(1f, 1f, 1f, 0.2f);
            _soiLineRenderer.endColor = new Color(1f, 1f, 1f, 0.2f);
            
            // 半径計算
            float r = VisualSOIRadius;
            
            // 安全策: 小さすぎると見えないのでMinキャップ
            float bodyRadiusUnit = (float)(Radius.Meters * MapManager.MapScale);
            if (r < bodyRadiusUnit * 4.0f) r = bodyRadiusUnit * 5.0f;

            for (int i = 0; i < 50; i++)
            {
                float angle = i * (2f * Mathf.PI / 50f);
                float x = Mathf.Cos(angle) * r;
                float y = Mathf.Sin(angle) * r;
                _soiLineRenderer.SetPosition(i, new Vector3(x, y, 0));
            }
            
            // デフォルトは非表示、MapManagerで制御してもよい
            _soiLineRenderer.enabled = false;
        }
        
        public void SetSOIVisibility(bool visible)
        {
            if (_soiLineRenderer != null) _soiLineRenderer.enabled = visible;
        }

        /// <summary>
        /// 指定時間におけるローカル座標を取得する。
        /// 現在のアクティブなローカル天体系に基づいて計算する。
        /// </summary>
        public Vector3 GetLocalPosition(double time)
        {
            // MapManagerが管理する現在の中心天体を取得
            var activeBody = MapManager.Instance.ActiveLocalBody;

            // 1. 自分が中心天体そのものの場合 -> (0,0,0)不動
            if (this == activeBody)
            {
                return Vector3.zero;
            }

            // 2. 親が中心天体の場合 (例: 地球中心で、月を表示) -> 軌道計算して表示
            if (ParentBody == activeBody)
            {
                // OrbitParametersはまだdouble/float混在だが、計算結果はVector3で返す
                return OrbitData.CalculatePosition(time);
            }

            // 3. それ以外 (表示対象外、あるいは孫など)
            // 現仕様では表示しない想定だが、汎用的に非表示位置を返すか、有効無効はMapManagerで制御する。
            // ここでは一応軌道位置を返すが、MapManagerでCullingされるべき。
            return OrbitData.CalculatePosition(time);
        }

        /// <summary>
        /// 指定時間におけるグローバル座標を取得する。
        /// 抽象化されたマップのため、物理演算ではなく定位置を返す。
        /// </summary>
        public Vector3 GetGlobalPosition(double time)
        {
            // グローバルマップは静的なアイコン配置
            return AbstractGlobalPosition;
        }
    }
}
