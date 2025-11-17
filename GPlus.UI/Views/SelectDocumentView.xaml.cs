using GPlus.UI.ViewsModels;
using System.Windows;


namespace GPlus.UI.Views
{
    /// <summary>
    /// Interaction logic for SelectModelView.xaml
    /// </summary>
    public partial class SelectDocumentView : Window
    {
        public SelectDocumentView(SelectDocumentVM viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.SelectDocument += (s, e) =>
            {
                    this.DialogResult = true;
                    this.Close();
            };
        }
        internal SelectDocumentView ViewModel => (SelectDocumentView)DataContext;
    }
}
