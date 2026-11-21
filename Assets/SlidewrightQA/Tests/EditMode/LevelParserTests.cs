using NUnit.Framework;
using SlidewrightQA.Logic;

namespace SlidewrightQA.Tests.EditMode
{
    /// <summary>
    /// Reading level files. Levels come from an external design tool (Slidewright), so the game must
    /// reject bad files with a clear error instead of crashing or loading a half-built level.
    /// </summary>
    [TestFixture]
    public class LevelParserTests
    {
        [Test]
        public void ValidLevel_ParsesAllFields()
        {
            var level = LevelParser.ParseLevel(
                "{\"name\":\"Demo\",\"optimalMoves\":7,\"grid\":{\"width\":6,\"height\":6}," +
                "\"exit\":{\"row\":2,\"side\":\"right\"},\"vehicles\":[" +
                "{\"id\":\"R\",\"x\":0,\"y\":2,\"length\":2,\"orientation\":\"H\"}," +
                "{\"id\":\"T\",\"x\":4,\"y\":0,\"length\":3,\"orientation\":\"V\"}]}");

            Assert.AreEqual("Demo", level.Name);
            Assert.AreEqual(6, level.Width);
            Assert.AreEqual(6, level.Height);
            Assert.AreEqual(2, level.ExitRow);
            Assert.AreEqual("right", level.ExitSide);
            Assert.AreEqual(7, level.OptimalMoves);
            Assert.AreEqual(2, level.Vehicles.Count);

            var truck = level.Vehicles[1];
            Assert.AreEqual("T", truck.Id);
            Assert.AreEqual(4, truck.X);
            Assert.AreEqual(0, truck.Y);
            Assert.AreEqual(3, truck.Length);
            Assert.AreEqual(Orientation.Vertical, truck.Orientation);
        }

        [Test]
        public void OptimalMoves_IsOptional()
        {
            Assert.IsNull(LevelParser.ParseLevel(TestLevels.Json()).OptimalMoves);
        }

        [Test]
        public void UnknownExtraFields_AreIgnored()
        {
            // Forward compatibility: newer Slidewright versions may add fields.
            var json = TestLevels.Json().Replace("\"name\"", "\"author\":\"x\",\"name\"");
            Assert.DoesNotThrow(() => LevelParser.ParseLevel(json));
        }

        [Test]
        public void Pack_AsObjectWithLevelsArray_Parses()
        {
            var pack = LevelParser.ParsePack("{\"name\":\"P\",\"levels\":[" + TestLevels.Json() + "," + TestLevels.Json() + "]}");
            Assert.AreEqual(2, pack.Count);
        }

        [Test]
        public void Pack_AsBareArray_Parses()
        {
            Assert.AreEqual(1, LevelParser.ParsePack("[" + TestLevels.Json() + "]").Count);
        }

        [Test]
        public void Pack_AsSingleLevel_Parses()
        {
            Assert.AreEqual(1, LevelParser.ParsePack(TestLevels.Json()).Count);
        }

        [Test]
        public void MalformedJson_IsRejected()
        {
            var json = TestLevels.Json().TrimEnd('}'); // truncated file, e.g. a bad copy/paste or partial download
            var ex = Assert.Throws<LevelFormatException>(() => LevelParser.ParseLevel(json));
            Assert.AreEqual(LevelErrorCode.MalformedJson, ex.Code);
        }

        [Test]
        public void EmptyFile_IsRejectedAsMalformed()
        {
            var ex = Assert.Throws<LevelFormatException>(() => LevelParser.ParseLevel("   "));
            Assert.AreEqual(LevelErrorCode.MalformedJson, ex.Code);
        }

        [Test]
        public void MissingRequiredField_IsRejected()
        {
            var json = TestLevels.Json(grid: "{\"width\":6}"); // no height
            var ex = Assert.Throws<LevelFormatException>(() => LevelParser.ParseLevel(json));
            Assert.AreEqual(LevelErrorCode.MissingField, ex.Code);
            StringAssert.Contains("height", ex.Message, "the message must say which field is missing");
        }

        [Test]
        public void WrongFieldType_IsRejected()
        {
            var json = TestLevels.Json("[{\"id\":\"R\",\"x\":\"zero\",\"y\":2,\"length\":2,\"orientation\":\"H\"}]");
            var ex = Assert.Throws<LevelFormatException>(() => LevelParser.ParseLevel(json));
            Assert.AreEqual(LevelErrorCode.InvalidValue, ex.Code);
        }

        [Test]
        public void UnknownOrientation_IsRejected()
        {
            var json = TestLevels.Json("[{\"id\":\"R\",\"x\":0,\"y\":2,\"length\":2,\"orientation\":\"D\"}]");
            var ex = Assert.Throws<LevelFormatException>(() => LevelParser.ParseLevel(json));
            Assert.AreEqual(LevelErrorCode.InvalidValue, ex.Code);
        }
    }
}
