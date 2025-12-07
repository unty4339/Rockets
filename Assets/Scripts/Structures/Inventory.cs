using System.Collections.Generic;
using UnityEngine;

namespace SpaceLogistics.Structures
{
    /// <summary>
    /// リソースの在庫を管理するクラス。
    /// </summary>
    [System.Serializable]
    public class Inventory
    {
        // UnityのシリアライズシステムではDictionaryは表示されないため、
        // インスペクター表示が必要な場合はリスト等でラップする必要がある。
        private Dictionary<ResourceType, float> _resources = new Dictionary<ResourceType, float>();

        /// <summary>
        /// リソースを指定量追加する。
        /// </summary>
        /// <param name="type">リソースの種類</param>
        /// <param name="amount">追加量</param>
        public void Add(ResourceType type, float amount)
        {
            if (!_resources.ContainsKey(type))
            {
                _resources[type] = 0;
            }
            _resources[type] += amount;
        }

        /// <summary>
        /// リソースの消費を試みる。
        /// 在庫が足りていれば消費してtrueを返し、足りなければ消費せずfalseを返す。
        /// </summary>
        /// <param name="type">リソースの種類</param>
        /// <param name="amount">消費量</param>
        /// <returns>消費に成功したかどうか</returns>
        public bool TryConsume(ResourceType type, float amount)
        {
            if (_resources.ContainsKey(type) && _resources[type] >= amount)
            {
                _resources[type] -= amount;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 指定したリソースの現在の在庫量を取得する。
        /// </summary>
        /// <param name="type">リソースの種類</param>
        /// <returns>在庫量</returns>
        public float GetAmount(ResourceType type)
        {
            return _resources.ContainsKey(type) ? _resources[type] : 0;
        }

        // デバッグ用ヘルパー
        public string GetDebugString()
        {
            string s = "";
            foreach (var kvp in _resources)
            {
                s += $"{kvp.Key}: {kvp.Value:F1}, ";
            }
            return s;
        }
    }
}
