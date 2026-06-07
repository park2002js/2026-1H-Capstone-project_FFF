using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Modifier;
using FFF.Battle.FSM; // BattleManager 참조용
using FFF.Battle.Card; // 추가: DeckSystem 참조용
using FFF.UI.Battle;   // 추가: BattleUIComponent 참조용
using FFF.Audio;       // 추가: 사운드 재생용
using FFF.Battle.Damage; // 추가: 예상 공격력 계산용
using FFF.Battle.Data;

// ItemFactoy 및 ItemBase 기반 코드 + IClickable을 사용함
namespace FFF.Data
{
    /// <summary>
    /// 조커 객체 생성 및 사용(클릭) 명령 중계 관리자.
    /// 임시 버프 관리는 전적으로 ModifierManager에 위임함.
    /// </summary>
    public class JokerManager : MonoBehaviour
    {
        [Header("=== 시스템 참조 ===")]
        [SerializeField] private BattleUIComponent _battleUI;
        [SerializeField] private DeckSystem _deckSystem;
        [SerializeField] private AccessoryManager _accessoryManager;

        /// <summary>현재 보유 중인 조커 C# 로직 객체 목록</summary>
        private readonly List<ItemBase> _heldJokers = new();
        public IReadOnlyList<ItemBase> HeldJokers => _heldJokers;

        /// <summary>
        /// 전투 시작 시 ID 리스트를 기반으로 조커 객체 생성.
        /// </summary>
        public void Initialize(List<string> jokerIds)
        {
            _heldJokers.Clear();
            foreach (var id in jokerIds)
            {
                var item = ItemFactory.CreateItem(id);
                if (item != null)
                {
                    _heldJokers.Add(item);
                }
            }
            Debug.Log($"[JokerManager] 조커 {jokerIds.Count}개 로드 및 생성 완료.");
        }

        // /// <summary>
        // /// 외부(UI) 요청에 의한 조커 사용 처리.
        // /// </summary>
        // public bool UseJoker(int jokerIndex, ModifierContext context)
        // {
        //     if (jokerIndex < 0 || jokerIndex >= _heldJokers.Count)
        //     {
        //         Debug.LogWarning($"[JokerManager] 잘못된 조커 인덱스: {jokerIndex}");
        //         return false;
        //     }

        //     var joker = _heldJokers[jokerIndex];

        //     // IClickable 인터페이스 확인으로 사용 가능한 아이템인지 판별
        //     if (joker is IClickable clickableJoker)
        //     {
        //         // 성공적으로 효과가 등록된 직후, 매니저 및 플레이어 데이터에서 영구 삭제하기 위한 콜백
        //         Action consumeAction = () =>
        //         {
        //             string consumedId = joker.Id;
        //             _heldJokers.RemoveAt(jokerIndex);
                    
        //             // 중앙 데이터 동기화 (PlayerDataBattle 리스트에서 제거)
        //             BattleManager.Instance.Context.PlayerData.ConsumeJoker(consumedId);
                    
        //             Debug.Log($"[JokerManager] 조커 소모 처리 완료: {consumedId}");
        //         };

        //         return clickableJoker.Use(context, consumeAction);
        //     }

        //     Debug.LogWarning($"[JokerManager] 클릭 불가능한 아이템에 대한 조커 사용 시도: {joker.Id}");
        //     return false;
        // }

        /// <summary>
        /// 외부(UI) 조커 클릭 이벤트 수신 및 처리.
        /// 페이즈 검증, 소모 처리, UI 갱신을 일괄 수행함.
        /// </summary>
        public void HandleJokerClicked(int jokerIndex, string jokerId)
        {
            BattleManager battleManager = BattleManager.Instance;
            if (battleManager == null || battleManager.Context?.PlayerData == null)
            {
                SoundManager.PlayUiSound(SoundIds.UiError);
                return;
            }

            // 요구사항: 조커는 TurnProceed와 TurnReady페이즈에서만 사용 가능함 
            if (battleManager.CurrentPhase == TurnState.TurnEnd)
            {
                SoundManager.PlayUiSound(SoundIds.UiError);
                Debug.LogWarning($"[JokerManager] 현재 페이즈에서는 조커를 사용할 수 없습니다: {battleManager.CurrentPhase}");
                return;
            }

            PlayerDataBattle player = battleManager.Context.PlayerData;
            
            // 인덱스 및 ID 유효성 검증
            if (jokerIndex < 0 || jokerIndex >= _heldJokers.Count || _heldJokers[jokerIndex].Id != jokerId)
            {
                SoundManager.PlayUiSound(SoundIds.UiError);
                Debug.LogWarning($"[JokerManager] 유효하지 않은 조커 클릭: 인덱스 {jokerIndex}, ID {jokerId}");
                return;
            }

            var joker = _heldJokers[jokerIndex];

            // IClickable 인터페이스 기반 조커 발동
            if (joker is IClickable clickableJoker)
            {
                // 사용 성공 시 즉각적인 데이터 및 UI 동기화를 위한 콜백 정의
                Action consumeAction = () =>
                {
                    string consumedId = joker.Id;
                    _heldJokers.RemoveAt(jokerIndex);
                    
                    // 마스터 데이터 동기화 대비 로컬 리스트 제거
                    player.ConsumeJoker(consumedId);
                    Debug.Log($"[JokerManager] 조커 소모 처리 완료: {consumedId}");

                    // UI 즉시 갱신 (아이콘 재구성 및 예상 공격력 갱신)
                    RefreshBattleUIAfterConsume(battleManager);
                };

                if (clickableJoker.Use(battleManager.CurrentModifierContext, consumeAction))
                {
                    SoundManager.PlaySfxSound(SoundIds.SfxJokerActivate);
                }
                else
                {
                    SoundManager.PlayUiSound(SoundIds.UiError);
                }
            }
            else
            {
                Debug.LogWarning($"[JokerManager] 클릭 불가능한 아이템에 대한 조커 사용 시도: {joker.Id}");
                SoundManager.PlayUiSound(SoundIds.UiError);
            }
        }

        /// <summary>
        /// 조커 소모 후 변경된 데이터를 바탕으로 UI 시각 정보 갱신.
        /// </summary>
        private void RefreshBattleUIAfterConsume(BattleManager battleManager)
        {
            if (_battleUI == null) return;

            // 아이콘 리스트 재렌더링
            _battleUI.SetupItemIcons(_accessoryManager.EquippedAccessories, _heldJokers);
            _battleUI.UpdateRerollState(_deckSystem.RerollsRemaining, _deckSystem.SelectedCards.Count);

            // 공격력 버프류 조커 사용 대비 실시간 데미지 재계산
            if (_deckSystem.SelectedCards.Count == 2)
            {
                var calculator = new CombatCalculator();
                int expectedPower = calculator.Strength.CalculateExpectedStrength(
                    _deckSystem.SelectedCards[0],
                    _deckSystem.SelectedCards[1],
                    battleManager.CurrentModifierContext);
                _battleUI.SetExpectedStrengthText(expectedPower.ToString());
            }
        }
    }
}