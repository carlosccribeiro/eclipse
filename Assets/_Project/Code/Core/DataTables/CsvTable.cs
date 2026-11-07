using System;
using System.Collections.Generic;
using System.Text;

namespace Eclipse.Core.DataTables
{
    /// <summary>Thrown for malformed CSV or invalid cell values; message carries line and column.</summary>
    public sealed class CsvFormatException : Exception
    {
        public int Line { get; }
        public string Column { get; }

        public CsvFormatException(string message, int line, string column = null)
            : base(Format(message, line, column))
        {
            Line = line;
            Column = column;
        }

        private static string Format(string message, int line, string column)
        {
            string where = column == null ? "line " + line : "line " + line + ", column '" + column + "'";
            return where + ": " + message;
        }
    }

    /// <summary>RFC 4180 CSV table with a mandatory header row.</summary>
    public sealed class CsvTable
    {
        private readonly string[] _columns;
        private readonly Dictionary<string, int> _index;
        private readonly List<CsvRow> _rows;

        private CsvTable(string[] columns, Dictionary<string, int> index, List<CsvRow> rows)
        {
            _columns = columns;
            _index = index;
            _rows = rows;
        }

        public IReadOnlyList<string> Columns => _columns;
        public IReadOnlyList<CsvRow> Rows => _rows;

        public bool HasColumn(string name) => name != null && _index.ContainsKey(name);

        /// <summary>Returns the column index, or -1 if absent.</summary>
        public int IndexOf(string name) => name != null && _index.TryGetValue(name, out int i) ? i : -1;

        public static CsvTable Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var records = ReadRecords(text);

            if (records.Count == 0)
                throw new CsvFormatException("header row is missing or empty", 1);

            var header = records[0].Fields;
            int headerLine = records[0].Line;
            var columns = new string[header.Count];
            var index = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < header.Count; i++)
            {
                string name = header[i].Trim();
                if (name.Length == 0)
                    throw new CsvFormatException("empty column name at position " + (i + 1), headerLine);
                if (index.ContainsKey(name))
                    throw new CsvFormatException("duplicate column name '" + name + "'", headerLine, name);
                columns[i] = name;
                index.Add(name, i);
            }

            var rows = new List<CsvRow>(records.Count - 1);
            for (int r = 1; r < records.Count; r++)
            {
                var rec = records[r];
                if (rec.Fields.Count > columns.Length)
                    throw new CsvFormatException(
                        "row has " + rec.Fields.Count + " cells but header has " + columns.Length, rec.Line);
                var cells = new string[columns.Length];
                for (int c = 0; c < cells.Length; c++)
                    cells[c] = c < rec.Fields.Count ? rec.Fields[c] : string.Empty;
                rows.Add(new CsvRow(cells, index, rec.Line));
            }

            return new CsvTable(columns, index, rows);
        }

        private struct Record
        {
            public List<string> Fields;
            public int Line;
        }

        private static List<Record> ReadRecords(string text)
        {
            var result = new List<Record>();
            int pos = 0;
            if (text.Length > 0 && text[0] == '﻿') pos = 1;

            int line = 1;
            var fields = new List<string>();
            var sb = new StringBuilder();
            int recordLine = 1;
            bool anyContent = false; // record has a delimiter or non-empty/quoted field

            void EndField()
            {
                fields.Add(sb.ToString());
                sb.Length = 0;
            }

            void EndRecord()
            {
                EndField();
                bool blank = !anyContent && fields.Count == 1 && fields[0].Length == 0;
                if (!blank)
                    result.Add(new Record { Fields = fields, Line = recordLine });
                fields = new List<string>();
                anyContent = false;
            }

            int n = text.Length;
            while (pos < n)
            {
                char ch = text[pos];
                if (ch == '"' && sb.Length == 0 && !anyFieldStarted(fields, sb))
                {
                    // quoted field
                    anyContent = true;
                    int startLine = line;
                    pos++;
                    bool closed = false;
                    while (pos < n)
                    {
                        char q = text[pos];
                        if (q == '"')
                        {
                            if (pos + 1 < n && text[pos + 1] == '"') { sb.Append('"'); pos += 2; continue; }
                            pos++;
                            closed = true;
                            break;
                        }
                        if (q == '\n') line++;
                        sb.Append(q);
                        pos++;
                    }
                    if (!closed)
                        throw new CsvFormatException("unterminated quoted field", startLine);
                    if (pos < n && text[pos] != ',' && text[pos] != '\r' && text[pos] != '\n')
                        throw new CsvFormatException("unexpected character '" + text[pos] + "' after closing quote", line);
                    continue;
                }
                if (ch == '"')
                    throw new CsvFormatException("unexpected quote inside unquoted field", line);
                if (ch == ',')
                {
                    anyContent = true;
                    EndField();
                    pos++;
                    continue;
                }
                if (ch == '\r' || ch == '\n')
                {
                    if (ch == '\r' && pos + 1 < n && text[pos + 1] == '\n') pos++;
                    pos++;
                    EndRecord();
                    line++;
                    recordLine = line;
                    continue;
                }
                sb.Append(ch);
                anyContent = true;
                pos++;
            }

            if (sb.Length > 0 || anyContent || fields.Count > 0)
                EndRecord();

            return result;
        }

        // A quote only opens a quoted field at the very start of a field.
        private static bool anyFieldStarted(List<string> fields, StringBuilder sb) => sb.Length > 0;
    }
}
