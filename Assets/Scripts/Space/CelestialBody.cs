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
        [Header("Body Stats")]
        public string BodyName;
        public Mass Mass; // PhysicsTypes.Mass
        public Distance Radius; // PhysicsTypes.Distance
        public Distance SOIRadius; // PhysicsTypes.Distance
        public bool HasAtmosphere;

        [Header("Map Positions")]
        public Vector3 AbstractGlobalPosition; // グローバルマップでの定位置 (アイコン表示用)

        [Header("Orbit")]
        public CelestialBody ParentBody;
        public OrbitParameters OrbitData;
        public Color OrbitColor = Color.white;

        [Header("Visuals")]
        public SpriteRenderer BodyRenderer;
        public float VisualScaleLocal = 1.0f;
        public float VisualScaleGlobal = 1.0f; // GlobalViewLogScaleが1.0になったのでこちらも調整

        private void Start()
        {
            if (BodyRenderer == null)
                BodyRenderer = GetComponent<SpriteRenderer>();
        }

        // 互換性維持のためのヘルパー (Inspector設定用には別途エディタ拡張が必要だが、今回はコードで初期化前提か既存値を読み替える)
        // 注意: Inspectorのdoubleフィールドはシリアライズされない可能性があるため、OnValidateなどでfloatから変換するなどの工夫が必要だが、
        // 今回の要件ではPhysicsTypesを導入したため、Inspectorでの直接入力が難しくなる。
        // MVPとしては、Awake等で初期値をセットするか、[SerializeField]なdoubleラッパーを使うのが定石。
        // ここでは、Inspectorで設定した値を保持するためのフィールドを残し、RuntimeでPhysicsTypesに変換するアプローチを取る。

        [SerializeField] private double _massKg;
        [SerializeField] private double _radiusKm;
        [SerializeField] private double _soiRadiusKm;

        private void Awake()
        {
            Mass = new Mass(_massKg);
            Radius = Distance.FromKilometers(_radiusKm);
            SOIRadius = Distance.FromKilometers(_soiRadiusKm);
        }

        private void OnValidate()
        {
            // Inspectorでの変更を反映 (Edit Mode用)
            Mass = new Mass(_massKg);
            Radius = Distance.FromKilometers(_radiusKm);
            SOIRadius = Distance.FromKilometers(_soiRadiusKm);
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
