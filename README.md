# SQL Server Data Dump Script Splitter

This tool splits your huge SQL Server data dump scripts into smaller/executable chunks sqlcmd can handle.

Usage:

```
mssql_data_dump_script_splitter.exe <pathToInputFile> <pathToOutputDir> [fileSizeLimitMB (defaults to 1024 MB)]
```

The reason for this tool to exist is that [sqlcmd](https://docs.microsoft.com/en-us/sql/tools/sqlcmd-utility) cannot handle files with size over 2 GBs (AFAIK). This tool splits the dump file by simultaneously reading and writing one byte at a time and when the number of bytes read exceeds the given limit it continues on writing to the current chunk until it finds a batch separator (Hard coded as `GO`).

## Features

* Tracks `set identity_insert ... on/off` statements and if `insert` statements span multiple files, `set identity_insert ... on` gets inserted to the beginning of the next file as needed.
* Blindly adds `set ansi_nulls on` and `set quoted_identifier on` to the start of each file.

## Assumptions

* Batch separator is assumed to be `GO`
* Dump file encoding is assumed to be UTF16 LE BOM

## Executing The Splitted Files

If you're executing the scripts against an existing database I strongly suggest you use `set nocount on` before you start by using the following command:

```sql
exec sp_configure 'user options' 512
reconfigure
```

You can use one of the scripts below to execute all the files sequentially.

```bat
:: Windows batch file
for %%G in (*.sql) do sqlcmd -S <server name> -d <database name> -E -i "%%G"
```

or

```sh
#!/bin/bash

for file_name in ./*.sql; do
    sqlcmd -S <server name> -d <database name> -E -i $file_name
done
```
