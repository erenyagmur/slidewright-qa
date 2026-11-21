using System;

namespace SlidewrightQA.Logic
{
    /// <summary>
    /// One play-through of one level: the board plus the move counter and restart.
    ///
    /// This is the single entry point for moves in the game. The MonoBehaviour input code AND the
    /// PlayMode test bot both call <see cref="TryMove"/>, so the bot exercises the same path a
    /// player does.
    /// </summary>
    public sealed class GameSession
    {
        public LevelData Level { get; }
        public Board Board { get; private set; }
        public int MoveCount { get; private set; }
        public bool IsWon => Board.IsSolved;

        /// <summary>Raised after every successful move.</summary>
        public event Action<Move> Moved;
        /// <summary>Raised once, on the move that solves the level.</summary>
        public event Action Won;
        /// <summary>Raised after <see cref="Restart"/>.</summary>
        public event Action Restarted;

        public GameSession(LevelData level)
        {
            Level = level;
            Board = new Board(level);
        }

        /// <summary>
        /// Attempts a move. Illegal moves and moves after the level is won are rejected and do not
        /// count towards the move counter.
        /// </summary>
        public MoveResult TryMove(string vehicleId, int delta)
        {
            if (IsWon) return MoveResult.Blocked;

            var result = Board.TryMove(vehicleId, delta);
            if (result != MoveResult.Ok) return result;

            MoveCount++;
            Moved?.Invoke(new Move(vehicleId, delta));
            if (Board.IsSolved) Won?.Invoke();
            return result;
        }

        public MoveResult TryMove(Move move) => TryMove(move.VehicleId, move.Delta);

        /// <summary>Puts every vehicle back to its starting cell and resets the counter.</summary>
        public void Restart()
        {
            Board = new Board(Level);
            MoveCount = 0;
            Restarted?.Invoke();
        }
    }
}
