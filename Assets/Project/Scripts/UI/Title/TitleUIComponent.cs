using UnityEngine;
using UnityEngine.UI;
using FFF.UI.Core;
using FFF.Core;

namespace FFF.UI.Title
{
    /// <summary>
    /// 백로그 10번: 타이틀 화면 UI.
    /// 
    /// 기능:
    /// - 게임 타이틀 표시
    /// - "Press Any Key" 문구 점멸
    /// - 마우스 클릭 또는 키보드 아무 키 입력 시 메인 화면(MainScene)으로 전환
    /// 
    /// Dumb 원칙:
    /// - 입력 감지와 UI 표시만 담당
    /// - Scene 전환 요청은 UIManager를 통해 전달
    /// </summary>
    public class TitleUIComponent : BaseUIComponent
    {
        [Header("=== 타이틀 화면 요소 ===")]
        [Tooltip("'Press Any Key' 텍스트")]
        [SerializeField] private Text _pressAnyKeyText;

        [Header("=== 점멸 설정 ===")]
        [Tooltip("점멸 속도 (초 단위, 한 사이클)")]
        [SerializeField] private float _blinkSpeed = 1.0f;

        [Tooltip("최소 알파값")]
        [SerializeField] private float _minAlpha = 0.2f;

        [Tooltip("최대 알파값")]
        [SerializeField] private float _maxAlpha = 1.0f;

        /// <summary>
        /// 입력을 받을 수 있는 상태인지 여부.
        /// Scene 전환 중 중복 입력 방지용.
        /// </summary>
        private bool _canAcceptInput = false;

        /// <summary>
        /// 점멸 타이머.
        /// </summary>
        private float _blinkTimer = 0f;

        protected override void OnInitialize()
        {
            _canAcceptInput = false;
            _blinkTimer = 0f;
        }

        protected override void OnShow()
        {
            // Show 후 약간의 딜레이를 두고 입력 허용
            // (Scene 로드 직후 잔여 입력 무시 목적)
            Invoke(nameof(EnableInput), 0.5f);
        }

        protected override void OnHide()
        {
            _canAcceptInput = false;
            CancelInvoke(nameof(EnableInput));
        }

        private void EnableInput()
        {
            _canAcceptInput = true;
        }

        private void Update()
        {
            if (!IsActive) return;

            UpdateBlink();
            CheckInput();
        }

        /// <summary>
        /// "Press Any Key" 텍스트 점멸 처리.
        /// 백로그 10번 Non-Functional: "타이틀 화면 하단에 Press any Key 문구의 점멸"
        /// </summary>
        private void UpdateBlink()
        {
            if (_pressAnyKeyText == null) return;

            _blinkTimer += Time.deltaTime;

            // sin 함수로 부드러운 점멸 효과
            float alpha = Mathf.Lerp(_minAlpha, _maxAlpha,
                (Mathf.Sin(_blinkTimer * Mathf.PI * 2f / _blinkSpeed) + 1f) / 2f);

            Color color = _pressAnyKeyText.color;
            color.a = alpha;
            _pressAnyKeyText.color = color;
        }

        /// <summary>
        /// 마우스 클릭 또는 키보드 아무 키 입력 감지.
        /// 백로그 10번 Functional: "아무 키를 입력했을 때, 메인 화면으로 전환된다."
        /// </summary>
        private void CheckInput()
        {
            if (!_canAcceptInput) return;

            bool anyInput = Input.anyKeyDown; // 키보드 + 마우스 클릭 모두 포함

            if (anyInput)
            {
                _canAcceptInput = false; // 중복 입력 방지
                OnAnyKeyPressed();
            }
        }

        /// <summary>
        /// 아무 키 입력 시 처리.
        /// Dumb 원칙에 따라 직접 Scene 전환하지 않고,
        /// SceneLoader를 통해 MainScene으로 이동한다.
        /// 
        /// 추후 전환 이펙트 추가 시 여기서 이펙트 재생 후 전환하도록 변경.
        /// </summary>
        private void OnAnyKeyPressed()
        {
            Debug.Log("[TitleUI] 아무 키 입력 감지 → MainScene 전환 요청");
            SceneLoader.LoadScene(SceneLoader.SceneNames.MAIN);
        }
    }
}