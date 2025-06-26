using GPlus.UI.ViewsModels;
using System.Windows;

namespace GPlus.UI.Views
{
    /// <summary>
    /// Interaction logic for AboutView.xaml
    /// </summary>
    public partial class AboutView : Window
    {
        public AboutView(AboutVM viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
        internal AboutVM ViewModel => (AboutVM)DataContext;
    }
}
