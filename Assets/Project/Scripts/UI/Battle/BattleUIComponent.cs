using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 
using UnityEngine.UI;
using UnityEngine.Serialization;
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
        [Serializable]
        private class ItemIconDefinition
        {
            [SerializeField] private string _id;
            [SerializeField] private Sprite _icon;
            [SerializeField] private string _displayName;

            public string Id => _id;
            public Sprite Icon => _icon;
            public string DisplayName => _displayName;
        }

        [Serializable]
        private class CardArtworkDefinition
        {
            [SerializeField] private string _cardId;
            [SerializeField] private Sprite _artwork;

            public string CardId => _cardId;
            public Sprite Artwork => _artwork;
        }

        [Header("=== UI 연결 ===")]
        [SerializeField] private TextMeshProUGUI _playerHpText;
        [SerializeField] private TextMeshProUGUI _enemyHpText;
        [SerializeField] private Transform _accessoryLayoutGroup; // 장신구 아이콘 부모
        [SerializeField] private Transform _jokerLayoutGroup;     // 조커 아이콘 부모

        // 아이템 생성 시연용 임시 프리팹 (나중엔 리소스 로드로 변경 가능)
        [SerializeField] private GameObject _tempAccessoryIconPrefab;
        [FormerlySerializedAs("_tempJockerIconPrefab")]
        [SerializeField] private GameObject _jokerIconPrefab;

        [Header("=== 아이템 아이콘 데이터 ===")]
        [SerializeField] private List<ItemIconDefinition> _accessoryIconDefinitions = new();
        [SerializeField] private List<ItemIconDefinition> _jokerIconDefinitions = new();

        [Header("=== 신규 연결 (TurnReady 용) ===")]
        [SerializeField] private Transform _handLayoutGroup; // 내 카드가 스폰될 부모
        [SerializeField] private GameObject _cardPrefab;     // CardUIComponent가 붙은 프리팹
        [SerializeField] private List<CardArtworkDefinition> _cardArtworkDefinitions = new();
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

        [Header("=== 신규 연결 (덱 카운트) ===")]
        [Tooltip("'뽑을 산 : N' 텍스트 — DrawingCard 오브젝트에 연결")]
        [SerializeField] private TextMeshProUGUI _drawPileCountText;
        [Tooltip("'묘지 : N' 텍스트 — DiscardCard 오브젝트에 연결")]
        [SerializeField] private TextMeshProUGUI _discardPileCountText;

        [Header("=== 애니메이션 연출 참조 ===")]
        [Tooltip("덱(뽑을 산) UI의 RectTransform — 드로우 출발점")]
        [SerializeField] private RectTransform _deckAreaRect;

        [Tooltip("카드 간 드로우 시차 (초)")]
        [SerializeField] private float _drawStaggerDelay = 0.2f;

        [Tooltip("양끝 카드의 최대 기울기 (도)")]
        [SerializeField] private float _maxTiltAngle = 4f;

        public void SetDrawPileCount(int count)
        {
            if (_drawPileCountText != null)
                _drawPileCountText.text = $"뽑을 산 : {count}";
        }

        public void SetDiscardPileCount(int count)
        {
            if (_discardPileCountText != null)
                _discardPileCountText.text = $"묘지 : {count}";
        }

        public void SetPileCounts(int drawPileCount, int discardPileCount)
        {
            SetDrawPileCount(drawPileCount);
            SetDiscardPileCount(discardPileCount);
        }

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
            accessoryIds ??= new List<string>();
            jokerIds ??= new List<string>();

            ClearChildren(_accessoryLayoutGroup);
            ClearChildren(_jokerLayoutGroup);

            CreateOrderedItemIcons(accessoryIds, _tempAccessoryIconPrefab, _accessoryLayoutGroup, _accessoryIconDefinitions);
            CreateOrderedItemIcons(jokerIds, _jokerIconPrefab, _jokerLayoutGroup, _jokerIconDefinitions);

            Debug.Log($"[BattleUI] 🎒 장신구 {accessoryIds.Count}개, 조커 {jokerIds.Count}개 아이콘 생성 (프리팹 기준)");
        }

        private void CreateOrderedItemIcons(
            IReadOnlyList<string> itemIds,
            GameObject iconPrefab,
            Transform parent,
            IReadOnlyList<ItemIconDefinition> iconDefinitions)
        {
            if (iconPrefab == null || parent == null) return;

            foreach (string itemId in itemIds)
            {
                if (string.IsNullOrEmpty(itemId)) continue;

                GameObject icon = Instantiate(iconPrefab, parent, false);
                icon.name = itemId;
                icon.transform.SetAsLastSibling();
                ApplyItemIconDefinition(icon, itemId, iconDefinitions);
            }
        }

        private void ApplyItemIconDefinition(
            GameObject iconObject,
            string itemId,
            IReadOnlyList<ItemIconDefinition> iconDefinitions)
        {
            ItemIconDefinition definition = FindItemIconDefinition(itemId, iconDefinitions);
            if (definition == null) return;

            if (!string.IsNullOrEmpty(definition.DisplayName))
                iconObject.name = definition.DisplayName;

            Image image = iconObject.GetComponent<Image>();
            if (image == null)
                image = iconObject.GetComponentInChildren<Image>();

            if (image != null && definition.Icon != null)
            {
                image.sprite = definition.Icon;
                image.preserveAspect = true;
            }

            TextMeshProUGUI label = iconObject.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = string.IsNullOrEmpty(definition.DisplayName) ? itemId : definition.DisplayName;
        }

        private ItemIconDefinition FindItemIconDefinition(
            string itemId,
            IReadOnlyList<ItemIconDefinition> iconDefinitions)
        {
            if (iconDefinitions == null) return null;

            foreach (ItemIconDefinition definition in iconDefinitions)
            {
                if (definition != null && definition.Id == itemId)
                    return definition;
            }

            return null;
        }

        private void ClearChildren(Transform parent)
        {
            if (parent == null) return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }

        // 1. 적 의도 표시
        public void ShowEnemyIntent(EnemyIntent intent)
        {
            if (_enemyIntentText != null)
                _enemyIntentText.text = $"적 의도: {intent.Card1.DisplayName} + {intent.Card2.DisplayName}\n(예상 공격력: {intent.BasePower})";
        }

        // 2. 내 손패 카드들을 화면에 생성 + 드로우 연출
        // 기존 UI 중 새 hand에 남아있는 카드는 재사용(연출 안 함), 빠진 카드는 폐기 연출, 신규 카드만 드로우 연출.
        public void UpdateHand(IReadOnlyList<HwaTuCard> handCards, Action<CardUIComponent> onCardClicked)
        {
            int totalCards = handCards.Count;

            // 비교용 HashSet (IReadOnlyList에는 Contains가 없음)
            var handSet = new HashSet<HwaTuCard>(handCards);

            // 1단계: 기존 자식들 분류 — 유지/제거 대상 식별
            var existingByCard = new Dictionary<HwaTuCard, GameObject>();
            var oldPositions = new Dictionary<GameObject, Vector2>();
            var oldRotations = new Dictionary<GameObject, float>();
            var toDestroy = new List<GameObject>();

            foreach (Transform child in _handLayoutGroup)
            {
                var childUI = child.GetComponent<CardUIComponent>();
                if (childUI == null || childUI.CardData == null)
                {
                    toDestroy.Add(child.gameObject);
                    continue;
                }

                if (handSet.Contains(childUI.CardData) && !existingByCard.ContainsKey(childUI.CardData))
                {
                    existingByCard[childUI.CardData] = child.gameObject;
                    var rt = child.GetComponent<RectTransform>();
                    oldPositions[child.gameObject] = rt.anchoredPosition;
                    oldRotations[child.gameObject] = rt.localEulerAngles.z;
                }
                else
                {
                    toDestroy.Add(child.gameObject);
                }
            }

            // 2단계: 빠진 카드는 폐기 연출 후 destroy. LayoutGroup 영향 안 받게 부모에서 분리.
            foreach (var go in toDestroy)
            {
                Transform detachedParent = _handLayoutGroup.parent != null ? _handLayoutGroup.parent : _handLayoutGroup;
                go.transform.SetParent(detachedParent, true);

                var animator = go.GetComponent<CardAnimator>();
                if (animator != null)
                {
                    var captured = go;
                    animator.PlayDiscardToBottom(() => Destroy(captured));
                }
                else
                {
                    Destroy(go);
                }
            }

            // 3단계: handCards 순서대로 cardObjects 구성 (재사용 + 신규 인스턴스화)
            var cardObjects = new List<GameObject>(totalCards);
            var newCards = new HashSet<GameObject>();
            foreach (var card in handCards)
            {
                if (existingByCard.TryGetValue(card, out var existingGo))
                {
                    cardObjects.Add(existingGo);
                    // 멀리건 종료 등으로 선택 상태가 남아있을 수 있으므로 시각적 선택 해제
                    var ui = existingGo.GetComponent<CardUIComponent>();
                    if (ui != null) ui.SetSelected(false);
                }
                else
                {
                    GameObject cardObj = Instantiate(_cardPrefab, _handLayoutGroup, false);
                    CardUIComponent cardUI = cardObj.GetComponent<CardUIComponent>();
                    if (cardUI == null)
                    {
                        Debug.LogError("[BattleUIComponent] CardPrefab에 CardUIComponent 스크립트가 부착되어 있지 않습니다!");
                        Destroy(cardObj);
                        continue;
                    }
                    cardUI.Setup(card, onCardClicked, FindCardArtwork(card.CardId));
                    cardObjects.Add(cardObj);
                    newCards.Add(cardObj);
                }
            }

            // 4단계: hand 순서대로 sibling index 재정렬
            for (int i = 0; i < cardObjects.Count; i++)
                cardObjects[i].transform.SetSiblingIndex(i);

            // 5단계: 모든 카드 ignoreLayout 일시 해제 → LayoutGroup이 새 위치 계산하도록
            foreach (var go in cardObjects)
            {
                var le = go.GetComponent<LayoutElement>();
                if (le != null) le.ignoreLayout = false;
            }
            if (_handLayoutGroup is RectTransform handRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(handRect);

            // 6단계: 각 카드의 새 목표 위치 캡처 → 기존 카드는 옛 위치로 되돌리고, ignoreLayout 다시 켜서 트윈
            int newCardOrderIdx = 0;
            for (int i = 0; i < cardObjects.Count; i++)
            {
                GameObject cardObj = cardObjects[i];
                RectTransform cardRect = cardObj.GetComponent<RectTransform>();
                Vector2 targetPos = cardRect.anchoredPosition;
                float targetRot = CalculateCardRotation(i, totalCards);

                bool isNew = newCards.Contains(cardObj);

                // 기존 카드는 옛 위치로 되돌려 두고 트윈 시작 (시각적 점프 없음)
                if (!isNew && oldPositions.TryGetValue(cardObj, out var oldPos))
                {
                    cardRect.anchoredPosition = oldPos;
                    if (oldRotations.TryGetValue(cardObj, out var oldRot))
                        cardRect.localEulerAngles = new Vector3(0, 0, oldRot);
                }

                // LayoutGroup이 트윈 중 위치 덮어쓰지 않도록
                LayoutElement layoutElement = cardObj.GetComponent<LayoutElement>();
                if (layoutElement == null)
                    layoutElement = cardObj.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;

                CardAnimator animator = cardObj.GetComponent<CardAnimator>();
                if (animator == null)
                    animator = cardObj.AddComponent<CardAnimator>();

                // 유지 카드는 이전 드로우 트윈이 중도 중단되어 alpha가 < 1로 남는 경우를 방지.
                // 신규 카드는 DrawSequence가 alpha를 0부터 다시 잡아주므로 건드리지 않음.
                if (!isNew)
                {
                    var cg = cardObj.GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = 1f;
                }

                if (isNew)
                {
                    Vector3 deckWorldPos = _deckAreaRect != null
                        ? _deckAreaRect.position
                        : _handLayoutGroup.position + Vector3.left * 300f;

                    animator.PlayDrawFromDeck(
                        deckWorldPos,
                        targetPos,
                        targetRot,
                        delay: newCardOrderIdx * _drawStaggerDelay
                    );
                    newCardOrderIdx++;
                }
                else
                {
                    // 기존 카드는 살짝 슬라이드만 (드로우 연출 없음)
                    animator.PlayArrangeToPosition(targetPos, targetRot);
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

            // childCount는 Destroy 호출 후에도 같은 프레임에서 바뀌지 않으므로 미리 저장
            List<Transform> children = new List<Transform>();
            foreach (Transform child in _handLayoutGroup) children.Add(child);

            // 폐기 연출 중인 카드를 즉시 layout group에서 떼어내,
            // 다음 턴의 UpdateHand가 (Destroy 지연으로) 살아있는 옛 카드를 "유지"로 오인하는 것을 방지.
            Transform detachedParent = _handLayoutGroup.parent != null ? _handLayoutGroup.parent : _handLayoutGroup;
            foreach (Transform child in children)
            {
                child.SetParent(detachedParent, true);
            }

            int remaining = children.Count;

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
        /// 위가 넓은 부채꼴(∨) 모양: 좌측 카드는 윗변이 좌측으로, 우측 카드는 윗변이 우측으로 기울어짐.
        /// </summary>
        private float CalculateCardRotation(int index, int totalCards)
        {
            if (totalCards <= 1) return 0f;
            float center = (totalCards - 1) / 2f;
            return ((center - index) / center) * _maxTiltAngle;
        }

        private Sprite FindCardArtwork(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || _cardArtworkDefinitions == null)
                return null;

            foreach (CardArtworkDefinition definition in _cardArtworkDefinitions)
            {
                if (definition != null && definition.CardId == cardId)
                    return definition.Artwork;
            }

            return null;
        }
    }
}
