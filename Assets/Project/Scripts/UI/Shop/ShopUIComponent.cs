using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FFF.Audio;
using FFF.Data;
using FFF.UI.Battle;
using FFF.UI.Core;

namespace FFF.UI.Shop
{
    /// <summary>
    /// 상점 화면 View. 상인 클릭, 보따리 열기, 구매 버튼 입력만 외부로 전달하거나 UI 상태로 반영한다.
    /// </summary>
    public class ShopUIComponent : BaseUIComponent
    {
        public sealed class ShopItemBinding
        {
            public string Id;
            public string DisplayName;
            public ShopItemKind Kind;
            public string PayloadId;
            public int Price;
            public Button Button;
            public TextMeshProUGUI PriceText;
            public TextMeshProUGUI StateText;
            public GameObject SoldOverlay;
        }

        public sealed class CardRemovalPanelBinding
        {
            public GameObject Panel;
            public Transform CardGrid;
            public Button ConfirmButton;
            public Button CancelButton;
            public TextMeshProUGUI SelectedText;
            public GameObject CardPrefab;
        }

        public enum ShopItemKind
        {
            Card,
            Accessory,
            CardRemoval
        }

        public Action OnLeave;
        public Action<string> OnAddDeckCard;
        public Action<string> OnAddAccessory;
        public Func<IReadOnlyList<string>> OnDeckCardIdsRequested;
        public Action<string> OnRemoveDeckCard;
        public Func<int> OnGoldRequested;
        public Func<int, bool> OnSpendGold;

        [SerializeField] private GameObject _dialogueGroup;
        [SerializeField] private GameObject _shopPanel;
        [SerializeField] private GameObject _cardRemovalPanel;
        [SerializeField] private Transform _cardRemovalGrid;
        [SerializeField] private Button _merchantButton;
        [SerializeField] private Button _passButton;
        [SerializeField] private Button _closeShopButton;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private Button _confirmCardRemovalButton;
        [SerializeField] private Button _cancelCardRemovalButton;
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _feedbackText;
        [SerializeField] private TextMeshProUGUI _selectedRemovalCardText;
        [SerializeField] private GameObject _cardPrefab;
        [SerializeField] private int _currentGold = 216;

        private readonly List<ShopItemBinding> _items = new List<ShopItemBinding>();
        private readonly HashSet<string> _soldItemIds = new HashSet<string>();
        private ShopItemBinding _activeCardRemovalItem;
        private string _selectedRemovalCardId;
        private CardUIComponent _selectedRemovalCardView;
        private Coroutine _feedbackRoutine;
        private Coroutine _goldRoutine;

        public void Bind(
            GameObject dialogueGroup,
            GameObject shopPanel,
            Button merchantButton,
            Button passButton,
            Button closeShopButton,
            Button leaveButton,
            TextMeshProUGUI goldText,
            TextMeshProUGUI feedbackText,
            IReadOnlyList<ShopItemBinding> itemBindings,
            CardRemovalPanelBinding cardRemovalPanel)
        {
            _dialogueGroup = dialogueGroup;
            _shopPanel = shopPanel;
            _merchantButton = merchantButton;
            _passButton = passButton;
            _closeShopButton = closeShopButton;
            _leaveButton = leaveButton;
            _goldText = goldText;
            _feedbackText = feedbackText;

            if (cardRemovalPanel != null)
            {
                _cardRemovalPanel = cardRemovalPanel.Panel;
                _cardRemovalGrid = cardRemovalPanel.CardGrid;
                _confirmCardRemovalButton = cardRemovalPanel.ConfirmButton;
                _cancelCardRemovalButton = cardRemovalPanel.CancelButton;
                _selectedRemovalCardText = cardRemovalPanel.SelectedText;
                _cardPrefab = cardRemovalPanel.CardPrefab;
            }

            _items.Clear();
            if (itemBindings != null)
                _items.AddRange(itemBindings);

            WireButtons();
            RefreshGold();
            ShowDialogue();
        }

        protected override void OnInitialize()
        {
            RefreshGold();
            ShowDialogue();
        }

        private void OnDestroy()
        {
            UnwireButtons();
        }

