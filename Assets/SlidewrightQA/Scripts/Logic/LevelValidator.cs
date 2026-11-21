using System.Collections.Generic;
using System.Linq;

namespace SlidewrightQA.Logic
{
    /// <summary>
    /// Checks that a parsed level is playable. Returns every problem it finds (not just the first)
    /// so a designer sees all issues in one test run. An empty list means the level is valid.
    ///
    /// Solvability is NOT checked here because it is expensive; the pack validation test runs the
    /// <see cref="Solver"/> separately.
    /// </summary>
    public static class LevelValidator
    {
        public const int MinGridSize = 3;
        public const int MaxGridSize = 12;

        public static IReadOnlyList<LevelError> Validate(LevelData level)
        {
            var errors = new List<LevelError>();

            // Grid: if the grid itself is broken, the cell checks below would be meaningless.
            if (level.Width < MinGridSize || level.Width > MaxGridSize ||
                level.Height < MinGridSize || level.Height > MaxGridSize)
            {
                errors.Add(new LevelError(LevelErrorCode.InvalidGrid,
                    $"Grid {level.Width}x{level.Height} is outside {MinGridSize}..{MaxGridSize}."));
                return errors;
            }

            // Exit: only right-edge exits are supported by this game.
            if (level.ExitSide != "right")
                errors.Add(new LevelError(LevelErrorCode.UnsupportedExit,
                    $"Exit side \"{level.ExitSide}\" is not supported (only \"right\")."));
            if (level.ExitRow < 0 || level.ExitRow >= level.Height)
                errors.Add(new LevelError(LevelErrorCode.UnsupportedExit,
                    $"Exit row {level.ExitRow} is outside the grid."));

            // Ids must be unique, otherwise moves and the solver can't address vehicles.
            foreach (var dup in level.Vehicles.GroupBy(v => v.Id).Where(g => g.Count() > 1))
                errors.Add(new LevelError(LevelErrorCode.DuplicateVehicleId,
                    $"Vehicle id \"{dup.Key}\" is used {dup.Count()} times."));

            // Per-vehicle size and bounds checks.
            var inBounds = new List<VehicleDef>();
            foreach (var v in level.Vehicles)
            {
                if (v.Length != 2 && v.Length != 3)
                {
                    errors.Add(new LevelError(LevelErrorCode.InvalidVehicleLength,
                        $"Vehicle {v.Id} has length {v.Length} (cars are 2, trucks are 3)."));
                    continue;
                }

                if (v.Cells().Any(c => c.X < 0 || c.Y < 0 || c.X >= level.Width || c.Y >= level.Height))
                {
                    errors.Add(new LevelError(LevelErrorCode.OutOfBounds,
                        $"Vehicle {v} extends outside the {level.Width}x{level.Height} grid."));
                    continue;
                }

                inBounds.Add(v);
            }

            // Overlaps: paint every in-bounds vehicle onto a grid and report each clashing pair once.
            var owner = new Dictionary<GridPos, VehicleDef>();
            var reported = new HashSet<string>();
            foreach (var v in inBounds)
            {
                foreach (var cell in v.Cells())
                {
                    if (owner.TryGetValue(cell, out var other))
                    {
                        if (reported.Add(other.Id + "|" + v.Id))
                            errors.Add(new LevelError(LevelErrorCode.Overlap,
                                $"Vehicles {other.Id} and {v.Id} overlap at {cell}."));
                    }
                    else
                    {
                        owner[cell] = v;
                    }
                }
            }

            // Red car: must exist and be horizontal on the exit row, or the level can never be won.
            var red = level.Vehicles.Where(v => v.IsTarget).ToList();
            if (red.Count == 0)
            {
                errors.Add(new LevelError(LevelErrorCode.MissingRedCar,
                    $"No vehicle with id \"{LevelData.TargetVehicleId}\" (the red car)."));
            }
            else if (red.Count == 1)
            {
                var r = red[0];
                if (!r.IsHorizontal || r.Y != level.ExitRow)
                    errors.Add(new LevelError(LevelErrorCode.MisplacedRedCar,
                        $"Red car must be horizontal on exit row {level.ExitRow}, but is {r}."));
            }
            // (More than one red car is already reported as DuplicateVehicleId.)

            return errors;
        }

        public static bool IsValid(LevelData level) => Validate(level).Count == 0;
    }
}
