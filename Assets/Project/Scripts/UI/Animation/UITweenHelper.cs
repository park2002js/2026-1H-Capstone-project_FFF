using System;
using System.Collections;
using UnityEngine;

namespace FFF.UI.Animation
{
    /// <summary>
    /// 코루틴 기반 UI 트윈 유틸리티.
    /// 
    /// ── 핵심 책임 ──
    /// "이 오브젝트를 여기로 옮겨줘" → 알아서 부드럽게 이동.
    /// "이 오브젝트를 흔들어줘" → 알아서 흔들림 연출.
    /// 호출자(CardAnimator 등)는 내부 보간 로직을 알 필요 없다.
    /// 
    /// ── 사용법 ──
    /// StartCoroutine(UITweenHelper.MoveTo(transform, targetPos, 0.4f, EaseType.OutBack));
    /// StartCoroutine(UITweenHelper.ScaleTo(transform, Vector3.one * 1.1f, 0.2f));
    /// StartCoroutine(UITweenHelper.Shake(transform, 0.3f, 5f));
    /// 
    /// ── 설계 원칙 ──
    /// - 외부 패키지 의존성 없음. Unity 코루틴만 사용.
    /// - static 메서드로 제공. MonoBehaviour 상속 불필요.
    /// - 각 메서드는 IEnumerator를 반환하므로 StartCoroutine()으로 호출.
    /// </summary>
    public static class UITweenHelper
    {
        #region === 이징 타입 ===

        /// <summary>
        /// 지원하는 이징 함수 목록.
        /// </summary>
        public enum EaseType
        {
            Linear,
            InQuad,
            OutQuad,
            InOutQuad,
            OutCubic,
            OutBack,    // 데모v3의 카드 이동에 사용 (살짝 튀는 느낌)
            InBack
        }

        #endregion

        #region === 위치 이동 ===

        /// <summary>
        /// RectTransform의 anchoredPosition을 목표 위치로 트윈한다.
        /// 데모v3: 카드 드로우(덱→손패), 카드 제출(손패→필드), 리롤(사라짐) 등에 사용.
        /// </summary>
        /// <param name="target">이동할 RectTransform</param>
        /// <param name="to">목표 anchoredPosition</param>
        /// <param name="duration">소요 시간(초)</param>
        /// <param name="ease">이징 타입</param>
        /// <param name="onComplete">완료 시 콜백 (null 가능)</param>
        public static IEnumerator MoveTo(RectTransform target, Vector2 to, float duration,
            EaseType ease = EaseType.OutCubic, Action onComplete = null)
        {
            Vector2 from = target.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = ApplyEase(t, ease);

                target.anchoredPosition = Vector2.LerpUnclamped(from, to, easedT);
                yield return null;
            }

            target.anchoredPosition = to;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Transform의 localPosition을 목표 위치로 트윈한다.
        /// RectTransform이 아닌 일반 Transform용.
        /// </summary>
        public static IEnumerator MoveLocalTo(Transform target, Vector3 to, float duration,
            EaseType ease = EaseType.OutCubic, Action onComplete = null)
        {
            Vector3 from = target.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = ApplyEase(t, ease);

                target.localPosition = Vector3.LerpUnclamped(from, to, easedT);
                yield return null;
            }

            target.localPosition = to;
            onComplete?.Invoke();
        }

        #endregion

        #region === 스케일 ===

        /// <summary>
        /// localScale을 목표 크기로 트윈한다.
        /// 데모v3: 카드 호버(1.08배), 선택(1.05배), 드로우 시 작은 상태에서 커지는 연출.
        /// </summary>
        public static IEnumerator ScaleTo(Transform target, Vector3 to, float duration,
            EaseType ease = EaseType.OutBack, Action onComplete = null)
        {
            Vector3 from = target.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = ApplyEase(t, ease);

                target.localScale = Vector3.LerpUnclamped(from, to, easedT);
                yield return null;
            }

