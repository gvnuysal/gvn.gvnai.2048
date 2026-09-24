using System.Collections;
using System.Collections.Generic;
using Game2048.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game2048
{
    /// <summary>
    /// Shows a <see cref="BoardModel"/>: empty cells on a GridLayoutGroup, numbered tiles on a separate
    /// layer above it so they can slide. Animations are purely visual; the model is already resolved.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField] private RectTransform tileLayer;
        [SerializeField] private TileView tilePrefab;
        [SerializeField, Range(0f, 0.1f)] private float spacingRatio = 0.03f;
        [SerializeField] private float slideDuration = 0.1f;
        [SerializeField] private float popDuration = 0.14f;
        [SerializeField] private float popScale = 1.15f;

        private readonly Stack<TileView> _pool = new Stack<TileView>();
        private readonly List<TileView> _detached = new List<TileView>(); // merged-away tiles still on screen
        private BoardModel _model;
        private TileView[,] _tiles;
        private Coroutine _animation;
        private Vector2 _lastSize;
        private float _cellSize;
        private float _spacing;

        public bool IsAnimating => _animation != null;

        private RectTransform Rect => (RectTransform)transform;

        public void Bind(BoardModel model)
        {
            _model = model;
            _tiles = new TileView[model.Size, model.Size];
            Relayout();
            Render();
        }

        /// <summary>Snaps any running animation to the current model state.</summary>
        public void FinishAnimations()
        {
            if (_animation == null) return;
            StopCoroutine(_animation);
            _animation = null;
            Render();
        }

        /// <summary>Rebuilds every tile from the model without animation.</summary>
        public void Render()
        {
            if (_model == null) return;
            foreach (var tile in _detached) Release(tile);
            _detached.Clear();

            for (int r = 0; r < _model.Size; r++)
            for (int c = 0; c < _model.Size; c++)
            {
                if (_tiles[r, c] != null) Release(_tiles[r, c]);
                _tiles[r, c] = null;
                int value = _model[r, c];
                if (value != 0) _tiles[r, c] = Acquire(new Cell(r, c), value);
            }
        }

        /// <summary>Animates a resolved turn: slide, then merge pop and spawn scale-in.</summary>
        public void Animate(TurnResult turn)
        {
            FinishAnimations();
            if (!isActiveAndEnabled)
            {
                Render();
                return;
            }
            _animation = StartCoroutine(AnimateTurn(turn));
        }

        private IEnumerator AnimateTurn(TurnResult turn)
        {
            int size = _model.Size;
            var next = new TileView[size, size];
            var sliding = new List<(TileView tile, Vector2 from, Vector2 to)>();

            foreach (var move in turn.Move.Moves)
            {
                if (_tiles[move.From.Row, move.From.Col] == null)
                {
                    // View got out of sync with the model; just redraw.
                    _animation = null;
                    Render();
                    yield break;
                }
            }

            foreach (var move in turn.Move.Moves)
            {
                var tile = _tiles[move.From.Row, move.From.Col];
                sliding.Add((tile, CellCenter(move.From), CellCenter(move.To)));
                if (move.Merged) _detached.Add(tile);
                else next[move.To.Row, move.To.Col] = tile;
            }
            _tiles = next;

            for (float t = 0f; t < slideDuration; t += Time.unscaledDeltaTime)
            {
                float k = EaseOutCubic(t / slideDuration);
                foreach (var s in sliding) s.tile.Rect.anchoredPosition = Vector2.LerpUnclamped(s.from, s.to, k);
                yield return null;
            }
            foreach (var s in sliding) s.tile.Rect.anchoredPosition = s.to;

            foreach (var tile in _detached) Release(tile);
            _detached.Clear();

            var popping = new List<TileView>();
            foreach (var merge in turn.Move.Merges)
            {
                var tile = Acquire(merge.Cell, merge.Value);
                _tiles[merge.Cell.Row, merge.Cell.Col] = tile;
                popping.Add(tile);
            }

            TileView spawned = null;
            if (turn.Spawned.HasValue)
            {
                var spawn = turn.Spawned.Value;
                spawned = Acquire(spawn.Cell, spawn.Value);
                _tiles[spawn.Cell.Row, spawn.Cell.Col] = spawned;
                spawned.Rect.localScale = Vector3.zero;
            }

            for (float t = 0f; t < popDuration; t += Time.unscaledDeltaTime)
            {
                float k = t / popDuration;
                float pop = 1f + (popScale - 1f) * Mathf.Sin(k * Mathf.PI);
                foreach (var tile in popping) tile.Rect.localScale = new Vector3(pop, pop, 1f);
                if (spawned != null) spawned.Rect.localScale = Vector3.one * EaseOutBack(k);
                yield return null;
            }

            foreach (var tile in popping) tile.Rect.localScale = Vector3.one;
            if (spawned != null) spawned.Rect.localScale = Vector3.one;
            _animation = null;
        }

        private void LateUpdate()
        {
            if (Rect.rect.size != _lastSize) Relayout();
        }

        private void Relayout()
        {
            if (_model == null) return;

            _lastSize = Rect.rect.size;
            int n = _model.Size;
            float side = Mathf.Min(_lastSize.x, _lastSize.y);
            _spacing = Mathf.Round(side * spacingRatio);
            _cellSize = (side - _spacing * (n + 1)) / n;

            int pad = (int)_spacing;
            grid.padding = new RectOffset(pad, pad, pad, pad);
            grid.spacing = new Vector2(_spacing, _spacing);
            grid.cellSize = new Vector2(_cellSize, _cellSize);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = n;

            if (_animation != null) FinishAnimations();
            else if (_tiles != null)
            {
                for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    if (_tiles[r, c] != null) _tiles[r, c].Place(CellCenter(new Cell(r, c)), _cellSize);
            }
        }

        /// <summary>Tile centre in tile-layer space (anchor top-left, y down is negative).</summary>
        private Vector2 CellCenter(Cell cell)
        {
            float step = _cellSize + _spacing;
            return new Vector2(
                _spacing + cell.Col * step + _cellSize * 0.5f,
                -(_spacing + cell.Row * step + _cellSize * 0.5f));
        }

        private TileView Acquire(Cell cell, int value)
        {
            var tile = _pool.Count > 0 ? _pool.Pop() : Instantiate(tilePrefab, tileLayer);
            tile.gameObject.SetActive(true);
            tile.Rect.anchorMin = tile.Rect.anchorMax = new Vector2(0f, 1f);
            tile.Rect.pivot = new Vector2(0.5f, 0.5f);
            tile.Rect.localScale = Vector3.one;
            tile.Rect.SetAsLastSibling();
            tile.Place(CellCenter(cell), _cellSize);
            tile.SetValue(value);
            return tile;
        }

        private void Release(TileView tile)
        {
            tile.gameObject.SetActive(false);
            _pool.Push(tile);
        }

        private static float EaseOutCubic(float x)
        {
            float inv = 1f - Mathf.Clamp01(x);
            return 1f - inv * inv * inv;
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = Mathf.Clamp01(x) - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }
    }
}
