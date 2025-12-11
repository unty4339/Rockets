using System;
using UnityEngine;

namespace SpaceLogistics.Core
{
    /// <summary>
    /// ゲーム内の時間進行を管理するクラス。
    /// タイムスケールの変更や、宇宙時間（UniverseTime）の更新を行う。
    /// </summary>
    /// <summary>
    /// ゲーム内の時間進行を管理するクラス。
    /// タイムスケールの変更や、宇宙時間（UniverseTime）の更新を行う。
    /// </summary>
    public class TimeManager : SingletonMonoBehaviour<TimeManager>
    {
        // Instance property inherited

        [Header("Time Settings")]
        [SerializeField] private float initialTimeScale = 1.0f;
        
        /// <summary>
        /// ゲーム開始からの累積経過時間（秒）。
        /// </summary>
        public double UniverseTime { get; private set; } = 0.0;

        /// <summary>
        /// 現在の時間の流れる速さ。1.0で等倍速。
        /// </summary>
        public float TimeScale { get; private set; } = 1.0f;

        /// <summary>
        /// 時間経過に合わせて更新が必要なシステム（生産、軌道計算など）のためのイベント。
        /// </summary>
        public event Action<float> OnTick;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            TimeScale = initialTimeScale;
        }

        private void Update()
        {
            float dt = Time.deltaTime * TimeScale;
            UniverseTime += dt;
            
            OnTick?.Invoke(dt);

            if (Time.frameCount % 200 == 0 && TimeScale > 0)
            {
                // Debug.Log($"[TimeManager] UniverseTime={UniverseTime:F1}, Scale={TimeScale}");
            }
        }

        /// <summary>
        /// タイムスケールを設定する。負の値は0（一時停止）にクランプされる。
        /// </summary>
        /// <param name="scale">設定するタイムスケール</param>
        public void SetTimeScale(float scale)
        {
            TimeScale = Mathf.Max(0, scale);
            Debug.Log($"Time Scale set to: {TimeScale}");
        }

        /// <summary>
        /// 時間の進行を一時停止する。
        /// </summary>
        public void Pause()
        {
            SetTimeScale(0);
        }

        /// <summary>
        /// 時間の進行を再開する（等倍速）。
        /// </summary>
        public void Resume()
        {
            SetTimeScale(1); // 必要であれば以前のスケールを復元するロジックに変更可能
        }
    }
}
