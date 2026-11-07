using Eclipse.Core.DataTables;

namespace Eclipse.Editor.DataPipeline
{
    /// <summary>Imports one CSV table into definition assets.</summary>
    public interface ITableImporter
    {
        /// <summary>Table name used in messages and summaries.</summary>
        string TableName { get; }

        /// <summary>CSV path relative to the DataSource folder (e.g. "Affinities.csv").</summary>
        string CsvPath { get; }

        /// <summary>Asset folder (Unity path, e.g. "Assets/_Project/Content/Affinities").</summary>
        string OutputFolder { get; }

        void Import(CsvTable table, ImportContext context);
    }
}
