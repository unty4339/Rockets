using SpaceLogistics.Structures;

namespace SpaceLogistics.Missions
{
    public enum TrajectoryProfile
    {
        Direct, // 直接遷移（高速、高コスト）
        Hohmann, // ホーマン遷移（標準的、高効率）
        GravityAssist // スイングバイ（低速、低コスト、実装難易度高のためプレースホルダー）
    }

    /// <summary>
    /// 運航ルートを定義するクラス。
    /// 出発地、目的地、軌道プロファイル、および所要時間などを含む。
    /// </summary>
    [System.Serializable]
    public class Route
    {
        public Base Origin;
        public Base Destination;
        public TrajectoryProfile Profile;
        
        // 計算された詳細情報
        public float TotalDuration; // トータル所要時間 (秒)
        public float RequiredDeltaV; // 必要Delta-V
        
        public Route(Base origin, Base dest, TrajectoryProfile profile)
        {
            Origin = origin;
            Destination = dest;
            Profile = profile;
        }
    }
}
