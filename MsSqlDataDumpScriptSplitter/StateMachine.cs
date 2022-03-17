//ş
using System;
using System.Collections.Generic;

namespace MsSqlDataDumpScriptSplitter;

internal abstract class StateMachine<TState> where TState : Enum {
    public TState CurrentState;

    protected StateMachine () {
        CurrentState = default;
    }

    protected List<Byte> CapturedBytes { get; } = new();

    public abstract void ProcessValue (Byte value);
}
