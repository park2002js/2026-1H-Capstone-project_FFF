using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FFF.UI.Animation
{
    /// <summary>
    /// 개별 카드의 애니메이션을 전담하는 컴포넌트.
    /// 카드 프리팹의 최상위 오브젝트에 CardUIComponent와 나란히 부착한다.
    /// 
    /// ── 핵심 책임 ──
    /// "여기로 이동해" → 알아서 부드럽게 트윈.
    /// "선택 상태로 바꿔" → 알아서 위로 올라가며 금테 연출.
    /// "사라져" → 알아서 아래로 떨어지며 페이드아웃.
    /// 호출자(BattleAnimationController)는 내부 트윈 로직을 모른다.
    /// 
    /// ── 데모v3 대응 ──
    /// - DrawFromDeck: 덱 위치에서 손패 위치로 날아옴 + 페이드인
    /// - ArrangeInHand: 지정된 위치/회전으로 이동 (부채꼴 배치)
    /// - HoverEnter/Exit: 제자리에서 살짝(15px) 위로 / 원위치
    /// - Select/Deselect: 20px 위로 + 스케일 1.05 / 원위치
    /// - DiscardToBottom: 아래로 떨어지며 페이드아웃
    /// - PlayToField: 위로 떠오르는 제출 연출
    /// 
    /// ── 설계 원칙 ──
    /// - CardUIComponent의 데이터/클릭 로직을 건드리지 않는다.
    /// - UITweenHelper를 사용하여 실제 트윈 수행.
    /// - 동시에 여러 트윈이 충돌하지 않도록 현재 실행 중인 코루틴을 추적.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CardAnimator : MonoBehaviour
    {
        #region === Inspector ===

        [Header("=== 애니메이션 설정 ===")]
        [Tooltip("드로우 시 이동 시간")]
        [SerializeField] private float _drawDuration = 0.45f;

        [Tooltip("손패 정렬 시 이동 시간")]
        [SerializeField] private float _arrangeDuration = 0.25f;

        [Tooltip("호버 시 상승 거리 (px)")]
        [SerializeField] private float _hoverRiseAmount = 15f;

        [Tooltip("선택 시 상승 거리 (px)")]
        [SerializeField] private float _selectRiseAmount = 20f;

        [Tooltip("선택 시 스케일")]
        [SerializeField] private float _selectScale = 1.05f;

        [Tooltip("폐기 시 낙하 거리 (px)")]
        [SerializeField] private float _discardDropAmount = 80f;

        [Tooltip("폐기 시 소요 시간")]
        [SerializeField] private float _discardDuration = 0.35f;

        [Header("=== 선택 시 테두리 색상 ===")]
        [Tooltip("선택 시 카드 테두리에 적용할 색상 (금색)")]
        [SerializeField] private Color _selectedBorderColor = new Color(1f, 0.84f, 0f, 1f);

        [Tooltip("카드 테두리 Image (Outline 또는 Border Image)")]
        [SerializeField] private Image _borderImage;

        #endregion

        #region === 내부 상태 ===

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;

        /// <summary>손패에서의 기준 위치 (anchoredPosition).</summary>
        private Vector2 _handPosition;

        /// <summary>손패에서의 기준 회전 (Z축 각도).</summary>
        private float _handRotation;

        /// <summary>기본 테두리 색상 (선택 해제 시 복귀용).</summary>
        private Color _defaultBorderColor;

        /// <summary>현재 실행 중인 주 트윈 코루틴. 충돌 방지용.</summary>
        private Coroutine _currentTween;

        /// <summary>현재 선택 상태인지.</summary>
        private bool _isSelected;

        /// <summary>현재 호버 상태인지.</summary>
        private bool _isHovered;

        #endregion

        #region === 초기화 ===

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            // CanvasGroup이 없으면 추가 (페이드 연출용)
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // 기본 테두리 색상 저장
            if (_borderImage != null)
                _defaultBorderColor = _borderImage.color;
        }

        #endregion

        #region === 외부 호출 API ===

        /// <summary>
        /// 덱 위치에서 손패의 목표 위치로 날아오는 드로우 애니메이션.
        /// 데모v3: 덱에서 출발 → OutBack 이징으로 손패 위치 도착, 페이드인 동반.
        /// 
        /// 호출자: BattleAnimationController
        /// "이 카드를 덱에서 손패로 드로우해" → 알아서 날아옴.
        /// </summary>
        /// <param name="deckWorldPos">덱의 월드 위치 (출발점 계산용)</param>
        /// <param name="targetLocalPos">손패에서의 목표 anchoredPosition</param>
        /// <param name="targetRotation">목표 Z축 회전 (부채꼴)</param>
        /// <param name="delay">다른 카드와의 시차 (0번 카드: 0초, 1번: 0.1초, ...)</param>
        /// <param name="onComplete">애니메이션 완료 콜백</param>
        public void PlayDrawFromDeck(Vector3 deckWorldPos, Vector2 targetLocalPos,
            float targetRotation, float delay = 0f, Action onComplete = null)
        {
            _handPosition = targetLocalPos;
            _handRotation = targetRotation;

            StopCurrentTween();
            _currentTween = StartCoroutine(DrawSequence(deckWorldPos, targetLocalPos, targetRotation, delay, onComplete));
        }

        /// <summary>
        /// 손패 내에서 위치를 재정렬한다.
        /// 데모v3: 리롤 후 남은 카드들이 새 위치로 슬라이드.
        /// </summary>
        public void PlayArrangeToPosition(Vector2 targetLocalPos, float targetRotation, Action onComplete = null)
        {
            _handPosition = targetLocalPos;
            _handRotation = targetRotation;

            StopCurrentTween();
            _currentTween = StartCoroutine(ArrangeSequence(targetLocalPos, targetRotation, onComplete));
        }

        /// <summary>
        /// 마우스 호버 진입. 제자리에서 살짝 위로 올라옴.
        /// 데모v3: translateY(-15px), 스케일 1.08, 기울기 절반으로.
        /// </summary>
        public void PlayHoverEnter()
        {
            if (_isSelected) return; // 선택 상태면 호버 무시
            _isHovered = true;

            StopCurrentTween();
            Vector2 hoverPos = _handPosition + Vector2.up * _hoverRiseAmount;
            _currentTween = StartCoroutine(HoverSequence(hoverPos, 1.08f, _handRotation * 0.5f));
        }

        /// <summary>
        /// 마우스 호버 해제. 원래 위치로 복귀.
        /// </summary>
        public void PlayHoverExit()
        {
            if (_isSelected) return;
            _isHovered = false;

            StopCurrentTween();
            _currentTween = StartCoroutine(HoverSequence(_handPosition, 1f, _handRotation));
        }

        /// <summary>
        /// 카드 선택. 위로 올라가며 금테 + 스케일 변화.
        /// 데모v3: translateY(-20px), 스케일 1.05, 금색 보더, 기울기 0도.
        /// </summary>
        public void PlaySelect()
        {
            _isSelected = true;
            _isHovered = false;

            if (_borderImage != null)
                _borderImage.color = _selectedBorderColor;

            StopCurrentTween();
            Vector2 selectPos = _handPosition + Vector2.up * _selectRiseAmount;
            _currentTween = StartCoroutine(HoverSequence(selectPos, _selectScale, 0f));
        }

        /// <summary>
        /// 카드 선택 해제. 원래 위치로 복귀.
        /// </summary>
        public void PlayDeselect()
        {
            _isSelected = false;

            if (_borderImage != null)
                _borderImage.color = _defaultBorderColor;

            StopCurrentTween();
            _currentTween = StartCoroutine(HoverSequence(_handPosition, 1f, _handRotation));
        }

        /// <summary>
        /// 카드를 아래로 떨어뜨리며 사라지게 한다.
        /// 데모v3: translateY(+80px) + 랜덤 회전 + 페이드아웃 + 스케일 축소.
        /// 리롤/턴종료 시 카드 폐기에 사용.
        /// </summary>
        /// <param name="onComplete">완료 콜백 (Destroy 등)</param>
        public void PlayDiscardToBottom(Action onComplete = null)
        {
            StopCurrentTween();
            _currentTween = StartCoroutine(DiscardSequence(onComplete));
        }

        /// <summary>
        /// 카드를 위로 띄워 제출 연출을 한다.
        /// 데모v3: translateY(-50px), 스케일 1.05.
        /// Turn Start 시 선택된 2장에 적용.
        /// </summary>
        public void PlaySubmitToField(Action onComplete = null)
        {
            StopCurrentTween();
            Vector2 submitPos = _handPosition + Vector2.up * 50f;
            _currentTween = StartCoroutine(SubmitSequence(submitPos, onComplete));
        }

        /// <summary>
        /// 모든 진행 중인 애니메이션을 즉시 중단한다.
        /// </summary>
        public void StopAllAnimations()
        {
            StopCurrentTween();
        }

        #endregion

        #region === 내부 코루틴 시퀀스 ===

        private IEnumerator DrawSequence(Vector3 deckWorldPos, Vector2 targetPos,
            float targetRot, float delay, Action onComplete)
        {
            // 초기 상태: 덱 위치, 투명, 작게
            _canvasGroup.alpha = 0f;
            transform.localScale = Vector3.one * 0.5f;
            transform.localEulerAngles = new Vector3(0, 0, 15f);

            // 덱의 월드 좌표를 이 카드의 부모 로컬 좌표로 변환
            if (transform.parent != null)
            {
                Vector3 localDeckPos = transform.parent.InverseTransformPoint(deckWorldPos);
                _rectTransform.anchoredPosition = new Vector2(localDeckPos.x, localDeckPos.y);
            }

            // 딜레이 (순차 드로우)
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            // 이동 + 페이드인 동시 실행
            yield return UITweenHelper.MoveAndFadeIn(
                _rectTransform, _canvasGroup, targetPos,
                _drawDuration, UITweenHelper.EaseType.OutBack);

            // 회전 + 스케일 정상화
            StartCoroutine(UITweenHelper.RotateTo(transform, targetRot, 0.2f));
            yield return UITweenHelper.ScaleTo(transform, Vector3.one, 0.2f, UITweenHelper.EaseType.OutCubic);

            onComplete?.Invoke();
        }

        private IEnumerator ArrangeSequence(Vector2 targetPos, float targetRot, Action onComplete)
        {
            // 이동과 회전을 동시에
            StartCoroutine(UITweenHelper.RotateTo(transform, targetRot, _arrangeDuration));
            yield return UITweenHelper.MoveTo(
                _rectTransform, targetPos, _arrangeDuration, UITweenHelper.EaseType.OutBack);

            onComplete?.Invoke();
        }

        private IEnumerator HoverSequence(Vector2 targetPos, float targetScale, float targetRot)
        {
            StartCoroutine(UITweenHelper.RotateTo(transform, targetRot, 0.15f));
            StartCoroutine(UITweenHelper.ScaleTo(transform, Vector3.one * targetScale, 0.15f, UITweenHelper.EaseType.OutCubic));
            yield return UITweenHelper.MoveTo(
                _rectTransform, targetPos, 0.15f, UITweenHelper.EaseType.OutCubic);
        }

        private IEnumerator DiscardSequence(Action onComplete)
        {
            // 랜덤 회전 추가
            float randomRot = UnityEngine.Random.Range(-15f, 15f);
            Vector2 dropTarget = _rectTransform.anchoredPosition + Vector2.down * _discardDropAmount;

            StartCoroutine(UITweenHelper.RotateTo(transform, randomRot, _discardDuration));
            StartCoroutine(UITweenHelper.ScaleTo(transform, Vector3.one * 0.4f, _discardDuration, UITweenHelper.EaseType.InBack));
            yield return UITweenHelper.MoveAndFadeOut(
                _rectTransform, _canvasGroup, dropTarget,
                _discardDuration, UITweenHelper.EaseType.InBack);

            onComplete?.Invoke();
        }

        private IEnumerator SubmitSequence(Vector2 targetPos, Action onComplete)
        {
            StartCoroutine(UITweenHelper.ScaleTo(transform, Vector3.one * 1.05f, 0.3f, UITweenHelper.EaseType.OutBack));
            StartCoroutine(UITweenHelper.RotateTo(transform, 0f, 0.3f));
            yield return UITweenHelper.MoveTo(
                _rectTransform, targetPos, 0.4f, UITweenHelper.EaseType.OutBack);

            onComplete?.Invoke();
        }

        #endregion

        #region === 유틸 ===

        private void StopCurrentTween()
        {
            if (_currentTween != null)
            {
                StopCoroutine(_currentTween);
                _currentTween = null;
            }
        }

        #endregion
    }
}
