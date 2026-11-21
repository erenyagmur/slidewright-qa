using System.Collections.Generic;

namespace SlidewrightQA.Logic
{
    /// <summary>
    /// Immutable description of one vehicle as it appears in a level file:
    /// its id, starting top-left cell, length (2 = car, 3 = truck) and orientation.
    /// The live position during play is tracked by <see cref="Board"/>, not here.
    /// </summary>
    public sealed class VehicleDef
    {
        public string Id { get; }
        public int X { get; }
        public int Y { get; }
        public int Length { get; }
        public Orientation Orientation { get; }

        public bool IsHorizontal => Orientation == Orientation.Horizontal;
        public bool IsTarget => Id == LevelData.TargetVehicleId;

        public VehicleDef(string id, int x, int y, int length, Orientation orientation)
        {
            Id = id;
            X = x;
            Y = y;
            Length = length;
            Orientation = orientation;
        }

        /// <summary>Cells covered by this vehicle at its starting position.</summary>
        public IEnumerable<GridPos> Cells()
        {
            for (var i = 0; i < Length; i++)
                yield return IsHorizontal ? new GridPos(X + i, Y) : new GridPos(X, Y + i);
        }

        public override string ToString() =>
            $"{Id}({X},{Y} len={Length} {(IsHorizontal ? "H" : "V")})";
    }
}
