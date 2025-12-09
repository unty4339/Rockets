using UnityEngine;
using SpaceLogistics.Core;

namespace SpaceLogistics.Structures
{
    /// <summary>
    /// グローバルなリソース（研究ポイントなど）や技術ツリーの状態を管理するシングルトンクラス。
    /// </summary>
    public class ResourceManager : SingletonMonoBehaviour<ResourceManager>
    {
        // Instance inherited from SingletonMonoBehaviour

        // グローバル研究ポイント
        public float ResearchPoints { get; private set; }
        public Inventory GlobalInventory; // 必要に応じて

        // Awake logic handled by SingletonMonoBehaviour

        /// <summary>
        /// 研究ポイントを加算する。
        /// </summary>
        /// <param name="amount">加算量</param>
        public void AddResearchPoints(float amount)
        {
            ResearchPoints += amount;
            // UI更新イベントなどを発火する可能性がある場所
        }

        /// <summary>
        /// 指定されたIDの技術をアンロックする。
        /// </summary>
        /// <param name="techId">技術ID</param>
        /// <returns>アンロックに成功したか</returns>
        public bool UnlockTech(string techId)
        {
            // 技術ツリーアンロックロジックのスタブ
            Debug.Log($"Tech Unlocked: {techId}");
            return true;
        }
    }
}
