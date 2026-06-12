using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FFF.Audio;
using FFF.UI.Core;

namespace FFF.UI.Treasure
{
    /// <summary>
    /// TreasureScene view. Handles chest/item/pass button presentation and sends user intent outward.
    /// </summary>
    public class TreasureUIComponent : BaseUIComponent
    {
        public sealed class TreasureRewardModel
        {
            public string Id;
            public string DisplayName;
            public string Description;
            public Sprite Icon;
        }

        public Action OnLeave;
        public Action OnChestOpened;
        public Action<string> OnRewardClaimed;

        [SerializeField] private GameObject _chestGroup;
        [SerializeField] private GameObject _rewardGroup;
        [SerializeField] private Button _chestButton;
        [SerializeField] private Button _rewardButton;
        [SerializeField] private Button _passButton;
        [SerializeField] private Image _rewardIcon;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private TextMeshProUGUI _rewardFallbackText;
        [SerializeField] private TextMeshProUGUI _rewardNameText;
        [SerializeField] private TextMeshProUGUI _rewardDescriptionText;
        [SerializeField] private TextMeshProUGUI _rewardHintText;

        private TreasureRewardModel _reward;
        private Coroutine _pulseRoutine;
        private bool _isChestOpen;
        private bool _isRewardClaimed;

        public void Bind(
            GameObject chestGroup,
            GameObject rewardGroup,
            Button chestButton,
            Button rewardButton,
            Button passButton,
            Image rewardIcon,
            TextMeshProUGUI titleText,
            TextMeshProUGUI messageText,
            TextMeshProUGUI rewardFallbackText,
            TextMeshProUGUI rewardNameText,
            TextMeshProUGUI rewardDescriptionText,
            TextMeshProUGUI rewardHintText)
        {
            _chestGroup = chestGroup;
            _rewardGroup = rewardGroup;
            _chestButton = chestButton;
            _rewardButton = rewardButton;
            _passButton = passButton;
            _rewardIcon = rewardIcon;
            _titleText = titleText;
            _messageText = messageText;
            _rewardFallbackText = rewardFallbackText;
            _rewardNameText = rewardNameText;
            _rewardDescriptionText = rewardDescriptionText;
            _rewardHintText = rewardHintText;

            WireButtons();
            ResetViewState();
        }

        public void SetReward(TreasureRewardModel reward)
        {
            _reward = reward;
            RefreshRewardVisual();
        }

        protected override void OnInitialize()
        {
            WireButtons();
            ResetViewState();
        }

        private void OnDestroy()
        {
            UnwireButtons();
        }

        private void WireButtons()
        {
            UnwireButtons();

            if (_chestButton != null)
                _chestButton.onClick.AddListener(OpenChest);
            if (_rewardButton != null)
                _rewardButton.onClick.AddListener(ClaimReward);
            if (_passButton != null)
                _passButton.onClick.AddListener(LeaveTreasure);
        }

        private void UnwireButtons()
        {
            if (_chestButton != null)
                _chestButton.onClick.RemoveListener(OpenChest);
            if (_rewardButton != null)
                _rewardButton.onClick.RemoveListener(ClaimReward);
            if (_passButton != null)
                _passButton.onClick.RemoveListener(LeaveTreasure);
        }

        private void ResetViewState()
        {
            _isChestOpen = false;
            _isRewardClaimed = false;

            if (_titleText != null)
                _titleText.text = "보물 상자";
            if (_messageText != null)
                _messageText.text = "낡은 상자 안에서 희미한 빛이 새어 나옵니다.";
            if (_chestGroup != null)
                _chestGroup.SetActive(true);
            if (_rewardGroup != null)
                _rewardGroup.SetActive(false);
            if (_chestButton != null)
                _chestButton.interactable = true;
            if (_passButton != null)
                _passButton.interactable = true;

            RefreshRewardVisual();
        }

        private void OpenChest()
        {
            if (_isChestOpen)
                return;

            _isChestOpen = true;
            SoundManager.PlaySfxSound(SoundIds.SfxRewardOpen);
            OnChestOpened?.Invoke();

            if (_chestButton != null)
                _chestButton.interactable = false;
            if (_chestGroup != null)
                _chestGroup.SetActive(false);
            if (_rewardGroup != null)
            {
                _rewardGroup.SetActive(true);
                PlayPulse(_rewardGroup.transform, 1.08f, 0.24f);
            }

            if (_messageText != null)
                _messageText.text = _reward != null
                    ? "상자에서 장신구 하나가 모습을 드러냈습니다."
                    : "상자는 이미 비어 있었습니다.";

            RefreshRewardVisual();
        }

        private void ClaimReward()
        {
            if (!_isChestOpen || _isRewardClaimed || _reward == null)
                return;

            _isRewardClaimed = true;
            if (_rewardButton != null)
                _rewardButton.interactable = false;
            if (_passButton != null)
                _passButton.interactable = false;
            if (_messageText != null)
                _messageText.text = $"{GetRewardName()}을(를) 챙겼습니다.";
            if (_rewardHintText != null)
                _rewardHintText.text = "획득 완료";

            SoundManager.PlaySfxSound(SoundIds.SfxRewardClaim);
            OnRewardClaimed?.Invoke(_reward.Id);
        }

        private void LeaveTreasure()
        {
            SoundManager.PlayUiSound(SoundIds.UiCancel);
            OnLeave?.Invoke();
        }

        private void RefreshRewardVisual()
        {
            bool hasReward = _reward != null && !string.IsNullOrEmpty(_reward.Id);

            if (_rewardButton != null)
                _rewardButton.interactable = _isChestOpen && hasReward && !_isRewardClaimed;

            if (_rewardIcon != null)
            {
                _rewardIcon.sprite = hasReward ? _reward.Icon : null;
                _rewardIcon.enabled = hasReward && _reward.Icon != null;
            }

            if (_rewardFallbackText != null)
            {
                _rewardFallbackText.gameObject.SetActive(!hasReward || _reward.Icon == null);
                _rewardFallbackText.text = hasReward ? "장신구" : "비어 있음";
            }

            if (_rewardNameText != null)
                _rewardNameText.text = hasReward ? GetRewardName() : "빈 상자";

            if (_rewardDescriptionText != null)
            {
                _rewardDescriptionText.text = hasReward && !string.IsNullOrWhiteSpace(_reward.Description)
                    ? _reward.Description
                    : "가져갈 수 있는 장신구가 없습니다.";
            }

            if (_rewardHintText != null)
                _rewardHintText.text = hasReward ? "눌러서 가져가기" : "지나갈 수 있습니다";
        }

        private string GetRewardName()
        {
            if (_reward == null || string.IsNullOrWhiteSpace(_reward.DisplayName))
                return _reward != null ? _reward.Id : "장신구";

            return _reward.DisplayName;
        }

        private void PlayPulse(Transform target, float scale, float duration)
        {
            if (target == null || !isActiveAndEnabled)
                return;

            if (_pulseRoutine != null)
                StopCoroutine(_pulseRoutine);
            _pulseRoutine = StartCoroutine(PulseTransform(target, scale, duration));
        }

        private IEnumerator PulseTransform(Transform target, float scale, float duration)
        {
            Vector3 original = Vector3.one;
            target.localScale = original * 0.9f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                target.localScale = Vector3.LerpUnclamped(original * 0.9f, original * scale, eased);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration * 0.6f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / (duration * 0.6f));
                target.localScale = Vector3.LerpUnclamped(original * scale, original, t);
                yield return null;
            }

            target.localScale = original;
            _pulseRoutine = null;
        }
    }
}
