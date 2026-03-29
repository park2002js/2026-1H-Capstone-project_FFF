using UnityEngine;
using UnityEngine.Events;

namespace FFF.Core.Events
{
    /// <summary>
    /// Inspector에서 GameEvent를 구독할 수 있게 해주는 컴포넌트.
    /// UIComponent가 아닌 일반 GameObject에서 이벤트를 받아야 할 때 사용한다.
    /// 
    /// UIManager와 BaseUIComponent는 코드에서 직접 Subscribe하므로
    /// 이 컴포넌트는 보조 용도이다.
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        [Header("구독할 이벤트 채널")]
        [SerializeField] private GameEvent _gameEvent;

        [Header("이벤트 수신 시 실행할 반응")]
        [SerializeField] private UnityEvent _onEventRaised;

        private void OnEnable()
        {
            if (_gameEvent != null)
            {
                _gameEvent.Subscribe(OnEventRaised);
            }
        }

        private void OnDisable()
        {
            if (_gameEvent != null)
            {
                _gameEvent.Unsubscribe(OnEventRaised);
            }
        }

        private void OnEventRaised()
        {
            _onEventRaised?.Invoke();
        }
    }
}