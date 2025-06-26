using GPlus.UI.ViewsModels;
using System.Windows;

namespace GPlus.UI.Views
{
    /// <summary>
    /// Interaction logic for NewSharedParametersView.xaml
    /// </summary>
    public partial class NewSharedParametersView : Window
    {
        public NewSharedParametersView(NewSharedParametersVM viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += (s, e) =>
            {
                this.DialogResult = e;
                this.Close();
            };
        }
        internal NewSharedParametersVM ViewModel => (NewSharedParametersVM)DataContext;
    }
}
