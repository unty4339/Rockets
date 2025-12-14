using UnityEngine;
using UnityEngine.UI;
using SpaceLogistics.Missions;

namespace SpaceLogistics.UI
{
    /// <summary>
    /// タスクリスト内の個別タスク項目を表示するUIコンポーネント。
    /// </summary>
    public class MissionTaskItemUI : MonoBehaviour
    {
        [Header("UI References")]
        public Text TaskTypeText;
        public Text TaskDetailsText;
        public Button DeleteButton;

        private MissionTask _task;
        private int _index;
        private System.Action<MissionTask, int> _onDeleteCallback;

        public void Initialize(MissionTask task, int index, System.Action<MissionTask, int> onDeleteCallback)
        {
            _task = task;
            _index = index;
            _onDeleteCallback = onDeleteCallback;

            UpdateDisplay();

            if (DeleteButton != null)
                DeleteButton.onClick.AddListener(() => _onDeleteCallback?.Invoke(_task, _index));
        }

        /// <summary>
        /// UI要素を手動で設定する（Auto Generate UI用）
        /// </summary>
        public void SetUIReferences(Text typeText, Text detailsText, Button deleteButton)
        {
            TaskTypeText = typeText;
            TaskDetailsText = detailsText;
            DeleteButton = deleteButton;
        }

        private void UpdateDisplay()
        {
            if (_task == null) return;

            if (TaskTypeText != null)
                TaskTypeText.text = _task.Type.ToString();

            if (TaskDetailsText != null)
            {
                string details = $"Duration: {_task.Duration:F0}s";
                if (_task.Type == TaskType.LoadCargo || _task.Type == TaskType.UnloadCargo || 
                    _task.Type == TaskType.Refuel)
                {
                    details += $"\n{_task.ResourceType}: {_task.Amount:F1}";
                }
                TaskDetailsText.text = details;
            }
        }
    }
}

