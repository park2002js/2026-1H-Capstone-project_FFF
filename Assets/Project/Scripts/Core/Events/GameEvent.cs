using System;
using UnityEngine;

namespace FFF.Core.Events
{
    /// <summary>
    /// ScriptableObject 기반 이벤트 채널 (데이터 없음).
    /// Controller(Presenter)에서 Raise()하고, View(UIManager)에서 구독한다.
    /// 
    /// 사용 예시:
    /// - OnMapInitialize: 맵 시스템 → UIManager "맵 UI로 초기화하라"
    /// - OnBattleInitialize: 초기화 로직 → UIManager "전투 UI로 초기화하라"
    /// - OnSceneChange: GameManager → UIManager "Scene 전환 처리하라"
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "FFF/Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private Action _onRaised;

        /// <summary>
        /// 이벤트를 구독한다. UIManager의 Observer 구독에 해당.
        /// </summary>
        public void Subscribe(Action listener)
        {
            _onRaised += listener;
        }

        /// <summary>
        /// 이벤트 구독을 해제한다.
        /// </summary>
        public void Unsubscribe(Action listener)
        {
            _onRaised -= listener;
        }

        /// <summary>
        /// 이벤트를 발행한다. Controller(Presenter)에서 호출.
        /// </summary>
        public void Raise()
        {
            _onRaised?.Invoke();
        }

        /// <summary>
        /// Play Mode 종료 시 구독자 정리 (에디터 메모리 누수 방지).
        /// </summary>
        private void OnDisable()
        {
            _onRaised = null;
        }
    }
}