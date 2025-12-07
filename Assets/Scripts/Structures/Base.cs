using System.Collections.Generic;
using UnityEngine;
using SpaceLogistics.Core;

namespace SpaceLogistics.Structures
{
    public enum BaseType
    {
        Surface,
        Orbital
    }

    /// <summary>
    /// 基地を表すクラス。
    /// リソース在庫（Inventory）と複数の施設（Facility）を保持・管理する。
    /// </summary>
    public class Base : MonoBehaviour
    {
        [Header("Base Info")]
        public string BaseName;
        public BaseType Type;
        
        [Header("State")]
        public Inventory Storage = new Inventory();
        public List<Facility> Facilities = new List<Facility>();

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTick += TickProduction;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTick -= TickProduction;
            }
        }

        /// <summary>
        /// 時間経過時の処理。各施設の生産処理を呼び出す。
        /// </summary>
        /// <param name="deltaTime">経過時間</param>
        public void TickProduction(float deltaTime)
        {
            foreach (var facility in Facilities)
            {
                facility.Process(Storage, deltaTime);
            }
        }

        /// <summary>
        /// 施設を追加するヘルパーメソッド。
        /// </summary>
        /// <param name="facility">追加する施設</param>
        public void AddFacility(Facility facility)
        {
            Facilities.Add(facility);
        }
    }
}
