using System.Data;

namespace GPlus.Base.Extensions
{
    public static class DataTableExtensions
    {
        public static DataTable CopyToDataTableOrEmpty(this IEnumerable<DataRow> source)
        {
            if (source.Any())
                return source.CopyToDataTable();
            else
                return source.FirstOrDefault()?.Table.Clone() ?? new DataTable();
        }
    }
}
