//ş
using System;
using System.IO;
using System.Linq;

namespace MsSqlDataDumpScriptSplitter;

internal class Program {
    private const Int32 sizeOfByteBag = 8;
    private static readonly UInt64 LowerCaseGo = ToBagOfBytes(0x67, 0x00, 0x6F, 0x00, 0x0D, 0x00, 0x0A, 0x00);
    private static readonly UInt64 UpperCaseGo = ToBagOfBytes(0x47, 0x00, 0x4F, 0x00, 0x0D, 0x00, 0x0A, 0x00);
    private static readonly Byte[] UTF16LE_BOM = { 0xFF, 0xFE };
    private static readonly Byte[] SetAnsiNullsAndQuotedIdentifierOnGo = {
        0x53, 0x00, 0x45, 0x00, 0x54, 0x00, 0x20, 0x00,
        0x41, 0x00, 0x4e, 0x00, 0x53, 0x00, 0x49, 0x00,
        0x5f, 0x00, 0x4e, 0x00, 0x55, 0x00, 0x4c, 0x00,
        0x4c, 0x00, 0x53, 0x00, 0x20, 0x00, 0x4f, 0x00,
        0x4e, 0x00, 0x0d, 0x00, 0x0a, 0x00, 0x47, 0x00,
        0x4f, 0x00, 0x0d, 0x00, 0x0a, 0x00, 0x53, 0x00,
        0x45, 0x00, 0x54, 0x00, 0x20, 0x00, 0x51, 0x00,
        0x55, 0x00, 0x4f, 0x00, 0x54, 0x00, 0x45, 0x00,
        0x44, 0x00, 0x5f, 0x00, 0x49, 0x00, 0x44, 0x00,
        0x45, 0x00, 0x4e, 0x00, 0x54, 0x00, 0x49, 0x00,
        0x46, 0x00, 0x49, 0x00, 0x45, 0x00, 0x52, 0x00,
        0x20, 0x00, 0x4f, 0x00, 0x4e, 0x00, 0x0d, 0x00,
        0x0a, 0x00, 0x47, 0x00, 0x4f, 0x00, 0x0d, 0x00,
        0x0a, 0x00, 0x0d, 0x00, 0x0a, 0x00
    };

    private static void Main (String[] args) {
        if (args.Length < 2) {
            Console.WriteLine("Usage: MsSqlDataDumpScriptSplitter.exe <pathToInputFile> <pathToOutputDir> [fileSizeLimitMB (defaults to 1024 MB)]");
            return;
        }

        var pathToInputFile = new FileInfo(Path.GetFullPath(args[0]));
        var pathToOutputDir = new DirectoryInfo(Path.GetFullPath(args[1]));
        var fileSizeLimitMB = args.Length < 3 ? 1024 : Int32.Parse(args[2]);
        var fileSizeLimitInBytes = fileSizeLimitMB * 1024L * 1024L;
        var inputFileNameWithoutExtension = Path.GetFileNameWithoutExtension(pathToInputFile.Name);
        var inputFileExtension = pathToInputFile.Extension;
        Int32 fileIndexDigitSize;
        String formatStr = null;
        String createPathToOutputFile (Int32 ix) => Path.Combine(
            pathToOutputDir.FullName,
            $"{inputFileNameWithoutExtension}_{ix.ToString(formatStr ??= "0".PadLeft(fileIndexDigitSize, '0'))}{inputFileExtension}"
        );
        var outputFileIx = 0;
        using var reader = pathToInputFile.OpenRead();
        FileStream writer = null;
        Int64 totalBytesRead = UTF16LE_BOM.Length;
        Int32 byteRead;
        var currentBagOfBytes = 0ul;
        var iiStateMachine = new StateMachines.IdentityInsertStateMachine();

        Directory.CreateDirectory(pathToOutputDir.FullName);
        fileIndexDigitSize = GetNumOfDigits((Int32) Math.Ceiling(reader.Length / (Double) fileSizeLimitInBytes));
        reader.Seek(UTF16LE_BOM.Length, SeekOrigin.Begin); // skip BOM

        while ((byteRead = reader.ReadByte()) != -1) {
            if (writer == null) {
                writer = File.OpenWrite(createPathToOutputFile(outputFileIx++));
                writer.Write(UTF16LE_BOM, 0, UTF16LE_BOM.Length);
                writer.Write(SetAnsiNullsAndQuotedIdentifierOnGo, 0, SetAnsiNullsAndQuotedIdentifierOnGo.Length);

                if (iiStateMachine.HasIdentityInsertTable) {
                    var setIdentityInsertOn = iiStateMachine.CreateStatement();
                    writer.Write(setIdentityInsertOn, 0, setIdentityInsertOn.Length);
                }
            }

            var byteValue = (Byte) byteRead;
            currentBagOfBytes = (currentBagOfBytes << 8) | byteValue;
            iiStateMachine.ProcessValue(byteValue);
            writer.WriteByte(byteValue);

            if (++totalBytesRead >= fileSizeLimitInBytes && IsGo(currentBagOfBytes)) {
                writer.Dispose();
                writer = null;
                totalBytesRead = UTF16LE_BOM.Length;
                currentBagOfBytes = 0ul;
            }
        }

        writer?.Dispose();
    }

    private static Int32 GetNumOfDigits (Int32 number) {
        var numOfDigits = 0;

        do
            numOfDigits++;
        while ((number /= 10) != 0);

        return numOfDigits;
    }

    private static UInt64 ToBagOfBytes (params Byte[] bytes) {
        if (bytes.Length != sizeOfByteBag) {
            throw new ArgumentException("nope", nameof(bytes));
        }

        return bytes
            .Select((b, ix) => ((UInt64) b) << (8 * (sizeOfByteBag - 1 - ix)))
            .Aggregate(0ul, (acc, cur) => acc | cur);
    }

    private static Boolean IsGo (UInt64 bagOfBytes) => bagOfBytes == LowerCaseGo || bagOfBytes == UpperCaseGo;
}
