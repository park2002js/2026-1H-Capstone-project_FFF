using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FFF.Core.Events;


namespace FFF.Battle.FSM
{
    // 전투의 현재 진행 상태를 나타내는 열거형
    public enum TurnState
    {
        None,       // 배틀 시작 & 종료
        TurnReady,  // 턴 준비 (드로우 및 멀리건)
        TurnProceed,// 턴 진행 (카드 선택, 아이템 사용)
        TurnEnd     // 턴 종료 (공격력 비교, 피해량 적용)
    }

    public class BattleManager : MonoBehaviour
    {
        // --- 싱글톤 인스턴스 ---
        public static BattleManager Instance { get; private set; }

        // --- 현재 상태 읽기 전용 프로퍼티 ---
        public TurnState CurrentPhase { get; private set; } = TurnState.None;

        // ==========================================
        // SO Event Channels (상태 변경 시 방송할 채널들)
        // ==========================================
        [Header("=== 이벤트 채널 ===")]
        [SerializeField] private GameEvent _onBattleStart;
        [SerializeField] private GameEvent _onTurnReady;
        [SerializeField] private GameEvent _onTurnProceed;
        [SerializeField] private GameEvent _onTurnEnd;
        [SerializeField] private GameEvent _onBattleEnd;

        // ==========================================
        // 초기화
        // ==========================================
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // ==========================================
        // 전투 흐름 제어 (외부에서 호출)
        // ==========================================

        /// <summary>
        /// 전투를 처음 시작할 때 호출
        /// </summary>
        public void StartBattle()
        {
            CurrentPhase = TurnState.None;
            
            // 전투 시작 이벤트 호출 (초기화 관련 로직들이 등록되어 있음)
            _onBattleStart?.Raise();
            
            // 첫 턴 준비 단계로 바로 진입
            ChangeState(TurnState.TurnReady);
        }

        /// <summary>
        /// 전투를 처음 시작할 때 호출
        /// </summary>
        public void EndBattle()
        {
            CurrentPhase = TurnState.None;
            
            // 전투 종료 이벤트 호출
            _onBattleEnd?.Raise();
        }

        /// <summary>
        /// 상태를 변경하고, 해당 상태에 등록된 이벤트들을 일제히 호출(Invoke)합니다.
        /// </summary>
        /// <param name="newState">변경할 새로운 상태</param>
        public void ChangeState(TurnState newState)
        {
            // 현재 상태와 같으면 무시
            if (CurrentPhase == newState) return;

            CurrentPhase = newState;

            // 변경된 상태에 맞춰 등록된 함수들을 단 한 번씩 호출
            switch (CurrentPhase)
            {
                case TurnState.TurnReady:
                    Debug.Log("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
                    _onTurnReady?.Raise();
                    break;
                case TurnState.TurnProceed:
                    _onTurnProceed?.Raise();
                    break;
                case TurnState.TurnEnd:
                    _onTurnEnd?.Raise();
                    break;
            }
        }
    }    
}