using UnityEngine;

namespace FFF.Core
{
    /// <summary>
    /// DontDestroyOnLoad 싱글턴 베이스 클래스.
    /// UIManager, GameManager 등이 상속하여 사용한다.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] '{typeof(T)}' 인스턴스가 이미 파괴되었습니다. null을 반환합니다.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindFirstObjectByType<T>();

                        if (_instance == null)
                        {
                            Debug.LogError($"[Singleton] '{typeof(T)}' 인스턴스를 찾을 수 없습니다. Scene에 배치해주세요.");
                        }
                    }

                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                OnInitialize();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[Singleton] '{typeof(T)}' 중복 인스턴스 파괴: {gameObject.name}");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 싱글턴 최초 초기화 시 호출. Awake 대신 이 메서드를 오버라이드한다.
        /// </summary>
        protected virtual void OnInitialize() { }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _applicationIsQuitting = true;
            }
        }
    }
}