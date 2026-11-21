using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SlidewrightQA.Logic
{
    /// <summary>
    /// Turns Slidewright level JSON into <see cref="LevelData"/>.
    ///
    /// Accepted shapes for a pack file:
    ///   - a single level object                    { "name": ..., "grid": ..., ... }
    ///   - an array of level objects                [ {...}, {...} ]
    ///   - an object with a "levels" array          { "name": "Pack", "levels": [ ... ] }
    ///
    /// The parser only checks that the file is well formed and every field has the right type.
    /// Game rules (overlaps, bounds, red car placement...) are checked by <see cref="LevelValidator"/>,
    /// so each kind of failure is reported by exactly one place.
    /// </summary>
    public static class LevelParser
    {
        /// <summary>Parses one level. Throws <see cref="LevelFormatException"/> on bad input.</summary>
        public static LevelData ParseLevel(string json)
        {
            var root = ParseJson(json);
            if (!(root is JObject obj))
                throw new LevelFormatException(LevelErrorCode.InvalidValue, "A level must be a JSON object.");
            return ReadLevel(obj, "level");
        }

        /// <summary>Parses a pack file (any of the three shapes above).</summary>
        public static IReadOnlyList<LevelData> ParsePack(string json)
        {
            var root = ParseJson(json);
            JArray array;
            if (root is JArray a)
                array = a;
            else if (root is JObject o && o["levels"] is JArray levels)
                array = levels;
            else if (root is JObject single)
                return new[] { ReadLevel(single, "level") };
            else
                throw new LevelFormatException(LevelErrorCode.InvalidValue, "A pack must be an object or an array.");

            var result = new List<LevelData>(array.Count);
            for (var i = 0; i < array.Count; i++)
            {
                if (!(array[i] is JObject levelObj))
                    throw new LevelFormatException(LevelErrorCode.InvalidValue, $"levels[{i}] is not an object.");
                result.Add(ReadLevel(levelObj, $"levels[{i}]"));
            }
            return result;
        }

        private static JToken ParseJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new LevelFormatException(LevelErrorCode.MalformedJson, "Level file is empty.");
            try
            {
                return JToken.Parse(json);
            }
            catch (JsonException e)
            {
                throw new LevelFormatException(LevelErrorCode.MalformedJson, $"Malformed JSON: {e.Message}", e);
            }
        }

        private static LevelData ReadLevel(JObject obj, string path)
        {
            var name = ReadString(obj, "name", path);
            var grid = ReadObject(obj, "grid", path);
            var width = ReadInt(grid, "width", path + ".grid");
            var height = ReadInt(grid, "height", path + ".grid");
            var exit = ReadObject(obj, "exit", path);
            var exitRow = ReadInt(exit, "row", path + ".exit");
            var exitSide = ReadString(exit, "side", path + ".exit");

            int? optimal = null;
            if (obj["optimalMoves"] != null && obj["optimalMoves"].Type != JTokenType.Null)
                optimal = ReadInt(obj, "optimalMoves", path);

            if (!(obj["vehicles"] is JArray vehiclesJson))
                throw obj["vehicles"] == null ? Missing(path, "vehicles") : WrongType(path, "vehicles", "an array");

            var vehicles = new List<VehicleDef>(vehiclesJson.Count);
            for (var i = 0; i < vehiclesJson.Count; i++)
            {
                var vPath = $"{path}.vehicles[{i}]";
                if (!(vehiclesJson[i] is JObject v))
                    throw new LevelFormatException(LevelErrorCode.InvalidValue, $"{vPath} is not an object.");
                vehicles.Add(new VehicleDef(
                    ReadString(v, "id", vPath),
                    ReadInt(v, "x", vPath),
                    ReadInt(v, "y", vPath),
                    ReadInt(v, "length", vPath),
                    ReadOrientation(v, vPath)));
            }

            return new LevelData(name, width, height, exitRow, exitSide, vehicles, optimal);
        }

        private static Orientation ReadOrientation(JObject v, string path)
        {
            var raw = ReadString(v, "orientation", path);
            switch (raw.ToUpperInvariant())
            {
                case "H":
                case "HORIZONTAL":
                    return Orientation.Horizontal;
                case "V":
                case "VERTICAL":
                    return Orientation.Vertical;
                default:
                    throw new LevelFormatException(LevelErrorCode.InvalidValue,
                        $"{path}.orientation must be \"H\" or \"V\", got \"{raw}\".");
            }
        }

        private static JObject ReadObject(JObject obj, string key, string path)
        {
            if (obj[key] is JObject o) return o;
            throw obj[key] == null ? Missing(path, key) : WrongType(path, key, "an object");
        }

        private static string ReadString(JObject obj, string key, string path)
        {
            var token = obj[key];
            if (token == null || token.Type == JTokenType.Null) throw Missing(path, key);
            if (token.Type != JTokenType.String) throw WrongType(path, key, "a string");
            return (string)token;
        }

        private static int ReadInt(JObject obj, string key, string path)
        {
            var token = obj[key];
            if (token == null || token.Type == JTokenType.Null) throw Missing(path, key);
            if (token.Type != JTokenType.Integer) throw WrongType(path, key, "an integer");
            try
            {
                return (int)token;
            }
            catch (OverflowException)
            {
                throw WrongType(path, key, "a 32-bit integer");
            }
        }

        private static LevelFormatException Missing(string path, string key) =>
            new LevelFormatException(LevelErrorCode.MissingField, $"{path}.{key} is missing.");

        private static LevelFormatException WrongType(string path, string key, string expected) =>
            new LevelFormatException(LevelErrorCode.InvalidValue, $"{path}.{key} must be {expected}.");
    }
}
