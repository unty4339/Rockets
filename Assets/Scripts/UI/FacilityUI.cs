using UnityEngine;
using SpaceLogistics.Structures;
using UnityEngine.UI; 

namespace SpaceLogistics.UI
{
    /// <summary>
    /// 施設情報を表示するUIクラス（プレースホルダー）。
    /// 選択された基地や施設の詳細を表示する機能を持つ予定。
    /// </summary>
    public class FacilityUI : MonoBehaviour
    {
        public Base SelectedBase;
        
        // References to UI Text elements would go here
        // public Text BaseNameText;
        // public Text ResourceListText;

        /// <summary>
        /// 表示対象の基地を設定する。
        /// </summary>
        public void SetBase(Base newBase)
        {
            SelectedBase = newBase;
            UpdateUI();
        }

        /// <summary>
        /// UI表示を更新する。
        /// </summary>
        public void UpdateUI()
        {
            if (SelectedBase == null) return;
            
            // Debug.Log($"Viewing Base: {SelectedBase.BaseName}");
            // Populate UI with Facilities and Resources
        }
        
        // Construction buttons would call Base.AddFacility();
    }
}
