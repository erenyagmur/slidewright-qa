using NUnit.Framework;
using SlidewrightQA.Logic;

namespace SlidewrightQA.Tests.EditMode
{
    /// <summary>
    /// The win check. A false positive lets players skip levels; a false negative makes a level
    /// unwinnable. Both are release blockers, so both directions are tested.
    /// </summary>
    [TestFixture]
    public class WinConditionTests
    {
        [Test]
        public void StartingPosition_IsNotSolved()
        {
            var board = new Board(TestLevels.FromAscii(
                "......",
                "......",
                "RR....",
                "......",
                "......",
                "......"));
            Assert.IsFalse(board.IsSolved);
        }

        [Test]
        public void RedCarAtRightEdgeOfExitRow_IsSolved()
        {
            var board = new Board(TestLevels.FromAscii(
                "......",
                "......",
                "RR....",
                "......",
                "......",
                "......"));
            Assert.AreEqual(MoveResult.Ok, board.TryMove("R", 4));
            Assert.IsTrue(board.IsSolved);
        }

        [Test]
        public void RedCarOneCellShortOfExit_IsNotSolved()
        {
            var board = new Board(TestLevels.FromAscii(
                "......",
                "......",
                "RR....",
                "......",
                "......",
                "......"));
            board.TryMove("R", 3);
            Assert.IsFalse(board.IsSolved);
        }

        [Test]
        public void OtherVehicleAtExit_DoesNotCountAsWin()
        {
            // Only the red car escaping wins; a different car on the exit row reaching the edge doesn't.
            var board = new Board(TestLevels.FromAscii(
                "......",
                "......",
                "RR..AA",
                "......",
                "......",
                "......"));
            Assert.IsFalse(board.IsSolved);
        }

        [Test]
        public void RedCarBlockedByVerticalTruckOnExitRow_CannotWin()
        {
            // T is a vertical truck covering rows 1-3 in column 4, sitting across the exit row.
            var board = new Board(TestLevels.FromAscii(
                "......",
                "....T.",
                "RR..T.",
                "....T.",
                "......",
                "......"));

            Assert.AreEqual(MoveResult.Blocked, board.TryMove("R", 4), "R must not drive through the truck");
            Assert.AreEqual(MoveResult.Ok, board.TryMove("R", 2), "R can still drive up to the truck");
            Assert.IsFalse(board.IsSolved, "touching the truck is not escaping");
        }

        [Test]
        public void RedCarBlockedByTruck_WinsOnceTruckIsMovedOutOfTheRow()
        {
            var board = new Board(TestLevels.FromAscii(
                "......",
                "....T.",
                "RR..T.",
                "....T.",
                "......",
                "......"));

            Assert.AreEqual(MoveResult.Ok, board.TryMove("T", 2), "truck down to rows 3-5");
            Assert.AreEqual(MoveResult.Ok, board.TryMove("R", 4));
            Assert.IsTrue(board.IsSolved);
        }

        [Test]
        public void TruckMovedOnlyPartlyOutOfTheRow_StillBlocks()
        {
            var board = new Board(TestLevels.FromAscii(
                "......",
                "....T.",
                "RR..T.",
                "....T.",
                "......",
                "......"));

            board.TryMove("T", 1); // rows 2-4: still on the exit row
            Assert.AreEqual(MoveResult.Blocked, board.TryMove("R", 4));
            Assert.IsFalse(board.IsSolved);
        }
    }
}
