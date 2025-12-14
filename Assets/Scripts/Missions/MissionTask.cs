using UnityEngine;
using System.Linq;
using SpaceLogistics.Missions;
using SpaceLogistics.Structures;

namespace SpaceLogistics.Missions
{
    public enum TaskType
    {
        Survey,         // 調査・観測 (Science Point獲得など)
        LoadCargo,      // 資源積み込み
        UnloadCargo,    // 資源積み下ろし
        Refuel,         // 燃料補給
        BuildFacility,  // 施設建設 (Deploy)
        Wait            // 待機 (タイミング調整用)
    }

    /// <summary>
    /// ミッションノードでの具体的なアクションを表すクラス。
    /// </summary>
    [System.Serializable]
    public class MissionTask
    {
        public TaskType Type;
        public double Duration; // 作業にかかる時間 (秒)

        // タスクパラメータ
        public ResourceType ResourceType; // 対象リソース
        public float Amount;      // 量

        /// <summary>
        /// タスク実行ロジック (イベント)
        /// </summary>
        /// <param name="rocket">実行中のロケット</param>
        /// <param name="localInventory">ローカルインベントリ（基地の在庫など）</param>
        public void Execute(ActiveRocket rocket, Inventory localInventory)
        {
            // 将来的にActiveRocketにInventoryプロパティを追加する想定
            // 現在は実装のスタブとして残す

            switch (Type)
            {
                case TaskType.LoadCargo:
                    // ロケットに物資を積み込む
                    // rocket.Inventory?.Add(ResourceType, Amount);
                    // localInventory.TryConsume(ResourceType, Amount);
                    Debug.Log($"Loading {Amount} of {ResourceType} to rocket");
                    break;

                case TaskType.UnloadCargo:
                    // ロケットから物資を降ろす
                    // rocket.Inventory?.TryConsume(ResourceType, Amount);
                    // localInventory.Add(ResourceType, Amount);
                    Debug.Log($"Unloading {Amount} of {ResourceType} from rocket");
                    break;

                case TaskType.Refuel:
                    // 燃料補給
                    Debug.Log($"Refueling rocket with {Amount} of {ResourceType}");
                    break;

                case TaskType.Survey:
                    // 調査・観測（科学ポイント獲得など）
                    Debug.Log($"Surveying location, gaining research data");
                    break;

                case TaskType.BuildFacility:
                    // 施設建設
                    Debug.Log($"Building facility: {ResourceType}");
                    break;

                case TaskType.Wait:
                    // 待機
                    Debug.Log($"Waiting for {Duration} seconds");
                    break;
            }
        }
    }
}

