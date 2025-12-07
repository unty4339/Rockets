namespace SpaceLogistics.Rocketry
{
    /// <summary>
    /// ロケットの統計情報を格納する構造体。
    /// 質量、Delta-V、推力重量比などを含む。
    /// </summary>
    [System.Serializable]
    public struct RocketStats
    {
        public float TotalMass;
        public float DryMass;
        public float DeltaV; // 到達可能な速度増分
        public float TWR_Surface; // 地表重力(g=9.81)における推力重量比

        public override string ToString()
        {
            return $"Mass: {TotalMass:F1}t | dV: {DeltaV:F0}m/s | TWR: {TWR_Surface:F2}";
        }
    }
}
