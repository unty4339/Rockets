using UnityEngine;

namespace SpaceLogistics.Core
{
    /// <summary>
    /// シングルトンパターンを実装するMonoBehaviourの基底クラス。
    /// 重複インスタンスの破棄とDontDestroyOnLoadを管理する。
    /// </summary>
    /// <typeparam name="T">継承クラスの型</typeparam>
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        /// <summary>
        /// 初期化処理。サブクラスでAwakeを使用する場合は必ずbase.Awake()を呼び、
        /// 直後に if (Instance != this) return; チェックを行うこと。
        /// </summary>
        protected virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = this as T;
                
                // 親がいる場合はルートに移動してからDDOLを適用（DDOLはルートオブジェクトでのみ有効なため）
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                
                DontDestroyOnLoad(gameObject);
                
                Debug.Log($"[{typeof(T).Name}] Initialized (Singleton)");
            }
            else
            {
                Debug.LogWarning($"[{typeof(T).Name}] Duplicate instance found. Destroying component.");
                Destroy(this);
            }
        }
    }
}
