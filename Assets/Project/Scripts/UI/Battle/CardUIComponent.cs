using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FFF.Data;
using System;

namespace FFF.UI.Battle
{
    /// <summary>
    /// 개별 카드 UI를 담당. 클릭 시 시각적 피드백(1.1배 확대)을 줍니다.
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
            // 애니메이션 없이 즉각적으로 1.1배 확대 / 1.0배 축소
            transform.localScale = isSelected ? new Vector3(1.1f, 1.1f, 1.1f) : Vector3.one;
        }
    }
}