using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 
using UnityEngine.UI;
using FFF.Data;
using FFF.UI.Core;
using FFF.UI.Animation;
using FFF.Battle.Enemy;

namespace FFF.UI.Battle
{
    /// <summary>
    /// 전투 화면을 그리는 역할만 담당합니다. (스스로 판단하지 않음)
    /// </summary>
    public class BattleUIComponent : BaseUIComponent
    {
        [Header("=== UI 연결 ===")]
        [SerializeField] private TextMeshProUGUI _playerHpText;
        [SerializeField] private TextMeshProUGUI _enemyHpText;
        [SerializeField] private Transform _accessoryLayoutGroup; // 장신구 아이콘 부모
        [SerializeField] private Transform _jokerLayoutGroup;     // 조커 아이콘 부모

        // 아이템 생성 시연용 임시 프리팹 (나중엔 리소스 로드로 변경 가능)
        [SerializeField] private GameObject _tempAccessoryIconPrefab;
        [SerializeField] private GameObject _tempJockerIconPrefab;

        [Header("=== 신규 연결 (TurnReady 용) ===")]
        [SerializeField] private Transform _handLayoutGroup; // 내 카드가 스폰될 부모
        [SerializeField] private GameObject _cardPrefab;     // CardUIComponent가 붙은 프리팹
        [SerializeField] private TextMeshProUGUI _enemyIntentText; // 적 행동 텍스트
        [SerializeField] private Button _rerollButton;       // 리롤 버튼
        [SerializeField] private TextMeshProUGUI _rerollCountText; // 남은 리롤 횟수 표시
        [SerializeField] private GameObject _rerollComponetsContainer;     // 리롤 관련 컴포넌트들을 하위 자식으로 갖는 부모 EmptyObject

        [Header("=== 신규 연결 (TurnProceed 용) ===")]
        [SerializeField] private TextMeshProUGUI _expectedStrengthText;
        [SerializeField] private Button _endTurnButton;

        [Header("=== 신규 연결 (BattleEnd 결과창) ===")]
        [SerializeField] private GameObject _battleResultPanel; // 결과창 전체 패널
        [SerializeField] private TextMeshProUGUI _battleResultText; // 승패 텍스트

        [Header("=== 애니메이션 연출 참조 ===")]
        [Tooltip("덱(뽑을 산) UI의 RectTransform — 드로우 출발점")]
        [SerializeField] private RectTransform _deckAreaRect;

        [Tooltip("카드 간 드로우 시차 (초)")]
        [SerializeField] private float _drawStaggerDelay = 0.1f;

        [Tooltip("양끝 카드의 최대 기울기 (도)")]
        [SerializeField] private float _maxTiltAngle = 4f;

        public void SetPlayerHealth(int current, int max)
        {
            if (_playerHpText != null) 
                _playerHpText.text = $"Player HP: {current} / {max}";
            Debug.Log($"[BattleUI] 플레이어 체력 갱신: {current} / {max}");
        }

        public void SetEnemyHealth(int current, int max)
        {
            if (_enemyHpText != null) 
                _enemyHpText.text = $"Enemy HP: {current} / {max}";
            Debug.Log($"[BattleUI] 적 체력 갱신: {current} / {max}");
        }

        public void SetupItemIcons(List<string> accessoryIds, List<string> jokerIds)
        {
            // 실제 시연을 위해 하위 오브젝트로 더미 아이콘 생성 (프리팹이 있을 경우)
            if (_tempJockerIconPrefab != null && _tempAccessoryIconPrefab != null)
            {
                foreach(var acc in accessoryIds) Instantiate(_tempAccessoryIconPrefab, _accessoryLayoutGroup);
                foreach(var jkr in jokerIds) Instantiate(_tempJockerIconPrefab, _jokerLayoutGroup);
            }
            Debug.Log($"[BattleUI] 🎒 장신구 {accessoryIds.Count}개, 조커 {jokerIds.Count}개 아이콘 생성 (프리팹 기준)");
        }

        // 1. 적 의도 표시
        public void ShowEnemyIntent(EnemyIntent intent)
        {
            if (_enemyIntentText != null)
                _enemyIntentText.text = $"적 의도: {intent.Card1.DisplayName} + {intent.Card2.DisplayName}\n(예상 공격력: {intent.BasePower})";
        }

        // 2. 내 손패 카드들을 화면에 생성 + 드로우 연출
        public void UpdateHand(IReadOnlyList<HwaTuCard> handCards, Action<CardUIComponent> onCardClicked)
        {
            // 기존에 있던 카드 UI 전부 삭제 (초기화)
            foreach (Transform child in _handLayoutGroup) Destroy(child.gameObject);

            int totalCards = handCards.Count;

            // 새 카드 생성 + 드로우 연출
            for (int i = 0; i < totalCards; i++)
            {
                var card = handCards[i];
                GameObject cardObj = Instantiate(_cardPrefab, _handLayoutGroup, false);
                CardUIComponent cardUI = cardObj.GetComponent<CardUIComponent>();
                if (cardUI == null)
                {
                    Debug.LogError("[BattleUIComponent] CardPrefab에 CardUIComponent 스크립트가 부착되어 있지 않습니다!");
                    continue; 
                }
                cardUI.Setup(card, onCardClicked);

                // ── 애니메이션 연출 추가 ──
                // CardAnimator가 프리팹에 붙어있으면 드로우 연출, 없으면 기존처럼 즉시 배치
                CardAnimator animator = cardObj.GetComponent<CardAnimator>();
                if (animator != null)
                {
                    RectTransform cardRect = cardObj.GetComponent<RectTransform>();
                    Vector2 targetPos = cardRect.anchoredPosition;
                    float targetRot = CalculateCardRotation(i, totalCards);

                    Vector3 deckWorldPos = _deckAreaRect != null
                        ? _deckAreaRect.position
                        : _handLayoutGroup.position + Vector3.left * 300f;

                    animator.PlayDrawFromDeck(
                        deckWorldPos,
                        targetPos,
                        targetRot,
                        delay: i * _drawStaggerDelay
                    );
                }
            }
        }

