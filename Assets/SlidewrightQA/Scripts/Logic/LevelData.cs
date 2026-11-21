using System.Collections.Generic;

namespace SlidewrightQA.Logic
{
    /// <summary>
    /// A parsed level, exactly as described by the JSON file. Parsing does NOT guarantee the
    /// level is playable; run <see cref="LevelValidator.Validate"/> for that.
    /// </summary>
    public sealed class LevelData
    {
        /// <summary>The red car that has to escape is always identified by this id.</summary>
        public const string TargetVehicleId = "R";

        public string Name { get; }
        public int Width { get; }
        public int Height { get; }
        public int ExitRow { get; }
        public string ExitSide { get; }
        public IReadOnlyList<VehicleDef> Vehicles { get; }

        /// <summary>Optional designer-supplied optimal move count (null when the file doesn't say).</summary>
        public int? OptimalMoves { get; }

        public LevelData(string name, int width, int height, int exitRow, string exitSide,
            IReadOnlyList<VehicleDef> vehicles, int? optimalMoves = null)
        {
            Name = name;
            Width = width;
            Height = height;
            ExitRow = exitRow;
            ExitSide = exitSide;
            Vehicles = vehicles;
            OptimalMoves = optimalMoves;
        }

        public override string ToString() => Name;
    }
}
