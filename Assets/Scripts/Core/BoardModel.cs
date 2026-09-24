using System;
using System.Collections.Generic;
using System.Text;

namespace Game2048.Core
{
    /// <summary>
    /// Pure 2048 board state and sliding/merging rules. No Unity dependency.
    /// Row 0 is the top row, column 0 is the left column.
    /// </summary>
    public sealed class BoardModel
    {
        public const int DefaultSize = 4;

        private readonly int[,] _cells;

        public int Size { get; }

        public BoardModel(int size = DefaultSize)
        {
            if (size < 2) throw new ArgumentOutOfRangeException(nameof(size));
            Size = size;
            _cells = new int[size, size];
        }

        public int this[int row, int col]
        {
            get => _cells[row, col];
            set => _cells[row, col] = value;
        }

        public static BoardModel FromArray(int[,] values)
        {
            int size = values.GetLength(0);
            if (values.GetLength(1) != size) throw new ArgumentException("Board must be square.", nameof(values));
            var board = new BoardModel(size);
            board.LoadFrom(values);
            return board;
        }

        public void LoadFrom(int[,] values)
        {
            if (values.GetLength(0) != Size || values.GetLength(1) != Size)
                throw new ArgumentException("Size mismatch.", nameof(values));
            Array.Copy(values, _cells, values.Length);
        }

        public int[,] ToArray() => (int[,])_cells.Clone();

        public BoardModel Clone() => FromArray(_cells);

        public void Clear() => Array.Clear(_cells, 0, _cells.Length);

        public int MaxValue
        {
            get
            {
                int max = 0;
                foreach (int v in _cells)
                    if (v > max) max = v;
                return max;
            }
        }

        public List<Cell> EmptyCells()
        {
            var empty = new List<Cell>();
            for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
                if (_cells[r, c] == 0) empty.Add(new Cell(r, c));
            return empty;
        }

        /// <summary>True when there is an empty cell or two equal horizontal/vertical neighbours.</summary>
        public bool HasMoves()
        {
            for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
            {
                int v = _cells[r, c];
                if (v == 0) return true;
                if (c + 1 < Size && _cells[r, c + 1] == v) return true;
                if (r + 1 < Size && _cells[r + 1, c] == v) return true;
            }
            return false;
        }

        /// <summary>Places a 2 (90%) or 4 (10%) on a random empty cell. Returns null if the board is full.</summary>
        public SpawnedTile? SpawnRandom(IRandom random)
        {
            var empty = EmptyCells();
            if (empty.Count == 0) return null;
            var cell = empty[random.Next(empty.Count)];
            int value = random.NextDouble() < 0.9 ? 2 : 4;
            _cells[cell.Row, cell.Col] = value;
            return new SpawnedTile(cell, value);
        }

        /// <summary>
        /// Slides all tiles in <paramref name="direction"/>. The board is modified in place;
        /// the result reports whether anything changed. Spawning is the caller's job.
        /// </summary>
        public MoveResult Move(Direction direction)
        {
            var result = new MoveResult();
            var lineCells = new Cell[Size];
            var line = new int[Size];

            for (int k = 0; k < Size; k++)
            {
                for (int j = 0; j < Size; j++)
                {
                    lineCells[j] = LineCell(direction, k, j);
                    line[j] = _cells[lineCells[j].Row, lineCells[j].Col];
                }

                var slid = SlideLine(line);

                for (int j = 0; j < Size; j++)
                {
                    var target = lineCells[j];
                    int value = slid.Values[j];
                    _cells[target.Row, target.Col] = value;
                    if (slid.MergedAt[j])
                    {
                        result.Merges.Add(new MergedTile(target, value));
                        if (value > result.MaxMergedValue) result.MaxMergedValue = value;
                    }

                    int to = slid.Targets[j];
                    if (to >= 0)
                        result.Moves.Add(new TileMove(lineCells[j], lineCells[to], line[j], slid.MergedAt[to]));
                }

                result.ScoreGained += slid.ScoreGained;
                result.Changed |= slid.Changed;
            }

            return result;
        }

        /// <summary>
        /// Direction-independent rule: slides a line towards index 0.
        /// Removes zeros, merges equal neighbours once per move, pads with zeros.
        /// </summary>
        public static LineResult SlideLine(int[] line)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));

            int n = line.Length;
            var values = new int[n];
            var targets = new int[n];
            var mergedAt = new bool[n];
            int write = 0;
            int score = 0;
            bool lastCanMerge = false;

            for (int i = 0; i < n; i++)
            {
                targets[i] = -1;
                int v = line[i];
                if (v == 0) continue;

                if (lastCanMerge && values[write - 1] == v)
                {
                    values[write - 1] = v * 2;
                    score += v * 2;
                    mergedAt[write - 1] = true;
                    targets[i] = write - 1;
                    lastCanMerge = false;
                }
                else
                {
                    values[write] = v;
                    targets[i] = write;
                    write++;
                    lastCanMerge = true;
                }
            }

            bool changed = false;
            for (int i = 0; i < n; i++)
            {
                if (values[i] != line[i])
                {
                    changed = true;
                    break;
                }
            }

            return new LineResult(values, score, targets, mergedAt, changed);
        }

        /// <summary>
        /// Maps (line index, position in line) to a board cell, where position 0 is the leading edge
        /// of the move direction.
        /// </summary>
        private Cell LineCell(Direction direction, int lineIndex, int position)
        {
            int last = Size - 1;
            switch (direction)
            {
                case Direction.Left: return new Cell(lineIndex, position);
                case Direction.Right: return new Cell(lineIndex, last - position);
                case Direction.Up: return new Cell(position, lineIndex);
                case Direction.Down: return new Cell(last - position, lineIndex);
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int r = 0; r < Size; r++)
            {
                sb.Append('[');
                for (int c = 0; c < Size; c++)
                {
                    if (c > 0) sb.Append(", ");
                    sb.Append(_cells[r, c]);
                }
                sb.Append(']');
                if (r < Size - 1) sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
