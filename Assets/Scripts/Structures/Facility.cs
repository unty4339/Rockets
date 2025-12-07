using System.Collections.Generic;
using UnityEngine;

namespace SpaceLogistics.Structures
{
    public enum FacilityType
    {
        Mine,
        Factory,
        Lab,
        Habitation,
        Spaceport
    }

    /// <summary>
    /// 基地内に建設される施設クラス。
    /// リソースを消費して別のリソース（または研究ポイント）を生産するロジックを持つ。
    /// </summary>
    [System.Serializable]
    public class Facility
    {
        public string Name;
        public FacilityType Type;
        public bool IsActive = true;

        // 実際のプロジェクトでは、これらはScriptableObject（FacilityData）として定義し、
        // データの重複を避けることが望ましい。
        public List<ResourceIO> InputResources = new List<ResourceIO>();
        public List<ResourceIO> OutputResources = new List<ResourceIO>();

        /// <summary>
        /// 時間経過に応じた生産/消費処理を行う。
        /// 在庫 (storage) から入力リソースを消費し、出力リソースを追加する。
        /// </summary>
        /// <param name="storage">対象となるインベントリ</param>
        /// <param name="deltaTime">経過時間</param>
        public void Process(Inventory storage, float deltaTime)
        {
            if (!IsActive) return;

            // 1. 入力リソースの確認
            // 簡易的に毎フレーム消費する形をとる。
            
            bool canProduce = true;

            // まず消費に必要な量が足りているか確認
            foreach (var input in InputResources)
            {
                float amountNeeded = input.RatePerSecond * deltaTime;
                if (storage.GetAmount(input.Type) < amountNeeded)
                {
                    canProduce = false;
                    break;
                }
            }

            if (canProduce)
            {
                // 消費実行
                foreach (var input in InputResources)
                {
                    storage.TryConsume(input.Type, input.RatePerSecond * deltaTime);
                }

                // 生産実行
                foreach (var output in OutputResources)
                {
                    if (output.Type == ResourceType.ResearchData)
                    {
                        // 研究データはグローバル管理
                        if (ResourceManager.Instance != null)
                        {
                            ResourceManager.Instance.AddResearchPoints(output.RatePerSecond * deltaTime);
                        }
                    }
                    else
                    {
                        // 通常リソースは基地の在庫へ
                        storage.Add(output.Type, output.RatePerSecond * deltaTime);
                    }
                }
            }
            else
            {
                // オプション: 資源不足時は自動停止するか、単にアイドリングするか
                // Debug.Log($"{Name} is missing resources.");
            }
        }
    }
}
