using GPlus.UI.ViewsModels;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;


namespace GPlus.UI.Views
{
    /// <summary>
    /// Interaction logic for DataLinkConflitsView.xaml
    /// </summary>
    public partial class DataLinkConflictsView : Window
    {
        public DataLinkConflictsView(DataLinkConflictsVM viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestMerge += (s, e) => this.Close();

        }
        internal DataLinkConflictsVM ViewModel => (DataLinkConflictsVM)DataContext;
        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
           
            if (e.PropertyType == typeof(bool) || e.PropertyType == typeof(bool?))
            {
                var checkBoxColumn = new DataGridCheckBoxColumn
                {
                    Header = e.Column.Header,
                    Binding = new System.Windows.Data.Binding(e.PropertyName)
                    {
                        Mode = BindingMode.TwoWay
                    },
                };

                e.Column = checkBoxColumn;
            }

            if (e.Column is DataGridTextColumn textColumn)
            {
                e.Column.IsReadOnly = true;
                textColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
            }
            else if (e.Column is DataGridCheckBoxColumn checkBoxColumn)
            {
                e.Column.IsReadOnly = false;
                checkBoxColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
            }
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header?.ToString() != "Selected")
                return;

            if (sender is not DataGrid sourceGrid)
                return;

            if (e.Row.Item is not DataRowView currentRowView)
                return;

            if (e.EditingElement is not System.Windows.Controls.CheckBox element || element.IsChecked != true)
                return;

            var currentElementId = currentRowView["ElementId"];
            if (currentElementId == null)
                return;
            var isRvtSource = sourceGrid.ItemsSource == ((dynamic)this.DataContext).DataRvt;
            var otherTableView = isRvtSource ? ((dynamic)this.DataContext).DataXls : ((dynamic)this.DataContext).DataRvt;

            foreach (var item in otherTableView)
            {
                if (item is DataRowView rowView)
                {
                    if (rowView["ElementId"].Equals(currentElementId))
                    {
                        rowView["Selected"] = false;
                    }
                }
            }
        }


    }
}
