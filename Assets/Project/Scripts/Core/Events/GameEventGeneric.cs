using System;
using UnityEngine;

namespace FFF.Core.Events
{
    /// <summary>
    /// ScriptableObject 기반 이벤트 채널 (데이터 포함).
    /// 이벤트 발행 시 T 타입의 데이터를 함께 전달한다.
    /// 
    /// 사용 예시:
    /// - GameEvent&lt;int&gt;: 스테이지 번호 전달
    /// - GameEvent&lt;string&gt;: Scene 이름 전달
    /// - GameEvent&lt;BattleResultData&gt;: 전투 결과 데이터 전달
    /// </summary>
    public abstract class GameEvent<T> : ScriptableObject
    {
        private Action<T> _onRaised;

        /// <summary>
        /// 이벤트를 구독한다.
        /// </summary>
        public void Subscribe(Action<T> listener)
        {
            _onRaised += listener;
        }

        /// <summary>
        /// 이벤트 구독을 해제한다.
        /// </summary>
        public void Unsubscribe(Action<T> listener)
        {
            _onRaised -= listener;
        }

        /// <summary>
        /// 데이터와 함께 이벤트를 발행한다.
        /// </summary>
        public void Raise(T value)
        {
            _onRaised?.Invoke(value);
        }

        private void OnDisable()
        {
            _onRaised = null;
        }
    }
}