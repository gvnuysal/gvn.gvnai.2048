using System;
using System.Collections.Generic;

namespace Game2048.Core
{
    public enum GameState
    {
        Playing,

        /// <summary>2048 was reached for the first time; input is blocked until Continue or Restart.</summary>
        Won,

        /// <summary>No moves left; input is blocked until Undo or Restart.</summary>
        Lost,

        /// <summary>Player chose to keep playing after winning.</summary>
        PlayingAfterWin
    }

    public interface IScoreStore
    {
        int LoadBest();
        void SaveBest(int best);
    }

    public sealed class InMemoryScoreStore : IScoreStore
    {
        private int _best;

        public InMemoryScoreStore(int best = 0)
        {
            _best = best;
        }

        public int LoadBest() => _best;

        public void SaveBest(int best) => _best = best;
    }

    /// <summary>Outcome of a valid move: the slide result and the tile spawned afterwards (if any).</summary>
    public sealed class TurnResult
    {
        public MoveResult Move { get; }
        public SpawnedTile? Spawned { get; }

        public TurnResult(MoveResult move, SpawnedTile? spawned)
        {
            Move = move;
            Spawned = spawned;
        }
    }

    /// <summary>
    /// Game rules and flow on top of <see cref="BoardModel"/>: new game, valid moves, spawning,
    /// score, best score, win/lose state, continue after win and one-step undo.
    /// </summary>
    public sealed class GameSession
    {
        public const int WinValue = 2048;
        public const int StartTiles = 2;

        private readonly IRandom _random;
        private readonly IScoreStore _scoreStore;
        private Snapshot _undo;

        public BoardModel Board { get; }
        public int Score { get; private set; }
        public int Best { get; private set; }
        public GameState State { get; private set; }

        /// <summary>True once 2048 has been reached in this game (survives Continue).</summary>
        public bool HasWon { get; private set; }

        public bool CanUndo => _undo != null;

        public bool AcceptsInput => State == GameState.Playing || State == GameState.PlayingAfterWin;

        public event Action Changed;

        public GameSession(IRandom random = null, IScoreStore scoreStore = null, int size = BoardModel.DefaultSize)
        {
            _random = random ?? new SystemRandom();
            _scoreStore = scoreStore ?? new InMemoryScoreStore();
            Board = new BoardModel(size);
            Best = _scoreStore.LoadBest();
        }

        /// <summary>Resets board, score, state and undo; spawns two start tiles.</summary>
        public List<SpawnedTile> NewGame()
        {
            Board.Clear();
            Score = 0;
            State = GameState.Playing;
            HasWon = false;
            _undo = null;

            var spawned = new List<SpawnedTile>(StartTiles);
            for (int i = 0; i < StartTiles; i++)
            {
                var tile = Board.SpawnRandom(_random);
                if (tile.HasValue) spawned.Add(tile.Value);
            }

            Changed?.Invoke();
            return spawned;
        }

        /// <summary>Loads a specific position (for tests/debug). Clears undo.</summary>
        public void Load(int[,] cells, int score = 0, GameState state = GameState.Playing, bool hasWon = false)
        {
            Board.LoadFrom(cells);
            Score = score;
            State = state;
            HasWon = hasWon;
            _undo = null;
            UpdateBest();
            Changed?.Invoke();
        }

        /// <summary>
        /// Applies a move. Returns null when input is not accepted or the move changes nothing
        /// (no score, no spawn in that case).
        /// </summary>
        public TurnResult TryMove(Direction direction)
        {
            if (!AcceptsInput) return null;

            var before = new Snapshot(Board.ToArray(), Score, State, HasWon);
            var move = Board.Move(direction);
            if (!move.Changed) return null;

            _undo = before;
            Score += move.ScoreGained;
            UpdateBest();

            var spawned = Board.SpawnRandom(_random);

            if (!HasWon && Board.MaxValue >= WinValue)
            {
                HasWon = true;
                State = GameState.Won;
            }
            else if (!Board.HasMoves())
            {
                State = GameState.Lost;
            }

            Changed?.Invoke();
            return new TurnResult(move, spawned);
        }

        /// <summary>Keep playing after the win screen.</summary>
        public bool Continue()
        {
            if (State != GameState.Won) return false;
            State = Board.HasMoves() ? GameState.PlayingAfterWin : GameState.Lost;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Restores the position before the last valid move. Best score is kept.</summary>
        public bool Undo()
        {
            if (_undo == null) return false;
            Board.LoadFrom(_undo.Cells);
            Score = _undo.Score;
            State = _undo.State;
            HasWon = _undo.HasWon;
            _undo = null;
            Changed?.Invoke();
            return true;
        }

        private void UpdateBest()
        {
            if (Score <= Best) return;
            Best = Score;
            _scoreStore.SaveBest(Best);
        }

        private sealed class Snapshot
        {
            public readonly int[,] Cells;
            public readonly int Score;
            public readonly GameState State;
            public readonly bool HasWon;

            public Snapshot(int[,] cells, int score, GameState state, bool hasWon)
            {
                Cells = cells;
                Score = score;
                State = state;
                HasWon = hasWon;
            }
        }
    }
}
