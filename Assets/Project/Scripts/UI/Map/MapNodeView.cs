using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FFF.Map;
using FFF.Audio;

namespace FFF.UI.Map
{
    /// <summary>
    /// 맵 위의 개별 노드(방) 하나를 표현하는 UI 컴포넌트.
    /// MapUIComponent.SpawnNodeView()에서 코드로 생성된다 — 프리팹 불필요.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MapNodeView : MonoBehaviour
    {
        public RectTransform RectTransform { get; private set; }

        private Button _button;
        private Image _iconImage;
        private GameObject _visitedMarker;
        private MapVisitedRingGraphic _visitedRing;
        private MapNode _node;
        private Action<MapNode> _onClick;
        private Coroutine _pulseRoutine;
        private Coroutine _clickRoutine;

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            _button = GetComponent<Button>();
            _iconImage = transform.Find("Icon")?.GetComponent<Image>();
            _visitedMarker = transform.Find("VisitedMarker")?.gameObject;
            _visitedRing = GetComponentInChildren<MapVisitedRingGraphic>(includeInactive: true);
            _button.onClick.AddListener(HandleClick);
        }

        public void Setup(MapNode node, Action<MapNode> onClick)
        {
            _node = node;
            _onClick = onClick;
        }

        public void SetIcon(Sprite sprite)
        {
            if (_iconImage != null) _iconImage.sprite = sprite;
        }

        public void SetColor(Color color)
        {
            var bg = GetComponent<Image>();
            if (bg != null) bg.color = color;
        }

        public void SetInteractable(bool interactable)
        {
            _button.interactable = interactable;
        }

        public void SetState(bool isReachable, bool isVisited)
        {
            SetInteractable(isReachable && !isVisited);

            if (_iconImage != null)
            {
                Color color = _iconImage.color;
                color.a = 1f;
                _iconImage.color = color;
            }

            if (_visitedMarker != null)
                _visitedMarker.SetActive(isVisited);
            else if (_visitedRing != null)
                _visitedRing.gameObject.SetActive(isVisited);

            if (isReachable && !isVisited)
                StartReachablePulse();
            else
                StopReachablePulse();

            if (isVisited)
                StartCoroutine(VisitedPop());
        }

        private void HandleClick()
        {
            if (_clickRoutine != null)
                return;

            SoundManager.PlaySfxSound(SoundIds.SfxMapNodeSelect);
            _button.interactable = false;
            _clickRoutine = StartCoroutine(ClickThenInvoke());
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        private void StartReachablePulse()
        {
            if (_pulseRoutine != null)
                return;

            _pulseRoutine = StartCoroutine(ReachablePulse());
        }

        private void StopReachablePulse()
        {
            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
                _pulseRoutine = null;
            }

            transform.localScale = Vector3.one;
        }

        private IEnumerator ReachablePulse()
        {
            while (true)
            {
                yield return ScaleTo(Vector3.one * 1.08f, 0.45f);
                yield return ScaleTo(Vector3.one, 0.45f);
                yield return new WaitForSeconds(0.35f);
            }
        }

        private IEnumerator ClickThenInvoke()
        {
            StopReachablePulse();
            yield return ScaleTo(Vector3.one * 1.18f, 0.08f);
            yield return ScaleTo(Vector3.one * 0.92f, 0.07f);
            yield return ScaleTo(Vector3.one, 0.06f);

            _onClick?.Invoke(_node);
            _clickRoutine = null;
        }

        private IEnumerator VisitedPop()
        {
            Transform marker = _visitedMarker != null ? _visitedMarker.transform :
                _visitedRing != null ? _visitedRing.transform : null;
            if (marker == null)
                yield break;

            marker.localScale = Vector3.one * 0.7f;
            yield return ScaleTo(marker, Vector3.one * 1.12f, 0.12f);
            yield return ScaleTo(marker, Vector3.one, 0.12f);
        }

        private IEnumerator ScaleTo(Vector3 to, float duration)
        {
            yield return ScaleTo(transform, to, duration);
        }

        private IEnumerator ScaleTo(Transform target, Vector3 to, float duration)
        {
            Vector3 from = target.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                target.localScale = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }

            target.localScale = to;
        }
    }

}