        // 3. 리롤 버튼 및 횟수 텍스트 갱신
        public void UpdateRerollState(int remainRerolls, int selectedCount)
        {
            if (_rerollCountText != null)
                _rerollCountText.text = $"리롤: {remainRerolls}회 남음";

            // 요구사항: 선택된 카드가 1장 이상이고, 리롤 횟수가 남아있을 때만 활성화
            if (_rerollButton != null)
                _rerollButton.interactable = (remainRerolls > 0 && selectedCount > 0);
        }

        /// <summary>
        /// 멀리건 페이즈 전용 UI 요소들의 활성화 상태를 제어합니다.
        /// </summary>
        public void SetTurnReadyUIVisibility(bool isVisible)
        {
            if (_rerollComponetsContainer != null) _rerollComponetsContainer.gameObject.SetActive(isVisible);
            Debug.Log($"[BattleUI] TurnReady UI 가시성 설정: {isVisible}");
        }


        /// <summary>
        /// 예상 공격력을 표시합니다. "-"가 들어올 수 있습니다.
        /// </summary>
        public void SetExpectedStrengthText(string text)
        {
            if (_expectedStrengthText != null)
                _expectedStrengthText.text = $"예상 공격력: {text}";
        }

        /// <summary>
        /// 턴 종료 버튼의 활성화/비활성화 상태를 토글합니다.
        /// </summary>
        public void SetEndTurnButtonInteractable(bool isInteractable)
        {
            if (_endTurnButton != null)
                _endTurnButton.interactable = isInteractable;
        }

        /// <summary>
        /// TurnProceed 페이즈 전용 UI 요소들의 활성화 상태를 제어합니다.
        /// </summary>
        public void SetTurnProceedUIVisibility(bool isVisible)
        {
            if (_expectedStrengthText != null) _expectedStrengthText.gameObject.SetActive(isVisible);
            if (_endTurnButton != null) _endTurnButton.gameObject.SetActive(isVisible);
        }

        /// <summary>
        /// 화면에 표시된 내 카드(Clone)들을 폐기 연출 후 삭제합니다.
        /// CardAnimator가 없으면 기존처럼 즉시 삭제합니다.
        /// onComplete는 모든 카드가 사라진 뒤 호출됩니다.
        /// </summary>
        public void ClearHandUI(Action onComplete = null)
        {
            if (_handLayoutGroup == null || _handLayoutGroup.childCount == 0)
            {
                onComplete?.Invoke();
                return;
            }

            // CardAnimator가 하나라도 있으면 연출 후 삭제
            bool hasAnimator = false;
            int remaining = _handLayoutGroup.childCount;

            // childCount는 Destroy 호출 후에도 같은 프레임에서 바뀌지 않으므로 미리 저장
            List<Transform> children = new List<Transform>();
            foreach (Transform child in _handLayoutGroup) children.Add(child);

            foreach (Transform child in children)
            {
                CardAnimator animator = child.GetComponent<CardAnimator>();
                if (animator != null)
                {
                    hasAnimator = true;
                    animator.PlayDiscardToBottom(() =>
                    {
                        Destroy(child.gameObject);
                        remaining--;
                        if (remaining <= 0) onComplete?.Invoke();
                    });
                }
                else
                {
                    Destroy(child.gameObject);
                    remaining--;
                }
            }

            // CardAnimator가 하나도 없었으면 즉시 콜백
            if (!hasAnimator)
            {
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// 결과창을 띄우고 승패 텍스트를 설정합니다.
        /// </summary>
        public void ShowBattleResult(string resultMessage)
        {
            if (_battleResultPanel != null) _battleResultPanel.SetActive(true);
            if (_battleResultText != null) _battleResultText.text = resultMessage;
        }

        /// <summary>
        /// 결과창을 숨깁니다 (초기화용).
        /// </summary>
        public void HideBattleResult()
        {
            if (_battleResultPanel != null) _battleResultPanel.SetActive(false);
        }

        /// <summary>
        /// 부채꼴 배치 기울기 계산. 양끝 ±maxTilt, 중앙 0도.
        /// </summary>
        private float CalculateCardRotation(int index, int totalCards)
        {
            if (totalCards <= 1) return 0f;
            float center = (totalCards - 1) / 2f;
            return ((index - center) / center) * _maxTiltAngle;
        }
    }
}
