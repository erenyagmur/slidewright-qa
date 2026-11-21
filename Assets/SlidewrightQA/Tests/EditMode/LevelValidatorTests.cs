using System.Linq;
using NUnit.Framework;
using SlidewrightQA.Logic;
using static SlidewrightQA.Tests.EditMode.TestLevels;

namespace SlidewrightQA.Tests.EditMode
{
    /// <summary>
    /// One test per validation rule. Each test builds a level that breaks exactly one rule and
    /// checks that exactly that rule fires, so a failing test points straight at the broken rule.
    /// </summary>
    [TestFixture]
    public class LevelValidatorTests
    {
        private static void AssertOnlyError(LevelData level, LevelErrorCode expected)
        {
            var errors = LevelValidator.Validate(level);
            var codes = errors.Select(e => e.Code).Distinct().ToList();
            Assert.AreEqual(new[] { expected }, codes,
                "Errors were: " + string.Join("; ", errors.Select(e => e.ToString())));
        }

        [Test]
        public void ValidLevel_HasNoErrors()
        {
            var level = Custom(H("R", 0, 2), V("A", 3, 1), H("T", 0, 5, 3));
            CollectionAssert.IsEmpty(LevelValidator.Validate(level));
        }

        [Test]
        public void OverlappingVehicles_AreRejected()
        {
            // A (vertical, column 1, rows 1-2) sits on top of R's second cell (1,2).
            AssertOnlyError(Custom(H("R", 0, 2), V("A", 1, 1)), LevelErrorCode.Overlap);
        }

        [Test]
        public void VehiclePastRightEdge_IsRejected()
        {
            // Truck starting at x=4 would need x=4,5,6 on a 6-wide grid.
            AssertOnlyError(Custom(H("R", 0, 2), H("T", 4, 0, 3)), LevelErrorCode.OutOfBounds);
        }

        [Test]
        public void VehicleAtNegativeCoordinate_IsRejected()
        {
            AssertOnlyError(Custom(H("R", 0, 2), V("A", 0, -1)), LevelErrorCode.OutOfBounds);
        }

        [Test]
        public void MissingRedCar_IsRejected()
        {
            AssertOnlyError(Custom(H("A", 0, 2), V("B", 3, 0)), LevelErrorCode.MissingRedCar);
        }

        [Test]
        public void RedCarOnWrongRow_IsRejected()
        {
            AssertOnlyError(Custom(H("R", 0, 3)), LevelErrorCode.MisplacedRedCar);
        }

        [Test]
        public void VerticalRedCar_IsRejected()
        {
            // Even though it covers the exit row, a vertical red car can never drive out sideways.
            AssertOnlyError(Custom(V("R", 0, 1)), LevelErrorCode.MisplacedRedCar);
        }

        [Test]
        public void DuplicateVehicleIds_AreRejected()
        {
            AssertOnlyError(Custom(H("R", 0, 2), V("A", 3, 0), V("A", 5, 0)), LevelErrorCode.DuplicateVehicleId);
        }

        [TestCase(1)]
        [TestCase(4)]
        public void InvalidVehicleLength_IsRejected(int length)
        {
            AssertOnlyError(Custom(H("R", 0, 2), V("A", 4, 0, length)), LevelErrorCode.InvalidVehicleLength);
        }

        [Test]
        public void InvalidGridSize_IsRejected()
        {
            var level = new LevelData("tiny", 2, 6, 2, "right", new[] { H("R", 0, 2) });
            AssertOnlyError(level, LevelErrorCode.InvalidGrid);
        }

        [Test]
        public void UnsupportedExitSide_IsRejected()
        {
            var level = new LevelData("left exit", 6, 6, 2, "left", new[] { H("R", 0, 2) });
            AssertOnlyError(level, LevelErrorCode.UnsupportedExit);
        }

        [Test]
        public void MultipleProblems_AreAllReported()
        {
            // Designers should see every issue in one run, not fix-rerun-fix.
            var level = Custom(H("A", 5, 0), V("B", 0, 0), V("C", 0, 1));
            var codes = LevelValidator.Validate(level).Select(e => e.Code).ToList();
            CollectionAssert.Contains(codes, LevelErrorCode.OutOfBounds);
            CollectionAssert.Contains(codes, LevelErrorCode.Overlap);
            CollectionAssert.Contains(codes, LevelErrorCode.MissingRedCar);
        }

        [Test]
        public void Board_RefusesToLoadInvalidLevel()
        {
            // Defence in depth: even if validation is skipped somewhere, the game won't start a broken level.
            Assert.Throws<System.ArgumentException>(() => new Board(Custom(H("A", 0, 2))));
        }
    }
}
