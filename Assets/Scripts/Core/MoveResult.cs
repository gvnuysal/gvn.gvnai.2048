using System.Collections.Generic;

namespace Game2048.Core
{
    public readonly struct Cell
    {
        public readonly int Row;
        public readonly int Col;

        public Cell(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public override string ToString() => $"({Row},{Col})";
    }

    /// <summary>One tile travelling from <see cref="From"/> to <see cref="To"/> during a move.</summary>
    public readonly struct TileMove
    {
        public readonly Cell From;
        public readonly Cell To;

        /// <summary>Value of the tile before the move.</summary>
        public readonly int Value;

        /// <summary>True when this tile ends up merged into another tile at <see cref="To"/>.</summary>
        public readonly bool Merged;

        public TileMove(Cell from, Cell to, int value, bool merged)
        {
            From = from;
            To = to;
            Value = value;
            Merged = merged;
        }
    }

    /// <summary>A tile created by a merge: the cell and its new value.</summary>
    public readonly struct MergedTile
    {
        public readonly Cell Cell;
        public readonly int Value;

        public MergedTile(Cell cell, int value)
        {
            Cell = cell;
            Value = value;
        }
    }

    public readonly struct SpawnedTile
    {
        public readonly Cell Cell;
        public readonly int Value;

        public SpawnedTile(Cell cell, int value)
        {
            Cell = cell;
            Value = value;
        }
    }

    public sealed class MoveResult
    {
        public bool Changed { get; internal set; }
        public int ScoreGained { get; internal set; }
        public int MaxMergedValue { get; internal set; }

        /// <summary>Every non-empty tile of the board before the move and where it went (including tiles that stayed).</summary>
        public List<TileMove> Moves { get; } = new List<TileMove>();

        public List<MergedTile> Merges { get; } = new List<MergedTile>();
    }

    /// <summary>Result of sliding a single line towards index 0.</summary>
    public sealed class LineResult
    {
        public int[] Values { get; }
        public int ScoreGained { get; }

        /// <summary>For each source index: the target index, or -1 when the source cell was empty.</summary>
        public int[] Targets { get; }

        /// <summary>For each target index: true when a merge happened there.</summary>
        public bool[] MergedAt { get; }

        public bool Changed { get; }

        public LineResult(int[] values, int scoreGained, int[] targets, bool[] mergedAt, bool changed)
        {
            Values = values;
            ScoreGained = scoreGained;
            Targets = targets;
            MergedAt = mergedAt;
            Changed = changed;
        }
    }
}
