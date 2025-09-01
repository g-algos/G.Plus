using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using System.Data;
using System.IO;

namespace GPlus.Base.Helpers
{
    public static class SpreadSheets
    {
        public static bool TryReadSheetTable(string filePath, string sheetName, out DataTable? dataTable)
        {
            dataTable = null;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null)
                return false;

            var table = worksheet.Tables.FirstOrDefault(t => t.Name == sheetName);
            if (table == null)
                return false;

  
            var metaSheet = package.Workbook.Worksheets[$"{sheetName}Meta"];
            if (metaSheet == null)
                return false;

            var addr = table.Address;
            dataTable = ToDataTable(worksheet, metaSheet, addr.Start.Row, addr.End.Row, addr.Start.Column, addr.End.Column);
            return true;
        }

        private static DataTable ToDataTable(ExcelWorksheet visibleSheet, ExcelWorksheet metaSheet, int startRow, int endRow, int startCol, int endCol)
        {
            var dt = new DataTable();

            for (int col = startCol; col <= endCol; col++)
            {
                var columnName = metaSheet.Cells[col, 2].Text;
                if (string.IsNullOrEmpty(columnName))
                {
                    columnName = visibleSheet.Cells[startRow, col].Text;
                }
                var caption = visibleSheet.Cells[startRow, col].Text;

                var column = new DataColumn(columnName)
                {
                    Caption = caption
                };
                dt.Columns.Add(column);
            }

            for (int row = startRow + 1; row <= endRow; row++)
            {
                var dataRow = dt.NewRow();
                for (int col = startCol; col <= endCol; col++)
                {
                    dataRow[col - startCol] = visibleSheet.Cells[row, col].Text;
                }
                dt.Rows.Add(dataRow);
            }

            return dt;
        }

