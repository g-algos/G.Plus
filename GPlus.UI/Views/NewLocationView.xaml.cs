using GPlus.UI.ViewsModels;
using System.Windows;

namespace GPlus.UI.Views
{
    /// <summary>
    /// Interaction logic for NewLocationView.xaml
    /// </summary>
    public partial class NewLocationView : Window
    {
        public NewLocationView(NewLocationVM viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += (s, e) => {
                this.DialogResult = e;
                this.Close();
            };

        }
        internal NewLocationVM ViewModel => (NewLocationVM)DataContext;
    }
}
