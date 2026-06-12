using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FFF.Audio;
using FFF.UI.Core;

namespace FFF.UI.Rest
{
    /// <summary>
    /// 휴식 화면 View. 휴식 버튼 입력만 외부로 전달하고, 체력 표시는 받은 데이터로 갱신한다.
    /// </summary>
    public class RestUIComponent : BaseUIComponent
    {
        public Action OnRestRequested;

        [SerializeField] private Button _restButton;
        [SerializeField] private TextMeshProUGUI _restButtonText;
        [SerializeField] private TextMeshProUGUI _storyText;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private TextMeshProUGUI _feedbackText;

        private int _currentHealth;
        private int _maxHealth;
        private bool _isResting;

        public void Bind(
            Button restButton,
            TextMeshProUGUI restButtonText,
            TextMeshProUGUI storyText,
            TextMeshProUGUI healthText,
            TextMeshProUGUI feedbackText)
        {
            _restButton = restButton;
            _restButtonText = restButtonText;
            _storyText = storyText;
            _healthText = healthText;
            _feedbackText = feedbackText;

            WireButtons();
            RefreshHealthText();
        }

        public void SetPlayerHealth(int current, int max)
        {
            _maxHealth = Mathf.Max(0, max);
            _currentHealth = Mathf.Clamp(current, 0, _maxHealth);
            RefreshHealthText();
        }

        public void SetStory(string story)
        {
            if (_storyText != null)
                _storyText.text = story;
        }

        protected override void OnInitialize()
        {
            _isResting = false;
            SetRestButtonInteractable(true);
            if (_feedbackText != null)
                _feedbackText.gameObject.SetActive(false);
            RefreshHealthText();
            WireButtons();
        }

        private void OnDestroy()
        {
            UnwireButtons();
        }

        private void WireButtons()
        {
            UnwireButtons();

            if (_restButton != null)
                _restButton.onClick.AddListener(HandleRestClicked);
        }

        private void UnwireButtons()
        {
            if (_restButton != null)
                _restButton.onClick.RemoveListener(HandleRestClicked);
        }

        private void HandleRestClicked()
        {
            if (_isResting)
                return;

            _isResting = true;
            SetRestButtonInteractable(false);
            SetFeedback("짧은 휴식이 몸에 온기를 되돌립니다.");
            SoundManager.PlayUiSound(SoundIds.UiConfirm);

            if (OnRestRequested != null)
            {
                OnRestRequested.Invoke();
                return;
            }

            HealStandalonePreview();
            _isResting = false;
            SetRestButtonInteractable(true);
        }

        private void HealStandalonePreview()
        {
            if (_maxHealth <= 0)
                return;

            int healAmount = Mathf.Max(1, Mathf.CeilToInt(_maxHealth * 0.15f));
            SetPlayerHealth(Mathf.Min(_currentHealth + healAmount, _maxHealth), _maxHealth);
        }

        private void SetRestButtonInteractable(bool interactable)
        {
            if (_restButton != null)
                _restButton.interactable = interactable;

            if (_restButtonText != null)
                _restButtonText.text = interactable ? "휴식" : "회복 중";
        }

        private void RefreshHealthText()
        {
            if (_healthText == null)
                return;

            _healthText.text = _maxHealth > 0
                ? $"체력 {_currentHealth} / {_maxHealth}"
                : "체력 - / -";
        }

        private void SetFeedback(string message)
        {
            if (_feedbackText == null)
                return;

            _feedbackText.gameObject.SetActive(true);
            _feedbackText.text = message;
        }
    }
}
