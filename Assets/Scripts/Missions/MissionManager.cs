using System.Collections.Generic;
using UnityEngine;
using SpaceLogistics.Rocketry;

namespace SpaceLogistics.Missions
{
    /// <summary>
    /// ミッションの管理を行うクラス。
    /// ルートとロケットの適合性検証や、アクティブなロケットの追跡を行う。
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        public List<ActiveRocket> OngoingMissions = new List<ActiveRocket>();
        public GameObject RocketPrefab; // ActiveRocket生成用プレハブ

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// ミッションが実行可能か検証する。
        /// ロケットのDelta-Vがルートの必要Delta-Vを満たしているか確認する。
        /// </summary>
        /// <param name="route">予定ルート</param>
        /// <param name="blueprint">ロケット設計図</param>
        /// <returns>実行可能であればtrue</returns>
        public bool ValidateMission(Route route, RocketBlueprint blueprint)
        {
            RocketStats stats = blueprint.CalculateTotalStats();
            
            // 簡易検証: ロケットdV > ルート必要dV
            if (stats.DeltaV >= route.RequiredDeltaV)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// ミッションを開始し、ロケットをインスタンス化する。
        /// </summary>
        /// <param name="route">予定ルート</param>
        /// <param name="blueprint">ロケット設計図</param>
        /// <returns>生成されたアクティブロケット（失敗時はnull）</returns>
        public ActiveRocket StartMission(Route route, RocketBlueprint blueprint)
        {
            if (!ValidateMission(route, blueprint))
            {
                Debug.LogWarning("Mission validation failed! Cannot start.");
                return null;
            }

            if (RocketPrefab == null)
            {
                Debug.LogError("Rocket Prefab not assigned!");
                return null;
            }

            GameObject go = Instantiate(RocketPrefab);
            ActiveRocket rocket = go.GetComponent<ActiveRocket>();
            if (rocket != null)
            {
                rocket.Blueprint = blueprint;
                rocket.AssignedRoute = route;
                
                // 発射初期化
                if (Core.TimeManager.Instance != null)
                {
                    rocket.Launch(Core.TimeManager.Instance.UniverseTime);
                }
                
                OngoingMissions.Add(rocket);
                return rocket;
            }
            return null;
        }
    }
}
