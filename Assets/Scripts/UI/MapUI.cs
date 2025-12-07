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

        private void Start()
        {
            if (ToggleMapButton != null)
            {
                ToggleMapButton.onClick.AddListener(OnToggleMapClicked);
            }
        }

        /// <summary>
        /// マップモード切り替えボタンがクリックされたときに呼び出される。
        /// </summary>
        private void OnToggleMapClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ToggleMapMode();
            }
        }
        
        // Additional methods to update Labels showing planet names etc.
    }
}
