using System.Collections.Generic;
using System.IO;
using System.Linq;
using SlidewrightQA.Logic;
using UnityEngine;

namespace SlidewrightQA.Tests.EditMode
{
    /// <summary>
    /// Finds the level packs shipped with the game. Every *.json in Assets/Resources/Levels is a pack,
    /// so dropping a new pack (e.g. Slidewright's pack.json) into that folder automatically puts it
    /// under the pack validation gate with no test changes.
    /// </summary>
    public static class LevelPacks
    {
        public static string Folder => Path.Combine(Application.dataPath, "Resources", "Levels");

        public static IEnumerable<string> PackFiles() =>
            Directory.Exists(Folder)
                ? Directory.GetFiles(Folder, "*.json").OrderBy(p => p)
                : Enumerable.Empty<string>();

        public static IReadOnlyList<LevelData> Load(string path) => LevelParser.ParsePack(File.ReadAllText(path));

        public static IReadOnlyList<LevelData> Examples() => Load(Path.Combine(Folder, "examples.json"));
    }
}
