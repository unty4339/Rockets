using UnityEngine;
using SpaceLogistics.Core;
using UnityEngine.UI;

namespace SpaceLogistics.UI
{
    /// <summary>
    /// マップ表示の切り替えを行うUIコンポーネント。
    /// ローカルマップとグローバルマップの表示切り替えを担当する。
    /// </summary>
    public class MapUI : MonoBehaviour
    {
        [Header("Buttons")]
        public Button ToggleMapButton;
        public Button OpenEditorButton; // 新しいボタン

        private void Start()
        {
            if (ToggleMapButton != null)
            {
                ToggleMapButton.onClick.AddListener(OnToggleMapClicked);
            }
            if (OpenEditorButton != null)
            {
                OpenEditorButton.onClick.AddListener(OnOpenEditorClicked);
            }

            // 状態変更イベントを購読
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
                // 初期状態の反映
                HandleStateChanged(GameManager.Instance.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void OnToggleMapClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ToggleMapMode();
            }
        }

        private void OnOpenEditorClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.RocketEditor);
            }
        }

        private void HandleStateChanged(GameState newState)
        {
            // マップモード時のみ表示し、エディタモードなどでは非表示にする
            bool showMapUI = (newState == GameState.LocalMap || newState == GameState.GlobalMap);
            
            // CanvasGroupがあればそれを使うが、簡易的にGameObjectのActive切り替え
            gameObject.SetActive(showMapUI);
        }
    }
}
