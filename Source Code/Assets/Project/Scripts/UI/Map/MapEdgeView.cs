using UnityEngine;

namespace FFF.UI.Map
{
    /// <summary>
    /// 두 노드를 잇는 연결선 하나를 표현하는 UI 컴포넌트.
    /// MapUIComponent.SpawnEdgeView()에서 코드로 생성된다 — 프리팹 불필요.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MapEdgeView : MonoBehaviour
    {
        private RectTransform _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        public void Setup(Vector2 from, Vector2 to)
        {
            Vector2 dir = to - from;
            float dist = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            _rect.anchoredPosition = (from + to) * 0.5f;
            _rect.sizeDelta = new Vector2(dist, _rect.sizeDelta.y);
            _rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
