using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FFF.Core;
using FFF.Core.Events;
using FFF.Data;
using FFF.Battle.Data;
using FFF.Battle.Modifier;


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
        public bool IsBattleActive { get; private set; }

        // --- 현재 전투의 데이터를 담을 공용 문맥 객체 (매 전투 스테이지마다 새로 갱신됨) ---
        public BattleContext Context { get; private set; }

        // ---  Modifier Manager 연결 ---
        [SerializeField] private ModifierManager _modifierManager;
        // --- 전투 내내 유지될 종합 배달통 ---
        public ModifierContext CurrentModifierContext { get; private set; }

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
            if (IsBattleActive)
            {
                Debug.LogWarning("[BattleManager] 이미 전투가 진행 중이라 StartBattle 중복 호출을 무시합니다.");
                return;
            }

            IsBattleActive = true;

            // Battle내에서 사용할 BattleContext 생성
            Context = new BattleContext();

            // GameManager로부터 구조체 형태의 전달 데이터 획득
            BattleEntryData entryData = GameManager.Instance.CurrentBattleEntryData;

            /// ----------  Player Data ---------------
            // 전달받은 구조체 내부의 Master Data(SO)를 복제 생성
            Context.PlayerData = new PlayerDataBattle(entryData.PlayerMasterData);
            /// --------------------------------------
            
            /// ----------  Enemy Data ---------------
            // 구조체에 담긴 적 ID를 이번 전투 Context에 할당
            Context.TargetEnemyId = entryData.TargetEnemyId;

            // TODO: [Step 4] entryData.EnemyBonusHealth 값을 EnemyDataBattle 생성/초기화 과정에 적용 필요.
            /// --------------------------------------
            Debug.Log($"적 아이디 : {Context.TargetEnemyId}");

            // 배달통 생성 및 초기화
            CurrentModifierContext = new ModifierContext
            {
                CurrentTurnNumber = 0, // 0턴부터 시작 : TurnReady를 거칠 때마다 1씩 증가하므로, 1턴시작을 위해선 0으로 시작
                Player = Context.PlayerData, // 복제된 로컬 데이터 할당
                //Enemy = EnemyManager.Instance.CurrentEnemy, 아직 적 시스템이 구현되지 않아서 임시로 주석화
                ActionHandResult = null
            };

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
            IsBattleActive = false;
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

        #region === Temp: 디버그용 갓 코드 ===
        
        private void Update()
        {
            // 백틱(`) 키를 누르면 즉시 적을 처치하고 전투 종료
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                TempKillCodeInBattle();
            }
        }

        private void TempKillCodeInBattle()
        {
            // 1. 중복 실행 방지: 이미 전투가 종료되었거나 종료 연출 중이라면 무시
            // (CurrentPhase 변수명은 실제 BattleManager 내부의 상태 변수명에 맞게 수정해주세요)
            if (CurrentPhase == TurnState.TurnEnd) 
            {
                return;
            }

            Debug.Log("[BattleManager] ☠️ 임시 킬 코드 발동! 적에게 99999 피해를 가하고 즉시 승리 처리합니다.");

            // 2. 씬 내의 적 객체(EnemyDataBattle)를 찾아서 99999 데미지 가하기
            // BattleManager에 직접적인 참조가 없을 수 있으므로 FindFirstObjectByType을 사용해 안전하게 탐색합니다.
            var enemy = UnityEngine.Object.FindFirstObjectByType<FFF.Battle.Enemy.EnemyDataBattle>();
            if (enemy != null)
            {
                enemy.TakeDamage(99999);
            }

            // 3. BattleContext의 승리 판정 플래그를 강제로 '플레이어 승리'로 설정
            if (Context != null)
            {
                Context.IsPlayerWinner = true;
            }

            // 4. 기존 FSM 흐름에 맞게 전투 종료 호출
            // 강제로 씬을 넘기는 것이 아니라 EndBattle()을 호출함으로써, 
            // BattleEndManager가 정상적으로 이벤트를 수신하고 보상 선택 UI를 띄우도록 합니다.
            EndBattle();
        }
        
        #endregion
    }    
}
