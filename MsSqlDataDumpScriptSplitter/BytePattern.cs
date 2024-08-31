namespace MsSqlDataDumpScriptSplitter;

internal class BytePattern<TState> (TState state, Byte[] pattern, Int32 expectedExistingByteCount) where TState : Enum {
    private TState State { get; } = state;

    public Byte[] Pattern { get; } = pattern;

    private Int32 ExpectedExistingByteCount { get; } = expectedExistingByteCount;

    public Int32 ExpectedByteCountOnFullCapture { get; } = expectedExistingByteCount + pattern.Length;

    private Boolean StartsWith (Byte value) => value == Pattern[0];

    public Boolean TryStartCapturing (List<Byte> capturedBytes, Byte value, ref TState? currentState) {
        var success = StartsWith(value);
        if (success) {
            currentState = State;
            if (ExpectedExistingByteCount == 0) {
                capturedBytes.Clear();
            }
            capturedBytes.Add(value);
        }
        return success;
    }

    public Boolean TryCaptureNext (List<Byte> capturedBytes, Byte value, ref TState? currentState) {
        var isExpectedValue = value == Pattern[capturedBytes.Count - ExpectedExistingByteCount];
        if (isExpectedValue) {
            capturedBytes.Add(value);
        }
        else {
            currentState = default;
        }
        return isExpectedValue;
    }

    public Boolean IsFullyCaptured (List<Byte> capturedBytes) => capturedBytes.Count == ExpectedByteCountOnFullCapture;
}
