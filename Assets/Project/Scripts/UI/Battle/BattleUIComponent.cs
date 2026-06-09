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
using FFF.UI.Common;
using FFF.UI.Map;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FFF.UI.Battle
{
    /// <summary>
    /// 전투 화면을 그리는 역할만 담당합니다. (스스로 판단하지 않음)
    /// </summary>
    public class BattleUIComponent : BaseUIComponent
    {
        private const float CombatRevealFadeInSeconds = 0.22f;
        private const float CombatRevealPopStaggerSeconds = 0.1f;
        private const float CombatRevealPopDurationSeconds = 0.32f;
        private const float CombatRevealPreResultDelaySeconds = 0.95f;
        private const float CombatRevealPostResultDelaySeconds = 1f;
        private const float CombatRevealFadeOutSeconds = 0.26f;
        private const float EnemyGimmickToastFadeInSeconds = 0.22f;
        private const float EnemyGimmickToastVisibleSeconds = 3.2f;
        private const float EnemyGimmickToastFadeOutSeconds = 0.8f;
        private const float HealthBarLerpSeconds = 0.28f;

        private static readonly Vector2 CombatPlayerSideCenter = new Vector2(-360f, 8f);
        private static readonly Vector2 CombatEnemySideCenter = new Vector2(360f, 8f);
        private static readonly Vector2 CombatSideSize = new Vector2(386f, 400f);
        private static readonly Vector2 CombatCardSize = new Vector2(136f, 204f);
        private static readonly Vector2 CombatFirstCardPosition = new Vector2(-86f, 48f);
        private static readonly Vector2 CombatSecondCardPosition = new Vector2(86f, 48f);
        private static readonly Vector2 HudJokerSlotSize = new Vector2(120f, 160f);
        private static readonly Vector2 HudJokerVisualSize = new Vector2(120f, 160f);
        private static readonly Vector2 HudAccessorySlotSize = new Vector2(100f, 100f);
        private static readonly Vector2 HudAccessoryVisualSize = new Vector2(160f, 160f);
        private const float RewardBoxWidth = 280f;
        private const float RewardBoxHeight = 320f;

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

        [Header("=== 족보 가이드 ===")]
        [Tooltip("전투 중 확인할 화투 족보 이미지")]
        [SerializeField] private Sprite _jokboGuideSprite;
        [Tooltip("Resources 기준 족보 이미지 경로. 예: Assets/Project/Resources/UI/Jokbo/seotda_jokbo.png")]
        [SerializeField] private string _jokboGuideResourcePath = "UI/Jokbo/seotda_jokbo";

        [Header("=== 적 기믹 안내 ===")]
        [Tooltip("체크하면 적 기믹 안내를 화면 하단 중앙에 표시합니다. 기본은 상단 중앙입니다.")]
        [SerializeField] private bool _showEnemyGimmickDescriptionAtBottom;

        [Header("=== 캐릭터 체력바 위치 ===")]
        [Tooltip("플레이어 캐릭터 아래 체력바 위치. Canvas 비율 기준입니다.")]
        [SerializeField] private Vector2 _playerHealthBarAnchor = new Vector2(0.28f, 0.5f);
        [SerializeField] private Vector2 _playerHealthBarPosition = new Vector2(42f, -257f);
        [Tooltip("적 캐릭터 아래 체력바 위치. Canvas 비율 기준입니다.")]
        [SerializeField] private Vector2 _enemyHealthBarAnchor = new Vector2(0.72f, 0.5f);
        [SerializeField] private Vector2 _enemyHealthBarPosition = new Vector2(0f, -257f);

        private GameObject _topHudRoot;
        private TextMeshProUGUI _topHealthText;
        private TextMeshProUGUI _topGoldText;
        private Transform _topJokerSlotRoot;
        private Transform _topAccessorySlotRoot;
        private Button _jokboGuideButton;
        private GameObject _deckOverlay;
        private GameObject _mapOverlay;
        private GameObject _settingsOverlay;
        private GameObject _jokboOverlay;
        private Image _jokboGuideImage;
        private TextMeshProUGUI _jokboGuideFallbackText;
        private GameObject _enemyGimmickToast;
        private TextMeshProUGUI _enemyGimmickToastText;
        private CanvasGroup _enemyGimmickToastCanvasGroup;
        private Coroutine _enemyGimmickToastRoutine;
        private GameObject _playerHealthBarRoot;
        private TextMeshProUGUI _playerHealthBarText;
        private Image _playerHealthFillImage;
        private GameObject _enemyHealthBarRoot;
        private TextMeshProUGUI _enemyHealthBarText;
        private Image _enemyHealthFillImage;
        private Coroutine _playerHealthFillRoutine;
        private Coroutine _enemyHealthFillRoutine;
        private readonly List<string> _currentDeckCardIds = new List<string>();
        private int _lastPlayerHealth = -1;
        private int _lastEnemyHealth = -1;
        private Action<int, string> _onJokerIconClicked;

        protected override void OnInitialize()
        {
            EnsureTopHud();
            EnsureJokboGuideButton();
            HideLegacyCharacterHealthTexts();
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
            EnsurePlayerHealthBar();

            if (_topHealthText != null)
                _topHealthText.text = $"{current}/{max}";
            if (_playerHealthBarText != null)
                _playerHealthBarText.text = $"{current}/{max}";
            SetHealthBarFill(_playerHealthFillImage, Mathf.Clamp01(max > 0 ? (float)current / max : 0f),
                ref _playerHealthFillRoutine, _lastPlayerHealth < 0);

            if (_lastPlayerHealth >= 0 && _lastPlayerHealth != current)
            {
                bool tookDamage = current < _lastPlayerHealth;
                AnimateHealthText(_topHealthText, tookDamage);
                AnimateHealthText(_playerHealthBarText, tookDamage);
            }
            _lastPlayerHealth = current;

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

        public void SetJokerClickHandler(Action<int, string> onJokerIconClicked)
        {
            _onJokerIconClicked = onJokerIconClicked;
        }

        public void SetEnemyHealth(int current, int max)
        {
            EnsureEnemyHealthBar();

            if (_enemyHealthBarText != null)
                _enemyHealthBarText.text = $"{current}/{max}";
            SetHealthBarFill(_enemyHealthFillImage, Mathf.Clamp01(max > 0 ? (float)current / max : 0f),
                ref _enemyHealthFillRoutine, _lastEnemyHealth < 0);

            if (_lastEnemyHealth >= 0 && _lastEnemyHealth != current)
            {
                bool tookDamage = current < _lastEnemyHealth;
                AnimateHealthText(_enemyHealthBarText, tookDamage);
            }
            _lastEnemyHealth = current;
            Debug.Log($"[BattleUI] 적 체력 갱신: {current} / {max}");
        }

        public void ShowEnemyGimmickDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description) || !gameObject.activeInHierarchy)
                return;

            EnsureEnemyGimmickToast();
            if (_enemyGimmickToast == null || _enemyGimmickToastText == null || _enemyGimmickToastCanvasGroup == null)
                return;

            if (_enemyGimmickToastRoutine != null)
                StopCoroutine(_enemyGimmickToastRoutine);

            _enemyGimmickToastText.text = description.Trim();
            _enemyGimmickToast.SetActive(true);
            _enemyGimmickToast.transform.SetAsLastSibling();
            _enemyGimmickToastRoutine = StartCoroutine(PlayEnemyGimmickToast());
        }

        public void SetupItemIcons(IReadOnlyList<ItemBase> accessories, IReadOnlyList<ItemBase> jokers)
        {
            EnsureTopHud();
            // null 방어 및 빈 리스트 초기화용 할당
            var safeAccessories = accessories ?? new List<ItemBase>();
            var safeJokers = jokers ?? new List<ItemBase>();

            ClearChildren(_accessoryLayoutGroup);
            ClearChildren(_jokerLayoutGroup);

            // ItemBase 객체를 바탕으로 아이콘 생성 지시
            CreateOrderedItemIcons(safeAccessories, _tempAccessoryIconPrefab, _accessoryLayoutGroup, isJoker: false);

            if (safeAccessories.Count == 0)
                CreateEmptyHudSlot(_accessoryLayoutGroup, isJoker: false);

            int jokerCount = Mathf.Min(safeJokers.Count, PlayerDataSO.MaxHeldJokerCount);
            for (int i = 0; i < jokerCount; i++)
            {
                CreateItemIcon(safeJokers[i], _jokerIconPrefab, _jokerLayoutGroup, isJoker: true, jokerIndex: i);
            }

            CreateJokerSlotPlaceholders(PlayerDataSO.MaxHeldJokerCount - jokerCount);
            Debug.Log($"[BattleUI] 🎒 장신구 {safeAccessories.Count}개, 조커 {safeJokers.Count}개 아이콘 생성 완료");
        }

        private void CreateOrderedItemIcons(
            IReadOnlyList<ItemBase> items,
            GameObject iconPrefab,
            Transform parent,
            bool isJoker)
        {
            if (iconPrefab == null || parent == null) return;

            foreach (ItemBase item in items)
            {
                CreateItemIcon(item, iconPrefab, parent, isJoker);
            }
        }

        private void CreateItemIcon(
            ItemBase item,
            GameObject iconPrefab,
            Transform parent,
            bool isJoker,
            int jokerIndex = -1)
        {
            if (item == null || iconPrefab == null || parent == null) return;

            GameObject iconObject = Instantiate(iconPrefab, parent, false);
            iconObject.name = item.Id;
            iconObject.transform.SetAsLastSibling();

            // ItemBase 내부의 Icon 속성을 직접 참조하여 UI 렌더링
            ApplyHudItemVisual(iconObject, item.Icon, isJoker);
            ConfigureHudIcon(iconObject, isJoker);
            ConfigureItemTooltip(iconObject, item, isJoker);

            if (isJoker)
                ConfigureJokerButton(iconObject, item.Id, jokerIndex);

            StartCoroutine(PopIn(iconObject.transform, 0f, 1.08f, 0.18f));
        }

        private void ApplyHudItemVisual(GameObject iconObject, Sprite sprite, bool isJoker)
        {
            if (iconObject == null || sprite == null)
                return;

            Transform visualTransform = iconObject.transform.Find("HudItemVisual");
            GameObject visualObject;
            if (visualTransform == null)
            {
                visualObject = CreateUIObject("HudItemVisual", iconObject.transform);
            }
            else
            {
                visualObject = visualTransform.gameObject;
            }

            RectTransform visualRect = visualObject.GetComponent<RectTransform>();
            visualRect.anchorMin = new Vector2(0.5f, 0.5f);
            visualRect.anchorMax = new Vector2(0.5f, 0.5f);
            visualRect.pivot = new Vector2(0.5f, 0.5f);
            visualRect.anchoredPosition = Vector2.zero;
            visualRect.sizeDelta = isJoker ? HudJokerVisualSize : HudAccessoryVisualSize;

            Image visualImage = visualObject.GetComponent<Image>();
            if (visualImage == null)
                visualImage = visualObject.AddComponent<Image>();

            visualImage.sprite = sprite;
            visualImage.preserveAspect = !isJoker;
            visualImage.raycastTarget = false;
            visualImage.color = Color.white;
            visualObject.transform.SetAsLastSibling();
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

        private Sprite ResolveItemArtwork(
            string itemId,
            IReadOnlyList<ItemIconDefinition> iconDefinitions,
            GameObject fallbackPrefab)
        {
            ItemIconDefinition definition = FindItemIconDefinition(itemId, iconDefinitions);
            if (definition != null && definition.Icon != null)
                return definition.Icon;

            Sprite mappedSprite = ResolveBuiltInItemSprite(itemId);
            if (mappedSprite != null)
                return mappedSprite;

            return ResolvePrefabImageSprite(fallbackPrefab);
        }

        private Sprite ResolveAccessoryArtwork(string itemId)
        {
            return ResolveItemArtwork(itemId, _accessoryIconDefinitions, _tempAccessoryIconPrefab);
        }

        private Sprite ResolveJokerArtwork(string itemId)
        {
            return ResolveItemArtwork(itemId, _jokerIconDefinitions, _jokerIconPrefab);
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
            rootRect.sizeDelta = new Vector2(0f, 174f);
            rootRect.anchoredPosition = Vector2.zero;
            _topHudRoot.transform.SetAsLastSibling();

            Image background = _topHudRoot.AddComponent<Image>();
            background.color = new Color(0.08f, 0.12f, 0.15f, 0.92f);
            background.raycastTarget = false;

            BuildHealthBlock(_topHudRoot.transform);
            BuildGoldBlock(_topHudRoot.transform);
            BuildJokerBlock(_topHudRoot.transform);
            BuildHudButtons(_topHudRoot.transform);
            BuildAccessoryRow(parent);
        }

        private void EnsureEnemyGimmickToast()
        {
            if (_enemyGimmickToast != null)
            {
                PositionEnemyGimmickToast();
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            _enemyGimmickToast = CreateUIObject("EnemyGimmickDescriptionToast", parent);
            RectTransform rootRect = _enemyGimmickToast.GetComponent<RectTransform>();
            SetStretch(rootRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(860f, 112f), Vector2.zero);
            PositionEnemyGimmickToast();

            _enemyGimmickToastCanvasGroup = _enemyGimmickToast.AddComponent<CanvasGroup>();
            _enemyGimmickToastCanvasGroup.alpha = 0f;
            _enemyGimmickToastCanvasGroup.blocksRaycasts = false;
            _enemyGimmickToastCanvasGroup.interactable = false;

            Image background = _enemyGimmickToast.AddComponent<Image>();
            background.color = new Color(0.07f, 0.08f, 0.1f, 0.86f);
            background.raycastTarget = false;

            Outline outline = _enemyGimmickToast.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.78f, 0.32f, 0.45f);
            outline.effectDistance = new Vector2(2f, -2f);

            TextMeshProUGUI title = CreateRewardText("Text_EnemyGimmickTitle", _enemyGimmickToast.transform,
                "적 기믹", 20, TextAlignmentOptions.Left, FontStyles.Bold);
            title.color = new Color(1f, 0.82f, 0.34f, 1f);
            SetStretch(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(-52f, -68f), new Vector2(0f, 28f));

            _enemyGimmickToastText = CreateRewardText("Text_EnemyGimmickDescription", _enemyGimmickToast.transform,
                "", 25, TextAlignmentOptions.Left, FontStyles.Normal);
            _enemyGimmickToastText.color = new Color(0.96f, 0.97f, 0.92f, 1f);
            _enemyGimmickToastText.overflowMode = TextOverflowModes.Ellipsis;
            SetStretch(_enemyGimmickToastText.rectTransform, Vector2.zero, Vector2.one, new Vector2(-52f, -46f), new Vector2(0f, -18f));

            _enemyGimmickToast.SetActive(false);
        }

        private void PositionEnemyGimmickToast()
        {
            if (_enemyGimmickToast == null)
                return;

            RectTransform rect = _enemyGimmickToast.GetComponent<RectTransform>();
            if (rect == null)
                return;

            if (_showEnemyGimmickDescriptionAtBottom)
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 150f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -228f);
            }
        }

        private IEnumerator PlayEnemyGimmickToast()
        {
            _enemyGimmickToastCanvasGroup.alpha = 0f;
            _enemyGimmickToast.transform.localScale = Vector3.one * 0.96f;

            yield return UITweenHelper.FadeTo(_enemyGimmickToastCanvasGroup, 1f,
                EnemyGimmickToastFadeInSeconds, UITweenHelper.EaseType.OutQuad);
            yield return ScaleTransform(_enemyGimmickToast.transform, Vector3.one, 0.12f, UITweenHelper.EaseType.OutCubic);
            yield return new WaitForSeconds(EnemyGimmickToastVisibleSeconds);
            yield return UITweenHelper.FadeTo(_enemyGimmickToastCanvasGroup, 0f,
                EnemyGimmickToastFadeOutSeconds, UITweenHelper.EaseType.Linear);

            if (_enemyGimmickToast != null)
                _enemyGimmickToast.SetActive(false);
            _enemyGimmickToastRoutine = null;
        }

        private void HideLegacyCharacterHealthTexts()
        {
            if (_playerHpText != null)
                _playerHpText.gameObject.SetActive(false);

            if (_enemyHpText != null)
                _enemyHpText.gameObject.SetActive(false);
        }

        private void BuildHealthBlock(Transform parent)
        {
            TextMeshProUGUI heart = CreateRewardText("Text_HeartIcon", parent, "♥", 38,
                TextAlignmentOptions.Center, FontStyles.Bold);
            heart.color = new Color(1f, 0.25f, 0.28f, 1f);
            SetStretch(heart.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(52f, 52f), new Vector2(38f, -1f));

            _topHealthText = CreateRewardText("Text_TopHealth", parent, "0/0", 30,
                TextAlignmentOptions.Left, FontStyles.Bold);
            _topHealthText.color = new Color(1f, 0.35f, 0.35f, 1f);
            SetStretch(_topHealthText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(220f, 52f), new Vector2(182f, -1f));
        }

        private void BuildGoldBlock(Transform parent)
        {
            TextMeshProUGUI coin = CreateRewardText("Text_CoinIcon", parent, "엽전", 30,
                TextAlignmentOptions.Center, FontStyles.Bold);
            coin.color = new Color(1f, 0.82f, 0.2f, 1f);
            SetStretch(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(84f, 52f), new Vector2(340f, -1f));

            _topGoldText = CreateRewardText("Text_TopGold", parent, "0", 30,
                TextAlignmentOptions.Left, FontStyles.Bold);
            _topGoldText.color = new Color(1f, 0.84f, 0.28f, 1f);
            SetStretch(_topGoldText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(132f, 52f), new Vector2(454f, -1f));
        }

        private void EnsurePlayerHealthBar()
        {
            if (_playerHealthBarRoot != null)
            {
                PositionCharacterHealthBar(_playerHealthBarRoot, _playerHealthBarAnchor, _playerHealthBarPosition);
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            _playerHealthBarRoot = CreateUIObject("PlayerCharacterHealthBar", parent);
            PositionCharacterHealthBar(_playerHealthBarRoot, _playerHealthBarAnchor, _playerHealthBarPosition);

            _playerHealthFillImage = CreateHealthBar("PlayerHealthGauge", _playerHealthBarRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(260f, 22f),
                Vector2.zero, new Color(0.83f, 0.04f, 0.04f, 1f));

            _playerHealthBarText = CreateRewardText("Text_PlayerHealth", _playerHealthBarRoot.transform,
                "0/0", 21, TextAlignmentOptions.Center, FontStyles.Bold);
            _playerHealthBarText.color = Color.white;
            _playerHealthBarText.outlineWidth = 0.18f;
            _playerHealthBarText.outlineColor = new Color(0.16f, 0.02f, 0.02f, 1f);
            SetStretch(_playerHealthBarText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(260f, 26f), Vector2.zero);

            _playerHealthBarRoot.transform.SetAsLastSibling();
        }

        private void EnsureEnemyHealthBar()
        {
            if (_enemyHealthBarRoot != null)
            {
                PositionCharacterHealthBar(_enemyHealthBarRoot, _enemyHealthBarAnchor, _enemyHealthBarPosition);
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            _enemyHealthBarRoot = CreateUIObject("EnemyHealthBar", parent);
            PositionCharacterHealthBar(_enemyHealthBarRoot, _enemyHealthBarAnchor, _enemyHealthBarPosition);

            _enemyHealthFillImage = CreateHealthBar("EnemyHealthGauge", _enemyHealthBarRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(260f, 22f),
                Vector2.zero, new Color(0.83f, 0.04f, 0.04f, 1f));

            _enemyHealthBarText = CreateRewardText("Text_EnemyHealth", _enemyHealthBarRoot.transform,
                "0/0", 21, TextAlignmentOptions.Center, FontStyles.Bold);
            _enemyHealthBarText.color = Color.white;
            _enemyHealthBarText.outlineWidth = 0.18f;
            _enemyHealthBarText.outlineColor = new Color(0.16f, 0.02f, 0.02f, 1f);
            SetStretch(_enemyHealthBarText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(260f, 26f), Vector2.zero);

            _enemyHealthBarRoot.transform.SetAsLastSibling();
        }

        private void PositionCharacterHealthBar(GameObject healthBarRoot, Vector2 anchor, Vector2 position)
        {
            if (healthBarRoot == null)
                return;

            RectTransform rootRect = healthBarRoot.GetComponent<RectTransform>();
            if (rootRect == null)
                return;

            SetStretch(rootRect, anchor, anchor, new Vector2(292f, 58f), position);
        }

        private Image CreateHealthBar(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 size,
            Vector2 position,
            Color fillColor)
        {
            GameObject root = CreateUIObject(name, parent);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetStretch(rootRect, anchorMin, anchorMax, size, position);

            Image background = root.AddComponent<Image>();
            background.color = new Color(0.12f, 0.03f, 0.03f, 0.96f);
            background.raycastTarget = false;

            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.72f);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject fill = CreateUIObject("Fill", root.transform);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;

            return fillImage;
        }

        private void BuildJokerBlock(Transform parent)
        {
            GameObject block = CreateUIObject("JokerHudBlock", parent);
            RectTransform rect = block.GetComponent<RectTransform>();
            SetStretch(rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(424f, 170f), new Vector2(756f, 0f));

            Image bg = block.AddComponent<Image>();
            bg.color = Color.clear;
            bg.raycastTarget = false;
            block.AddComponent<RectMask2D>();

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
                layoutRect.offsetMin = new Vector2(16f, 5f);
                layoutRect.offsetMax = new Vector2(-16f, -5f);
                layoutRect.anchoredPosition = Vector2.zero;
            }

            HorizontalLayoutGroup layout = _jokerLayoutGroup.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
                layout = _jokerLayoutGroup.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10f;
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
            rect.sizeDelta = new Vector2(0f, 118f);
            rect.anchoredPosition = new Vector2(0f, -150f);

            Image bg = block.AddComponent<Image>();
            bg.color = Color.clear;
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
                layoutRect.offsetMin = new Vector2(22f, 6f);
                layoutRect.offsetMax = new Vector2(-22f, -6f);
            }

            HorizontalLayoutGroup layout = _accessoryLayoutGroup.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
                layout = _accessoryLayoutGroup.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 12f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            _topAccessorySlotRoot = _accessoryLayoutGroup;
        }

        private void BuildHudButtons(Transform parent)
        {
            CreateHudButton("Button_TopMap", parent, "맵", new Vector2(-244f, 0f), ShowMapOverlay);
            CreateHudButton("Button_TopDeck", parent, "덱", new Vector2(-158f, 0f), ShowDeckOverlay);
            CreateHudButton("Button_TopSettings", parent, "설정", new Vector2(-68f, 0f), ShowSettingsOverlay);
        }

        private Button CreateHudButton(string name, Transform parent, string label, Vector2 position, Action onClick)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetStretch(rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(76f, 58f), position);

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.16f, 0.2f, 0.23f, 0.95f);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                SoundManager.PlayDefaultUiClick();
                onClick?.Invoke();
            });

            TextMeshProUGUI text = CreateRewardText($"Text_{name}", go.transform, label, 21,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private void EnsureJokboGuideButton()
        {
            if (_jokboGuideButton != null)
                return;

            Transform parent = _deckAreaRect != null ? _deckAreaRect : _topHudRoot != null ? _topHudRoot.transform : transform;
            GameObject go = CreateUIObject("Button_JokboGuide", parent);
            RectTransform rect = go.GetComponent<RectTransform>();

            if (_deckAreaRect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.sizeDelta = new Vector2(120f, 52f);
                rect.anchoredPosition = new Vector2(0f, 14f);
            }
            else
            {
                SetStretch(rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(92f, 52f), new Vector2(-306f, 0f));
            }

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.26f, 0.3f, 0.96f);

            _jokboGuideButton = go.AddComponent<Button>();
            _jokboGuideButton.targetGraphic = image;
            _jokboGuideButton.onClick.AddListener(() =>
            {
                SoundManager.PlayDefaultUiClick();
                ToggleJokboOverlay();
            });

            TextMeshProUGUI text = CreateRewardText("Text_Button_JokboGuide", go.transform, "족보", 22,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            go.transform.SetAsLastSibling();
        }

        private void ConfigureHudIcon(GameObject icon, bool isJoker)
        {
            if (icon == null)
                return;

            Vector2 size = isJoker ? HudJokerSlotSize : HudAccessorySlotSize;

            RectTransform rect = icon.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = size;

            LayoutElement layout = icon.GetComponent<LayoutElement>();
            if (layout == null)
                layout = icon.AddComponent<LayoutElement>();

            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            layout.minWidth = size.x;
            layout.minHeight = size.y;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }

        private void ConfigureJokerButton(GameObject icon, string itemId, int jokerIndex)
        {
            if (icon == null || jokerIndex < 0)
                return;

            Image hitArea = icon.GetComponent<Image>();
            if (hitArea == null)
                hitArea = icon.AddComponent<Image>();

            hitArea.enabled = true;
            hitArea.color = new Color(1f, 1f, 1f, 0f);
            hitArea.raycastTarget = true;

            Button button = icon.GetComponent<Button>();
            if (button == null)
                button = icon.AddComponent<Button>();

            button.targetGraphic = hitArea;
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveAllListeners();

            int capturedIndex = jokerIndex;
            string capturedId = itemId;
            button.onClick.AddListener(() =>
            {
                SoundManager.PlayDefaultUiClick();
                _onJokerIconClicked?.Invoke(capturedIndex, capturedId);
            });
        }

        private void ConfigureItemTooltip(GameObject icon, ItemBase item, bool isJoker)
        {
            if (icon == null || item == null)
                return;

            Image hitArea = icon.GetComponent<Image>();
            if (hitArea == null)
            {
                hitArea = icon.AddComponent<Image>();
                hitArea.color = new Color(1f, 1f, 1f, 0f);
            }

            hitArea.enabled = true;
            hitArea.raycastTarget = true;

            ItemTooltipTrigger tooltip = icon.GetComponent<ItemTooltipTrigger>();
            if (tooltip == null)
                tooltip = icon.AddComponent<ItemTooltipTrigger>();

            tooltip.SetContent(item.DisplayName, item.Description, isJoker ? "조커 카드" : "장신구");
        }

        private void CreateJokerSlotPlaceholders(int count)
        {
            for (int i = 0; i < count; i++)
                CreateEmptyHudSlot(_jokerLayoutGroup, isJoker: true);
        }

        private void CreateEmptyHudSlot(Transform parent, bool isJoker)
        {
            if (parent == null)
                return;

            GameObject slot = CreateUIObject(isJoker ? "EmptyJokerSlot" : "EmptyAccessorySlot", parent);
            Image image = slot.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;
            ConfigureHudIcon(slot, isJoker);
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
            {
                _enemyIntentText.text = $"적 의도: {intent.Card1.DisplayName} + {intent.Card2.DisplayName}\n(예상 공격력: {intent.BasePower})";
                StartCoroutine(PulseText(_enemyIntentText, new Color(1f, 0.78f, 0.28f, 1f), 1.08f));
            }
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
            {
                _expectedStrengthText.text = $"예상 공격력: {text}";
                if (!string.IsNullOrEmpty(text) && text != "-")
                    StartCoroutine(PulseText(_expectedStrengthText, new Color(1f, 0.88f, 0.28f, 1f), 1.06f));
            }
        }

        /// <summary>
        /// 턴 종료 버튼의 활성화/비활성화 상태를 토글합니다.
        /// </summary>
        public void SetEndTurnButtonInteractable(bool isInteractable)
        {
            if (_endTurnButton != null)
            {
                bool changed = _endTurnButton.interactable != isInteractable;
                _endTurnButton.interactable = isInteractable;
                if (changed && isInteractable)
                    StartCoroutine(PulseTransform(_endTurnButton.transform, 1.07f, 0.16f));
            }
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
        public IEnumerator PlayCombatReveal(
            IReadOnlyList<HwaTuCard> playerCards,
            string playerHandName,
            int playerPower,
            EnemyIntent enemyIntent,
            int outcome)
        {
            TryAnimateSelectedHandCards(playerCards);
            SoundManager.PlaySfxSound(SoundIds.SfxHandReveal);

            GameObject overlay = CreateCombatRevealOverlay(
                playerCards,
                playerHandName,
                playerPower,
                enemyIntent,
                outcome,
                out List<Transform> popTargets,
                out RectTransform resultRect);

            if (overlay == null)
                yield break;

            CanvasGroup canvasGroup = overlay.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            yield return UITweenHelper.FadeTo(canvasGroup, 1f, CombatRevealFadeInSeconds, UITweenHelper.EaseType.Linear);

            for (int i = 0; i < popTargets.Count; i++)
                StartCoroutine(PopIn(popTargets[i], i * CombatRevealPopStaggerSeconds, 1.08f, CombatRevealPopDurationSeconds));

            yield return new WaitForSeconds(CombatRevealPreResultDelaySeconds);

            if (resultRect != null)
            {
                if (outcome == 0)
                {
                    SoundManager.PlaySfxSound(SoundIds.SfxMiss);
                    StartCoroutine(UITweenHelper.ShakeRect(resultRect, 0.34f, 10f));
                }
                else
                {
                    SoundManager.PlaySfxSound(outcome > 0 ? SoundIds.SfxRoundWin : SoundIds.SfxRoundLose);
                    StartCoroutine(PulseTransform(resultRect, 1.12f, 0.3f));
                }
            }

            yield return new WaitForSeconds(CombatRevealPostResultDelaySeconds);
            yield return UITweenHelper.FadeTo(canvasGroup, 0f, CombatRevealFadeOutSeconds, UITweenHelper.EaseType.Linear);
            Destroy(overlay);
        }

        private GameObject CreateCombatRevealOverlay(
            IReadOnlyList<HwaTuCard> playerCards,
            string playerHandName,
            int playerPower,
            EnemyIntent enemyIntent,
            int outcome,
            out List<Transform> popTargets,
            out RectTransform resultRect)
        {
            popTargets = new List<Transform>();
            resultRect = null;

            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            GameObject overlay = CreateUIObject("CombatRevealOverlay", parent);
            overlay.transform.SetAsLastSibling();

            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            dim.raycastTarget = false;

            overlay.AddComponent<CanvasGroup>();

            TextMeshProUGUI title = CreateRewardText("Text_CombatTitle", overlay.transform, "승부", 52,
                TextAlignmentOptions.Center, FontStyles.Bold);
            title.color = new Color(1f, 0.9f, 0.48f, 1f);
            SetStretch(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(580f, 72f), new Vector2(0f, 260f));

            CreateCombatSide(overlay.transform, "나", playerHandName, playerPower,
                GetCardAt(playerCards, 0), GetCardAt(playerCards, 1), CombatPlayerSideCenter,
                new Color(0.22f, 0.58f, 0.92f, 1f), popTargets);

            CreateCombatSide(overlay.transform, "상대", enemyIntent.HandName, enemyIntent.BasePower,
                enemyIntent.Card1, enemyIntent.Card2, CombatEnemySideCenter, new Color(0.82f, 0.24f, 0.24f, 1f), popTargets);

            TextMeshProUGUI versus = CreateRewardText("Text_CombatVersus", overlay.transform, "VS", 58,
                TextAlignmentOptions.Center, FontStyles.Bold);
            versus.color = new Color(1f, 0.94f, 0.62f, 1f);
            SetStretch(versus.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(150f, 78f), new Vector2(0f, 86f));
            popTargets.Add(versus.transform);

            string resultText = outcome > 0 ? "승리!" : outcome < 0 ? "패배..." : "무승부";
            Color resultColor = outcome > 0
                ? new Color(1f, 0.86f, 0.25f, 1f)
                : outcome < 0
                    ? new Color(1f, 0.34f, 0.28f, 1f)
                    : new Color(0.82f, 0.86f, 0.9f, 1f);

            TextMeshProUGUI result = CreateRewardText("Text_CombatResult", overlay.transform, resultText, 66,
                TextAlignmentOptions.Center, FontStyles.Bold);
            result.color = resultColor;
            SetStretch(result.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(280f, 90f), new Vector2(0f, -64f));
            resultRect = result.rectTransform;
            popTargets.Add(result.transform);

            return overlay;
        }

        private void CreateCombatSide(
            Transform parent,
            string sideName,
            string handName,
            int power,
            HwaTuCard card1,
            HwaTuCard card2,
            Vector2 center,
            Color accent,
            List<Transform> popTargets)
        {
            GameObject group = CreateUIObject($"CombatSide_{sideName}", parent);
            RectTransform groupRect = group.GetComponent<RectTransform>();
            SetStretch(groupRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                CombatSideSize, center);

            TextMeshProUGUI sideLabel = CreateRewardText($"Text_Combat_{sideName}_Side", group.transform,
                sideName, 31, TextAlignmentOptions.Center, FontStyles.Bold);
            sideLabel.color = accent;
            SetStretch(sideLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(190f, 44f), new Vector2(0f, -24f));

            Transform first = CreateCombatCard(group.transform, card1, CombatFirstCardPosition, accent);
            Transform second = CreateCombatCard(group.transform, card2, CombatSecondCardPosition, accent);

            TextMeshProUGUI handLabel = CreateRewardText($"Text_Combat_{sideName}_Hand", group.transform,
                string.IsNullOrWhiteSpace(handName) ? "-" : handName, 27, TextAlignmentOptions.Center, FontStyles.Bold);
            handLabel.color = Color.white;
            SetStretch(handLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(330f, 44f), new Vector2(0f, 67f));

            TextMeshProUGUI powerLabel = CreateRewardText($"Text_Combat_{sideName}_Power", group.transform,
                $"공격력 {power}", 23, TextAlignmentOptions.Center, FontStyles.Bold);
            powerLabel.color = new Color(0.86f, 0.9f, 0.95f, 1f);
            SetStretch(powerLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(270f, 36f), new Vector2(0f, 27f));

            popTargets.Add(first);
            popTargets.Add(second);
            popTargets.Add(sideLabel.transform);
            popTargets.Add(handLabel.transform);
            popTargets.Add(powerLabel.transform);
        }

        private Transform CreateCombatCard(Transform parent, HwaTuCard card, Vector2 position, Color accent)
        {
            GameObject cardObject = _cardPrefab != null && card != null
                ? Instantiate(_cardPrefab, parent, false)
                : CreateUIObject($"CombatCard_{card?.CardId ?? "None"}", parent);
            cardObject.name = $"CombatCard_{card?.CardId ?? "None"}";

            RectTransform rect = cardObject.GetComponent<RectTransform>();
            SetStretch(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                CombatCardSize, position);

            CardUIComponent cardUI = cardObject.GetComponent<CardUIComponent>();
            if (cardUI != null && card != null)
            {
                cardUI.Setup(card, _ => { }, card != null ? FindCardArtwork(card.CardId) : null);
                cardUI.SetSelected(false);

                Button cardButton = cardObject.GetComponent<Button>();
                if (cardButton != null)
                {
                    cardButton.onClick.RemoveAllListeners();
                    cardButton.enabled = false;
                }

                return cardObject.transform;
            }

            Image bg = cardObject.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.09f, 0.11f, 0.96f);
            bg.raycastTarget = false;

            GameObject accentObject = CreateUIObject("Accent", cardObject.transform);
            RectTransform accentRect = accentObject.GetComponent<RectTransform>();
            SetStretch(accentRect, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 5f), new Vector2(0f, -2f));
            Image accentImage = accentObject.AddComponent<Image>();
            accentImage.color = accent;
            accentImage.raycastTarget = false;

            Sprite artwork = card != null ? HwaTuCardDatabase.GetArtwork(card.CardId) : null;
            if (artwork != null)
            {
                GameObject artObject = CreateUIObject("Artwork", cardObject.transform);
                RectTransform artRect = artObject.GetComponent<RectTransform>();
                SetStretch(artRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(100f, 126f), new Vector2(0f, -70f));
                Image art = artObject.AddComponent<Image>();
                art.sprite = artwork;
                art.preserveAspect = true;
                art.raycastTarget = false;
            }

            TextMeshProUGUI name = CreateRewardText("Text_CardName", cardObject.transform,
                card != null ? card.DisplayName : "-", 17, TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(112f, 42f), new Vector2(0f, 24f));

            return cardObject.transform;
        }

        private void TryAnimateSelectedHandCards(IReadOnlyList<HwaTuCard> selectedCards)
        {
            if (_handLayoutGroup == null || selectedCards == null)
                return;

            foreach (Transform child in _handLayoutGroup)
            {
                CardUIComponent cardUI = child.GetComponent<CardUIComponent>();
                if (cardUI == null || !ContainsCard(selectedCards, cardUI.CardData))
                    continue;

                CardAnimator animator = child.GetComponent<CardAnimator>();
                if (animator != null)
                    animator.PlaySubmitToField();
            }
        }

        private static bool ContainsCard(IReadOnlyList<HwaTuCard> cards, HwaTuCard target)
        {
            if (cards == null || target == null)
                return false;

            for (int i = 0; i < cards.Count; i++)
            {
                if (ReferenceEquals(cards[i], target) || cards[i] == target)
                    return true;
            }

            return false;
        }

        private static HwaTuCard GetCardAt(IReadOnlyList<HwaTuCard> cards, int index)
        {
            return cards != null && index >= 0 && index < cards.Count ? cards[index] : null;
        }

        public void ShowBattleResult(string resultMessage)
        {
            HideRewardSelection();
            if (_battleResultPanel != null) _battleResultPanel.SetActive(true);
            if (_battleResultText != null) _battleResultText.text = resultMessage;
            if (_battleResultPanel != null)
                StartCoroutine(PopIn(_battleResultPanel.transform, 0f, 1.04f, 0.24f));
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
                StartCoroutine(PopIn(_rewardPanel.transform, 0f, 1.01f, 0.2f));
            }
            SoundManager.PlaySfxSound(SoundIds.SfxRewardOpen);

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

        private void ToggleJokboOverlay()
        {
            if (_jokboOverlay != null && _jokboOverlay.activeSelf)
            {
                _jokboOverlay.SetActive(false);
                return;
            }

            ShowJokboOverlay();
        }

        private void ShowJokboOverlay()
        {
            if (_jokboOverlay == null)
                BuildJokboOverlay();

            RefreshJokboOverlay();
            _jokboOverlay.SetActive(true);
            _jokboOverlay.transform.SetAsLastSibling();
        }

        private void BuildJokboOverlay()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            _jokboOverlay = CreateUIObject("JokboGuideOverlay", parent);
            RectTransform overlayRect = _jokboOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            GameObject panel = CreatePanel(_jokboOverlay.transform, "화투 족보", new Vector2(1600f, 900f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchoredPosition = new Vector2(0f, 18f);

            GameObject imageFrame = CreateUIObject("JokboImageFrame", panel.transform);
            RectTransform frameRect = imageFrame.GetComponent<RectTransform>();
            SetStretch(frameRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1550f, 817f), new Vector2(0f, -28f));

            Image frameImage = imageFrame.AddComponent<Image>();
            frameImage.color = new Color(0.03f, 0.04f, 0.05f, 0.78f);
            frameImage.raycastTarget = true;

            GameObject guideImageObject = CreateUIObject("Image_JokboGuide", imageFrame.transform);
            RectTransform guideRect = guideImageObject.GetComponent<RectTransform>();
            guideRect.anchorMin = new Vector2(0.5f, 0.5f);
            guideRect.anchorMax = new Vector2(0.5f, 0.5f);
            guideRect.pivot = new Vector2(0.5f, 0.5f);
            guideRect.sizeDelta = new Vector2(1440f, 1080f);
            guideRect.anchoredPosition = Vector2.zero;

            _jokboGuideImage = guideImageObject.AddComponent<Image>();
            _jokboGuideImage.preserveAspect = true;
            _jokboGuideImage.raycastTarget = false;

            _jokboGuideFallbackText = CreateRewardText("Text_JokboGuideFallback", imageFrame.transform,
                "족보 이미지가 아직 연결되지 않았습니다.\n인스펙터의 Jokbo Guide Sprite에 이미지를 넣거나\nResources/UI/Jokbo/seotda_jokbo 경로로 저장해주세요.",
                24, TextAlignmentOptions.Center, FontStyles.Bold);
            _jokboGuideFallbackText.color = new Color(0.92f, 0.94f, 0.96f, 1f);
            SetStretch(_jokboGuideFallbackText.rectTransform, Vector2.zero, Vector2.one, new Vector2(-64f, -64f), Vector2.zero);

            Button close = CreateOverlayButton("Button_CloseJokboGuide", panel.transform, "닫기", new Vector2(695f, 404f), () => _jokboOverlay.SetActive(false));
            close.gameObject.transform.SetAsLastSibling();

            _jokboOverlay.SetActive(false);
        }

        private void RefreshJokboOverlay()
        {
            Sprite sprite = ResolveJokboGuideSprite();

            if (_jokboGuideImage != null)
            {
                _jokboGuideImage.sprite = sprite;
                _jokboGuideImage.enabled = sprite != null;
            }

            if (_jokboGuideFallbackText != null)
                _jokboGuideFallbackText.gameObject.SetActive(sprite == null);
        }

        private Sprite ResolveJokboGuideSprite()
        {
            if (_jokboGuideSprite != null)
                return _jokboGuideSprite;

            if (string.IsNullOrWhiteSpace(_jokboGuideResourcePath))
                return null;

            Sprite sprite = Resources.Load<Sprite>(_jokboGuideResourcePath);
            if (sprite != null)
            {
                _jokboGuideSprite = sprite;
                return _jokboGuideSprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(_jokboGuideResourcePath);
            if (texture == null)
                return null;

            _jokboGuideSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
            return _jokboGuideSprite;
        }

        private void ShowDeckOverlay()
        {
            if (_deckOverlay == null)
                BuildDeckOverlay();

            _deckOverlay.SetActive(true);
            _deckOverlay.transform.SetAsLastSibling();
            RefreshDeckOverlay();
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
            viewport.AddComponent<RectMask2D>();

            GameObject content = CreateUIObject("DeckContent", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 390f);

            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(98f, 208f);
            grid.spacing = new Vector2(14f, 24f);
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
            EnsureDeckCardsForOverlay();

            Transform content = _deckOverlay != null ? _deckOverlay.transform.Find("Panel/DeckScrollView/Viewport/DeckContent") : null;
            ClearChildren(content);
            if (content == null)
                return;

            if (_currentDeckCardIds.Count == 0)
            {
                TextMeshProUGUI text = CreateRewardText("Text_EmptyDeck", content, "덱에 카드가 없습니다.", 22,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                SetStretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                return;
            }

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
                        cardView.Setup(card, _ => { }, FindCardArtwork(cardId));
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

            if (content is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        private void EnsureDeckCardsForOverlay()
        {
            if (_currentDeckCardIds.Count > 0)
                return;

            bool hasAuthoritativeDeck = false;
            PlayerDataSO masterData = GameManager.Instance != null ? GameManager.Instance.MasterPlayerData : null;
            if (masterData != null && masterData.DeckCardIds != null)
            {
                hasAuthoritativeDeck = true;
                _currentDeckCardIds.AddRange(masterData.DeckCardIds);
            }

            if (_currentDeckCardIds.Count > 0 || hasAuthoritativeDeck)
                return;

            foreach (HwaTuCard card in HwaTuCardDatabase.CreateDefaultInitialDeck())
            {
                if (card != null && !string.IsNullOrEmpty(card.CardId))
                    _currentDeckCardIds.Add(card.CardId);
            }
        }

        private void ShowMapOverlay()
        {
            if (_mapOverlay == null)
                BuildMapOverlay();

            _mapOverlay.SetActive(true);
            _mapOverlay.transform.SetAsLastSibling();
            RefreshMapOverlay();
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

            viewport.AddComponent<RectMask2D>();

            GameObject mapContent = CreateUIObject("MapContent", viewport.transform);
            RectTransform contentRect = mapContent.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0f);
            contentRect.anchorMax = new Vector2(0.5f, 0f);
            contentRect.pivot = new Vector2(0.5f, 0f);
            contentRect.sizeDelta = new Vector2(760f, 470f);
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

            MapData mapData = ResolveMapDataForOverlay();
            if (mapData == null)
            {
                TextMeshProUGUI text = CreateRewardText("Text_NoMap", mapContent, "확인할 스테이지 맵이 없습니다.", 24,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                SetStretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                return;
            }

            try
            {
                MapUIComponent mapView = mapContent.GetComponent<MapUIComponent>();
                if (mapView == null)
                    mapView = mapContent.gameObject.AddComponent<MapUIComponent>();

                mapView.BuildReadOnlyMap(mapContent.GetComponent<RectTransform>(), mapData);

                ScrollRect scroll = _mapOverlay.GetComponentInChildren<ScrollRect>(true);
                if (scroll != null)
                    scroll.verticalNormalizedPosition = 0f;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BattleUI] 맵 오버레이 생성 실패: {ex.Message}\n{ex.StackTrace}");
                ClearChildren(mapContent);
                TextMeshProUGUI text = CreateRewardText("Text_MapError", mapContent, "맵을 표시하는 중 오류가 발생했습니다.", 23,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                SetStretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
        }

        private MapData ResolveMapDataForOverlay()
        {
            GameManager gameManager = GameManager.Instance;
            return gameManager != null ? gameManager.GetCurrentOrRestoreRunMap() : null;
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
                "보상 하나를 선택하세요.", 24, TextAlignmentOptions.Center, FontStyles.Normal);
            SetStretch(_rewardFeedbackText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(760f, 58f), new Vector2(0f, -152f));

            GameObject container = CreateUIObject("RewardBoxes", _rewardPanel.transform);
            _rewardBoxContainer = container.transform;
            RectTransform containerRect = container.GetComponent<RectTransform>();
            SetStretch(containerRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(1040f, 340f), new Vector2(0f, -2f));

            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 46f;
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
            rect.sizeDelta = new Vector2(RewardBoxWidth, RewardBoxHeight);
            int displayIndex = _rewardBoxContainer != null ? _rewardBoxContainer.childCount - 1 : 0;

            Image background = box.AddComponent<Image>();
            background.color = new Color(0.18f, 0.19f, 0.23f, 0.98f);

            Button button = box.AddComponent<Button>();
            button.targetGraphic = background;

            LayoutElement layout = box.AddComponent<LayoutElement>();
            layout.preferredWidth = RewardBoxWidth;
            layout.preferredHeight = RewardBoxHeight;
            layout.minWidth = RewardBoxWidth;
            layout.minHeight = RewardBoxHeight;

            TextMeshProUGUI question = CreateRewardText("Text_Question", box.transform, "?", 96,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(question.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject artFrame = CreateUIObject("RewardArtworkFrame", box.transform);
            RectTransform artFrameRect = artFrame.GetComponent<RectTransform>();
            SetStretch(artFrameRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(176f, 136f), new Vector2(0f, -148f));
            Image artFrameImage = artFrame.AddComponent<Image>();
            artFrameImage.color = GetRewardArtworkFrameColor(option);

            Sprite resolvedArtwork = ResolveRewardArtwork(option);
            Image artwork = null;
            if (resolvedArtwork != null)
            {
                GameObject artObject = CreateUIObject("Image_Reward", artFrame.transform);
                RectTransform artRect = artObject.GetComponent<RectTransform>();
                SetStretch(artRect, Vector2.zero, Vector2.one, new Vector2(-16f, -16f), Vector2.zero);
                artwork = artObject.AddComponent<Image>();
                artwork.sprite = resolvedArtwork;
                artwork.preserveAspect = true;
            }

            TextMeshProUGUI fallbackIcon = CreateRewardText("Text_RewardIcon", artFrame.transform,
                GetRewardFallbackIcon(option), 58, TextAlignmentOptions.Center, FontStyles.Bold);
            fallbackIcon.color = new Color(1f, 0.93f, 0.63f, 1f);
            fallbackIcon.textWrappingMode = TextWrappingModes.NoWrap;
            SetStretch(fallbackIcon.rectTransform, Vector2.zero, Vector2.one, new Vector2(-18f, -18f), Vector2.zero);
            fallbackIcon.gameObject.SetActive(artwork == null);

            TextMeshProUGUI category = CreateRewardText("Text_Category", box.transform, "", 18,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(category.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(220f, 28f), new Vector2(0f, -54f));
            category.gameObject.SetActive(false);

            TextMeshProUGUI name = CreateRewardText("Text_Name", box.transform, "", 22,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(230f, 36f), new Vector2(0f, 82f));
            name.gameObject.SetActive(false);

            TextMeshProUGUI description = CreateRewardText("Text_Description", box.transform, "", 16,
                TextAlignmentOptions.Center, FontStyles.Normal);
            SetStretch(description.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(230f, 48f), new Vector2(0f, 28f));
            bool showDetails = !_hideRewardDetailsUntilSelection;
            question.gameObject.SetActive(!showDetails);
            artFrame.SetActive(showDetails);
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
                new Vector2(160f, 30f), new Vector2(0f, -22f));
            selectedLabel.gameObject.SetActive(false);

            button.onClick.AddListener(() =>
            {
                if (_hasSelectedReward || option == null)
                    return;

                bool isFinalSelection = _isFinalRewardSelection;
                SoundManager.PlaySfxSound(isFinalSelection ? SoundIds.SfxRewardClaim : SoundIds.SfxRewardSelect);
                StartCoroutine(PulseTransform(box.transform, 1.06f, 0.2f));
                _hasSelectedReward = true;

                question.gameObject.SetActive(false);
                artFrame.SetActive(true);
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

            StartCoroutine(PopIn(box.transform, displayIndex * 0.08f, 1.06f, 0.22f));
        }

        private Sprite ResolveRewardArtwork(RewardOption option)
        {
            if (option == null)
                return null;

            if (option.Artwork != null)
                return option.Artwork;

            return option.Kind switch
            {
                RewardKind.HwaTuCard => FindCardArtwork(
                    string.IsNullOrWhiteSpace(option.PayloadId) ? "M1_Gwang" : option.PayloadId),
                RewardKind.Joker => ResolveJokerArtwork(option.PayloadId),
                RewardKind.Accessory => ResolveAccessoryArtwork(option.PayloadId),
                _ => null
            };
        }

        private static Sprite ResolveBuiltInItemSprite(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

#if UNITY_EDITOR
            string path = itemId switch
            {
                "ACC_REROLL_BONUS" => "Assets/Project/Art/Accessories/norigae.png",
                "ACC_DAMAGE_BONUS" => "Assets/Project/Art/Accessories/silverknife.png",
                "ACC_JADE_RING" => "Assets/Project/Art/Accessories/jadering.png",
                "ACC_GAT" => "Assets/Project/Art/Accessories/gat.png",
                "ACC_MAPE" => "Assets/Project/Art/Accessories/mape.png",
                "ACC_NORIGAE" => "Assets/Project/Art/Accessories/norigae.png",
                "JKR_REROLL_BURST" => "Assets/Project/Art/Joker/boone.png",
                "JKR_HIGH_CARD" => "Assets/Project/Art/Joker/yangban.png",
                "JKR_DOUBLE_PIP" => "Assets/Project/Art/Joker/mokjoong.png",
                "JKR_LUCKY_CHARM" => "Assets/Project/Art/Joker/gaksi.png",
                _ => null
            };

            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
        }

        private static Sprite ResolvePrefabImageSprite(GameObject prefab)
        {
            if (prefab == null)
                return null;

            Image image = prefab.GetComponentInChildren<Image>(true);
            return image != null ? image.sprite : null;
        }

        private static string GetRewardFallbackIcon(RewardOption option)
        {
            if (option == null)
                return "?";

            return option.Kind switch
            {
                RewardKind.HwaTuCard => "패",
                RewardKind.Joker => "J",
                RewardKind.Accessory => "장",
                _ => "?"
            };
        }

        private static Color GetRewardArtworkFrameColor(RewardOption option)
        {
            if (option == null)
                return new Color(0.15f, 0.16f, 0.2f, 1f);

            return option.Kind switch
            {
                RewardKind.HwaTuCard => new Color(0.25f, 0.14f, 0.14f, 1f),
                RewardKind.Joker => new Color(0.13f, 0.18f, 0.28f, 1f),
                RewardKind.Accessory => new Color(0.2f, 0.18f, 0.1f, 1f),
                _ => new Color(0.15f, 0.16f, 0.2f, 1f)
            };
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
            TMP_FontAsset fallback = null;

            if (_battleResultText != null && _battleResultText.font != null)
                fallback = _battleResultText.font;

            if (fallback == null && _playerHpText != null && _playerHpText.font != null)
                fallback = _playerHpText.font;

            if (fallback == null && _enemyHpText != null && _enemyHpText.font != null)
                fallback = _enemyHpText.font;

            return GameUIFont.Resolve(fallback);
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
        private void AnimateHealthText(TextMeshProUGUI text, bool tookDamage)
        {
            if (text == null || !gameObject.activeInHierarchy)
                return;

            Color flashColor = tookDamage
                ? new Color(1f, 0.28f, 0.24f, 1f)
                : new Color(0.35f, 1f, 0.55f, 1f);
            StartCoroutine(PulseText(text, flashColor, tookDamage ? 1.16f : 1.09f));

            if (tookDamage)
                StartCoroutine(UITweenHelper.ShakeRect(text.rectTransform, 0.22f, 5f));
        }

        private void SetHealthBarFill(Image fillImage, float targetFill, ref Coroutine routine, bool instant)
        {
            if (fillImage == null)
                return;

            targetFill = Mathf.Clamp01(targetFill);
            if (routine != null)
                StopCoroutine(routine);

            if (instant || !gameObject.activeInHierarchy)
            {
                ApplyHealthBarFill(fillImage, targetFill);
                routine = null;
                return;
            }

            routine = StartCoroutine(AnimateHealthBarFill(fillImage, targetFill));
        }

        private IEnumerator AnimateHealthBarFill(Image fillImage, float targetFill)
        {
            if (fillImage == null)
                yield break;

            float from = fillImage.fillAmount;
            float elapsed = 0f;
            while (elapsed < HealthBarLerpSeconds)
            {
                if (fillImage == null)
                    yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / HealthBarLerpSeconds);
                ApplyHealthBarFill(fillImage, Mathf.Lerp(from, targetFill, ApplyLocalEase(t, UITweenHelper.EaseType.OutQuad)));
                yield return null;
            }

            if (fillImage != null)
                ApplyHealthBarFill(fillImage, targetFill);
        }

        private void ApplyHealthBarFill(Image fillImage, float fill)
        {
            if (fillImage == null)
                return;

            fill = Mathf.Clamp01(fill);
            fillImage.fillAmount = fill;

            RectTransform rect = fillImage.rectTransform;
            Vector2 anchorMax = rect.anchorMax;
            anchorMax.x = fill;
            rect.anchorMax = anchorMax;
        }

        private IEnumerator PulseText(TextMeshProUGUI text, Color flashColor, float scale)
        {
            if (text == null)
                yield break;

            Color originalColor = text.color;
            Transform target = text.transform;
            Vector3 originalScale = target.localScale;

            text.color = flashColor;
            yield return ScaleTransform(target, originalScale * scale, 0.08f, UITweenHelper.EaseType.OutCubic);
            yield return ScaleTransform(target, originalScale, 0.14f, UITweenHelper.EaseType.OutBack);
            if (text == null)
                yield break;

            text.color = originalColor;
        }

        private IEnumerator PulseTransform(Transform target, float scale, float duration)
        {
            if (target == null)
                yield break;

            Vector3 originalScale = target.localScale;
            yield return ScaleTransform(target, originalScale * scale, duration * 0.45f, UITweenHelper.EaseType.OutCubic);
            yield return ScaleTransform(target, originalScale, duration * 0.55f, UITweenHelper.EaseType.OutBack);
        }

        private IEnumerator PopIn(Transform target, float delay, float scale, float duration)
        {
            if (target == null)
                yield break;

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (target == null)
                yield break;

            Vector3 finalScale = Vector3.one;
            target.localScale = finalScale * 0.58f;
            yield return ScaleTransform(target, finalScale * scale, duration * 0.55f, UITweenHelper.EaseType.OutBack);
            yield return ScaleTransform(target, finalScale, duration * 0.45f, UITweenHelper.EaseType.OutCubic);
        }

        private IEnumerator ScaleTransform(Transform target, Vector3 to, float duration, UITweenHelper.EaseType ease)
        {
            if (target == null)
                yield break;

            Vector3 from = target.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null)
                    yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = ApplyLocalEase(t, ease);
                target.localScale = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }

            if (target == null)
                yield break;

            target.localScale = to;
        }

        private static float ApplyLocalEase(float t, UITweenHelper.EaseType ease)
        {
            switch (ease)
            {
                case UITweenHelper.EaseType.OutBack:
                    const float s = 1.70158f;
                    float t1 = t - 1f;
                    return t1 * t1 * ((s + 1f) * t1 + s) + 1f;
                case UITweenHelper.EaseType.OutCubic:
                    float f = t - 1f;
                    return f * f * f + 1f;
                case UITweenHelper.EaseType.OutQuad:
                    return t * (2f - t);
                default:
                    return t;
            }
        }

        private float CalculateCardRotation(int index, int totalCards)
        {
            if (totalCards <= 1) return 0f;
            float center = (totalCards - 1) / 2f;
            return ((center - index) / center) * _maxTiltAngle;
        }

        private Sprite FindCardArtwork(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return null;

            if (_cardArtworkDefinitions != null)
            {
                foreach (CardArtworkDefinition definition in _cardArtworkDefinitions)
                {
                    if (definition != null && definition.CardId == cardId && definition.Artwork != null)
                        return definition.Artwork;
                }
            }

            Sprite artwork = ResolveCardPrefabArtwork(cardId);
            if (artwork != null)
                return artwork;

            return HwaTuCardDatabase.GetArtwork(cardId);
        }

        private Sprite ResolveCardPrefabArtwork(string cardId)
        {
            if (_cardPrefab == null)
                return null;

            CardUIComponent cardView = _cardPrefab.GetComponent<CardUIComponent>();
            if (cardView == null)
                cardView = _cardPrefab.GetComponentInChildren<CardUIComponent>(true);

            return cardView != null ? cardView.ResolveArtworkForCardId(cardId) : null;
        }
    }
}
