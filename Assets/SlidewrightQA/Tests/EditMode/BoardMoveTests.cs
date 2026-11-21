using NUnit.Framework;
using SlidewrightQA.Logic;

namespace SlidewrightQA.Tests.EditMode
{
    /// <summary>
    /// Movement rules. These are the core of the game: if any of these break, players can
    /// cheat through walls, get stuck, or see vehicles overlap.
    /// </summary>
    [TestFixture]
    public class BoardMoveTests
    {
        // A small layout reused by most tests:
        //   A = vertical car in column 3, B = horizontal truck on the bottom row.
        private static Board MakeBoard() => new Board(TestLevels.FromAscii(
            "......",
            "...A..",
            "RR.A..",
            "......",
            "......",
            "BBB..."));

        [Test]
        public void HorizontalCar_SlidesRightOneCell()
        {
            var board = MakeBoard();
            Assert.AreEqual(MoveResult.Ok, board.TryMove("R", 1));
            Assert.AreEqual(new GridPos(1, 2), board.GetPosition("R"));
        }

        [Test]
        public void VerticalCar_SlidesUpAndDown_AlongItsColumnOnly()
        {
            var board = MakeBoard();
            Assert.AreEqual(MoveResult.Ok, board.TryMove("A", -1), "up");
            Assert.AreEqual(new GridPos(3, 0), board.GetPosition("A"));
            Assert.AreEqual(MoveResult.Ok, board.TryMove("A", 3), "down");
            Assert.AreEqual(new GridPos(3, 3), board.GetPosition("A"), "x must never change for a vertical car");
        }

        [Test]
        public void MultiCellSlide_CountsAsOneMoveAndLandsOnTarget()
        {
            var board = MakeBoard();
            Assert.AreEqual(MoveResult.Ok, board.TryMove("B", 3));
            Assert.AreEqual(new GridPos(3, 5), board.GetPosition("B"));
        }

        [Test]
        public void Slide_IntoAnotherVehicle_IsBlocked()
        {
            var board = MakeBoard();
            // R occupies x=0..1; A is at x=3. Sliding 2 would put R's front on A.
            Assert.AreEqual(MoveResult.Blocked, board.TryMove("R", 2));
        }

        [Test]
        public void MultiCellSlide_CannotJumpOverVehicle_EvenIfDestinationIsFree()
        {
            var board = MakeBoard();
            // Destination x=4..5 is empty, but A sits in between at x=3.
            Assert.AreEqual(MoveResult.Blocked, board.TryMove("R", 4));
        }

        [Test]
        public void Slide_PastLeftEdge_IsOutOfBounds()
        {
            var board = MakeBoard();
            Assert.AreEqual(MoveResult.OutOfBounds, board.TryMove("R", -1));
        }

        [Test]
        public void Slide_PastBottomEdge_IsOutOfBounds()
        {
            var board = MakeBoard();
            // A covers rows 1-2; it can go down 3 (to rows 4-5) but not 4.
            Assert.AreEqual(MoveResult.OutOfBounds, board.TryMove("A", 4));
        }

        [Test]
        public void Slide_PastRightEdge_IsOutOfBounds()
        {
            var board = MakeBoard();
            Assert.AreEqual(MoveResult.OutOfBounds, board.TryMove("B", 4));
        }

        [Test]
        public void IllegalMove_LeavesBoardUnchanged()
        {
            var board = MakeBoard();
            var before = board.StateKey();
            board.TryMove("R", 4);  // blocked
            board.TryMove("B", -1); // edge
            Assert.AreEqual(before, board.StateKey());
        }

        [Test]
        public void UnknownVehicle_IsRejected()
        {
            Assert.AreEqual(MoveResult.UnknownVehicle, MakeBoard().TryMove("Z", 1));
        }

        [Test]
        public void ZeroDistanceMove_IsRejected()
        {
            Assert.AreEqual(MoveResult.ZeroDistance, MakeBoard().TryMove("R", 0));
        }

        [Test]
        public void SlideRange_ReportsHowFarEachVehicleCanGo()
        {
            var board = MakeBoard();
            Assert.AreEqual((0, 1), board.GetSlideRange(board.IndexOf("R")), "R: wall on the left, A on the right");
            Assert.AreEqual((-1, 3), board.GetSlideRange(board.IndexOf("A")));
            Assert.AreEqual((0, 3), board.GetSlideRange(board.IndexOf("B")));
        }

        [Test]
        public void Occupancy_FollowsTheVehicleAfterAMove()
        {
            var board = MakeBoard();
            board.TryMove("B", 3);
            Assert.IsNull(board.VehicleAt(0, 5), "old cell must be freed");
            Assert.AreEqual("B", board.VehicleAt(5, 5), "new cell must be taken");
        }

        [Test]
        public void Clone_IsIndependentOfOriginal()
        {
            var board = MakeBoard();
            var copy = board.Clone();
            copy.TryMove("B", 3);
            Assert.AreEqual(new GridPos(0, 5), board.GetPosition("B"));
        }
    }
}