        private void WireButtons()
        {
            UnwireButtons();

            if (_merchantButton != null) _merchantButton.onClick.AddListener(OpenShopBag);
            if (_passButton != null) _passButton.onClick.AddListener(LeaveShop);
            if (_closeShopButton != null) _closeShopButton.onClick.AddListener(ShowDialogue);
            if (_leaveButton != null) _leaveButton.onClick.AddListener(LeaveShop);
            if (_confirmCardRemovalButton != null) _confirmCardRemovalButton.onClick.AddListener(ConfirmCardRemoval);
            if (_cancelCardRemovalButton != null) _cancelCardRemovalButton.onClick.AddListener(CloseCardRemovalPanel);

            foreach (ShopItemBinding item in _items)
            {
                ShopItemBinding captured = item;
                if (captured != null && captured.Button != null)
                    captured.Button.onClick.AddListener(() => TryPurchase(captured));
            }
        }

        private void UnwireButtons()
        {
            if (_merchantButton != null) _merchantButton.onClick.RemoveListener(OpenShopBag);
            if (_passButton != null) _passButton.onClick.RemoveListener(LeaveShop);
            if (_closeShopButton != null) _closeShopButton.onClick.RemoveListener(ShowDialogue);
            if (_leaveButton != null) _leaveButton.onClick.RemoveListener(LeaveShop);
            if (_confirmCardRemovalButton != null) _confirmCardRemovalButton.onClick.RemoveListener(ConfirmCardRemoval);
            if (_cancelCardRemovalButton != null) _cancelCardRemovalButton.onClick.RemoveListener(CloseCardRemovalPanel);

            foreach (ShopItemBinding item in _items)
            {
                if (item != null && item.Button != null)
                    item.Button.onClick.RemoveAllListeners();
            }
        }

        private void ShowDialogue()
        {
            if (_dialogueGroup != null) _dialogueGroup.SetActive(true);
            if (_shopPanel != null) _shopPanel.SetActive(false);
            if (_cardRemovalPanel != null) _cardRemovalPanel.SetActive(false);
            if (_passButton != null) _passButton.gameObject.SetActive(true);
            if (_feedbackText != null) _feedbackText.gameObject.SetActive(false);
            PlayPanelIn(_dialogueGroup);
        }

        private void OpenShopBag()
        {
            SoundManager.PlaySfxSound(SoundIds.SfxShopOpen);

            if (_dialogueGroup != null) _dialogueGroup.SetActive(false);
            if (_shopPanel != null) _shopPanel.SetActive(true);
            if (_cardRemovalPanel != null) _cardRemovalPanel.SetActive(false);
            if (_passButton != null) _passButton.gameObject.SetActive(false);
            if (_feedbackText != null)
            {
                _feedbackText.gameObject.SetActive(true);
                _feedbackText.text = "필요한 물건을 골라보라냥.";
            }
            PlayPanelIn(_shopPanel);
        }

        private void LeaveShop()
        {
            SoundManager.PlaySfxSound(SoundIds.SfxShopLeave);
            OnLeave?.Invoke();
        }

        private void TryPurchase(ShopItemBinding item)
        {
            if (item == null || string.IsNullOrEmpty(item.Id))
                return;

            SyncGoldFromProvider();

            if (item.Kind == ShopItemKind.CardRemoval)
            {
                OpenCardRemovalPanel(item);
                return;
            }

            if (_soldItemIds.Contains(item.Id))
            {
                SoundManager.PlaySfxSound(SoundIds.SfxShopCannotBuy);
                SetFeedback("이미 챙긴 물건이다냥.");
                return;
            }

            if (_currentGold < item.Price)
            {
                SoundManager.PlaySfxSound(SoundIds.SfxShopCannotBuy);
                SetFeedback("엽전이 부족하다냥.");
                ShakeText(_goldText);
                return;
            }

            if (!CompletePurchase(item))
                return;

            SoundManager.PlaySfxSound(SoundIds.SfxShopBuy);
            if (item.Kind == ShopItemKind.Card)
                OnAddDeckCard?.Invoke(item.PayloadId);
            else if (item.Kind == ShopItemKind.Accessory)
                OnAddAccessory?.Invoke(item.PayloadId);

            SetFeedback($"{item.DisplayName}을(를) 샀다냥.");
        }

