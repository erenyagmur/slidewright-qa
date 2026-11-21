using System;

namespace SlidewrightQA.Logic
{
    /// <summary>Thrown by <see cref="LevelParser"/> when a level file can't be read at all.</summary>
    public sealed class LevelFormatException : Exception
    {
        public LevelErrorCode Code { get; }

        public LevelFormatException(LevelErrorCode code, string message, Exception inner = null)
            : base(message, inner)
        {
            Code = code;
        }
    }
}