            target.localScale = to;
            onComplete?.Invoke();
        }

        #endregion

        #region === 회전 ===

        /// <summary>
        /// localRotation의 Z축을 목표 각도로 트윈한다.
        /// 데모v3: 카드 부채꼴 배치 시 양끝 카드의 ±4도 기울기.
        /// </summary>
        public static IEnumerator RotateTo(Transform target, float toAngleZ, float duration,
            EaseType ease = EaseType.OutCubic, Action onComplete = null)
        {
            float fromAngle = target.localEulerAngles.z;
            // 각도를 -180~180 범위로 정규화
            if (fromAngle > 180f) fromAngle -= 360f;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = ApplyEase(t, ease);

                float angle = Mathf.LerpUnclamped(fromAngle, toAngleZ, easedT);
                target.localEulerAngles = new Vector3(0, 0, angle);
                yield return null;
            }

            target.localEulerAngles = new Vector3(0, 0, toAngleZ);
            onComplete?.Invoke();
        }

        #endregion

        #region === 페이드 (CanvasGroup) ===

        /// <summary>
        /// CanvasGroup의 alpha를 목표 값으로 트윈한다.
        /// 데모v3: 카드 드로우 시 0→1 페이드인, 폐기 시 1→0 페이드아웃, 씬 전환 페이드.
        /// </summary>
        public static IEnumerator FadeTo(CanvasGroup target, float to, float duration,
            EaseType ease = EaseType.Linear, Action onComplete = null)
        {
            float from = target.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = ApplyEase(t, ease);

                target.alpha = Mathf.LerpUnclamped(from, to, easedT);
                yield return null;
            }

            target.alpha = to;
            onComplete?.Invoke();
        }

        #endregion

        #region === 흔들림 ===

        /// <summary>
        /// 위치를 랜덤하게 흔든다. 원래 위치로 자동 복귀.
        /// 데모v3: 적 피격 시 좌우 흔들림, 화면 흔들림(screen shake).
        /// </summary>
        /// <param name="target">흔들 Transform</param>
        /// <param name="duration">흔들림 지속 시간</param>
        /// <param name="magnitude">흔들림 강도 (픽셀)</param>
        /// <param name="onComplete">완료 콜백</param>
        public static IEnumerator Shake(Transform target, float duration, float magnitude,
            Action onComplete = null)
        {
            Vector3 originalPos = target.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float dampening = 1f - (elapsed / duration); // 시간에 따라 감쇠

                float offsetX = UnityEngine.Random.Range(-1f, 1f) * magnitude * dampening;
                float offsetY = UnityEngine.Random.Range(-1f, 1f) * magnitude * dampening * 0.5f;

                target.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
                yield return null;
            }

            target.localPosition = originalPos;
            onComplete?.Invoke();
        }

        /// <summary>
        /// RectTransform 전용 흔들림. anchoredPosition 기준.
        /// </summary>
        public static IEnumerator ShakeRect(RectTransform target, float duration, float magnitude,
            Action onComplete = null)
        {
            Vector2 originalPos = target.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float dampening = 1f - (elapsed / duration);

                float offsetX = UnityEngine.Random.Range(-1f, 1f) * magnitude * dampening;
                float offsetY = UnityEngine.Random.Range(-1f, 1f) * magnitude * dampening * 0.5f;

                target.anchoredPosition = originalPos + new Vector2(offsetX, offsetY);
                yield return null;
            }

            target.anchoredPosition = originalPos;
            onComplete?.Invoke();
        }

        #endregion

        #region === 복합: 이동 + 페이드 + 스케일 동시 ===

        /// <summary>
        /// 이동 + 페이드인을 동시에 수행한다.
        /// 데모v3: 카드 드로우 시 덱 위치에서 날아오면서 서서히 나타나는 연출.
        /// </summary>
        public static IEnumerator MoveAndFadeIn(RectTransform target, CanvasGroup cg,
            Vector2 to, float duration, EaseType ease = EaseType.OutBack, Action onComplete = null)
        {
            Vector2 from = target.anchoredPosition;
            float elapsed = 0f;
            cg.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = ApplyEase(t, ease);

                target.anchoredPosition = Vector2.LerpUnclamped(from, to, easedT);
                // 페이드는 처음 40% 구간에서 완료
                cg.alpha = Mathf.Clamp01(t / 0.4f);
                yield return null;
            }

            target.anchoredPosition = to;
            cg.alpha = 1f;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 이동 + 페이드아웃을 동시에 수행한다.
        /// 데모v3: 카드 폐기 시 아래로 떨어지면서 사라지는 연출.
        /// </summary>
        public static IEnumerator MoveAndFadeOut(RectTransform target, CanvasGroup cg,
            Vector2 to, float duration, EaseType ease = EaseType.InBack, Action onComplete = null)
        {
            Vector2 from = target.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = ApplyEase(t, ease);

                target.anchoredPosition = Vector2.LerpUnclamped(from, to, easedT);
                // 페이드는 후반 60%에서 진행
                cg.alpha = 1f - Mathf.Clamp01((t - 0.4f) / 0.6f);
                yield return null;
            }

            target.anchoredPosition = to;
            cg.alpha = 0f;
            onComplete?.Invoke();
        }

        #endregion

        #region === 지연 ===

        /// <summary>
        /// 지정한 시간만큼 대기한다. 시퀀스 연출에서 딜레이용.
        /// </summary>
        public static IEnumerator Delay(float seconds, Action onComplete = null)
        {
            yield return new WaitForSeconds(seconds);
            onComplete?.Invoke();
        }

        #endregion

        #region === 이징 함수 ===

        /// <summary>
        /// t(0~1)에 이징 커브를 적용하여 반환한다.
        /// </summary>
        private static float ApplyEase(float t, EaseType ease)
        {
            switch (ease)
            {
                case EaseType.Linear:
                    return t;

                case EaseType.InQuad:
                    return t * t;

                case EaseType.OutQuad:
                    return t * (2f - t);

                case EaseType.InOutQuad:
                    return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

                case EaseType.OutCubic:
                    float f = t - 1f;
                    return f * f * f + 1f;

                case EaseType.OutBack:
                    // 데모v3의 cubic-bezier(0.34, 1.56, 0.64, 1) 느낌 재현
                    const float s = 1.70158f;
                    float t1 = t - 1f;
                    return t1 * t1 * ((s + 1f) * t1 + s) + 1f;

                case EaseType.InBack:
                    const float s2 = 1.70158f;
                    return t * t * ((s2 + 1f) * t - s2);

                default:
                    return t;
            }
        }

        #endregion
    }
}
