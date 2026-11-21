namespace SlidewrightQA.Logic
{
    /// <summary>Every reason a level file can be rejected. Each one has its own unit test.</summary>
    public enum LevelErrorCode
    {
        // Parse errors (the file itself is broken)
        MalformedJson,
        MissingField,
        InvalidValue,

        // Validation errors (the file is readable but the level is not playable)
        InvalidGrid,
        UnsupportedExit,
        InvalidVehicleLength,
        DuplicateVehicleId,
        OutOfBounds,
        Overlap,
        MissingRedCar,
        MisplacedRedCar
    }

    /// <summary>One problem found in a level, with a human-readable message for test reports.</summary>
    public sealed class LevelError
    {
        public LevelErrorCode Code { get; }
        public string Message { get; }

        public LevelError(LevelErrorCode code, string message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString() => $"{Code}: {Message}";
    }
}
