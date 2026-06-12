using System;
using UnityEngine;

namespace FFF.UI.Core
{
    /// <summary>
    /// 모든 UI 화면(Screen)의 베이스 클래스.
    /// 아키텍처 다이어그램에서 "UI Components - Dumb" 역할에 해당한다.
    /// 
    /// 각 UI 화면(TitleUI, MainUI, MapUI, BattleUI, ShopUI)은
    /// 이 클래스를 상속받아 구현한다.
    /// 
    /// Dumb 원칙:
    /// - 자신의 UI 표시/숨김만 담당한다.
    /// - 비즈니스 로직을 알지 못한다.
    /// - 사용자 행동은 이벤트(Action)로 UIManager에게 전달만 한다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseUIComponent : MonoBehaviour
    {
        [Header("UI 화면 설정")]
        [SerializeField] private CanvasGroup _canvasGroup;

        /// <summary>
        /// 이 UI 화면이 현재 활성화 상태인지 여부.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// UI 화면을 표시한다. UIManager에서 호출.
        /// </summary>
        public virtual void Show()
        {
            gameObject.SetActive(true);
            IsActive = true;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            OnShow();
        }

        /// <summary>
        /// UI 화면을 숨긴다. UIManager에서 호출.
        /// </summary>
        public virtual void Hide()
        {
            IsActive = false;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);

            OnHide();
        }

        /// <summary>
        /// UI 화면의 데이터를 초기화한다. Scene 전환 후 호출.
        /// </summary>
        public virtual void Initialize()
        {
            OnInitialize();
        }

        /// <summary>
        /// Show 시 하위 클래스에서 추가 처리할 내용.
        /// 예: 애니메이션 재생, 데이터 바인딩 등
        /// </summary>
        protected virtual void OnShow() { }

        /// <summary>
        /// Hide 시 하위 클래스에서 추가 처리할 내용.
        /// 예: 애니메이션 정리, 타이머 해제 등
        /// </summary>
        protected virtual void OnHide() { }

        /// <summary>
        /// Initialize 시 하위 클래스에서 추가 처리할 내용.
        /// 예: UI 요소 참조 캐싱, 초기 상태 설정 등
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// CanvasGroup이 Inspector에서 할당되지 않았으면 자동 탐색.
        /// </summary>
        protected virtual void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }
    }
}