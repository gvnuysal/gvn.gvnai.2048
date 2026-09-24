using Game2048.Core;
using NUnit.Framework;

namespace Game2048.Tests
{
    public class GameSessionTests
    {
        private static int CountTiles(BoardModel board)
        {
            int count = 0;
            for (int r = 0; r < board.Size; r++)
            for (int c = 0; c < board.Size; c++)
                if (board[r, c] != 0) count++;
            return count;
        }

        private static int[,] Empty() => new int[4, 4];

        [Test]
        public void NewGame_SpawnsTwoTiles_ScoreZero_Playing()
        {
            var session = new GameSession(new SystemRandom(42));
            var spawned = session.NewGame();

            Assert.AreEqual(2, spawned.Count);
            Assert.AreEqual(2, CountTiles(session.Board));
            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(GameState.Playing, session.State);
            Assert.IsFalse(session.CanUndo);
        }

        [Test]
        public void NewGame_TileValuesAreTwoOrFour()
        {
            var session = new GameSession(new SystemRandom(7));
            for (int i = 0; i < 50; i++)
            {
                session.NewGame();
                foreach (int v in session.Board.ToArray())
                    Assert.That(v == 0 || v == 2 || v == 4);
            }
        }

        [Test]
        public void ValidMove_AddsScore_AndSpawnsOneTile()
        {
            var session = new GameSession(new TestRandom());
            var cells = Empty();
            cells[0, 0] = 2;
            cells[0, 2] = 2;
            session.Load(cells);

            var turn = session.TryMove(Direction.Left);

            Assert.IsNotNull(turn);
            Assert.AreEqual(4, session.Score);
            Assert.AreEqual(4, session.Board[0, 0]);
            Assert.IsTrue(turn.Spawned.HasValue);
            Assert.AreEqual(2, CountTiles(session.Board));
        }

        [Test]
        public void InvalidMove_NoScore_NoSpawn()
        {
            var random = new TestRandom();
            var session = new GameSession(random);
            var cells = Empty();
            cells[0, 0] = 2;
            session.Load(cells);

            var turn = session.TryMove(Direction.Left);

            Assert.IsNull(turn);
            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(1, CountTiles(session.Board));
            Assert.AreEqual(0, random.NextCalls);
            Assert.IsFalse(session.CanUndo);
        }

        [Test]
        public void FullBoard_WithEqualNeighbours_IsNotGameOver()
        {
            var session = new GameSession(new TestRandom());
            session.Load(new[,]
            {
                { 2, 4, 2, 4 },
                { 4, 2, 4, 2 },
                { 2, 4, 2, 4 },
                { 4, 2, 8, 8 }
            });

            Assert.AreEqual(GameState.Playing, session.State);
            Assert.IsNotNull(session.TryMove(Direction.Left));
        }

