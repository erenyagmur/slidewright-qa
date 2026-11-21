using System.Linq;
using NUnit.Framework;
using SlidewrightQA.Logic;

namespace SlidewrightQA.Tests.EditMode
{
    /// <summary>
    /// The BFS solver is test infrastructure: the pack gate uses it to prove every level is solvable,
    /// and the PlayMode bot replays its solutions. So it gets tested like production code.
    /// </summary>
    [TestFixture]
    public class SolverTests
    {
        // Known optimal move counts for the bundled example levels. These were worked out by hand and
        // cross-checked with an independent BFS written in Python.
        [TestCase("Example 1 - First Steps", 2)]
        [TestCase("Example 2 - Truck Stop", 3)]
        [TestCase("Example 3 - Chain Reaction", 5)]
        public void ExampleLevel_SolvesInKnownOptimalMoveCount(string levelName, int expectedMoves)
        {
            var level = LevelPacks.Examples().Single(l => l.Name == levelName);
            var result = Solver.Solve(level);

            Assert.IsTrue(result.Solvable, $"{levelName} should be solvable");
            Assert.AreEqual(expectedMoves, result.MoveCount,
                $"{levelName}: solver found [{string.Join(", ", result.Moves)}]");
        }

        [Test]
        public void Solution_ReplaysToAWinOnAFreshBoard()
        {
            // The solver's own claim isn't enough: replay every move through the normal move rules.
            foreach (var level in LevelPacks.Examples())
            {
                var board = new Board(level);
                foreach (var move in Solver.Solve(level).Moves)
                    Assert.AreEqual(MoveResult.Ok, board.TryMove(move), $"{level.Name}: illegal move {move}");
                Assert.IsTrue(board.IsSolved, $"{level.Name}: replay did not end in a win");
            }
        }

        [Test]
        public void ClearPath_IsOneMove()
        {
            var level = TestLevels.FromAscii(
                "......",
                "......",
                "RR....",
                "......",
                "......",
                "......");
            var result = Solver.Solve(level);
            Assert.AreEqual(new[] { new Move("R", 4) }, result.Moves.ToArray());
        }

        [Test]
        public void AlreadySolved_NeedsZeroMoves()
        {
            var level = TestLevels.FromAscii(
                "......",
                "......",
                "....RR",
                "......",
                "......",
                "......");
            var result = Solver.Solve(level);
            Assert.IsTrue(result.Solvable);
            Assert.AreEqual(0, result.MoveCount);
        }

        [Test]
        public void UnsolvableLevel_IsReportedAsUnsolvable()
        {
            // Column 4 is completely filled by two vertical trucks, so nothing can ever clear the exit row.
            var level = TestLevels.FromAscii(
                "....A.",
                "....A.",
                "RR..A.",
                "....B.",
                "....B.",
                "....B.");
            var result = Solver.Solve(level);
            Assert.IsFalse(result.Solvable);
            Assert.IsFalse(result.HitStateLimit, "the search should finish, not give up");
        }

        [Test]
        public void StateLimit_IsReportedAsInconclusive()
        {
            var level = LevelPacks.Examples().Single(l => l.Name == "Example 3 - Chain Reaction");
            var result = Solver.Solve(level, maxStates: 10);
            Assert.IsFalse(result.Solvable);
            Assert.IsTrue(result.HitStateLimit);
        }
    }
}
