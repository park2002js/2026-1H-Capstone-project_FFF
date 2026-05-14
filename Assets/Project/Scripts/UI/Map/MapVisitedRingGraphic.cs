using UnityEngine;
using UnityEngine.UI;

namespace FFF.UI.Map
{
    public class MapVisitedRingGraphic : MaskableGraphic
    {
        [SerializeField, Min(1f)] private float _thickness = 5f;
        [SerializeField, Range(12, 96)] private int _segments = 48;

        public float Thickness
        {
            get => _thickness;
            set
            {
                _thickness = Mathf.Max(1f, value);
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform.rect;
            float outerRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
            float innerRadius = Mathf.Max(0f, outerRadius - _thickness);

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            for (int i = 0; i < _segments; i++)
            {
                float angle = Mathf.PI * 2f * i / _segments;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                vertex.position = direction * outerRadius;
                vh.AddVert(vertex);

                vertex.position = direction * innerRadius;
                vh.AddVert(vertex);
            }

            for (int i = 0; i < _segments; i++)
            {
                int next = (i + 1) % _segments;
                int outerCurrent = i * 2;
                int innerCurrent = outerCurrent + 1;
                int outerNext = next * 2;
                int innerNext = outerNext + 1;

                vh.AddTriangle(outerCurrent, outerNext, innerNext);
                vh.AddTriangle(innerNext, innerCurrent, outerCurrent);
            }
        }
    }
}
