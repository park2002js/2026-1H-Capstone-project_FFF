using System.Collections;
using TMPro;
using UnityEngine;

namespace FFF.UI.Battle
{
    /// <summary>
    /// 화면 중앙에 잠깐 표시되는 상태 메시지 UI.
    /// 데모v3의 .status-bar에 대응.
    /// 예) "카드를 2장 선택하세요", "청단패 (18점)! Turn Start로 제출"
    /// </summary>
    public class StatusBarUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Tooltip("메시지 표시 지속 시간 (초)")]
        [SerializeField] private float _displayDuration = 2.5f;

        [Tooltip("페이드 인/아웃 시간 (초)")]
        [SerializeField] private float _fadeDuration = 0.3f;

        private Coroutine _current;

        private void Awake()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            _canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// 메시지를 표시한다. 이전 메시지가 있으면 즉시 교체한다.
        /// </summary>
        public void Show(string message)
        {
            if (_current != null)
                StopCoroutine(_current);

            _messageText.text = message;
            _current = StartCoroutine(ShowSequence());
        }

        private IEnumerator ShowSequence()
        {
            // 페이드 인
            yield return Fade(0f, 1f, _fadeDuration);

            // 표시 유지
            yield return new WaitForSeconds(_displayDuration);

            // 페이드 아웃
            yield return Fade(1f, 0f, _fadeDuration);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            _canvasGroup.alpha = to;
        }
    }
}
