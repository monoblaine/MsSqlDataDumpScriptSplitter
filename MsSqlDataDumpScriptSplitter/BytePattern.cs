//ş
using System;
using System.Collections.Generic;

namespace MsSqlDataDumpScriptSplitter;

internal class BytePattern<TState> where TState : Enum {
    public BytePattern (TState state, Byte[] pattern, Int32 expectedExistingByteCount) {
        State = state;
        Pattern = pattern;
        ExpectedExistingByteCount = expectedExistingByteCount;
        ExpectedByteCountOnFullCapture = expectedExistingByteCount + pattern.Length;
    }

    private TState State { get; }

    public Byte[] Pattern { get; }

    private Int32 ExpectedExistingByteCount { get; }

    public Int32 ExpectedByteCountOnFullCapture { get; }

    private Boolean StartsWith (Byte value) => value == Pattern[0];

    public Boolean TryStartCapturing (List<Byte> capturedBytes, Byte value, ref TState currentState) {
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

    public Boolean TryCaptureNext (List<Byte> capturedBytes, Byte value, ref TState currentState) {
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