        [Test]
        public void MoveThatLeavesNoMoves_IsGameOver_AndBlocksInput()
        {
            // Left merges 2+2 in the last row, spawn (2) lands at [3,3] → no equal neighbours left.
            var session = new GameSession(new TestRandom());
            session.Load(new[,]
            {
                { 2, 4, 2, 4 },
                { 4, 2, 4, 2 },
                { 2, 4, 2, 4 },
                { 8, 16, 4, 4 }
            });

            var turn = session.TryMove(Direction.Left);
            Assert.IsNotNull(turn);
            CollectionAssert.AreEqual(new[,]
            {
                { 2, 4, 2, 4 },
                { 4, 2, 4, 2 },
                { 2, 4, 2, 4 },
                { 8, 16, 8, 2 }
            }, session.Board.ToArray());
            Assert.AreEqual(GameState.Lost, session.State);
            Assert.IsFalse(session.AcceptsInput);

            foreach (Direction d in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
                Assert.IsNull(session.TryMove(d));
        }

        [Test]
        public void Reaching2048_ShowsWin_ScoreCorrect_InputBlocked()
        {
            var session = new GameSession(new TestRandom());
            var cells = Empty();
            cells[0, 0] = 1024;
            cells[0, 1] = 1024;
            session.Load(cells, score: 100);

            session.TryMove(Direction.Left);

            Assert.AreEqual(GameState.Won, session.State);
            Assert.AreEqual(2148, session.Score);
            Assert.IsTrue(session.HasWon);
            Assert.IsNull(session.TryMove(Direction.Right));
        }

        [Test]
        public void Win_ThenRestart_Resets()
        {
            var session = new GameSession(new TestRandom());
            var cells = Empty();
            cells[0, 0] = 1024;
            cells[0, 1] = 1024;
            session.Load(cells);
            session.TryMove(Direction.Left);

            session.NewGame();

            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(GameState.Playing, session.State);
            Assert.IsFalse(session.HasWon);
            Assert.AreEqual(2, CountTiles(session.Board));
            Assert.LessOrEqual(session.Board.MaxValue, 4);
        }

        [Test]
        public void Continue_AfterWin_KeepsPlaying_AndDoesNotWinAgain()
        {
            var session = new GameSession(new TestRandom());
            var cells = Empty();
            cells[0, 0] = 1024;
            cells[0, 1] = 1024;
            cells[1, 0] = 1024;
            cells[1, 1] = 1024;
            session.Load(cells);

            session.TryMove(Direction.Left);
            Assert.AreEqual(GameState.Won, session.State);

            Assert.IsTrue(session.Continue());
            Assert.AreEqual(GameState.PlayingAfterWin, session.State);

            session.TryMove(Direction.Up); // 2048 + 2048 → 4096
            Assert.AreEqual(GameState.PlayingAfterWin, session.State);
            Assert.AreEqual(4096, session.Board[0, 0]);
        }

        [Test]
        public void Restart_ClearsScoreAndTiles()
        {
            var session = new GameSession(new TestRandom());
            session.Load(new[,]
            {
                { 2, 4, 8, 16 },
                { 32, 64, 128, 256 },
                { 2, 4, 8, 16 },
                { 32, 64, 128, 256 }
            }, score: 999);

            session.NewGame();

            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(2, CountTiles(session.Board));
            Assert.AreEqual(GameState.Playing, session.State);
        }

        [Test]
        public void Undo_RestoresBoardScoreAndState_KeepsBest()
        {
            var store = new InMemoryScoreStore();
            var session = new GameSession(new TestRandom(), store);
            var cells = Empty();
            cells[0, 0] = 2;
            cells[0, 1] = 2;
            session.Load(cells);

            session.TryMove(Direction.Left);
            Assert.AreEqual(4, session.Score);
            Assert.IsTrue(session.CanUndo);

            Assert.IsTrue(session.Undo());
            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(4, session.Best);
            CollectionAssert.AreEqual(cells, session.Board.ToArray());
            Assert.IsFalse(session.CanUndo);
            Assert.IsFalse(session.Undo());
        }

        [Test]
        public void Undo_FromGameOver_ReturnsToPlaying()
        {
            var session = new GameSession(new TestRandom());
            session.Load(new[,]
            {
                { 2, 4, 2, 4 },
                { 4, 2, 4, 2 },
                { 2, 4, 2, 4 },
                { 8, 16, 4, 4 }
            });
            session.TryMove(Direction.Left);
            Assert.AreEqual(GameState.Lost, session.State);

            session.Undo();
            Assert.AreEqual(GameState.Playing, session.State);
        }

        [Test]
        public void Undo_FromWin_RevertsWinFlag()
        {
            var session = new GameSession(new TestRandom());
            var cells = Empty();
            cells[0, 0] = 1024;
            cells[0, 1] = 1024;
            session.Load(cells);
            session.TryMove(Direction.Left);

            session.Undo();

            Assert.AreEqual(GameState.Playing, session.State);
            Assert.IsFalse(session.HasWon);
        }

        [Test]
        public void Best_IsLoadedAndPersisted()
        {
            var store = new InMemoryScoreStore(10);
            var session = new GameSession(new TestRandom(), store);
            Assert.AreEqual(10, session.Best);

            var cells = Empty();
            cells[0, 0] = 8;
            cells[0, 1] = 8;
            session.Load(cells);
            session.TryMove(Direction.Left);

            Assert.AreEqual(16, session.Best);
            Assert.AreEqual(16, store.LoadBest());

            session.NewGame();
            Assert.AreEqual(16, session.Best);
        }
    }
}
