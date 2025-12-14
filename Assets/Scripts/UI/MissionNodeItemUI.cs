using UnityEngine;
using UnityEngine.UI;
using SpaceLogistics.Missions;

namespace SpaceLogistics.UI
{
    /// <summary>
    /// ノードリスト内の個別ノード項目を表示するUIコンポーネント。
    /// </summary>
    public class MissionNodeItemUI : MonoBehaviour
    {
        [Header("UI References")]
        public Text NodeInfoText;
        public Text NodeDetailsText;
        public Button EditButton;
        public Button DeleteButton;

        private MissionNode _node;
        private int _index;
        private MissionPlanEditorUI _parentEditor;

        public void Initialize(MissionNode node, int index, MissionPlanEditorUI parentEditor)
        {
            _node = node;
            _index = index;
            _parentEditor = parentEditor;

            UpdateDisplay();

            if (EditButton != null)
                EditButton.onClick.AddListener(() => _parentEditor?.OnNodeEditClicked(_node, _index));
            
            if (DeleteButton != null)
                DeleteButton.onClick.AddListener(() => _parentEditor?.OnNodeDeleteClicked(_node, _index));
        }

        /// <summary>
        /// UI要素を手動で設定する（Auto Generate UI用）
        /// </summary>
        public void SetUIReferences(Text infoText, Text detailsText, Button editButton, Button deleteButton)
        {
            NodeInfoText = infoText;
            NodeDetailsText = detailsText;
            EditButton = editButton;
            DeleteButton = deleteButton;
        }

        private void UpdateDisplay()
        {
            if (_node == null) return;

            // ノード情報テキスト
            if (NodeInfoText != null)
            {
                string bodyName = _node.TargetBody != null ? _node.TargetBody.BodyName : "Unknown";
                NodeInfoText.text = $"{_index + 1}. {_node.NodeName}";
            }

            // 詳細情報テキスト
            if (NodeDetailsText != null)
            {
                string bodyName = _node.TargetBody != null ? _node.TargetBody.BodyName : "Unknown";
                string baseName = _node.TargetBase != null ? _node.TargetBase.BaseName : "None";
                string details = $"Type: {_node.Type}\n" +
                               $"Body: {bodyName}\n" +
                               $"Base: {baseName}\n" +
                               $"Tasks: {_node.Tasks.Count}\n" +
                               $"Stay: {_node.StayDuration:F0}s";
                
                NodeDetailsText.text = details;
            }
        }
    }
}

