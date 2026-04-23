using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FFF.Data;
using FFF.UI.Animation;
using System;

namespace FFF.UI.Battle
{
    /// <summary>
    /// 개별 카드 UI를 담당. 클릭 시 시각적 피드백(애니메이션 연출)을 줍니다.
    /// </summary>
    public class CardUIComponent : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _cardNameText; // 임시로 이름만 텍스트로 표시
        [SerializeField] private Button _cardButton;

        public HwaTuCard CardData { get; private set; }
        private Action<CardUIComponent> _onClickCallback;

        public void Setup(HwaTuCard cardData, Action<CardUIComponent> onClickCallback)
        {
            CardData = cardData;
            _onClickCallback = onClickCallback;

            if (_cardNameText != null) 
                _cardNameText.text = cardData.DisplayName;

            _cardButton.onClick.RemoveAllListeners();
            _cardButton.onClick.AddListener(() => _onClickCallback?.Invoke(this));
            
            SetSelected(false); // 초기 상태는 선택 해제 (크기 1.0)
        }

        public void SetSelected(bool isSelected)
        {
            // 기존 즉시 스케일 (CardAnimator가 없을 경우의 폴백)
            transform.localScale = isSelected ? new Vector3(1.1f, 1.1f, 1.1f) : Vector3.one;

            // CardAnimator가 프리팹에 붙어있으면 부드러운 연출로 위임
            var animator = GetComponent<CardAnimator>();
            if (animator != null)
            {
                if (isSelected) animator.PlaySelect();
                else animator.PlayDeselect();
            }
        }
    }
}
