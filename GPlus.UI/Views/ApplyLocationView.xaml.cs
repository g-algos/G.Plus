using GPlus.UI.ViewsModels;
using System.Windows;


namespace GPlus.UI.Views
{
    /// <summary>
    /// Interaction logic for ApplyLocationView.xaml
    /// </summary>
    public partial class ApplyLocationView : Window
    {
        public ApplyLocationView(ApplyLocationSchemaVM viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.ApplySchema += (s, e) =>
            {
                    this.DialogResult = true;
                    this.Close();
            };
        }
        internal ApplyLocationView ViewModel => (ApplyLocationView)DataContext;
    }
}
