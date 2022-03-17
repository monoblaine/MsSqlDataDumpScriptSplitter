//ş
using System;

namespace MsSqlDataDumpScriptSplitter.StateMachines.States;

public enum IdentityInsert : Byte {
    None,
    Begin,
    TableName,
    AfterTableName,
    EndByOn,
    EndByOffGo
}
