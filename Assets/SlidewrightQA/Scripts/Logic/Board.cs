using System;
using System.Collections.Generic;

namespace SlidewrightQA.Logic
{
    /// <summary>
    /// The live puzzle state: where every vehicle currently is, and the movement rules.
    ///
    /// Representation: each vehicle has a fixed "cross-axis" coordinate (its row if horizontal,
    /// its column if vertical) and one moving coordinate along its axis. So the whole state is a
    /// single int per vehicle, which keeps cloning and hashing cheap for the BFS solver.
    ///
    /// Rules enforced here:
    ///   - vehicles move only along their own axis;
    ///   - they cannot leave the grid;
    ///   - they cannot pass through or overlap other vehicles.
    /// </summary>
    public sealed class Board
    {
        private readonly VehicleDef[] _vehicles;
        private readonly Dictionary<string, int> _indexById;
        private readonly int[] _positions;   // moving coordinate per vehicle (x for H, y for V)
        private readonly int[,] _occupancy;  // vehicle index per cell, -1 = empty

        public LevelData Level { get; }
        public int Width => Level.Width;
        public int Height => Level.Height;
        public int VehicleCount => _vehicles.Length;
        public int TargetIndex { get; }

        /// <summary>Builds a board at the level's starting position. The level must be valid.</summary>
        public Board(LevelData level)
        {
            var errors = LevelValidator.Validate(level);
            if (errors.Count > 0)
                throw new ArgumentException($"Level \"{level.Name}\" is invalid: {errors[0]}", nameof(level));

            Level = level;
            _vehicles = new VehicleDef[level.Vehicles.Count];
            _positions = new int[_vehicles.Length];
            _indexById = new Dictionary<string, int>();
            for (var i = 0; i < _vehicles.Length; i++)
            {
                var v = level.Vehicles[i];
                _vehicles[i] = v;
                _positions[i] = v.IsHorizontal ? v.X : v.Y;
                _indexById[v.Id] = i;
            }

            TargetIndex = _indexById[LevelData.TargetVehicleId];
            _occupancy = new int[Width, Height];
            RebuildOccupancy();
        }

        private Board(Board other)
        {
            Level = other.Level;
            _vehicles = other._vehicles;           // immutable, safe to share
            _indexById = other._indexById;         // never mutated after construction
            TargetIndex = other.TargetIndex;
            _positions = (int[])other._positions.Clone();
            _occupancy = (int[,])other._occupancy.Clone();
        }

        public Board Clone() => new Board(this);

        public VehicleDef GetVehicle(int index) => _vehicles[index];

        /// <summary>Index of a vehicle by id, or -1 if no such vehicle exists.</summary>
        public int IndexOf(string id) => id != null && _indexById.TryGetValue(id, out var i) ? i : -1;

        /// <summary>Current top-left cell of a vehicle.</summary>
        public GridPos GetPosition(int index)
        {
            var v = _vehicles[index];
            return v.IsHorizontal ? new GridPos(_positions[index], v.Y) : new GridPos(v.X, _positions[index]);
        }

        public GridPos GetPosition(string id) => GetPosition(RequireIndex(id));

        /// <summary>Id of the vehicle in a cell, or null if empty / outside the grid.</summary>
        public string VehicleAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return null;
            var i = _occupancy[x, y];
            return i < 0 ? null : _vehicles[i].Id;
        }

        /// <summary>
        /// How far a vehicle can slide right now: Min is the most negative delta (left/up),
        /// Max the most positive (right/down). Both are 0 if the vehicle is boxed in.
        /// </summary>
        public (int Min, int Max) GetSlideRange(int index)
        {
            var min = 0;
            while (CheckStep(index, min - 1) == MoveResult.Ok) min--;
            var max = 0;
            while (CheckStep(index, max + 1) == MoveResult.Ok) max++;
            return (min, max);
        }

        public MoveResult TryMove(Move move) => TryMove(move.VehicleId, move.Delta);

        public MoveResult TryMove(string vehicleId, int delta)
        {
            var index = IndexOf(vehicleId);
            return index < 0 ? MoveResult.UnknownVehicle : TryMove(index, delta);
        }

        /// <summary>
        /// Slides a vehicle by <paramref name="delta"/> cells. The path is checked cell by cell in the
        /// direction of travel, so the result names the FIRST obstacle hit (edge or vehicle).
        /// On failure the board is unchanged.
        /// </summary>
        public MoveResult TryMove(int index, int delta)
        {
            if (index < 0 || index >= _vehicles.Length) return MoveResult.UnknownVehicle;
            if (delta == 0) return MoveResult.ZeroDistance;

            var step = Math.Sign(delta);
            for (var d = step; d != delta + step; d += step)
            {
                var result = CheckStep(index, d);
                if (result != MoveResult.Ok) return result;
            }

            SetOccupancy(index, -1);
            _positions[index] += delta;
            SetOccupancy(index, index);
            return MoveResult.Ok;
        }

        /// <summary>True when the red car's front has reached the exit cell on the right edge.</summary>
        public bool IsSolved
        {
            get
            {
                var red = _vehicles[TargetIndex];
                return _positions[TargetIndex] + red.Length == Width && red.Y == Level.ExitRow;
            }
        }

        /// <summary>
        /// Compact, unique key for the current arrangement. Two boards of the same level with the
        /// same key are the same puzzle state (used by the solver's visited set).
        /// </summary>
        public string StateKey()
        {
            var chars = new char[_positions.Length];
            for (var i = 0; i < chars.Length; i++) chars[i] = (char)('0' + _positions[i]);
            return new string(chars);
        }

        /// <summary>
        /// Checks the single "leading" cell a vehicle would newly enter if its total offset were
        /// <paramref name="offset"/>. Used both for range queries and to validate multi-cell slides.
        /// </summary>
        private MoveResult CheckStep(int index, int offset)
        {
            var v = _vehicles[index];
            var newStart = _positions[index] + offset;
            var leading = offset > 0 ? newStart + v.Length - 1 : newStart;
            var limit = v.IsHorizontal ? Width : Height;

            if (leading < 0 || leading >= limit) return MoveResult.OutOfBounds;

            var occupant = v.IsHorizontal ? _occupancy[leading, v.Y] : _occupancy[v.X, leading];
            return occupant >= 0 && occupant != index ? MoveResult.Blocked : MoveResult.Ok;
        }

        private void RebuildOccupancy()
        {
            for (var x = 0; x < Width; x++)
            for (var y = 0; y < Height; y++)
                _occupancy[x, y] = -1;
            for (var i = 0; i < _vehicles.Length; i++) SetOccupancy(i, i);
        }

        private void SetOccupancy(int index, int value)
        {
            var v = _vehicles[index];
            var p = _positions[index];
            for (var k = 0; k < v.Length; k++)
            {
                if (v.IsHorizontal) _occupancy[p + k, v.Y] = value;
                else _occupancy[v.X, p + k] = value;
            }
        }

        private int RequireIndex(string id)
        {
            var i = IndexOf(id);
            if (i < 0) throw new KeyNotFoundException($"No vehicle with id \"{id}\".");
            return i;
        }

        /// <summary>ASCII picture of the board, handy in test failure messages.</summary>
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var id = VehicleAt(x, y);
                    sb.Append(id == null ? '.' : id[0]);
                }
                if (y == Level.ExitRow) sb.Append(" <exit");
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
