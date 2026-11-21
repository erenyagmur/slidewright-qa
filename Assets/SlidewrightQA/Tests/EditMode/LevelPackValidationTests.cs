using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SlidewrightQA.Logic;

namespace SlidewrightQA.Tests.EditMode
{
    /// <summary>
    /// "Nobody ships a broken level" gate.
    ///
    /// Data-driven: NUnit generates one test per level in every shipped pack, named after the pack and
    /// level, so the Test Runner / CI report says exactly which level is broken, e.g.
    ///     PackLevel_IsValidAndSolvable(pack.json #07 Rush Hour)  FAILED: MisplacedRedCar ...
    /// </summary>
    [TestFixture]
    [Category("LevelPack")]
    public class LevelPackValidationTests
    {
        /// <summary>One test case per pack file, so a pack that doesn't even parse still shows up as a failure.</summary>
        private static IEnumerable<TestCaseData> PackFileCases() =>
            LevelPacks.PackFiles().Select(path =>
                new TestCaseData(path).SetName($"PackFile_Parses({Path.GetFileName(path)})"));

        /// <summary>One test case per level in every pack that parses.</summary>
        private static IEnumerable<TestCaseData> LevelCases()
        {
            foreach (var path in LevelPacks.PackFiles())
            {
                IReadOnlyList<LevelData> levels;
                try
                {
                    levels = LevelPacks.Load(path);
                }
                catch (LevelFormatException)
                {
                    continue; // reported by PackFile_Parses
                }

                var file = Path.GetFileName(path);
                for (var i = 0; i < levels.Count; i++)
                    yield return new TestCaseData(levels[i])
                        .SetName($"PackLevel_IsValidAndSolvable({file} #{i + 1:00} {levels[i].Name})");
            }
        }

        [Test]
        public void AtLeastOnePackIsShipped()
        {
            CollectionAssert.IsNotEmpty(LevelPacks.PackFiles(), $"No level packs found in {LevelPacks.Folder}");
        }

        [TestCaseSource(nameof(PackFileCases))]
        public void PackFile_Parses(string path)
        {
            IReadOnlyList<LevelData> levels = null;
            Assert.DoesNotThrow(() => levels = LevelPacks.Load(path));
            CollectionAssert.IsNotEmpty(levels, "Pack contains no levels");

            var duplicateNames = levels.GroupBy(l => l.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            CollectionAssert.IsEmpty(duplicateNames, "Level names must be unique within a pack (they identify levels in reports)");
        }

        [TestCaseSource(nameof(LevelCases))]
        public void PackLevel_IsValidAndSolvable(LevelData level)
        {
            // 1. Structurally valid?
            var errors = LevelValidator.Validate(level);
            Assert.IsEmpty(errors, $"Level \"{level.Name}\" is invalid:\n  " + string.Join("\n  ", errors));

            // 2. Solvable?
            var result = Solver.Solve(level);
            Assert.IsFalse(result.HitStateLimit,
                $"Level \"{level.Name}\": solver gave up after {result.StatesExplored} states (inconclusive)");
            Assert.IsTrue(result.Solvable,
                $"Level \"{level.Name}\" is UNSOLVABLE ({result.StatesExplored} states explored):\n{new Board(level)}");

            // 3. Not trivially solved at the start?
            Assert.Greater(result.MoveCount, 0, $"Level \"{level.Name}\" is already solved at the start");

            // 4. If the designer recorded an optimal move count, it must match reality
            //    (catches levels edited after their par was set).
            if (level.OptimalMoves.HasValue)
                Assert.AreEqual(level.OptimalMoves.Value, result.MoveCount,
                    $"Level \"{level.Name}\": file says optimal is {level.OptimalMoves}, solver found " +
                    $"{result.MoveCount}: {string.Join(" ", result.Moves)}");

            TestContext.WriteLine($"{level.Name}: optimal {result.MoveCount} moves, {result.StatesExplored} states");
        }
    }
}
