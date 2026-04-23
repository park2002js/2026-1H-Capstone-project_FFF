using System;
using UnityEngine;
using UnityEngine.UI;
using FFF.UI.Core;

namespace FFF.UI.Main
{
    public class MainUIComponent : BaseUIComponent
    {
        public Action OnNewGame;
        public Action OnContinue;

        [Header("UI 참조")]
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _continueButton;

        protected override void Awake()
        {
            base.Awake();
            
            if (_newGameButton != null)
            {
                _newGameButton.onClick.AddListener(OnNewGameButton_Clicked);
            }
            
            if (_continueButton != null)
            {
                _continueButton.onClick.AddListener(OnContinueButton_Clicked);
            }
        }

        private void OnNewGameButton_Clicked()
        {
            Debug.Log("[MainUI] New Game 버튼 클릭! GameManager로 이벤트를 전달합니다.");
            OnNewGame?.Invoke();
        }

        private void OnContinueButton_Clicked()
        {
            Debug.Log("[MainUI] Continue 버튼 클릭! GameManager로 이벤트를 전달합니다.");
            OnContinue?.Invoke();
        }
    }
}