using System;
using System.Collections.Generic;
using SlidewrightQA.Logic;

namespace SlidewrightQA.Tests.EditMode
{
    /// <summary>
    /// Builds test levels from ASCII pictures so every test shows the board it is about:
    ///
    ///     TestLevels.FromAscii(
    ///         "......",
    ///         "...A..",
    ///         "RR.A..",   // exit is on the right of this row (row 2)
    ///         "......",
    ///         "......",
    ///         "......");
    ///
    /// Each letter is a vehicle id; a horizontal run of the same letter is an H vehicle, a vertical
    /// run is a V vehicle. '.' is an empty cell. Ids must be unique, so neighbours need different letters.
    /// </summary>
    public static class TestLevels
    {
        public static LevelData FromAscii(params string[] rows) => FromAscii("ascii", 2, rows);

        public static LevelData FromAscii(string name, int exitRow, params string[] rows)
        {
            var height = rows.Length;
            var width = rows[0].Length;
            var cells = new Dictionary<char, List<GridPos>>();
            var order = new List<char>();

            for (var y = 0; y < height; y++)
            {
                if (rows[y].Length != width) throw new ArgumentException($"Row {y} has a different width.");
                for (var x = 0; x < width; x++)
                {
                    var c = rows[y][x];
                    if (c == '.') continue;
                    if (!cells.ContainsKey(c))
                    {
                        cells[c] = new List<GridPos>();
                        order.Add(c);
                    }
                    cells[c].Add(new GridPos(x, y));
                }
            }

            var vehicles = new List<VehicleDef>();
            foreach (var id in order)
            {
                var list = cells[id];
                var first = list[0]; // top-left, because we scanned row by row, left to right
                var horizontal = list.Count == 1 || list[1].Y == first.Y;
                vehicles.Add(new VehicleDef(id.ToString(), first.X, first.Y, list.Count,
                    horizontal ? Orientation.Horizontal : Orientation.Vertical));
            }

            return new LevelData(name, width, height, exitRow, "right", vehicles);
        }

        /// <summary>A level built directly from vehicles, for validator tests that need invalid layouts.</summary>
        public static LevelData Custom(params VehicleDef[] vehicles) =>
            new LevelData("custom", 6, 6, 2, "right", vehicles);

        public static VehicleDef H(string id, int x, int y, int length = 2) =>
            new VehicleDef(id, x, y, length, Orientation.Horizontal);

        public static VehicleDef V(string id, int x, int y, int length = 2) =>
            new VehicleDef(id, x, y, length, Orientation.Vertical);

        /// <summary>A minimal valid level JSON string with an optional raw "vehicles" array override.</summary>
        public static string Json(string vehicles = null, string grid = "{\"width\":6,\"height\":6}",
            string exit = "{\"row\":2,\"side\":\"right\"}") =>
            "{\"name\":\"json test\",\"grid\":" + grid + ",\"exit\":" + exit + ",\"vehicles\":" +
            (vehicles ?? "[{\"id\":\"R\",\"x\":0,\"y\":2,\"length\":2,\"orientation\":\"H\"}]") + "}";
    }
}
