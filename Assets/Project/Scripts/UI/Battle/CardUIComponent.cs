using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using FFF.Data;
using FFF.UI.Animation;
using System;
using System.Collections;
using System.Collections.Generic;

namespace FFF.UI.Battle
{
    /// <summary>
    /// 개별 카드 UI를 담당. 클릭 시 시각적 피드백(애니메이션 연출)을 줍니다.
    /// </summary>
    public class CardUIComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Serializable]
        private class CardArtworkDefinition
        {
            [SerializeField] private string _cardId;
            [SerializeField] private Sprite _artwork;

            public string CardId => _cardId;
            public Sprite Artwork => _artwork;
        }

        [SerializeField] private List<CardArtworkDefinition> _artworkDefinitions = new();
        [SerializeField] private Image _cardImage;
        [SerializeField] private TextMeshProUGUI _cardNameText;
        [SerializeField] private Button _cardButton;
        [SerializeField] private float _extendedHoverHeight = 170f;
        [SerializeField] private float _extendedHoverHorizontalInset = 10f;

        public HwaTuCard CardData { get; private set; }
        private Action<CardUIComponent> _onClickCallback;
        private Coroutine _rejectRoutine;
        private GameObject _extendedHoverCatchArea;
        private bool _isSelected;
        private bool _isPointerInside;

        public void Setup(HwaTuCard cardData, Action<CardUIComponent> onClickCallback, Sprite artworkOverride = null)
        {
            CardData = cardData;
            _onClickCallback = onClickCallback;
            EnsureReferences();

            Sprite artwork = ResolveArtwork(cardData, artworkOverride);

            if (_cardImage != null && artwork != null)
            {
                _cardImage.sprite = artwork;
                _cardImage.type = Image.Type.Simple;
                _cardImage.preserveAspect = true;
                _cardImage.color = Color.white;
            }

            if (_cardNameText != null)
            {
                _cardNameText.text = cardData.DisplayName;
                _cardNameText.gameObject.SetActive(true);
            }

            if (_cardButton != null)
            {
                _cardButton.transition = Selectable.Transition.None;
                _cardButton.onClick.RemoveAllListeners();
                _cardButton.onClick.AddListener(HandleClick);
            }
            
            SetSelected(false); // 초기 상태는 선택 해제 (크기 1.0)
        }

        public Sprite ResolveArtworkForCardId(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return null;

            // 프리팹에 직접 지정한 아트워크가 있으면 우선 사용하고,
            // 그 외에는 모든 화면이 공유하는 HwaTuCardDatabase 해석 경로로 통일한다.
            Sprite artwork = FindLocalArtwork(cardId);
            if (artwork != null)
                return artwork;

            return HwaTuCardDatabase.ResolveArtwork(cardId);
        }

        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;

            // CardAnimator가 프리팹에 붙어있으면 부드러운 연출로 위임
            var animator = GetComponent<CardAnimator>();
            if (animator != null)
            {
                if (isSelected)
                {
                    transform.SetAsLastSibling();
                    animator.PlaySelect();
                }
                else
                {
                    animator.PlayDeselect();

                    if (_isPointerInside)
                        animator.PlayHoverEnter();
                }

                return;
            }

            // CardAnimator가 없을 경우의 폴백.
            transform.localScale = isSelected ? new Vector3(1.1f, 1.1f, 1.1f) : Vector3.one;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerInside = true;

            var animator = GetComponent<CardAnimator>();
            if (animator == null || _isSelected)
                return;

            transform.SetAsLastSibling();
            animator.PlayHoverEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerInside = false;

            var animator = GetComponent<CardAnimator>();
            if (animator == null || _isSelected)
                return;

            animator.PlayHoverExit();
        }

        public void SetExtendedHoverCatchArea(bool isEnabled)
        {
            if (isEnabled)
                EnsureExtendedHoverCatchArea();

            if (_extendedHoverCatchArea != null)
                _extendedHoverCatchArea.SetActive(isEnabled);
        }

        public void PlayRejectFeedback()
        {
            if (_rejectRoutine != null)
                StopCoroutine(_rejectRoutine);

            _rejectRoutine = StartCoroutine(RejectFeedbackRoutine());
        }

        private void HandleClick()
        {
            _onClickCallback?.Invoke(this);
        }

        private void EnsureExtendedHoverCatchArea()
        {
            if (_extendedHoverCatchArea == null)
            {
                Transform existing = transform.Find("HoverCatchArea");
                if (existing != null)
                    _extendedHoverCatchArea = existing.gameObject;
            }

            if (_extendedHoverCatchArea == null)
            {
                _extendedHoverCatchArea = new GameObject("HoverCatchArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                _extendedHoverCatchArea.transform.SetParent(transform, false);
                _extendedHoverCatchArea.transform.SetAsFirstSibling();
            }

            RectTransform areaRect = _extendedHoverCatchArea.GetComponent<RectTransform>();
            RectTransform cardRect = transform as RectTransform;
            float cardWidth = cardRect != null ? cardRect.rect.width : 0f;
            if (cardWidth <= 0f && cardRect != null)
                cardWidth = cardRect.sizeDelta.x;

            float areaWidth = Mathf.Max(1f, cardWidth - (_extendedHoverHorizontalInset * 2f));
            areaRect.anchorMin = new Vector2(0.5f, 0f);
            areaRect.anchorMax = new Vector2(0.5f, 0f);
            areaRect.pivot = new Vector2(0.5f, 1f);
            areaRect.anchoredPosition = Vector2.zero;
            areaRect.sizeDelta = new Vector2(areaWidth, _extendedHoverHeight);

            Image catchImage = _extendedHoverCatchArea.GetComponent<Image>();
            catchImage.color = new Color(1f, 1f, 1f, 0f);
            catchImage.raycastTarget = true;
        }

        private IEnumerator RejectFeedbackRoutine()
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (rect == null)
                yield break;

            Vector2 originalPosition = rect.anchoredPosition;
            Vector3 originalScale = transform.localScale;
            const float duration = 0.18f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(t * Mathf.PI * 6f);
                rect.anchoredPosition = originalPosition + Vector2.right * (wave * 8f);
                transform.localScale = Vector3.LerpUnclamped(originalScale * 1.04f, originalScale, t);
                yield return null;
            }

            rect.anchoredPosition = originalPosition;
            transform.localScale = originalScale;
            _rejectRoutine = null;
        }

        private Sprite ResolveArtwork(HwaTuCard cardData, Sprite artworkOverride)
        {
            if (artworkOverride != null)
                return artworkOverride;

            Sprite artwork = ResolveArtworkForCardId(cardData.CardId);
            if (artwork != null)
                return artwork;

            Debug.LogWarning($"[CardUIComponent] 카드 이미지가 없어 텍스트로 표시합니다. CardId: {cardData.CardId}, Month: {cardData.Month}, Type: {cardData.Type}");
            return null;
        }

        private void EnsureReferences()
        {
            if (_cardImage == null)
                _cardImage = GetComponent<Image>();

            if (_cardButton == null)
                _cardButton = GetComponent<Button>();
        }

        private Sprite FindLocalArtwork(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || _artworkDefinitions == null)
                return null;

            foreach (CardArtworkDefinition definition in _artworkDefinitions)
            {
                if (definition != null && definition.CardId == cardId)
                    return definition.Artwork;
            }

            return null;
        }
    }
}
