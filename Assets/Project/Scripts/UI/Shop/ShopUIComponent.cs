using System;
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
        }

        private void OpenShopBag()
        {
            SoundManager.PlayDefaultUiClick();

            if (_dialogueGroup != null) _dialogueGroup.SetActive(false);
            if (_shopPanel != null) _shopPanel.SetActive(true);
            if (_cardRemovalPanel != null) _cardRemovalPanel.SetActive(false);
            if (_passButton != null) _passButton.gameObject.SetActive(false);
            if (_feedbackText != null)
            {
                _feedbackText.gameObject.SetActive(true);
                _feedbackText.text = "필요한 물건을 골라보라냥.";
            }
        }

        private void LeaveShop()
        {
            SoundManager.PlayDefaultUiClick();
            OnLeave?.Invoke();
        }

        private void TryPurchase(ShopItemBinding item)
        {
            if (item == null || string.IsNullOrEmpty(item.Id))
                return;

            SoundManager.PlayDefaultUiClick();
            SyncGoldFromProvider();

            if (item.Kind == ShopItemKind.CardRemoval)
            {
                OpenCardRemovalPanel(item);
                return;
            }

            if (_soldItemIds.Contains(item.Id))
            {
                SetFeedback("이미 챙긴 물건이다냥.");
                return;
            }

            if (_currentGold < item.Price)
            {
                SetFeedback("엽전이 부족하다냥.");
                return;
            }

            if (!CompletePurchase(item))
                return;

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
                SetFeedback("이미 이용한 서비스다냥.");
                return;
            }

            if (_currentGold < item.Price)
            {
                SetFeedback("엽전이 부족하다냥.");
                return;
            }

            _activeCardRemovalItem = item;
            _selectedRemovalCardId = null;
            _selectedRemovalCardView = null;
            BuildCardRemovalGrid();

            if (_cardRemovalPanel != null) _cardRemovalPanel.SetActive(true);
            if (_confirmCardRemovalButton != null) _confirmCardRemovalButton.interactable = false;
            if (_selectedRemovalCardText != null) _selectedRemovalCardText.text = "삭제할 카드 1장을 선택하세요.";
            SetFeedback("삭제할 카드 1장을 고르라냥.");
        }

        private void BuildCardRemovalGrid()
        {
            ClearChildren(_cardRemovalGrid);

            IReadOnlyList<string> deckCardIds = OnDeckCardIdsRequested?.Invoke();
            if (deckCardIds == null || deckCardIds.Count == 0)
            {
                SetFeedback("삭제할 카드가 없다냥.");
                return;
            }

            if (_cardPrefab == null)
            {
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
                SetFeedback("삭제할 카드를 먼저 고르라냥.");
                return;
            }

            if (_currentGold < _activeCardRemovalItem.Price)
            {
                SetFeedback("엽전이 부족하다냥.");
                return;
            }

            SoundManager.PlayDefaultUiClick();
            if (!CompletePurchase(_activeCardRemovalItem))
                return;

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
            if (!TrySpendGold(item.Price))
            {
                SetFeedback("엽전이 부족하다냥.");
                return false;
            }

            _soldItemIds.Add(item.Id);

            if (item.Button != null) item.Button.interactable = false;
            if (item.StateText != null) item.StateText.text = "구매 완료";
            if (item.SoldOverlay != null) item.SoldOverlay.SetActive(true);

            RefreshGold();
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
                return false;

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
            }
        }
    }
}
