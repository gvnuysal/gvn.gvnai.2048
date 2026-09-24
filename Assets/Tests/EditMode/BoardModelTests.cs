using System.Linq;
using Game2048.Core;
using NUnit.Framework;

namespace Game2048.Tests
{
    public class BoardModelTests
    {
        private static BoardModel SingleRow(params int[] row)
        {
            var cells = new int[4, 4];
            for (int c = 0; c < 4; c++) cells[0, c] = row[c];
            return BoardModel.FromArray(cells);
        }

        private static int[] Row(BoardModel board, int r) =>
            Enumerable.Range(0, board.Size).Select(c => board[r, c]).ToArray();

        private static int[] Col(BoardModel board, int c) =>
            Enumerable.Range(0, board.Size).Select(r => board[r, c]).ToArray();

        // --- SlideLine: direction-independent rule ---

        [TestCase(new[] { 2, 0, 2, 0 }, new[] { 4, 0, 0, 0 }, 4)]
        [TestCase(new[] { 2, 2, 2, 2 }, new[] { 4, 4, 0, 0 }, 8)]
        [TestCase(new[] { 2, 2, 4, 0 }, new[] { 4, 4, 0, 0 }, 4)]
        [TestCase(new[] { 4, 4, 4, 0 }, new[] { 8, 4, 0, 0 }, 8)]
        [TestCase(new[] { 0, 0, 0, 2 }, new[] { 2, 0, 0, 0 }, 0)]
        [TestCase(new[] { 2, 4, 8, 16 }, new[] { 2, 4, 8, 16 }, 0)]
        [TestCase(new[] { 8, 0, 8, 8 }, new[] { 16, 8, 0, 0 }, 16)]
        [TestCase(new[] { 1024, 1024, 0, 0 }, new[] { 2048, 0, 0, 0 }, 2048)]
        public void SlideLine_ProducesExpectedValuesAndScore(int[] input, int[] expected, int score)
        {
            var result = BoardModel.SlideLine(input);
            CollectionAssert.AreEqual(expected, result.Values);
            Assert.AreEqual(score, result.ScoreGained);
        }

        [Test]
        public void SlideLine_DoesNotModifyInput()
        {
            var input = new[] { 2, 2, 0, 0 };
            BoardModel.SlideLine(input);
            CollectionAssert.AreEqual(new[] { 2, 2, 0, 0 }, input);
        }

        [Test]
        public void SlideLine_UnchangedLine_ReportsNoChange()
        {
            Assert.IsFalse(BoardModel.SlideLine(new[] { 2, 0, 0, 0 }).Changed);
            Assert.IsFalse(BoardModel.SlideLine(new[] { 0, 0, 0, 0 }).Changed);
            Assert.IsTrue(BoardModel.SlideLine(new[] { 0, 2, 0, 0 }).Changed);
        }

        [Test]
        public void SlideLine_TargetsTrackEachTile()
        {
            var result = BoardModel.SlideLine(new[] { 2, 2, 0, 4 });
            CollectionAssert.AreEqual(new[] { 0, 0, -1, 1 }, result.Targets);
            CollectionAssert.AreEqual(new[] { true, false, false, false }, result.MergedAt);
        }

        // --- Acceptance scenarios (analysis doc, section 5) ---

        [Test]
        public void Left_2020_Gives_4000_Plus4()
        {
            var board = SingleRow(2, 0, 2, 0);
            var result = board.Move(Direction.Left);
            CollectionAssert.AreEqual(new[] { 4, 0, 0, 0 }, Row(board, 0));
            Assert.AreEqual(4, result.ScoreGained);
            Assert.IsTrue(result.Changed);
        }

        [Test]
        public void Left_2222_Gives_4400_Plus8_NoChainMerge()
        {
            var board = SingleRow(2, 2, 2, 2);
            var result = board.Move(Direction.Left);
            CollectionAssert.AreEqual(new[] { 4, 4, 0, 0 }, Row(board, 0));
            Assert.AreEqual(8, result.ScoreGained);
        }

