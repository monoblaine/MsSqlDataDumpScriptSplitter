//ş
using System;
using System.Collections.Generic;
using State = MsSqlDataDumpScriptSplitter.StateMachines.States.IdentityInsert;

namespace MsSqlDataDumpScriptSplitter.StateMachines;

internal class IdentityInsertStateMachine : StateMachine<State> {
    private static readonly BytePattern<State> Begin = new(
        state: State.Begin,
        pattern: new Byte[] {
            // `SET IDENTITY_INSERT [dbo].[`
            0x53, 0x00, 0x45, 0x00, 0x54, 0x00, 0x20, 0x00,
            0x49, 0x00, 0x44, 0x00, 0x45, 0x00, 0x4E, 0x00,
            0x54, 0x00, 0x49, 0x00, 0x54, 0x00, 0x59, 0x00,
            0x5F, 0x00, 0x49, 0x00, 0x4E, 0x00, 0x53, 0x00,
            0x45, 0x00, 0x52, 0x00, 0x54, 0x00, 0x20, 0x00,
            0x5B, 0x00, 0x64, 0x00, 0x62, 0x00, 0x6F, 0x00,
            0x5D, 0x00, 0x2E, 0x00, 0x5B, 0x00
        },
        expectedExistingByteCount: 0
    );

    private static readonly BytePattern<State> AfterTableName = new(
        state: State.AfterTableName,
        pattern: new Byte[] {
            // `] O`
            0x5D, 0x00, 0x20, 0x00, 0x4F, 0x00
        },
        expectedExistingByteCount: Begin.ExpectedByteCountOnFullCapture
    );

    private static readonly BytePattern<State> EndByOn = new(
        state: State.EndByOn,
        pattern: new Byte[] {
            /*
             * `N \r\n`
             */
            0x4E, 0x00, 0x20, 0x00, 0x0D, 0x00, 0x0A, 0x00
        },
        expectedExistingByteCount: AfterTableName.ExpectedByteCountOnFullCapture
    );

    private static readonly BytePattern<State> EndByOffGo = new(
        state: State.EndByOffGo,
        pattern: new Byte[] {
            // `FF\r\nGO\r\n`
            0x46, 0x00, 0x46, 0x00, 0x0D, 0x00, 0x0A, 0x00,
            0x47, 0x00, 0x4F, 0x00, 0x0D, 0x00, 0x0A, 0x00
        },
        expectedExistingByteCount: AfterTableName.ExpectedByteCountOnFullCapture
    );

    private readonly List<Byte> currentIdentityInsertTable = new();
    private readonly List<Byte> tmpIdentityInsertTable = new();

    public override void ProcessValue (Byte value) {
        switch (CurrentState) {
            case State.None:
                Begin.TryStartCapturing(CapturedBytes, value, ref CurrentState);
                break;

            case State.Begin:
                if (Begin.IsFullyCaptured(CapturedBytes)) {
                    tmpIdentityInsertTable.Clear();
                    tmpIdentityInsertTable.Add(value);
                    CurrentState = State.TableName;
                }
                else {
                    Begin.TryCaptureNext(CapturedBytes, value, ref CurrentState);
                }
                break;

            case State.TableName:
                if (!AfterTableName.TryStartCapturing(CapturedBytes, value, ref CurrentState)) {
                    tmpIdentityInsertTable.Add(value);
                }
                break;

            case State.AfterTableName:
                if (AfterTableName.IsFullyCaptured(CapturedBytes)) {
                    if (
                        !EndByOn.TryStartCapturing(CapturedBytes, value, ref CurrentState) &&
                        !EndByOffGo.TryStartCapturing(CapturedBytes, value, ref CurrentState)
                    ) {
                        CurrentState = default;
                    }
                }
                else {
                    AfterTableName.TryCaptureNext(CapturedBytes, value, ref CurrentState);
                }
                break;

            case State.EndByOn:
                if (EndByOn.IsFullyCaptured(CapturedBytes)) {
                    currentIdentityInsertTable.Clear();
                    currentIdentityInsertTable.AddRange(tmpIdentityInsertTable);
                    tmpIdentityInsertTable.Clear();
                    CurrentState = default;
                    Console.WriteLine($"set identity_insert [dbo].[{System.Text.Encoding.Unicode.GetString(currentIdentityInsertTable.ToArray())}] on");
                    goto case State.None;
                }
                else {
                    EndByOn.TryCaptureNext(CapturedBytes, value, ref CurrentState);
                }
                break;

            case State.EndByOffGo:
                if (EndByOffGo.IsFullyCaptured(CapturedBytes)) {
                    Console.WriteLine($"set identity_insert [dbo].[{System.Text.Encoding.Unicode.GetString(tmpIdentityInsertTable.ToArray())}] off");
                    currentIdentityInsertTable.Clear();
                    CurrentState = default;
                    goto case State.None;
                }
                else {
                    EndByOffGo.TryCaptureNext(CapturedBytes, value, ref CurrentState);
                }
                break;

            default:
                throw new NotImplementedException();
        }
    }

    public Boolean HasIdentityInsertTable => currentIdentityInsertTable.Count > 0;

    public Byte[] CreateStatement () {
        var statement = new List<Byte>(
            Begin.Pattern.Length +
            currentIdentityInsertTable.Count +
            AfterTableName.Pattern.Length +
            EndByOn.Pattern.Length
        );

        statement.AddRange(Begin.Pattern);
        statement.AddRange(currentIdentityInsertTable);
        statement.AddRange(AfterTableName.Pattern);
        statement.AddRange(EndByOn.Pattern);

        return statement.ToArray();
    }
}
