using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game2048
{
    /// <summary>A single numbered tile. Lives on Tile.prefab.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class TileView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;

        public int Value { get; private set; }

        public RectTransform Rect => (RectTransform)transform;

        public void SetValue(int value)
        {
            Value = value;
            label.text = value.ToString();
            background.color = TileColors.TileBackground(value);
            label.color = TileColors.TileText(value);
        }

        public void Place(Vector2 position, float size)
        {
            Rect.anchoredPosition = position;
            Rect.sizeDelta = new Vector2(size, size);
        }
    }
}
