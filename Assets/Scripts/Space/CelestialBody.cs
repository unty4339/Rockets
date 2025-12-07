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
        public double Mass;
        public double Radius;
        public double SOIRadius; // 重力圏半径
        public bool HasAtmosphere;

        [Header("Orbit")]
        public CelestialBody ParentBody;
        public OrbitParameters OrbitData;
        public Color OrbitColor = Color.white;

        [Header("Visuals")]
        public SpriteRenderer BodyRenderer;
        public float VisualScaleLocal = 1.0f;
        public float VisualScaleGlobal = 0.5f;

        private void Start()
        {
            if (BodyRenderer == null)
                BodyRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// 指定時間におけるローカル座標（親天体からの相対位置）を取得する。
        /// </summary>
        /// <param name="time">時間</param>
        /// <returns>ローカル座標</returns>
        public Vector3 GetLocalPosition(double time)
        {
            // 親がいない（中心天体、太陽など）場合は原点とする
            if (ParentBody == null) return Vector3.zero;

            // それ以外は親からの相対位置を計算
            return OrbitData.CalculatePosition(time);
        }

        /// <summary>
        /// 指定時間におけるグローバル座標（太陽系全体での位置）を取得する。
        /// </summary>
        /// <param name="time">時間</param>
        /// <returns>グローバル座標</returns>
        public Vector3 GetGlobalPosition(double time)
        {
            // グローバルマップでは、太陽系内での絶対位置として扱う
            if (ParentBody == null) return Vector3.zero;
            
            Vector3 parentPos = ParentBody.GetGlobalPosition(time);
            Vector3 relativePos = OrbitData.CalculatePosition(time);
            
            return parentPos + relativePos;
        }

        private void OnDrawGizmos()
        {
            // デバッグ用に軌道パスを描画
            if (ParentBody != null && OrbitData != null && OrbitData.SemiMajorAxis > 0)
            {
                Gizmos.color = OrbitColor;
                int segments = 50;
                Vector3 prevPos = OrbitData.CalculatePosition(0);
                
                // 親天体の現在位置を中心に軌道を描画する（簡易表示）
                if (ParentBody != null)
                {
                     Vector3 parentWorldPos = ParentBody.transform.position;
                     for (int i = 1; i <= segments; i++)
                     {
                         // 単純に0から2πまで一周させる
                         double angle = (double)i / segments * 2 * Math.PI;
                         
                         // OrbitParametersの簡易計算ロジックを再利用して一貫性を保つ
                         float x = (float)(OrbitData.SemiMajorAxis * Math.Cos(angle));
                         float y = (float)(OrbitData.SemiMajorAxis * Math.Sin(angle));
                         Vector3 nextPos = new Vector3(x, y, 0);
                         
                         Gizmos.DrawLine(parentWorldPos + prevPos, parentWorldPos + nextPos);
                         prevPos = nextPos;
                     }
                }
            }
        }
    }
}
