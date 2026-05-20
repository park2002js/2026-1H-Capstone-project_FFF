using System.Collections;
using UnityEngine;

namespace FFF.UI.Animation
{
    /// <summary>
    /// 캐릭터의 평상시(idle) / 공격(attack) 외형 GameObject를 토글한다.
    /// PlayAttack(duration)을 호출하면 attack 외형이 잠시 표시되고 자동으로 idle로 복귀.
    /// </summary>
    public class CharacterAttackVisual : MonoBehaviour
    {
        [Tooltip("평상시 외형 GameObject (idle 이미지 보유)")]
        [SerializeField] private GameObject _idleVisual;

        [Tooltip("공격 시 외형 GameObject (attack 이미지 보유)")]
        [SerializeField] private GameObject _attackVisual;

        private Coroutine _running;
        private Coroutine _idleBreathRoutine;
        private Vector3 _initialScale;

        [SerializeField] private bool _playIdleBreath = true;
        [SerializeField] private float _idleBreathScale = 1.025f;
        [SerializeField] private float _idleBreathDuration = 1.1f;

        private void Awake()
        {
            _initialScale = transform.localScale;
        }

        private void OnEnable()
        {
            Show(_idleVisual);
            Hide(_attackVisual);
            if (_playIdleBreath)
                _idleBreathRoutine = StartCoroutine(IdleBreathRoutine());
        }

        private void OnDisable()
        {
            if (_idleBreathRoutine != null)
            {
                StopCoroutine(_idleBreathRoutine);
                _idleBreathRoutine = null;
            }

            transform.localScale = _initialScale;
        }

        /// <summary>지정한 시간만큼 attack 외형을 표시한 뒤 idle로 복귀.</summary>
        public void PlayAttack(float duration)
        {
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(AttackRoutine(duration));
        }

        /// <summary>즉시 attack 외형으로 전환. (시퀀스 외부에서 직접 타이밍 제어 시 사용)</summary>
        public void SwitchToAttack()
        {
            if (_running != null) { StopCoroutine(_running); _running = null; }
            Hide(_idleVisual);
            Show(_attackVisual);
        }

        /// <summary>즉시 idle 외형으로 전환.</summary>
        public void SwitchToIdle()
        {
            if (_running != null) { StopCoroutine(_running); _running = null; }
            Show(_idleVisual);
            Hide(_attackVisual);
        }

        private IEnumerator AttackRoutine(float duration)
        {
            Hide(_idleVisual);
            Show(_attackVisual);

            yield return new WaitForSeconds(duration);

            Show(_idleVisual);
            Hide(_attackVisual);
            _running = null;
        }

        private void Show(GameObject go)
        {
            if (go != null && !go.activeSelf) go.SetActive(true);
        }

        private void Hide(GameObject go)
        {
            if (go != null && go.activeSelf) go.SetActive(false);
        }

        private IEnumerator IdleBreathRoutine()
        {
            while (true)
            {
                yield return ScaleRoot(_initialScale * _idleBreathScale, _idleBreathDuration);
                yield return ScaleRoot(_initialScale, _idleBreathDuration);
            }
        }

        private IEnumerator ScaleRoot(Vector3 targetScale, float duration)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 0.5f - Mathf.Cos(t * Mathf.PI) * 0.5f;
                transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
                yield return null;
            }

            transform.localScale = targetScale;
        }
    }
}
