using UnityEngine;

namespace SpaceLogistics.Rocketry
{
    public enum PartType
    {
        Engine,
        FuelTank,
        CommandModule,
        Structure,
        Utility
    }

    /// <summary>
    /// ロケットのパーツ（エンジン、タンクなど）を定義するScriptableObject。
    /// 性能データ（質量、コスト、推力など）を保持する。
    /// </summary>
    [CreateAssetMenu(fileName = "NewRocketPart", menuName = "SpaceLogistics/RocketPart")]
    public class RocketPart : ScriptableObject
    {
        public string PartName;
        public PartType Type;
        public float MassDry; // 燃料を含まない乾燥重量
        public float Cost;
        
        [Header("Engine Stats (if Engine)")]
        public float Thrust; // 推力 (kN)
        public float Isp; // 比推力 (s)

        [Header("Fuel Stats (if Tank)")]
        public float FuelCapacity; // 搭載可能な燃料の質量
    }
}