        private void OpenCardRemovalPanel(ShopItemBinding item)
        {
            SyncGoldFromProvider();

            if (_soldItemIds.Contains(item.Id))
            {
                SoundManager.PlaySfxSound(SoundIds.SfxShopCannotBuy);
                SetFeedback("이미 이용한 서비스다냥.");
                return;
            }

            if (_currentGold < item.Price)
            {
                SoundManager.PlaySfxSound(SoundIds.SfxShopCannotBuy);
                SetFeedback("엽전이 부족하다냥.");
                ShakeText(_goldText);
                return;
            }

            _activeCardRemovalItem = item;
            _selectedRemovalCardId = null;
            _selectedRemovalCardView = null;
            BuildCardRemovalGrid();

            if (_cardRemovalPanel != null) _cardRemovalPanel.SetActive(true);
            if (_confirmCardRemovalButton != null) _confirmCardRemovalButton.interactable = false;
            if (_selectedRemovalCardText != null) _selectedRemovalCardText.text = "삭제할 카드 1장을 선택하세요.";
            SoundManager.PlaySfxSound(SoundIds.SfxShopCardRemove, 0.75f);
            SetFeedback("삭제할 카드 1장을 고르라냥.");
            PlayPanelIn(_cardRemovalPanel);
        }

        private void BuildCardRemovalGrid()
        {
            ClearChildren(_cardRemovalGrid);

            IReadOnlyList<string> deckCardIds = OnDeckCardIdsRequested?.Invoke();
            if (deckCardIds == null || deckCardIds.Count == 0)
            {
                SoundManager.PlayUiSound(SoundIds.UiError);
                SetFeedback("삭제할 카드가 없다냥.");
                return;
            }

            if (_cardPrefab == null)
            {
                SoundManager.PlayUiSound(SoundIds.UiError);
                SetFeedback("카드 프리팹 연결이 필요하다냥.");
                return;
            }

            foreach (string cardId in deckCardIds)
            {
                HwaTuCard card = HwaTuCardDatabase.FindById(cardId);
                if (card == null)
                    continue;

                GameObject cardObject = Instantiate(_cardPrefab, _cardRemovalGrid, false);
                RectTransform rect = cardObject.GetComponent<RectTransform>();
                if (rect != null)
                    rect.sizeDelta = new Vector2(112f, 168f);

                CardUIComponent cardView = cardObject.GetComponent<CardUIComponent>();
                if (cardView != null)
                {
                    cardView.Setup(card, clicked => SelectRemovalCard(cardId, clicked), HwaTuCardDatabase.GetArtwork(cardId));
                }
                else
                {
                    Button button = cardObject.GetComponent<Button>();
                    if (button != null)
                    {
                        string capturedId = cardId;
                        button.onClick.RemoveAllListeners();
                        button.onClick.AddListener(() => SelectRemovalCard(capturedId, null));
                    }
                }
            }
        }

        private void SelectRemovalCard(string cardId, CardUIComponent cardView)
        {
            if (_selectedRemovalCardView != null)
                _selectedRemovalCardView.SetSelected(false);

            _selectedRemovalCardId = cardId;
            _selectedRemovalCardView = cardView;

            if (_selectedRemovalCardView != null)
                _selectedRemovalCardView.SetSelected(true);
            SoundManager.PlaySfxSound(SoundIds.SfxCardSelect);

            HwaTuCard card = HwaTuCardDatabase.FindById(cardId);
            if (_selectedRemovalCardText != null)
                _selectedRemovalCardText.text = card != null ? $"{card.DisplayName} 선택됨" : "카드 선택됨";

            if (_confirmCardRemovalButton != null)
                _confirmCardRemovalButton.interactable = true;
        }

        private void ConfirmCardRemoval()
        {
            if (_activeCardRemovalItem == null || string.IsNullOrEmpty(_selectedRemovalCardId))
            {
                SoundManager.PlayUiSound(SoundIds.UiError);
                SetFeedback("삭제할 카드를 먼저 고르라냥.");
                return;
            }

            if (_currentGold < _activeCardRemovalItem.Price)
            {
                SoundManager.PlaySfxSound(SoundIds.SfxShopCannotBuy);
                SetFeedback("엽전이 부족하다냥.");
                ShakeText(_goldText);
                return;
            }

            if (!CompletePurchase(_activeCardRemovalItem))
                return;

            SoundManager.PlaySfxSound(SoundIds.SfxShopCardRemove);
            OnRemoveDeckCard?.Invoke(_selectedRemovalCardId);
            CloseCardRemovalPanel();
            SetFeedback("카드 1장을 덱에서 제거했다냥.");
        }

