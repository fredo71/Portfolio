using System.Globalization;
using GameStatsGen.Models;

namespace GameStatsGen;

public sealed class CsvParseException(string message) : Exception(message);

public static class CsvParser
{
    static readonly string[] RequiredColumns =
    [
        "frame_index", "elapsed_s", "frame_time_ms", "total_memory",
        "gfx_driver_bytes", "visible_chunks", "shadow_groups", "chunks_completed",
    ];

    public static FrameRow[] Parse(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
            throw new CsvParseException($"{path}: file is empty.");

        var header = lines[0].Split(',');
        var columnIndex = new Dictionary<string, int>();
        for (var i = 0; i < header.Length; i++) columnIndex[header[i]] = i;

        var missing = RequiredColumns.Where(c => !columnIndex.ContainsKey(c)).ToArray();
        if (missing.Length > 0)
        {
            throw new CsvParseException(
                $"{path}: header is missing required column(s): {string.Join(", ", missing)}.\n" +
                $"  found: {string.Join(",", header)}");
        }

        var rows = new FrameRow[lines.Length - 1];
        for (var i = 1; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var fields = lines[i].Split(',');
            if (fields.Length != header.Length)
            {
                throw new CsvParseException(
                    $"{path} line {lineNumber}: expected {header.Length} columns (matching the header), found {fields.Length}.");
            }

            rows[i - 1] = new FrameRow(
                FrameIndex: ParseInt(path, lineNumber, "frame_index", Field(fields, columnIndex, "frame_index")),
                ElapsedS: ParseDouble(path, lineNumber, "elapsed_s", Field(fields, columnIndex, "elapsed_s")),
                FrameTimeMs: ParseDouble(path, lineNumber, "frame_time_ms", Field(fields, columnIndex, "frame_time_ms")),
                TotalMemoryBytes: ParseLong(path, lineNumber, "total_memory", Field(fields, columnIndex, "total_memory")),
                GfxDriverBytes: ParseLong(path, lineNumber, "gfx_driver_bytes", Field(fields, columnIndex, "gfx_driver_bytes")),
                VisibleChunks: ParseInt(path, lineNumber, "visible_chunks", Field(fields, columnIndex, "visible_chunks")),
                ShadowGroups: ParseInt(path, lineNumber, "shadow_groups", Field(fields, columnIndex, "shadow_groups")),
                ChunksCompleted: ParseInt(path, lineNumber, "chunks_completed", Field(fields, columnIndex, "chunks_completed")));
        }

        return rows;
    }

    static string Field(string[] fields, Dictionary<string, int> columnIndex, string column) =>
        fields[columnIndex[column]];

    static int ParseInt(string path, int lineNumber, string column, string raw)
    {
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return value;
        throw ParseError(path, lineNumber, column, raw);
    }

    static long ParseLong(string path, int lineNumber, string column, string raw)
    {
        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return value;
        throw ParseError(path, lineNumber, column, raw);
    }

    static double ParseDouble(string path, int lineNumber, string column, string raw)
    {
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return value;
        throw ParseError(path, lineNumber, column, raw);
    }

    static CsvParseException ParseError(string path, int lineNumber, string column, string raw) =>
        new($"{path} line {lineNumber}: column '{column}' value '{raw}' is not a valid " +
            "invariant-culture number (check for a comma decimal separator).");
}
