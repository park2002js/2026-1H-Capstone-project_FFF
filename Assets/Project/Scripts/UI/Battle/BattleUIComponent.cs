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
using FFF.Audio;
using FFF.Core;
using FFF.Map;
using FFF.UI.Map;

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

        public enum RewardKind
        {
            HwaTuCard,
            Joker,
            Accessory
        }

        public sealed class RewardOption
        {
            public string Id;
            public RewardKind Kind;
            public string PayloadId;
            public string DisplayName;
            public string Category;
            public string Description;
            public Sprite Artwork;
        }

        [Header("=== UI 연결 ===")]
        [SerializeField] private TextMeshProUGUI _playerHpText;
        [SerializeField] private TextMeshProUGUI _enemyHpText;
        [SerializeField] private Transform _accessoryLayoutGroup; // 장신구 아이콘 부모
        [SerializeField] private Transform _jokerLayoutGroup;     // 조커 아이콘 부모
        [SerializeField] private Sprite _playerPortraitSprite;

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

        private GameObject _rewardPanel;
        private Transform _rewardBoxContainer;
        private TextMeshProUGUI _rewardFeedbackText;
        private Button _nextStageButton;
        private Action<RewardOption> _onRewardSelected;
        private Action _onRewardNextStage;
        private bool _hasSelectedReward;
        private bool _isFinalRewardSelection;
        private bool _hideRewardDetailsUntilSelection;

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

        private GameObject _topHudRoot;
        private TextMeshProUGUI _topHealthText;
        private TextMeshProUGUI _topGoldText;
        private Transform _topJokerSlotRoot;
        private Transform _topAccessorySlotRoot;
        private GameObject _deckOverlay;
        private GameObject _mapOverlay;
        private GameObject _settingsOverlay;
        private readonly List<string> _currentDeckCardIds = new List<string>();

        protected override void OnInitialize()
        {
            EnsureTopHud();
            HideBattleResult();
            HideRewardSelection();
        }

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
            EnsureTopHud();

            if (_playerHpText != null) 
                _playerHpText.text = $"Player HP: {current} / {max}";
            if (_topHealthText != null)
                _topHealthText.text = $"{current}/{max}";

            Debug.Log($"[BattleUI] 플레이어 체력 갱신: {current} / {max}");
        }

        public void SetPlayerGold(int gold)
        {
            EnsureTopHud();

            if (_topGoldText != null)
                _topGoldText.text = gold.ToString();
        }

        public void SetDeckCards(IReadOnlyList<string> deckCardIds)
        {
            _currentDeckCardIds.Clear();
            if (deckCardIds != null)
                _currentDeckCardIds.AddRange(deckCardIds);
        }

        public void SetEnemyHealth(int current, int max)
        {
            if (_enemyHpText != null) 
                _enemyHpText.text = $"Enemy HP: {current} / {max}";
            Debug.Log($"[BattleUI] 적 체력 갱신: {current} / {max}");
        }

        public void SetupItemIcons(List<string> accessoryIds, List<string> jokerIds)
        {
            EnsureTopHud();

            accessoryIds ??= new List<string>();
            jokerIds ??= new List<string>();

            ClearChildren(_accessoryLayoutGroup);
            ClearChildren(_jokerLayoutGroup);

            CreateOrderedItemIcons(accessoryIds, _tempAccessoryIconPrefab, _accessoryLayoutGroup, _accessoryIconDefinitions);

            int jokerCount = Mathf.Min(jokerIds.Count, PlayerDataSO.MaxHeldJokerCount);
            for (int i = 0; i < jokerCount; i++)
            {
                CreateItemIcon(jokerIds[i], _jokerIconPrefab, _jokerLayoutGroup, _jokerIconDefinitions);
            }

            CreateJokerSlotPlaceholders(PlayerDataSO.MaxHeldJokerCount - jokerCount);

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
                CreateItemIcon(itemId, iconPrefab, parent, iconDefinitions);
            }
        }

        private void CreateItemIcon(
            string itemId,
            GameObject iconPrefab,
            Transform parent,
            IReadOnlyList<ItemIconDefinition> iconDefinitions)
        {
            if (string.IsNullOrEmpty(itemId) || iconPrefab == null || parent == null) return;

            GameObject icon = Instantiate(iconPrefab, parent, false);
            icon.name = itemId;
            icon.transform.SetAsLastSibling();
            ApplyItemIconDefinition(icon, itemId, iconDefinitions);
            ConfigureHudIcon(icon);
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

        private void EnsureTopHud()
        {
            if (_topHudRoot != null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            _topHudRoot = CreateUIObject("TopRunHud", parent);
            RectTransform rootRect = _topHudRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.sizeDelta = new Vector2(0f, 76f);
            rootRect.anchoredPosition = Vector2.zero;
            _topHudRoot.transform.SetAsLastSibling();

            Image background = _topHudRoot.AddComponent<Image>();
            background.color = new Color(0.08f, 0.12f, 0.15f, 0.92f);
            background.raycastTarget = false;

            BuildPortrait(_topHudRoot.transform);
            BuildHealthBlock(_topHudRoot.transform);
            BuildGoldBlock(_topHudRoot.transform);
            BuildJokerBlock(_topHudRoot.transform);
            BuildHudButtons(_topHudRoot.transform);
            BuildAccessoryRow(parent);
        }

        private void BuildPortrait(Transform parent)
        {
            GameObject frame = CreateUIObject("PortraitFrame", parent);
            RectTransform rect = frame.GetComponent<RectTransform>();
            SetStretch(rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, 58f), new Vector2(42f, 0f));

            Image image = frame.AddComponent<Image>();
            image.sprite = _playerPortraitSprite;
            image.color = _playerPortraitSprite != null ? Color.white : new Color(0.88f, 0.78f, 0.48f, 1f);
            image.preserveAspect = true;

            if (_playerPortraitSprite == null)
            {
                TextMeshProUGUI label = CreateRewardText("Text_PortraitFallback", frame.transform, "P", 24,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                SetStretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
        }

        private void BuildHealthBlock(Transform parent)
        {
            TextMeshProUGUI heart = CreateRewardText("Text_HeartIcon", parent, "♥", 32,
                TextAlignmentOptions.Center, FontStyles.Bold);
            heart.color = new Color(1f, 0.25f, 0.28f, 1f);
            SetStretch(heart.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 42f), new Vector2(114f, -1f));

            _topHealthText = CreateRewardText("Text_TopHealth", parent, "0/0", 25,
                TextAlignmentOptions.Left, FontStyles.Bold);
            _topHealthText.color = new Color(1f, 0.35f, 0.35f, 1f);
            SetStretch(_topHealthText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(112f, 44f), new Vector2(188f, -1f));
        }

        private void BuildGoldBlock(Transform parent)
        {
            TextMeshProUGUI coin = CreateRewardText("Text_CoinIcon", parent, "전", 24,
                TextAlignmentOptions.Center, FontStyles.Bold);
            coin.color = new Color(1f, 0.82f, 0.2f, 1f);
            SetStretch(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 42f), new Vector2(278f, -1f));

            _topGoldText = CreateRewardText("Text_TopGold", parent, "0", 25,
                TextAlignmentOptions.Left, FontStyles.Bold);
            _topGoldText.color = new Color(1f, 0.84f, 0.28f, 1f);
            SetStretch(_topGoldText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(90f, 44f), new Vector2(340f, -1f));
        }

        private void BuildJokerBlock(Transform parent)
        {
            GameObject block = CreateUIObject("JokerHudBlock", parent);
            RectTransform rect = block.GetComponent<RectTransform>();
            SetStretch(rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(216f, 58f), new Vector2(482f, 0f));

            Image bg = block.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.04f, 0.06f, 0.56f);
            bg.raycastTarget = false;

            Transform originalJokerLayout = _jokerLayoutGroup;
            if (originalJokerLayout == null)
            {
                GameObject layoutGo = CreateUIObject("Layout_Jokers", block.transform);
                _jokerLayoutGroup = layoutGo.transform;
            }
            else
            {
                originalJokerLayout.SetParent(block.transform, false);
                _jokerLayoutGroup = originalJokerLayout;
            }

            RectTransform layoutRect = _jokerLayoutGroup.GetComponent<RectTransform>();
            if (layoutRect != null)
            {
                layoutRect.anchorMin = Vector2.zero;
                layoutRect.anchorMax = Vector2.one;
                layoutRect.offsetMin = new Vector2(10f, 7f);
                layoutRect.offsetMax = new Vector2(-10f, -7f);
                layoutRect.anchoredPosition = Vector2.zero;
            }

            HorizontalLayoutGroup layout = _jokerLayoutGroup.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
                layout = _jokerLayoutGroup.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 9f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            _topJokerSlotRoot = _jokerLayoutGroup;
        }

        private void BuildAccessoryRow(Transform parent)
        {
            GameObject block = CreateUIObject("AccessoryHudRow", parent);
            RectTransform rect = block.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 44f);
            rect.anchoredPosition = new Vector2(0f, -76f);

            Image bg = block.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.07f, 0.08f, 0.45f);
            bg.raycastTarget = false;

            Transform originalAccessoryLayout = _accessoryLayoutGroup;
            if (originalAccessoryLayout == null)
            {
                GameObject layoutGo = CreateUIObject("Layout_Accessories", block.transform);
                _accessoryLayoutGroup = layoutGo.transform;
            }
            else
            {
                originalAccessoryLayout.SetParent(block.transform, false);
                _accessoryLayoutGroup = originalAccessoryLayout;
            }

            RectTransform layoutRect = _accessoryLayoutGroup.GetComponent<RectTransform>();
            if (layoutRect != null)
            {
                layoutRect.anchorMin = new Vector2(0f, 0f);
                layoutRect.anchorMax = new Vector2(1f, 1f);
                layoutRect.offsetMin = new Vector2(18f, 4f);
                layoutRect.offsetMax = new Vector2(-18f, -4f);
            }

            HorizontalLayoutGroup layout = _accessoryLayoutGroup.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
                layout = _accessoryLayoutGroup.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            _topAccessorySlotRoot = _accessoryLayoutGroup;
        }

        private void BuildHudButtons(Transform parent)
        {
            CreateHudButton("Button_TopMap", parent, "맵", new Vector2(-218f, 0f), ShowMapOverlay);
            CreateHudButton("Button_TopDeck", parent, "덱", new Vector2(-146f, 0f), ShowDeckOverlay);
            CreateHudButton("Button_TopSettings", parent, "설정", new Vector2(-66f, 0f), ShowSettingsOverlay);
        }

        private Button CreateHudButton(string name, Transform parent, string label, Vector2 position, Action onClick)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetStretch(rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(64f, 48f), position);

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.16f, 0.2f, 0.23f, 0.95f);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                SoundManager.PlayDefaultUiClick();
                onClick?.Invoke();
            });

            TextMeshProUGUI text = CreateRewardText($"Text_{name}", go.transform, label, 18,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private void ConfigureHudIcon(GameObject icon)
        {
            if (icon == null)
                return;

            RectTransform rect = icon.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = new Vector2(42f, 42f);

            LayoutElement layout = icon.GetComponent<LayoutElement>();
            if (layout == null)
                layout = icon.AddComponent<LayoutElement>();

            layout.preferredWidth = 42f;
            layout.preferredHeight = 42f;
            layout.minWidth = 42f;
            layout.minHeight = 42f;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }

        private void CreateJokerSlotPlaceholders(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject slot = CreateUIObject("EmptyJokerSlot", _jokerLayoutGroup);
                Image image = slot.AddComponent<Image>();
                image.color = new Color(0.15f, 0.18f, 0.19f, 0.74f);
                ConfigureHudIcon(slot);
            }
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
            HideRewardSelection();
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

        public void ShowRewardSelection(
            IReadOnlyList<RewardOption> options,
            Action<RewardOption> onRewardSelected,
            Action onNextStage,
            string promptMessage = "물음표 보따리 하나를 선택하세요.",
            bool isFinalRewardSelection = true,
            bool hideRewardDetailsUntilSelection = true)
        {
            HideBattleResult();

            if (_rewardPanel == null)
                BuildRewardPanel();

            _onRewardSelected = onRewardSelected;
            _onRewardNextStage = onNextStage;
            _hasSelectedReward = false;
            _isFinalRewardSelection = isFinalRewardSelection;
            _hideRewardDetailsUntilSelection = hideRewardDetailsUntilSelection;

            if (_rewardPanel != null)
            {
                _rewardPanel.SetActive(true);
                _rewardPanel.transform.SetAsLastSibling();
            }

            if (_rewardFeedbackText != null)
                _rewardFeedbackText.text = promptMessage;

            if (_nextStageButton != null)
                _nextStageButton.gameObject.SetActive(false);

            ClearChildren(_rewardBoxContainer);
            if (options == null) return;

            foreach (RewardOption option in options)
            {
                CreateRewardBox(option);
            }
        }

        public void HideRewardSelection()
        {
            if (_rewardPanel != null)
                _rewardPanel.SetActive(false);
        }

        private void ShowDeckOverlay()
        {
            if (_deckOverlay == null)
                BuildDeckOverlay();

            RefreshDeckOverlay();
            _deckOverlay.SetActive(true);
            _deckOverlay.transform.SetAsLastSibling();
        }

        private void BuildDeckOverlay()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            _deckOverlay = CreateOverlayRoot("DeckOverlay");

            GameObject panel = CreatePanel(_deckOverlay.transform, "보유 덱", new Vector2(900f, 560f));

            GameObject scrollRoot = CreateUIObject("DeckScrollView", panel.transform);
            RectTransform scrollRect = scrollRoot.GetComponent<RectTransform>();
            SetStretch(scrollRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(820f, 390f), new Vector2(0f, -10f));

            Image scrollBg = scrollRoot.AddComponent<Image>();
            scrollBg.color = new Color(0.05f, 0.06f, 0.07f, 0.62f);

            ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            GameObject viewport = CreateUIObject("Viewport", scrollRoot.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(12f, 12f);
            viewportRect.offsetMax = new Vector2(-12f, -12f);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);

            GameObject content = CreateUIObject("DeckContent", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);

            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(98f, 146f);
            grid.spacing = new Vector2(14f, 18f);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            Button close = CreateOverlayButton("Button_CloseDeck", panel.transform, "닫기", new Vector2(0f, -234f), () => _deckOverlay.SetActive(false));
            close.gameObject.transform.SetAsLastSibling();

            _deckOverlay.SetActive(false);
        }

        private void RefreshDeckOverlay()
        {
            Transform content = _deckOverlay != null ? _deckOverlay.transform.Find("Panel/DeckScrollView/Viewport/DeckContent") : null;
            ClearChildren(content);
            if (content == null)
                return;

            foreach (string cardId in _currentDeckCardIds)
            {
                HwaTuCard card = HwaTuCardDatabase.FindById(cardId);
                if (_cardPrefab != null && card != null)
                {
                    GameObject cardObject = Instantiate(_cardPrefab, content, false);
                    RectTransform rect = cardObject.GetComponent<RectTransform>();
                    if (rect != null)
                        rect.sizeDelta = new Vector2(98f, 146f);

                    CardUIComponent cardView = cardObject.GetComponent<CardUIComponent>();
                    if (cardView != null)
                        cardView.Setup(card, _ => { }, HwaTuCardDatabase.GetArtwork(cardId));
                }
                else
                {
                    GameObject fallback = CreateUIObject($"DeckCard_{cardId}", content);
                    Image image = fallback.AddComponent<Image>();
                    image.color = new Color(0.18f, 0.2f, 0.24f, 1f);
                    TextMeshProUGUI text = CreateRewardText("Text_CardName", fallback.transform, card != null ? card.DisplayName : cardId,
                        15, TextAlignmentOptions.Center, FontStyles.Bold);
                    SetStretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                }
            }
        }

        private void ShowMapOverlay()
        {
            if (_mapOverlay == null)
                BuildMapOverlay();

            RefreshMapOverlay();
            _mapOverlay.SetActive(true);
            _mapOverlay.transform.SetAsLastSibling();
        }

        private void BuildMapOverlay()
        {
            _mapOverlay = CreateOverlayRoot("MapOverlay");
            GameObject panel = CreatePanel(_mapOverlay.transform, "스테이지 맵", new Vector2(920f, 640f));

            GameObject scrollRoot = CreateUIObject("MapScrollView", panel.transform);
            RectTransform scrollRect = scrollRoot.GetComponent<RectTransform>();
            SetStretch(scrollRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(820f, 470f), new Vector2(0f, -8f));

            Image scrollBackground = scrollRoot.AddComponent<Image>();
            scrollBackground.color = new Color(0.05f, 0.06f, 0.07f, 0.62f);

            ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            GameObject viewport = CreateUIObject("Viewport", scrollRoot.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(12f, 12f);
            viewportRect.offsetMax = new Vector2(-12f, -12f);

            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject mapContent = CreateUIObject("MapContent", viewport.transform);
            RectTransform contentRect = mapContent.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0f);
            contentRect.anchorMax = new Vector2(0.5f, 0f);
            contentRect.pivot = new Vector2(0.5f, 0f);
            contentRect.sizeDelta = new Vector2(760f, 0f);
            contentRect.anchoredPosition = Vector2.zero;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            Button close = CreateOverlayButton("Button_CloseMap", panel.transform, "닫기", new Vector2(0f, -254f), () => _mapOverlay.SetActive(false));
            close.gameObject.transform.SetAsLastSibling();

            _mapOverlay.SetActive(false);
        }

        private void RefreshMapOverlay()
        {
            Transform mapContent = _mapOverlay != null ? _mapOverlay.transform.Find("Panel/MapScrollView/Viewport/MapContent") : null;
            ClearChildren(mapContent);
            if (mapContent == null)
                return;

            MapData mapData = GameManager.Instance != null ? GameManager.Instance.CurrentMapData : null;
            if (mapData == null)
            {
                TextMeshProUGUI text = CreateRewardText("Text_NoMap", mapContent, "확인할 스테이지 맵이 없습니다.", 24,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                SetStretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                return;
            }

            MapUIComponent mapView = mapContent.GetComponent<MapUIComponent>();
            if (mapView == null)
                mapView = mapContent.gameObject.AddComponent<MapUIComponent>();

            mapView.BuildReadOnlyMap(mapContent.GetComponent<RectTransform>(), mapData);

            ScrollRect scroll = _mapOverlay.GetComponentInChildren<ScrollRect>(true);
            if (scroll != null)
                scroll.verticalNormalizedPosition = 0f;
        }

        private void ShowSettingsOverlay()
        {
            if (_settingsOverlay == null)
                BuildSettingsOverlay();

            _settingsOverlay.SetActive(true);
            _settingsOverlay.transform.SetAsLastSibling();
        }

        private void BuildSettingsOverlay()
        {
            _settingsOverlay = CreateOverlayRoot("BattleSettingsOverlay");
            GameObject panel = CreatePanel(_settingsOverlay.transform, "환경 설정", new Vector2(660f, 460f));

            CreateSoundSlider(panel.transform, "전체 음량", SoundBus.Master, 118f);
            CreateSoundSlider(panel.transform, "배경음", SoundBus.Bgm, 56f);
            CreateSoundSlider(panel.transform, "효과음", SoundBus.Sfx, -6f);
            CreateSoundSlider(panel.transform, "UI 효과음", SoundBus.Ui, -68f);

            Button close = CreateOverlayButton("Button_CloseSettings", panel.transform, "닫기", new Vector2(0f, -178f), () => _settingsOverlay.SetActive(false));
            close.gameObject.transform.SetAsLastSibling();

            _settingsOverlay.SetActive(false);
        }

        private void CreateSoundSlider(Transform parent, string label, SoundBus bus, float y)
        {
            SoundManager soundManager = SoundManager.EnsureExists();

            TextMeshProUGUI name = CreateRewardText($"Text_{bus}Label", parent, label, 20,
                TextAlignmentOptions.Left, FontStyles.Bold);
            SetStretch(name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(140f, 34f), new Vector2(-205f, y));

            TextMeshProUGUI valueText = CreateRewardText($"Text_{bus}Value", parent, ToPercent(soundManager.GetVolume(bus)), 19,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(valueText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70f, 34f), new Vector2(178f, y));

            Slider slider = CreateOverlaySlider($"Slider_{bus}", parent, soundManager.GetVolume(bus), new Vector2(250f, 24f), new Vector2(-20f, y));
            slider.onValueChanged.AddListener(value =>
            {
                soundManager.SetVolume(bus, value);
                valueText.text = ToPercent(value);
            });
        }

        private GameObject CreateOverlayRoot(string name)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            GameObject overlay = CreateUIObject(name, parent);
            RectTransform rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.62f);
            return overlay;
        }

        private GameObject CreatePanel(Transform parent, string titleText, Vector2 size)
        {
            GameObject panel = CreateUIObject("Panel", parent);
            RectTransform rect = panel.GetComponent<RectTransform>();
            SetStretch(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, Vector2.zero);

            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.12f, 0.14f, 0.17f, 0.98f);

            TextMeshProUGUI title = CreateRewardText("Text_Title", panel.transform, titleText, 34,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(420f, 54f), new Vector2(0f, -42f));
            return panel;
        }

        private Button CreateOverlayButton(string name, Transform parent, string label, Vector2 position, Action onClick)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetStretch(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160f, 46f), position);

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.32f, 0.35f, 0.4f, 1f);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                SoundManager.PlayDefaultUiClick();
                onClick?.Invoke();
            });

            TextMeshProUGUI text = CreateRewardText($"Text_{name}", go.transform, label, 20,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private Slider CreateOverlaySlider(string name, Transform parent, float value, Vector2 size, Vector2 position)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetStretch(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, position);

            Slider slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = Mathf.Clamp01(value);

            GameObject background = CreateUIObject("Background", go.transform);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(1f, 0.5f);
            bgRect.sizeDelta = new Vector2(0f, 8f);
            bgRect.anchoredPosition = Vector2.zero;
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.22f, 0.25f, 0.29f, 1f);

            GameObject fill = CreateUIObject("Fill", background.transform);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(1f, 0.84f, 0.14f, 1f);

            GameObject handle = CreateUIObject("Handle", go.transform);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(22f, 22f);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            return slider;
        }

        private static string ToPercent(float value)
        {
            return $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
        }

        private void BuildRewardPanel()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            _rewardPanel = CreateUIObject("RewardSelectionPanel", parent);
            RectTransform rootRect = _rewardPanel.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image dim = _rewardPanel.AddComponent<Image>();
            dim.color = new Color(0.04f, 0.05f, 0.07f, 0.86f);

            TextMeshProUGUI title = CreateRewardText("Text_RewardTitle", _rewardPanel.transform, "보상 선택", 44,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(680f, 72f), new Vector2(0f, -82f));

            _rewardFeedbackText = CreateRewardText("Text_RewardFeedback", _rewardPanel.transform,
                "물음표 보따리 하나를 선택하세요.", 24, TextAlignmentOptions.Center, FontStyles.Normal);
            SetStretch(_rewardFeedbackText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(760f, 58f), new Vector2(0f, -152f));

            GameObject container = CreateUIObject("RewardBoxes", _rewardPanel.transform);
            _rewardBoxContainer = container.transform;
            RectTransform containerRect = container.GetComponent<RectTransform>();
            SetStretch(containerRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(900f, 270f), new Vector2(0f, 12f));

            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 42f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            _nextStageButton = CreateRewardButton("Button_NextStage", _rewardPanel.transform, "다음 스테이지로",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(230f, 58f), new Vector2(-166f, 58f),
                new Color(0.19f, 0.52f, 0.28f, 1f));
            _nextStageButton.onClick.AddListener(() =>
            {
                _onRewardNextStage?.Invoke();
            });
            _nextStageButton.gameObject.SetActive(false);

            _rewardPanel.SetActive(false);
        }

        private void CreateRewardBox(RewardOption option)
        {
            GameObject box = CreateUIObject($"RewardBox_{option?.Id ?? "Unknown"}", _rewardBoxContainer);
            RectTransform rect = box.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240f, 260f);

            Image background = box.AddComponent<Image>();
            background.color = new Color(0.18f, 0.19f, 0.23f, 0.98f);

            Button button = box.AddComponent<Button>();
            button.targetGraphic = background;

            LayoutElement layout = box.AddComponent<LayoutElement>();
            layout.preferredWidth = 240f;
            layout.preferredHeight = 260f;
            layout.minWidth = 240f;
            layout.minHeight = 260f;

            TextMeshProUGUI question = CreateRewardText("Text_Question", box.transform, "?", 96,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(question.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image artwork = null;
            if (option != null && option.Artwork != null)
            {
                GameObject artObject = CreateUIObject("Image_Reward", box.transform);
                RectTransform artRect = artObject.GetComponent<RectTransform>();
                SetStretch(artRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(132f, 132f), new Vector2(0f, -78f));
                artwork = artObject.AddComponent<Image>();
                artwork.sprite = option.Artwork;
                artwork.preserveAspect = true;
                artwork.gameObject.SetActive(false);
            }

            TextMeshProUGUI category = CreateRewardText("Text_Category", box.transform, "", 18,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(category.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(190f, 32f), new Vector2(0f, -156f));
            category.gameObject.SetActive(false);

            TextMeshProUGUI name = CreateRewardText("Text_Name", box.transform, "", 22,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(200f, 44f), new Vector2(0f, 88f));
            name.gameObject.SetActive(false);

            TextMeshProUGUI description = CreateRewardText("Text_Description", box.transform, "", 16,
                TextAlignmentOptions.Center, FontStyles.Normal);
            SetStretch(description.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(200f, 44f), new Vector2(0f, 37f));
            bool showDetails = !_hideRewardDetailsUntilSelection;
            question.gameObject.SetActive(!showDetails);
            if (artwork != null)
                artwork.gameObject.SetActive(showDetails);
            category.text = option != null ? option.Category : "";
            category.gameObject.SetActive(showDetails);
            name.text = option != null ? option.DisplayName : "";
            name.gameObject.SetActive(showDetails);
            description.text = option != null ? option.Description : "";
            description.gameObject.SetActive(showDetails);

            TextMeshProUGUI selectedLabel = CreateRewardText("Text_Selected", box.transform, "선택 완료", 20,
                TextAlignmentOptions.Center, FontStyles.Bold);
            selectedLabel.color = new Color(1f, 0.92f, 0.35f, 1f);
            SetStretch(selectedLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(160f, 34f), new Vector2(0f, -28f));
            selectedLabel.gameObject.SetActive(false);

            button.onClick.AddListener(() =>
            {
                if (_hasSelectedReward || option == null)
                    return;

                SoundManager.PlayDefaultUiClick();
                _hasSelectedReward = true;
                bool isFinalSelection = _isFinalRewardSelection;

                question.gameObject.SetActive(false);
                if (artwork != null)
                    artwork.gameObject.SetActive(true);
                background.color = isFinalSelection
                    ? new Color(0.25f, 0.38f, 0.25f, 0.98f)
                    : new Color(0.25f, 0.29f, 0.39f, 0.98f);
                selectedLabel.gameObject.SetActive(true);

                category.text = option.Category;
                category.gameObject.SetActive(true);
                name.text = option.DisplayName;
                name.gameObject.SetActive(true);
                description.text = option.Description;
                description.gameObject.SetActive(true);

                foreach (Transform sibling in _rewardBoxContainer)
                {
                    Button siblingButton = sibling.GetComponent<Button>();
                    if (siblingButton != null)
                        siblingButton.interactable = sibling == box.transform;

                    if (sibling != box.transform)
                    {
                        Image siblingBackground = sibling.GetComponent<Image>();
                        if (siblingBackground != null)
                            siblingBackground.color = new Color(0.09f, 0.1f, 0.12f, 0.62f);
                    }
                }

                if (_rewardFeedbackText != null)
                {
                    _rewardFeedbackText.text = isFinalSelection
                        ? $"{option.DisplayName} 획득!"
                        : $"{option.Category} 보상을 발견했습니다.";
                }

                if (isFinalSelection && _nextStageButton != null)
                {
                    _nextStageButton.gameObject.SetActive(true);
                    _nextStageButton.interactable = true;
                    _nextStageButton.transform.SetAsLastSibling();
                }

                _onRewardSelected?.Invoke(option);
                Debug.Log($"[BattleUI] 보상 선택 UI 처리 완료: {option.Category} / {option.DisplayName}");
            });
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private TextMeshProUGUI CreateRewardText(
            string name,
            Transform parent,
            string text,
            int fontSize,
            TextAlignmentOptions alignment,
            FontStyles style)
        {
            GameObject go = CreateUIObject(name, parent);
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            TMP_FontAsset rewardFont = ResolveRewardFont();
            if (rewardFont != null)
                label.font = rewardFont;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        private TMP_FontAsset ResolveRewardFont()
        {
            if (_battleResultText != null && _battleResultText.font != null)
                return _battleResultText.font;

            if (_playerHpText != null && _playerHpText.font != null)
                return _playerHpText.font;

            if (_enemyHpText != null && _enemyHpText.font != null)
                return _enemyHpText.font;

            return null;
        }

        private Button CreateRewardButton(
            string name,
            Transform parent,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 size,
            Vector2 position,
            Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetStretch(rect, anchorMin, anchorMax, size, position);

            Image image = go.AddComponent<Image>();
            image.color = color;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI text = CreateRewardText($"Text_{name}", go.transform, label, 21,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return button;
        }

        private void SetStretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
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
