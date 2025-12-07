using UnityEngine;

namespace SpaceLogistics.Structures
{
    public enum ResourceType
    {
        Money,
        Iron,
        Fuel,
        ResearchData,
        Food,
        Alloy
    }

    /// <summary>
    /// 施設の入出力リソース設定を定義するScriptableObject。
    /// リソースの種類と、1秒あたりの消費/生産量を指定する。
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(fileName = "NewResourceIO", menuName = "SpaceLogistics/ResourceIO")]
    public class ResourceIO : ScriptableObject
    {
        public ResourceType Type;
        public float RatePerSecond; // 正の値で出力。消費ロジックでも使用可能だが、通常はリストで管理する
    }
}
