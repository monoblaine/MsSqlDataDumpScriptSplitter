namespace MsSqlDataDumpScriptSplitter;

internal abstract class StateMachine<TState> where TState : Enum {
    public TState? CurrentState;

    protected StateMachine () => CurrentState = default;

    protected List<Byte> CapturedBytes { get; } = [];

    public abstract void ProcessValue (Byte value);
}
