using System;

namespace SlidewrightQA.Logic
{
    /// <summary>
    /// One player move: slide vehicle <see cref="VehicleId"/> by <see cref="Delta"/> cells along its axis.
    /// Positive = right/down, negative = left/up. A slide of any distance counts as ONE move,
    /// which is how both the in-game move counter and the solver's "optimal" count are defined.
    /// </summary>
    public readonly struct Move : IEquatable<Move>
    {
        public readonly string VehicleId;
        public readonly int Delta;

        public Move(string vehicleId, int delta)
        {
            VehicleId = vehicleId;
            Delta = delta;
        }

        public bool Equals(Move other) => VehicleId == other.VehicleId && Delta == other.Delta;
        public override bool Equals(object obj) => obj is Move other && Equals(other);
        public override int GetHashCode() => (VehicleId?.GetHashCode() ?? 0) * 31 + Delta;
        public override string ToString() => $"{VehicleId}{(Delta > 0 ? "+" : "")}{Delta}";
    }

    /// <summary>Outcome of trying a move. Anything other than Ok leaves the board unchanged.</summary>
    public enum MoveResult
    {
        Ok,
        UnknownVehicle,
        ZeroDistance,
        /// <summary>Another vehicle is in the way.</summary>
        Blocked,
        /// <summary>The slide would push the vehicle past the board edge.</summary>
        OutOfBounds
    }
}
