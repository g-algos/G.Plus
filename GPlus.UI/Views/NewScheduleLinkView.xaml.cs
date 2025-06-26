using GPlus.UI.ViewsModels;
using System.Windows;

namespace GPlus.UI.Views
{
    /// <summary>
    /// Interaction logic for NewScheduleLinkView.xaml
    /// </summary>
    public partial class NewScheduleLinkView : Window
    {
        public NewScheduleLinkView(NewScheduleLinkVM viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += (s, e) => {
                this.DialogResult = e;
                this.Close();
            };

        }
        internal NewScheduleLinkVM ViewModel => (NewScheduleLinkVM)DataContext;
    }
}
