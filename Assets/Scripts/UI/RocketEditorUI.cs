using UnityEngine;
using SpaceLogistics.Rocketry;
using UnityEngine.UI;

namespace SpaceLogistics.UI
{
    /// <summary>
    /// ロケット設計画面（VAB）のUIクラス（プレースホルダー）。
    /// パーツの追加や統計情報の表示を行う機能を持つ予定。
    /// </summary>
    public class RocketEditorUI : MonoBehaviour
    {
        public RocketBlueprint CurrentBlueprint;

        // UI References
        // public Text StatsText;
        // public Dropdown PartDropdown;

        private void Start()
        {
            CurrentBlueprint = new RocketBlueprint();
        }

        /// <summary>
        /// 新しいステージを追加する。
        /// </summary>
        public void AddStage()
        {
            CurrentBlueprint.Stages.Add(new RocketStage() { StageIndex = CurrentBlueprint.Stages.Count });
            UpdateStatsDisplay();
        }

        /// <summary>
        /// 現在のステージにパーツを追加する。
        /// </summary>
        public void AddPartToCurrentStage(RocketPart part)
        {
            if (CurrentBlueprint.Stages.Count == 0) AddStage();
            
            CurrentBlueprint.Stages[CurrentBlueprint.Stages.Count - 1].Parts.Add(part);
            UpdateStatsDisplay();
        }

        /// <summary>
        /// 画面上の統計情報を更新する。
        /// </summary>
        private void UpdateStatsDisplay()
        {
            RocketStats stats = CurrentBlueprint.CalculateTotalStats();
            // StatsText.text = stats.ToString();
            Debug.Log($"Blueprint Updated: {stats}");
        }
    }
}
