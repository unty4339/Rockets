using System;
using UnityEngine;

namespace SpaceLogistics.Core
{
    public enum GameState
    {
        LocalMap,
        GlobalMap,
        RocketEditor,
        BaseView
    }

    /// <summary>
    /// ゲーム全体の進行と状態を管理するシングルトンクラス。
    /// マップ切り替えやゲームモードの遷移を制御する。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>
        /// GameManagerの唯一のインスタンス。
        /// </summary>
        public static GameManager Instance { get; private set; }

        /// <summary>
        /// 現在のゲーム状態。
        /// </summary>
        public GameState CurrentState { get; private set; } = GameState.LocalMap;

        /// <summary>
        /// 状態が変更されたときに発火するイベント。
        /// </summary>
        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// ゲーム状態を指定された新しい状態に変更する。
        /// 既に同じ状態の場合は何もしない。
        /// </summary>
        /// <param name="newState">新しいゲーム状態</param>
        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            Debug.Log($"Game State Changed to: {CurrentState}");
            OnStateChanged?.Invoke(CurrentState);
        }

        /// <summary>
        /// ローカルマップとグローバルマップの状態を切り替える。
        /// </summary>
        public void ToggleMapMode()
        {
            if (CurrentState == GameState.LocalMap)
            {
                SetState(GameState.GlobalMap);
            }
            else if (CurrentState == GameState.GlobalMap)
            {
                SetState(GameState.LocalMap);
            }
        }
    }
}