        [Test]
        public void Right_4440_Gives_0048_Plus8()
        {
            var board = SingleRow(4, 4, 4, 0);
            var result = board.Move(Direction.Right);
            CollectionAssert.AreEqual(new[] { 0, 0, 4, 8 }, Row(board, 0));
            Assert.AreEqual(8, result.ScoreGained);
        }

        [Test]
        public void Left_2000_IsNotAChange()
        {
            var board = SingleRow(2, 0, 0, 0);
            var result = board.Move(Direction.Left);
            CollectionAssert.AreEqual(new[] { 2, 0, 0, 0 }, Row(board, 0));
            Assert.IsFalse(result.Changed);
            Assert.AreEqual(0, result.ScoreGained);
        }

        [Test]
        public void UpAndDown_WorkOnColumns()
        {
            var cells = new int[4, 4];
            cells[0, 1] = 2;
            cells[1, 1] = 2;
            cells[2, 1] = 4;
            cells[3, 1] = 4;

            var up = BoardModel.FromArray(cells);
            Assert.AreEqual(12, up.Move(Direction.Up).ScoreGained);
            CollectionAssert.AreEqual(new[] { 4, 8, 0, 0 }, Col(up, 1));

            var down = BoardModel.FromArray(cells);
            Assert.AreEqual(12, down.Move(Direction.Down).ScoreGained);
            CollectionAssert.AreEqual(new[] { 0, 0, 4, 8 }, Col(down, 1));
        }

        [Test]
        public void Move_ReportsTileMovesAndMerges()
        {
            var board = SingleRow(0, 2, 0, 2);
            var result = board.Move(Direction.Left);

            Assert.AreEqual(2, result.Moves.Count);
            Assert.IsTrue(result.Moves.All(m => m.To.Row == 0 && m.To.Col == 0 && m.Merged));
            Assert.AreEqual(1, result.Merges.Count);
            Assert.AreEqual(4, result.Merges[0].Value);
            Assert.AreEqual(4, result.MaxMergedValue);
        }

        // --- Game over detection ---

        [Test]
        public void FullBoard_WithEqualNeighbours_HasMoves()
        {
            var board = BoardModel.FromArray(new[,]
            {
                { 2, 4, 2, 4 },
                { 4, 2, 4, 2 },
                { 2, 4, 2, 4 },
                { 4, 2, 4, 4 }
            });
            Assert.IsTrue(board.HasMoves());
        }

        [Test]
        public void FullBoard_WithEqualVerticalNeighbours_HasMoves()
        {
            var board = BoardModel.FromArray(new[,]
            {
                { 2, 4, 2, 4 },
                { 4, 2, 4, 2 },
                { 2, 4, 2, 8 },
                { 4, 2, 4, 8 }
            });
            Assert.IsTrue(board.HasMoves());
        }

        [Test]
        public void FullBoard_WithoutEqualNeighbours_HasNoMoves()
        {
            var board = BoardModel.FromArray(new[,]
            {
                { 2, 4, 2, 4 },
                { 4, 2, 4, 2 },
                { 2, 4, 2, 4 },
                { 4, 2, 4, 2 }
            });
            Assert.IsFalse(board.HasMoves());
        }

        [Test]
        public void SpawnRandom_UsesEmptyCellAndRoll()
        {
            var board = new BoardModel();
            board[0, 0] = 8;

            var two = board.SpawnRandom(new TestRandom(0.0));
            Assert.IsTrue(two.HasValue);
            Assert.AreEqual(2, two.Value.Value);
            Assert.AreEqual(1, two.Value.Cell.Col);

            var four = board.SpawnRandom(new TestRandom(0.95));
            Assert.AreEqual(4, four.Value.Value);
            Assert.AreEqual(2, four.Value.Cell.Col);
        }

        [Test]
        public void SpawnRandom_FullBoard_ReturnsNull()
        {
            var board = BoardModel.FromArray(new[,]
            {
                { 2, 4, 2, 4 },
                { 4, 2, 4, 2 },
                { 2, 4, 2, 4 },
                { 4, 2, 4, 2 }
            });
            Assert.IsNull(board.SpawnRandom(new TestRandom()));
        }
    }
}
