using System;
using System.Collections.Generic;
using System.Globalization;

namespace Eclipse.Core.DataTables
{
    /// <summary>One data row of a <see cref="CsvTable"/>. Numbers use the invariant culture.</summary>
    public sealed class CsvRow
    {
        private readonly string[] _cells;
        private readonly Dictionary<string, int> _index;

        internal CsvRow(string[] cells, Dictionary<string, int> index, int line)
        {
            _cells = cells;
            _index = index;
            Line = line;
        }

        /// <summary>1-based line number where this row starts in the source text.</summary>
        public int Line { get; }

        public bool HasColumn(string col) => col != null && _index.ContainsKey(col);

        /// <summary>True if the cell is empty or whitespace-only.</summary>
        public bool IsEmpty(string col) => Raw(col).Trim().Length == 0;

        private string Raw(string col)
        {
            if (col == null) throw new ArgumentNullException(nameof(col));
            if (!_index.TryGetValue(col, out int i))
                throw new CsvFormatException("column '" + col + "' does not exist", Line, col);
            return _cells[i];
        }

        private string Required(string col)
        {
            string v = Raw(col).Trim();
            if (v.Length == 0)
                throw new CsvFormatException("required value is empty", Line, col);
            return v;
        }

        // ---- string ----
        public string GetString(string col) => Required(col);
        public string GetString(string col, string defaultValue) => IsEmpty(col) ? defaultValue : Raw(col).Trim();

        // ---- int ----
        public int GetInt(string col) => ParseInt(col, Required(col));
        public int GetInt(string col, int defaultValue) => IsEmpty(col) ? defaultValue : ParseInt(col, Raw(col).Trim());

        private int ParseInt(string col, string v)
        {
            if (int.TryParse(v, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int r)) return r;
            throw new CsvFormatException("invalid integer '" + v + "'", Line, col);
        }

        // ---- float ----
        public float GetFloat(string col) => ParseFloat(col, Required(col));
        public float GetFloat(string col, float defaultValue) => IsEmpty(col) ? defaultValue : ParseFloat(col, Raw(col).Trim());

        private float ParseFloat(string col, string v)
        {
            const NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
            if (float.TryParse(v, styles, CultureInfo.InvariantCulture, out float r)
                && !float.IsNaN(r) && !float.IsInfinity(r)) return r;
            throw new CsvFormatException("invalid number '" + v + "'", Line, col);
        }

        // ---- bool ----
        public bool GetBool(string col) => ParseBool(col, Required(col));
        public bool GetBool(string col, bool defaultValue) => IsEmpty(col) ? defaultValue : ParseBool(col, Raw(col).Trim());

        private bool ParseBool(string col, string v)
        {
            if (string.Equals(v, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(v, "false", StringComparison.OrdinalIgnoreCase)) return false;
            throw new CsvFormatException("invalid boolean '" + v + "' (expected true or false)", Line, col);
        }

        // ---- enum ----
        public T GetEnum<T>(string col) where T : struct, Enum => ParseEnum<T>(col, Required(col));
        public T GetEnum<T>(string col, T defaultValue) where T : struct, Enum =>
            IsEmpty(col) ? defaultValue : ParseEnum<T>(col, Raw(col).Trim());

        private T ParseEnum<T>(string col, string v) where T : struct, Enum
        {
            // Names only: reject numeric strings so the CSV stays readable and stable.
            if (!char.IsDigit(v[0]) && v[0] != '-' && v[0] != '+'
                && Enum.TryParse(v, false, out T r) && Enum.IsDefined(typeof(T), r)) return r;
            throw new CsvFormatException(
                "unknown " + typeof(T).Name + " '" + v + "' (valid: " + string.Join(", ", Enum.GetNames(typeof(T))) + ")",
                Line, col);
        }

        // ---- list ----
        /// <summary>Splits on '|', trims items; empty cell gives an empty list.</summary>
        public List<string> GetList(string col)
        {
            var list = new List<string>();
            string v = Raw(col);
            if (v.Trim().Length == 0) return list;
            foreach (string part in v.Split('|'))
            {
                string t = part.Trim();
                if (t.Length > 0) list.Add(t);
            }
            return list;
        }
    }
}
