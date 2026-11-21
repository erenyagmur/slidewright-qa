using System.Collections.Generic;

namespace SlidewrightQA.Logic
{
    /// <summary>Result of a solver run.</summary>
    public sealed class SolverResult
    {
        public bool Solvable { get; }
        /// <summary>The optimal move sequence (empty if unsolvable or already solved).</summary>
        public IReadOnlyList<Move> Moves { get; }
        public int MoveCount => Moves.Count;
        /// <summary>How many distinct board states were visited; useful as a difficulty / perf metric.</summary>
        public int StatesExplored { get; }
        /// <summary>True if the search hit its state limit before finishing (result is inconclusive).</summary>
        public bool HitStateLimit { get; }

        public SolverResult(bool solvable, IReadOnlyList<Move> moves, int statesExplored, bool hitStateLimit)
        {
            Solvable = solvable;
            Moves = moves;
            StatesExplored = statesExplored;
            HitStateLimit = hitStateLimit;
        }
    }

    /// <summary>
    /// Breadth-first search over board states.
    ///
    /// Why BFS: every move costs 1 (a slide of any distance is one move), so the first time BFS
    /// reaches a solved state it has found a solution with the fewest possible moves. That gives
    /// us an "optimal move count" we can assert on, and a guaranteed-correct move list the
    /// PlayMode bot can replay.
    /// </summary>
    public static class Solver
    {
        public const int DefaultMaxStates = 2_000_000;

        public static SolverResult Solve(LevelData level, int maxStates = DefaultMaxStates) =>
            Solve(new Board(level), maxStates);

        public static SolverResult Solve(Board start, int maxStates = DefaultMaxStates)
        {
            if (start.IsSolved)
                return new SolverResult(true, new Move[0], 1, false);

            // parent[stateKey] = (previous state key, move that led here). Also acts as the visited set.
            var parent = new Dictionary<string, (string Prev, Move Move)> { [start.StateKey()] = (null, default) };
            var queue = new Queue<Board>();
            queue.Enqueue(start.Clone());

            while (queue.Count > 0)
            {
                var board = queue.Dequeue();
                var key = board.StateKey();

                for (var i = 0; i < board.VehicleCount; i++)
                {
                    var (min, max) = board.GetSlideRange(i);
                    for (var delta = min; delta <= max; delta++)
                    {
                        if (delta == 0) continue;

                        var next = board.Clone();
                        next.TryMove(i, delta);
                        var nextKey = next.StateKey();
                        if (parent.ContainsKey(nextKey)) continue;

                        var move = new Move(board.GetVehicle(i).Id, delta);
                        parent[nextKey] = (key, move);

                        if (next.IsSolved)
                            return new SolverResult(true, Reconstruct(parent, nextKey), parent.Count, false);

                        if (parent.Count >= maxStates)
                            return new SolverResult(false, new Move[0], parent.Count, true);

                        queue.Enqueue(next);
                    }
                }
            }

            // Every reachable state was explored and none was solved.
            return new SolverResult(false, new Move[0], parent.Count, false);
        }

        private static List<Move> Reconstruct(Dictionary<string, (string Prev, Move Move)> parent, string endKey)
        {
            var moves = new List<Move>();
            var key = endKey;
            while (parent[key].Prev != null)
            {
                moves.Add(parent[key].Move);
                key = parent[key].Prev;
            }
            moves.Reverse();
            return moves;
        }
    }
}