        private void CloseCardRemovalPanel()
        {
            if (_cardRemovalPanel != null)
                _cardRemovalPanel.SetActive(false);

            _activeCardRemovalItem = null;
            _selectedRemovalCardId = null;
            _selectedRemovalCardView = null;
            ClearChildren(_cardRemovalGrid);
        }

        private bool CompletePurchase(ShopItemBinding item)
        {
            int beforeGold = _currentGold;
            if (!TrySpendGold(item.Price))
            {
                SetFeedback("엽전이 부족하다냥.");
                return false;
            }

            _soldItemIds.Add(item.Id);

            if (item.Button != null) item.Button.interactable = false;
            if (item.StateText != null) item.StateText.text = "구매 완료";
            if (item.SoldOverlay != null) item.SoldOverlay.SetActive(true);

            if (item.Button != null)
                StartCoroutine(PulseTransform(item.Button.transform, 1.08f, 0.18f));

            RefreshGoldAnimated(beforeGold, _currentGold);
            return true;
        }

        private void ClearChildren(Transform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }

        private void RefreshGold()
        {
            SyncGoldFromProvider();
            if (_goldText != null)
                _goldText.text = $"엽전 {_currentGold}";
        }

        private void RefreshGoldAnimated(int from, int to)
        {
            if (_goldText == null)
            {
                RefreshGold();
                return;
            }

            if (_goldRoutine != null)
                StopCoroutine(_goldRoutine);
            _goldRoutine = StartCoroutine(AnimateGold(from, to));
        }

        private void SyncGoldFromProvider()
        {
            if (OnGoldRequested != null)
                _currentGold = Mathf.Max(0, OnGoldRequested.Invoke());
        }

        private bool TrySpendGold(int amount)
        {
            if (amount <= 0)
                return true;

            bool spent = OnSpendGold != null ? OnSpendGold.Invoke(amount) : _currentGold >= amount;
            if (!spent)
            {
                SoundManager.PlaySfxSound(SoundIds.SfxShopCannotBuy);
                return false;
            }

            if (OnSpendGold == null)
                _currentGold -= amount;
            else
                SyncGoldFromProvider();

            return true;
        }

        private void SetFeedback(string message)
        {
            if (_feedbackText != null)
            {
                _feedbackText.gameObject.SetActive(true);
                _feedbackText.text = message;
                if (_feedbackRoutine != null)
                    StopCoroutine(_feedbackRoutine);
                _feedbackRoutine = StartCoroutine(PulseText(_feedbackText, 1.05f, 0.16f));
            }
        }

        private void PlayPanelIn(GameObject panel)
        {
            if (panel == null || !panel.activeInHierarchy)
                return;

            StartCoroutine(PanelIn(panel.transform));
        }

        private IEnumerator PanelIn(Transform panel)
        {
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            panel.localScale = Vector3.one * 0.94f;

            float elapsed = 0f;
            const float duration = 0.18f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                panel.localScale = Vector3.LerpUnclamped(Vector3.one * 0.94f, Vector3.one, eased);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            panel.localScale = Vector3.one;
        }

        private IEnumerator AnimateGold(int from, int to)
        {
            float elapsed = 0f;
            const float duration = 0.35f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                int value = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
                _goldText.text = $"엽전 {value}";
                yield return null;
            }

            _goldText.text = $"엽전 {to}";
            yield return PulseText(_goldText, 1.08f, 0.14f);
            _goldRoutine = null;
        }

        private void ShakeText(TextMeshProUGUI text)
        {
            if (text != null)
                StartCoroutine(ShakeRect(text.rectTransform, 0.2f, 5f));
        }

        private IEnumerator PulseText(TextMeshProUGUI text, float scale, float duration)
        {
            if (text == null)
                yield break;

            yield return PulseTransform(text.transform, scale, duration);
        }

        private IEnumerator PulseTransform(Transform target, float scale, float duration)
        {
            Vector3 original = target.localScale;
            yield return ScaleTo(target, original * scale, duration * 0.45f);
            yield return ScaleTo(target, original, duration * 0.55f);
        }

        private IEnumerator ScaleTo(Transform target, Vector3 to, float duration)
        {
            Vector3 from = target.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                target.localScale = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }

            target.localScale = to;
        }

        private IEnumerator ShakeRect(RectTransform rect, float duration, float magnitude)
        {
            Vector2 original = rect.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                rect.anchoredPosition = original + UnityEngine.Random.insideUnitCircle * magnitude;
                yield return null;
            }

            rect.anchoredPosition = original;
        }
    }
}
