using UnityEngine;

namespace Game2048
{
    /// <summary>
    /// Keeps this RectTransform a square as large as fits in its parent, pinned to the parent's top edge,
    /// so the board stays readable on any screen or window size.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SquareFitter : MonoBehaviour
    {
        private Vector2 _parentSize = new Vector2(-1f, -1f);

        private void OnEnable() => Fit();

        private void LateUpdate()
        {
            var parent = transform.parent as RectTransform;
            if (parent != null && parent.rect.size != _parentSize) Fit();
        }

        private void Fit()
        {
            var parent = transform.parent as RectTransform;
            if (parent == null) return;

            _parentSize = parent.rect.size;
            float side = Mathf.Max(0f, Mathf.Min(_parentSize.x, _parentSize.y));

            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(side, side);
        }
    }
}
