using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GPlus.Base.Extensions;
using System.Data;
namespace GPlus.UI.ViewsModels
{
    public partial class DataLinkConflictsVM : ObservableObject
    {
        public DataView DataRvt { get; }
        private readonly DataTable _rvtTable;
        public DataView DataXls { get; }
        private readonly DataTable _xlsTable;
        public DataLinkConflictsVM(DataTable rvtTable, DataTable xlsTable)
        {

            rvtTable.Columns["Action"].ColumnMapping = MappingType.Hidden;
            xlsTable.Columns["Action"].ColumnMapping = MappingType.Hidden;
            DataColumn dcrvt = new DataColumn("Accept", typeof(bool));
            dcrvt.DefaultValue = false;
            DataColumn dcxls = new DataColumn("Accept", typeof(bool));
            dcxls.DefaultValue = false;
            rvtTable.Columns.Add(dcrvt);
            xlsTable.Columns.Add(dcxls);
            SwitchCaptionName(rvtTable);
            SwitchCaptionName(xlsTable);
            DataRvt = rvtTable.DefaultView;
            DataRvt.Sort = "ElementId ASC";
            DataXls = xlsTable.DefaultView;
            DataXls.Sort = "ElementId ASC";
            _rvtTable = rvtTable;
            _xlsTable = xlsTable;
        }
        public event EventHandler<Tuple<DataTable?, DataTable?>?> RequestMerge;
        
        [RelayCommand]
        void OnMerge()
        {
            var rvtSelectedRows = DataRvt.Table.AsEnumerable()
                           .Where(row => row.Field<bool>("Accept"))
                           .CopyToDataTableOrEmpty();
            if (rvtSelectedRows.Rows.Count > 0)
            {
                rvtSelectedRows.Columns.Remove("Accept");
                foreach (var column in rvtSelectedRows.Columns.Cast<DataColumn>())
                {
                    var dataColumn = _rvtTable.Columns.Cast<DataColumn>()
                         .FirstOrDefault(c => c.ColumnName == column.ColumnName);
                    column.ColumnName = dataColumn?.Caption ?? column.ColumnName;
                    column.Caption = dataColumn?.ColumnName ?? column.Caption;
                }
            }
            var xlsSelectedRows = DataXls.Table.AsEnumerable()
                        .Where(row => row.Field<bool>("Accept"))
                        .CopyToDataTableOrEmpty();
            if (xlsSelectedRows.Rows.Count > 0)
            {
                xlsSelectedRows.Columns.Remove("Accept");
                foreach (var column in xlsSelectedRows.Columns.Cast<DataColumn>())
                {
                    var dataColumn = _rvtTable.Columns.Cast<DataColumn>()
                         .FirstOrDefault(c => c.ColumnName == column.ColumnName);
                    column.ColumnName = dataColumn?.Caption ?? column.ColumnName;
                    column.Caption = dataColumn?.ColumnName ?? column.Caption;
                }
            }
            if (xlsSelectedRows.Rows.Count == 0 && rvtSelectedRows.Rows.Count == 0)
                return;
            
            RequestMerge?.Invoke(this, new(rvtSelectedRows, xlsSelectedRows));
        }

        private void SwitchCaptionName(DataTable table)
        {
            foreach (DataColumn column in table.Columns)
            {
                if (long.TryParse(column.ColumnName, out long id))
                {
                    string name = column.ColumnName;
                    string caption = column.Caption;
                    column.Caption = name;
                    column.ColumnName = caption;
                }

            }
        }
        private void SwitchNameCaption(DataTable table)
        {
            foreach (DataColumn column in table.Columns)
            {
                if (long.TryParse(column.Caption, out long id))
                {
                    string name = column.ColumnName;
                    string caption = column.Caption;
                    column.Caption = name;
                    column.ColumnName = caption;
                }

            }
        }
    }
}
