using UnityEngine;
using FFF.Battle.FSM;
using FFF.Battle.Card;
using FFF.UI.Battle;
using FFF.Battle.Damage;
using FFF.Core.Events;
using FFF.Battle.Modifier;

namespace FFF.Battle.FSM
{
    public class TurnProceedManager : MonoBehaviour
    {
        [Header("=== 시스템 참조 ===")]
        [SerializeField] private BattleManager _battleManager;
        [SerializeField] private DeckSystem _deckSystem;
        [SerializeField] private BattleUIComponent _battleUI;
        [SerializeField] private ModifierManager _modifierManager;

        [Header("=== 수신할 이벤트 ===")]
        [SerializeField] private GameEvent _onTurnProceedEvent;
        // DeckSystem이 카드가 클릭될 때마다 발송하는 이벤트 (동적 UI 갱신용)
        [SerializeField] private GameEvent _onSelectionChangedEvent; 

        private CombatCalculator _combatCalculator;

        private void Awake()
        {
            // MonoBehaviour가 아닌 순수 C# 객체이므로 직접 생성해서 들고 있습니다.
            _combatCalculator = new CombatCalculator();
        }

        private void OnEnable()
        {
            if (_onTurnProceedEvent != null) _onTurnProceedEvent.Subscribe(HandleTurnProceedEnter);
            if (_onSelectionChangedEvent != null) _onSelectionChangedEvent.Subscribe(HandleSelectionChanged);
        }

        private void OnDisable()
        {
            if (_onTurnProceedEvent != null) _onTurnProceedEvent.Unsubscribe(HandleTurnProceedEnter);
            if (_onSelectionChangedEvent != null) _onSelectionChangedEvent.Unsubscribe(HandleSelectionChanged);
        }

        // 1. TurnProceed 상태 진입 시 초기화
        private void HandleTurnProceedEnter()
        {
            _battleUI.SetTurnProceedUIVisibility(true);
            Debug.Log("========== [TurnProceed] 메인 행동 페이즈 진입 ==========");
            
            // 처음엔 선택된 카드가 없으므로 "-" 표기 및 버튼 비활성화
            _battleUI.SetExpectedStrengthText("-");
            _battleUI.SetEndTurnButtonInteractable(false);
        }

        // 2. 카드가 선택되거나 해제될 때마다 실시간으로 호출됨 (UI 롤백 처리 포함)
        private void HandleSelectionChanged()
        {
            // 현재 TurnProceed 상태가 아니라면 무시
            if (_battleManager.CurrentPhase != TurnState.TurnProceed) return;

            var selected = _deckSystem.SelectedCards;

            // 정책 1: 딱 2장이 선택되었을 때만 계산
            if (selected.Count == 2)
            {
                // Manager와 Context를 넘겨서 예상 데미지를 계산합니다.
                int expectedPower = _combatCalculator.Strength.CalculateExpectedStrength(
                    selected[0], 
                    selected[1], 
                    _modifierManager, 
                    _battleManager.CurrentModifierContext
                );
                
                _battleUI.SetExpectedStrengthText(expectedPower.ToString());
                _battleUI.SetEndTurnButtonInteractable(true);
            }
            else
            {
                // 정책 2: 2장이 아니면 즉시 "-" 표기 및 턴 종료 버튼 락(Lock)
                _battleUI.SetExpectedStrengthText("-");
                _battleUI.SetEndTurnButtonInteractable(false);
            }
        }

        // 3. 턴 종료 버튼 클릭 시 호출
        public void OnEndTurnButtonClicked()
        {
            // 안전 장치: 2장이 아닐 땐 강제로 넘어갈 수 없음
            if (_deckSystem.SelectedCards.Count != 2)
            {
                Debug.LogWarning("[TurnProceed] 카드 2장을 선택해야 턴을 종료할 수 있습니다.");
                return;
            }

            Debug.Log("[TurnProceed] 플레이어 행동 확정. TurnEnd로 넘어갑니다.");
            _battleManager.ChangeState(TurnState.TurnEnd);
        }
    }
}