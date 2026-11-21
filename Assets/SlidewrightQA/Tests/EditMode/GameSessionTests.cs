using NUnit.Framework;
using SlidewrightQA.Logic;

namespace SlidewrightQA.Tests.EditMode
{
    /// <summary>
    /// Move counter, win event and restart: the game-facing layer on top of the board rules.
    /// </summary>
    [TestFixture]
    public class GameSessionTests
    {
        private static GameSession MakeSession() => new GameSession(TestLevels.FromAscii(
            "......",
            "...A..",
            "RR.A..",
            "......",
            "......",
            "......"));

        [Test]
        public void LegalMove_IncrementsMoveCounter()
        {
            var session = MakeSession();
            session.TryMove("A", 2);
            Assert.AreEqual(1, session.MoveCount);
        }

        [Test]
        public void IllegalMove_DoesNotIncrementMoveCounter()
        {
            var session = MakeSession();
            session.TryMove("R", 4);  // blocked by A
            session.TryMove("R", -1); // edge
            Assert.AreEqual(0, session.MoveCount);
        }

        [Test]
        public void WinningMove_RaisesWonExactlyOnce()
        {
            var session = MakeSession();
            var wonCount = 0;
            session.Won += () => wonCount++;

            session.TryMove("A", 2);
            Assert.AreEqual(0, wonCount);
            session.TryMove("R", 4);
            Assert.AreEqual(1, wonCount);
            Assert.IsTrue(session.IsWon);
        }

        [Test]
        public void MovesAfterWin_AreRejected()
        {
            var session = MakeSession();
            session.TryMove("A", 2);
            session.TryMove("R", 4);
            Assert.AreNotEqual(MoveResult.Ok, session.TryMove("R", -1));
            Assert.AreEqual(2, session.MoveCount);
        }

        [Test]
        public void Restart_ResetsBoardAndCounter()
        {
            var session = MakeSession();
            var start = session.Board.StateKey();
            session.TryMove("A", 2);
            session.TryMove("R", 1);

            session.Restart();

            Assert.AreEqual(0, session.MoveCount);
            Assert.AreEqual(start, session.Board.StateKey());
            Assert.IsFalse(session.IsWon);
        }
    }
}
