using GPlus.UI.ViewsModels;
using System.Windows;

namespace GPlus.UI.Views
{
    /// <summary>
    /// Interaction logic for ManageLocationSchemasView.xaml
    /// </summary>
    public partial class ManageLocationSchemasView : Window
    {
        public ManageLocationSchemasView(ManageLocationSchemasVM viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
        internal ManageLocationSchemasVM ViewModel => (ManageLocationSchemasVM)DataContext;

        private bool _isRightPanelVisible = false;

        private void Expand_Click(object sender, RoutedEventArgs e)
        {
            if (_isRightPanelVisible)
            {
                this.Width -= 200;
                RightPanel.Width = new GridLength(0);
            }
            else
            {
                this.Width += 200;
                RightPanel.Width = new GridLength(200);
            }

            _isRightPanelVisible = !_isRightPanelVisible;
        }
    }
}