        public static bool TryWriteTableToExcel(string filePath, string sheetName, DataTable table)
        {
            try
            {
                if (filePath == null) return false;
                FileInfo fileInfo = new FileInfo(filePath);
                using var package = fileInfo.Exists ? new ExcelPackage(fileInfo) : new ExcelPackage();

                var worksheet = package.Workbook.Worksheets[sheetName] ?? package.Workbook.Worksheets.Add(sheetName);
                var existingTable = worksheet.Tables[sheetName];
                if (existingTable != null)
                {
                    worksheet.Tables.Delete(existingTable);
                }
                worksheet.Cells.Clear();

                int numCols = table.Columns.Count;
                int numRows = table.Rows.Count;

                for (int col = 0; col < numCols; col++)
                {
                    var cell = worksheet.Cells[1, col + 1];
                    cell.Value = table.Columns[col].Caption ?? table.Columns[col].ColumnName;

                    if (table.Columns[col].ReadOnly)
                    {
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }
                }

                for (int row = 0; row < numRows; row++)
                {
                    for (int col = 0; col < numCols; col++)
                    {
                        var cell = worksheet.Cells[row + 2, col + 1];
                        cell.Value = table.Rows[row][col];

                        if (table.Columns[col].ReadOnly)
                        {
                            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        }
                    }
                }

                var metaSheet = package.Workbook.Worksheets[$"{sheetName}Meta"];
                if (metaSheet == null)
                    metaSheet = package.Workbook.Worksheets.Add($"{sheetName}Meta");

                metaSheet.Hidden = eWorkSheetHidden.VeryHidden;
                metaSheet.Cells.Clear();

                for (int col = 0; col < numCols; col++)
                {
                    var caption = table.Columns[col].Caption ?? table.Columns[col].ColumnName;
                    var internalName = table.Columns[col].ColumnName;

                    metaSheet.Cells[col + 1, 1].Value = caption;
                    metaSheet.Cells[col + 1, 2].Value = internalName;
                }

                string range = worksheet.Cells[1, 1, numRows + 1, numCols].Address;
                var newTable = worksheet.Tables.Add(worksheet.Cells[range], sheetName);
                newTable.ShowHeader = true;
                newTable.TableStyle = TableStyles.Medium2;

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                package.SaveAs(fileInfo);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryAddToTableToExcel(string filePath, string sheetName, DataTable table, Dictionary<int, ElementId> createdElements)
        {
            try
            {
                if (filePath == null) return false;
                FileInfo fileInfo = new(filePath);
                using var package = fileInfo.Exists ? new ExcelPackage(fileInfo) : new ExcelPackage();
                var worksheet = package.Workbook.Worksheets[sheetName] ?? package.Workbook.Worksheets.Add(sheetName);

                if(! TryReadSheetTable(filePath, sheetName, out DataTable? excelTable))
                {
                    if (table.Columns.Contains("Action")) table.Columns.Remove("Action");
                    return TryWriteTableToExcel(filePath, sheetName, table);
                }

                foreach (DataColumn column in table.Columns)
                {
                    if(column.ColumnName == "Action") continue;
                    if (!excelTable.Columns.Contains(column.ColumnName))
                    {
                        excelTable.Columns.Add(column.ColumnName, column.DataType);
                    }
                }


                foreach (var createdElement in createdElements)
                {
                    var key = createdElement.Key;
                    var matchingRow = excelTable.Rows.Cast<DataRow>().ElementAt(key);
                    if (matchingRow == null) continue;
                    var excelRowId = excelTable.Rows.IndexOf(matchingRow);
#if V2023
                    matchingRow["ElementId"] = createdElement.Value.IntegerValue.ToString();
#else
                    matchingRow["ElementId"] = createdElement.Value.Value.ToString();
#endif
                }
                if (table != null && table.Rows.Count > 0)
                {
                    if (!table.Columns.Contains("Action"))
                        return false;

                    List<DataRow> rowsToDelte = new List<DataRow>();
                    List<DataRow> rowsToAdd = new List<DataRow>();

                    foreach (DataRow row in table.Rows)
                    {
                        var action = row["Action"].ToString();
                        if (action == "created")
                        {
                            var newRow = excelTable.NewRow();
                            foreach (DataColumn column in table.Columns)
                            {
                                try
                                {
                                    if (column.ColumnName == "Action") continue;
                                    newRow[column.ColumnName] = row[column];
                                }
                                catch { }
                            }
                            rowsToAdd.Add(newRow);
                            continue;
                        }
                        else if (action == "edited")
                        {
                            var key = row.Table.Columns.Contains("ElementId") ? row["ElementId"]?.ToString() : row[0]?.ToString();
                            var matchingRow = excelTable.Rows.Cast<DataRow>().FirstOrDefault(e =>
                            {
                                var eKey = e.Table.Columns.Contains("ElementId") ? e["ElementId"]?.ToString() : e[0]?.ToString();
                                return eKey == key;
                            });
                            if (matchingRow == null) continue;
                            var excelRowId = excelTable.Rows.IndexOf(matchingRow);
                            foreach (DataColumn column in table.Columns)
                            {
                                try
                                {
                                    if (column.ColumnName == "Action") continue;
                                    matchingRow[column.ColumnName] = row[column];
                                }
                                catch { }
                            }
                            continue;
                        }
                        else if (action == "deleted")
                        {
                            var key = row.Table.Columns.Contains("ElementId") ? row["ElementId"]?.ToString() : row[0]?.ToString();

                            var matchingRow = excelTable.Rows.Cast<DataRow>().FirstOrDefault(e =>
                            {

                                var eKey = e.Table.Columns.Contains("ElementId") ? e["ElementId"]?.ToString() : e[0]?.ToString();
#if V2023
                                if (!int.TryParse(eKey, out int id) || new ElementId(id) == ElementId.InvalidElementId)
#else
                                if (!long.TryParse(eKey, out long id) || new ElementId(id) == ElementId.InvalidElementId)
#endif
                                {
                                    eKey = excelTable.Rows.IndexOf(e).ToString();
                                }
                                return eKey == key;
                            });
                            if (matchingRow == null) continue;
                            rowsToDelte.Add(matchingRow);
                            continue;
                        }

                    }

                    if (rowsToDelte.Any())
                    {
                        foreach (var row in rowsToDelte)
                        {
                            excelTable.Rows.Remove(row);
                        }
                    }
                    if (rowsToAdd.Any())
                    {
                        foreach (var row in rowsToAdd)
                        {
                            excelTable.Rows.Add(row);
                        }
                    }
                }
                return TryWriteTableToExcel(filePath, sheetName, excelTable);
            }
            catch
            {
                return false;
            }
        }

    }
}
